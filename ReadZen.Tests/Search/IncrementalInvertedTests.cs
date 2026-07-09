using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ReadZen.App.Models;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Search;

/// <summary>
/// INC-4A: the gramsets sidecar (6th artifact) wired into the build — the inverted
/// index is built from cached + fresh UNCUT gram sets (transpose path), the high-DF
/// cutoff is applied at save time inside <c>InvertedSearchIndex.Build</c>, and the
/// sidecar is persisted best-effort after the family commit.
///
/// Proven here:
///   - DF resurrection: a term cut by a previous build (&gt;80% doc frequency) comes
///     BACK with correct postings when the corpus shrinks below the threshold —
///     possible only because cached gram sets are stored uncut;
///   - cache-lost / corrupted sidecar: the build stays correct and equivalent, never
///     crashes, never falls back to a full rebuild, and re-creates the sidecar;
///   - staleness safety: the sidecar is never trusted across a content change;
///   - warm-path efficiency: an N-entry delta computes exactly N gram sets
///     (<c>LastBuildGramComputeCount</c>);
///   - the sidecar manifest records the family IndexStamp of the build that saved it.
/// </summary>
[Trait("Domain", "SearchSprint")]
public class IncrementalInvertedTests
{
    private const string InvertedBinName = "search.inverted.bin";

    // =====================================================================
    // Shared flow (mirrors IncrementalEquivalenceTests): mutate → incremental
    // → snapshot+backstops → full rebuild → family equivalence.
    // =====================================================================

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

        // Comparison full rebuild of the same corpus state (no file touched between
        // the builds — LastWriteUtcTicks equality depends on it).
        svc.InvalidateIndexCaches();
        await svc.BuildAsync(fx.Root, fx.OrigDir, new[] { fx.TranDir });
        var full = ArtifactFamilyAssert.SnapshotFamily(fx.Root);
        await ArtifactFamilyAssert.AssertFamilyStampsAsync(fx.Root);

        ArtifactFamilyAssert.AssertEquivalent(incremental, full);

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

    private static async Task<InvertedSearchIndex> LoadInvertedAsync(string root, string? stamp)
    {
        Assert.False(string.IsNullOrEmpty(stamp));
        var inv = new InvertedSearchIndex();
        Assert.True(await inv.TryLoadAsync(Path.Combine(root, InvertedBinName), stamp!),
            "inverted index must load against the build's IndexStamp");
        return inv;
    }

    /// <summary>Reads the gramsets sidecar manifest's IndexStamp via JsonDocument (no DTO coupling).</summary>
    private static string? ReadGramSetsSidecarStamp(string root)
    {
        var path = Path.Combine(root, GramSetsStore.ManifestFileName);
        Assert.True(File.Exists(path), $"gramsets sidecar manifest missing: {path}");
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        return doc.RootElement.TryGetProperty("IndexStamp", out var el) && el.ValueKind == JsonValueKind.String
            ? el.GetString()
            : null;
    }

    /// <summary>
    /// Writes an extra ORIGINAL-side file that deliberately does NOT contain the
    /// fixture's common gram (無門), so the corpus-wide DF ratio of that gram can drop
    /// below 1.0. Body chars avoid every reserved fixture range (U+5100 orig uniques,
    /// U+5800 tran uniques, U+6100 change markers).
    /// </summary>
    private static void WriteNoCommonGramOrig(IndexFixtureCorpus fx, string rel)
    {
        var path = fx.OrigPath(rel);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path,
            "<TEI xmlns=\"http://www.tei-c.org/ns/1.0\"><text><body><p>" +
            "如是我聞一時佛在舍衛國祇樹給孤獨園" +
            "</p></body></text></TEI>");
    }

    // ============ (a) DF-cutoff resurrection ============

    [Fact]
    public async Task DfCutoff_TermCutAtBaseline_ResurrectsAfterRemovals_EquivalentToFull()
    {
        using var fx = new IndexFixtureCorpus();

        // Baseline arithmetic: the 20 fixture winner docs ALL contain 無門. Adding 4
        // orig-only docs WITHOUT it gives 24 docs, 20 of them with the gram:
        // maxDf = (int)(24 * 0.8) = 19, and 20 > 19 → the term is CUT at baseline.
        for (int k = 0; k < 4; k++)
            WriteNoCommonGramOrig(fx, $"T/T48/z0{k}00.xml");

        var svc = new SearchIndexService();
        await svc.BuildAsync(fx.Root, fx.OrigDir, new[] { fx.TranDir });

        var baseline = ArtifactFamilyAssert.SnapshotFamily(fx.Root);
        var baselineInv = await LoadInvertedAsync(fx.Root, baseline.MainManifest.IndexStamp);
        var cutHits = baselineInv.Search(IndexFixtureCorpus.CommonGram);
        Assert.NotNull(cutHits);
        Assert.Empty(cutHits!); // absent term ⇒ zero results: the cutoff dropped it

        // Delta: REMOVE 4 rels that contain the gram → 20 docs, 16 with the gram:
        // maxDf = (int)(20 * 0.8) = 16, and 16 <= 16 → the term must RESURRECT.
        // Resurrection is only possible because the sidecar caches UNCUT gram sets.
        for (int k = 0; k < 4; k++)
            fx.RemoveFile(fx.BothSidesRels[0]);

        var expectedRels = fx.AllRels; // the 16 remaining fixture rels (extras lack the gram)

        await RunIncrementalVsFullAsync(fx, svc, async snap =>
        {
            var inv = await LoadInvertedAsync(fx.Root, snap.MainManifest.IndexStamp);
            var hits = inv.Search(IndexFixtureCorpus.CommonGram);
            Assert.NotNull(hits);
            Assert.Equal(expectedRels.Count, hits!.Length);

            var hitRels = hits.Select(h => inv.GetRelPath(h)!).ToHashSet(StringComparer.OrdinalIgnoreCase);
            foreach (var rel in expectedRels)
                Assert.Contains(rel, hitRels);
        });
    }

    // ============ (b) cache lost: sidecar deleted ============

    [Fact]
    public async Task SidecarDeleted_IncrementalStillEquivalent_SidecarRecreatedWithBuildStamp()
    {
        using var fx = new IndexFixtureCorpus();
        var svc = new SearchIndexService();
        await svc.BuildAsync(fx.Root, fx.OrigDir, new[] { fx.TranDir });

        // The full build persisted the sidecar (that is what warms it).
        Assert.True(File.Exists(Path.Combine(fx.Root, GramSetsStore.ManifestFileName)));
        Assert.True(File.Exists(Path.Combine(fx.Root, GramSetsStore.BinFileName)));

        File.Delete(Path.Combine(fx.Root, GramSetsStore.ManifestFileName));
        File.Delete(Path.Combine(fx.Root, GramSetsStore.BinFileName));

        fx.ChangeFile(fx.BothSidesRels[2]);

        await RunIncrementalVsFullAsync(fx, svc, snap =>
        {
            // Losing the cache only costs speed: every entry recomputed its sets…
            Assert.Equal(fx.TotalFileCount, svc.LastBuildGramComputeCount);
            // …but never from XML — unchanged entries recompute from old text.bin blocks.
            Assert.Equal(1, svc.LastBuildXmlReadCount);
            Assert.Equal(0, svc.LastBuildFallbackCount);

            // Sidecar re-created by the incremental build, stamped with ITS family stamp.
            Assert.True(File.Exists(Path.Combine(fx.Root, GramSetsStore.BinFileName)),
                "gramsets bin must be re-created after the build");
            Assert.Equal(snap.MainManifest.IndexStamp, ReadGramSetsSidecarStamp(fx.Root));
            return Task.CompletedTask;
        });
    }

    // ============ (c) corrupted sidecar bin ============

    [Fact]
    public async Task SidecarBinTruncated_TreatedAsAbsent_BuildEquivalent_NoFallback_NoTmpFiles()
    {
        using var fx = new IndexFixtureCorpus();
        var svc = new SearchIndexService();
        await svc.BuildAsync(fx.Root, fx.OrigDir, new[] { fx.TranDir });

        // Truncate the bin to 10 bytes: magic survives but every entry's bounds
        // validation fails → TryLoadAsync returns null → whole sidecar treated absent.
        using (var trunc = new FileStream(Path.Combine(fx.Root, GramSetsStore.BinFileName),
                   FileMode.Open, FileAccess.Write, FileShare.None))
        {
            trunc.SetLength(10);
        }

        fx.ChangeFile(fx.BothSidesRels[1]);

        await RunIncrementalVsFullAsync(fx, svc, snap =>
        {
            // Sidecar corruption must never trigger the full-rebuild fallback…
            Assert.Equal(0, svc.LastBuildFallbackCount);
            // …and never re-read unchanged XML; it only costs recompute of the sets.
            Assert.Equal(fx.TotalFileCount, svc.LastBuildGramComputeCount);
            Assert.Equal(1, svc.LastBuildXmlReadCount);
            return Task.CompletedTask;
        });
    }

    // ============ (d) staleness safety: content change is never served from cache ============

    [Fact]
    public async Task ContentChange_RemovedGramStopsHitting_NewGramHits()
    {
        using var fx = new IndexFixtureCorpus();
        var svc = new SearchIndexService();
        await svc.BuildAsync(fx.Root, fx.OrigDir, new[] { fx.TranDir });

        // Rewrite one orig file REPLACING its unique gram with a new pair (same byte
        // length — the classification catches it via mtime; the sidecar must not be
        // trusted across the change).
        var rel = fx.BothSidesRels[4];
        var oldGram = fx.UniqueOrigGram(rel);
        const string newGram = "洀洁"; // U+6D00/U+6D01 — a range no fixture text uses
        var path = fx.OrigPath(rel);
        File.WriteAllText(path, File.ReadAllText(path).Replace(oldGram, newGram, StringComparison.Ordinal));

        await RunIncrementalVsFullAsync(fx, svc, async snap =>
        {
            var inv = await LoadInvertedAsync(fx.Root, snap.MainManifest.IndexStamp);

            // Content REMOVED from the file no longer hits it.
            var oldHits = inv.Search(oldGram);
            Assert.NotNull(oldHits);
            Assert.Empty(oldHits!);

            // NEW content does hit it.
            var newHits = inv.Search(newGram);
            Assert.NotNull(newHits);
            var hit = Assert.Single(newHits!);
            Assert.True(string.Equals(rel, inv.GetRelPath(hit), StringComparison.OrdinalIgnoreCase),
                $"gram '{newGram}' resolved to '{inv.GetRelPath(hit)}', expected '{rel}'");
        });
    }

    // ============ (e) warm-path efficiency ============

    [Fact]
    public async Task WarmSidecar_OneFileChanged_ComputesExactlyOneGramSet()
    {
        using var fx = new IndexFixtureCorpus();
        var svc = new SearchIndexService();

        await svc.BuildAsync(fx.Root, fx.OrigDir, new[] { fx.TranDir });
        // The full build computed every entry's sets (and warmed the sidecar doing so).
        Assert.Equal(fx.TotalFileCount, svc.LastBuildGramComputeCount);

        // ChangeFile rewrites exactly ONE file (the orig side of a both-sides rel).
        fx.ChangeFile(fx.BothSidesRels[3]);

        await svc.BuildOrUpdateAsync(fx.Root, fx.OrigDir, new[] { fx.TranDir }, forceRebuild: false);

        // Exactly the changed entry computed; every other entry was sidecar-read.
        Assert.Equal(1, svc.LastBuildGramComputeCount);
        Assert.Equal(1, svc.LastBuildXmlReadCount);
        Assert.Equal(0, svc.LastBuildFallbackCount);
        AssertNoFamilyTmpFiles(fx.Root);
    }

    // ============ (f) sidecar save failure never fails the build ============

    [Fact]
    public async Task SidecarSaveFailure_BuildStillCommitsFamily_Equivalent_NoTmpFiles()
    {
        using var fx = new IndexFixtureCorpus();
        var svc = new SearchIndexService();
        await svc.BuildAsync(fx.Root, fx.OrigDir, new[] { fx.TranDir });

        var baselineSidecarStamp = ReadGramSetsSidecarStamp(fx.Root);
        Assert.False(string.IsNullOrEmpty(baselineSidecarStamp));

        fx.ChangeFile(fx.BothSidesRels[2]);

        // Block the sidecar save: replace search.gramsets.bin with a DIRECTORY of the
        // same name. GramSetsStore.SaveAsync writes its bin tmp fine, but the
        // File.Move(tmp, final, overwrite: true) cannot replace a directory — SaveAsync
        // deletes its tmps and rethrows, and the BUILD must swallow that (the sidecar
        // is an accelerator; losing it may never cost correctness or fail the build).
        // TryLoadAsync also treats the sidecar as absent during the run (File.Exists is
        // false for a directory), so every gram set recomputes — the documented cost.
        var binPath = Path.Combine(fx.Root, GramSetsStore.BinFileName);
        File.Delete(binPath);
        Directory.CreateDirectory(binPath);
        try
        {
            await svc.BuildOrUpdateAsync(fx.Root, fx.OrigDir, new[] { fx.TranDir }, forceRebuild: false);

            // The build committed the complete family despite the sidecar save failure:
            // still the incremental path (no fallback, one XML read), no stray tmps.
            Assert.Equal(0, svc.LastBuildFallbackCount);
            Assert.Equal(1, svc.LastBuildXmlReadCount);
            Assert.Equal(fx.TotalFileCount, svc.LastBuildGramComputeCount); // cold sidecar
            AssertNoFamilyTmpFiles(fx.Root);

            var incremental = ArtifactFamilyAssert.SnapshotFamily(fx.Root);
            await ArtifactFamilyAssert.AssertFamilyStampsAsync(fx.Root);

            // The sidecar save really DID fail: the manifest on disk is still the
            // baseline build's (old stamp), not re-stamped by the incremental build.
            Assert.Equal(baselineSidecarStamp, ReadGramSetsSidecarStamp(fx.Root));
            Assert.False(string.Equals(incremental.MainManifest.IndexStamp, baselineSidecarStamp, StringComparison.Ordinal),
                "the incremental build minted a fresh family stamp distinct from the stale sidecar's");

            // The committed family works: the changed file's ORIGINAL side still
            // resolves through a freshly loaded inverted index.
            var inv = await LoadInvertedAsync(fx.Root, incremental.MainManifest.IndexStamp);
            var hits = inv.Search(fx.UniqueOrigGram(fx.BothSidesRels[2]));
            Assert.NotNull(hits);
            var hit = Assert.Single(hits!);
            Assert.True(string.Equals(fx.BothSidesRels[2], inv.GetRelPath(hit), StringComparison.OrdinalIgnoreCase),
                $"changed file's unique gram resolved to '{inv.GetRelPath(hit)}', expected '{fx.BothSidesRels[2]}'");

            // Unblock and compare against a from-scratch full rebuild of the SAME
            // corpus state (no corpus file touched since the incremental run).
            Directory.Delete(binPath);
            svc.InvalidateIndexCaches();
            await svc.BuildAsync(fx.Root, fx.OrigDir, new[] { fx.TranDir });
            var full = ArtifactFamilyAssert.SnapshotFamily(fx.Root);
            ArtifactFamilyAssert.AssertEquivalent(incremental, full);
        }
        finally
        {
            // Never leave the blocking directory behind for the fixture's Dispose.
            if (Directory.Exists(binPath))
                try { Directory.Delete(binPath); } catch { }
        }
    }

    // ============ (g) sidecar stamp == family stamp ============

    [Fact]
    public async Task SidecarManifest_CarriesTheFamilyIndexStamp_OnFullAndIncrementalBuilds()
    {
        using var fx = new IndexFixtureCorpus();
        var svc = new SearchIndexService();

        await svc.BuildAsync(fx.Root, fx.OrigDir, new[] { fx.TranDir });
        var fullSnap = ArtifactFamilyAssert.SnapshotFamily(fx.Root);
        var fullSidecarStamp = ReadGramSetsSidecarStamp(fx.Root);
        Assert.False(string.IsNullOrEmpty(fullSidecarStamp));
        Assert.Equal(fullSnap.MainManifest.IndexStamp, fullSidecarStamp);

        fx.ChangeFile(fx.BothSidesRels[0]);
        await svc.BuildOrUpdateAsync(fx.Root, fx.OrigDir, new[] { fx.TranDir }, forceRebuild: false);

        var incSnap = ArtifactFamilyAssert.SnapshotFamily(fx.Root);
        var incSidecarStamp = ReadGramSetsSidecarStamp(fx.Root);
        Assert.Equal(incSnap.MainManifest.IndexStamp, incSidecarStamp);
        Assert.False(string.Equals(fullSidecarStamp, incSidecarStamp, StringComparison.Ordinal),
            "each build must re-stamp the sidecar with its own family stamp");
    }
}
