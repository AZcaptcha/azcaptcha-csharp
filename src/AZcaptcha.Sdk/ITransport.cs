using System.Collections.Generic;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Azcaptcha
{
    /// <summary>Minimal HTTP surface the client needs; tests provide a fake.</summary>
    public interface ITransport
    {
        Task<Dictionary<string, JsonElement>> PostFormAsync(
            string path, IDictionary<string, string?> form, CancellationToken cancellationToken);

        Task<Dictionary<string, JsonElement>> PostJsonAsync(
            string path, object payload, CancellationToken cancellationToken);

        Task<Dictionary<string, JsonElement>> GetAsync(
            string path, IDictionary<string, string?> query, CancellationToken cancellationToken);
    }
}
