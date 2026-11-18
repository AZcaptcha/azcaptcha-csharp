using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;

namespace Azcaptcha
{
    /// <summary>Proxy and image parameter normalisation.</summary>
    public static class Params
    {
        public static readonly IReadOnlyDictionary<string, TimeSpan> DefaultTimeouts =
            new Dictionary<string, TimeSpan>
            {
                ["base64"] = TimeSpan.FromSeconds(120),
                ["grid"] = TimeSpan.FromSeconds(120),
                ["userrecaptcha"] = TimeSpan.FromSeconds(300),
                ["recaptcha_v3"] = TimeSpan.FromSeconds(300),
                ["hcaptcha"] = TimeSpan.FromSeconds(120),
                ["hcaptcha_enterprise"] = TimeSpan.FromSeconds(120),
                ["turnstile"] = TimeSpan.FromSeconds(180),
                ["funcaptcha"] = TimeSpan.FromSeconds(300),
                ["geetest"] = TimeSpan.FromSeconds(300),
                ["geetest_v4"] = TimeSpan.FromSeconds(300),
                ["mtcaptcha"] = TimeSpan.FromSeconds(300),
                ["friendlycaptcha"] = TimeSpan.FromSeconds(300),
                ["altcha"] = TimeSpan.FromSeconds(300),
                ["cf_clearance"] = TimeSpan.FromSeconds(300),
                ["aws_waf"] = TimeSpan.FromSeconds(300),
                ["filecrypt"] = TimeSpan.FromSeconds(300)
            };

        /// <summary>Any accepted proxy spelling -> (proxy, proxytype); the scheme is never sent.</summary>
        public static (string Proxy, string ProxyType) NormalizeProxy(string proxy)
        {
            var raw = proxy.Trim();
            var proxyType = "HTTP";
            var schemeIndex = raw.IndexOf("://", StringComparison.Ordinal);
            if (schemeIndex >= 0)
            {
                switch (raw.Substring(0, schemeIndex).ToLowerInvariant())
                {
                    case "socks4": proxyType = "SOCKS4"; break;
                    case "socks5": proxyType = "SOCKS5"; break;
                    default: proxyType = "HTTP"; break;
                }
                raw = raw.Substring(schemeIndex + 3);
            }
            var parts = raw.Split(':');
            if (parts.Length == 4 && Regex.IsMatch(parts[1], "^\\d+$"))
            {
                raw = parts[2] + ":" + parts[3] + "@" + parts[0] + ":" + parts[1];
            }
            else if (!raw.Contains("@"))
            {
                var portIndex = raw.LastIndexOf(':');
                if (portIndex < 0 || !Regex.IsMatch(raw.Substring(portIndex + 1), "^\\d+$"))
                {
                    throw new ArgumentException("proxy must include a numeric port", nameof(proxy));
                }
            }
            return (raw, proxyType);
        }

        public static string ImageBody(byte[] image) => Convert.ToBase64String(image);

        public static string ImageFile(string path)
        {
            try
            {
                return ImageBody(File.ReadAllBytes(path));
            }
            catch (IOException error)
            {
                throw ErrorCodes.Error("ERROR_IMAGE_NOT_FOUND", error.Message);
            }
        }

        public static string ImageBase64(string image)
        {
            var value = image.Trim();
            var comma = value.IndexOf(',');
            if (value.StartsWith("data:", StringComparison.Ordinal) && comma >= 0)
            {
                value = value.Substring(comma + 1);
            }
            return Regex.Replace(value, "\\s+", "");
        }

        internal static TimeSpan TimeoutFor(string method) =>
            DefaultTimeouts.TryGetValue(method, out var timeout) ? timeout : TimeSpan.FromSeconds(300);

        internal static Dictionary<string, object?> Merge(
            IDictionary<string, object?>? baseParams, params object?[] pairs)
        {
            var result = new Dictionary<string, object?>();
            if (baseParams != null)
            {
                foreach (var pair in baseParams)
                {
                    result[pair.Key] = pair.Value;
                }
            }
            for (var i = 0; i + 1 < pairs.Length; i += 2)
            {
                result[(string)pairs[i]!] = pairs[i + 1];
            }
            return result;
        }
    }
}
