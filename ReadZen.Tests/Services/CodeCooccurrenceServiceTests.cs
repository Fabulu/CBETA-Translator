using System;
using System.Collections.Generic;
using ReadZen.App.Models;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

/// <summary>
/// Tests for CodeCooccurrenceService: overlapping ranges, non-overlapping, symmetry, self-occurrence.
/// </summary>
public class CodeCooccurrenceServiceTests
{
    private static DocumentTag MakeTag(string tagId, string fromLb, string toLb, string relPath = "T48/T48n2005.xml")
        => new()
        {
            Id = Guid.NewGuid().ToString("N"),
            RelPath = relPath,
            FromLb = fromLb,
            ToLb = toLb,
            TagId = tagId,
            CreatedUtc = DateTimeOffset.UtcNow
        };

    private static TagVocabulary MakeVocab(params (string id, string name)[] defs)
    {
        var v = new TagVocabulary();
        foreach (var (id, name) in defs)
            v.Tags.Add(new TagDefinition { Id = id, Name = name, Color = "#3498DB", CreatedUtc = DateTimeOffset.UtcNow });
        return v;
    }

    [Fact]
    public void EmptyTags_ReturnsEmptyMatrix()
    {
        var m = CodeCooccurrenceService.Compute(new List<DocumentTag>(), new TagVocabulary());
        Assert.Empty(m.CodeIds);
        Assert.Equal(0, m.Matrix.GetLength(0));
    }

    [Fact]
    public void OverlappingRanges_CountsCooccurrence()
    {
        // Two tags in same file with overlapping lb-ranges
        var tags = new List<DocumentTag>
        {
            MakeTag("t1", "p0001a01", "p0005a01"),
            MakeTag("t2", "p0003a01", "p0007a01")
        };
        var vocab = MakeVocab(("t1", "Theme"), ("t2", "Metaphor"));

        var m = CodeCooccurrenceService.Compute(tags, vocab);

        Assert.Equal(2, m.CodeIds.Count);
        int i1 = m.CodeIds.IndexOf("t1");
        int i2 = m.CodeIds.IndexOf("t2");

        // t1 and t2 overlap in this file
        Assert.Equal(1, m.Matrix[i1, i2]);
        Assert.Equal(1, m.Matrix[i2, i1]); // symmetry
    }

    [Fact]
    public void NonOverlappingRanges_ZeroCooccurrence()
    {
        // Two tags in same file but non-overlapping lb-ranges
        var tags = new List<DocumentTag>
        {
            MakeTag("t1", "p0001a01", "p0002a01"),
            MakeTag("t2", "p0005a01", "p0007a01")
        };
        var vocab = MakeVocab(("t1", "Theme"), ("t2", "Metaphor"));

        var m = CodeCooccurrenceService.Compute(tags, vocab);

        int i1 = m.CodeIds.IndexOf("t1");
        int i2 = m.CodeIds.IndexOf("t2");

        Assert.Equal(0, m.Matrix[i1, i2]);
        Assert.Equal(0, m.Matrix[i2, i1]);
    }

    [Fact]
    public void Symmetry_MatrixIsSymmetric()
    {
        var tags = new List<DocumentTag>
        {
            MakeTag("t1", "p0001a01", "p0010a01"),
            MakeTag("t2", "p0005a01", "p0015a01"),
            MakeTag("t3", "p0008a01", "p0012a01")
        };
        var vocab = MakeVocab(("t1", "A"), ("t2", "B"), ("t3", "C"));

        var m = CodeCooccurrenceService.Compute(tags, vocab);

        int n = m.CodeIds.Count;
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
                Assert.Equal(m.Matrix[i, j], m.Matrix[j, i]);
    }

    [Fact]
    public void SelfOccurrence_DiagonalCountsFiles()
    {
        var tags = new List<DocumentTag>
        {
            MakeTag("t1", "p0001a01", "p0005a01", "file1.xml"),
            MakeTag("t1", "p0001a01", "p0005a01", "file2.xml")
        };
        var vocab = MakeVocab(("t1", "Theme"));

        var m = CodeCooccurrenceService.Compute(tags, vocab);

        int i = m.CodeIds.IndexOf("t1");
        Assert.Equal(2, m.Matrix[i, i]); // appears in 2 files
    }

    [Fact]
    public void MultipleFiles_CountsEachFileSeparately()
    {
        var tags = new List<DocumentTag>
        {
            MakeTag("t1", "p0001a01", "p0005a01", "file1.xml"),
            MakeTag("t2", "p0003a01", "p0007a01", "file1.xml"),
            MakeTag("t1", "p0001a01", "p0005a01", "file2.xml"),
            MakeTag("t2", "p0003a01", "p0007a01", "file2.xml")
        };
        var vocab = MakeVocab(("t1", "Theme"), ("t2", "Metaphor"));

        var m = CodeCooccurrenceService.Compute(tags, vocab);

        int i1 = m.CodeIds.IndexOf("t1");
        int i2 = m.CodeIds.IndexOf("t2");

        Assert.Equal(2, m.Matrix[i1, i2]); // overlap in both files
    }

    [Fact]
    public void HasOverlap_DetectsOverlap()
    {
        var a = MakeTag("t1", "p0001a01", "p0005a01");
        var b = MakeTag("t2", "p0003a01", "p0007a01");
        Assert.True(CodeCooccurrenceService.HasOverlap(a, b));
    }

    [Fact]
    public void HasOverlap_DetectsNoOverlap()
    {
        var a = MakeTag("t1", "p0001a01", "p0002a01");
        var b = MakeTag("t2", "p0005a01", "p0007a01");
        Assert.False(CodeCooccurrenceService.HasOverlap(a, b));
    }

    [Fact]
    public void BuildHtml_ContainsTable()
    {
        var tags = new List<DocumentTag>
        {
            MakeTag("t1", "p0001a01", "p0005a01"),
            MakeTag("t2", "p0003a01", "p0007a01")
        };
        var vocab = MakeVocab(("t1", "Theme"), ("t2", "Metaphor"));
        var m = CodeCooccurrenceService.Compute(tags, vocab);

        var html = CodeCooccurrenceService.BuildHtml(m);

        Assert.Contains("<table>", html);
        Assert.Contains("Theme", html);
        Assert.Contains("Metaphor", html);
        Assert.Contains("Co-occurrence", html);
    }

    [Fact]
    public void ExportCsv_HeaderAndRows()
    {
        var tags = new List<DocumentTag>
        {
            MakeTag("t1", "p0001a01", "p0005a01"),
            MakeTag("t2", "p0003a01", "p0007a01")
        };
        var vocab = MakeVocab(("t1", "Theme"), ("t2", "Metaphor"));
        var m = CodeCooccurrenceService.Compute(tags, vocab);

        var csv = CodeCooccurrenceService.ExportCsv(m);

        Assert.Contains("Code,", csv);
        Assert.Contains("Theme", csv);
        Assert.Contains("Metaphor", csv);
    }
}
