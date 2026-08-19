using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ActDim.Observability.Tests.OpenObserve
{
    /// <summary>
    /// Lightweight HTTP client for OpenObserve JSON log ingestion (<c>/api/{org}/{stream}/_json</c>) and SQL search processing (<c>/api/{org}/_search</c>).
    /// </summary>
    public sealed class OpenObserveClient
    {
        private readonly HttpClient _httpClient;
        private readonly OpenObserveOptions _options;

        public OpenObserveClient(HttpClient httpClient, OpenObserveOptions options)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            _options = options ?? throw new ArgumentNullException(nameof(options));
        }

        public OpenObserveClient(OpenObserveOptions options)
            : this(new HttpClient(), options)
        {
        }

        public OpenObserveOptions Options => _options;

        public async Task<bool> IsServerAvailableAsync(CancellationToken ct = default)
        {
            try
            {
                var uri = new Uri(new Uri(_options.BaseUrl), "/healthz");
                using var request = new HttpRequestMessage(HttpMethod.Get, uri);
                using var response = await _httpClient.SendAsync(request, ct);
                return response.IsSuccessStatusCode;
            }
            catch
            {
                try
                {
                    var uri = new Uri(new Uri(_options.BaseUrl), "/");
                    using var request = new HttpRequestMessage(HttpMethod.Get, uri);
                    using var response = await _httpClient.SendAsync(request, ct);
                    return response.IsSuccessStatusCode;
                }
                catch
                {
                    return false;
                }
            }
        }

        public async Task IngestRecordsAsync(IEnumerable<Dictionary<string, object?>> records, CancellationToken ct = default)
        {
            ArgumentNullException.ThrowIfNull(records, nameof(records));

            var recordList = new List<Dictionary<string, object?>>(records);
            if (recordList.Count == 0) return;

            var microsecondEpoch = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() * 1000;

            foreach (var record in recordList)
            {
                if (!record.ContainsKey("_timestamp"))
                {
                    record["_timestamp"] = microsecondEpoch;
                }
            }

            var uri = new Uri(new Uri(_options.BaseUrl), $"/api/{_options.Organization}/{_options.Stream}/_json");
            var jsonBody = JsonSerializer.Serialize(recordList);

            using var request = new HttpRequestMessage(HttpMethod.Post, uri);
            request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            var authHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_options.UserEmail}:{_options.UserPassword}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authHeader);

            using var response = await _httpClient.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();
        }

        public async Task<IReadOnlyList<Dictionary<string, object?>>> QuerySqlQueryAsync(string sqlQuery, CancellationToken ct = default)
        {
            if (string.IsNullOrEmpty(sqlQuery)) throw new ArgumentException("SQL Query cannot be null or empty", nameof(sqlQuery));

            var uri = new Uri(new Uri(_options.BaseUrl), $"/api/{_options.Organization}/_search?query_type=logs");
            var payload = new
            {
                query = new
                {
                    sql = sqlQuery,
                    start_time = 0,
                    end_time = 0,
                    from = 0,
                    size = 100
                }
            };

            var jsonBody = JsonSerializer.Serialize(payload);
            using var request = new HttpRequestMessage(HttpMethod.Post, uri);
            request.Content = new StringContent(jsonBody, Encoding.UTF8, "application/json");

            var authHeader = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{_options.UserEmail}:{_options.UserPassword}"));
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", authHeader);

            using var response = await _httpClient.SendAsync(request, ct);
            response.EnsureSuccessStatusCode();

            var jsonContent = await response.Content.ReadAsStringAsync(ct);
            var results = new List<Dictionary<string, object?>>();

            if (string.IsNullOrWhiteSpace(jsonContent))
            {
                return results;
            }

            using var doc = JsonDocument.Parse(jsonContent);
            if (doc.RootElement.TryGetProperty("hits", out var hits) && hits.ValueKind == JsonValueKind.Array)
            {
                foreach (var hit in hits.EnumerateArray())
                {
                    results.Add(ConvertJsonElementToDictionary(hit));
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
                _ => val.ToString()
            };
        }
    }
}
