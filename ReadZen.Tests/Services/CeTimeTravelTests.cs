using System.Collections.Generic;
using System.Linq;
using ReadZen.App.Infrastructure;
using ReadZen.App.Models;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

/// <summary>
/// Tests for critical-edition time-travel components:
/// CorrectionLogService, ConfidenceAnalyzer, and VariantAwareTmAnnotator.
/// </summary>
public class CeTimeTravelTests
{
    // ── CorrectionLogService.ReconstructAtStep ──────────────────────────

    private static List<(string Locus, string Text)> SampleText() => new()
    {
        ("T1-p001.l01a", "至道無難"),
        ("T1-p001.l02a", "唯嫌揀擇"),
        ("T1-p001.l03a", "但莫憎愛"),
    };

    private static List<CorrectionEntry> SampleCorrections() => new()
    {
        new() { Index = 0, Locus = "T1-p001.l01a", Before = "至道無雅", After = "至道無難" },
        new() { Index = 1, Locus = "T1-p001.l02a", Before = "催焦择", After = "唯嫌揀擇" },
    };

    [Fact]
    public void ReconstructAtStep0_ReturnsRawOcr()
    {
        var state = CorrectionLogService.ReconstructAtStep(SampleText(), SampleCorrections(), 0);

        Assert.Equal(0, state.CorrectionCount);
        Assert.Equal("至道無雅", state.Lines[0].Text);
        Assert.Equal("催焦择", state.Lines[1].Text);
        Assert.Equal("但莫憎愛", state.Lines[2].Text); // untouched
        Assert.Null(state.HighlightLocus);
    }

    [Fact]
    public void ReconstructAtStepTotal_ReturnsCurrentText()
    {
        var corrections = SampleCorrections();
        var state = CorrectionLogService.ReconstructAtStep(SampleText(), corrections, corrections.Count);

        Assert.Equal(2, state.CorrectionCount);
        Assert.Equal("至道無難", state.Lines[0].Text);
        Assert.Equal("唯嫌揀擇", state.Lines[1].Text);
        Assert.Equal("但莫憎愛", state.Lines[2].Text);
    }

    [Fact]
    public void ReconstructAtStep1_AppliesOnlyFirstCorrection()
    {
        var state = CorrectionLogService.ReconstructAtStep(SampleText(), SampleCorrections(), 1);

        Assert.Equal(1, state.CorrectionCount);
        Assert.Equal("至道無難", state.Lines[0].Text);  // corrected
        Assert.Equal("催焦择", state.Lines[1].Text);    // still raw
        Assert.Equal("T1-p001.l01a", state.HighlightLocus);
    }

    [Fact]
    public void ReconstructAtStep_NewLocusNotInText_DoesNotCrash()
    {
        var text = new List<(string, string)> { ("T1-p001.l01a", "至道無難") };
        var corrections = new List<CorrectionEntry>
        {
            new() { Index = 0, Locus = "T1-p021.l01a", Before = "X", After = "Y" },
        };

        var state = CorrectionLogService.ReconstructAtStep(text, corrections, 0);
        Assert.Single(state.Lines);
        Assert.Equal("至道無難", state.Lines[0].Text);
    }

    // ── ConfidenceAnalyzer ──────────────────────────────────────────────

    [Fact]
    public void Analyze_NoApparatus_AllHigh()
    {
        var result = ConfidenceAnalyzer.Analyze(null, 5);

        Assert.Equal(5, result.Count);
        Assert.All(result.Values, v => Assert.Equal(ConfidenceLevel.High, v));
    }

    [Fact]
    public void Analyze_SingleReading_StaysHigh()
    {
        var apparatus = new ApparatusInfo
        {
            Entries = new()
            {
                new ApparatusEntry
                {
                    LocusId = "line-3",
                    Status = "accepted",
                    Readings = new() { new ApparatusReading { WitnessId = "W1", IsHumanChecked = true } },
                },
            },
        };

        var result = ConfidenceAnalyzer.Analyze(apparatus, 5);
        Assert.Equal(ConfidenceLevel.High, result[2]); // line-3 -> index 2
    }

    [Fact]
    public void Analyze_MultipleReadings_AllHumanChecked_Medium()
    {
        var apparatus = new ApparatusInfo
        {
            Entries = new()
            {
                new ApparatusEntry
                {
                    LocusId = "line-2",
                    Status = "accepted",
                    Readings = new()
                    {
                        new ApparatusReading { WitnessId = "W1", IsHumanChecked = true, IsOcrOnly = false },
                        new ApparatusReading { WitnessId = "W2", IsHumanChecked = true, IsOcrOnly = false },
                        new ApparatusReading { WitnessId = "W3", IsHumanChecked = true, IsOcrOnly = false },
                    },
                },
            },
        };

        var result = ConfidenceAnalyzer.Analyze(apparatus, 5);
        Assert.Equal(ConfidenceLevel.Medium, result[1]);
    }

    [Fact]
    public void Analyze_OcrOnlyReading_Low()
    {
        var apparatus = new ApparatusInfo
        {
            Entries = new()
            {
                new ApparatusEntry
                {
                    LocusId = "line-1",
                    Status = "accepted",
                    Readings = new()
                    {
                        new ApparatusReading { WitnessId = "W1", IsHumanChecked = true },
                        new ApparatusReading { WitnessId = "W2", IsOcrOnly = true },
                    },
                },
            },
        };

        var result = ConfidenceAnalyzer.Analyze(apparatus, 3);
        Assert.Equal(ConfidenceLevel.Low, result[0]);
    }

    // ── VariantAwareTmAnnotator ─────────────────────────────────────────

    [Fact]
    public void Annotate_IdenticalSource_NotMarkedVariant()
    {
        var matches = new List<TranslationTmMatch>
        {
            new() { SourceText = "至道無難", TargetText = "The Great Way is not difficult" },
        };

        VariantAwareTmAnnotator.Annotate(matches, "至道無難");

        Assert.False(matches[0].IsVariantMatch);
        Assert.Null(matches[0].VariantNote);
    }

    [Fact]
    public void Annotate_OneCharDiff_MarkedAsVariant()
    {
        var matches = new List<TranslationTmMatch>
        {
            new() { SourceText = "至道無雅", TargetText = "The Great Way is not difficult" },
        };

        VariantAwareTmAnnotator.Annotate(matches, "至道無難");

        Assert.True(matches[0].IsVariantMatch);
        Assert.Contains("variant reading", matches[0].VariantNote);
    }

    [Fact]
    public void Annotate_CompletelyDifferent_NotMarked()
    {
        // Use strings that share some chars but differ in >3 spans,
        // exceeding MaxChangedSpans so the annotator skips them.
        var matches = new List<TranslationTmMatch>
        {
            new() { SourceText = "A道B嫌C擇D愛", TargetText = "something" },
        };

        VariantAwareTmAnnotator.Annotate(matches, "至道無嫌唯擇揀愛");

        Assert.False(matches[0].IsVariantMatch);
        Assert.Null(matches[0].VariantNote);
    }

    [Fact]
    public void Annotate_KnownCorrection_NotesSaysKnownCorrection()
    {
        var matches = new List<TranslationTmMatch>
        {
            new() { SourceText = "至道無雅", TargetText = "The Great Way" },
        };
        var corrections = new List<CorrectionEntry>
        {
            new() { Before = "雅", After = "難" },
        };

        VariantAwareTmAnnotator.Annotate(matches, "至道無難", corrections);

        Assert.True(matches[0].IsVariantMatch);
        Assert.Contains("known correction", matches[0].VariantNote);
    }
}
