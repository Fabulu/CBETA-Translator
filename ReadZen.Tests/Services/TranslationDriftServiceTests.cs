using System.Collections.Generic;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

public class TranslationDriftServiceTests
{
    private static List<CorrectionEntry> MakeCorrections(params (string locus, string before, string after)[] entries)
    {
        var list = new List<CorrectionEntry>();
        for (int i = 0; i < entries.Length; i++)
        {
            list.Add(new CorrectionEntry
            {
                Index = i,
                Locus = entries[i].locus,
                Before = entries[i].before,
                After = entries[i].after,
                Date = "2026-04-14",
                ChangeType = "fix",
                Status = "fixed"
            });
        }
        return list;
    }

    private static List<(string, string)> MakeText(params (string locus, string text)[] lines)
        => new(lines);

    [Fact]
    public void NoDrift_WhenNoCorrections()
    {
        var text = MakeText(("L1", "至道無難"), ("L2", "唯嫌揀擇"));
        var translations = new Dictionary<string, string> { ["L1"] = "Supreme Way", ["L2"] = "Picking" };

        var report = TranslationDriftService.ComputeDrift(new(), text, translations);

        Assert.Equal(2, report.TotalSegments);
        Assert.Equal(2, report.TranslatedSegments);
        Assert.Equal(2, report.CurrentSegments);
        Assert.Equal(0, report.StaleSegments);
        Assert.Empty(report.Drifts);
    }

    [Fact]
    public void DetectsDrift_WhenCorrectionAfterTranslation()
    {
        var corrections = MakeCorrections(("L1", "至道無雅", "至道無難"));
        var text = MakeText(("L1", "至道無難"), ("L2", "唯嫌揀擇"));
        var translations = new Dictionary<string, string> { ["L1"] = "old translation", ["L2"] = "picking" };
        // L1 translated at step 0 (before any corrections)
        var translatedAt = new Dictionary<string, int> { ["L1"] = 0, ["L2"] = 0 };

        var report = TranslationDriftService.ComputeDrift(corrections, text, translations, translatedAt);

        Assert.Equal(1, report.StaleSegments);
        Assert.Equal(1, report.CurrentSegments); // L2 has no corrections → current
        Assert.Single(report.Drifts);
        Assert.Equal("L1", report.Drifts[0].Locus);
        Assert.Equal("至道無雅", report.Drifts[0].ChineseBefore);
        Assert.Equal("至道無難", report.Drifts[0].ChineseAfter);
        Assert.Contains("[-", report.Drifts[0].DiffSummary);
    }

    [Fact]
    public void NoDrift_WhenTranslationUpdatedAfterCorrection()
    {
        var corrections = MakeCorrections(("L1", "至道無雅", "至道無難"));
        var text = MakeText(("L1", "至道無難"));
        var translations = new Dictionary<string, string> { ["L1"] = "updated translation" };
        // L1 translated at step 1 (after the correction at step 1)
        var translatedAt = new Dictionary<string, int> { ["L1"] = 1 };

        var report = TranslationDriftService.ComputeDrift(corrections, text, translations, translatedAt);

        Assert.Equal(0, report.StaleSegments);
        Assert.Equal(1, report.CurrentSegments);
    }

    [Fact]
    public void UntranslatedSegments_NotCountedAsDrift()
    {
        var corrections = MakeCorrections(("L1", "A", "B"));
        var text = MakeText(("L1", "B"), ("L2", "C"));
        var translations = new Dictionary<string, string>(); // nothing translated

        var report = TranslationDriftService.ComputeDrift(corrections, text, translations);

        Assert.Equal(2, report.UntranslatedSegments);
        Assert.Equal(0, report.StaleSegments);
        Assert.Equal(0, report.TranslatedSegments);
    }

    [Fact]
    public void MultipleCorrections_LatestOneDefinesDrift()
    {
        // L1 corrected twice: step 0 and step 1
        var corrections = MakeCorrections(
            ("L1", "A", "B"),
            ("L1", "B", "C")
        );
        var text = MakeText(("L1", "C"));
        var translations = new Dictionary<string, string> { ["L1"] = "translated after first correction" };
        // Translated after step 1 but before step 2
        var translatedAt = new Dictionary<string, int> { ["L1"] = 1 };

        var report = TranslationDriftService.ComputeDrift(corrections, text, translations, translatedAt);

        // Latest correction is step 2, translation at step 1 → stale
        Assert.Equal(1, report.StaleSegments);
        Assert.Equal("B", report.Drifts[0].ChineseBefore); // the LATEST correction's before
        Assert.Equal("C", report.Drifts[0].ChineseAfter);
    }

    [Fact]
    public void CurrentPercent_CalculatesCorrectly()
    {
        var corrections = MakeCorrections(("L1", "A", "B"));
        var text = MakeText(("L1", "B"), ("L2", "C"), ("L3", "D"));
        var translations = new Dictionary<string, string>
        {
            ["L1"] = "stale", ["L2"] = "current", ["L3"] = "current"
        };
        var translatedAt = new Dictionary<string, int> { ["L1"] = 0, ["L2"] = 0, ["L3"] = 0 };

        var report = TranslationDriftService.ComputeDrift(corrections, text, translations, translatedAt);

        // 3 translated, 1 stale, 2 current → 66.7%
        Assert.Equal(3, report.TranslatedSegments);
        Assert.Equal(2, report.CurrentSegments);
        Assert.InRange(report.CurrentPercent, 66.0, 67.0);
    }
}
