using ReadZen.App.Infrastructure;
using ReadZen.App.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ReadZen.App.Services;

/// <summary>
/// Zen-scoped dictionary evidence generator. Streams the existing search index (relocated,
/// app-owned) filtered to the prescriptive Zen allowlist, so occurrences and the document-
/// frequency count are natively Zen-scoped. Master attribution joins each text's RelPath to the
/// master-corpus index (primary author). Pure mapping/rollup helpers are exposed for testing.
/// </summary>
public sealed class DictionaryEvidenceService : IDictionaryEvidenceService
{
    private readonly ISearchIndexService _searchIndex;
    private readonly IZenTextsService _zenTexts;
    private readonly MasterCorpusSearchService _masters;

    public DictionaryEvidenceService(
        ISearchIndexService searchIndex,
        IZenTextsService zenTexts,
        MasterCorpusSearchService masters)
    {
        _searchIndex = searchIndex;
        _zenTexts = zenTexts;
        _masters = masters;
    }

    public async Task<DictionaryEvidence> GetEvidenceAsync(
        string term,
        string originalDir,
        string translatedDir,
        string? masterCacheDir = null,
        IReadOnlyCollection<string>? restrictToRelPaths = null,
        int maxTexts = 50,
        int samplesPerText = 3,
        CancellationToken ct = default)
    {
        term = term?.Trim() ?? "";
        var ev = new DictionaryEvidence { Term = term };
        if (string.IsNullOrEmpty(term) || string.IsNullOrWhiteSpace(originalDir))
            return ev;

        var indexRoot = AppPaths.GetSearchIndexRoot(originalDir);
        var manifest = await _searchIndex.TryLoadAsync(indexRoot);
        if (manifest == null)
            return ev; // no index → empty evidence (caller shows "build the index")

        // Zen-scope + optional sense-scope.
        HashSet<string>? restrict = restrictToRelPaths == null
            ? null
            : new HashSet<string>(restrictToRelPaths.Select(NormRel), StringComparer.OrdinalIgnoreCase);
        Func<string, bool> relFilter = rel =>
            _zenTexts.IsZen(rel) && (restrict == null || restrict.Contains(NormRel(rel)));

        int zenTextCount = 0;
        long totalHits = 0;
        bool truncated = false;

        await foreach (var group in _searchIndex.SearchAllAsync(
            indexRoot, originalDir, translatedDir, manifest, term,
            includeOriginal: true, includeTranslated: false,
            fileMeta: _ => ("", "", null),
            contextWidth: 40,
            relPathFilter: relFilter,
            ct: ct))
        {
            ct.ThrowIfCancellationRequested();

            zenTextCount++;
            totalHits += group.HitsOriginal;

            if (ev.Texts.Count < maxTexts)
            {
                var samples = group.Children
                    .Where(c => c.Side == SearchSide.Original && !c.IsSkippedVerify && !string.IsNullOrEmpty(c.Hit.Match))
                    .Take(samplesPerText)
                    .Select(c => MapOccurrence(c, group.RelPath))
                    .ToList();

                ev.Texts.Add(new DictEvidenceGroup
                {
                    RelPath = group.RelPath,
                    DisplayName = string.IsNullOrEmpty(group.DisplayName) ? group.RelPath : group.DisplayName,
                    HitCount = group.HitsOriginal,
                    Samples = samples,
                });
            }
            else
            {
                truncated = true;
            }
        }

        ev.ZenTextCount = zenTextCount;
        ev.TotalHitCount = (int)Math.Min(totalHits, int.MaxValue);
        ev.Truncated = truncated;

        // Best-effort master attribution.
        MasterCorpusIndex? mi = null;
        if (!string.IsNullOrWhiteSpace(masterCacheDir))
        {
            try { mi = await _masters.TryLoadAsync(masterCacheDir!, ct); }
            catch { mi = null; }
        }
        ev.Masters = AttachMasterAttribution(ev.Texts, mi);

        return ev;
    }

    // ---- pure helpers (testable) ----

    /// <summary>Maps a search hit to a dictionary occurrence (KWIC + soft char offset; not yet curated).</summary>
    public static DictOccurrence MapOccurrence(SearchResultChild child, string relPath) => new()
    {
        RelPath = relPath,
        Kwic = child.Hit.SnippetText,
        CharOffset = child.Hit.Index,
        Curated = false,
    };

    /// <summary>
    /// Sets each group's primary-author MasterName from the master-corpus index and returns a
    /// per-master rollup (ranked by text count, then hit count). Null index ⇒ no attribution.
    /// </summary>
    public static List<DictMasterUsage> AttachMasterAttribution(List<DictEvidenceGroup> texts, MasterCorpusIndex? index)
    {
        if (index?.Appearances == null || texts == null)
            return new List<DictMasterUsage>();

        // RelPath(normalized) → primary-author master names.
        var primaryByPath = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var a in index.Appearances)
        {
            if (!string.Equals(a.AppearanceType, "primary", StringComparison.OrdinalIgnoreCase)) continue;
            if (string.IsNullOrWhiteSpace(a.RelPath) || string.IsNullOrWhiteSpace(a.MasterName)) continue;
            var key = NormRel(a.RelPath);
            if (!primaryByPath.TryGetValue(key, out var list)) primaryByPath[key] = list = new List<string>();
            if (!list.Contains(a.MasterName, StringComparer.Ordinal)) list.Add(a.MasterName);
        }

        var rollup = new Dictionary<string, DictMasterUsage>(StringComparer.OrdinalIgnoreCase);
        foreach (var g in texts)
        {
            if (!primaryByPath.TryGetValue(NormRel(g.RelPath), out var masters) || masters.Count == 0)
                continue;

            g.MasterName = masters[0];
            foreach (var m in masters)
            {
                if (!rollup.TryGetValue(m, out var u))
                    rollup[m] = u = new DictMasterUsage { MasterName = m };
                u.TextCount += 1;
                u.HitCount += g.HitCount;
            }
        }

        return rollup.Values
            .OrderByDescending(u => u.TextCount)
            .ThenByDescending(u => u.HitCount)
            .ThenBy(u => u.MasterName, StringComparer.Ordinal)
            .ToList();
    }

    private static string NormRel(string p) => (p ?? "").Replace('\\', '/').TrimStart('/');
}
