using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

/// <summary>
/// Guards the display-scope fix in the master-corpus baker: mention counts (and snippets) must be
/// taken over the SAME content the reader displays, i.e. with the elements the TEI parser suppresses
/// removed — &lt;teiHeader&gt;, &lt;note&gt; (incl. place="inline"), &lt;cb:mulu&gt; (a nav duplicate of
/// &lt;head&gt;), and &lt;rdg&gt; variant readings. Before the fix the baker substring-counted the whole
/// file, so a biographical subject named in a cb:mulu+head pair counted 2 but the reader could only
/// surface 1 (the "N× shows N-1 passages" / "1× shows nothing" bug on e.g. 密庵咸傑).
/// </summary>
public class MasterCorpusDisplayScopeTests
{
    private static int DisplayCount(string xml, string name) =>
        MasterCorpusSearchService.CountOccurrences(
            MasterCorpusSearchService.BuildDisplayContent(xml), name);

    [Fact]
    public void MuluDuplicateOfHead_CountsOnce()
    {
        // CBETA gives a biographical subject a <cb:mulu> table-of-contents entry that duplicates the
        // adjacent <head>. The reader strips the mulu; the baker must too, or it double-counts one mention.
        var xml = @"<TEI><text><body><cb:div><cb:mulu type=""其他"">慶元府天童密庵咸傑禪師</cb:mulu><head>慶元府天童密庵咸傑禪師</head><p>師云</p></cb:div></body></text></TEI>";
        Assert.Equal(1, DisplayCount(xml, "密庵咸傑"));
    }

    [Fact]
    public void NameOnlyInNote_CountsZero()
    {
        // Notes (including place="inline") are editorial commentary the reader suppresses.
        var xml = @"<TEI><text><body><p>甲<note place=""inline"">三峯藏云密庵咸傑禪師語</note>乙</p></body></text></TEI>";
        Assert.Equal(0, DisplayCount(xml, "密庵咸傑"));
    }

    [Fact]
    public void NameInVariantReading_Excluded_ButLemKept()
    {
        // <app>: the reader renders <lem> (base reading) and suppresses <rdg> (variant readings).
        var xml = @"<TEI><text><body><p><app><lem>正字</lem><rdg wit=""#a"">密庵咸傑</rdg></app></p></body></text></TEI>";
        Assert.Equal(0, DisplayCount(xml, "密庵咸傑"));
    }

    [Fact]
    public void NameInTeiHeader_Excluded()
    {
        // Header/author metadata is not a displayable body passage.
        var xml = @"<TEI><teiHeader><author>密庵咸傑</author></teiHeader><text><body><p>正文</p></body></text></TEI>";
        Assert.Equal(0, DisplayCount(xml, "密庵咸傑"));
    }

    [Fact]
    public void PlainBodyOccurrences_AllCounted()
    {
        var xml = @"<TEI><text><body><p>密庵咸傑云。又問密庵咸傑。</p></body></text></TEI>";
        Assert.Equal(2, DisplayCount(xml, "密庵咸傑"));
    }

    [Fact]
    public void RealPattern_MuluPlusHeadPlusNote_CountsOne()
    {
        // Mirrors GA031n0032: mulu + head + one note = 3 raw substring hits, 1 displayable passage.
        var xml = @"<cb:mulu>密庵咸傑禪師</cb:mulu><head>密庵咸傑禪師</head><note>此密庵咸傑條</note><p>正文</p>";
        Assert.Equal(1, DisplayCount(xml, "密庵咸傑"));
    }

    [Fact]
    public void CleanBody_IsUnchanged()
    {
        // No suppressed elements: display scope must equal the raw content.
        var xml = @"<p>庭前柏樹子</p>";
        Assert.Equal(xml, MasterCorpusSearchService.BuildDisplayContent(xml));
    }
}
