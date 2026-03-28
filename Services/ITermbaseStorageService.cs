using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CbetaTranslator.App.Models;

namespace CbetaTranslator.App.Services;

public interface ITermbaseStorageService
{
    Task<List<TermbaseEntry>> LoadAsync(string root, CancellationToken ct = default);
    Task SaveAsync(string root, IEnumerable<TermbaseEntry> entries, CancellationToken ct = default);

    Task WriteUserJsonlAsync(string communityDir, string username, List<TermbaseEntry> entries, CancellationToken ct = default);
    Task<Dictionary<string, List<TermbaseEntry>>> LoadAllCommunityJsonlAsync(string communityDir, CancellationToken ct = default);

    static string GetCommunityTermbasesDir(string repoRoot) => System.IO.Path.Combine(repoRoot, "community", "termbases");
}
