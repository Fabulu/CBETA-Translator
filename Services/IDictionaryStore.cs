using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ReadZen.App.Models;

namespace ReadZen.App.Services;

/// <summary>
/// Reads/writes the rich Zen dictionary (schema v2) in a dual-file layout: the canonical
/// termbase.v2.json (envelope, new clients only) plus a downgraded termbase.json (bare
/// TermbaseEntry array) kept in sync for legacy clients. Migrates a legacy-only repo to
/// v2 on first load. See SPEC_v3 (dual-file sidecar decision).
/// </summary>
public interface IDictionaryStore
{
    /// <summary>
    /// Load the rich dictionary. Prefers termbase.v2.json; if absent, migrates from the
    /// legacy termbase.json (or a seed) into a v2 model in memory (no write until SaveAsync).
    /// </summary>
    Task<DictionaryFile> LoadAsync(string root, CancellationToken ct = default);

    /// <summary>
    /// Persist the rich dictionary to termbase.v2.json AND write the downgraded legacy
    /// termbase.json alongside it, both atomically.
    /// </summary>
    Task SaveAsync(string root, DictionaryFile file, CancellationToken ct = default);

    static string GetV2Path(string root) => Path.Combine(root, "termbase.v2.json");
    static string GetLegacyPath(string root) => Path.Combine(root, "termbase.json");
}
