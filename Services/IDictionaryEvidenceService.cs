using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ReadZen.App.Models;

namespace ReadZen.App.Services;

/// <summary>
/// Generates dictionary occurrence evidence for a head term, scoped to the Zen corpus (the
/// prescriptive allowlist via <see cref="IZenTextsService.IsZen"/>). Summary-first: returns
/// counts + a capped page of per-text groups (with a few sample KWIC each) + a master rollup,
/// NOT a flat dump — the full occurrence set stays queryable in the index. Optionally scope to
/// a single sense by restricting to that sense's texts.
/// </summary>
public interface IDictionaryEvidenceService
{
    /// <summary>
    /// Query Zen-corpus occurrences of <paramref name="term"/>.
    /// </summary>
    /// <param name="masterCacheDir">Optional .readzen-cache dir holding the master-corpus index; when present,
    /// the result's per-text MasterName + Masters rollup are populated. Best-effort — null/absent ⇒ no rollup.</param>
    /// <param name="restrictToRelPaths">Optional sense-scope: only count texts in this set (e.g. a sense's SourceTexts).</param>
    /// <remarks>The caller must have loaded <see cref="IZenTextsService"/> (the app does on corpus set).</remarks>
    Task<DictionaryEvidence> GetEvidenceAsync(
        string term,
        string originalDir,
        string translatedDir,
        string? masterCacheDir = null,
        IReadOnlyCollection<string>? restrictToRelPaths = null,
        int maxTexts = 50,
        int samplesPerText = 3,
        CancellationToken ct = default);
}
