using System;
using System.Collections.Generic;
using ReadZen.App.Models;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

/// <summary>
/// Tests for CodeFrequencyService: empty tags, single code, multiple codes, CSV format.
/// </summary>
public class CodeFrequencyServiceTests
{
    private static DocumentTag MakeTag(string tagId, string relPath = "T48/T48n2005.xml")
        => new()
        {
            Id = Guid.NewGuid().ToString("N"),
            RelPath = relPath,
            FromLb = "p0001a01",
            ToLb = "p0001a01",
            TagId = tagId,
            CreatedUtc = DateTimeOffset.UtcNow
        };

    private static TagVocabulary MakeVocab(params (string id, string name, string color)[] defs)
    {
        var v = new TagVocabulary();
        foreach (var (id, name, color) in defs)
            v.Tags.Add(new TagDefinition { Id = id, Name = name, Color = color, CreatedUtc = DateTimeOffset.UtcNow });
        return v;
    }

    [Fact]
    public void EmptyTags_ReturnsEmptyReport()
    {
        var report = CodeFrequencyService.Compute(new List<DocumentTag>(), new TagVocabulary());
        Assert.NotNull(report);
        Assert.Empty(report.Rows);
    }

    [Fact]
    public void SingleCode_CountsCorrectly()
    {
        var tags = new List<DocumentTag>
        {
            MakeTag("t1"),
            MakeTag("t1"),
            MakeTag("t1", "T49/T49n2006.xml")
        };
        var vocab = MakeVocab(("t1", "Theme", "#FF0000"));

        var report = CodeFrequencyService.Compute(tags, vocab);

        Assert.Single(report.Rows);
        Assert.Equal("t1", report.Rows[0].TagId);
        Assert.Equal("Theme", report.Rows[0].TagName);
        Assert.Equal("#FF0000", report.Rows[0].Color);
        Assert.Equal(3, report.Rows[0].SegmentCount);
        Assert.Equal(2, report.Rows[0].FileCount);
    }

    [Fact]
    public void MultipleCodes_SortedByTagId()
    {
        var tags = new List<DocumentTag>
        {
            MakeTag("t2"),
            MakeTag("t1"),
            MakeTag("t2"),
            MakeTag("t1"),
            MakeTag("t1")
        };
        var vocab = MakeVocab(("t1", "Theme", "#FF0000"), ("t2", "Metaphor", "#00FF00"));

        var report = CodeFrequencyService.Compute(tags, vocab);

        Assert.Equal(2, report.Rows.Count);
        Assert.Equal("t1", report.Rows[0].TagId);
        Assert.Equal(3, report.Rows[0].SegmentCount);
        Assert.Equal("t2", report.Rows[1].TagId);
        Assert.Equal(2, report.Rows[1].SegmentCount);
    }

    [Fact]
    public void FileCount_CountsDistinctFiles()
    {
        var tags = new List<DocumentTag>
        {
            MakeTag("t1", "file1.xml"),
            MakeTag("t1", "file1.xml"),
            MakeTag("t1", "file2.xml"),
            MakeTag("t1", "file3.xml")
        };
        var vocab = MakeVocab(("t1", "Theme", "#FF0000"));

        var report = CodeFrequencyService.Compute(tags, vocab);
        Assert.Equal(4, report.Rows[0].SegmentCount);
        Assert.Equal(3, report.Rows[0].FileCount);
    }

    [Fact]
    public void UnknownTagId_FallsBackToId()
    {
        var tags = new List<DocumentTag> { MakeTag("unknown-code") };
        var report = CodeFrequencyService.Compute(tags, new TagVocabulary());

        Assert.Single(report.Rows);
        Assert.Equal("unknown-code", report.Rows[0].TagName);
        Assert.Equal("#808080", report.Rows[0].Color);
    }

    [Fact]
    public void ExportCsv_ContainsHeaderAndRows()
    {
        var tags = new List<DocumentTag>
        {
            MakeTag("t1"),
            MakeTag("t2"),
            MakeTag("t2")
        };
        var vocab = MakeVocab(("t1", "Theme", "#FF0000"), ("t2", "Metaphor", "#00FF00"));
        var report = CodeFrequencyService.Compute(tags, vocab);

        var csv = CodeFrequencyService.ExportCsv(report);

        Assert.Contains("TagId,TagName,Color,SegmentCount,FileCount", csv);
        Assert.Contains("t1,Theme,#FF0000,1,1", csv);
        Assert.Contains("t2,Metaphor,#00FF00,2,1", csv);
    }

    [Fact]
    public void ExportCsv_EscapesCommasInName()
    {
        var vocab = MakeVocab(("t1", "Hello, World", "#FF0000"));
        var tags = new List<DocumentTag> { MakeTag("t1") };
        var report = CodeFrequencyService.Compute(tags, vocab);
        var csv = CodeFrequencyService.ExportCsv(report);

        // Name with comma should be quoted
        Assert.Contains("\"Hello, World\"", csv);
    }

    [Fact]
    public void ExportCsv_EmptyReport_OnlyHeader()
    {
        var report = new CodeFrequencyReport();
        var csv = CodeFrequencyService.ExportCsv(report);

        var lines = csv.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        Assert.Single(lines);
        Assert.StartsWith("TagId,", lines[0]);
    }
}
