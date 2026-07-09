using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Search;

/// <summary>
/// INC-1C: full-rebuild determinism baseline for the artifact-family equivalence
/// harness. Two consecutive from-scratch builds over the SAME corpus state must
/// produce equivalent artifact families (identical modulo BuiltUtc / IndexStamp).
///
/// This validates the harness itself (fixture + <see cref="ArtifactFamilyAssert"/>)
/// and extraction determinism BEFORE any incremental path exists — the later
/// incremental-vs-full equivalence tests reuse exactly these comparators.
/// </summary>
[Trait("Domain", "SearchSprint")]
public class FullRebuildDeterminismTests
{
    // ===== Fixture self-test: locks in the corpus shape the harness depends on =====

    [Fact]
    public void FixtureShape_SelfTest()
    {
        using var fx = new IndexFixtureCorpus();

        // ~30-40 minimal TEI files.
        Assert.InRange(fx.TotalFileCount, 30, 40);

        // Several rels with BOTH orig and tran sides.
        Assert.True(fx.BothSidesRels.Count >= 3, "fixture must have several both-sides rels");
        foreach (var rel in fx.BothSidesRels)
        {
            Assert.True(File.Exists(fx.OrigPath(rel)), $"orig side missing for both-sides rel '{rel}'");
            Assert.True(File.Exists(fx.TranPath(rel)), $"tran side missing for both-sides rel '{rel}'");
        }

        // At least one orig-only rel and one tran-only rel.
        Assert.NotEmpty(fx.OrigOnlyRels);
        foreach (var rel in fx.OrigOnlyRels)
        {
            Assert.True(File.Exists(fx.OrigPath(rel)), $"orig side missing for orig-only rel '{rel}'");
            Assert.False(File.Exists(fx.TranPath(rel)), $"tran side unexpectedly present for orig-only rel '{rel}'");
        }
        Assert.NotEmpty(fx.TranOnlyRels);
        foreach (var rel in fx.TranOnlyRels)
        {
            Assert.False(File.Exists(fx.OrigPath(rel)), $"orig side unexpectedly present for tran-only rel '{rel}'");
            Assert.True(File.Exists(fx.TranPath(rel)), $"tran side missing for tran-only rel '{rel}'");
        }

        // Tran sides carry CJK (the common gram) — spec: "tran side carries some CJK too".
        foreach (var rel in fx.BothSidesRels.Take(1).Concat(fx.TranOnlyRels))
        {
            Assert.Contains(IndexFixtureCorpus.CommonGram,
                File.ReadAllText(fx.TranPath(rel)), StringComparison.Ordinal);
        }

        // Deliberate sort gaps: the next gap rel is NOT in the corpus yet, and sorts
        // strictly between two existing rels (i.e. mid-corpus, never first/last).
        var gap = fx.NextGapRel; // "T/T01/c0015.xml"
        var sorted = fx.AllRels.OrderBy(r => r, StringComparer.OrdinalIgnoreCase).ToList();
        Assert.DoesNotContain(gap, sorted, StringComparer.OrdinalIgnoreCase);
        Assert.Contains(sorted, r => StringComparer.OrdinalIgnoreCase.Compare(r, gap) < 0);
        Assert.Contains(sorted, r => StringComparer.OrdinalIgnoreCase.Compare(r, gap) > 0);
        // And the concrete neighbors the gap name was designed to sit between exist.
        Assert.Contains("T/T01/b0010.xml", sorted, StringComparer.OrdinalIgnoreCase);
        Assert.Contains("T/T01/d0020.xml", sorted, StringComparer.OrdinalIgnoreCase);
        Assert.True(StringComparer.OrdinalIgnoreCase.Compare("T/T01/b0010.xml", gap) < 0
                 && StringComparer.OrdinalIgnoreCase.Compare(gap, "T/T01/d0020.xml") < 0,
            $"gap rel '{gap}' must sort strictly between b0010 and d0020");

        // Common gram in >80% of winner docs (arms the 0.8 DF cutoff).
        var fraction = fx.CommonGramWinnerDocFraction();
        Assert.True(fraction > 0.8, $"common gram winner-doc fraction {fraction:F2} must exceed 0.8");

        // --- Mutation helpers ---

        // AddFileMidCorpus: file appears and sorts strictly mid-corpus.
        var added = fx.AddFileMidCorpus();
        Assert.True(File.Exists(fx.OrigPath(added)), "added rel must exist on the orig side");
        Assert.True(File.Exists(fx.TranPath(added)), "added rel must exist on the tran side");
        var sortedAfterAdd = fx.AllRels.OrderBy(r => r, StringComparer.OrdinalIgnoreCase).ToList();
        int idx = sortedAfterAdd.FindIndex(r => string.Equals(r, added, StringComparison.OrdinalIgnoreCase));
        Assert.True(idx > 0 && idx < sortedAfterAdd.Count - 1,
            $"added rel '{added}' must sort mid-corpus (index {idx} of {sortedAfterAdd.Count})");

        // ChangeFile: byte length MUST change (stat miss AND hash miss).
        var changeRel = fx.BothSidesRels.First(r => !string.Equals(r, added, StringComparison.OrdinalIgnoreCase));
        long lenBefore = new FileInfo(fx.OrigPath(changeRel)).Length;
        var changed = fx.ChangeFile(changeRel);
        Assert.Equal(changeRel, changed);
        Assert.NotEqual(lenBefore, new FileInfo(fx.OrigPath(changeRel)).Length);

        // RemoveFile: both sides gone, tracking updated.
        var removeRel = fx.BothSidesRels.First(r =>
            !string.Equals(r, added, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(r, changeRel, StringComparison.OrdinalIgnoreCase));
        var removed = fx.RemoveFile(removeRel);
        Assert.Equal(removeRel, removed);
        Assert.False(File.Exists(fx.OrigPath(removeRel)));
        Assert.False(File.Exists(fx.TranPath(removeRel)));
        Assert.DoesNotContain(removeRel, fx.AllRels, StringComparer.OrdinalIgnoreCase);
    }

    // ===== (a) Determinism over an untouched corpus =====

    [Fact]
    public async Task FullRebuild_Twice_SameCorpus_ProducesEquivalentFamily()
    {
        using var fx = new IndexFixtureCorpus();
        var svc = new SearchIndexService();

        await svc.BuildAsync(fx.Root, fx.OrigDir, new[] { fx.TranDir });
        var a = ArtifactFamilyAssert.SnapshotFamily(fx.Root);
        await ArtifactFamilyAssert.AssertFamilyStampsAsync(fx.Root);

        // Do NOT touch corpus files between the two builds (LastWriteUtcTicks
        // equality depends on it).
        svc.InvalidateIndexCaches();
        await svc.BuildAsync(fx.Root, fx.OrigDir, new[] { fx.TranDir });
        var b = ArtifactFamilyAssert.SnapshotFamily(fx.Root);
        await ArtifactFamilyAssert.AssertFamilyStampsAsync(fx.Root);

        ArtifactFamilyAssert.AssertEquivalent(a, b);

        // The stamps must NOT be equal across builds — proves the comparison passes
        // because of the allowlists, not because both builds accidentally shared a stamp.
        Assert.False(string.Equals(a.MainManifest.IndexStamp, b.MainManifest.IndexStamp, StringComparison.Ordinal),
            "each build must mint a fresh IndexStamp");
        Assert.NotEmpty(a.MainManifest.Entries);
        Assert.Equal(a.MainManifest.Entries.Count, a.TextManifest.Entries.Count);
    }

    // ===== (b) Determinism after a mutation batch (add + remove + change) =====

    [Fact]
    public async Task FullRebuild_Twice_AfterMutationBatch_ProducesEquivalentFamily()
    {
        using var fx = new IndexFixtureCorpus();
        var svc = new SearchIndexService();

        // Build an initial family so the mutations happen against an existing state
        // (mirrors the real-world "git pull touched a few texts" situation).
        await svc.BuildAsync(fx.Root, fx.OrigDir, new[] { fx.TranDir });

        // Mutation batch: mid-corpus add (shifts later positional Ids), removal,
        // and an in-place content change.
        var added = fx.AddFileMidCorpus();
        var removeRel = fx.BothSidesRels.First(r => !string.Equals(r, added, StringComparison.OrdinalIgnoreCase));
        fx.RemoveFile(removeRel);
        var changeRel = fx.BothSidesRels.First(r => !string.Equals(r, added, StringComparison.OrdinalIgnoreCase));
        fx.ChangeFile(changeRel);

        svc.InvalidateIndexCaches();
        await svc.BuildAsync(fx.Root, fx.OrigDir, new[] { fx.TranDir });
        var a = ArtifactFamilyAssert.SnapshotFamily(fx.Root);
        await ArtifactFamilyAssert.AssertFamilyStampsAsync(fx.Root);

        svc.InvalidateIndexCaches();
        await svc.BuildAsync(fx.Root, fx.OrigDir, new[] { fx.TranDir });
        var b = ArtifactFamilyAssert.SnapshotFamily(fx.Root);
        await ArtifactFamilyAssert.AssertFamilyStampsAsync(fx.Root);

        ArtifactFamilyAssert.AssertEquivalent(a, b);
        Assert.False(string.Equals(a.MainManifest.IndexStamp, b.MainManifest.IndexStamp, StringComparison.Ordinal),
            "each build must mint a fresh IndexStamp");

        // The added rel made it into the manifest; the removed one is gone.
        Assert.Contains(a.MainManifest.Entries, e => string.Equals(e.RelPath, added, StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(a.MainManifest.Entries, e => string.Equals(e.RelPath, removeRel, StringComparison.OrdinalIgnoreCase));
    }

    // ===== DF-cutoff arming: the fixture's common gram really is cut at 0.8 =====

    [Fact]
    public async Task DfCutoff_Armed_CommonGramCut_RareGramResolvesToItsRel()
    {
        using var fx = new IndexFixtureCorpus();
        var svc = new SearchIndexService();
        await svc.BuildAsync(fx.Root, fx.OrigDir, new[] { fx.TranDir });

        var manifest = await svc.TryLoadAsync(fx.Root);
        Assert.NotNull(manifest);
        Assert.False(string.IsNullOrEmpty(manifest!.IndexStamp));

        var inv = new InvertedSearchIndex();
        Assert.True(await inv.TryLoadAsync(Path.Combine(fx.Root, "search.inverted.bin"), manifest.IndexStamp!),
            "inverted index must load against the freshly built manifest stamp");

        // The common gram is in 100% of winner docs — above the 0.8 DF cutoff — so its
        // postings were dropped at save time; the index reports zero candidates for it.
        var common = inv.Search(IndexFixtureCorpus.CommonGram);
        Assert.NotNull(common);
        Assert.Empty(common!);

        // A per-file rare gram survives the cutoff and resolves to exactly its rel.
        var rel = fx.BothSidesRels[0];
        var hits = inv.Search(fx.UniqueOrigGram(rel));
        Assert.NotNull(hits);
        var hitRels = hits!.Select(h => inv.GetRelPath(h)).ToList();
        var match = Assert.Single(hitRels);
        Assert.True(string.Equals(rel, match, StringComparison.OrdinalIgnoreCase),
            $"rare gram of '{rel}' resolved to '{match}'");
    }
}
