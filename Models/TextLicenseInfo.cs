// Models/TextLicenseInfo.cs
// Machine-readable snapshot of a single TEI file's licensing, attribution, and
// provenance metadata. Produced by TextLicenseExtractor from the teiHeader
// <publicationStmt>/<availability> and <sourceDesc> blocks.
//
// This record is the MVP contract — any display surface (top-bar badge, reader
// chip, flyout, copy-with-attribution) reads from here.

using System.Collections.Generic;

namespace ReadZen.App.Models;

public sealed record TextLicenseInfo
{
    /// <summary>Which corpus this file belongs to (detected heuristically or from TEI hints).</summary>
    public CorpusKind Corpus { get; init; } = CorpusKind.Unknown;

    /// <summary>Coarse classification that drives UI color coding.</summary>
    public LicenseClass LicenseClass { get; init; } = LicenseClass.Unknown;

    /// <summary>Short SPDX-ish label, e.g. "CC-BY-SA-4.0", "PD-old", "CBETA-NC".</summary>
    public string ShortLabel { get; init; } = "";

    /// <summary>Full human-readable license text(s), joined with newlines.</summary>
    public string LongText { get; init; } = "";

    /// <summary>Work title (preferred English title, falling back to Chinese).</summary>
    public string? Title { get; init; }

    /// <summary>Original author (with dates if present).</summary>
    public string? Author { get; init; }

    /// <summary>Composition year as string (so era names work).</summary>
    public string? YearComposed { get; init; }

    /// <summary>Best upstream URL to the source witness (e.g. Wikisource page).</summary>
    public string? SourceUrl { get; init; }

    /// <summary>Permanent-link URL pinned to a specific revision (oldid=/permalink/).</summary>
    public string? StableRevisionUrl { get; init; }

    /// <summary>Plain-English rights basis text from the availability block.</summary>
    public string? RightsBasisText { get; init; }

    /// <summary>Vetting confidence if declared in the header (high/medium/low/unknown).</summary>
    public string? VettingConfidence { get; init; }

    /// <summary>True if the header affirms the file contains no CBETA-derived material.</summary>
    public bool NoCbetaMaterial { get; init; }

    /// <summary>Short-form attribution string the header requires downstream users to preserve.</summary>
    public string? RequiredAttribution { get; init; }

    public bool AttributionRequired { get; init; }
    public bool ShareAlikeRequired { get; init; }
    public bool CommercialUseAllowed { get; init; }

    /// <summary>Chinese title from &lt;title xml:lang="zh-Hant" level="m"&gt;.</summary>
    public string? TitleZh { get; init; }

    /// <summary>CBETA canon letter, e.g. "T", "J", "X", "B", "A".</summary>
    public string? CbetaCanon { get; init; }

    /// <summary>CBETA volume number, e.g. 48.</summary>
    public int? CbetaVolume { get; init; }

    /// <summary>CBETA text number, e.g. "2005" or "B137".</summary>
    public string? CbetaNumber { get; init; }

    /// <summary>Source edition description from sourceDesc/bibl, e.g. "大正新脩大藏經".</summary>
    public string? SourceEdition { get; init; }

    /// <summary>Extent of the work, e.g. "1卷", "22卷".</summary>
    public string? Extent { get; init; }

    /// <summary>Dynasty extracted from author prefix, e.g. "宋", "唐", "後秦".</summary>
    public string? Dynasty { get; init; }

    /// <summary>CBETA data version date from publicationStmt/date.</summary>
    public string? CbetaVersionDate { get; init; }

    /// <summary>Derived file identifier, e.g. "T48n2005".</summary>
    public string? FileId { get; init; }

    /// <summary>Rel-path (or file id) this info was extracted for, for diagnostics.</summary>
    public string? RelPath { get; init; }

    /// <summary>Contributors from &lt;respStmt&gt; elements (name + optional role).</summary>
    public List<string>? Contributors { get; init; }

    /// <summary>Witness sigla/descriptions from &lt;listWit&gt;/&lt;witness&gt; in the teiHeader.</summary>
    public List<string>? Witnesses { get; init; }

    /// <summary>Unknown-state sentinel record used when no teiHeader is present.</summary>
    public static TextLicenseInfo UnknownDefault { get; } = new()
    {
        Corpus = CorpusKind.Unknown,
        LicenseClass = LicenseClass.Unknown,
        ShortLabel = "Unknown"
    };
}
