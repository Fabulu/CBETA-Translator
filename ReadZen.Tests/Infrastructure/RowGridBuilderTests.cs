// RowGridBuilderTests — pure builder for the unified row-grid reading surface (Wave C1).
// C1 implements only AlignedLines: one two-column row per <lb/>, ZH from the original and
// EN from the translation's same-key segment (or "" on a graceful miss), keyed lb -> row.

using System;
using System.Collections.Generic;
using ReadZen.App.Infrastructure;
using ReadZen.App.Models;
using Xunit;

namespace ReadZen.Tests.Infrastructure;

[Trait("Domain", "Reader")]
public class RowGridBuilderTests
{
    // Builds a RenderedDocument with the given text and (lb-key, start, endExclusive) segments.
    private static RenderedDocument Doc(string text, params (string key, int start, int end)[] segs)
    {
        var list = new List<RenderSegment>();
        foreach (var (key, start, end) in segs)
            list.Add(new RenderSegment(key, start, end));
        return new RenderedDocument(
            text,
            list,
            new List<DocAnnotation>(),
            new List<AnnotationMarkerInserter.MarkerSpan>());
    }

    // "初句\n二句"  -> 初=0 句=1 \n=2 二=3 句=4  ; "first\nsecond" -> first=0..4, second=6..11
    private static RenderedDocument OrigTwoLines()
        => Doc("初句\n二句", ("lb|0001a01", 0, 2), ("lb|0001a02", 3, 5));

    private static RenderedDocument TranTwoLines()
        => Doc("first\nsecond", ("lb|0001a01", 0, 5), ("lb|0001a02", 6, 12));

    [Fact]
    public void AlignedLines_EmitsOneTwoColumnRowPerLb_WithPairedText()
    {
        var model = RowGridBuilder.Build(
            OrigTwoLines(), TranTwoLines(), segMap: null,
            ReadingLayoutMode.AlignedLines, ReaderViewMode.Both, showLineIds: false);

        Assert.Equal(2, model.Rows.Count);

        Assert.Equal(0, model.Rows[0].Index);
        Assert.Equal("0001a01", model.Rows[0].Lb);
        Assert.Equal(RowShape.TwoColumn, model.Rows[0].Shape);
        Assert.Equal("初句", model.Rows[0].ZhText);
        Assert.Equal("first", model.Rows[0].EnText);

        Assert.Equal(1, model.Rows[1].Index);
        Assert.Equal("0001a02", model.Rows[1].Lb);
        Assert.Equal("二句", model.Rows[1].ZhText);
        Assert.Equal("second", model.Rows[1].EnText);
    }

    [Fact]
    public void AlignedLines_LbToRow_MapsEachLbToItsRowIndex()
    {
        var model = RowGridBuilder.Build(
            OrigTwoLines(), TranTwoLines(), null,
            ReadingLayoutMode.AlignedLines, ReaderViewMode.Both, false);

        Assert.Equal(0, model.LbToRow["0001a01"]);
        Assert.Equal(1, model.LbToRow["0001a02"]);
    }

    [Fact]
    public void AlignedLines_RecoversText_FromZeroLengthLbAnchors()
    {
        // The canonical CBETA "lb-before-p" shape: <lb/> is a ZERO-LENGTH anchor and the
        // line's text lives in the following p| segment. The builder must span lb-start →
        // next-lb-start (NOT the lb segment's own [Start,EndExclusive), which is empty) or it
        // drops the first line of every paragraph. Text "趙州狗子\n無佛性": 趙0 州1 狗2 子3 \n4 無5 佛6 性7.
        var orig = Doc("趙州狗子\n無佛性",
            ("lb|0001a01", 0, 0),   // zero-length anchor
            ("p|p1", 0, 8),          // the paragraph carries the text
            ("lb|0001a02", 5, 5));   // zero-length anchor before 無
        var tran = Doc("Zhaozhou\ndog-nature",
            ("lb|0001a01", 0, 0), ("p|q1", 0, 19), ("lb|0001a02", 9, 9));

        var model = RowGridBuilder.Build(
            orig, tran, null, ReadingLayoutMode.AlignedLines, ReaderViewMode.Both, false);

        Assert.Equal(2, model.Rows.Count);
        Assert.Equal("趙州狗子", model.Rows[0].ZhText);   // would be "" under the naive lb-slice bug
        Assert.Equal("無佛性", model.Rows[1].ZhText);
        Assert.Equal("Zhaozhou", model.Rows[0].EnText);
        Assert.Equal("dog-nature", model.Rows[1].EnText);
    }

    [Fact]
    public void AlignedLines_PairsEn_ToleratingEditionSuffixInLbKeys()
    {
        // Translation lb keys carry an edition suffix ("lb|…|T") that the original's do not.
        // Pairing keys on the bare n-value must still match, or the whole EN column blanks.
        var orig = Doc("初\n二", ("lb|0001a01", 0, 0), ("p|p", 0, 3), ("lb|0001a02", 2, 2));
        var tran = Doc("one\ntwo", ("lb|0001a01|T", 0, 0), ("p|q", 0, 7), ("lb|0001a02|T", 4, 4));

        var model = RowGridBuilder.Build(
            orig, tran, null, ReadingLayoutMode.AlignedLines, ReaderViewMode.Both, false);

        Assert.Equal("one", model.Rows[0].EnText);
        Assert.Equal("two", model.Rows[1].EnText);
    }

    [Fact]
    public void AlignedLines_MissingTranslationLine_YieldsEmptyEn_Gracefully()
    {
        // Translation carries only the first lb; the second row must still render with EN "".
        var tran = Doc("first", ("lb|0001a01", 0, 5));
        var model = RowGridBuilder.Build(
            OrigTwoLines(), tran, null,
            ReadingLayoutMode.AlignedLines, ReaderViewMode.Both, false);

        Assert.Equal(2, model.Rows.Count);
        Assert.Equal("first", model.Rows[0].EnText);
        Assert.Equal("", model.Rows[1].EnText);   // graceful miss, not a crash
        Assert.Equal("二句", model.Rows[1].ZhText); // ZH still present
    }

    [Theory]
    [InlineData(true, "0001a01")]
    [InlineData(false, "")]
    public void AlignedLines_ShowLineIds_TogglesIdLabel(bool show, string expectedFirstLabel)
    {
        var model = RowGridBuilder.Build(
            OrigTwoLines(), TranTwoLines(), null,
            ReadingLayoutMode.AlignedLines, ReaderViewMode.Both, showLineIds: show);

        Assert.Equal(expectedFirstLabel, model.Rows[0].IdLabel);
    }

    [Fact]
    public void AlignedLines_SkipsNonLbSegments()
    {
        // A stray non-"lb|" segment must not become a row.
        var orig = Doc("初句\n二句",
            ("head|x", 0, 0), ("lb|0001a01", 0, 2), ("lb|0001a02", 3, 5));
        var model = RowGridBuilder.Build(
            orig, TranTwoLines(), null,
            ReadingLayoutMode.AlignedLines, ReaderViewMode.Both, false);

        Assert.Equal(2, model.Rows.Count);
        Assert.All(model.Rows, r => Assert.StartsWith("0001a0", r.Lb));
    }

    [Fact]
    public void AlignedLines_NeedsNoSegmentMap()
    {
        // Passing a null segMap must not throw — AlignedLines pairs purely by lb key.
        var ex = Record.Exception(() => RowGridBuilder.Build(
            OrigTwoLines(), TranTwoLines(), null,
            ReadingLayoutMode.AlignedLines, ReaderViewMode.Both, false));
        Assert.Null(ex);
    }

    [Fact]
    public void EmptyOriginal_YieldsEmptyModel()
    {
        var model = RowGridBuilder.Build(
            RenderedDocument.Empty, RenderedDocument.Empty, null,
            ReadingLayoutMode.AlignedLines, ReaderViewMode.Both, false);

        Assert.Empty(model.Rows);
        Assert.Empty(model.LbToRow);
    }

    [Theory]
    [InlineData(ReadingLayoutMode.AlignedBlocks)]
    [InlineData(ReadingLayoutMode.Interleaved)]
    [InlineData(ReadingLayoutMode.MergedStacked)]
    [InlineData(ReadingLayoutMode.Page)]
    [InlineData(ReadingLayoutMode.MergedFlow)]
    public void NotYetImplementedModes_Throw(ReadingLayoutMode mode)
    {
        // C1 routes only AlignedLines to the grid; every other mode must fail loudly rather
        // than silently render blank (the render strategy router never sends them here yet).
        Assert.Throws<NotSupportedException>(() => RowGridBuilder.Build(
            OrigTwoLines(), TranTwoLines(), null, mode, ReaderViewMode.Both, false));
    }
}
