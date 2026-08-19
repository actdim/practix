using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ActDim.Observability.VictoriaLogs
{
    /// <summary>
    /// Lightweight HTTP client for VictoriaLogs JSON Lines ingestion (<c>/insert/jsonline</c>) and LogsQL query processing (<c>/select/logsql/query</c>).
    /// </summary>
    public sealed class VictoriaLogsClient
    {
        private readonly HttpClient _httpClient;
        private readonly VictoriaLogsOptions _options;

        public VictoriaLogsClient(HttpClient httpClient, VictoriaLogsOptions options)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public VictoriaLogsClient(VictoriaLogsOptions options)
            : this(new HttpClient(), options)
        {
        }

        public VictoriaLogsOptions Options => _options;

        /// <summary>
        /// Checks if the VictoriaLogs instance is available at <see cref="VictoriaLogsOptions.BaseUrl"/>.
        /// </summary>
        public async Task<bool> IsServerAvailableAsync(CancellationToken ct = default)
        {
            try
            {
                var uri = new Uri(new Uri(_options.BaseUrl), "/metrics");
                using var request = new HttpRequestMessage(HttpMethod.Get, uri);
                using var response = await _httpClient.SendAsync(request, ct);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Ingests a set of structured log records formatted as NDJSON lines into VictoriaLogs via <c>/insert/jsonline</c>.
        /// </summary>
        public async Task IngestRecordsAsync(IEnumerable<Dictionary<string, object?>> records, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(records, nameof(records));

            var sb = new StringBuilder();
            foreach (var record in records)
            {
                if (!record.ContainsKey("_stream") && !string.IsNullOrEmpty(_options.Stream))
                {
                    record["_stream"] = _options.Stream;
                }

                if (!record.ContainsKey("_time"))
                {
                    record["_time"] = DateTimeOffset.UtcNow.ToString("O");
                }

                var jsonLine = JsonSerializer.Serialize(record);
                sb.AppendLine(jsonLine);
            }

            if (sb.Length == 0) return;

            var uri = new Uri(new Uri(_options.BaseUrl), "/insert/jsonline");
            using var content = new StringContent(sb.ToString(), Encoding.UTF8, "application/x-ndjson");
            using var response = await _httpClient.PostAsync(uri, content, ct);
            response.EnsureSuccessStatusCode();
        }

        /// <summary>
        /// Executes a LogsQL query against VictoriaLogs (<c>/select/logsql/query</c>) and returns the resulting log records.
        /// </summary>
        /// <param name="logsQlQuery">The LogsQL query string (e.g. <c>_stream:{app="actdim"} AND level:info</c>).</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A list of JSON objects representing matched log entries.</returns>
        public async Task<IReadOnlyList<Dictionary<string, object?>>> QueryLogsQLAsync(string logsQlQuery, CancellationToken ct = default)
        {
            ArgumentException.ThrowIfNullOrEmpty(logsQlQuery, nameof(logsQlQuery));

            var uri = new Uri(new Uri(_options.BaseUrl), $"/select/logsql/query?query={Uri.EscapeDataString(logsQlQuery)}");
            using var response = await _httpClient.GetAsync(uri, ct);
            response.EnsureSuccessStatusCode();

            var jsonContent = await response.Content.ReadAsStringAsync(ct);
            var results = new List<Dictionary<string, object?>>();

            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                return results;
            }

            using var reader = new StringReader(jsonContent);
            string? line;
            while ((line = await reader.ReadLineAsync(ct)) != null)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                using var doc = JsonDocument.Parse(line);
                var dict = ConvertJsonElementToDictionary(doc.RootElement);
                results.Add(dict);
            }

            return results;
        }

        private static Dictionary<string, object?> ConvertJsonElementToDictionary(JsonElement element)
        {
            var dict = new Dictionary<string, object?>();
            if (element.ValueKind != JsonValueKind.Object) return dict;

            foreach (var prop in element.EnumerateObject())
            {
                dict[prop.Name] = ConvertJsonValue(prop.Value);
            }

            return dict;
        }

        private static object? ConvertJsonValue(JsonElement val)
        {
            return val.ValueKind switch
            {
                JsonValueKind.String => val.GetString(),
                JsonValueKind.Number => val.TryGetInt64(out var l) ? l : val.GetDouble(),
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                JsonValueKind.Null => null,
                JsonValueKind.Object => ConvertJsonElementToDictionary(val),
                _ => val.ToString()
            };
        }
    }
}
