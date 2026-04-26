// Services/ICitationService.cs
using ReadZen.App.Models;

namespace ReadZen.App.Services;

public interface ICitationService
{
    /// <summary>Generate a formatted citation string.</summary>
    string Generate(CitationMetadata metadata, CitationStyle style);

    /// <summary>Build CitationMetadata from a TextLicenseInfo + passage context.</summary>
    CitationMetadata BuildMetadata(
        TextLicenseInfo? license,
        string? fromLb = null,
        string? toLb = null,
        string? quotedText = null,
        string? translatorName = null);

    /// <summary>
    /// Format CBETA canonical reference from lb value.
    /// "0292c18" with canon=T, vol=48, no=2005 -> "T no. 2005, 48: 292c18"
    /// </summary>
    string? FormatCbetaReference(string? lbValue, string? canon, int? volume, string? number);
}
