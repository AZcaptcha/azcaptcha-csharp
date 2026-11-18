using System.Text.Json;
using Azcaptcha;

namespace AZcaptcha.Sdk.Tests;

/// <summary>Replays canned JSON bodies and records every call.</summary>
public sealed class FakeTransport : ITransport
{
    public sealed record Call(string Kind, string Path, object Payload);

    private readonly Queue<Dictionary<string, JsonElement>> _responses;
    public List<Call> Calls { get; } = new();

    public FakeTransport(params string[] bodies)
    {
        _responses = new Queue<Dictionary<string, JsonElement>>(
            bodies.Select(body => JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(body)!));
    }

    public Task<Dictionary<string, JsonElement>> PostFormAsync(
        string path, IDictionary<string, string?> form, CancellationToken cancellationToken)
    {
        return Next("form", path, form.ToDictionary(pair => pair.Key, pair => pair.Value));
    }

    public Task<Dictionary<string, JsonElement>> PostJsonAsync(
        string path, object payload, CancellationToken cancellationToken)
    {
        return Next("json", path, payload);
    }

    public Task<Dictionary<string, JsonElement>> GetAsync(
        string path, IDictionary<string, string?> query, CancellationToken cancellationToken)
    {
        return Next("get", path, query.ToDictionary(pair => pair.Key, pair => pair.Value));
    }

    private Task<Dictionary<string, JsonElement>> Next(string kind, string path, object payload)
    {
        Calls.Add(new Call(kind, path, payload));
        if (_responses.Count == 0)
        {
            throw new InvalidOperationException($"unexpected call: {kind} {path}");
        }
        return Task.FromResult(_responses.Dequeue());
    }
}
