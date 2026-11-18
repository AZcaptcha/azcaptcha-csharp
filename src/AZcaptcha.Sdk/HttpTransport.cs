using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Azcaptcha
{
    /// <summary>Default transport — HttpClient, JSON envelopes only.</summary>
    public sealed class HttpTransport : ITransport
    {
        private const string UserAgent = "azcaptcha-csharp/1.0.0";
        private readonly HttpClient _client;
        private readonly string _baseUrl;

        public HttpTransport(string baseUrl, TimeSpan timeout)
        {
            _baseUrl = baseUrl.TrimEnd('/');
            _client = new HttpClient { Timeout = timeout };
        }

        public Task<Dictionary<string, JsonElement>> PostFormAsync(
            string path, IDictionary<string, string?> form, CancellationToken cancellationToken)
        {
            return SendAsync(HttpMethod.Post, path, new FormUrlEncodedContent(Clean(form)), cancellationToken);
        }

        public Task<Dictionary<string, JsonElement>> PostJsonAsync(
            string path, object payload, CancellationToken cancellationToken)
        {
            var json = JsonSerializer.Serialize(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            return SendAsync(HttpMethod.Post, path, content, cancellationToken);
        }

        public Task<Dictionary<string, JsonElement>> GetAsync(
            string path, IDictionary<string, string?> query, CancellationToken cancellationToken)
        {
            var pairs = new List<string>();
            foreach (var pair in query)
            {
                if (pair.Value != null)
                {
                    pairs.Add(Uri.EscapeDataString(pair.Key) + "=" + Uri.EscapeDataString(pair.Value));
                }
            }
            var suffix = pairs.Count == 0 ? string.Empty : "?" + string.Join("&", pairs);
            return SendAsync(HttpMethod.Get, path + suffix, null, cancellationToken);
        }

        private async Task<Dictionary<string, JsonElement>> SendAsync(
            HttpMethod method, string path, HttpContent? content, CancellationToken cancellationToken)
        {
            using (var request = new HttpRequestMessage(method, _baseUrl + path))
            {
                request.Headers.TryAddWithoutValidation("Accept", "application/json");
                request.Headers.TryAddWithoutValidation("User-Agent", UserAgent);
                request.Content = content;
                HttpResponseMessage response;
                try
                {
                    response = await _client.SendAsync(request, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception error) when (error is HttpRequestException || error is TaskCanceledException)
                {
                    throw new AzcaptchaException("ERROR_NETWORK", ErrorCategory.Network, true, error.Message);
                }
                using (response)
                {
                    var body = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    var status = (int)response.StatusCode;
                    if (status >= 500)
                    {
                        throw ErrorCodes.Error("ERROR_HTTP_5XX", "HTTP " + status);
                    }
                    try
                    {
                        using (var document = JsonDocument.Parse(body))
                        {
                            if (document.RootElement.ValueKind != JsonValueKind.Object)
                            {
                                throw ErrorCodes.Error("ERROR_BAD_RESPONSE", body);
                            }
                            var result = new Dictionary<string, JsonElement>();
                            foreach (var property in document.RootElement.EnumerateObject())
                            {
                                result[property.Name] = property.Value.Clone();
                            }
                            return result;
                        }
                    }
                    catch (JsonException)
                    {
                        throw ErrorCodes.Error("ERROR_BAD_RESPONSE", body.Length > 200 ? body.Substring(0, 200) : body);
                    }
                }
            }
        }

        private static IEnumerable<KeyValuePair<string, string>> Clean(IDictionary<string, string?> form)
        {
            foreach (var pair in form)
            {
                if (pair.Value != null)
                {
                    yield return new KeyValuePair<string, string>(pair.Key, pair.Value);
                }
            }
        }
    }
}
