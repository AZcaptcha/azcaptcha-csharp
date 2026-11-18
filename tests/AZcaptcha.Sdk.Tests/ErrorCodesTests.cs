using Azcaptcha;
using Xunit;

namespace AZcaptcha.Sdk.Tests;

public class ErrorCodesTests
{
    [Fact]
    public void EverySpecCodeMapsToTypedCategory()
    {
        var spec = Fixtures.Load("errors");
        foreach (var section in new[] { "errors", "envelope_codes" })
        {
            foreach (var code in spec.GetProperty(section).EnumerateObject())
            {
                var error = ErrorCodes.Error(code.Name);
                Assert.Equal(code.Name, error.Code);
                Assert.NotEqual(ErrorCategory.Unknown, error.Category);
            }
        }
    }

    [Fact]
    public void RetryableFlagsMatchSpec()
    {
        var spec = Fixtures.Load("errors");
        var flags = ErrorCodes.Taxonomy();
        foreach (var section in new[] { "errors", "envelope_codes" })
        {
            foreach (var code in spec.GetProperty(section).EnumerateObject())
            {
                var expected = code.Value.GetProperty("retryable").GetBoolean();
                Assert.Equal(expected, flags[code.Name]);
                Assert.Equal(expected, ErrorCodes.Error(code.Name).Retryable);
            }
        }
    }

    [Fact]
    public void UnknownCodesAreForwardCompatible()
    {
        var error = ErrorCodes.Error("ERROR_FROM_THE_FUTURE");
        Assert.Equal(ErrorCategory.Unknown, error.Category);
        Assert.False(error.Retryable);
    }
}
