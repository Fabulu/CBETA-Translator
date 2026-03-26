using CbetaTranslator.App.Services;
using Xunit;

namespace CbetaTranslator.Tests.Services;

public class XmlReconciliationTests
{
    // ---- ReconcileWithOriginalFormatting ----

    [Fact]
    public void Reconcile_UnchangedXml_ReturnsOriginalVerbatim()
    {
        var original = "<?xml version=\"1.0\"?>\n<root>\n  <child>text</child>\n</root>";
        var newXml   = "<?xml version=\"1.0\"?>\n<root>\n  <child>text</child>\n</root>";

        var result = XmlReconciliationHelper.ReconcileWithOriginalFormatting(original, newXml);

        Assert.Equal(original, result);
    }

    [Fact]
    public void Reconcile_CrlfPreservation_OriginalHasCrlf_OutputPreservesCrlf()
    {
        var original = "<?xml version=\"1.0\"?>\r\n<root>\r\n  <child>text</child>\r\n</root>";
        var newXml   = "<?xml version=\"1.0\"?>\n<root>\n  <child>text</child>\n</root>";

        var result = XmlReconciliationHelper.ReconcileWithOriginalFormatting(original, newXml);

        // Lines are joined with CRLF because original had CRLF
        Assert.Contains("\r\n", result);
        Assert.DoesNotContain("\n\n", result.Replace("\r\n", "CRLF")); // no bare LFs
    }

    [Fact]
    public void Reconcile_LfPreservation_OriginalHasLf_OutputPreservesLf()
    {
        var original = "<?xml version=\"1.0\"?>\n<root>\n  <child>text</child>\n</root>";
        var newXml   = "<?xml version=\"1.0\"?>\r\n<root>\r\n  <child>text</child>\r\n</root>";

        var result = XmlReconciliationHelper.ReconcileWithOriginalFormatting(original, newXml);

        // Lines are joined with LF because original had LF
        Assert.DoesNotContain("\r\n", result);
    }

    [Fact]
    public void Reconcile_ChangedLine_OnlyChangedLineDiffers()
    {
        var original = "<root>\n  <a>old</a>\n  <b>keep</b>\n</root>";
        var newXml   = "<root>\n  <a>new</a>\n  <b>keep</b>\n</root>";

        var result = XmlReconciliationHelper.ReconcileWithOriginalFormatting(original, newXml);

        var lines = result.Split('\n');
        Assert.Equal(4, lines.Length);
        Assert.Equal("<root>", lines[0]);
        Assert.Equal("  <a>new</a>", lines[1]); // changed line uses new content
        Assert.Equal("  <b>keep</b>", lines[2]); // preserved from original
        Assert.Equal("</root>", lines[3]);
    }

    [Fact]
    public void Reconcile_EmptyOriginal_ReturnsNewXmlAsIs()
    {
        var newXml = "<root>\n  <child>text</child>\n</root>";

        var result = XmlReconciliationHelper.ReconcileWithOriginalFormatting("", newXml);

        Assert.Equal(newXml, result);
    }

    [Fact]
    public void Reconcile_NullOriginal_ReturnsNewXmlAsIs()
    {
        var newXml = "<root>\n  <child>text</child>\n</root>";

        var result = XmlReconciliationHelper.ReconcileWithOriginalFormatting(null!, newXml);

        Assert.Equal(newXml, result);
    }

    [Fact]
    public void Reconcile_PreservesOriginalIndentation_WhenContentMatches()
    {
        // Original has tabs, new has spaces - original formatting should win for matching lines
        var original = "<root>\n\t<child>text</child>\n</root>";
        var newXml   = "<root>\n    <child>text</child>\n</root>";

        var result = XmlReconciliationHelper.ReconcileWithOriginalFormatting(original, newXml);

        var lines = result.Split('\n');
        Assert.Equal("\t<child>text</child>", lines[1]); // original tab-indented line preserved
    }

    [Fact]
    public void Reconcile_DuplicateLines_MatchesInOrder()
    {
        var original = "<root>\n  <item/>\n  <item/>\n</root>";
        var newXml   = "<root>\n  <item/>\n  <item/>\n</root>";

        var result = XmlReconciliationHelper.ReconcileWithOriginalFormatting(original, newXml);

        Assert.Equal(original, result);
    }

    // ---- NormalizeForComparison ----

    [Fact]
    public void Normalize_EmptyString_ReturnsEmpty()
    {
        var result = XmlReconciliationHelper.NormalizeForComparison("");
        Assert.Equal("", result);
    }

    [Fact]
    public void Normalize_WhitespaceOnly_ReturnsEmpty()
    {
        Assert.Equal("", XmlReconciliationHelper.NormalizeForComparison("   "));
        Assert.Equal("", XmlReconciliationHelper.NormalizeForComparison("\t\t"));
        Assert.Equal("", XmlReconciliationHelper.NormalizeForComparison("  \t  "));
    }

    [Fact]
    public void Normalize_TrimsLeadingAndTrailing()
    {
        var result = XmlReconciliationHelper.NormalizeForComparison("  <tag>  ");
        Assert.Equal("<tag>", result);
    }

    [Fact]
    public void Normalize_CollapsesInternalWhitespace()
    {
        var result = XmlReconciliationHelper.NormalizeForComparison("  <a>   some   text   </a>  ");
        Assert.Equal("<a> some text </a>", result);
    }

    [Theory]
    [InlineData("\t<tag/>", "<tag/>")]
    [InlineData("    <tag/>", "<tag/>")]
    [InlineData("<tag  attr=\"val\" />", "<tag attr=\"val\" />")]
    public void Normalize_VariousInputs(string input, string expected)
    {
        Assert.Equal(expected, XmlReconciliationHelper.NormalizeForComparison(input));
    }
}
