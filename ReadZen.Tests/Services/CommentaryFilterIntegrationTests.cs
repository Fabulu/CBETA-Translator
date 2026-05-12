// CommentaryFilterIntegrationTests — verifies the classifier + filter +
// side-map pipeline end-to-end against a real CommentaryService and a
// temp-dir commentary.json fixture. Proves:
//   1. Reader call (with allowedLanguages) drops Japanese; admin call (null) keeps everything.
//   2. The classifier fills missing Language fields BEFORE the filter sees them.
//   3. GetInferenceTag exposes provenance for inferred entries.

using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using ReadZen.App.Models;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

[Trait("Domain", "FiM")]
public class CommentaryFilterIntegrationTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _xmlPath;

    public CommentaryFilterIntegrationTests()
    {
        _tempDir = Path.Combine(
            Path.GetTempPath(),
            "readzen-commentary-filter-test-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
        _xmlPath = Path.Combine(_tempDir, "sample.xml");
        File.WriteAllText(_xmlPath, "<TEI/>");
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); } catch { }
    }

    private void WriteCommentary(IEnumerable<CommentaryEntry> entries)
    {
        var info = new CommentaryInfo { Entries = new List<CommentaryEntry>(entries) };
        var json = JsonSerializer.Serialize(info);
        File.WriteAllText(Path.Combine(_tempDir, "commentary.json"), json);
    }

    [Fact]
    public void ReaderCall_FiltersJapaneseOut_AdminCall_KeepsAll()
    {
        // Mixed fixture: 2 explicit-ja, 1 explicit-zh-Hant, 1 null/Japanese-titled
        // (will be classified as ja via Tier 3). Reader expects only the zh-Hant
        // entry; admin expects all 4.
        WriteCommentary(new[]
        {
            new CommentaryEntry { CommentaryId = "C1", Language = "ja", Title = "信心銘講話" },
            new CommentaryEntry { CommentaryId = "C2", Language = "ja", Title = "信心銘和譯" },
            new CommentaryEntry { CommentaryId = "C3", Language = "zh-Hant", Title = "信心銘新註" },
            new CommentaryEntry { CommentaryId = "C4", Language = null, Title = "信心銘講話" }, // Tier 3 → ja
        });

        var svc = new CommentaryService();

        var readerResult = svc.TryLoad(_xmlPath, new[] { "zh-Hant", "zh-Hans" });
        Assert.NotNull(readerResult);
        Assert.Single(readerResult!.Entries!);
        Assert.Equal("C3", readerResult.Entries![0].CommentaryId);

        var adminResult = svc.TryLoad(_xmlPath, allowedLanguages: null);
        Assert.NotNull(adminResult);
        Assert.Equal(4, adminResult!.Entries!.Count);
    }

    [Fact]
    public void ClassifierFillsMissingLanguage_BeforeFilter()
    {
        // Entry with null Language and title 信心銘講話 → classifier infers "ja"
        // via Tier 3 → reader filter drops it. The cached entry's Language
        // field is mutated to "ja" so subsequent admin reads see the inferred tag.
        WriteCommentary(new[]
        {
            new CommentaryEntry { CommentaryId = "C1", Language = null, Title = "信心銘講話", Body = null },
        });

        var svc = new CommentaryService();

        var readerResult = svc.TryLoad(_xmlPath, new[] { "zh-Hant", "zh-Hans" });
        Assert.NotNull(readerResult);
        Assert.Empty(readerResult!.Entries!);

        // Side-map provenance confirms the inferred classification.
        var tag = svc.GetInferenceTag("C1");
        Assert.NotNull(tag);
        Assert.Equal("ja", tag!.Bcp47);
        Assert.Equal(LanguageInferenceSource.TitleKeyword, tag.Source);

        // The cached entry now carries the inferred Language; an admin
        // read sees it surfaced (unfiltered).
        var adminResult = svc.TryLoad(_xmlPath, allowedLanguages: null);
        Assert.NotNull(adminResult);
        Assert.Single(adminResult!.Entries!);
        Assert.Equal("ja", adminResult.Entries![0].Language);
    }

    [Fact]
    public void GetInferenceTag_ReturnsProvenance_ForInferredEntry()
    {
        // Fixture with one Tier-3 entry, one Tier-1 entry, and one missing id.
        // Tier 1 entries skip classification entirely (GetInferenceTag returns null).
        // The Tier 3 entry's tag is inspectable with Source = TitleKeyword.
        WriteCommentary(new[]
        {
            new CommentaryEntry { CommentaryId = "C1", Language = "ja", Title = "explicit" },
            new CommentaryEntry { CommentaryId = "C3", Language = null, Title = "信心銘講話", Body = null },
        });

        var svc = new CommentaryService();
        var loaded = svc.TryLoad(_xmlPath, allowedLanguages: null);
        Assert.NotNull(loaded);

        // C3 was classified via TitleKeyword; its tag is in the side map.
        var c3Tag = svc.GetInferenceTag("C3");
        Assert.NotNull(c3Tag);
        Assert.Equal(LanguageInferenceSource.TitleKeyword, c3Tag!.Source);
        Assert.Equal("ja", c3Tag.Bcp47);
        Assert.Contains("講話", c3Tag.Evidence);

        // C1 had explicit metadata — no inference ran, so the side map
        // returns null for it.
        Assert.Null(svc.GetInferenceTag("C1"));

        // Unknown id → null (not an exception).
        Assert.Null(svc.GetInferenceTag("does-not-exist"));
    }

    // ─────────────────────────────────────────────────────────────
    // Gap-fill facts (Wave 4 test-writer pass — RUN-20260512-1754).
    // ─────────────────────────────────────────────────────────────

    [Fact]
    public void GetInferenceTag_ReturnsNull_ForNullOrEmptyId()
    {
        // Defensive guard: GetInferenceTag(null) / "" / whitespace must
        // return null (not throw). None of the first 3 integration facts
        // exercised this branch.
        WriteCommentary(new[]
        {
            new CommentaryEntry { CommentaryId = "C1", Language = null, Title = "信心銘講話", Body = null },
        });

        var svc = new CommentaryService();
        var loaded = svc.TryLoad(_xmlPath, allowedLanguages: null);
        Assert.NotNull(loaded);

        Assert.Null(svc.GetInferenceTag(null!));
        Assert.Null(svc.GetInferenceTag(""));
        // Whitespace-only id: real service guards via string.IsNullOrEmpty
        // only — a whitespace key won't be in the side map either way.
        Assert.Null(svc.GetInferenceTag("   "));

        // Sanity-check that the real (valid) inference tag IS still
        // accessible — the null/empty guard didn't blanket-suppress everything.
        Assert.NotNull(svc.GetInferenceTag("C1"));
    }
}
