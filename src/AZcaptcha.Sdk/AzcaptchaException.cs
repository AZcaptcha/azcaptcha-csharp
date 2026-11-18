using System;

namespace Azcaptcha
{
    /// <summary>Error categories from spec/errors.json.</summary>
    public enum ErrorCategory
    {
        Processing,
        Auth,
        Balance,
        Request,
        Limit,
        Capacity,
        Service,
        Result,
        Proxy,
        Pingback,
        Module,
        Network,
        Unknown
    }

    /// <summary>Typed error carrying the API code, category and retryable flag.</summary>
    public sealed class AzcaptchaException : Exception
    {
        public string Code { get; }

        public ErrorCategory Category { get; }

        public bool Retryable { get; }

        public AzcaptchaException(string code, ErrorCategory category, bool retryable, string? message = null)
            : base(message ?? code)
        {
            Code = code;
            Category = category;
            Retryable = retryable;
        }
    }
}
