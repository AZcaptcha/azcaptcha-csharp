using System.Text.Json;

namespace AZcaptcha.Sdk.Tests;

public static class Fixtures
{
    private static readonly JsonDocumentOptions Options = new() { AllowTrailingCommas = true };

    public static JsonElement Load(string name)
    {
        var path = System.IO.Path.Combine(AppContext.BaseDirectory, "fixtures", name + ".json");
        return JsonDocument.Parse(File.ReadAllText(path), Options).RootElement.Clone();
    }

    public static string Body(string name) =>
        Load(name).GetProperty("response").GetProperty("json").GetRawText();
}
