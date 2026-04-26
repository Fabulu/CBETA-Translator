// Services/CitationService.cs
// Central citation formatting engine. All citation surfaces (Reader, Editor,
// Search, SPA) funnel through this service. Phase 1 ships Plain, Chicago, APA,
// MLA, BibTeX, CslJson, and CbetaReference. Phase 2 adds RIS and SBL.
//
// IMPORTANT: This service works WITHOUT the enhanced TextLicenseExtractor
// fields (Phase 1A). It uses whatever fields are available on TextLicenseInfo
// and gracefully handles nulls. When 1A lands, the richer metadata
// (TitleZh, CbetaCanon, CbetaVolume, CbetaNumber, etc.) will flow through
// automatically via BuildMetadata.

using System;
using System.Text;
using System.Text.Json;
using ReadZen.App.Models;

namespace ReadZen.App.Services;

public sealed class CitationService : ICitationService
{
    public string Generate(CitationMetadata m, CitationStyle style)
    {
        return style switch
        {
            CitationStyle.Plain          => FormatPlain(m),
            CitationStyle.Chicago        => FormatChicago(m),
            CitationStyle.Apa            => FormatApa(m),
            CitationStyle.Mla            => FormatMla(m),
            CitationStyle.BibTeX         => FormatBibTeX(m),
            CitationStyle.CslJson        => FormatCslJson(m),
            CitationStyle.CbetaReference => FormatCbetaRefOnly(m),
            _                            => FormatPlain(m),
        };
    }

    public CitationMetadata BuildMetadata(
        TextLicenseInfo? license,
        string? fromLb = null,
        string? toLb = null,
        string? quotedText = null,
        string? translatorName = null)
    {
        if (license == null) return new CitationMetadata();

        // Derive FileId from RelPath if available
        string? fileId = null;
        if (!string.IsNullOrEmpty(license.RelPath))
            fileId = ZenUriParser.RelPathToFileId(license.RelPath);

        // Build shareable URL
        string? shareUrl = null;
        if (!string.IsNullOrEmpty(fileId))
        {
            shareUrl = ZenUriParser.ShareableBase + fileId;
            if (!string.IsNullOrEmpty(fromLb))
                shareUrl += "/" + fromLb;
            if (!string.IsNullOrEmpty(toLb) && toLb != fromLb)
                shareUrl += "-" + toLb;
        }

        // Try to parse CBETA fields from FileId (e.g., "T48n2005")
        string? cbetaCanon = null;
        int? cbetaVolume = null;
        string? cbetaNumber = null;
        if (!string.IsNullOrEmpty(fileId))
            TryParseCbetaFromFileId(fileId, out cbetaCanon, out cbetaVolume, out cbetaNumber);

        return new CitationMetadata
        {
            TitleEn = license.Title,
            Author = license.Author,
            YearComposed = license.YearComposed,
            Corpus = license.Corpus,
            LicenseLabel = license.ShortLabel,
            CbetaCanon = cbetaCanon,
            CbetaVolume = cbetaVolume,
            CbetaNumber = cbetaNumber,
            FileId = fileId,
            FromLb = fromLb,
            ToLb = toLb,
            QuotedText = quotedText,
            TranslatorName = translatorName,
            ShareableUrl = shareUrl,
            SourceUrl = license.SourceUrl,
            RelPath = license.RelPath,
        };
    }

    // ---------------------------------------------------------------
    // CBETA Canonical Reference
    // ---------------------------------------------------------------

    public string? FormatCbetaReference(string? lbValue, string? canon, int? volume, string? number)
    {
        if (string.IsNullOrEmpty(canon) || !volume.HasValue || string.IsNullOrEmpty(number))
            return null;

        // Base reference without page: "T no. 2005, 48"
        var refBase = $"{canon} no. {number}, {volume.Value}";

        if (string.IsNullOrEmpty(lbValue)) return refBase;

        // Parse lb value: "0292c18" -> page=292, col=c, line=18
        var pageRef = FormatLbAsPageRef(lbValue);
        if (pageRef == null) return refBase;

        return $"{refBase}: {pageRef}";
    }

    /// <summary>
    /// Convert lb n-value "0292c18" to page reference "292c18".
    /// Strips leading zeros from the 4-digit page number.
    /// </summary>
    internal static string? FormatLbAsPageRef(string lbValue)
    {
        if (string.IsNullOrEmpty(lbValue) || lbValue.Length < 5) return null;

        // Format: PPPP + column_letter + LL (e.g., 0292c18)
        // Page is first 4 chars, rest is column+line
        var pageStr = lbValue.Substring(0, 4);
        var rest = lbValue.Substring(4); // "c18"

        // Strip leading zeros from page
        var page = pageStr.TrimStart('0');
        if (page.Length == 0) page = "0";

        return page + rest;
    }

    // ---------------------------------------------------------------
    // Style Formatters
    // ---------------------------------------------------------------

    private string FormatPlain(CitationMetadata m)
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(m.QuotedText))
            sb.Append('\u201C').Append(m.QuotedText!.Trim()).Append('\u201D').AppendLine().AppendLine();

        AppendTitleAuthor(sb, m);

        var cbetaRef = FormatCbetaReference(m.FromLb, m.CbetaCanon, m.CbetaVolume, m.CbetaNumber);
        if (cbetaRef != null) sb.Append(cbetaRef).Append(". ");

        if (!string.IsNullOrWhiteSpace(m.LicenseLabel))
            sb.Append(m.LicenseLabel).Append(". ");

        if (!string.IsNullOrWhiteSpace(m.ShareableUrl))
            sb.Append(m.ShareableUrl);

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// Chicago Notes-Bibliography (17th ed.) for Buddhist Studies.
    /// Standard form for primary sources:
    ///   Author, Title ZhTitle [EnTitle],
    ///   CBETA ref. Database. URL.
    /// </summary>
    private string FormatChicago(CitationMetadata m)
    {
        var sb = new StringBuilder();

        // Author with dynasty stripped
        var authorClean = StripDynasty(m.Author, m.Dynasty);
        if (!string.IsNullOrWhiteSpace(authorClean))
        {
            sb.Append(authorClean);
            sb.Append(", ");
        }

        // Title: TitleZh [TitleEn] or just one
        AppendChicagoTitle(sb, m);

        // CBETA canonical reference
        var cbetaRef = FormatCbetaReference(m.FromLb, m.CbetaCanon, m.CbetaVolume, m.CbetaNumber);
        if (cbetaRef != null)
            sb.Append(cbetaRef).Append(". ");

        // Database
        sb.Append("CBETA. ");

        // Translator
        if (!string.IsNullOrWhiteSpace(m.TranslatorName))
            sb.Append("Translated by ").Append(m.TranslatorName).Append(". ");

        // URL
        if (!string.IsNullOrWhiteSpace(m.ShareableUrl))
            sb.Append(m.ShareableUrl);

        return sb.ToString().TrimEnd().TrimEnd('.');
        // Intentionally no trailing period -- Chicago style for online sources
        // ends with the URL without a period.
    }

    /// <summary>
    /// APA 7th edition.
    /// Author. (Year). Title (FileId). Database. URL
    /// </summary>
    private string FormatApa(CitationMetadata m)
    {
        var sb = new StringBuilder();

        var authorClean = StripDynasty(m.Author, m.Dynasty);
        if (!string.IsNullOrWhiteSpace(authorClean))
            sb.Append(authorClean).Append(". ");

        // Year
        if (!string.IsNullOrWhiteSpace(m.YearComposed))
            sb.Append('(').Append(m.YearComposed).Append("). ");
        else if (!string.IsNullOrWhiteSpace(m.Dynasty))
            sb.Append('(').Append(m.Dynasty).Append(" dynasty). ");

        // Title (italicized in APA, but we output plain text)
        var title = m.TitleEn ?? m.TitleZh ?? "Untitled";
        sb.Append(title);

        // CBETA ref in parentheses
        var cbetaRef = FormatCbetaReference(m.FromLb, m.CbetaCanon, m.CbetaVolume, m.CbetaNumber);
        if (cbetaRef != null)
            sb.Append(" (").Append(cbetaRef).Append(')');
        sb.Append(". ");

        // Database + URL
        sb.Append("CBETA. ");
        if (!string.IsNullOrWhiteSpace(m.ShareableUrl))
            sb.Append(m.ShareableUrl);

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// MLA 9th edition.
    /// Author. "Title." Container, CBETA ref, Database, URL.
    /// </summary>
    private string FormatMla(CitationMetadata m)
    {
        var sb = new StringBuilder();

        var authorClean = StripDynasty(m.Author, m.Dynasty);
        if (!string.IsNullOrWhiteSpace(authorClean))
            sb.Append(authorClean).Append(". ");

        // Title in quotes for MLA (work within a collection)
        var title = BuildDisplayTitle(m);
        sb.Append('\u201C').Append(title).Append(".\u201D ");

        // Container (the Tripitaka series)
        if (!string.IsNullOrWhiteSpace(m.SourceEdition))
            sb.Append(m.SourceEdition).Append(", ");
        else if (!string.IsNullOrWhiteSpace(m.SeriesTitle))
            sb.Append(m.SeriesTitle).Append(", ");

        // CBETA ref
        var cbetaRef = FormatCbetaReference(m.FromLb, m.CbetaCanon, m.CbetaVolume, m.CbetaNumber);
        if (cbetaRef != null)
            sb.Append(cbetaRef).Append(", ");

        // Database + URL
        sb.Append("CBETA, ");
        if (!string.IsNullOrWhiteSpace(m.ShareableUrl))
            sb.Append(m.ShareableUrl);

        sb.Append('.');
        return sb.ToString();
    }

    /// <summary>
    /// BibTeX @misc entry.
    /// Phase 1 uses @misc; Phase 2 upgrades to @incollection with booktitle/editor.
    /// </summary>
    private string FormatBibTeX(CitationMetadata m)
    {
        var key = BuildBibTeXKey(m);
        var sb = new StringBuilder();
        sb.AppendLine($"@misc{{{key},");

        var title = BuildDisplayTitle(m);
        sb.AppendLine($"  title = {{{EscapeBibTeX(title)}}},");

        var authorClean = StripDynasty(m.Author, m.Dynasty);
        if (!string.IsNullOrWhiteSpace(authorClean))
            sb.AppendLine($"  author = {{{EscapeBibTeX(authorClean)}}},");

        if (m.CbetaVolume.HasValue)
            sb.AppendLine($"  volume = {{{m.CbetaVolume.Value}}},");
        if (!string.IsNullOrWhiteSpace(m.CbetaNumber))
            sb.AppendLine($"  number = {{{EscapeBibTeX(m.CbetaNumber!)}}},");

        var cbetaRef = FormatCbetaReference(m.FromLb, m.CbetaCanon, m.CbetaVolume, m.CbetaNumber);
        if (cbetaRef != null)
            sb.AppendLine($"  note = {{{EscapeBibTeX(cbetaRef)}}},");

        if (!string.IsNullOrWhiteSpace(m.ShareableUrl))
            sb.AppendLine($"  url = {{{m.ShareableUrl}}},");

        sb.AppendLine($"  publisher = {{CBETA}},");

        if (!string.IsNullOrWhiteSpace(m.CbetaVersionDate) && m.CbetaVersionDate.Length >= 4)
            sb.AppendLine($"  year = {{{m.CbetaVersionDate.Substring(0, 4)}}},");

        sb.AppendLine("}");
        return sb.ToString();
    }

    /// <summary>CSL-JSON single entry.</summary>
    private string FormatCslJson(CitationMetadata m)
    {
        var entry = new System.Collections.Generic.Dictionary<string, object?>();
        entry["id"] = BuildBibTeXKey(m);
        entry["type"] = "chapter"; // work within a collection

        var title = BuildDisplayTitle(m);
        entry["title"] = title;

        var authorClean = StripDynasty(m.Author, m.Dynasty);
        if (!string.IsNullOrWhiteSpace(authorClean))
        {
            entry["author"] = new[]
            {
                new System.Collections.Generic.Dictionary<string, string>
                {
                    ["literal"] = authorClean
                }
            };
        }

        if (!string.IsNullOrWhiteSpace(m.SourceEdition))
            entry["container-title"] = m.SourceEdition;
        if (m.CbetaVolume.HasValue)
            entry["volume"] = m.CbetaVolume.Value.ToString();
        if (!string.IsNullOrWhiteSpace(m.CbetaNumber))
            entry["number"] = m.CbetaNumber;

        var cbetaRef = FormatCbetaReference(m.FromLb, m.CbetaCanon, m.CbetaVolume, m.CbetaNumber);
        if (cbetaRef != null)
            entry["note"] = cbetaRef;

        entry["publisher"] = "CBETA";
        if (!string.IsNullOrWhiteSpace(m.ShareableUrl))
            entry["URL"] = m.ShareableUrl;

        return JsonSerializer.Serialize(new[] { entry },
            new JsonSerializerOptions { WriteIndented = true });
    }

    private string FormatCbetaRefOnly(CitationMetadata m)
    {
        return FormatCbetaReference(m.FromLb, m.CbetaCanon, m.CbetaVolume, m.CbetaNumber)
            ?? "(no CBETA reference available)";
    }

    // ---------------------------------------------------------------
    // Helpers
    // ---------------------------------------------------------------

    private static void AppendTitleAuthor(StringBuilder sb, CitationMetadata m)
    {
        var title = BuildDisplayTitle(m);
        if (!string.IsNullOrWhiteSpace(title))
        {
            sb.Append(title);
            var authorClean = StripDynasty(m.Author, m.Dynasty);
            if (!string.IsNullOrWhiteSpace(authorClean))
                sb.Append(", ").Append(authorClean);
            sb.Append(". ");
        }
    }

    private static void AppendChicagoTitle(StringBuilder sb, CitationMetadata m)
    {
        // Buddhist Studies convention: ZhTitle [EnTitle]
        if (!string.IsNullOrWhiteSpace(m.TitleZh) && !string.IsNullOrWhiteSpace(m.TitleEn))
        {
            sb.Append(m.TitleZh).Append(" [").Append(m.TitleEn).Append("], ");
        }
        else if (!string.IsNullOrWhiteSpace(m.TitleZh))
        {
            sb.Append(m.TitleZh).Append(", ");
        }
        else if (!string.IsNullOrWhiteSpace(m.TitleEn))
        {
            sb.Append(m.TitleEn).Append(", ");
        }
    }

    private static string BuildDisplayTitle(CitationMetadata m)
    {
        if (!string.IsNullOrWhiteSpace(m.TitleEn) && !string.IsNullOrWhiteSpace(m.TitleZh))
            return $"{m.TitleZh} ({m.TitleEn})";
        return m.TitleEn ?? m.TitleZh ?? "Untitled";
    }

    /// <summary>Remove dynasty prefix from author for citation display.</summary>
    private static string? StripDynasty(string? author, string? dynasty)
    {
        if (string.IsNullOrWhiteSpace(author)) return null;
        if (string.IsNullOrEmpty(dynasty)) return author;
        if (author.StartsWith(dynasty, StringComparison.Ordinal))
        {
            var rest = author.Substring(dynasty.Length).TrimStart(' ', '\u3000');
            return string.IsNullOrEmpty(rest) ? author : rest;
        }
        return author;
    }

    private static string BuildBibTeXKey(CitationMetadata m)
    {
        var fileId = m.FileId ?? "unknown";
        var lb = m.FromLb ?? "full";
        return $"cbeta:{fileId}:{lb}";
    }

    private static string EscapeBibTeX(string value)
    {
        return value.Replace("{", "\\{").Replace("}", "\\}");
    }

    /// <summary>
    /// Try to parse CBETA canon/volume/number from a FileId like "T48n2005".
    /// This provides a fallback when the enhanced TextLicenseExtractor (1A) has
    /// not yet been integrated.
    /// </summary>
    private static void TryParseCbetaFromFileId(string fileId, out string? canon, out int? volume, out string? number)
    {
        canon = null;
        volume = null;
        number = null;

        if (string.IsNullOrEmpty(fileId)) return;

        // Match pattern like "T48n2005", "X70n1363", "J26nB180"
        // Canon = letter(s), Volume = digits before 'n', Number = digits after 'n'
        int nIdx = fileId.IndexOf('n');
        if (nIdx < 2) return; // need at least 1 canon char + 1 vol digit

        // Find where digits start (after canon letter(s))
        int volStart = 0;
        for (int i = 0; i < nIdx; i++)
        {
            if (char.IsDigit(fileId[i]))
            {
                volStart = i;
                break;
            }
        }
        if (volStart == 0) return; // no canon prefix found

        var canonStr = fileId.Substring(0, volStart);
        var volStr = fileId.Substring(volStart, nIdx - volStart);
        var numStr = fileId.Substring(nIdx + 1);

        if (int.TryParse(volStr, out var vol) && !string.IsNullOrEmpty(numStr))
        {
            canon = canonStr;
            volume = vol;
            number = numStr;
        }
    }
}
