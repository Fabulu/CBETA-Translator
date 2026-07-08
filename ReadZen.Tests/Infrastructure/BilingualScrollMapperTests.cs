using ReadZen.App.Infrastructure;
using ReadZen.App.Models;
using ReadZen.App.Text;
using Xunit;

namespace ReadZen.Tests.Infrastructure;

/// <summary>
/// Tests for the bilingual scroll-offset mapper (audit P4.3a). Documents are rendered
/// through the real TeiRenderer so segment keys/offsets come from the production
/// pipeline; the ZH and EN sides share lb keys, exactly like the reader panes.
/// </summary>
[Trait("Domain", "Segmentation")]
public class BilingualScrollMapperTests
{
    private const string Header =
        "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
        "<TEI xmlns=\"http://www.tei-c.org/ns/1.0\">" +
        "<teiHeader><fileDesc><titleStmt><title>T</title></titleStmt></fileDesc></teiHeader>" +
        "<text><body><div1>";

    private const string Footer = "</div1></body></text></TEI>";

    private static RenderedDocument Render(string body) => TeiRenderer.Render(Header + body + Footer);

    // ZH: short lines; EN: much longer lines — the classic asymmetric bilingual pair.
    private static readonly RenderedDocument Zh = Render(
        "<p><lb n=\"0001a01\" ed=\"T\"/>甲甲甲甲" +
        "<lb n=\"0001a02\" ed=\"T\"/>乙乙乙乙" +
        "<lb n=\"0001a03\" ed=\"T\"/>丙丙丙丙</p>");

    private static readonly RenderedDocument En = Render(
        "<p><lb n=\"0001a01\" ed=\"T\"/>First English line with much more text than the Chinese." +
        "<lb n=\"0001a02\" ed=\"T\"/>Second English line, also long." +
        "<lb n=\"0001a03\" ed=\"T\"/>Third English line here.</p>");

    [Fact]
    public void MapOffset_SegmentStart_LandsOnPeerSegmentStart()
    {
        Assert.True(Zh.TryGetSegmentByKey("lb|0001a02|T", out var zhSeg));
        Assert.True(En.TryGetSegmentByKey("lb|0001a02|T", out var enSeg));

        var mapped = BilingualScrollMapper.MapOffset(Zh, En, zhSeg.Start);

        Assert.Equal(enSeg.Start, mapped);
    }

    [Fact]
    public void MapOffset_InterpolatesWithinTheSegment()
    {
        Assert.True(Zh.TryGetSegmentByKey("lb|0001a02|T", out var zhSeg));
        Assert.True(En.TryGetSegmentByKey("lb|0001a02|T", out var enSeg));

        int zhMid = zhSeg.Start + (zhSeg.EndExclusive - zhSeg.Start) / 2;
        var mapped = BilingualScrollMapper.MapOffset(Zh, En, zhMid);

        Assert.NotNull(mapped);
        // Proportional: roughly the middle of the (much longer) EN segment,
        // strictly inside it.
        Assert.InRange(mapped!.Value, enSeg.Start + 1, enSeg.EndExclusive - 1);
    }

    [Fact]
    public void MapOffset_IsSymmetricAcrossDirections()
    {
        Assert.True(En.TryGetSegmentByKey("lb|0001a03|T", out var enSeg));
        Assert.True(Zh.TryGetSegmentByKey("lb|0001a03|T", out var zhSeg));

        var mapped = BilingualScrollMapper.MapOffset(En, Zh, enSeg.Start);

        Assert.Equal(zhSeg.Start, mapped);
    }

    [Fact]
    public void MapOffset_MissingKeyInTarget_WalksBackToPrecedingSharedSegment()
    {
        // EN is missing the third lb entirely (untranslated tail).
        var enShort = Render(
            "<p><lb n=\"0001a01\" ed=\"T\"/>First English line." +
            "<lb n=\"0001a02\" ed=\"T\"/>Second English line.</p>");

        Assert.True(Zh.TryGetSegmentByKey("lb|0001a03|T", out var zhSeg3));
        Assert.True(enShort.TryGetSegmentByKey("lb|0001a02|T", out var enSeg2));

        var mapped = BilingualScrollMapper.MapOffset(Zh, enShort, zhSeg3.Start + 2);

        Assert.NotNull(mapped);
        // Fell back to the nearest preceding shared segment (0001a02), anchored at
        // its start (no interpolation across different segments).
        Assert.Equal(enSeg2.Start, mapped);
    }

    [Fact]
    public void MapOffset_EmptyDocuments_ReturnsNull()
    {
        Assert.Null(BilingualScrollMapper.MapOffset(RenderedDocument.Empty, En, 0));
        Assert.Null(BilingualScrollMapper.MapOffset(Zh, RenderedDocument.Empty, 0));
    }

    [Fact]
    public void MapOffset_OffsetBeyondText_ClampsInsteadOfThrowing()
    {
        var mapped = BilingualScrollMapper.MapOffset(Zh, En, int.MaxValue);
        Assert.NotNull(mapped);
        Assert.InRange(mapped!.Value, 0, En.Text.Length);
    }
}
