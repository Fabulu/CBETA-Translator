using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ReadZen.App.Models;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

/// <summary>
/// FL4 GOLDEN GATE (design §7.3): index ONE synthetic corpus BOTH ways —
/// (A) the legacy COMBINED build (search.index.* / search.text.* / search.corpusfreq.* /
///     search.inverted.*), and
/// (B) the SPLIT build (origin layer via <see cref="SearchIndexService.BuildOriginLayerAsync"/>
///     + overlay layer via <see cref="SearchIndexService.BuildOverlayLayerAsync"/>) —
/// into two separate index roots over the SAME corpus dirs, then asserts that
/// <see cref="SearchIndexService.SearchAllAsync"/> produces IDENTICAL output from both roots
/// for every query class the design enumerates:
/// <list type="bullet">
///   <item>single-bigram CJK (inverted fast path + skip-verify);</item>
///   <item>multi-bigram CJK phrase (inverted, verify-all);</item>
///   <item>an English term (bloom fallback for a translation-of-origin rel);</item>
///   <item>a 1-char brute query (union of both manifests);</item>
///   <item>a bloom-fallback query with the inverted indexes DELETED (two-bin bloom sweep);</item>
///   <item>the §11.1 tricky case — a CJK bigram present ONLY in a translation whose rel also
///     has an origin (its translated side is not an inverted doc → bloom fallback).</item>
/// </list>
/// Identical means: the emitted group (rel) set; per-group HitsOriginal/HitsTranslated; the
/// verified-vs-skipped partition (the observable proxy for tf/size candidate ordering's top-N
/// cut); the service skip-verify/verified counters; and every child snippet string. Also the
/// SPLIT merged corpusfreq == the COMBINED recount (exact char/bigram dict equality + totalChars).
///
/// This test BLOCKS FL4: any parity break here means the read-path swap changed results.
/// </summary>
public class SplitParityTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly string _origDir;    // xml-p5   (active origin corpus)
    private readonly string _tranDir;    // xml-p5t  (translations)
    private readonly string _addOrigDir; // xml-open (an additional original corpus)
    private readonly string _rootCombined;
    private readonly string _rootSplit;

    // Rels chosen to hit every partition case (mirrors LayerPartitionTests + §11.1):
    //  a/b/d.xml  → origin; a.xml also has a translation (both sides)
    //  c.xml      → origin only, no shared bigram
    //  shadow.xml → in origin AND additional-orig (additional-orig copy is shadowed out)
    //  t1.xml     → translation only (no origin)
    //  add1.xml   → additional-orig only (an overlay Original entry)
    private const string A = "a.xml";
    private const string B = "b.xml";
    private const string C = "c.xml";
    private const string D = "d.xml";
    private const string Shadow = "shadow.xml";
    private const string T1 = "t1.xml";
    private const string Add1 = "add1.xml";

    public SplitParityTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "readzen-splitparity-" + Guid.NewGuid().ToString("N")[..8]);
        _origDir = Path.Combine(_tempRoot, "xml-p5");
        _tranDir = Path.Combine(_tempRoot, "xml-p5t");
        _addOrigDir = Path.Combine(_tempRoot, "xml-open");
        _rootCombined = Path.Combine(_tempRoot, "index-combined");
        _rootSplit = Path.Combine(_tempRoot, "index-split");
        Directory.CreateDirectory(_origDir);
        Directory.CreateDirectory(_tranDir);
        Directory.CreateDirectory(_addOrigDir);
        Directory.CreateDirectory(_rootCombined);
        Directory.CreateDirectory(_rootSplit);

        // ── Origin corpus (xml-p5) ──
        // 禪宗 (single-bigram query) appears in a, b, d and the additional-orig add1 — NOT in
        // c, NOT in shadow's origin text. Varied sizes so the size-proxy candidate ordering is
        // unambiguous. 心印 is deliberately ABSENT here (it lives only in a.xml's translation).
        Write(_origDir, A, "禪宗祖師傳法" + Filler(1));
        Write(_origDir, B, "禪宗語錄公案" + Filler(3));
        Write(_origDir, C, "山水清音妙道" + Filler(2));
        Write(_origDir, D, "禪宗大意玄旨" + Filler(4));
        Write(_origDir, Shadow, "無門關公案語");

        // ── Translations (xml-p5t) ──
        // a.xml has BOTH sides; its translation carries the English term "ancestral" AND the
        // CJK bigram 心印 that is absent from every origin (the §11.1 tricky case).
        Write(_tranDir, A, "Zen ancestral teaching 心印妙義");
        Write(_tranDir, T1, "般若波羅蜜多智慧");

        // ── Additional origin corpus (xml-open) ──
        // add1 is an overlay Original entry (has 禪宗). shadow collides with the active origin
        // shadow.xml → shadowed out of the overlay entirely (must never be indexed twice).
        Write(_addOrigDir, Add1, "白雲深處禪宗行");
        Write(_addOrigDir, Shadow, "此文本應被遮蔽禪宗");
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempRoot, true); } catch { }
    }

    private static string Filler(int n) => new string('文', n * 40);

    private static void Write(string dir, string rel, string cjk)
    {
        var xml =
            "<?xml version=\"1.0\" encoding=\"UTF-8\"?>\n" +
            "<TEI xmlns=\"http://www.tei-c.org/ns/1.0\"><text><body>\n" +
            $"<p>{cjk}</p>\n" +
            "</body></text></TEI>\n";
        File.WriteAllText(Path.Combine(dir, rel), xml);
    }

    /// <summary>Build the combined family into _rootCombined (one shared instance is fine — the
    /// build warms Combined; each search uses a fresh service so cache state never leaks).</summary>
    private async Task BuildCombinedAsync()
    {
        var svc = new SearchIndexService();
        await svc.BuildOrUpdateAsync(
            _rootCombined, _origDir, new[] { _tranDir }, forceRebuild: true,
            additionalOriginalDirs: new[] { _addOrigDir }, additionalTranslatedDirs: null);
    }

    /// <summary>Build the origin + overlay families into _rootSplit via the FL3 entry points.</summary>
    private async Task BuildSplitAsync()
    {
        var svc = new SearchIndexService();
        await svc.BuildOriginLayerAsync(_rootSplit, _origDir);
        await svc.BuildOverlayLayerAsync(_rootSplit, new[] { _tranDir }, new[] { _addOrigDir }, null);
    }

    private sealed class SearchOutcome
    {
        public Dictionary<string, (int hitsO, int hitsT, bool skipVerify, List<string> snippets)> Groups = new(StringComparer.OrdinalIgnoreCase);
        public int SkippedVerifyGroups;
        public int VerifiedGroups;
    }

    private async Task<SearchOutcome> RunSearchAsync(
        string root, string query, bool includeOriginal, bool includeTranslated,
        int skipVerifyTopN, bool deleteInverted)
    {
        if (deleteInverted)
        {
            foreach (var f in new[]
                     {
                         "search.inverted.bin", "search.inverted.bin.paths",
                         "search.origin.inverted.bin", "search.origin.inverted.bin.paths",
                         "search.overlay.inverted.bin", "search.overlay.inverted.bin.paths",
                     })
            {
                var p = Path.Combine(root, f);
                if (File.Exists(p)) File.Delete(p);
            }
        }

        // Fresh service per run → no cross-run cache contamination; also proves TryLoadAsync
        // reconstructs the full serving state (families, inverted, corpusfreq) from disk.
        var svc = new SearchIndexService();
        svc.Options.SkipVerifySnippetTopN = skipVerifyTopN;

        var manifest = await svc.TryLoadAsync(root);
        Assert.NotNull(manifest);

        var outcome = new SearchOutcome();
        await foreach (var g in svc.SearchAllAsync(
            root, _origDir, _tranDir, manifest!, query,
            includeOriginal: includeOriginal, includeTranslated: includeTranslated,
            fileMeta: rel => (rel, rel, (TranslationStatus?)null),
            contextWidth: 20,
            additionalOriginalDirs: new[] { _addOrigDir }))
        {
            bool skip = g.Children.Count > 0 && g.Children[0].IsSkippedVerify;
            var snippets = g.Children
                .Select(c => $"{c.Side}|{c.Hit.Left}|{c.Hit.Match}|{c.Hit.Right}")
                .OrderBy(s => s, StringComparer.Ordinal)
                .ToList();
            outcome.Groups[g.RelPath] = (g.HitsOriginal, g.HitsTranslated, skip, snippets);
        }

        outcome.SkippedVerifyGroups = svc.LastSearchSkippedVerifyGroups;
        outcome.VerifiedGroups = svc.LastSearchVerifiedGroups;
        return outcome;
    }

    private static void AssertOutcomesIdentical(string label, SearchOutcome combined, SearchOutcome split)
    {
        // Emitted group (rel) SET — order-independent (groups arrive via a channel in verify-
        // completion order, which is non-deterministic; the SET is the contract).
        Assert.True(
            combined.Groups.Keys.ToHashSet(StringComparer.OrdinalIgnoreCase)
                .SetEquals(split.Groups.Keys),
            $"[{label}] group rel set differs. combined=[{string.Join(",", combined.Groups.Keys.OrderBy(k => k))}] " +
            $"split=[{string.Join(",", split.Groups.Keys.OrderBy(k => k))}]");

        foreach (var rel in combined.Groups.Keys)
        {
            var c = combined.Groups[rel];
            var s = split.Groups[rel];
            Assert.True(c.hitsO == s.hitsO, $"[{label}] {rel} HitsOriginal {c.hitsO} != {s.hitsO}");
            Assert.True(c.hitsT == s.hitsT, $"[{label}] {rel} HitsTranslated {c.hitsT} != {s.hitsT}");
            // verified-vs-skipped partition = the observable proxy for the tf/size candidate
            // ordering's top-N cut.
            Assert.True(c.skipVerify == s.skipVerify, $"[{label}] {rel} skipVerify {c.skipVerify} != {s.skipVerify}");
            Assert.True(c.snippets.SequenceEqual(s.snippets, StringComparer.Ordinal),
                $"[{label}] {rel} snippets differ.\n  combined: {string.Join(" ;; ", c.snippets)}\n  split:    {string.Join(" ;; ", s.snippets)}");
        }

        // Skip-verify placeholder counts + verified counts.
        Assert.True(combined.SkippedVerifyGroups == split.SkippedVerifyGroups,
            $"[{label}] SkippedVerifyGroups {combined.SkippedVerifyGroups} != {split.SkippedVerifyGroups}");
        Assert.True(combined.VerifiedGroups == split.VerifiedGroups,
            $"[{label}] VerifiedGroups {combined.VerifiedGroups} != {split.VerifiedGroups}");
    }

    [Fact]
    public async Task Split_And_Combined_ProduceIdenticalSearchResults()
    {
        await BuildCombinedAsync();
        await BuildSplitAsync();

        // ── Case 1: single-bigram CJK on the inverted fast path, with skip-verify forced ──
        // 禪宗 ∈ {a, b, d (origin), add1 (overlay orig)} → 4 candidates; top-2 verified, 2 skipped.
        await CompareAsync("single-bigram-CJK", "禪宗", true, true, skipVerifyTopN: 2, deleteInverted: false);

        // ── Case 2: multi-bigram CJK phrase (inverted, verify-all; no skip for len>2) ──
        await CompareAsync("multi-bigram-CJK", "祖師傳法", true, true, skipVerifyTopN: 2, deleteInverted: false);

        // ── Case 3: English term — a.xml translation "ancestral"; the rel has an origin so its
        // translated side is not an inverted doc → bloom fallback + translated verify. ──
        await CompareAsync("english-term", "ancestral", true, true, skipVerifyTopN: 2, deleteInverted: false);

        // ── Case 4: 1-char brute query (union of both manifests' entries, verify filter) ──
        await CompareAsync("one-char-brute", "禪", true, true, skipVerifyTopN: 2, deleteInverted: false);

        // ── Case 5: bloom-fallback with the inverted indexes DELETED → the two-bin bloom sweep
        // must reproduce the single-bin sweep exactly (same query that would use inverted). ──
        await CompareAsync("bloom-fallback-inverted-deleted", "禪宗", true, true, skipVerifyTopN: 2, deleteInverted: true);

        // ── Case 6 (§11.1): CJK bigram present ONLY in a translation whose rel also has an origin.
        // Inverted has no 心印 (a.xml's inverted doc is its origin; its translation is excluded
        // from the overlay inverted feed) → both roots reach it via bloom fallback + verify. ──
        await CompareAsync("cjk-in-translation-of-origin-rel", "心印", true, true, skipVerifyTopN: 2, deleteInverted: false);

        // ── corpusfreq: merged split view == combined recount (exact dict equality + N) ──
        await AssertCorpusFreqIdenticalAsync();
    }

    private async Task CompareAsync(string label, string query, bool includeO, bool includeT, int skipVerifyTopN, bool deleteInverted)
    {
        var combined = await RunSearchAsync(_rootCombined, query, includeO, includeT, skipVerifyTopN, deleteInverted);
        var split = await RunSearchAsync(_rootSplit, query, includeO, includeT, skipVerifyTopN, deleteInverted);

        // Guard against a vacuous pass: every query is designed to match at least one rel.
        Assert.True(combined.Groups.Count > 0, $"[{label}] combined produced no groups — test corpus/query drifted");
        AssertOutcomesIdentical(label, combined, split);
    }

    private async Task AssertCorpusFreqIdenticalAsync()
    {
        var svcCombined = new SearchIndexService();
        Assert.NotNull(await svcCombined.TryLoadAsync(_rootCombined));

        var svcSplit = new SearchIndexService();
        Assert.NotNull(await svcSplit.TryLoadAsync(_rootSplit));

        Assert.True(svcCombined.HasCorpusFrequencies, "combined root has no corpusfreq");
        Assert.True(svcSplit.HasCorpusFrequencies, "split root has no merged corpusfreq");

        Assert.Equal(svcCombined.CorpusTotalChars, svcSplit.CorpusTotalChars);

        var cChars = svcCombined.CorpusCharFreqs!;
        var sChars = svcSplit.CorpusCharFreqs!;
        Assert.Equal(cChars.Count, sChars.Count);
        foreach (var kv in cChars)
        {
            Assert.True(sChars.TryGetValue(kv.Key, out var sv), $"char '{kv.Key}' missing from split corpusfreq");
            Assert.True(kv.Value == sv, $"char '{kv.Key}' freq combined={kv.Value} split={sv}");
        }

        var cBigrams = svcCombined.CorpusBigramFreqs!;
        var sBigrams = svcSplit.CorpusBigramFreqs!;
        Assert.Equal(cBigrams.Count, sBigrams.Count);
        foreach (var kv in cBigrams)
        {
            Assert.True(sBigrams.TryGetValue(kv.Key, out var sv), $"bigram '{kv.Key}' missing from split corpusfreq");
            Assert.True(kv.Value == sv, $"bigram '{kv.Key}' freq combined={kv.Value} split={sv}");
        }
    }
}
