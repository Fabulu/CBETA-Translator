// Tests for TextLicenseExtractor — the parser that turns a TEI file's
// <teiHeader>/<publicationStmt>/<availability> block into a TextLicenseInfo.
//
// The first test reads the canonical OpenZenTexts Wumenguan file directly
// from C:\programmieren\OpenZenTexts. It's gated on File.Exists so CI on
// other machines (where that path doesn't exist) skips the assertion
// without failing — the rest of the suite uses inline XML strings.

using System.IO;
using ReadZen.App.Models;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

public class TextLicenseExtractorTests
{
    private const string OpenZenTextsWumenguanPath =
        @"C:\programmieren\OpenZenTexts\xml-open\ws\gateless-barrier\gateless-barrier.xml";

    [Fact]
    public void Extract_ReturnsNull_ForEmptyOrWhitespace()
    {
        Assert.Null(TextLicenseExtractor.Extract(""));
        Assert.Null(TextLicenseExtractor.Extract("   "));
    }

    [Fact]
    public void Extract_ReturnsNull_ForMalformedXml()
    {
        Assert.Null(TextLicenseExtractor.Extract("<not-closed"));
    }

    [Fact]
    public void Extract_ReturnsNull_WhenTeiHeaderMissing()
    {
        const string xml = @"<TEI xmlns=""http://www.tei-c.org/ns/1.0""><text><body/></text></TEI>";
        Assert.Null(TextLicenseExtractor.Extract(xml));
    }

    [Fact]
    public void Extract_ReturnsUnknownDefault_WhenAvailabilityMissing()
    {
        const string xml = @"<TEI xmlns=""http://www.tei-c.org/ns/1.0"">
            <teiHeader><fileDesc><titleStmt><title>Test</title></titleStmt><publicationStmt><p>nothing here</p></publicationStmt><sourceDesc><p>x</p></sourceDesc></fileDesc></teiHeader>
            <text><body/></text>
        </TEI>";
        var info = TextLicenseExtractor.Extract(xml);
        Assert.NotNull(info);
        Assert.Equal(LicenseClass.Unknown, info!.LicenseClass);
        Assert.Equal("Unknown", info.ShortLabel);
    }

    [Fact]
    public void Extract_ClassifiesCcBySa_AsCopyleftAttribution()
    {
        const string xml = @"<TEI xmlns=""http://www.tei-c.org/ns/1.0"">
            <teiHeader><fileDesc>
                <titleStmt><title>Test</title></titleStmt>
                <publicationStmt>
                    <availability>
                        <licence target=""https://creativecommons.org/licenses/by-sa/4.0/"">
                            <p>Available under CC BY-SA 4.0.</p>
                        </licence>
                    </availability>
                </publicationStmt>
                <sourceDesc><p>x</p></sourceDesc>
            </fileDesc></teiHeader>
            <text><body/></text>
        </TEI>";
        var info = TextLicenseExtractor.Extract(xml);
        Assert.NotNull(info);
        Assert.Equal(LicenseClass.CopyleftAttribution, info!.LicenseClass);
        Assert.Equal("CC-BY-SA-4.0", info.ShortLabel);
        Assert.True(info.AttributionRequired);
        Assert.True(info.ShareAlikeRequired);
        Assert.True(info.CommercialUseAllowed);
    }

    [Fact]
    public void Extract_ClassifiesCbetaNonCommercial_WithCbetaNcLabel()
    {
        const string xml = @"<TEI xmlns=""http://www.tei-c.org/ns/1.0"">
            <teiHeader><fileDesc>
                <titleStmt><title>Test</title></titleStmt>
                <publicationStmt>
                    <availability>
                        <p>Available for non-commercial use only when distributed with this CBETA header intact.</p>
                    </availability>
                </publicationStmt>
                <sourceDesc><p>x</p></sourceDesc>
            </fileDesc></teiHeader>
            <text><body/></text>
        </TEI>";
        var info = TextLicenseExtractor.Extract(xml);
        Assert.NotNull(info);
        Assert.Equal(LicenseClass.NonCommercial, info!.LicenseClass);
        Assert.Equal("CBETA-NC", info.ShortLabel);
        Assert.False(info.CommercialUseAllowed);
        Assert.Equal(CorpusKind.Cbeta, info.Corpus);
    }

    [Fact]
    public void Extract_ClassifiesGenericNonCommercial_WithoutCbetaMarker()
    {
        // Text says non-commercial but doesn't mention CBETA — should land
        // in the generic non-commercial branch with the human-readable label.
        const string xml = @"<TEI xmlns=""http://www.tei-c.org/ns/1.0"">
            <teiHeader><fileDesc>
                <titleStmt><title>Test</title></titleStmt>
                <publicationStmt>
                    <availability>
                        <p>Released for non-commercial educational use only.</p>
                    </availability>
                </publicationStmt>
                <sourceDesc><p>x</p></sourceDesc>
            </fileDesc></teiHeader>
            <text><body/></text>
        </TEI>";
        var info = TextLicenseExtractor.Extract(xml);
        Assert.NotNull(info);
        Assert.Equal(LicenseClass.NonCommercial, info!.LicenseClass);
        Assert.Equal("Non-commercial", info.ShortLabel);
        Assert.False(info.CommercialUseAllowed);
    }

    [Fact]
    public void Extract_OpenZenTextsHeaderMentioningCbeta_ClassifiesAsOpen()
    {
        // Regression for QA finding B4: an OpenZenTexts file's header
        // legitimately mentions "CBETA" in disclaimers ("excludes CBETA-
        // derived material", "never to collide with CBETA notation").
        // The previous classifier inferred Cbeta from any "CBETA" substring;
        // the corrected classifier should detect the noCbeta marker and
        // classify as Open.
        const string xml = @"<TEI xmlns=""http://www.tei-c.org/ns/1.0"">
            <teiHeader><fileDesc>
                <titleStmt><title xml:lang=""en"">Test</title></titleStmt>
                <publicationStmt>
                    <availability>
                        <licence target=""https://creativecommons.org/licenses/by-sa/4.0/"">
                            <p>Available under CC BY-SA 4.0.</p>
                        </licence>
                        <p>This file excludes CBETA-derived material; the line identifiers never collide with CBETA woodblock notation.</p>
                        <p><label>Provenance check:</label> The captured source package shows no CBETA marker.</p>
                    </availability>
                </publicationStmt>
                <sourceDesc><p>x</p></sourceDesc>
            </fileDesc></teiHeader>
            <text><body/></text>
        </TEI>";
        var info = TextLicenseExtractor.Extract(xml);
        Assert.NotNull(info);
        Assert.Equal(CorpusKind.Open, info!.Corpus);
        Assert.True(info.NoCbetaMaterial);
        Assert.Equal(LicenseClass.CopyleftAttribution, info.LicenseClass);
    }

    [Fact]
    public void Extract_CbetaFileWithIdnoMarker_ClassifiesAsCbeta()
    {
        // Regression for QA finding B4: a real CBETA file should still
        // classify as Cbeta via the structural <idno type="CBETA"> marker
        // even if the availability text doesn't mention "non-commercial"
        // explicitly (e.g. headers that defer to a separate license file).
        const string xml = @"<TEI xmlns=""http://www.tei-c.org/ns/1.0"">
            <teiHeader><fileDesc>
                <titleStmt><title>Sample CBETA Text</title></titleStmt>
                <publicationStmt>
                    <idno type=""CBETA"">T48n2005</idno>
                    <availability>
                        <p>See https://www.cbeta.org/ for terms.</p>
                    </availability>
                </publicationStmt>
                <sourceDesc><p>x</p></sourceDesc>
            </fileDesc></teiHeader>
            <text><body/></text>
        </TEI>";
        var info = TextLicenseExtractor.Extract(xml);
        Assert.NotNull(info);
        Assert.Equal(CorpusKind.Cbeta, info!.Corpus);
    }

    [Fact]
    public void Extract_DetectsNoCbetaMaterial_FromAvailabilityText()
    {
        const string xml = @"<TEI xmlns=""http://www.tei-c.org/ns/1.0"">
            <teiHeader><fileDesc>
                <titleStmt><title xml:lang=""en"">Demo</title></titleStmt>
                <publicationStmt>
                    <availability>
                        <licence target=""https://creativecommons.org/publicdomain/mark/1.0/"">
                            <p>Public domain (PD-old).</p>
                        </licence>
                        <p><label>Provenance check:</label> The captured source package shows no CBETA marker.</p>
                    </availability>
                </publicationStmt>
                <sourceDesc><p>x</p></sourceDesc>
            </fileDesc></teiHeader>
            <text><body/></text>
        </TEI>";
        var info = TextLicenseExtractor.Extract(xml);
        Assert.NotNull(info);
        Assert.True(info!.NoCbetaMaterial);
        Assert.Equal(CorpusKind.Open, info.Corpus);
        Assert.Equal(LicenseClass.PublicDomain, info.LicenseClass);
    }

    [Fact]
    public void Extract_OpenZenTextsWumenguan_RoundTrip()
    {
        if (!File.Exists(OpenZenTextsWumenguanPath))
            return; // skip on machines without the OpenZenTexts clone

        var xml = File.ReadAllText(OpenZenTextsWumenguanPath);
        var info = TextLicenseExtractor.Extract(xml, "ws/gateless-barrier/gateless-barrier.xml");

        Assert.NotNull(info);
        Assert.NotNull(info!.Title);
        Assert.Contains("Gateless", info.Title!);
        Assert.NotNull(info.Author);
        Assert.Contains("Wumen", info.Author!);
        Assert.Equal("1228", info.YearComposed);

        Assert.Equal(LicenseClass.CopyleftAttribution, info.LicenseClass);
        Assert.Equal("CC-BY-SA-4.0", info.ShortLabel);
        Assert.True(info.AttributionRequired);
        Assert.True(info.ShareAlikeRequired);
        Assert.True(info.CommercialUseAllowed);
        Assert.True(info.NoCbetaMaterial);
        Assert.Equal(CorpusKind.Open, info.Corpus);

        Assert.NotNull(info.SourceUrl);
        Assert.Contains("wikisource", info.SourceUrl!);

        Assert.NotNull(info.StableRevisionUrl);
        Assert.Contains("oldid=2648998", info.StableRevisionUrl!);

        Assert.Equal("high", info.VettingConfidence);
    }
}
