using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ReadZen.App.Models;
using ReadZen.App.Services;
using ReadZen.App.ViewModels;
using CommunityToolkit.Mvvm.Messaging;
using Xunit;

namespace ReadZen.Tests.Search;

/// <summary>
/// Tests for instant search (inverted-index v4 per-posting term frequency):
///   - the index round-trips tf through Save/Load;
///   - <see cref="InvertedSearchIndex.SearchWithTf"/> returns exact tf for a single
///     bigram and a min-over-bigrams estimate for a phrase;
///   - the service instant path (Options.InstantSearch) ranks candidates by tf and,
///     for single-bigram (2-char) queries, shows the tf count on skipped rows without
///     verifying the tail; multi-bigram queries are tf-ranked but fully verified so
///     scattered-bigram false positives never surface;
///   - the AppConfig / settings / SearchTabViewModel wiring defaults ON and applies
///     the persisted value via SettingsAppliedMessage.
/// </summary>
public sealed class InstantSearchTfTests : IDisposable
{
    private readonly string _root;
    private readonly string _origDir;
    private readonly string _tranDir;

    public InstantSearchTfTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "readzen-instant-" + Guid.NewGuid().ToString("N")[..8]);
        _origDir = Path.Combine(_root, "xml-p5");
        _tranDir = Path.Combine(_root, "xml-p5t");
        Directory.CreateDirectory(_origDir);
        Directory.CreateDirectory(_tranDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, true); } catch { }
    }

    // ===== InvertedSearchIndex tf unit tests =====

    [Fact]
    public void ComputeGramSetAndCounts_CountsRepeatedBigrams()
    {
        // "無門無門": bigrams 無門(pos0), 門無(pos1), 無門(pos2) → 無門×2, 門無×1.
        var (grams, counts) = InvertedSearchIndex.ComputeGramSetAndCounts("無門無門");

        // grams identical (unique, sorted) to the set-only producer.
        Assert.Equal(InvertedSearchIndex.ComputeGramSet("無門無門"), grams);

        var byGram = grams.Zip(counts).ToDictionary(z => z.First, z => z.Second);
        uint wumen = ((uint)'無' << 16) | '門';
        uint menwu = ((uint)'門' << 16) | '無';
        Assert.Equal(2, byGram[wumen]);
        Assert.Equal(1, byGram[menwu]);
        Assert.All(counts, c => Assert.True(c >= 1));
    }

    [Fact]
    public async Task SearchWithTf_SingleBigram_IsExactCount_AndRoundTrips()
    {
        var idx = new InvertedSearchIndex();
        // Filler docs keep 無門's document frequency under the 80% high-DF cutoff
        // (2 of 5 docs = 40%), so the term survives in the index.
        idx.Build(new List<(string, string)>
        {
            ("a.xml", "無門無門無門"),  // 無門 occurs 3×
            ("b.xml", "無門"),          // 無門 occurs 1×
            ("f1.xml", "山水山水"),
            ("f2.xml", "祖師祖師"),
            ("f3.xml", "西來西來"),
        });

        var hits = idx.SearchWithTf("無門");
        Assert.NotNull(hits);
        var tfByPath = hits!.ToDictionary(h => idx.GetRelPath(h.docId)!, h => h.tf);
        Assert.Equal(3, tfByPath["a.xml"]);
        Assert.Equal(1, tfByPath["b.xml"]);

        // Round-trip through Save/Load preserves tf.
        var bin = Path.Combine(_root, "rt.inverted.bin");
        await idx.SaveAsync(bin, "stamp-rt");
        var loaded = new InvertedSearchIndex();
        Assert.True(await loaded.TryLoadAsync(bin, "stamp-rt"));

        var loadedHits = loaded.SearchWithTf("無門");
        Assert.NotNull(loadedHits);
        var loadedTf = loadedHits!.ToDictionary(h => loaded.GetRelPath(h.docId)!, h => h.tf);
        Assert.Equal(3, loadedTf["a.xml"]);
        Assert.Equal(1, loadedTf["b.xml"]);

        // docId-only Search is unchanged (still returns the matching docs, ascending).
        Assert.Equal(loaded.Search("無門"), loadedHits.Select(h => h.docId).ToArray());
    }

    [Fact]
    public void SearchWithTf_Phrase_IsMinOverBigrams()
    {
        var idx = new InvertedSearchIndex();
        idx.Build(new List<(string, string)>
        {
            // 無門關×2 contiguous: 無門×2, 門關×2 → min = 2.
            ("full.xml", "無門關無門關"),
            // 無門×2 but 門關×1 → min = 1.
            ("partial.xml", "無門關無門"),
            // Filler keeps 無門/門關 under the 80% high-DF cutoff.
            ("f1.xml", "山水山水"),
            ("f2.xml", "祖師祖師"),
            ("f3.xml", "西來西來"),
        });

        var hits = idx.SearchWithTf("無門關");
        Assert.NotNull(hits);
        var tf = hits!.ToDictionary(h => idx.GetRelPath(h.docId)!, h => h.tf);
        Assert.Equal(2, tf["full.xml"]);
        Assert.Equal(1, tf["partial.xml"]);
    }

    [Fact]
    public void SearchWithTf_MissingBigram_ReturnsEmpty()
    {
        var idx = new InvertedSearchIndex();
        idx.Build(new List<(string, string)> { ("a.xml", "無門") });
        Assert.Empty(idx.SearchWithTf("祖師")!);
    }

    // ===== Service instant-mode integration =====

    /// <summary>
    /// Build a corpus where the highest-frequency files are the SMALLEST: file i gets
    /// (fileCount - i) contiguous repetitions of <paramref name="match"/> (so tf desc =
    /// file index asc) plus i*50 padding chars (so size desc = file index desc). tf
    /// ranking and size ranking therefore disagree — proving instant mode ranks by tf.
    /// </summary>
    private async Task<SearchIndexService> BuildInverseCorpusAsync(int fileCount, string match, int topN)
    {
        for (int i = 0; i < fileCount; i++)
        {
            int reps = fileCount - i;
            var sb = new System.Text.StringBuilder();
            for (int k = 0; k < reps; k++) sb.Append(match);
            sb.Append(new string('中', i * 50)); // padding grows with i (non-matching)
            File.WriteAllText(Path.Combine(_origDir, $"f{i:D3}.xml"),
                $"<TEI><text><body>{sb}</body></text></TEI>");
        }

        // Filler files WITHOUT the match keep the query bigrams under the 80% high-DF
        // cutoff so they stay in the inverted index (needed for the instant/tf path).
        // fileCount match files must be ≤ 80% of the total → add fileCount/2 fillers.
        int fillerCount = fileCount; // total = 2*fileCount → match ratio 50%
        for (int i = 0; i < fillerCount; i++)
        {
            File.WriteAllText(Path.Combine(_origDir, $"g{i:D3}.xml"),
                $"<TEI><text><body>{new string('水', 60)}</body></text></TEI>");
        }

        var svc = new SearchIndexService();
        svc.Options.InstantSearch = true;
        svc.Options.SkipVerifySnippetTopN = topN;
        await svc.BuildAsync(_root, _origDir, new[] { _tranDir });
        return svc;
    }

    private async Task<List<SearchResultGroup>> RunAsync(SearchIndexService svc, string query)
    {
        var manifest = await svc.TryLoadAsync(_root);
        Assert.NotNull(manifest);
        var groups = new List<SearchResultGroup>();
        await foreach (var g in svc.SearchAllAsync(
            _root, _origDir, _tranDir, manifest!, query,
            includeOriginal: true, includeTranslated: false,
            fileMeta: rel => (rel, rel, (TranslationStatus?)null), contextWidth: 30))
        {
            groups.Add(g);
        }
        return groups;
    }

    [Fact]
    public async Task InstantMode_ThreeCharQuery_VerifiesEveryone_NoSkipVerify()
    {
        // 25 files, 3-char (multi-bigram) query. The inverted index proves only that the
        // constituent bigrams (無門, 門關) CO-OCCUR in a doc, never that they form the
        // contiguous phrase 無門關 — so instant mode must NOT skip-verify the tail for a
        // multi-bigram query (that would surface scattered-bigram false positives). Every
        // candidate is verified; the tf count on each row is the real verified match count.
        var svc = await BuildInverseCorpusAsync(fileCount: 25, match: "無門關", topN: 5);
        var groups = await RunAsync(svc, "無門關");

        Assert.Equal(25, groups.Count);

        // No skip-verify for a multi-bigram query: all 25 candidates eagerly verified.
        Assert.Equal(0, svc.LastSearchSkippedVerifyGroups);
        Assert.Equal(25, svc.LastSearchVerifiedGroups);
        Assert.All(groups, g => Assert.False(g.Children[0].IsSkippedVerify));

        // Every emitted row is a real match with a populated KWIC snippet.
        Assert.All(groups, g => Assert.NotEqual("", g.Children[0].Hit.Match));

        // Verified counts reflect the actual contiguous repetitions: file i has (25 - i)
        // copies of 無門關 → f010 has 15.
        var f010 = groups.Single(g => Path.GetFileName(g.RelPath) == "f010.xml");
        Assert.Equal(15, f010.HitsOriginal);
    }

    [Fact]
    public async Task InstantMode_MultiBigramQuery_ExcludesScatteredBigramFalsePositive()
    {
        // Regression (S9 critical): a doc that contains 無門 and 門關 in unrelated places
        // but NEVER the contiguous phrase 無門關 satisfies the inverted-index bigram
        // intersection (tf = min = 1). Pre-fix, if it fell into the instant skip-verify
        // tail it was emitted as a result with a positive hit count and zero real matches.
        // Post-fix, multi-bigram instant queries verify every candidate, so it is dropped.
        File.WriteAllText(Path.Combine(_origDir, "scatter.xml"),
            "<TEI><text><body>無門山山山門關</body></text></TEI>"); // 無門 + 門關, no 無門關
        File.WriteAllText(Path.Combine(_origDir, "real.xml"),
            "<TEI><text><body>無門關無門關</body></text></TEI>");   // genuine phrase ×2
        // Filler keeps 無門/門關 document frequency under the 80% high-DF cutoff.
        for (int i = 0; i < 6; i++)
            File.WriteAllText(Path.Combine(_origDir, $"g{i:D2}.xml"),
                $"<TEI><text><body>{new string('水', 40)}</body></text></TEI>");

        var svc = new SearchIndexService();
        svc.Options.InstantSearch = true;
        // Tiny budget: pre-fix, any candidate beyond the top-1 became an unverified
        // placeholder — so the scattered doc would have been emitted with tf=1.
        svc.Options.SkipVerifySnippetTopN = 1;
        await svc.BuildAsync(_root, _origDir, new[] { _tranDir });

        var groups = await RunAsync(svc, "無門關");

        Assert.Equal(0, svc.LastSearchSkippedVerifyGroups);
        Assert.All(groups, g => Assert.False(g.Children[0].IsSkippedVerify));
        Assert.DoesNotContain(groups, g => Path.GetFileName(g.RelPath) == "scatter.xml");
        Assert.Contains(groups, g => Path.GetFileName(g.RelPath) == "real.xml");
    }

    [Fact]
    public async Task InstantMode_SingleBigram_BothSides_TfOnlyOnKeptSide()
    {
        // Regression (S9 major): the inverted index dedups to one doc per relPath
        // (keep-first, original-then-translated), so tf measures only ONE side. When both
        // sides are requested for a skip-verified single-bigram row, the exact tf must be
        // attributed to a single side (original, matching keep-first) and the other side
        // must keep the honest "at least one" sentinel — not a precise count for a side the
        // index never measured.
        File.WriteAllText(Path.Combine(_origDir, "a.xml"),
            "<TEI><text><body>無門無門無門</body></text></TEI>"); // 無門 ×3 (original side)
        File.WriteAllText(Path.Combine(_tranDir, "a.xml"),
            "<TEI><text><body>無門</body></text></TEI>");         // translated side (unmeasured)
        // A higher-tf sibling so, with topN=1, a.xml lands in the SKIPPED tail (its row is
        // the one whose count comes straight from tf, not from verification).
        File.WriteAllText(Path.Combine(_origDir, "b.xml"),
            "<TEI><text><body>無門無門無門無門無門</body></text></TEI>"); // 無門 ×5
        // Filler keeps 無門 under the high-DF cutoff.
        for (int i = 0; i < 4; i++)
            File.WriteAllText(Path.Combine(_origDir, $"g{i:D2}.xml"),
                $"<TEI><text><body>{new string('水', 40)}</body></text></TEI>");

        var svc = new SearchIndexService();
        svc.Options.InstantSearch = true;
        svc.Options.SkipVerifySnippetTopN = 1; // only the top-tf doc (b.xml) is verified; a.xml is skipped
        await svc.BuildAsync(_root, _origDir, new[] { _tranDir });

        var manifest = await svc.TryLoadAsync(_root);
        Assert.NotNull(manifest);
        var groups = new List<SearchResultGroup>();
        await foreach (var g in svc.SearchAllAsync(
            _root, _origDir, _tranDir, manifest!, "無門",
            includeOriginal: true, includeTranslated: true,
            fileMeta: rel => (rel, rel, (TranslationStatus?)null), contextWidth: 30))
        {
            groups.Add(g);
        }

        var a = groups.Single(g => Path.GetFileName(g.RelPath) == "a.xml");
        Assert.True(a.Children[0].IsSkippedVerify);
        Assert.Equal(3, a.HitsOriginal);          // exact tf on the kept (original) side
        Assert.Equal(1, a.HitsTranslated);        // sentinel, not the wrong side's count
    }

    [Fact]
    public async Task InstantMode_Off_ThreeCharQuery_VerifiesEveryone()
    {
        // Same corpus, instant OFF (service default). 3-char falls through the legacy
        // 2-char-only hybrid, so every candidate is eagerly verified — the tail is NOT
        // skipped. Proves the flag gates the new behaviour.
        var svc = await BuildInverseCorpusAsync(fileCount: 15, match: "無門關", topN: 5);
        svc.Options.InstantSearch = false;
        var groups = await RunAsync(svc, "無門關");

        Assert.Equal(15, groups.Count);
        Assert.Equal(0, svc.LastSearchSkippedVerifyGroups);
        Assert.All(groups, g => Assert.False(g.Children[0].IsSkippedVerify));
    }

    // ===== Config / settings / VM wiring =====

    [Fact]
    public void AppConfig_InstantSearch_DefaultsTrue()
    {
        Assert.True(new AppConfig().InstantSearch);
    }

    [Fact]
    public void SettingsViewModel_RoundTripsInstantSearch()
    {
        var cfg = new AppConfig { InstantSearch = false };
        var vm = new SettingsWindowViewModel(cfg);
        Assert.False(vm.InstantSearch);

        vm.Username = "tester";
        vm.InstantSearch = true;
        vm.ApplyCommand.Execute(null);

        Assert.NotNull(vm.Result);
        Assert.True(vm.Result!.InstantSearch);
    }

    [Fact]
    public void SearchTabViewModel_DefaultsInstantOn_AndPushesToServiceOptions()
    {
        var svc = new SearchIndexService();
        // Isolated messenger so concurrent tests' SettingsAppliedMessage broadcasts on the
        // process-wide default cannot flip this VM's InstantSearch mid-assert.
        var vm = new SearchTabViewModel(svc, messenger: new WeakReferenceMessenger());

        Assert.True(vm.InstantSearch);
        Assert.True(svc.Options.InstantSearch); // pushed in ctor

        vm.InstantSearch = false;
        Assert.False(svc.Options.InstantSearch); // live push on change
    }

    [Fact]
    public void SearchTabViewModel_AppliesInstantSearchFromSettingsAppliedMessage()
    {
        // Regression (S9 major): the 'Instant search' settings checkbox round-trips into
        // AppConfig.InstantSearch, but nothing wired that persisted value onto the VM /
        // service — the ctor default (true) always won. The VM now subscribes to
        // SettingsAppliedMessage (broadcast on both startup config load and Settings ▸
        // Apply), so AppConfig.InstantSearch=false reaches Options.InstantSearch=false.
        var svc = new SearchIndexService();
        var messenger = new WeakReferenceMessenger();
        var vm = new SearchTabViewModel(svc, messenger: messenger);

        Assert.True(vm.InstantSearch);          // ctor default
        Assert.True(svc.Options.InstantSearch);

        messenger.Send(new ReadZen.App.Messages.SettingsAppliedMessage(
            new AppConfig { InstantSearch = false }));

        Assert.False(vm.InstantSearch);         // AppConfig → VM
        Assert.False(svc.Options.InstantSearch); // VM → service Options

        // And back on again.
        messenger.Send(new ReadZen.App.Messages.SettingsAppliedMessage(
            new AppConfig { InstantSearch = true }));
        Assert.True(vm.InstantSearch);
        Assert.True(svc.Options.InstantSearch);
    }
}
