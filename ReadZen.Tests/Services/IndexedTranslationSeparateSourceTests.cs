using System.Linq;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

/// <summary>
/// Gap coverage for two v8.0.0..HEAD changes in <see cref="IndexedTranslationService"/> that
/// no existing test asserted:
///
/// 1. <c>HasSeparateTranslatedSource</c> is now computed in <c>BuildIndex</c> over the
///    already-parsed documents with an ORDINAL fast path plus an XML-equivalence fallback
///    (<c>SaveOptions.DisableFormatting</c>) — the audit P2.4 / R2-M7 de-quadratic-parse fix.
///    It gates EN population in the projection (only a genuinely separate translated source
///    fills EN lines), so its truth table is load-bearing and was untested.
///
/// 2. A SUCCESSFUL <c>BuildTranslatedXml</c> now leaves <c>LastBuildTranslatedXmlDebugDumpPath</c>
///    empty (dump kept in memory only; the per-save %TEMP% write was removed — audit P2.1 / R2-H3).
///
/// A third test pins the invariant that the post-save recompute of the flag agrees with a
/// fresh <c>BuildIndex</c> on the same strings, since the two code paths compute the same
/// invariant with two separate implementations.
/// </summary>
public class IndexedTranslationSeparateSourceTests
{
    private const string TeiOpen =
        "<?xml version=\"1.0\" encoding=\"UTF-8\"?>" +
        "<TEI xmlns=\"http://www.tei-c.org/ns/1.0\" xmlns:cb=\"http://www.cbeta.org/ns/1.0\">" +
        "<teiHeader><fileDesc><titleStmt><title>T</title></titleStmt></fileDesc></teiHeader>" +
        "<text><body>";

    private const string TeiClose = "</body></text></TEI>";

    private static string Wrap(string body) => TeiOpen + body + TeiClose;

    // ------------------------------------------------ HasSeparateTranslatedSource truth table

    [Fact]
    public void BuildIndex_IdenticalStrings_HasNoSeparateSource()
    {
        // The ubiquitous "untranslated copy" case (BuildIndex(orig, orig)) hits the ordinal
        // fast path and reports no separate source.
        var svc = new IndexedTranslationService();
        var orig = Wrap("<p>甲乙丙丁</p>");

        var doc = svc.BuildIndex(orig, orig);

        Assert.False(doc.HasSeparateTranslatedSource);
    }

    [Fact]
    public void BuildIndex_NullOrWhitespaceTranslated_FallsBackToOriginal_NoSeparateSource()
    {
        var svc = new IndexedTranslationService();
        var orig = Wrap("<p>甲乙丙丁</p>");

        Assert.False(svc.BuildIndex(orig, null).HasSeparateTranslatedSource);
        Assert.False(svc.BuildIndex(orig, "   ").HasSeparateTranslatedSource);
    }

    [Fact]
    public void BuildIndex_EmptyOriginal_EarlyReturns_NoSeparateSource()
    {
        var svc = new IndexedTranslationService();
        Assert.False(svc.BuildIndex("", "<TEI/>").HasSeparateTranslatedSource);
        Assert.False(svc.BuildIndex("   ", "<TEI/>").HasSeparateTranslatedSource);
    }

    [Fact]
    public void BuildIndex_XmlEquivalentButFormattingDiffers_HasNoSeparateSource()
    {
        // The subtle new branch: the two strings differ ordinally (attribute quoted with a
        // single quote, plus insignificant whitespace inside the start tag) but are
        // XML-equivalent — XDocument reserializes both to a double-quoted, whitespace-normalized
        // start tag — so the DisableFormatting fallback must still report no separate source.
        var svc = new IndexedTranslationService();
        var orig = Wrap("<p type=\"x\">甲乙丙丁</p>");
        var reformatted = Wrap("<p  type='x'>甲乙丙丁</p>"); // single quotes + extra spaces in the tag

        var doc = svc.BuildIndex(orig, reformatted);

        Assert.NotEqual(orig, reformatted); // genuinely differ ordinally...
        Assert.False(doc.HasSeparateTranslatedSource); // ...but equivalent → not separate
    }

    [Fact]
    public void BuildIndex_GenuinelyDifferentTranslatedContent_HasSeparateSource()
    {
        var svc = new IndexedTranslationService();
        var orig = Wrap("<p>甲乙丙丁</p>");
        var translated = Wrap("<p>Rendered English</p>");

        var doc = svc.BuildIndex(orig, translated);

        Assert.True(doc.HasSeparateTranslatedSource);
    }

    [Fact]
    public void BuildIndex_SeparateSource_PopulatesEnProjectionFromTranslatedDoc()
    {
        // The behavioral consequence of the flag: with a genuinely separate translated
        // source, the body unit's EN is drawn from the translated document.
        var svc = new IndexedTranslationService();
        var orig = Wrap("<p>甲乙丙丁</p>");
        var translated = Wrap("<p>Rendered English</p>");

        var doc = svc.BuildIndex(orig, translated);

        var body = doc.Units.Single(u => u.Kind == TranslationUnitKind.Body);
        Assert.Equal("甲乙丙丁", body.Zh);
        Assert.Equal("Rendered English", body.En);
    }

    // ------------------------------------------------ successful save leaves no debug-dump path

    [Fact]
    public void BuildTranslatedXml_SuccessfulSave_LeavesDebugDumpPathEmpty_ButKeepsInMemoryDump()
    {
        var svc = new IndexedTranslationService();
        var orig = Wrap("<p>甲乙丙丁</p>");

        var doc = svc.BuildIndex(orig, orig);
        var unit = doc.Units.Single(u => u.Kind == TranslationUnitKind.Body);
        unit.En = "HELLO";
        unit.IsDirty = true;

        var result = svc.BuildTranslatedXml(doc, out var updated);

        Assert.Equal(1, updated);
        Assert.Contains("HELLO", result);
        // No %TEMP% litter on the success path...
        Assert.Equal("", svc.LastBuildTranslatedXmlDebugDumpPath);
        // ...but the dump is still available in memory for diagnostics.
        Assert.False(string.IsNullOrEmpty(svc.LastBuildTranslatedXmlDebugDump));
    }

    // ------------------------------------------------ two computation paths agree

    [Fact]
    public void SeparateSourceFlag_PostSave_MatchesFreshBuildIndex()
    {
        // BuildIndex computes the flag with ordinal + DisableFormatting; BuildTranslatedXml
        // recomputes it at the end via !XmlEquivalent(...). The two must agree so the flag can
        // never drift depending on which path last touched the doc.
        var svc = new IndexedTranslationService();
        var orig = Wrap("<p>甲乙丙丁</p>");

        var doc = svc.BuildIndex(orig, orig);
        var unit = doc.Units.Single(u => u.Kind == TranslationUnitKind.Body);
        unit.En = "A translated line";
        unit.IsDirty = true;

        var saved = svc.BuildTranslatedXml(doc, out _);

        // After the save the doc now genuinely has a separate translated body.
        Assert.True(doc.HasSeparateTranslatedSource);

        // A fresh index over the same (orig, saved) strings must agree.
        var fresh = svc.BuildIndex(orig, saved);
        Assert.Equal(fresh.HasSeparateTranslatedSource, doc.HasSeparateTranslatedSource);
    }
}
