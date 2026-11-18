using Azcaptcha;
using Xunit;

namespace AZcaptcha.Sdk.Tests;

public class ParamsTests
{
    [Fact]
    public void NormalisesProxies()
    {
        Assert.Equal(("1.2.3.4:8080", "HTTP"), Params.NormalizeProxy("1.2.3.4:8080"));
        Assert.Equal(("user:pass@1.2.3.4:8080", "HTTP"), Params.NormalizeProxy("user:pass@1.2.3.4:8080"));
        Assert.Equal(("user:pass@1.2.3.4:8080", "HTTP"), Params.NormalizeProxy("1.2.3.4:8080:user:pass"));
        Assert.Equal(("u:p@1.2.3.4:1080", "SOCKS5"), Params.NormalizeProxy("socks5://u:p@1.2.3.4:1080"));
        Assert.Equal(("u:p@1.2.3.4:1080", "SOCKS4"), Params.NormalizeProxy("socks4://u:p@1.2.3.4:1080"));
        Assert.Equal(("u:p@1.2.3.4:8080", "HTTP"), Params.NormalizeProxy("https://u:p@1.2.3.4:8080"));
    }

    [Fact]
    public void RejectsProxyWithoutPort()
    {
        Assert.Throws<ArgumentException>(() => Params.NormalizeProxy("1.2.3.4"));
    }

    [Fact]
    public void EncodesImages()
    {
        Assert.Equal("UE5H", Params.ImageBody("PNG"u8.ToArray()));
        Assert.Equal("AAAA", Params.ImageBase64("data:image/png;base64,AAAA"));
        Assert.Equal("iVBORw0KGgo=", Params.ImageBase64("iVBORw0KGgo=\n"));

        var path = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName() + ".png");
        File.WriteAllBytes(path, "PNG"u8.ToArray());
        try
        {
            Assert.Equal("UE5H", Params.ImageFile(path));
        }
        finally
        {
            File.Delete(path);
        }
        Assert.Throws<AzcaptchaException>(() => Params.ImageFile(path + ".missing"));
    }
}
