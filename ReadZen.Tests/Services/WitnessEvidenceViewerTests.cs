using System;
using System.IO;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

public class WitnessEvidenceViewerTests : IDisposable
{
    private readonly string _tempDir;

    public WitnessEvidenceViewerTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "readzen-evidence-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); } catch { }
    }

    // ── WitnessTextLocatorService.FindInWitness ──────────────────────

    [Fact]
    public void FindInWitness_ExactMatch_ReturnsConfidence1()
    {
        var results = WitnessTextLocatorService.FindInWitness("無門關", "趙州無門關第一則");
        Assert.Single(results);
        Assert.Equal(1.0, results[0].Confidence);
        Assert.Equal("exact", results[0].MatchType);
        Assert.Equal("無門關", results[0].MatchedText);
    }

    [Fact]
    public void FindInWitness_NormalizedMatch_StrippingCjkPunctuation_ReturnsConfidence09()
    {
        // Query without punctuation, witness has CJK punctuation interleaved
        var results = WitnessTextLocatorService.FindInWitness("趙州狗子", "趙州、狗子");
        Assert.Single(results);
        Assert.Equal(0.9, results[0].Confidence);
        Assert.Equal("normalized", results[0].MatchType);
    }

    [Fact]
    public void FindInWitness_CjkPunctuationStripped_AllTypes()
    {
        // All these CJK punctuation chars should be stripped: 。，、！？
        var witness = "一。二，三、四！五？六";
        var results = WitnessTextLocatorService.FindInWitness("一二三四五六", witness);
        Assert.Single(results);
        Assert.Equal(0.9, results[0].Confidence);
    }

    [Fact]
    public void FindInWitness_PartialMatch80_ReturnsConfidence07()
    {
        // Build a query where exact and normalized both fail, but 80% center substring matches
        var core = "佛法僧寶戒定慧";
        var query = "甲" + core + "乙"; // 9 chars; 80% = 7 chars from center = "僧寶戒定慧" area
        // Witness has the core but not the flanking chars
        var results = WitnessTextLocatorService.FindInWitness(query, "前文" + core + "後文");
        Assert.NotEmpty(results);
        Assert.True(results[0].Confidence <= 0.9);
    }

    [Fact]
    public void FindInWitness_NoMatch_ReturnsEmptyList()
    {
        var results = WitnessTextLocatorService.FindInWitness("完全不同", "毫無關係的文字在此");
        Assert.Empty(results);
    }

    [Fact]
    public void FindInWitness_EmptyInputs_ReturnsEmptyList()
    {
        Assert.Empty(WitnessTextLocatorService.FindInWitness("", "some text"));
        Assert.Empty(WitnessTextLocatorService.FindInWitness("query", ""));
        Assert.Empty(WitnessTextLocatorService.FindInWitness("", ""));
    }

    [Fact]
    public void FindInWitness_MultipleMatches_SortedByPosition()
    {
        var results = WitnessTextLocatorService.FindInWitness("無", "第一無第二無第三無");
        Assert.Equal(3, results.Count);
        Assert.True(results[0].StartIndex < results[1].StartIndex);
        Assert.True(results[1].StartIndex < results[2].StartIndex);
    }

    // ── PdfEvidenceService.ResolvePageImagePath ──────────────────────

    [Fact]
    public void ResolvePageImagePath_StripsLineSuffix_BuildsCorrectPath()
    {
        // Create the expected file on disk
        var imgDir = Path.Combine(_tempDir, "T1", "page-images");
        Directory.CreateDirectory(imgDir);
        File.WriteAllText(Path.Combine(imgDir, "T1-p008.png"), "fake");

        var result = PdfEvidenceService.ResolvePageImagePath(_tempDir, "T1", "T1-p008.l01");
        Assert.NotNull(result);
        Assert.EndsWith("T1-p008.png", result!);
    }

    [Fact]
    public void ResolvePageImagePath_NoLineSuffix_StillWorks()
    {
        var imgDir = Path.Combine(_tempDir, "W1", "page-images");
        Directory.CreateDirectory(imgDir);
        File.WriteAllText(Path.Combine(imgDir, "W1-p042.png"), "fake");

        var result = PdfEvidenceService.ResolvePageImagePath(_tempDir, "W1", "W1-p042");
        Assert.NotNull(result);
        Assert.EndsWith("W1-p042.png", result!);
    }

    [Fact]
    public void ResolvePageImagePath_NonExistentFile_ReturnsNull()
    {
        var result = PdfEvidenceService.ResolvePageImagePath(_tempDir, "T1", "T1-p999.l01");
        Assert.Null(result);
    }

    [Fact]
    public void ResolvePageImagePath_EmptyInputs_ReturnsNull()
    {
        Assert.Null(PdfEvidenceService.ResolvePageImagePath("", "T1", "T1-p008.l01"));
        Assert.Null(PdfEvidenceService.ResolvePageImagePath(_tempDir, "", "T1-p008.l01"));
        Assert.Null(PdfEvidenceService.ResolvePageImagePath(_tempDir, "T1", ""));
    }

    // ── WitnessOcrLoader.LoadAllEngineTexts ──────────────────────────

    [Fact]
    public void LoadAllEngineTexts_NonExistentDir_ReturnsEmptyDict()
    {
        var result = WitnessOcrLoader.LoadAllEngineTexts(
            Path.Combine(_tempDir, "nonexistent"), "T1", "p001");
        Assert.Empty(result);
    }

    [Fact]
    public void LoadAllEngineTexts_LoadsFromEngineSubdirs()
    {
        var ocrDir = Path.Combine(_tempDir, "T1", "ocr");
        var rapidDir = Path.Combine(ocrDir, "rapidocr");
        var tesseDir = Path.Combine(ocrDir, "tesseract");
        Directory.CreateDirectory(rapidDir);
        Directory.CreateDirectory(tesseDir);

        File.WriteAllText(Path.Combine(rapidDir, "T1-p001.txt"), "rapid text");
        File.WriteAllText(Path.Combine(tesseDir, "T1-p001.txt"), "tess text");

        var result = WitnessOcrLoader.LoadAllEngineTexts(_tempDir, "T1", "p001");

        Assert.Equal(2, result.Count);
        Assert.Equal("rapid text", result["rapidocr"]);
        Assert.Equal("tess text", result["tesseract"]);
    }

    [Fact]
    public void LoadAllEngineTexts_SkipsEnginesWithoutMatchingFile()
    {
        var ocrDir = Path.Combine(_tempDir, "T1", "ocr");
        var rapidDir = Path.Combine(ocrDir, "rapidocr");
        var emptyDir = Path.Combine(ocrDir, "paddleocr");
        Directory.CreateDirectory(rapidDir);
        Directory.CreateDirectory(emptyDir);

        File.WriteAllText(Path.Combine(rapidDir, "T1-p002.txt"), "has data");
        // paddleocr dir exists but has no matching file

        var result = WitnessOcrLoader.LoadAllEngineTexts(_tempDir, "T1", "p002");

        Assert.Single(result);
        Assert.True(result.ContainsKey("rapidocr"));
        Assert.False(result.ContainsKey("paddleocr"));
    }
}
