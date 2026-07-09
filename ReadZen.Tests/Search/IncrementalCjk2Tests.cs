using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using ReadZen.App.Models;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Search;

/// <summary>
/// INC-5A: cjk2 manifest built by TRANSPOSING the Phase-1 per-entry gram sets
/// (no full search.text.bin re-scan). These tests prove the transpose reproduces
/// the old scan's output exactly:
///   (a) full-vs-incremental cjk2 manifests are byte-equal under canonical
///       serialization (IndexStamp nulled, BuiltUtc zeroed, same WriteIndented=true
///       options) — gram-string production identical, escaping included;
///   (b) renumbering backstop: after add+remove, a gram unique to a mid-corpus
///       shifted file maps to that entry's NEW positional Id;
///   (c) an empty-body entry contributes no grams but is still counted in EntryCount;
///   (d) Latin bigrams from an English translation side are present (spot check;
///       full parity is proven by (a));
///   (e) surrogate content (CJK Ext-B) survives Normalize, round-trips through
///       pack/unpack, and serializes identically on both paths;
///   (f) crash-window replay over the transpose path: a stale cjk2 sibling left
///       behind next to a NEWER main manifest is refused by the IndexStamp gate.
/// </summary>
[Trait("Domain", "SearchSprint")]
public class IncrementalCjk2Tests
{
    // ============ (a) canonical byte-equal serialization, full vs incremental ============

    [Fact]
    public async Task Cjk2_AddAndChange_CanonicalSerialization_ByteEqual_FullVsIncremental()
    {
        using var fx = new IndexFixtureCorpus();
        var svc = new SearchIndexService();
        await svc.BuildAsync(fx.Root, fx.OrigDir, new[] { fx.TranDir });

        fx.AddFileMidCorpus();
        fx.ChangeFile(fx.BothSidesRels[1]);

        await svc.BuildOrUpdateAsync(fx.Root, fx.OrigDir, new[] { fx.TranDir }, forceRebuild: false);
        var incremental = ArtifactFamilyAssert.SnapshotFamily(fx.Root);

        // Comparison full rebuild of the SAME corpus state (no file touched between).
        svc.InvalidateIndexCaches();
        await svc.BuildAsync(fx.Root, fx.OrigDir, new[] { fx.TranDir });
        var full = ArtifactFamilyAssert.SnapshotFamily(fx.Root);

        Assert.Equal(
            Cjk2Canonical.Serialize(full.Cjk2Manifest),
            Cjk2Canonical.Serialize(incremental.Cjk2Manifest));
    }

    // ============ (b) renumbering backstop: mid-corpus shift → NEW Id ============

    [Fact]
    public async Task Cjk2_AddAndRemove_GramUniqueToShiftedFile_MapsToItsNewId()
    {
        using var fx = new IndexFixtureCorpus();
        var svc = new SearchIndexService();
        await svc.BuildAsync(fx.Root, fx.OrigDir, new[] { fx.TranDir });
        var before = ArtifactFamilyAssert.SnapshotFamily(fx.Root);

        // Shifted victim: the SECOND orig-only rel (T/T48/p0080), captured before the
        // mutations. The add (both sides of T/T01/c0015) inserts 2 entries before it
        // and the removal of the FIRST orig-only rel (T/T48/n0070) deletes 1, so its
        // positional Id shifts by a guaranteed non-zero net +1.
        var shifted = fx.OrigOnlyRels[1];
        var added = fx.AddFileMidCorpus();
        var removed = fx.RemoveFile(fx.OrigOnlyRels[0]);
        Assert.True(StringComparer.OrdinalIgnoreCase.Compare(shifted, added) > 0);
        Assert.True(StringComparer.OrdinalIgnoreCase.Compare(shifted, removed) > 0);

        await svc.BuildOrUpdateAsync(fx.Root, fx.OrigDir, new[] { fx.TranDir }, forceRebuild: false);
        var snap = ArtifactFamilyAssert.SnapshotFamily(fx.Root);

        var newEntry = snap.MainManifest.Entries.Single(e =>
            string.Equals(e.RelPath, shifted, StringComparison.OrdinalIgnoreCase) &&
            e.Side == SearchSide.Original);

        // The cjk2 posting for the shifted file's unique gram carries the NEW Id.
        var gram = fx.UniqueOrigGram(shifted);
        var posting = snap.Cjk2Manifest.Postings.Single(p => string.Equals(p.Gram, gram, StringComparison.Ordinal));
        var id = Assert.Single(posting.EntryIds);
        Assert.Equal(newEntry.Id, id);

        // Sanity: the Id really shifted relative to the pre-mutation build — the
        // assertion above is only meaningful if renumbering actually happened.
        var oldEntry = before.MainManifest.Entries.Single(e =>
            string.Equals(e.RelPath, shifted, StringComparison.OrdinalIgnoreCase) &&
            e.Side == SearchSide.Original);
        Assert.NotEqual(oldEntry.Id, newEntry.Id);
    }

    // ============ (c) empty-body entry: zero grams, still counted ============

    [Fact]
    public async Task Cjk2_EmptyBodyEntry_ContributesNoGrams_ButCountsInEntryCount()
    {
        using var fx = new IndexFixtureCorpus();
        var svc = new SearchIndexService();
        await svc.BuildAsync(fx.Root, fx.OrigDir, new[] { fx.TranDir });

        // Orig-only add with an EMPTY body, sorted mid-corpus (e0025 between d0020
        // and f0030). Its searchable text is empty/whitespace-only, so it must
        // contribute zero cjk2 grams while still occupying a manifest slot.
        const string emptyRel = "T/T48/e0025.xml";
        var emptyPath = fx.OrigPath(emptyRel);
        Directory.CreateDirectory(Path.GetDirectoryName(emptyPath)!);
        File.WriteAllText(emptyPath,
            "<TEI xmlns=\"http://www.tei-c.org/ns/1.0\"><text><body><p></p></body></text></TEI>");

        await svc.BuildOrUpdateAsync(fx.Root, fx.OrigDir, new[] { fx.TranDir }, forceRebuild: false);
        var snap = ArtifactFamilyAssert.SnapshotFamily(fx.Root);

        var emptyEntry = snap.MainManifest.Entries.Single(e =>
            string.Equals(e.RelPath, emptyRel, StringComparison.OrdinalIgnoreCase) &&
            e.Side == SearchSide.Original);

        // Counted in EntryCount (skipped/empty entries are NOT subtracted)...
        Assert.Equal(snap.MainManifest.Entries.Count, snap.Cjk2Manifest.EntryCount);
        // ...but present in no posting.
        Assert.DoesNotContain(snap.Cjk2Manifest.Postings, p => p.EntryIds.Contains(emptyEntry.Id));

        // And the full rebuild of the same state agrees byte-for-byte.
        svc.InvalidateIndexCaches();
        await svc.BuildAsync(fx.Root, fx.OrigDir, new[] { fx.TranDir });
        var full = ArtifactFamilyAssert.SnapshotFamily(fx.Root);
        Assert.Equal(
            Cjk2Canonical.Serialize(full.Cjk2Manifest),
            Cjk2Canonical.Serialize(snap.Cjk2Manifest));
    }

    // ============ (d) English translation side: Latin bigrams present ============

    [Fact]
    public async Task Cjk2_EnglishTranslationSide_LatinBigramsPresent()
    {
        using var fx = new IndexFixtureCorpus();
        var svc = new SearchIndexService();
        await svc.BuildAsync(fx.Root, fx.OrigDir, new[] { fx.TranDir });

        // Change one file so the next build takes the incremental transpose path.
        fx.ChangeFile(fx.BothSidesRels[0]);
        await svc.BuildOrUpdateAsync(fx.Root, fx.OrigDir, new[] { fx.TranDir }, forceRebuild: false);
        var snap = ArtifactFamilyAssert.SnapshotFamily(fx.Root);

        // Every fixture tran body contains "gateless barrier" — "rr" occurs ONLY in
        // "barrier", so the posting exists and lists exactly the tran-side entries.
        var posting = snap.Cjk2Manifest.Postings.SingleOrDefault(p =>
            string.Equals(p.Gram, "rr", StringComparison.Ordinal));
        Assert.True(posting != null, "Latin bigram 'rr' (from 'barrier') missing from cjk2 postings");

        var tranIds = snap.MainManifest.Entries
            .Where(e => e.Side == SearchSide.Translated)
            .Select(e => e.Id)
            .OrderBy(i => i)
            .ToList();
        Assert.Equal(tranIds, posting!.EntryIds.OrderBy(i => i).ToList());
    }

    // ============ (e) CJK Extension B surrogate content ============

    [Fact]
    public void GramSetCodec_PackUnpack_RoundTrips_SurrogateHalves()
    {
        // Valid Ext-B pair U+20000 and an ill-formed reversed pair both round-trip.
        Assert.Equal("𠀀", GramSetCodec.UnpackGram(GramSetCodec.PackGram('\uD840', '\uDC00')));
        Assert.Equal("\uDC00\uD840", GramSetCodec.UnpackGram(GramSetCodec.PackGram('\uDC00', '\uD840')));
    }

    [Fact]
    public async Task Cjk2_ExtB_SurrogateContent_ReproducesOldScanBehavior_AndSerializesIdentically()
    {
        using var fx = new IndexFixtureCorpus();
        var svc = new SearchIndexService();
        await svc.BuildAsync(fx.Root, fx.OrigDir, new[] { fx.TranDir });

        // Orig-only add carrying CJK Ext-B (U+20000 U+20001): code units
        // D840 DC00 D840 DC01. CjkMatchNormalizer strips EVERY low surrogate half
        // (its IsStrippedForMatch drops surrogates >= U+DB00, and all lows are
        // U+DC00-DFFF), so compact = "\uD840\uD840" and the entry's ONLY cjk2 gram
        // is the ill-formed high-half pair (D840,D840) — exactly what the old
        // Substring-over-compact scan produced. The transpose must reproduce that,
        // including the serializer's escaping/replacement of the ill-formed pair.
        const string extBRel = "T/T01/e0025.xml";
        var extBPath = fx.OrigPath(extBRel);
        Directory.CreateDirectory(Path.GetDirectoryName(extBPath)!);
        File.WriteAllText(extBPath,
            "<TEI xmlns=\"http://www.tei-c.org/ns/1.0\"><text><body><p>\U00020000\U00020001</p></body></text></TEI>");

        await svc.BuildOrUpdateAsync(fx.Root, fx.OrigDir, new[] { fx.TranDir }, forceRebuild: false);
        var incremental = ArtifactFamilyAssert.SnapshotFamily(fx.Root);

        var extBEntry = incremental.MainManifest.Entries.Single(e =>
            string.Equals(e.RelPath, extBRel, StringComparison.OrdinalIgnoreCase) &&
            e.Side == SearchSide.Original);

        // Exactly one posting carries the Ext-B entry (its single high-half gram)...
        var posting = Assert.Single(incremental.Cjk2Manifest.Postings, p => p.EntryIds.Contains(extBEntry.Id));
        var id = Assert.Single(posting.EntryIds);
        Assert.Equal(extBEntry.Id, id);

        // ...and its Gram is the JSON round-trip of "\uD840\uD840": in-memory the
        // transpose produced the raw high-half pair (pack/unpack preserves it — see
        // GramSetCodec_PackUnpack_RoundTrips_SurrogateHalves); on disk it carries
        // whatever escaping/replacement System.Text.Json applies to the ill-formed
        // pair — computed here with the same serializer rather than hardcoded.
        var expectedGram = JsonSerializer.Deserialize<string>(JsonSerializer.Serialize("\uD840\uD840"));
        Assert.Equal(expectedGram, posting.Gram);

        // Full rebuild of the same state: identical canonical JSON — this covers the
        // ill-formed surrogate-half grams too (whatever escaping/replacement the
        // serializer applies, it applies identically on both paths).
        svc.InvalidateIndexCaches();
        await svc.BuildAsync(fx.Root, fx.OrigDir, new[] { fx.TranDir });
        var full = ArtifactFamilyAssert.SnapshotFamily(fx.Root);
        Assert.Equal(
            Cjk2Canonical.Serialize(full.Cjk2Manifest),
            Cjk2Canonical.Serialize(incremental.Cjk2Manifest));
    }

    // ============ (f) crash-window replay over the transpose path ============

    [Fact]
    public async Task Cjk2_CrashWindow_StaleSiblingNextToIncrementalManifest_RefusedByStamp()
    {
        using var fx = new IndexFixtureCorpus();
        var svc = new SearchIndexService();
        await svc.BuildAsync(fx.Root, fx.OrigDir, new[] { fx.TranDir });

        // Keep the v1 cjk2 manifest aside — the file a crash between the main
        // manifest commit and the cjk2 save would leave behind.
        var cjk2Path = Path.Combine(fx.Root, "search.cjk2.manifest.json");
        var staleJson = File.ReadAllText(cjk2Path);

        // Mutate + incremental (transpose) build → new main manifest, new stamp.
        fx.AddFileMidCorpus();
        await svc.BuildOrUpdateAsync(fx.Root, fx.OrigDir, new[] { fx.TranDir }, forceRebuild: false);

        // Simulate the crash: the OLD cjk2 sibling sits next to the NEW manifest.
        File.WriteAllText(cjk2Path, staleJson);
        svc.InvalidateIndexCaches();

        var snap = ArtifactFamilyAssert.SnapshotFamily(fx.Root);
        var staleCjk2 = await svc.TryLoadCjk2ManifestAsync(fx.Root);
        Assert.NotNull(staleCjk2); // structurally loadable...
        Assert.False(SearchIndexService.IsCjk2Usable(staleCjk2!, snap.MainManifest),
            "stale cjk2 sibling must be refused by the IndexStamp usability gate");
    }
}

/// <summary>
/// Canonical serialization for the INC-5A byte-equal cjk2 comparison: per-build
/// values (IndexStamp, BuiltUtc) neutralized, then serialized with the same
/// WriteIndented=true options the app uses. Two manifests whose canonical strings
/// are equal produced identical gram strings (escaping included), postings order,
/// EntryIds and counts.
/// </summary>
internal static class Cjk2Canonical
{
    private static readonly JsonSerializerOptions Opts = new() { WriteIndented = true };

    public static string Serialize(SearchCjkBigramManifest m)
    {
        // Shallow clone so snapshots are never mutated; Postings are shared read-only.
        var clone = new SearchCjkBigramManifest
        {
            Version = m.Version,
            RootPath = m.RootPath,
            BuiltUtc = default,
            BuildGuid = m.BuildGuid,
            GramSize = m.GramSize,
            EntryCount = m.EntryCount,
            IndexStamp = null,
            Postings = m.Postings,
        };
        return JsonSerializer.Serialize(clone, Opts);
    }
}
