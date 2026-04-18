// Services/ApparatusTextExportService.cs
// Exports critical apparatus as plain-text Leiden notation or CSV.

using System.Linq;
using System.Text;
using ReadZen.App.Models;

namespace ReadZen.App.Services;

/// <summary>
/// Generates human-readable plain-text and CSV representations of a critical apparatus.
/// </summary>
public static class ApparatusTextExportService
{
    /// <summary>
    /// Exports the apparatus in Leiden-convention notation.
    /// One line per entry: <c>lemma ] reading1 W1 W3 | reading2 W2</c>.
    /// Type prefixes (om. / add. / transp.) are prepended to readings when present.
    /// </summary>
    public static string ExportLeiden(ApparatusInfo apparatus)
    {
        if (apparatus.Entries is not { Count: > 0 } entries)
            return string.Empty;

        var sb = new StringBuilder();

        foreach (var entry in entries)
        {
            var lemma = entry.Lemma ?? "—";
            sb.Append(lemma);
            sb.Append(" ] ");

            if (entry.Readings is { Count: > 0 })
            {
                var parts = entry.Readings.Select(FormatLeidenReading);
                sb.Append(string.Join(" | ", parts));
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static string FormatLeidenReading(ApparatusReading r)
    {
        var sb = new StringBuilder();

        // Type prefix
        if (!string.IsNullOrEmpty(r.Type))
        {
            var prefix = r.Type.ToLowerInvariant() switch
            {
                "om" => "om.",
                "add" => "add.",
                "transp" => "transp.",
                _ => r.Type + ".",
            };
            sb.Append(prefix);
            sb.Append(' ');
        }

        sb.Append(r.Reading ?? "—");

        if (!string.IsNullOrEmpty(r.WitnessId))
        {
            sb.Append(' ');
            sb.Append(r.WitnessId);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Exports the apparatus as CSV with one row per reading.
    /// Header: locus,lemma,reading,witness_id,type,certainty,is_human_checked.
    /// </summary>
    public static string ExportCsv(ApparatusInfo apparatus)
    {
        if (apparatus.Entries is not { Count: > 0 } entries)
            return "locus,lemma,reading,witness_id,type,certainty,is_human_checked\n";

        var sb = new StringBuilder();
        sb.AppendLine("locus,lemma,reading,witness_id,type,certainty,is_human_checked");

        foreach (var entry in entries)
        {
            if (entry.Readings is not { Count: > 0 }) continue;

            foreach (var r in entry.Readings)
            {
                sb.Append(CsvEscape(entry.LocusId));
                sb.Append(',');
                sb.Append(CsvEscape(entry.Lemma));
                sb.Append(',');
                sb.Append(CsvEscape(r.Reading));
                sb.Append(',');
                sb.Append(CsvEscape(r.WitnessId));
                sb.Append(',');
                sb.Append(CsvEscape(r.Type));
                sb.Append(',');
                sb.Append(CsvEscape(r.Certainty));
                sb.Append(',');
                sb.Append(r.IsHumanChecked == true ? "true" : "false");
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }

    private static string CsvEscape(string? value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        return value;
    }
}
