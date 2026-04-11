// Tests for AttributionFormatter.Plain — the formatter that turns a
// TextLicenseInfo into a clipboard-ready attribution block for the
// "Copy with attribution" context-menu flow.

using ReadZen.App.Models;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

public class AttributionFormatterTests
{
    [Fact]
    public void Plain_NullLicense_ReturnsUnknownNotice()
    {
        var output = AttributionFormatter.Plain(null);
        Assert.Contains("Source license unknown", output);
    }

    [Fact]
    public void Plain_UnknownLicenseClass_ReturnsUnknownNotice()
    {
        var info = new TextLicenseInfo { LicenseClass = LicenseClass.Unknown };
        var output = AttributionFormatter.Plain(info);
        Assert.Contains("Source license unknown", output);
    }

    [Fact]
    public void Plain_FullLicense_IncludesAllFields()
    {
        var info = new TextLicenseInfo
        {
            LicenseClass = LicenseClass.CopyleftAttribution,
            ShortLabel = "CC-BY-SA-4.0",
            Title = "The Gateless Barrier",
            Author = "Wumen Huikai",
            YearComposed = "1228",
            RequiredAttribution = "Wumenguan, public domain. Transcription from Chinese Wikisource (oldid 2648998), CC BY-SA 4.0.",
            StableRevisionUrl = "https://zh.wikisource.org/w/index.php?title=%E7%84%A1%E9%96%80%E9%97%9C&oldid=2648998",
            SourceUrl = "https://zh.wikisource.org/wiki/%E7%84%A1%E9%96%80%E9%97%9C"
        };
        var output = AttributionFormatter.Plain(info);

        Assert.Contains("The Gateless Barrier", output);
        Assert.Contains("Wumen Huikai", output);
        Assert.Contains("1228", output);
        Assert.Contains("CC-BY-SA-4.0", output);
        Assert.Contains("oldid=2648998", output);
        Assert.Contains("Wikisource", output);
    }

    [Fact]
    public void Plain_PrependsQuotedRange_WhenProvided()
    {
        var info = new TextLicenseInfo
        {
            LicenseClass = LicenseClass.PublicDomain,
            ShortLabel = "PD-old",
            Title = "Test"
        };
        var output = AttributionFormatter.Plain(info, "趙州和尚因僧問");
        // Curly quotes around the selection
        Assert.Contains('\u201C', output);
        Assert.Contains('\u201D', output);
        Assert.Contains("趙州和尚因僧問", output);
        Assert.Contains("PD-old", output);
    }

    [Fact]
    public void Plain_NoQuotedRange_DoesNotIncludeCurlyQuotes()
    {
        var info = new TextLicenseInfo
        {
            LicenseClass = LicenseClass.PublicDomain,
            ShortLabel = "PD-old",
            Title = "Test"
        };
        var output = AttributionFormatter.Plain(info);
        Assert.DoesNotContain('\u201C', output);
        Assert.DoesNotContain('\u201D', output);
    }

    [Fact]
    public void Plain_FallsBackToSourceUrl_WhenNoStableRevisionUrl()
    {
        var info = new TextLicenseInfo
        {
            LicenseClass = LicenseClass.PublicDomain,
            ShortLabel = "PD-old",
            Title = "Test",
            SourceUrl = "https://example.org/text",
            StableRevisionUrl = null
        };
        var output = AttributionFormatter.Plain(info);
        Assert.Contains("Source: https://example.org/text", output);
    }
}
