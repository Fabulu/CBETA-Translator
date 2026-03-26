using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace CbetaTranslator.App.Services;

/// <summary>
/// Exposes XML reconciliation logic for testing. These methods are extracted from
/// IndexedTranslationService to allow direct unit testing while keeping the original
/// private methods as thin delegates.
/// </summary>
internal static class XmlReconciliationHelper
{
    private static readonly Regex s_whitespaceRun = new(@"\s+", RegexOptions.Compiled);

    /// <summary>
    /// Reconciles newly serialized XML with the original string to preserve formatting
    /// (indentation, line endings, whitespace) for all unchanged lines.
    /// </summary>
    internal static string ReconcileWithOriginalFormatting(string originalXml, string newXml)
    {
        if (string.IsNullOrEmpty(originalXml)) return newXml;

        // Detect original line ending style
        bool hasCrlf = originalXml.Contains("\r\n");
        string eol = hasCrlf ? "\r\n" : "\n";

        // Normalize both to \n for line-by-line comparison
        var origLines = originalXml.Replace("\r\n", "\n").Split('\n');
        var newLines = newXml.Replace("\r\n", "\n").Split('\n');

        // Build a content-normalized version of original lines for matching
        var origMap = new Dictionary<string, List<int>>();
        for (int i = 0; i < origLines.Length; i++)
        {
            string key = NormalizeForComparison(origLines[i]);
            if (!origMap.TryGetValue(key, out var list))
            {
                list = new List<int>();
                origMap[key] = list;
            }
            list.Add(i);
        }

        // Walk new lines, try to match each to an original line
        var result = new List<string>(newLines.Length);
        var usedOrigIndices = new HashSet<int>();

        foreach (var newLine in newLines)
        {
            string key = NormalizeForComparison(newLine);

            int matchIdx = -1;
            if (origMap.TryGetValue(key, out var candidates))
            {
                foreach (int ci in candidates)
                {
                    if (!usedOrigIndices.Contains(ci))
                    {
                        matchIdx = ci;
                        break;
                    }
                }
            }

            if (matchIdx >= 0)
            {
                result.Add(origLines[matchIdx]);
                usedOrigIndices.Add(matchIdx);
            }
            else
            {
                result.Add(newLine);
            }
        }

        return string.Join(eol, result);
    }

    /// <summary>
    /// Normalizes a line for comparison by trimming and collapsing whitespace.
    /// </summary>
    internal static string NormalizeForComparison(string line)
    {
        var trimmed = line.Trim();
        return s_whitespaceRun.Replace(trimmed, " ");
    }
}
