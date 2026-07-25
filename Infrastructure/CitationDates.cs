// Infrastructure/CitationDates.cs
// Access-date formatting for citations of web/digital resources.
//
// Web content is mutable, so any citation that carries a URL to the ReadZen
// site (readzen.pages.dev share links, master profiles, source URLs, etc.)
// must state the date the resource was accessed. Print-source citations
// (Taisho/CBETA canonical references without a URL) never carry an access
// date — a fixed edition needs none.
//
// InvariantGlobalization=true is set project-wide, so every formatter here
// uses CultureInfo.InvariantCulture explicitly (English month names, stable
// digits) rather than relying on the ambient culture.

using System;
using System.Globalization;

namespace ReadZen.App.Infrastructure;

/// <summary>
/// Shared access-date formatting used by CitationService, ScholarExportService,
/// AttributionFormatter, and the view-level citation copy paths.
/// </summary>
public static class CitationDates
{
    /// <summary>
    /// Today per the local system clock. The app runs locally, so this is a
    /// real clock (unlike a hosted service pinned to UTC server time).
    /// </summary>
    public static DateTime Today => DateTime.Today;

    /// <summary>ISO date for BibTeX <c>urldate</c>: e.g. "2026-07-24".</summary>
    public static string Iso(DateTime date) =>
        date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    /// <summary>MLA / plain-text access-date form: e.g. "24 July 2026".</summary>
    public static string DayMonthYear(DateTime date) =>
        date.ToString("d MMMM yyyy", CultureInfo.InvariantCulture);

    /// <summary>Chicago / APA access-date form: e.g. "July 24, 2026".</summary>
    public static string MonthDayYear(DateTime date) =>
        date.ToString("MMMM d, yyyy", CultureInfo.InvariantCulture);

    /// <summary>RIS access-date (Y2 tag) form: e.g. "2026/07/24".</summary>
    public static string RisY2(DateTime date) =>
        date.ToString("yyyy/MM/dd", CultureInfo.InvariantCulture);
}
