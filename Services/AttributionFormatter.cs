// Services/AttributionFormatter.cs
// Formats a TextLicenseInfo into human-readable attribution strings for the
// "Copy with attribution" context-menu flow. MVP ships only Plain(); Markdown,
// HtmlFooter, and BibTeX formatters are deferred to Phase 2.
using System.Text;
using ReadZen.App.Infrastructure;
using ReadZen.App.Models;

namespace ReadZen.App.Services;

public static class AttributionFormatter
{
    /// <summary>
    /// Produce a plain-text attribution block. If <paramref name="quotedRange"/>
    /// is non-null it is prepended in curly quotes before the attribution.
    /// </summary>
    public static string Plain(TextLicenseInfo? license, string? quotedRange = null)
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(quotedRange))
        {
            sb.Append('\u201C').Append(quotedRange!.Trim()).Append('\u201D').AppendLine().AppendLine();
        }

        if (license == null || license.LicenseClass == LicenseClass.Unknown)
        {
            sb.Append("Source license unknown. Treat as all-rights-reserved pending verification.");
            return sb.ToString();
        }

        if (!string.IsNullOrWhiteSpace(license.Title))
        {
            sb.Append(license.Title);
            if (!string.IsNullOrWhiteSpace(license.Author))
                sb.Append(", ").Append(license.Author);
            if (!string.IsNullOrWhiteSpace(license.YearComposed))
                sb.Append(" (").Append(license.YearComposed).Append(')');
            sb.Append('.').AppendLine();
        }

        if (!string.IsNullOrWhiteSpace(license.ShortLabel))
            sb.Append("License: ").Append(license.ShortLabel).Append('.').AppendLine();

        if (!string.IsNullOrWhiteSpace(license.RequiredAttribution))
            sb.AppendLine(license.RequiredAttribution!.Trim());

        if (!string.IsNullOrWhiteSpace(license.StableRevisionUrl))
        {
            sb.Append("Source (stable): ").AppendLine(license.StableRevisionUrl);
            AppendAccessedLine(sb);
        }
        else if (!string.IsNullOrWhiteSpace(license.SourceUrl))
        {
            sb.Append("Source: ").AppendLine(license.SourceUrl);
            AppendAccessedLine(sb);
        }

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// Appends the access-date line for a web source. Only called when a
    /// source URL was emitted — attribution without a URL is a print citation
    /// and carries no access date.
    /// </summary>
    private static void AppendAccessedLine(StringBuilder sb) =>
        sb.Append("Accessed ").Append(CitationDates.DayMonthYear(CitationDates.Today)).Append('.').AppendLine();
}
