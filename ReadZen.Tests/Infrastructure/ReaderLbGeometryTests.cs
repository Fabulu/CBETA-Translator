using System.Collections.Generic;
using ReadZen.App.Infrastructure;
using ReadZen.App.Models;
using Xunit;

namespace ReadZen.Tests.Infrastructure;

/// <summary>
/// Behavior pins for <see cref="ReaderLbGeometry"/>, the pure line-break/segment
/// geometry helpers extracted verbatim from ReadableTabView (MVVM renovation P7).
/// These map lb n-values to rendered-text offsets and drive bookmarks, resume
/// re-anchoring, the row-grid apparatus binding, and lb navigation, so their span
/// arithmetic must stay stable. Fixtures use CJK content so the CJK-boundary helper
/// and whitespace scanning are exercised on real ideographs.
/// </summary>
public class ReaderLbGeometryTests
{
    // Three-line fixture. Indices:
    //   0:甲 1:乙 2:\n 3:丙 4:丁 5:\n 6:戊 7:己   (length 8)
    // lb|1 covers "甲乙", lb|2 covers "丙丁", lb|3 covers "戊己".
    private const string ThreeLineText = "甲乙\n丙丁\n戊己";

    private static RenderedDocument BuildDoc(
        string text,
        IEnumerable<RenderSegment> segments,
        IEnumerable<DocAnnotation>? annotations = null)
    {
        return new RenderedDocument(
            text,
            new List<RenderSegment>(segments),
            new List<DocAnnotation>(annotations ?? System.Array.Empty<DocAnnotation>()),
            new List<AnnotationMarkerInserter.MarkerSpan>());
    }

    private static RenderedDocument ThreeLineDoc(IEnumerable<DocAnnotation>? annotations = null)
        => BuildDoc(ThreeLineText, new[]
        {
            new RenderSegment("lb|1", 0, 2),
            new RenderSegment("lb|2", 3, 5),
            new RenderSegment("lb|3", 6, 8),
        }, annotations);

    // ---- ResolveLbAtOffset ----

    [Fact]
    public void ResolveLbAtOffset_NullDoc_ReturnsNone()
    {
        var (lb, start) = ReaderLbGeometry.ResolveLbAtOffset(null, 5);
        Assert.Null(lb);
        Assert.Equal(0, start);
    }

    [Fact]
    public void ResolveLbAtOffset_EmptySegments_ReturnsNone()
    {
        var doc = BuildDoc("abc", System.Array.Empty<RenderSegment>());
        var (lb, start) = ReaderLbGeometry.ResolveLbAtOffset(doc, 1);
        Assert.Null(lb);
        Assert.Equal(0, start);
    }

    [Theory]
    [InlineData(0, "1", 0)]
    [InlineData(2, "1", 0)]
    [InlineData(3, "2", 3)]
    [InlineData(4, "2", 3)]
    [InlineData(7, "3", 6)]
    public void ResolveLbAtOffset_PicksLbAtOrBeforeOffset(int offset, string expectedLb, int expectedStart)
    {
        var doc = ThreeLineDoc();
        Assert.Equal((expectedLb, expectedStart), ReaderLbGeometry.ResolveLbAtOffset(doc, offset));
    }

    [Fact]
    public void ResolveLbAtOffset_OffsetBeforeFirstLb_ReturnsNone()
    {
        // First lb starts at 0; a synthetic doc whose first lb starts at 3 leaves 0..2 uncovered.
        var doc = BuildDoc("xx\nAB", new[]
        {
            new RenderSegment("start", 0, 2),
            new RenderSegment("lb|9", 3, 5),
        });
        var (lb, start) = ReaderLbGeometry.ResolveLbAtOffset(doc, 1);
        Assert.Null(lb);
        Assert.Equal(0, start);
    }

    // ---- ResolveLbRange ----

    [Fact]
    public void ResolveLbRange_SingleLb_SpansUpToNextLbStart()
    {
        var doc = ThreeLineDoc();
        // lb|2 (single) meaningful span runs from its Start (3) to the next lb Start (6).
        Assert.Equal((3, 3), ReaderLbGeometry.ResolveLbRange(doc, "2", null));
    }

    [Fact]
    public void ResolveLbRange_LastSingleLb_SpansToEndOfText()
    {
        var doc = ThreeLineDoc();
        Assert.Equal((6, 2), ReaderLbGeometry.ResolveLbRange(doc, "3", null));
    }

    [Fact]
    public void ResolveLbRange_DistinctFromTo_SpansFromStartToToEndExclusive()
    {
        var doc = ThreeLineDoc();
        // from=lb|1 Start=0, to=lb|3 EndExclusive=8.
        Assert.Equal((0, 8), ReaderLbGeometry.ResolveLbRange(doc, "1", "3"));
    }

    [Fact]
    public void ResolveLbRange_UnknownFromLb_ReturnsNotFound()
    {
        var doc = ThreeLineDoc();
        Assert.Equal((-1, 0), ReaderLbGeometry.ResolveLbRange(doc, "99", null));
    }

    // ---- ResolveSingleLbMeaningfulSpan ----

    [Fact]
    public void ResolveSingleLbMeaningfulSpan_SkipsLeadingWhitespace()
    {
        // "  甲乙" then lb|2 at 4. Leading spaces (0,1) are skipped; span starts at 甲 (2).
        var doc = BuildDoc("  甲乙丙", new[]
        {
            new RenderSegment("lb|1", 0, 2),
            new RenderSegment("lb|2", 4, 5),
        });
        doc.TryGetSegmentByKey("lb|1", out var seg1);
        Assert.Equal((2, 4), ReaderLbGeometry.ResolveSingleLbMeaningfulSpan(doc, seg1));
    }

    // ---- FindFirstNonWhitespace ----

    [Theory]
    [InlineData("  AB", 0, 4, 2)]
    [InlineData("AB", 0, 2, 0)]
    [InlineData("   ", 0, 3, -1)]
    [InlineData("", 0, 0, -1)]
    public void FindFirstNonWhitespace_ReturnsFirstNonWsIndex(string text, int start, int end, int expected)
    {
        Assert.Equal(expected, ReaderLbGeometry.FindFirstNonWhitespace(text, start, end));
    }

    [Fact]
    public void FindFirstNonWhitespace_ClampsOutOfRangeBounds()
    {
        // start below 0 and end beyond length are clamped; still finds the 'A' at index 0.
        Assert.Equal(0, ReaderLbGeometry.FindFirstNonWhitespace("A", -5, 99));
    }

    // ---- TryFindSegmentByLb ----

    [Fact]
    public void TryFindSegmentByLb_BareKey_Found()
    {
        var doc = ThreeLineDoc();
        Assert.True(ReaderLbGeometry.TryFindSegmentByLb(doc, "2", out var seg));
        Assert.Equal("lb|2", seg.Key);
    }

    [Fact]
    public void TryFindSegmentByLb_EditionSuffixKey_Found()
    {
        // No bare "lb|0292a27"; only "lb|0292a27|CB" exists. Suffix probe must find it.
        var doc = BuildDoc("AB", new[]
        {
            new RenderSegment("lb|0292a27|CB", 0, 2),
        });
        Assert.True(ReaderLbGeometry.TryFindSegmentByLb(doc, "0292a27", out var seg));
        Assert.Equal("lb|0292a27|CB", seg.Key);
    }

    [Fact]
    public void TryFindSegmentByLb_UncommonSuffix_FoundByBruteForceScan()
    {
        // Suffix "ZZ" is not in the fast-probe list; brute-force parts[1] match must catch it.
        var doc = BuildDoc("AB", new[]
        {
            new RenderSegment("lb|7|ZZ", 0, 2),
        });
        Assert.True(ReaderLbGeometry.TryFindSegmentByLb(doc, "7", out var seg));
        Assert.Equal("lb|7|ZZ", seg.Key);
    }

    [Fact]
    public void TryFindSegmentByLb_NotFound_ReturnsFalse()
    {
        var doc = ThreeLineDoc();
        Assert.False(ReaderLbGeometry.TryFindSegmentByLb(doc, "404", out var seg));
        Assert.Equal(default, seg);
    }

    // ---- ExtractTextBetweenLbs ----

    [Fact]
    public void ExtractTextBetweenLbs_EmptyDoc_ReturnsEmpty()
    {
        Assert.Equal("", ReaderLbGeometry.ExtractTextBetweenLbs(RenderedDocument.Empty, "1", null));
    }

    [Fact]
    public void ExtractTextBetweenLbs_SingleLb_ReturnsLineText()
    {
        var doc = ThreeLineDoc();
        // Single-lb span runs to the next lb Start, so it includes the trailing newline.
        Assert.Equal("甲乙\n", ReaderLbGeometry.ExtractTextBetweenLbs(doc, "1", null));
    }

    [Fact]
    public void ExtractTextBetweenLbs_LastSingleLb_ReturnsFinalLine()
    {
        var doc = ThreeLineDoc();
        Assert.Equal("戊己", ReaderLbGeometry.ExtractTextBetweenLbs(doc, "3", null));
    }

    [Fact]
    public void ExtractTextBetweenLbs_DistinctRange_ReturnsSpannedText()
    {
        var doc = ThreeLineDoc();
        Assert.Equal(ThreeLineText, ReaderLbGeometry.ExtractTextBetweenLbs(doc, "1", "3"));
    }

    [Fact]
    public void ExtractTextBetweenLbs_UnknownFromLb_ReturnsEmpty()
    {
        var doc = ThreeLineDoc();
        Assert.Equal("", ReaderLbGeometry.ExtractTextBetweenLbs(doc, "nope", null));
    }

    // ---- ExtractApparatusForLbRange ----

    private static DocAnnotation Apparatus(int start, string text)
        => new DocAnnotation(start, start + 1, text, kind: "apparatus");

    [Fact]
    public void ExtractApparatusForLbRange_EmptyDoc_ReturnsNull()
    {
        Assert.Null(ReaderLbGeometry.ExtractApparatusForLbRange(RenderedDocument.Empty, "1", null));
    }

    [Fact]
    public void ExtractApparatusForLbRange_NoAnnotations_ReturnsNull()
    {
        var doc = ThreeLineDoc();
        Assert.Null(ReaderLbGeometry.ExtractApparatusForLbRange(doc, "2", null));
    }

    [Fact]
    public void ExtractApparatusForLbRange_UnknownLb_ReturnsNull()
    {
        var doc = ThreeLineDoc(new[] { Apparatus(4, "Lem: 丁\nRdg: 中 [W1]") });
        Assert.Null(ReaderLbGeometry.ExtractApparatusForLbRange(doc, "missing", null));
    }

    [Fact]
    public void ExtractApparatusForLbRange_AnnotationInRange_ReturnsEntry()
    {
        // lb|2 range is [3,6); the apparatus anchored at 4 falls inside it.
        var doc = ThreeLineDoc(new[] { Apparatus(4, "Lem: 丁\nRdg: 中 [W1]") });
        var entries = ReaderLbGeometry.ExtractApparatusForLbRange(doc, "2", "2");
        Assert.NotNull(entries);
        var entry = Assert.Single(entries!);
        Assert.Equal("丁", entry.Lemma);
        Assert.NotNull(entry.Readings);
        var rdg = Assert.Single(entry.Readings!);
        Assert.Equal("W1", rdg.WitnessId);
        Assert.Equal("中", rdg.Reading);
    }

    [Fact]
    public void ExtractApparatusForLbRange_AnnotationOutsideRange_Excluded()
    {
        // Anchor at 7 is past lb|1's single-lb range [0,3); must be excluded.
        var doc = ThreeLineDoc(new[] { Apparatus(7, "Lem: 己 [W1]") });
        Assert.Null(ReaderLbGeometry.ExtractApparatusForLbRange(doc, "1", null));
    }

    [Fact]
    public void ExtractApparatusForLbRange_NonApparatusKind_Ignored()
    {
        var doc = ThreeLineDoc(new[] { new DocAnnotation(4, 5, "just a note", kind: "community") });
        Assert.Null(ReaderLbGeometry.ExtractApparatusForLbRange(doc, "2", null));
    }

    // ---- IsCjkChar ----

    [Theory]
    [InlineData('甲', true)]
    [InlineData('乙', true)]
    [InlineData('A', false)]
    [InlineData('1', false)]
    [InlineData(' ', false)]
    [InlineData('\n', false)]
    public void IsCjkChar_ClassifiesIdeographs(char c, bool expected)
    {
        Assert.Equal(expected, ReaderLbGeometry.IsCjkChar(c));
    }
}
