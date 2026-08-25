using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ActDim.Observability.Tests.Seq
{
    /// <summary>
    /// Lightweight HTTP client for Seq raw CLEF ingestion (<c>/api/events/raw</c>) and event querying (<c>/api/events</c>).
    /// </summary>
    public sealed class SeqClient
    {
        private readonly HttpClient _httpClient;
        private readonly SeqOptions _options;

        public SeqClient(HttpClient httpClient, SeqOptions options)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public SeqClient(SeqOptions options)
            : this(new HttpClient(), options)
        {
        }

        public SeqOptions Options => _options;

        public async Task<bool> IsServerAvailableAsync(CancellationToken ct = default)
        {
            try
            {
                var uri = new Uri(new Uri(_options.BaseUrl), "/api/health");
                using var request = new HttpRequestMessage(HttpMethod.Get, uri);
                using var response = await _httpClient.SendAsync(request, ct);
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }

                // Fallback check on root API endpoint
                var apiUri = new Uri(new Uri(_options.BaseUrl), "/api");
                using var apiRequest = new HttpRequestMessage(HttpMethod.Get, apiUri);
                using var apiResponse = await _httpClient.SendAsync(apiRequest, ct);
                return apiResponse.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

        public async Task IngestClefRecordsAsync(IEnumerable<Dictionary<string, object?>> records, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(records, nameof(records));

            var sb = new StringBuilder();
            foreach (var record in records)
            {
                if (!record.ContainsKey("@t"))
                {
                    record["@t"] = DateTimeOffset.UtcNow.ToString("O");
                }

                var jsonLine = JsonSerializer.Serialize(record);
                sb.AppendLine(jsonLine);
            }

            if (sb.Length == 0) return;

            var apiKeyParam = !string.IsNullOrEmpty(_options.ApiKey) ? $"?apiKey={Uri.EscapeDataString(_options.ApiKey)}" : "";
            var uri = new Uri(new Uri(_options.BaseUrl), $"/api/events/raw{apiKeyParam}");
            using var content = new StringContent(sb.ToString(), Encoding.UTF8, "application/vnd.serilog.clef");
            using var response = await _httpClient.PostAsync(uri, content, ct);
            response.EnsureSuccessStatusCode();
        }

        public async Task<IReadOnlyList<Dictionary<string, object?>>> QueryEventsAsync(string? filter = null, CancellationToken ct = default)
        {
            var filterParam = !string.IsNullOrEmpty(filter) ? $"&filter={Uri.EscapeDataString(filter)}" : "";
            var apiKeyParam = !string.IsNullOrEmpty(_options.ApiKey) ? $"&apiKey={Uri.EscapeDataString(_options.ApiKey)}" : "";
            var uri = new Uri(new Uri(_options.BaseUrl), $"/api/events?count=50{filterParam}{apiKeyParam}");

            using var response = await _httpClient.GetAsync(uri, ct);
            response.EnsureSuccessStatusCode();

            var jsonContent = await response.Content.ReadAsStringAsync(ct);
            var results = new List<Dictionary<string, object?>>();

            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                return results;
            }

            using var doc = JsonDocument.Parse(jsonContent);
            var root = doc.RootElement;

            JsonElement itemsElement = default;
            if (root.ValueKind == JsonValueKind.Array)
            {
                itemsElement = root;
            }
            else if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("Items", out var itemsProp) && itemsProp.ValueKind == JsonValueKind.Array)
            {
                itemsElement = itemsProp;
            }

            if (itemsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in itemsElement.EnumerateArray())
                {
                    var dict = ConvertJsonElementToDictionary(item);
                    results.Add(dict);
                }
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
                JsonValueKind.Array => ConvertJsonArrayToList(val),
                _ => val.ToString()
            };
        }

        private static List<object?> ConvertJsonArrayToList(JsonElement val)
        {
            var list = new List<object?>();
            foreach (var item in val.EnumerateArray())
            {
                list.Add(ConvertJsonValue(item));
            }
            return list;
        }
    }
}
