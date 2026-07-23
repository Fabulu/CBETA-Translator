using System;
using System.Threading;
using System.Threading.Tasks;
using ReadZen.App.Models;

namespace ReadZen.App.Services;

public interface IIndexCacheService
{
    string GetCachePath(string root);

    /// <summary>
    /// Structure-only load: returns the cached index when it passes the
    /// structural gate (present, non-empty, matching RootPath, Version, and
    /// BuildGuid), otherwise null. Does NOT refresh per-entry statuses — call
    /// <see cref="RefreshAsync"/> afterward. The git-HEAD gate was removed in v4.
    /// </summary>
    Task<IndexCache?> TryLoadAsync(string root);
    Task SaveAsync(string root, IndexCache cache);

    TranslationStatus ComputeStatusForPairLive(
        string origAbs,
        string tranAbs,
        string rootForLogs,
        string relKeyForLogs,
        bool verboseLog = true);

    Task<IndexCache> BuildAsync(
        string originalDir,
        string translatedDir,
        string root,
        IProgress<(int done, int total)>? progress = null,
        CancellationToken ct = default);

    /// <summary>
    /// Incrementally refreshes <paramref name="oldCache"/> against the current
    /// filesystem: reuses entries whose (orig, tran) file stats and resolved
    /// translation path are unchanged, recomputes status only for changed
    /// entries, adds new originals and drops removed ones. Falls back to a full
    /// <see cref="BuildAsync"/> when the structural gate fails (guid/version/root
    /// mismatch or a titles.jsonl change). Saves only when something changed and
    /// returns the refreshed cache. Progress is reported over the recompute set,
    /// not the whole corpus.
    /// </summary>
    Task<IndexCache> RefreshAsync(
        IndexCache oldCache,
        string originalDir,
        string translatedDir,
        string root,
        IProgress<(int done, int total)>? progress = null,
        CancellationToken ct = default);
}
