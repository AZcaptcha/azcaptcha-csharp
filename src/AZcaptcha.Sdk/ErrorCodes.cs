using System.Collections.Generic;

namespace Azcaptcha
{
    /// <summary>Maps every API code from spec/errors.json to a category and retry flag.</summary>
    public static class ErrorCodes
    {
        private static readonly Dictionary<string, (ErrorCategory Category, bool Retryable)> Table =
            Build();

        public static AzcaptchaException Error(string code, string? message = null)
        {
            var meta = Table.TryGetValue(code, out var found)
                ? found
                : (ErrorCategory.Unknown, false);
            return new AzcaptchaException(code, meta.Item1, meta.Item2, message);
        }

        public static IReadOnlyDictionary<string, bool> Taxonomy()
        {
            var result = new Dictionary<string, bool>();
            foreach (var pair in Table)
            {
                result[pair.Key] = pair.Value.Retryable;
            }
            return result;
        }

        private static Dictionary<string, (ErrorCategory, bool)> Build()
        {
            var table = new Dictionary<string, (ErrorCategory, bool)>
            {
                ["CAPCHA_NOT_READY"] = (ErrorCategory.Processing, true),
                ["ERROR_KEY_DOES_NOT_EXIST"] = (ErrorCategory.Auth, false),
                ["ERROR_ACCOUNT_SUSPENDED"] = (ErrorCategory.Auth, false),
                ["ERROR_ZERO_BALANCE"] = (ErrorCategory.Balance, false),
                ["ERROR_BAD_PARAMETERS"] = (ErrorCategory.Request, false),
                ["ERROR_WRONG_ID_FORMAT"] = (ErrorCategory.Request, false),
                ["ERROR_NO_SUCH_CAPCHA_ID"] = (ErrorCategory.Request, false),
                ["ERROR_TASK_NOT_FOUND"] = (ErrorCategory.Request, false),
                ["ERROR_TASK_NOT_SUPPORTED"] = (ErrorCategory.Request, false),
                ["ERROR_WRONG_GOOGLEKEY"] = (ErrorCategory.Request, false),
                ["ERROR_RECAPTCHA_INVALID_SITEKEY"] = (ErrorCategory.Request, false),
                ["ERROR_WRONG_PAGEURL"] = (ErrorCategory.Request, false),
                ["ERROR_IMAGE_NOT_FOUND"] = (ErrorCategory.Request, false),
                ["ERROR_IMAGE_TYPE_NOT_SUPPORTED"] = (ErrorCategory.Request, false),
                ["ERROR_ZERO_CAPTCHA_FILESIZE"] = (ErrorCategory.Request, false),
                ["ERROR_TOO_BIG_CAPTCHA_FILESIZE"] = (ErrorCategory.Request, false),
                ["ERROR_DAILY_LIMIT_EXCEEDED"] = (ErrorCategory.Limit, false),
                ["ERROR_TOO_MANY_THREADS"] = (ErrorCategory.Limit, true),
                ["ERROR_NO_SLOT_AVAILABLE"] = (ErrorCategory.Capacity, true),
                ["ERROR_MAINTENANCE"] = (ErrorCategory.Service, false),
                ["ERROR_TIMEOUT"] = (ErrorCategory.Result, false),
                ["ERROR_TTL"] = (ErrorCategory.Result, false),
                ["ERROR_TOKEN_EXPIRED"] = (ErrorCategory.Result, false),
                ["ERROR_CAPTCHA_UNSOLVABLE"] = (ErrorCategory.Result, false),
                ["ERROR_PROXY_REQUIRED"] = (ErrorCategory.Proxy, false),
                ["ERROR_PROXY_NOT_AUTHORIZED"] = (ErrorCategory.Proxy, false),
                ["ERROR_PROXY_BANNED"] = (ErrorCategory.Proxy, false),
                ["ERROR_PINGBACK_NOT_VERIFIED"] = (ErrorCategory.Pingback, false),
                ["ERROR_MODULE_ACCESS_DENIED"] = (ErrorCategory.Module, false),
                ["ERROR_MODULE_NOT_TRAINED"] = (ErrorCategory.Module, false)
            };
            return table;
        }
    }
}
