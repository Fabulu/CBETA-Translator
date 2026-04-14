using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using ReadZen.App.Models;

namespace ReadZen.App.Services;

/// <summary>
/// Computes a co-occurrence matrix: for each pair of codes, how many files contain
/// overlapping lb-ranges tagged with both codes.
/// </summary>
public static class CodeCooccurrenceService
{
    /// <summary>
    /// Builds an N x N matrix where Matrix[i,j] = number of files where code i and code j
    /// both appear with overlapping lb-ranges.
    /// Overlap: tag1.FromLb &lt;= tag2.ToLb &amp;&amp; tag2.FromLb &lt;= tag1.ToLb (ordinal).
    /// </summary>
    public static CodeCooccurrenceMatrix Compute(List<DocumentTag> tags, TagVocabulary vocab)
    {
        if (tags == null) throw new ArgumentNullException(nameof(tags));

        var tagLookup = new Dictionary<string, TagDefinition>(StringComparer.Ordinal);
        if (vocab?.Tags != null)
        {
            foreach (var td in vocab.Tags)
                tagLookup.TryAdd(td.Id, td);
        }

        // Collect distinct code IDs in deterministic order
        var codeIdSet = new HashSet<string>(StringComparer.Ordinal);
        foreach (var t in tags) codeIdSet.Add(t.TagId);
        var codeIds = codeIdSet.OrderBy(id => id, StringComparer.Ordinal).ToList();

        int n = codeIds.Count;
        var codeNames = new List<string>(n);
        var codeColors = new List<string>(n);
        var codeIndex = new Dictionary<string, int>(n, StringComparer.Ordinal);

        for (int i = 0; i < n; i++)
        {
            codeIndex[codeIds[i]] = i;
            if (tagLookup.TryGetValue(codeIds[i], out var def))
            {
                codeNames.Add(def.DisplayName);
                codeColors.Add(def.Color);
            }
            else
            {
                codeNames.Add(codeIds[i]);
                codeColors.Add("#808080");
            }
        }

        var matrix = new int[n, n];

        // Group tags by file
        var byFile = tags.GroupBy(t => t.RelPath, StringComparer.OrdinalIgnoreCase);

        foreach (var fileGroup in byFile)
        {
            var fileTags = fileGroup.ToList();

            // For each pair of tags in this file, check overlap
            for (int a = 0; a < fileTags.Count; a++)
            {
                for (int b = a + 1; b < fileTags.Count; b++)
                {
                    var ta = fileTags[a];
                    var tb = fileTags[b];

                    if (string.Equals(ta.TagId, tb.TagId, StringComparison.Ordinal))
                        continue; // skip same code — self-occurrence handled separately

                    if (!HasOverlap(ta, tb))
                        continue;

                    int ia = codeIndex[ta.TagId];
                    int ib = codeIndex[tb.TagId];

                    // We use a set to avoid double-counting within the same file
                    // Handled below after collecting pairs
                }
            }
        }

        // More efficient: collect per-file co-occurring pairs, deduplicate per file
        foreach (var fileGroup in byFile)
        {
            var fileTags = fileGroup.ToList();
            var pairsSeen = new HashSet<(int, int)>();

            for (int a = 0; a < fileTags.Count; a++)
            {
                for (int b = a + 1; b < fileTags.Count; b++)
                {
                    var ta = fileTags[a];
                    var tb = fileTags[b];

                    if (!HasOverlap(ta, tb))
                        continue;

                    int ia = codeIndex[ta.TagId];
                    int ib = codeIndex[tb.TagId];

                    int lo = Math.Min(ia, ib);
                    int hi = Math.Max(ia, ib);

                    pairsSeen.Add((lo, hi));
                }

                // Self-occurrence: count files where this code appears
                int selfIdx = codeIndex[fileTags[a].TagId];
                pairsSeen.Add((selfIdx, selfIdx));
            }

            foreach (var (lo, hi) in pairsSeen)
            {
                matrix[lo, hi]++;
                if (lo != hi)
                    matrix[hi, lo]++;
            }
        }

        return new CodeCooccurrenceMatrix
        {
            CodeIds = codeIds,
            CodeNames = codeNames,
            CodeColors = codeColors,
            Matrix = matrix
        };
    }

    /// <summary>
    /// Checks whether two tags have overlapping lb-ranges (ordinal comparison).
    /// </summary>
    internal static bool HasOverlap(DocumentTag a, DocumentTag b)
    {
        return string.Compare(a.FromLb, b.ToLb, StringComparison.Ordinal) <= 0 &&
               string.Compare(b.FromLb, a.ToLb, StringComparison.Ordinal) <= 0;
    }

    /// <summary>
    /// Builds a self-contained HTML page with an N x N color-intensity table.
    /// </summary>
    public static string BuildHtml(CodeCooccurrenceMatrix m)
    {
        if (m == null) throw new ArgumentNullException(nameof(m));

        int n = m.CodeIds.Count;
        int maxVal = 0;
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
                if (m.Matrix[i, j] > maxVal) maxVal = m.Matrix[i, j];

        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html><head><meta charset='utf-8'/>");
        sb.AppendLine("<title>Code Co-occurrence Matrix</title>");
        sb.AppendLine("<style>");
        sb.AppendLine("body { font-family: sans-serif; margin: 20px; }");
        sb.AppendLine("table { border-collapse: collapse; }");
        sb.AppendLine("th, td { border: 1px solid #ccc; padding: 6px 10px; text-align: center; min-width: 40px; }");
        sb.AppendLine("th { background: #f5f5f5; font-size: 12px; }");
        sb.AppendLine("</style></head><body>");
        sb.AppendLine("<h2>Code Co-occurrence Matrix</h2>");
        sb.AppendLine("<table><tr><th></th>");

        for (int j = 0; j < n; j++)
            sb.Append($"<th>{WebUtility.HtmlEncode(m.CodeNames[j])}</th>");
        sb.AppendLine("</tr>");

        for (int i = 0; i < n; i++)
        {
            sb.Append($"<tr><th>{WebUtility.HtmlEncode(m.CodeNames[i])}</th>");
            for (int j = 0; j < n; j++)
            {
                int val = m.Matrix[i, j];
                double intensity = maxVal > 0 ? (double)val / maxVal : 0;
                int r = 255, g = (int)(255 * (1 - intensity * 0.8)), b = (int)(255 * (1 - intensity * 0.8));
                string bg = $"rgb({r},{g},{b})";
                sb.Append($"<td style='background:{bg}'>{val}</td>");
            }
            sb.AppendLine("</tr>");
        }

        sb.AppendLine("</table></body></html>");
        return sb.ToString();
    }

    /// <summary>
    /// Exports the co-occurrence matrix as a CSV string.
    /// </summary>
    public static string ExportCsv(CodeCooccurrenceMatrix m)
    {
        if (m == null) throw new ArgumentNullException(nameof(m));

        int n = m.CodeIds.Count;
        var sb = new StringBuilder();

        // Header row
        sb.Append("Code");
        for (int j = 0; j < n; j++)
        {
            sb.Append(',');
            sb.Append(CsvEscape(m.CodeNames[j]));
        }
        sb.AppendLine();

        // Data rows
        for (int i = 0; i < n; i++)
        {
            sb.Append(CsvEscape(m.CodeNames[i]));
            for (int j = 0; j < n; j++)
            {
                sb.Append(',');
                sb.Append(m.Matrix[i, j]);
            }
            sb.AppendLine();
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
