using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using ReadZen.App.Models;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Search;

/// <summary>
/// Store-level contract tests for the gramsets sidecar, focused on the torn-PAIR
/// window: a crash between the bin File.Move and the manifest File.Move leaves the
/// OLD manifest next to the NEW bin. Both files are individually well-formed, magic/
/// Version/BuildGuid/RootPath all match, and the old manifest's offsets typically
/// stay in bounds because the new bin is LARGER — so only the per-save pairing token
/// (written into both files) can refuse the pair. Serving it would silently return
/// OTHER entries' shifted gram data and poison every later incremental build.
/// </summary>
[Trait("Domain", "SearchSprint")]
public class GramSetsStoreTests
{
    private static List<(GramSetsEntry meta, uint[] invGrams)> MakeEntries(
        params (string rel, SearchSide side, uint[] inv)[] rows)
    {
        var list = new List<(GramSetsEntry, uint[])>(rows.Length);
        foreach (var (rel, side, inv) in rows)
        {
            list.Add((new GramSetsEntry
            {
                RelPath = rel,
                Side = side,
                ContentHash = "hash-" + rel + "-" + side, // constant across saves: per-entry identity HITS
                LastWriteUtcTicks = 1234567890L,
                LengthBytes = 42,
            }, inv));
        }
        return list;
    }

    [Fact]
    public async Task TornPair_OldManifestNextToNewBin_TreatedAsAbsent()
    {
        string root = Directory.CreateTempSubdirectory("gramsets-torn-").FullName;
        try
        {
            // ── Save A: small arrays. ──
            var entriesA = MakeEntries(
                ("T/T01/a.xml", SearchSide.Original, new uint[] { 1, 2, 3 }));
            await GramSetsStore.SaveAsync(root, "stamp-A", entriesA, default);

            var loadedA = await GramSetsStore.TryLoadAsync(root, default);
            Assert.NotNull(loadedA); // intact pair A loads
            Assert.True(loadedA!.TryGet("T/T01/a.xml", SearchSide.Original, out var entryA));
            Assert.Equal(new uint[] { 1, 2, 3 }, loadedA.ReadInvGrams(entryA));

            // Stash A's manifest before build B replaces it.
            string manifestPath = GramSetsStore.GetManifestPath(root);
            string stash = manifestPath + ".stash";
            File.Copy(manifestPath, stash);

            // ── Save B: same key (identity fields unchanged ⇒ every per-entry check
            // would HIT), but LARGER arrays plus an extra entry, so bin_B is strictly
            // longer than bin_A and manifest_A's offsets all stay in bounds. ──
            var entriesB = MakeEntries(
                ("T/T01/a.xml", SearchSide.Original, new uint[] { 100, 101, 102, 103, 104 }),
                ("T/T02/b.xml", SearchSide.Translated, new uint[] { 200, 201 }));
            await GramSetsStore.SaveAsync(root, "stamp-B", entriesB, default);
            Assert.NotNull(await GramSetsStore.TryLoadAsync(root, default)); // intact pair B loads

            long binALength = 4 + 16 + 4L * 3;
            Assert.True(new FileInfo(GramSetsStore.GetBinPath(root)).Length > binALength,
                "test premise: bin_B must be larger than bin_A so manifest_A's bounds pass");

            // ── Simulate the crash window: bin_B was moved into place, the process
            // died before manifest_B — manifest_A + bin_B on disk. ──
            File.Copy(stash, manifestPath, overwrite: true);

            Assert.Null(await GramSetsStore.TryLoadAsync(root, default));
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { /* best effort */ }
        }
    }

    [Fact]
    public async Task IntactPair_ReloadsAcrossProcesses_IndexStampStaysInformational()
    {
        string root = Directory.CreateTempSubdirectory("gramsets-intact-").FullName;
        try
        {
            var entries = MakeEntries(
                ("T/T01/a.xml", SearchSide.Original, new uint[] { 7, 8 }));
            await GramSetsStore.SaveAsync(root, "some-old-family-stamp", entries, default);

            // An intact pair from ANY previous build loads — the pairing token binds
            // bin to manifest, it does not tie the sidecar to the current family stamp.
            var loaded = await GramSetsStore.TryLoadAsync(root, default);
            Assert.NotNull(loaded);
            Assert.True(loaded!.TryGet("T/T01/a.xml", SearchSide.Original, out var e));
            Assert.Equal(new uint[] { 7, 8 }, loaded.ReadInvGrams(e));
        }
        finally
        {
            try { Directory.Delete(root, recursive: true); } catch { /* best effort */ }
        }
    }
}
