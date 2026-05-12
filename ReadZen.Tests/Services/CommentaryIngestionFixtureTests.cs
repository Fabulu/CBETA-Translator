// CommentaryIngestionFixtureTests — locks in the shape of PR 4's
// commentary.json fixture (produced by eng/tools/build-faith-in-mind-commentary.ps1
// from the woodblocks witness-register). The fixture lives in the OpenZen
// sibling repo at xml-open/ce/faith-in-mind/commentary.json, NOT in the
// desktop repo. When that file is absent (CI runners without the OpenZen
// checkout, or before the sibling commit lands), the tests pass with a
// clear "OpenZen sibling commit pending" message logged via
// ITestOutputHelper — they DO NOT silently lose coverage and they DO NOT
// fail. When the sibling file IS present, every assertion runs.
//
// Sprint: RUN-20260512-1754-faith-in-mind-commentary-filter, PR 4.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using ReadZen.App.Models;
using Xunit;
using Xunit.Abstractions;

namespace ReadZen.Tests.Services;

[Trait("Domain", "FiM")]
public class CommentaryIngestionFixtureTests
{
    private readonly ITestOutputHelper _output;

    public CommentaryIngestionFixtureTests(ITestOutputHelper output)
    {
        _output = output;
    }

    // Candidate locations to search for the OpenZen sibling repo. The
    // first one that contains the FiM commentary.json wins.
    private static readonly string[] CandidateOpenZenRoots = new[]
    {
        @"C:\Programmieren\OpenZenTexts",
        @"C:\programmieren\OpenZenTexts",
        @"/c/Programmieren/OpenZenTexts",
        @"/mnt/c/Programmieren/OpenZenTexts",
    };

    private const string FixtureRelativePath = @"xml-open\ce\faith-in-mind\commentary.json";
    private const string FixtureRelativePathPosix = "xml-open/ce/faith-in-mind/commentary.json";

    private const string SkipReason =
        "OpenZen sibling commit pending: commentary.json not found in any " +
        "checkout location. Run eng/tools/build-faith-in-mind-commentary.ps1 " +
        "-OutputPath <OpenZenTexts>/xml-open/ce/faith-in-mind/commentary.json " +
        "and commit it to the OpenZen repo. Or set OPENZEN_FIM_COMMENTARY=<path> " +
        "to point at the file directly. Test PASSES without running assertions.";

    private static string? TryFindFixturePath()
    {
        // Honor environment override first (useful for CI agents and
        // non-default checkouts).
        var env = System.Environment.GetEnvironmentVariable("OPENZEN_FIM_COMMENTARY");
        if (!string.IsNullOrEmpty(env) && File.Exists(env)) return env;

        foreach (var root in CandidateOpenZenRoots)
        {
            foreach (var rel in new[] { FixtureRelativePath, FixtureRelativePathPosix })
            {
                var candidate = Path.Combine(root, rel);
                if (File.Exists(candidate)) return candidate;
            }
        }
        return null;
    }

    /// <summary>
    /// Returns the parsed fixture, or null if the file isn't present.
    /// When null is returned, the caller MUST early-return after logging
    /// the skip reason — the test passes without running assertions
    /// (xUnit 2.x lacks Assert.Skip; this is the documented soft-skip
    /// pattern).
    /// </summary>
    private CommentaryInfo? TryLoadFixtureOrLogSkip()
    {
        var path = TryFindFixturePath();
        if (path is null)
        {
            _output.WriteLine("[SKIP] " + SkipReason);
            return null;
        }
        _output.WriteLine($"Loaded fixture: {path}");
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<CommentaryInfo>(json);
    }

    [Fact]
    public void Fixture_DeserializesIntoCommentaryInfo_WithoutThrowing()
    {
        var info = TryLoadFixtureOrLogSkip();
        if (info is null) return; // soft-skip
        Assert.NotNull(info);
        Assert.NotNull(info.Entries);
    }

    [Fact]
    public void Fixture_Contains_Exactly_Seventeen_Entries()
    {
        var info = TryLoadFixtureOrLogSkip();
        if (info is null) return;
        Assert.NotNull(info.Entries);
        Assert.Equal(17, info.Entries!.Count);
    }

    [Fact]
    public void Fixture_All_Entries_Are_Tagged_Japanese()
    {
        var info = TryLoadFixtureOrLogSkip();
        if (info is null) return;
        Assert.NotNull(info.Entries);
        Assert.All(info.Entries!, e => Assert.Equal("ja", e.Language));
    }

    [Fact]
    public void Fixture_Ids_Are_C1_Through_C17_With_No_Gaps()
    {
        var info = TryLoadFixtureOrLogSkip();
        if (info is null) return;
        Assert.NotNull(info.Entries);
        var actualIds = info.Entries!.Select(e => e.CommentaryId ?? "").ToList();
        var expectedIds = Enumerable.Range(1, 17).Select(n => $"C{n}").ToList();
        // Order-independent: the fixture orders by C-ID, but the contract
        // is "all 17 IDs present, no duplicates, no gaps".
        Assert.Equal(
            expectedIds.OrderBy(s => s, System.StringComparer.Ordinal).ToList(),
            actualIds.OrderBy(s => s, System.StringComparer.Ordinal).ToList());
    }

    [Fact]
    public void Fixture_Every_Entry_Has_NonEmpty_Title_And_Source()
    {
        var info = TryLoadFixtureOrLogSkip();
        if (info is null) return;
        Assert.NotNull(info.Entries);
        foreach (var e in info.Entries!)
        {
            Assert.False(
                string.IsNullOrWhiteSpace(e.Title),
                $"entry {e.CommentaryId} has empty title");
            Assert.False(
                string.IsNullOrWhiteSpace(e.Source),
                $"entry {e.CommentaryId} has empty source");
        }
    }

    [Fact]
    public void Fixture_Witness_Id_Matches_Commentary_Id_OneToOne()
    {
        // Per Recon A's mapping: every commentary entry's witness_id
        // mirrors its commentary_id (e.g. C5/C5). No cross-referencing.
        var info = TryLoadFixtureOrLogSkip();
        if (info is null) return;
        Assert.NotNull(info.Entries);
        foreach (var e in info.Entries!)
        {
            Assert.Equal(e.CommentaryId, e.WitnessId);
        }
    }

    [Fact]
    public void Fixture_BodyAndLocus_Are_Null_TodayPerReconA()
    {
        // Recon A documents that most C* items have no OCR'd text — the
        // PR 4 fixture leaves locus_id / anchor_text / body all null.
        // This test guards against a regression where someone fabricates
        // body content rather than ingesting genuine OCR.
        var info = TryLoadFixtureOrLogSkip();
        if (info is null) return;
        Assert.NotNull(info.Entries);
        Assert.All(info.Entries!, e =>
        {
            Assert.Null(e.LocusId);
            Assert.Null(e.AnchorText);
            Assert.Null(e.Body);
        });
    }

    // ─────────────────────────────────────────────────────────────
    // Gap-fill fact (Wave 4 test-writer pass — RUN-20260512-1754).
    // PR 4's CHANGES.md claims "Manifest update is non-destructive:
    // existing fields untouched (only commentary_file +
    // commentary_reader_languages added)". The first 7 facts assert
    // shape of commentary.json but never look at manifest.json.
    // This fact loads the sibling manifest and locks in:
    //   1. Both new fields are present with the expected values.
    //   2. text_id, work_name, edition_kind, base_witness_id (a
    //      sampling of original fields) survived the patch.
    // Soft-skips when OpenZen sibling isn't checked out.
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public void Fixture_Manifest_NonDestructive_RetainsPriorFields_AndAddsCommentaryFields()
    {
        var fixturePath = TryFindFixturePath();
        if (fixturePath is null)
        {
            _output.WriteLine("[SKIP] " + SkipReason);
            return;
        }

        // manifest.json sits in the same dir as commentary.json.
        var manifestPath = Path.Combine(
            Path.GetDirectoryName(fixturePath)!,
            "manifest.json");
        if (!File.Exists(manifestPath))
        {
            _output.WriteLine("[SKIP] manifest.json missing alongside commentary.json: " + manifestPath);
            return;
        }

        var json = File.ReadAllText(manifestPath);
        var manifest = JsonSerializer.Deserialize<ManifestInfo>(json);
        Assert.NotNull(manifest);

        // The two NEW fields PR 4 added must be present + populated.
        Assert.Equal("commentary.json", manifest!.CommentaryFile);
        Assert.NotNull(manifest.CommentaryReaderLanguages);
        Assert.Contains("zh-Hant", manifest.CommentaryReaderLanguages!);
        Assert.Contains("zh-Hans", manifest.CommentaryReaderLanguages!);

        // A sampling of pre-existing fields must still be populated —
        // proves the patch was additive, not a rewrite. We don't assert
        // exact values (the FiM manifest is editorial and may evolve);
        // we only assert these fields are non-empty / non-null where the
        // schema requires them to be.
        Assert.False(
            string.IsNullOrWhiteSpace(manifest.TextId),
            "manifest.text_id should survive the patch");
        Assert.False(
            string.IsNullOrWhiteSpace(manifest.WorkName),
            "manifest.work_name should survive the patch");
        Assert.False(
            string.IsNullOrWhiteSpace(manifest.EditionKind),
            "manifest.edition_kind should survive the patch");
        Assert.False(
            string.IsNullOrWhiteSpace(manifest.BaseWitnessId),
            "manifest.base_witness_id should survive the patch");
    }
}
