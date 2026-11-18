using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Azcaptcha
{
    /// <summary>Shared state and envelope handling.</summary>
    public abstract class BaseClient
    {
        protected BaseClient(string apiKey, AzcaptchaConfig? config)
        {
            if (string.IsNullOrEmpty(apiKey))
            {
                throw ErrorCodes.Error("ERROR_KEY_DOES_NOT_EXIST", "apiKey is required");
            }
            config ??= new AzcaptchaConfig();
            ApiKey = apiKey;
            SoftId = config.SoftId;
            Http = config.Transport ?? new HttpTransport(config.Host, config.HttpTimeout);
            Delay = config.Delay;
        }

        protected string ApiKey { get; }

        protected int SoftId { get; }

        protected ITransport Http { get; }

        protected Func<TimeSpan, CancellationToken, Task> Delay { get; }

        protected static string Unwrap(IDictionary<string, JsonElement> body)
        {
            var request = ValueText(body, "request");
            if (request != null && request.StartsWith("ERROR_", StringComparison.Ordinal))
            {
                throw ErrorCodes.Error(request);
            }
            if (IntValue(body, "status") != 1)
            {
                throw ErrorCodes.Error(request ?? "ERROR_REQUEST_FAILED");
            }
            return request ?? string.Empty;
        }

        protected static void RaiseModern(IDictionary<string, JsonElement> body)
        {
            if (IntValue(body, "errorId") == 0)
            {
                return;
            }
            var code = ValueText(body, "errorCode") ?? "ERROR_UNKNOWN";
            throw ErrorCodes.Error(code, ValueText(body, "errorDescription"));
        }

        protected static string? ValueText(IDictionary<string, JsonElement> body, string key)
        {
            if (!body.TryGetValue(key, out var element))
            {
                return null;
            }
            return element.ValueKind == JsonValueKind.String ? element.GetString() : element.ToString();
        }

        protected static int IntValue(IDictionary<string, JsonElement> body, string key)
        {
            if (!body.TryGetValue(key, out var element) || element.ValueKind != JsonValueKind.Number)
            {
                return 0;
            }
            return element.TryGetInt32(out var value) ? value : 0;
        }

        protected static double DoubleValue(IDictionary<string, JsonElement> body, string key)
        {
            if (!body.TryGetValue(key, out var element) || element.ValueKind != JsonValueKind.Number)
            {
                return 0;
            }
            return element.GetDouble();
        }
    }
}
