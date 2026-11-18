using System.Text.Json;
using Azcaptcha;
using Xunit;

namespace AZcaptcha.Sdk.Tests;

public class ClientLegacyTests
{
    private readonly List<TimeSpan> _sleeps = new();

    private AzcaptchaClient Client(FakeTransport transport, int softId = 0) =>
        new("test-key", new AzcaptchaConfig { Transport = transport, SoftId = softId, Delay = Delay });

    private Task Delay(TimeSpan duration, CancellationToken token)
    {
        _sleeps.Add(duration);
        return Task.CompletedTask;
    }

    [Fact]
    public async Task SolvesImageFromFixture()
    {
        var transport = new FakeTransport(Fixtures.Body("submit-image"), "{\"status\":1,\"request\":\"wxyz\"}");
        var result = await Client(transport).SolveImageAsync("PNG"u8.ToArray());
        Assert.Equal("wxyz", result);

        var call = transport.Calls[0];
        Assert.Equal(("form", "/in.php"), (call.Kind, call.Path));
        var form = Assert.IsType<Dictionary<string, string?>>(call.Payload);
        Assert.Equal("base64", form["method"]);
        Assert.Equal("test-key", form["key"]);
        Assert.Equal("1", form["json"]);
        Assert.Equal(Convert.ToBase64String("PNG"u8.ToArray()), form["body"]);
    }

    [Fact]
    public async Task SubmitsRecaptchaV2FromFixture()
    {
        var transport = new FakeTransport(Fixtures.Body("submit-recaptcha-v2"));
        var id = await Client(transport).SubmitAsync("userrecaptcha", new Dictionary<string, object?>
        {
            ["googlekey"] = "6Le-wvkSAAAAAPBMRTvw0Q4Muexq9bi0DJwx_mJ-",
            ["pageurl"] = "https://example.com/login"
        });
        Assert.Equal("1234567891", id);
        var form = Assert.IsType<Dictionary<string, string?>>(transport.Calls[0].Payload);
        Assert.False(form.ContainsKey("soft_id"));
    }

    [Fact]
    public async Task SendsSoftIdWhenConfigured()
    {
        var transport = new FakeTransport("{\"status\":1,\"request\":\"1\"}");
        await Client(transport, 4242).SubmitAsync("base64", new Dictionary<string, object?> { ["body"] = "AA==" });
        var form = Assert.IsType<Dictionary<string, string?>>(transport.Calls[0].Payload);
        Assert.Equal("4242", form["soft_id"]);
    }

    [Fact]
    public async Task NormalisesProxyFields()
    {
        var transport = new FakeTransport("{\"status\":1,\"request\":\"1\"}");
        await Client(transport).SubmitAsync("turnstile", new Dictionary<string, object?>
        {
            ["sitekey"] = "0x4AAA",
            ["pageurl"] = "https://e.com",
            ["proxy"] = "socks5://u:p@1.2.3.4:1080"
        });
        var form = Assert.IsType<Dictionary<string, string?>>(transport.Calls[0].Payload);
        Assert.Equal("u:p@1.2.3.4:1080", form["proxy"]);
        Assert.Equal("SOCKS5", form["proxytype"]);
    }

    [Fact]
    public async Task MapsZeroBalanceFixture()
    {
        var transport = new FakeTransport(Fixtures.Body("error-zero-balance"));
        var error = await Assert.ThrowsAsync<AzcaptchaException>(() =>
            Client(transport).SubmitAsync("userrecaptcha", new Dictionary<string, object?>
            {
                ["googlekey"] = "k",
                ["pageurl"] = "https://e.com"
            }));
        Assert.Equal("ERROR_ZERO_BALANCE", error.Code);
        Assert.Equal(ErrorCategory.Balance, error.Category);
        Assert.False(error.Retryable);
    }

    [Fact]
    public async Task MapsNoSlotFixture()
    {
        var transport = new FakeTransport(Fixtures.Body("error-no-slot"));
        var error = await Assert.ThrowsAsync<AzcaptchaException>(() =>
            Client(transport).SubmitAsync("turnstile", new Dictionary<string, object?>
            {
                ["sitekey"] = "k",
                ["pageurl"] = "https://e.com"
            }));
        Assert.Equal(ErrorCategory.Capacity, error.Category);
        Assert.True(error.Retryable);
    }

    [Fact]
    public async Task ReadsPollFixtures()
    {
        Assert.Equal("03AGdBq26...",
            await Client(new FakeTransport(Fixtures.Body("poll-ready-recaptcha"))).ResultAsync("1234567891"));

        var error = await Assert.ThrowsAsync<AzcaptchaException>(() =>
            Client(new FakeTransport(Fixtures.Body("poll-processing"))).ResultAsync("1234567891"));
        Assert.Equal("CAPCHA_NOT_READY", error.Code);
    }

    [Fact]
    public async Task WaitsWithContractDelays()
    {
        var transport = new FakeTransport(Fixtures.Body("poll-processing"), Fixtures.Body("poll-ready-recaptcha"));
        Assert.Equal("03AGdBq26...", await Client(transport).WaitResultAsync("1234567891"));
        Assert.Equal(new[] { TimeSpan.FromSeconds(1) }, _sleeps);
    }

    [Fact]
    public async Task BacksOffOnNoSlot()
    {
        var transport = new FakeTransport(
            "{\"status\":0,\"request\":\"ERROR_NO_SLOT_AVAILABLE\"}",
            "{\"status\":1,\"request\":\"token\"}");
        Assert.Equal("token", await Client(transport).WaitResultAsync("1"));
        Assert.Equal(new[] { TimeSpan.FromSeconds(10) }, _sleeps);
    }

    [Fact]
    public async Task RaisesNonRetryableImmediately()
    {
        var transport = new FakeTransport("{\"status\":0,\"request\":\"ERROR_CAPTCHA_UNSOLVABLE\"}");
        var error = await Assert.ThrowsAsync<AzcaptchaException>(() => Client(transport).WaitResultAsync("1"));
        Assert.Equal(ErrorCategory.Result, error.Category);
        Assert.Empty(_sleeps);
    }

    [Fact]
    public async Task TimesOut()
    {
        var transport = new FakeTransport("{\"status\":0,\"request\":\"CAPCHA_NOT_READY\"}");
        var error = await Assert.ThrowsAsync<AzcaptchaException>(() =>
            Client(transport).WaitResultAsync("1", TimeSpan.FromMilliseconds(1)));
        Assert.Equal("ERROR_TIMEOUT", error.Code);
    }

    [Fact]
    public async Task ReadsBalanceAndReportsBad()
    {
        var transport = new FakeTransport(
            "{\"status\":1,\"request\":12.5}",
            "{\"status\":1,\"request\":\"OK_REPORT_RECORDED\"}");
        var client = Client(transport);
        Assert.Equal(12.5, await client.BalanceAsync());
        Assert.True(await client.ReportBadAsync("123"));
    }

    [Fact]
    public void RejectsEmptyKey()
    {
        Assert.Throws<AzcaptchaException>(() => new AzcaptchaClient(""));
    }
}
