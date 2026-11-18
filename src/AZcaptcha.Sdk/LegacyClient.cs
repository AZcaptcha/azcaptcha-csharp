using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Azcaptcha
{
    /// <summary>Legacy in.php/res.php surface plus contract polling.</summary>
    public class LegacyClient : BaseClient
    {
        protected LegacyClient(string apiKey, AzcaptchaConfig? config) : base(apiKey, config)
        {
        }

        public async Task<string> SubmitAsync(
            string method, IDictionary<string, object?>? parameters = null, CancellationToken cancellationToken = default)
        {
            var form = new Dictionary<string, string?>
            {
                ["key"] = ApiKey,
                ["method"] = method,
                ["json"] = "1"
            };
            if (SoftId > 0)
            {
                form["soft_id"] = SoftId.ToString();
            }
            var extra = new Dictionary<string, object?>();
            if (parameters != null)
            {
                foreach (var pair in parameters)
                {
                    extra[pair.Key] = pair.Value;
                }
            }
            if (extra.TryGetValue("proxy", out var proxy) && proxy is string proxyText && proxyText.Length > 0)
            {
                var normalized = Params.NormalizeProxy(proxyText);
                form["proxy"] = normalized.Proxy;
                form["proxytype"] = normalized.ProxyType;
                extra.Remove("proxy");
            }
            if (extra.TryGetValue("proxytype", out var proxyType) && proxyType != null)
            {
                form["proxytype"] = proxyType.ToString()!.ToUpperInvariant();
                extra.Remove("proxytype");
            }
            foreach (var pair in extra)
            {
                if (pair.Value != null)
                {
                    form[pair.Key] = Convert.ToString(pair.Value, System.Globalization.CultureInfo.InvariantCulture);
                }
            }
            var body = await Http.PostFormAsync("/in.php", form, cancellationToken).ConfigureAwait(false);
            return Unwrap(body);
        }

        public async Task<string> ResultAsync(string taskId, CancellationToken cancellationToken = default)
        {
            var query = new Dictionary<string, string?>
            {
                ["key"] = ApiKey,
                ["action"] = "get",
                ["id"] = taskId,
                ["json"] = "1"
            };
            var body = await Http.GetAsync("/res.php", query, cancellationToken).ConfigureAwait(false);
            if (ValueText(body, "request") == "CAPCHA_NOT_READY")
            {
                throw ErrorCodes.Error("CAPCHA_NOT_READY");
            }
            return Unwrap(body);
        }

        public async Task<string> WaitResultAsync(
            string taskId, TimeSpan? timeout = null, CancellationToken cancellationToken = default)
        {
            var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(300));
            var delay = TimeSpan.FromSeconds(1);
            var backoff = TimeSpan.FromSeconds(10);
            while (true)
            {
                cancellationToken.ThrowIfCancellationRequested();
                TimeSpan pause;
                try
                {
                    return await ResultAsync(taskId, cancellationToken).ConfigureAwait(false);
                }
                catch (AzcaptchaException error)
                {
                    if (error.Code == "CAPCHA_NOT_READY")
                    {
                        pause = delay;
                        delay = TimeSpan.FromSeconds(5);
                    }
                    else if (error.Category == ErrorCategory.Capacity ||
                             (error.Category == ErrorCategory.Limit && error.Retryable))
                    {
                        pause = backoff;
                        backoff = TimeSpan.FromSeconds(Math.Min(backoff.TotalSeconds * 1.5, 60));
                    }
                    else
                    {
                        throw;
                    }
                }
                if (DateTime.UtcNow + pause >= deadline)
                {
                    throw ErrorCodes.Error("ERROR_TIMEOUT", "task " + taskId + " not ready");
                }
                await Delay(pause, cancellationToken).ConfigureAwait(false);
            }
        }

        public async Task<double> BalanceAsync(CancellationToken cancellationToken = default)
        {
            var query = new Dictionary<string, string?>
            {
                ["key"] = ApiKey,
                ["action"] = "getbalance",
                ["json"] = "1"
            };
            var body = await Http.GetAsync("/res.php", query, cancellationToken).ConfigureAwait(false);
            return double.Parse(Unwrap(body), System.Globalization.CultureInfo.InvariantCulture);
        }

        public async Task<bool> ReportBadAsync(string taskId, CancellationToken cancellationToken = default)
        {
            var query = new Dictionary<string, string?>
            {
                ["key"] = ApiKey,
                ["action"] = "reportbad",
                ["id"] = taskId,
                ["json"] = "1"
            };
            var body = await Http.GetAsync("/res.php", query, cancellationToken).ConfigureAwait(false);
            return Unwrap(body).StartsWith("OK", StringComparison.OrdinalIgnoreCase);
        }
    }
}
