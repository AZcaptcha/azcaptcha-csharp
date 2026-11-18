# AZcaptcha .NET SDK

Official .NET client for [AZcaptcha](https://azcaptcha.com) — image captchas,
reCAPTCHA v2/v3, hCaptcha, Cloudflare Turnstile, FunCaptcha and GeeTest.

- Async/await throughout (`Task`), `CancellationToken` on every call.
- Targets `netstandard2.0` and `net8.0`.
- 2Captcha-compatible surface (`SolveAsync`, `ResultAsync`, `BalanceAsync`,
  `ReportBadAsync`) plus the modern JSON API (`CreateTaskAsync`,
  `GetTaskResultAsync`).
- Typed `AzcaptchaException` with `Code`, `Category` and `Retryable`
  ([spec-v1.0.0](https://github.com/AZcaptcha/sdk-spec)).
- Contract polling with `NO_SLOT` backoff and per-type deadlines.

## Install

```bash
dotnet add package AZcaptcha.Sdk
```

## Quickstart

```csharp
using Azcaptcha;

var client = new AzcaptchaClient("YOUR_API_KEY");

// reCAPTCHA v2
string token = await client.SolveRecaptchaV2Async(
    "6Le-wvkSAAAAAPBMRTvw0Q4Muexq9bi0DJwx_mJ-",
    "https://example.com/login");

// Image captcha (bytes, file path or base64/data: URI)
string answer = await client.SolveImageFileAsync("captcha.png");

// hCaptcha / Turnstile
string h = await client.SolveHCaptchaAsync("SITEKEY", "https://example.com/");
string t = await client.SolveTurnstileAsync("0x4AAAAAAA...", "https://example.com/");
```

### Low-level (2Captcha style)

```csharp
string taskId = await client.SubmitAsync("userrecaptcha", new Dictionary<string, object?>
{
    ["googlekey"] = "SITEKEY",
    ["pageurl"] = "https://example.com/"
});
string token = await client.WaitResultAsync(taskId, TimeSpan.FromSeconds(300));
```

### Modern JSON API

```csharp
int taskId = await client.CreateTaskAsync("RecaptchaV2TaskProxyless",
    new Dictionary<string, object?>
    {
        ["websiteURL"] = "https://example.com/login",
        ["websiteKey"] = "SITEKEY"
    });
var solution = await client.WaitTaskResultAsync(taskId);
Console.WriteLine(solution["gRecaptchaResponse"].GetString());
```

## Proxies

```csharp
await client.SolveRecaptchaV2Async(sitekey, pageurl, invisible: false, enterprise: false,
    parameters: new Dictionary<string, object?>
    {
        ["proxy"] = "socks5://user:pass@1.2.3.4:1080"
    });
```

Any accepted spelling works (`scheme://user:pass@host:port`,
`host:port:user:pass`, `user:pass@host:port`). The scheme is never sent
upstream; `proxytype` is derived (`http`/`https` → `HTTP`, `socks4`, `socks5`).

## Errors

```csharp
try
{
    string token = await client.SolveTurnstileAsync(sitekey, pageurl);
}
catch (AzcaptchaException error) when (error.Category == ErrorCategory.Balance)
{
    // ERROR_ZERO_BALANCE
}
catch (AzcaptchaException error) when (error.Category == ErrorCategory.Capacity)
{
    // ERROR_NO_SLOT_AVAILABLE — retry with backoff
}
```

`WaitResultAsync` retries only codes marked retryable and backs off 10s ×1.5 up
to 60s.

## Timeouts

Defaults match server settings: image 120s, hCaptcha 120s, Turnstile 180s,
reCAPTCHA and others 300s. Override with
`client.SolveAsync(method, parameters, TimeSpan.FromSeconds(90))`.

## Custom transport

```csharp
var client = new AzcaptchaClient("KEY", new AzcaptchaConfig
{
    Transport = myTransport,
    Delay = (duration, token) => Task.Delay(duration, token)
});
```

`ITransport` requires `PostFormAsync`, `PostJsonAsync` and `GetAsync`.

## Links

- API contract: <https://github.com/AZcaptcha/sdk-spec>
- Docs: <https://azcaptcha.com/docs>
- C# guide: <https://azcaptcha.com/lang/csharp>
- Changelog: [CHANGELOG.md](CHANGELOG.md)

## License

MIT — see [LICENSE](LICENSE).
