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
using ReadZen.App.Infrastructure;
using ReadZen.App.Models;

namespace ReadZen.App.Services;

public sealed class CitationService : ICitationService
{
    // NOTE (audit P3.2 / R3-M1): a mutable public static `DefaultStyleIndex` plus a
    // static `GetPreferredStyle()` used to live here. They were dead — the only writer
    // (MainWindow's config-loaded handler) wrote to state nothing read, and the real
    // preferred-style resolution goes through CitationMenuHelper reading AppConfig
    // directly. Removed so this DI-registered service carries no ambient global state.

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
            CitationStyle.Ris            => FormatRis(m),
            CitationStyle.Sbl            => FormatSbl(m),
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

    /// <summary>
    /// Delegates to <see cref="CbetaReferenceHelper.FormatCbetaReference"/>.
    /// Kept as instance method so existing callers (Reader, Editor, Search) are unaffected.
    /// </summary>
    public string? FormatCbetaReference(string? lbValue, string? canon, int? volume, string? number)
        => CbetaReferenceHelper.FormatCbetaReference(lbValue, canon, volume, number);

    /// <summary>
    /// Delegates to <see cref="CbetaReferenceHelper.FormatLbAsPageRef"/>.
    /// Kept internal for any existing test references.
    /// </summary>
    internal static string? FormatLbAsPageRef(string? lbValue)
        => CbetaReferenceHelper.FormatLbAsPageRef(lbValue);

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

        // Translator attribution (APA: after title in parentheses)
        if (!string.IsNullOrWhiteSpace(m.TranslatorName))
            sb.Append(" (Trans. ").Append(m.TranslatorName).Append(')');

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

        // Translator attribution (MLA: after title)
        if (!string.IsNullOrWhiteSpace(m.TranslatorName))
            sb.Append("Trans. ").Append(m.TranslatorName).Append(". ");

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
    /// BibTeX entry. Uses @incollection with Taisho booktitle/editor when CBETA
    /// provenance is available (CbetaCanon + CbetaVolume). Falls back to @misc
    /// for OpenZen texts and texts without CBETA provenance.
    /// </summary>
    private string FormatBibTeX(CitationMetadata m)
    {
        var key = BuildBibTeXKey(m);
        var sb = new StringBuilder();

        bool hasCbetaProvenance = !string.IsNullOrWhiteSpace(m.CbetaCanon) && m.CbetaVolume.HasValue;

        // @incollection for CBETA texts, @misc for everything else
        var entryType = hasCbetaProvenance ? "incollection" : "misc";
        sb.AppendLine($"@{entryType}{{{key},");

        var title = BuildDisplayTitle(m);
        sb.AppendLine($"  title = {{{EscapeBibTeX(title)}}},");

        var authorClean = StripDynasty(m.Author, m.Dynasty);
        if (!string.IsNullOrWhiteSpace(authorClean))
            sb.AppendLine($"  author = {{{EscapeBibTeX(authorClean)}}},");

        if (!string.IsNullOrWhiteSpace(m.TranslatorName))
            sb.AppendLine($"  translator = {{{EscapeBibTeX(m.TranslatorName!)}}},");

        if (hasCbetaProvenance)
        {
            // Taisho-specific fields
            var booktitle = m.SourceEdition ?? "Taish\\={o} shinsh\\={u} daiz\\={o}ky\\={o}";
            sb.AppendLine($"  booktitle = {{{EscapeBibTeX(booktitle)}}},");
            sb.AppendLine($"  editor = {{Takakusu Junjir\\=o and Watanabe Kaikyoku}},");
            sb.AppendLine($"  volume = {{{m.CbetaVolume!.Value}}},");
            if (!string.IsNullOrWhiteSpace(m.CbetaNumber))
                sb.AppendLine($"  number = {{{EscapeBibTeX(m.CbetaNumber!)}}},");
        }
        else
        {
            if (m.CbetaVolume.HasValue)
                sb.AppendLine($"  volume = {{{m.CbetaVolume.Value}}},");
            if (!string.IsNullOrWhiteSpace(m.CbetaNumber))
                sb.AppendLine($"  number = {{{EscapeBibTeX(m.CbetaNumber!)}}},");
        }

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

        if (!string.IsNullOrWhiteSpace(m.TranslatorName))
        {
            entry["translator"] = new[]
            {
                new System.Collections.Generic.Dictionary<string, string>
                {
                    ["family"] = m.TranslatorName!,
                    ["literal"] = m.TranslatorName!
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

    /// <summary>
    /// RIS format for single citation. TY=BOOK for whole works.
    /// Tags: TY, AU, TI, PY, VL, SP, EP, PB, UR, DB, KW, M1, N1, A4, ER.
    /// </summary>
    private string FormatRis(CitationMetadata m)
    {
        var sb = new StringBuilder();
        sb.Append("TY  - BOOK\r\n");

        // Author
        var authorClean = StripDynasty(m.Author, m.Dynasty);
        if (!string.IsNullOrWhiteSpace(authorClean))
            sb.Append("AU  - ").Append(authorClean).Append("\r\n");

        // Title: "ZhTitle [EnTitle]"
        var title = BuildRisTitle(m);
        sb.Append("TI  - ").Append(title).Append("\r\n");

        // Year
        if (!string.IsNullOrWhiteSpace(m.YearComposed))
            sb.Append("PY  - ").Append(m.YearComposed).Append("\r\n");

        // Volume
        if (m.CbetaVolume.HasValue)
            sb.Append("VL  - ").Append(m.CbetaVolume.Value).Append("\r\n");

        // Start/end page from lb
        var pageRef = !string.IsNullOrEmpty(m.FromLb) ? FormatLbAsPageRef(m.FromLb) : null;
        if (pageRef != null)
            sb.Append("SP  - ").Append(pageRef).Append("\r\n");
        var endRef = !string.IsNullOrEmpty(m.ToLb) ? FormatLbAsPageRef(m.ToLb) : null;
        if (endRef != null)
            sb.Append("EP  - ").Append(endRef).Append("\r\n");

        sb.Append("PB  - CBETA\r\n");

        if (!string.IsNullOrWhiteSpace(m.ShareableUrl))
            sb.Append("UR  - ").Append(m.ShareableUrl).Append("\r\n");

        sb.Append("DB  - CBETA\r\n");

        // CBETA ref in M1 and N1
        var cbetaRef = FormatCbetaReference(m.FromLb, m.CbetaCanon, m.CbetaVolume, m.CbetaNumber);
        if (!string.IsNullOrWhiteSpace(m.FileId))
            sb.Append("M1  - ").Append(m.FileId).Append("\r\n");
        if (cbetaRef != null)
            sb.Append("N1  - ").Append(cbetaRef).Append("\r\n");

        // Translator in A4
        if (!string.IsNullOrWhiteSpace(m.TranslatorName))
            sb.Append("A4  - ").Append(m.TranslatorName).Append("\r\n");

        sb.Append("ER  - \r\n");
        return sb.ToString();
    }

    private static string BuildRisTitle(CitationMetadata m)
    {
        if (!string.IsNullOrWhiteSpace(m.TitleZh) && !string.IsNullOrWhiteSpace(m.TitleEn))
            return $"{m.TitleZh} [{m.TitleEn}]";
        return m.TitleEn ?? m.TitleZh ?? "Untitled";
    }

    /// <summary>
    /// Society of Biblical Literature (SBL) style, adapted for Buddhist Studies.
    /// Footnote: Author, Title ZhTitle, CBETA ref.
    /// Bibliography: Author ZhAuthor. Title ZhTitle. CBETA ref, vol.
    /// </summary>
    private string FormatSbl(CitationMetadata m)
    {
        var sb = new StringBuilder();

        var authorClean = StripDynasty(m.Author, m.Dynasty);
        if (!string.IsNullOrWhiteSpace(authorClean))
        {
            sb.Append(authorClean);
            // Add Chinese name if available (SBL convention for non-Latin authors)
            if (!string.IsNullOrWhiteSpace(m.Dynasty) && !string.IsNullOrWhiteSpace(m.Author))
            {
                var zhName = m.Author; // Author field often has Chinese name
                if (zhName != authorClean)
                    sb.Append(' ').Append(zhName);
            }
            sb.Append(", ");
        }

        // Title: TitleEn TitleZh
        if (!string.IsNullOrWhiteSpace(m.TitleEn) && !string.IsNullOrWhiteSpace(m.TitleZh))
            sb.Append(m.TitleEn).Append(' ').Append(m.TitleZh);
        else if (!string.IsNullOrWhiteSpace(m.TitleEn))
            sb.Append(m.TitleEn);
        else if (!string.IsNullOrWhiteSpace(m.TitleZh))
            sb.Append(m.TitleZh);

        // Translator
        if (!string.IsNullOrWhiteSpace(m.TranslatorName))
            sb.Append(", trans. ").Append(m.TranslatorName);

        sb.Append(", ");

        // CBETA canonical reference (passage-level)
        var cbetaRef = FormatCbetaReference(m.FromLb, m.CbetaCanon, m.CbetaVolume, m.CbetaNumber);
        if (cbetaRef != null)
            sb.Append(cbetaRef).Append('.');
        else if (m.CbetaCanon != null && m.CbetaVolume.HasValue && !string.IsNullOrEmpty(m.CbetaNumber))
            sb.Append(m.CbetaCanon).Append(" no. ").Append(m.CbetaNumber).Append(", vol. ").Append(m.CbetaVolume.Value).Append('.');
        else
            sb.Append("CBETA.");

        return sb.ToString().TrimEnd();
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

    /// <summary>
    /// Delegates to <see cref="CbetaReferenceHelper.EscapeBibTeX"/> (5-replacement version).
    /// </summary>
    private static string EscapeBibTeX(string value)
        => CbetaReferenceHelper.EscapeBibTeX(value);

    /// <summary>
    /// Delegates to <see cref="CbetaReferenceHelper.TryParseCbetaFromFileId"/>.
    /// </summary>
    private static void TryParseCbetaFromFileId(string fileId, out string? canon, out int? volume, out string? number)
        => CbetaReferenceHelper.TryParseCbetaFromFileId(fileId, out canon, out volume, out number);
}
