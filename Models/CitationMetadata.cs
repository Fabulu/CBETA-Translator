// Models/CitationMetadata.cs
// Consolidated citation metadata record. Unifies work-level, passage-level,
// and translation metadata into a single object consumed by CitationService.

namespace ReadZen.App.Models;

/// <summary>
/// All metadata needed to generate a citation in any supported style.
/// Constructed from TextLicenseInfo + passage context + optional translation info.
/// </summary>
public sealed record CitationMetadata
{
    // --- Work-level ---
    public string? TitleEn { get; init; }
    public string? TitleZh { get; init; }
    public string? Author { get; init; }
    public string? Dynasty { get; init; }
    public string? YearComposed { get; init; }
    public CorpusKind Corpus { get; init; } = CorpusKind.Unknown;
    public string? LicenseLabel { get; init; }
    public string? SourceEdition { get; init; }
    public string? SeriesTitle { get; init; }

    // --- CBETA reference ---
    public string? CbetaCanon { get; init; }
    public int? CbetaVolume { get; init; }
    public string? CbetaNumber { get; init; }
    public string? FileId { get; init; }
    public string? Extent { get; init; }
    public string? CbetaVersionDate { get; init; }

    // --- Passage-level ---
    public string? FromLb { get; init; }
    public string? ToLb { get; init; }
    public string? RelPath { get; init; }
    public string? QuotedText { get; init; }

    // --- Translation ---
    public string? TranslatorName { get; init; }

    // --- URLs ---
    public string? ShareableUrl { get; init; }
    public string? ReadZenUrl { get; init; }
    public string? SourceUrl { get; init; }

    // --- Access date ---
    /// <summary>
    /// Date the online resource was accessed. Only emitted by formatters when
    /// the citation carries a URL (web citations need an access date; print
    /// CBETA/Taisho references do not). When null, formatters fall back to
    /// the system clock (<c>DateTime.Today</c>) at generation time.
    /// </summary>
    public System.DateTime? AccessedDate { get; init; }
}
