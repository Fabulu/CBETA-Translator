using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using ReadZen.App.Models;

namespace ReadZen.App.Services;

/// <summary>
/// Computes code (tag) frequency statistics: how many segments and files each code appears in.
/// </summary>
public static class CodeFrequencyService
{
    /// <summary>
    /// Groups applied tags by TagId and counts distinct segments and files per code.
    /// </summary>
    public static CodeFrequencyReport Compute(List<DocumentTag> tags, TagVocabulary vocab)
    {
        if (tags == null) throw new ArgumentNullException(nameof(tags));

        var tagLookup = new Dictionary<string, TagDefinition>(StringComparer.Ordinal);
        if (vocab?.Tags != null)
        {
            foreach (var td in vocab.Tags)
                tagLookup.TryAdd(td.Id, td);
        }

        var report = new CodeFrequencyReport();

        var grouped = tags.GroupBy(t => t.TagId, StringComparer.Ordinal);
        foreach (var group in grouped.OrderBy(g => g.Key, StringComparer.Ordinal))
        {
            string tagId = group.Key;
            int segmentCount = group.Count();
            int fileCount = group.Select(t => t.RelPath)
                                 .Distinct(StringComparer.OrdinalIgnoreCase)
                                 .Count();

            string name = tagId;
            string color = "#808080";
            if (tagLookup.TryGetValue(tagId, out var def))
            {
                name = def.DisplayName;
                color = def.Color;
            }

            report.Rows.Add(new CodeFrequencyRow(tagId, name, color, segmentCount, fileCount));
        }

        return report;
    }

    /// <summary>
    /// Exports a code frequency report as a CSV string.
    /// </summary>
    public static string ExportCsv(CodeFrequencyReport report)
    {
        if (report == null) throw new ArgumentNullException(nameof(report));

        var sb = new StringBuilder();
        sb.AppendLine("TagId,TagName,Color,SegmentCount,FileCount");
        foreach (var row in report.Rows)
        {
            sb.Append(CsvEscape(row.TagId));
            sb.Append(',');
            sb.Append(CsvEscape(row.TagName));
            sb.Append(',');
            sb.Append(CsvEscape(row.Color));
            sb.Append(',');
            sb.Append(row.SegmentCount);
            sb.Append(',');
            sb.AppendLine(row.FileCount.ToString());
        }
        return sb.ToString();
    }

    private static string CsvEscape(string value)
    {
        if (string.IsNullOrEmpty(value)) return "";
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        return value;
    }
}
