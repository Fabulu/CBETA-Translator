using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ReadZen.App.Models;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Search;

/// <summary>
/// INC-2A: incremental-vs-full equivalence for the search index artifact family.
///
/// The EQUIVALENCE INVARIANT (S6, non-negotiable): <c>BuildOrUpdateAsync(forceRebuild:
/// false)</c> over a corpus delta must produce artifacts equivalent to a from-scratch
/// full rebuild of the same corpus state — identical modulo BuiltUtc / IndexStamp.
/// Positional-Id renumbering is the highest-severity risk: manifest Ids, cjk2 EntryIds
/// and inverted docIds must renumber consistently on every add/remove, which is why
/// every case here runs the full <see cref="ArtifactFamilyAssert"/> comparison plus
/// semantic backstops against the freshly written incremental artifacts.
///
/// Also proven here:
///   - skip-read (D3 item 3): an N-file delta reads exactly N XML files
///     (<c>LastBuildXmlReadCount</c>);
///   - InputHash single-pass (D3 item 1): the manifest InputHash written by the build
///     is value-identical to a cache:null computation;
///   - single fallback (S5): a fault in incremental-only code retries ONCE as a full
///     rebuild inside the same gate acquisition, leaving no family tmp files behind.
/// </summary>
[Trait("Domain", "SearchSprint")]
public class IncrementalEquivalenceTests
{
    // =====================================================================
    // Shared flow: mutate → incremental → snapshot+backstops → full → compare
    // =====================================================================

    /// <summary>
    /// Runs the incremental update, snapshots + stamp-checks its family, then runs the
    /// comparison full rebuild over the SAME corpus state (no file touched in between)
    /// and asserts family equivalence. Returns the INCREMENTAL snapshot (already
    /// validated) so callers can run additional semantic backstops against it.
    /// The <paramref name="incrementalBackstops"/> callback runs while the incremental
    /// artifacts are still the ones on disk (before the comparison rebuild replaces them).
    /// </summary>
    private static async Task<ArtifactFamilyAssert.FamilySnapshot> RunIncrementalVsFullAsync(
        IndexFixtureCorpus fx,
        SearchIndexService svc,
        Func<ArtifactFamilyAssert.FamilySnapshot, Task>? incrementalBackstops = null)
    {
        await svc.BuildOrUpdateAsync(fx.Root, fx.OrigDir, new[] { fx.TranDir }, forceRebuild: false);
        var incremental = ArtifactFamilyAssert.SnapshotFamily(fx.Root);
        await ArtifactFamilyAssert.AssertFamilyStampsAsync(fx.Root);
        AssertNoFamilyTmpFiles(fx.Root);

        if (incrementalBackstops != null)
            await incrementalBackstops(incremental);

        // Comparison full rebuild of the same corpus state. Corpus files are NOT
        // touched between the two builds (LastWriteUtcTicks equality depends on it).
        svc.InvalidateIndexCaches();
        await svc.BuildAsync(fx.Root, fx.OrigDir, new[] { fx.TranDir });
        var full = ArtifactFamilyAssert.SnapshotFamily(fx.Root);
        await ArtifactFamilyAssert.AssertFamilyStampsAsync(fx.Root);

        ArtifactFamilyAssert.AssertEquivalent(incremental, full);

        // INC-5A: beyond the DTO allowlist compare, the cjk2 manifests must be
        // byte-equal under canonical serialization (IndexStamp nulled, BuiltUtc
        // zeroed, same WriteIndented=true options) — proves the transpose's
        // gram-string production is identical to the full path's, escaping included.
        Assert.Equal(
            Cjk2Canonical.Serialize(full.Cjk2Manifest),
            Cjk2Canonical.Serialize(incremental.Cjk2Manifest));

        // Both builds minted their own stamps — proves the comparison passed via the
        // allowlists, not via accidentally shared per-build values.
        Assert.False(string.Equals(incremental.MainManifest.IndexStamp, full.MainManifest.IndexStamp, StringComparison.Ordinal),
            "incremental and full builds must mint distinct IndexStamps");

        return incremental;
    }

    private static void AssertNoFamilyTmpFiles(string root)
    {
        var strays = Directory.GetFiles(root, "*.tmp", SearchOption.TopDirectoryOnly);
        Assert.True(strays.Length == 0,
            $"no *.tmp files may remain in the index root after a build; found: {string.Join(", ", strays.Select(Path.GetFileName))}");
    }

    /// <summary>
    /// Semantic backstop: loads the on-disk inverted index against <paramref name="stamp"/>
    /// and asserts <paramref name="gram"/> resolves to exactly <paramref name="expectedRel"/>.
    /// </summary>
    private static async Task AssertInvertedResolvesAsync(string root, string? stamp, string gram, string expectedRel)
    {
        Assert.False(string.IsNullOrEmpty(stamp));
        var inv = new InvertedSearchIndex();
        Assert.True(await inv.TryLoadAsync(Path.Combine(root, "search.inverted.bin"), stamp!),
            "inverted index must load against the incremental build's IndexStamp");

        var hits = inv.Search(gram);
        Assert.NotNull(hits);
        var rels = hits!.Select(h => inv.GetRelPath(h)).ToList();
        var match = Assert.Single(rels);
        Assert.True(string.Equals(expectedRel, match, StringComparison.OrdinalIgnoreCase),
            $"gram '{gram}' resolved to '{match}', expected '{expectedRel}'");
    }

    /// <summary>
    /// Semantic backstop: a cjk2 gram unique to one (rel, side) entry must map to that
    /// entry's NEW positional Id in the same snapshot's main manifest.
    /// </summary>
    private static void AssertCjk2GramMapsToEntry(
        ArtifactFamilyAssert.FamilySnapshot snap, string gram, string expectedRel, SearchSide expectedSide)
    {
        var posting = snap.Cjk2Manifest.Postings.FirstOrDefault(p => string.Equals(p.Gram, gram, StringComparison.Ordinal));
        Assert.True(posting != null, $"cjk2 posting for gram '{gram}' missing");
        var id = Assert.Single(posting!.EntryIds);
        var entry = snap.MainManifest.Entries.FirstOrDefault(e => e.Id == id);
        Assert.True(entry != null, $"cjk2 EntryId {id} has no matching main manifest entry");
        Assert.Equal(expectedSide, entry!.Side);
        Assert.True(string.Equals(expectedRel, entry.RelPath, StringComparison.OrdinalIgnoreCase),
            $"cjk2 gram '{gram}' mapped to '{entry.RelPath}'/{entry.Side}, expected '{expectedRel}'/{expectedSide}");
    }

    // ============ (1) add-only, mid sort order (positional Id shift) ============

    [Fact]
    public async Task Incremental_AddOnly_MidCorpus_EquivalentToFull()
    {
        using var fx = new IndexFixtureCorpus();
        var svc = new SearchIndexService();
        await svc.BuildAsync(fx.Root, fx.OrigDir, new[] { fx.TranDir });

        var added = fx.AddFileMidCorpus();
        // A pre-existing rel that sorts AFTER the insertion point: its positional Ids shift.
        var shifted = fx.BothSidesRels.First(r =>
            !string.Equals(r, added, StringComparison.OrdinalIgnoreCase) &&
            StringComparer.OrdinalIgnoreCase.Compare(r, added) > 0);

        await RunIncrementalVsFullAsync(fx, svc, async snap =>
        {
            // Query hitting the ADDED file resolves to the correct relPath.
            await AssertInvertedResolvesAsync(fx.Root, snap.MainManifest.IndexStamp, fx.UniqueOrigGram(added), added);
            // A cjk2 gram unique to a SHIFTED file maps to its NEW manifest Id.
            AssertCjk2GramMapsToEntry(snap, fx.UniqueOrigGram(shifted), shifted, SearchSide.Original);
        });
    }

    // ============ (2) remove-only ============

    [Fact]
    public async Task Incremental_RemoveOnly_EquivalentToFull()
    {
        using var fx = new IndexFixtureCorpus();
        var svc = new SearchIndexService();
        await svc.BuildAsync(fx.Root, fx.OrigDir, new[] { fx.TranDir });

        var removed = fx.RemoveFile(fx.BothSidesRels[1]);
        // A rel sorted AFTER the removal point: its positional Ids shift down.
        var afterRemoval = fx.BothSidesRels.First(r => StringComparer.OrdinalIgnoreCase.Compare(r, removed) > 0);

        await RunIncrementalVsFullAsync(fx, svc, async snap =>
        {
            Assert.DoesNotContain(snap.MainManifest.Entries,
                e => string.Equals(e.RelPath, removed, StringComparison.OrdinalIgnoreCase));
            // A file sorted after the removal point still resolves to its correct relPath.
            await AssertInvertedResolvesAsync(fx.Root, snap.MainManifest.IndexStamp, fx.UniqueOrigGram(afterRemoval), afterRemoval);
            AssertCjk2GramMapsToEntry(snap, fx.UniqueOrigGram(afterRemoval), afterRemoval, SearchSide.Original);
        });
    }

    // ============ (3) mid-corpus content change ============

    [Fact]
    public async Task Incremental_MidCorpusContentChange_EquivalentToFull()
    {
        using var fx = new IndexFixtureCorpus();
        var svc = new SearchIndexService();
        await svc.BuildAsync(fx.Root, fx.OrigDir, new[] { fx.TranDir });

        var changed = fx.ChangeFile(fx.BothSidesRels[fx.BothSidesRels.Count / 2]);

        await RunIncrementalVsFullAsync(fx, svc, async snap =>
        {
            await AssertInvertedResolvesAsync(fx.Root, snap.MainManifest.IndexStamp, fx.UniqueOrigGram(changed), changed);
        });
    }

    // ============ (4) tran-side-only change of a rel that HAS an original ============

    [Fact]
    public async Task Incremental_TranSideOnlyChange_OfOrigedRel_EquivalentToFull()
    {
        using var fx = new IndexFixtureCorpus();
        var svc = new SearchIndexService();
        await svc.BuildAsync(fx.Root, fx.OrigDir, new[] { fx.TranDir });

        // Rewrite ONLY the translated side (fixture's ChangeFile prefers orig). The rel
        // has an original, so the inverted winner doc (keep-first = orig side) is
        // provably unchanged — the equivalence comparison proves the incremental path
        // reproduces that exactly. Marker chars from U+6900 avoid every fixture range.
        var rel = fx.BothSidesRels[2];
        var tranPath = fx.TranPath(rel);
        var text = File.ReadAllText(tranPath);
        File.WriteAllText(tranPath, text.Replace("</p>", "椀椁譯文</p>", StringComparison.Ordinal));

        await RunIncrementalVsFullAsync(fx, svc, async snap =>
        {
            // Winner doc for the rel is still the ORIGINAL side.
            await AssertInvertedResolvesAsync(fx.Root, snap.MainManifest.IndexStamp, fx.UniqueOrigGram(rel), rel);
            // The tran-side entry picked up the new content in cjk2 (new unique pair <U+6900 U+6901>).
            AssertCjk2GramMapsToEntry(snap, "椀椁", rel, SearchSide.Translated);
        });
    }

    // ============ (5) winner flip, both directions ============

    [Fact]
    public async Task Incremental_WinnerFlip_BothDirections_EquivalentToFull()
    {
        using var fx = new IndexFixtureCorpus();
        var svc = new SearchIndexService();
        await svc.BuildAsync(fx.Root, fx.OrigDir, new[] { fx.TranDir });

        // Direction A: ADD an original over a tran-only rel → the inverted winner for
        // that rel flips from the tran side to the new orig side.
        var flipToOrig = fx.TranOnlyRels[0];
        const string newOrigGram = "渀渁"; // unique pair from a range no fixture text uses
        var origPath = fx.OrigPath(flipToOrig);
        Directory.CreateDirectory(Path.GetDirectoryName(origPath)!);
        File.WriteAllText(origPath,
            "<TEI xmlns=\"http://www.tei-c.org/ns/1.0\"><text><body><p>" +
            IndexFixtureCorpus.CommonGram + newOrigGram + "參禅問道</p></body></text></TEI>");

        // Direction B: REMOVE the original of a both-sides rel, leaving the tran side →
        // the inverted winner flips from orig to tran.
        var flipToTran = fx.BothSidesRels[0];
        File.Delete(fx.OrigPath(flipToTran));

        await RunIncrementalVsFullAsync(fx, svc, async snap =>
        {
            // A: the new orig side is present, and is the inverted winner for the rel.
            await AssertInvertedResolvesAsync(fx.Root, snap.MainManifest.IndexStamp, newOrigGram, flipToOrig);
            AssertCjk2GramMapsToEntry(snap, newOrigGram, flipToOrig, SearchSide.Original);

            // B: the tran side is now the winner doc for its rel.
            await AssertInvertedResolvesAsync(fx.Root, snap.MainManifest.IndexStamp, fx.UniqueTranGram(flipToTran), flipToTran);
            Assert.DoesNotContain(snap.MainManifest.Entries, e =>
                string.Equals(e.RelPath, flipToTran, StringComparison.OrdinalIgnoreCase) && e.Side == SearchSide.Original);
        });
    }

    // ============ (6) flagship: combined add + remove + change ============

    [Fact]
    public async Task Incremental_Flagship_AddRemoveChange_EquivalentToFull()
    {
        using var fx = new IndexFixtureCorpus();
        var svc = new SearchIndexService();
        await svc.BuildAsync(fx.Root, fx.OrigDir, new[] { fx.TranDir });

        var added = fx.AddFileMidCorpus();
        var removed = fx.RemoveFile(fx.BothSidesRels.First(r => !string.Equals(r, added, StringComparison.OrdinalIgnoreCase)));
        var changed = fx.ChangeFile(fx.BothSidesRels.First(r => !string.Equals(r, added, StringComparison.OrdinalIgnoreCase)));

        // A rel untouched by all three mutations, sorted after BOTH the add and the
        // removal point, so its positional Ids definitely shifted.
        var shifted = fx.BothSidesRels.Last(r =>
            !string.Equals(r, added, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(r, changed, StringComparison.OrdinalIgnoreCase) &&
            StringComparer.OrdinalIgnoreCase.Compare(r, added) > 0 &&
            StringComparer.OrdinalIgnoreCase.Compare(r, removed) > 0);

        await RunIncrementalVsFullAsync(fx, svc, async snap =>
        {
            Assert.DoesNotContain(snap.MainManifest.Entries,
                e => string.Equals(e.RelPath, removed, StringComparison.OrdinalIgnoreCase));

            // Added file, changed file, and a shifted untouched file all resolve via the
            // loaded inverted index.
            await AssertInvertedResolvesAsync(fx.Root, snap.MainManifest.IndexStamp, fx.UniqueOrigGram(added), added);
            await AssertInvertedResolvesAsync(fx.Root, snap.MainManifest.IndexStamp, fx.UniqueOrigGram(changed), changed);
            await AssertInvertedResolvesAsync(fx.Root, snap.MainManifest.IndexStamp, fx.UniqueOrigGram(shifted), shifted);

            // cjk2 grams unique to shifted files map to their NEW manifest Ids.
            AssertCjk2GramMapsToEntry(snap, fx.UniqueOrigGram(shifted), shifted, SearchSide.Original);
            AssertCjk2GramMapsToEntry(snap, fx.UniqueTranGram(shifted), shifted, SearchSide.Translated);
        });
    }

    // ============ (6b) INC-4A flagship: add + remove + change + winner flips over a
    // WARM gramsets sidecar — the transpose path (cached + fresh uncut gram sets) must
    // reproduce the full rebuild exactly (.paths byte-identical, inverted bin
    // structurally identical — both enforced by AssertEquivalent). ============

    [Fact]
    public async Task Incremental_Flagship_WithWinnerFlips_TransposePath_EquivalentToFull()
    {
        using var fx = new IndexFixtureCorpus();
        var svc = new SearchIndexService();
        // Full baseline build — persists the gramsets sidecar, so the incremental run
        // below sources unchanged entries' gram sets from the cache (transpose path).
        await svc.BuildAsync(fx.Root, fx.OrigDir, new[] { fx.TranDir });

        var added = fx.AddFileMidCorpus();
        var removed = fx.RemoveFile(fx.BothSidesRels.First(r => !string.Equals(r, added, StringComparison.OrdinalIgnoreCase)));
        var changed = fx.ChangeFile(fx.BothSidesRels.First(r => !string.Equals(r, added, StringComparison.OrdinalIgnoreCase)));

        // Winner flip A: ADD an original over a tran-only rel (winner: tran → orig).
        var flipToOrig = fx.TranOnlyRels[0];
        const string newOrigGram = "潀潁"; // unique pair from a range no fixture/test text uses
        var flipOrigPath = fx.OrigPath(flipToOrig);
        Directory.CreateDirectory(Path.GetDirectoryName(flipOrigPath)!);
        File.WriteAllText(flipOrigPath,
            "<TEI xmlns=\"http://www.tei-c.org/ns/1.0\"><text><body><p>" +
            IndexFixtureCorpus.CommonGram + newOrigGram + "拈花微笑</p></body></text></TEI>");

        // Winner flip B: REMOVE the original of a both-sides rel (winner: orig → tran).
        var flipToTran = fx.BothSidesRels.First(r =>
            !string.Equals(r, added, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(r, changed, StringComparison.OrdinalIgnoreCase));
        File.Delete(fx.OrigPath(flipToTran));

        // An untouched rel sorted after BOTH the add and the removal point, so its
        // positional Ids (manifest Id, cjk2 EntryId, inverted docId) definitely shifted.
        var shifted = fx.BothSidesRels.Last(r =>
            !string.Equals(r, added, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(r, changed, StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(r, flipToTran, StringComparison.OrdinalIgnoreCase) &&
            StringComparer.OrdinalIgnoreCase.Compare(r, added) > 0 &&
            StringComparer.OrdinalIgnoreCase.Compare(r, removed) > 0);

        await RunIncrementalVsFullAsync(fx, svc, async snap =>
        {
            // Warm-sidecar transpose proof: exactly the genuinely new/changed entries
            // computed gram sets — added orig+tran (2) + changed orig (1) + the new
            // flip original (1). Everything else was sidecar-read.
            Assert.Equal(4, svc.LastBuildGramComputeCount);

            Assert.DoesNotContain(snap.MainManifest.Entries,
                e => string.Equals(e.RelPath, removed, StringComparison.OrdinalIgnoreCase));

            await AssertInvertedResolvesAsync(fx.Root, snap.MainManifest.IndexStamp, fx.UniqueOrigGram(added), added);
            await AssertInvertedResolvesAsync(fx.Root, snap.MainManifest.IndexStamp, fx.UniqueOrigGram(changed), changed);
            await AssertInvertedResolvesAsync(fx.Root, snap.MainManifest.IndexStamp, fx.UniqueOrigGram(shifted), shifted);

            // Flip A: the new orig side is the inverted winner for its rel.
            await AssertInvertedResolvesAsync(fx.Root, snap.MainManifest.IndexStamp, newOrigGram, flipToOrig);
            AssertCjk2GramMapsToEntry(snap, newOrigGram, flipToOrig, SearchSide.Original);

            // Flip B: the tran side is now the winner doc for its rel.
            await AssertInvertedResolvesAsync(fx.Root, snap.MainManifest.IndexStamp, fx.UniqueTranGram(flipToTran), flipToTran);

            // cjk2 grams unique to the shifted file map to its NEW manifest Ids.
            AssertCjk2GramMapsToEntry(snap, fx.UniqueOrigGram(shifted), shifted, SearchSide.Original);
            AssertCjk2GramMapsToEntry(snap, fx.UniqueTranGram(shifted), shifted, SearchSide.Translated);
        });
    }

    // ============ (7) corrupted old text.bin → build still succeeds + fallback proof ============

    [Fact]
    public async Task Incremental_TruncatedOldTextBin_BuildSucceeds_Equivalent_NoTmpFiles()
    {
        using var fx = new IndexFixtureCorpus();
        var svc = new SearchIndexService();
        await svc.BuildAsync(fx.Root, fx.OrigDir, new[] { fx.TranDir });

        // Truncate the committed text sidecar to 10 bytes. The old text manifest then
        // fails its bounds validation on load, every entry is classified changed, and
        // the incremental attempt degrades to a full per-entry recompute — it must
        // still commit a complete, correct family without leaving tmp files around.
        using (var trunc = new FileStream(Path.Combine(fx.Root, "search.text.bin"), FileMode.Open, FileAccess.Write, FileShare.None))
        {
            trunc.SetLength(10);
        }

        var incremental = await RunIncrementalVsFullAsync(fx, svc, snap =>
        {
            // At this point the last core run is the incremental attempt: graceful
            // degradation re-read every XML (nothing could be sourced from the ruined
            // sidecar), yet the family committed completely.
            Assert.Equal(fx.TotalFileCount, svc.LastBuildXmlReadCount);
            return Task.CompletedTask;
        });
        Assert.NotEmpty(incremental.MainManifest.Entries);
    }

    [Fact]
    public async Task Incremental_FaultInIncrementalPath_RetriesAsFullRebuild_Equivalent_NoTmpFiles()
    {
        using var fx = new IndexFixtureCorpus();
        var svc = new SearchIndexService();
        await svc.BuildAsync(fx.Root, fx.OrigDir, new[] { fx.TranDir });

        fx.ChangeFile(fx.BothSidesRels[0]);

        // Inject a fault into incremental-only code (the seam fires only when
        // allowIncremental is true) — S5 single fallback: the build must retry ONCE as
        // a full rebuild inside the same gate acquisition and succeed.
        svc.TestOnlyIncrementalFault = () => throw new InvalidOperationException("injected incremental-path fault");
        try
        {
            await svc.BuildOrUpdateAsync(fx.Root, fx.OrigDir, new[] { fx.TranDir }, forceRebuild: false);
        }
        finally
        {
            svc.TestOnlyIncrementalFault = null;
        }

        Assert.Equal(1, svc.LastBuildFallbackCount);
        // The fallback ran as a FULL rebuild: every XML file was read.
        Assert.Equal(fx.TotalFileCount, svc.LastBuildXmlReadCount);
        AssertNoFamilyTmpFiles(fx.Root);

        var fallbackFamily = ArtifactFamilyAssert.SnapshotFamily(fx.Root);
        await ArtifactFamilyAssert.AssertFamilyStampsAsync(fx.Root);

        // Equivalent to a plain full rebuild of the same corpus state.
        svc.InvalidateIndexCaches();
        await svc.BuildAsync(fx.Root, fx.OrigDir, new[] { fx.TranDir });
        Assert.Equal(0, svc.LastBuildFallbackCount); // forceRebuild never uses the fallback
        var full = ArtifactFamilyAssert.SnapshotFamily(fx.Root);
        ArtifactFamilyAssert.AssertEquivalent(fallbackFamily, full);
    }

    // ============ (8) skip-read proof: O(delta) XML reads ============

    [Fact]
    public async Task Incremental_OneFileChanged_ReadsExactlyOneXmlFile()
    {
        using var fx = new IndexFixtureCorpus();
        var svc = new SearchIndexService();

        await svc.BuildAsync(fx.Root, fx.OrigDir, new[] { fx.TranDir });
        // The full build read every file.
        Assert.Equal(fx.TotalFileCount, svc.LastBuildXmlReadCount);

        // ChangeFile rewrites exactly ONE file (the orig side of a both-sides rel).
        fx.ChangeFile(fx.BothSidesRels[3]);

        await svc.BuildOrUpdateAsync(fx.Root, fx.OrigDir, new[] { fx.TranDir }, forceRebuild: false);

        // Skip-read: only the changed entry's XML was read; every other entry was
        // sourced from the old text.bin block.
        Assert.Equal(1, svc.LastBuildXmlReadCount);
        Assert.Equal(0, svc.LastBuildFallbackCount);
        AssertNoFamilyTmpFiles(fx.Root);
    }

    // ============ (9) InputHash proof: single pass equals cache:null computation ============

    [Fact]
    public async Task Incremental_ManifestInputHash_EqualsUncachedComputation()
    {
        using var fx = new IndexFixtureCorpus();
        var svc = new SearchIndexService();
        await svc.BuildAsync(fx.Root, fx.OrigDir, new[] { fx.TranDir });

        fx.AddFileMidCorpus();
        fx.ChangeFile(fx.BothSidesRels[1]);

        await svc.BuildOrUpdateAsync(fx.Root, fx.OrigDir, new[] { fx.TranDir }, forceRebuild: false);
        var snap = ArtifactFamilyAssert.SnapshotFamily(fx.Root);

        // The InputHash written by the incremental build (computed from the Phase-1
        // hash cache with zero extra corpus reads) must be value-identical to a
        // from-scratch, cache-less computation over the same corpus.
        var uncached = await SearchIndexService.ComputeInputHashAsync(
            fx.OrigDir, new[] { fx.TranDir }, cache: null, writeBack: null, CancellationToken.None);

        Assert.False(string.IsNullOrEmpty(snap.MainManifest.InputHash));
        Assert.Equal(uncached, snap.MainManifest.InputHash);
    }
}
