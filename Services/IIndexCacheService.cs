using System;
using System.Threading;
using System.Threading.Tasks;
using ReadZen.App.Models;

namespace ReadZen.App.Services;

public interface IIndexCacheService
{
    string GetCachePath(string root);

    /// <summary>
    /// Structure-only load: returns the cached index when it passes the structural gate
    /// (present, non-empty, current Version, and matching BuildGuid), otherwise null.
    /// v5 (NAV_CACHE_REDESIGN §2.1): the RootPath is NO LONGER compared — a foreign-root
    /// cache loads and is re-homed on the next save. A v4 cache returns null in PR-NV2
    /// (routed to rebuild; migration is PR-NV3). Does NOT refresh per-entry statuses —
    /// call <see cref="RefreshAsync"/> afterward.
    /// </summary>
    Task<IndexCache?> TryLoadAsync(string root);

    /// <summary>
    /// Root-tolerant classified load (NAV_CACHE_REDESIGN §4.4): reports whether the
    /// on-disk cache is a usable v5, a v4 that needs migration, or unusable — without a
    /// second file read. In PR-NV2 the launch ladder is not yet wired to it (that is
    /// PR-NV3), but it is the load surface the migration/adoption rungs will branch on.
    /// </summary>
    Task<NavCacheLoadResult> LoadAsync(string root);
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
    /// Incrementally refreshes <paramref name="oldCache"/> against the current filesystem
    /// (NAV_CACHE_REDESIGN §3.4, RefreshAsync v2). Structural gate: Version/BuildGuid drift
    /// or an empty cache ⇒ full <see cref="BuildAsync"/> (RootPath is NOT compared). A
    /// titles.jsonl change re-derives display fields ONLY, keeping statuses. Computes the
    /// live OriginalsSig + scans the translation-source manifest for the live SourceSig;
    /// when both equal the stored sigs (and titles unchanged) it returns the cache
    /// verbatim with ZERO recomputes (post-clone it heals mtime hints and saves once).
    /// Otherwise it reuses entries whose OrigSizeBytes and candidate {(Token,ContentSig)}
    /// set are unchanged and recomputes only the changed candidates via the
    /// <c>INavStatusEvaluator</c>. Saves only when something changed; progress is reported
    /// over the recompute set, not the whole corpus.
    /// </summary>
    Task<IndexCache> RefreshAsync(
        IndexCache oldCache,
        string originalDir,
        string translatedDir,
        string root,
        IProgress<(int done, int total)>? progress = null,
        CancellationToken ct = default);

    /// <summary>
    /// One-time v4 -&gt; v5 migration (NAV_CACHE_REDESIGN §4.4, PR-NV3). Converts each v4
    /// entry's absolute <c>TranResolvedPath</c> into a relative source <c>Token</c> +
    /// <c>ContentSig</c> (hashing the ~21 referenced files) while carrying over its display
    /// fields and locally-computed <c>Status</c>, then runs the normal gated
    /// <see cref="RefreshAsync"/> so only the manifest overlap/dropped set is recomputed and
    /// the upgraded cache is saved as v5. Migration BEATS a bundle because v4 holds the
    /// user's own local statuses.
    /// </summary>
    Task<IndexCache> MigrateV4(
        IndexCache oldCache,
        string originalDir,
        string translatedDir,
        string root,
        IProgress<(int done, int total)>? progress = null,
        CancellationToken ct = default);

    /// <summary>
    /// PR-NV4 (NAV_CACHE_REDESIGN §3.5.1): recompute ONE nav entry after a local
    /// translation save. Rescans the translation sources for THIS rel only (a canonical
    /// stat + one loop over <c>community/translations/{user}</c> for the single rel — never
    /// the full-corpus sweep), re-evaluates via the single <c>INavStatusEvaluator</c>
    /// pipeline (reusing <paramref name="storedEntry"/>'s per-candidate verdicts whose
    /// <c>(Token, ContentSig)</c> are unchanged), and returns a fresh <see cref="FileNavItem"/>
    /// whose <c>Status</c> AND <c>Sources</c>/<c>TranLocalHints</c> are coherent. The caller
    /// copies those onto the bound nav item, fixing the old partial-update drift (only
    /// <c>Status</c> written, <c>Sources</c> left stale) that forced a recompute next launch.
    /// Display fields carry over from <paramref name="storedEntry"/> unchanged (a translation
    /// save never touches titles.jsonl).
    /// </summary>
    Task<FileNavItem> RefreshEntryAsync(
        FileNavItem? storedEntry,
        string relPath,
        string originalDir,
        string translatedDir,
        string root,
        CancellationToken ct = default);

    /// <summary>
    /// Bundle-adoption rung (NAV_CACHE_REDESIGN §4.2 row 3, PR-NV5): when no usable local
    /// cache exists, adopt the exe-adjacent prebuilt nav cache shipped with the app
    /// (<c>Assets/Data/nav-cache.{kind}.json</c>). Adopts ONLY a usable v5 bundle whose
    /// <c>CorpusKind</c> matches <paramref name="activeKind"/>, copying it atomically
    /// (tmp+move — no re-homing, nothing in v5 is absolute) into
    /// <c>{root}/index.cache.json</c> and returning the parsed cache so the caller runs the
    /// normal gated <see cref="RefreshAsync"/> as catch-up (fast path ⇒ zero recomputes on a
    /// fresh install of the shipped corpus). Returns null — writing no cache file — when the
    /// bundle is absent, corrupt, or kind-mismatched, so the launch ladder falls through to
    /// a cold <see cref="BuildAsync"/>. The caller must have already ruled out a usable local
    /// v5/v4 cache (rows 1-2): local truth always wins over the bundle.
    /// </summary>
    Task<IndexCache?> TryAdoptBundle(
        string root,
        CorpusKind activeKind,
        CancellationToken ct = default);
}
