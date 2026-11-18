using Azcaptcha;
using Xunit;

namespace AZcaptcha.Sdk.Tests;

public class ClientModernTests
{
    private readonly List<TimeSpan> _sleeps = new();

    private AzcaptchaClient Client(FakeTransport transport) =>
        new("test-key", new AzcaptchaConfig
        {
            Transport = transport,
            Delay = (duration, token) =>
            {
                _sleeps.Add(duration);
                return Task.CompletedTask;
            }
        });

    [Fact]
    public async Task CreatesTaskFromFixture()
    {
        var transport = new FakeTransport(Fixtures.Body("modern-create-task"));
        var id = await Client(transport).CreateTaskAsync("RecaptchaV2TaskProxyless", new Dictionary<string, object?>
        {
            ["websiteURL"] = "https://example.com/login",
            ["websiteKey"] = "6Le-wvkSAAAAAPBMRTvw0Q4Muexq9bi0DJwx_mJ-"
        });
        Assert.Equal(1234567892, id);
        var call = transport.Calls[0];
        Assert.Equal(("json", "/createTask"), (call.Kind, call.Path));
        var payload = Assert.IsType<Dictionary<string, object?>>(call.Payload);
        Assert.Equal("test-key", payload["clientKey"]);
        var task = Assert.IsType<Dictionary<string, object?>>(payload["task"]);
        var expected = Fixtures.Load("modern-create-task").GetProperty("request").GetProperty("json").GetProperty("task");
        Assert.Equal(expected.GetProperty("type").GetString(), task["type"]);
        Assert.Equal(expected.GetProperty("websiteURL").GetString(), task["websiteURL"]);
        Assert.Equal(expected.GetProperty("websiteKey").GetString(), task["websiteKey"]);
    }

    [Fact]
    public async Task ReadsReadyTaskResult()
    {
        var body = await Client(new FakeTransport(Fixtures.Body("modern-task-ready"))).GetTaskResultAsync(1234567892);
        Assert.Equal("ready", body["status"].GetString());
        Assert.Equal("03AGdBq26...", body["solution"].GetProperty("gRecaptchaResponse").GetString());
    }

    [Fact]
    public async Task PollsUntilReady()
    {
        var transport = new FakeTransport(
            "{\"errorId\":0,\"status\":\"processing\"}",
            "{\"errorId\":0,\"status\":\"ready\",\"solution\":{\"token\":\"abc\"}}");
        var solution = await Client(transport).WaitTaskResultAsync(1);
        Assert.Equal("abc", solution["token"].GetString());
        Assert.Equal(new[] { TimeSpan.FromSeconds(1) }, _sleeps);
    }

    [Fact]
    public async Task MapsModernErrors()
    {
        var transport = new FakeTransport("{\"errorId\":12,\"errorCode\":\"ERROR_ZERO_BALANCE\"}");
        var error = await Assert.ThrowsAsync<AzcaptchaException>(() =>
            Client(transport).CreateTaskAsync("RecaptchaV2TaskProxyless"));
        Assert.Equal(ErrorCategory.Balance, error.Category);
    }

    [Fact]
    public async Task TimesOut()
    {
        var transport = new FakeTransport("{\"errorId\":0,\"status\":\"processing\"}");
        var error = await Assert.ThrowsAsync<AzcaptchaException>(() =>
            Client(transport).WaitTaskResultAsync(1, TimeSpan.FromMilliseconds(1)));
        Assert.Equal("ERROR_TIMEOUT", error.Code);
    }
}
