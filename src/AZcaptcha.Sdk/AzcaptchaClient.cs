using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Azcaptcha
{
    /// <summary>Primary client: legacy + modern APIs and typed helpers.</summary>
    public sealed class AzcaptchaClient : LegacyClient
    {
        public const int DefaultSoftId = 0;
        public const string NotReady = "CAPCHA_NOT_READY";

        public AzcaptchaClient(string apiKey, AzcaptchaConfig? config = null) : base(apiKey, config)
        {
        }

        // ── modern API ────────────────────────────────────────────────
        public async Task<int> CreateTaskAsync(
            string type, IDictionary<string, object?>? task = null, CancellationToken cancellationToken = default)
        {
            var merged = new Dictionary<string, object?> { ["type"] = type };
            if (task != null)
            {
                foreach (var pair in task)
                {
                    if (pair.Value != null)
                    {
                        merged[pair.Key] = pair.Value;
                    }
                }
            }
            var payload = new Dictionary<string, object?>
            {
                ["clientKey"] = ApiKey,
                ["task"] = merged
            };
            if (SoftId > 0)
            {
                payload["softId"] = SoftId;
            }
            var body = await Http.PostJsonAsync("/createTask", payload, cancellationToken).ConfigureAwait(false);
            RaiseModern(body);
            return IntValue(body, "taskId");
        }

        public async Task<Dictionary<string, JsonElement>> GetTaskResultAsync(
            int taskId, CancellationToken cancellationToken = default)
        {
            var payload = new Dictionary<string, object?>
            {
                ["clientKey"] = ApiKey,
                ["taskId"] = taskId
            };
            var body = await Http.PostJsonAsync("/getTaskResult", payload, cancellationToken).ConfigureAwait(false);
            RaiseModern(body);
            return body;
        }

        public async Task<Dictionary<string, JsonElement>> WaitTaskResultAsync(
            int taskId, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
        {
            var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(300));
            var delay = TimeSpan.FromSeconds(1);
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                var body = await GetTaskResultAsync(taskId, cancellationToken).ConfigureAwait(false);
                if (ValueText(body, "status") == "ready")
                {
                    return body.TryGetValue("solution", out var solution) &&
                           solution.ValueKind == JsonValueKind.Object
                        ? ToDictionary(solution)
                        : new Dictionary<string, JsonElement>();
                }
                if (DateTime.UtcNow + delay >= deadline)
                {
                    throw ErrorCodes.Error("ERROR_TIMEOUT", "modern task " + taskId + " not ready");
                }
                await Delay(delay, cancellationToken).ConfigureAwait(false);
                delay = TimeSpan.FromSeconds(5);
            }
        }

        // ── helpers ───────────────────────────────────────────────────
        public Task<string> SolveAsync(
            string method, IDictionary<string, object?>? parameters = null,
            CancellationToken cancellationToken = default) =>
            SolveAsync(method, parameters, Params.TimeoutFor(method), cancellationToken);

        public async Task<string> SolveAsync(
            string method, IDictionary<string, object?>? parameters, TimeSpan timeout,
            CancellationToken cancellationToken = default)
        {
            var taskId = await SubmitAsync(method, parameters, cancellationToken).ConfigureAwait(false);
            return await WaitResultAsync(taskId, timeout, cancellationToken).ConfigureAwait(false);
        }

        public Task<string> SolveImageAsync(
            byte[] image, IDictionary<string, object?>? parameters = null,
            CancellationToken cancellationToken = default) =>
            SolveAsync("base64", Params.Merge(parameters, "body", Params.ImageBody(image)), cancellationToken);

        public Task<string> SolveImageFileAsync(
            string path, IDictionary<string, object?>? parameters = null,
            CancellationToken cancellationToken = default) =>
            SolveAsync("base64", Params.Merge(parameters, "body", Params.ImageFile(path)), cancellationToken);

        public Task<string> SolveImageBase64Async(
            string image, IDictionary<string, object?>? parameters = null,
            CancellationToken cancellationToken = default) =>
            SolveAsync("base64", Params.Merge(parameters, "body", Params.ImageBase64(image)), cancellationToken);

        public Task<string> SolveRecaptchaV2Async(
            string sitekey, string pageurl, bool invisible = false, bool enterprise = false,
            IDictionary<string, object?>? parameters = null, CancellationToken cancellationToken = default)
        {
            var values = new Dictionary<string, object?>
            {
                ["googlekey"] = sitekey,
                ["pageurl"] = pageurl
            };
            if (invisible)
            {
                values["invisible"] = 1;
            }
            if (enterprise)
            {
                values["enterprise"] = 1;
            }
            if (parameters != null)
            {
                foreach (var pair in parameters)
                {
                    values[pair.Key] = pair.Value;
                }
            }
            return SolveAsync("userrecaptcha", values, cancellationToken);
        }

        public Task<string> SolveRecaptchaV3Async(
            string sitekey, string pageurl, string action, double minScore = 0.6,
            IDictionary<string, object?>? parameters = null, CancellationToken cancellationToken = default)
        {
            var values = new Dictionary<string, object?>
            {
                ["googlekey"] = sitekey,
                ["pageurl"] = pageurl,
                ["version"] = "v3",
                ["action"] = action,
                ["min_score"] = minScore
            };
            if (parameters != null)
            {
                foreach (var pair in parameters)
                {
                    values[pair.Key] = pair.Value;
                }
            }
            return SolveAsync("userrecaptcha", values, cancellationToken);
        }

        public Task<string> SolveHCaptchaAsync(
            string sitekey, string pageurl, IDictionary<string, object?>? parameters = null,
            CancellationToken cancellationToken = default) =>
            SolveAsync("hcaptcha", Params.Merge(parameters, "sitekey", sitekey, "pageurl", pageurl), cancellationToken);

        public Task<string> SolveTurnstileAsync(
            string sitekey, string pageurl, IDictionary<string, object?>? parameters = null,
            CancellationToken cancellationToken = default) =>
            SolveAsync("turnstile", Params.Merge(parameters, "sitekey", sitekey, "pageurl", pageurl), cancellationToken);

        public Task<string> SolveFunCaptchaAsync(
            string publickey, string pageurl, IDictionary<string, object?>? parameters = null,
            CancellationToken cancellationToken = default) =>
            SolveAsync("funcaptcha", Params.Merge(parameters, "publickey", publickey, "pageurl", pageurl), cancellationToken);

        public Task<string> SolveGeeTestAsync(
            string gt, string challenge, string pageurl, IDictionary<string, object?>? parameters = null,
            CancellationToken cancellationToken = default) =>
            SolveAsync("geetest", Params.Merge(parameters, "gt", gt, "challenge", challenge, "pageurl", pageurl),
                cancellationToken);

        private static Dictionary<string, JsonElement> ToDictionary(JsonElement element)
        {
            var result = new Dictionary<string, JsonElement>();
            foreach (var property in element.EnumerateObject())
            {
                result[property.Name] = property.Value.Clone();
            }
            return result;
        }
    }
}
