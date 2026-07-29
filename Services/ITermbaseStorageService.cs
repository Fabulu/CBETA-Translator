using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ReadZen.App.Models;

namespace ReadZen.App.Services;

public interface ITermbaseStorageService
{
    Task<List<TermbaseEntry>> LoadAsync(string root, CancellationToken ct = default);
    Task SaveAsync(string root, IEnumerable<TermbaseEntry> entries, CancellationToken ct = default);

    Task<List<TermbaseEntry>> LoadUserAsync(string root, string username, CancellationToken ct = default);
    Task SaveUserAsync(string root, string username, IEnumerable<TermbaseEntry> entries, CancellationToken ct = default);

    // WriteUserJsonlAsync / LoadAllCommunityJsonlAsync removed: personal termbases are
    // local-only (no community publish, no rendering of other users' termbases).

    static string GetCommunityTermbasesDir(string repoRoot) => System.IO.Path.Combine(repoRoot, "community", "termbases");
    static string GetUserPath(string root, string username)
        => System.IO.Path.Combine(GetCommunityTermbasesDir(root), Infrastructure.AppPaths.SanitizeUsername(username) + ".json");
}
