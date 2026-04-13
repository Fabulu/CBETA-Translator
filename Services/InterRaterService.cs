using System;
using System.Collections.Generic;
using System.Linq;
using ReadZen.App.Models;

namespace ReadZen.App.Services;

/// <summary>
/// Computes inter-rater reliability (Cohen's kappa + percent agreement)
/// between two coders' tag sets on a single document.
/// </summary>
public static class InterRaterService
{
    /// <summary>
    /// Extracts unique lb n-values from a rendered document's segments.
    /// These form the unit-of-analysis list for inter-rater comparison.
    /// </summary>
    public static List<string> ExtractLbValues(RenderedDocument doc)
    {
        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var seg in doc.Segments)
        {
            if (!seg.Key.StartsWith("lb|", StringComparison.Ordinal))
                continue;

            var parts = seg.Key.Split('|');
            if (parts.Length < 2) continue;

            string nValue = parts[1];
            if (seen.Add(nValue))
                result.Add(nValue);
        }

        return result;
    }

    /// <summary>
    /// Compares two coders' tag sets across a set of lb units, producing
    /// per-code and overall agreement metrics (percent agreement + Cohen's kappa).
    /// </summary>
    public static InterRaterResult Compare(
        string relPath,
        string coder1,
        string coder2,
        List<string> allLbValues,
        List<DocumentTag> coder1Tags,
        List<DocumentTag> coder2Tags,
        TagVocabulary? vocab1,
        TagVocabulary? vocab2)
    {
        // Filter tags to only those for this document
        var tags1 = coder1Tags.Where(t => string.Equals(t.RelPath, relPath, StringComparison.Ordinal)).ToList();
        var tags2 = coder2Tags.Where(t => string.Equals(t.RelPath, relPath, StringComparison.Ordinal)).ToList();

        // Build tag name lookup from both vocabularies
        var tagNames = new Dictionary<string, string>(StringComparer.Ordinal);
        if (vocab1?.Tags != null)
            foreach (var td in vocab1.Tags)
                tagNames[td.Id] = td.DisplayName;
        if (vocab2?.Tags != null)
            foreach (var td in vocab2.Tags)
                tagNames.TryAdd(td.Id, td.DisplayName);

        // Union of all tag IDs used by either coder
        var allTagIds = new HashSet<string>(StringComparer.Ordinal);
        foreach (var t in tags1) allTagIds.Add(t.TagId);
        foreach (var t in tags2) allTagIds.Add(t.TagId);

        int totalUnits = allLbValues.Count;

        // Micro-average accumulators
        int sumA = 0, sumB = 0, sumC = 0, sumD = 0;
        var perCode = new List<PerCodeAgreement>();

        foreach (var tagId in allTagIds.OrderBy(id => id, StringComparer.Ordinal))
        {
            int a = 0, b = 0, c = 0, d = 0; // a=both, b=only1, c=only2, d=neither

            foreach (var lb in allLbValues)
            {
                bool c1 = LbContainedByAny(lb, tags1, tagId);
                bool c2 = LbContainedByAny(lb, tags2, tagId);

                if (c1 && c2) a++;
                else if (c1) b++;
                else if (c2) c++;
                else d++;
            }

            sumA += a; sumB += b; sumC += c; sumD += d;

            string name = tagNames.TryGetValue(tagId, out var n) ? n : tagId;

            perCode.Add(new PerCodeAgreement
            {
                TagId = tagId,
                TagName = name,
                BothPresent = a,
                OnlyCoder1 = b,
                OnlyCoder2 = c,
                NeitherPresent = d,
                PercentAgreement = ComputePercentAgreement(a, b, c, d),
                CohensKappa = ComputeKappa(a, b, c, d)
            });
        }

        return new InterRaterResult
        {
            RelPath = relPath,
            Coder1 = coder1,
            Coder2 = coder2,
            TotalUnits = totalUnits,
            OverallPercentAgreement = ComputePercentAgreement(sumA, sumB, sumC, sumD),
            OverallCohensKappa = ComputeKappa(sumA, sumB, sumC, sumD),
            PerCode = perCode
        };
    }

    /// <summary>
    /// Checks whether a given lb n-value falls within any tag range for the specified tagId.
    /// Containment: tag.FromLb &lt;= lb &lt;= tag.ToLb (ordinal).
    /// </summary>
    private static bool LbContainedByAny(string lb, List<DocumentTag> tags, string tagId)
    {
        foreach (var tag in tags)
        {
            if (!string.Equals(tag.TagId, tagId, StringComparison.Ordinal))
                continue;

            if (string.Compare(tag.FromLb, lb, StringComparison.Ordinal) <= 0 &&
                string.Compare(lb, tag.ToLb, StringComparison.Ordinal) <= 0)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Percent agreement from a 2x2 contingency table: (a + d) / (a + b + c + d).
    /// Returns 1.0 if total is zero (trivial agreement).
    /// </summary>
    internal static double ComputePercentAgreement(int a, int b, int c, int d)
    {
        int total = a + b + c + d;
        if (total == 0) return 1.0;
        return (double)(a + d) / total;
    }

    /// <summary>
    /// Cohen's kappa from a 2x2 contingency table.
    /// Returns 1.0 if expected agreement is ~1 (no variance).
    /// </summary>
    internal static double ComputeKappa(int a, int b, int c, int d)
    {
        int total = a + b + c + d;
        if (total == 0) return 1.0;

        double po = (double)(a + d) / total;
        double p1Yes = (double)(a + b) / total;
        double p2Yes = (double)(a + c) / total;
        double pe = p1Yes * p2Yes + (1.0 - p1Yes) * (1.0 - p2Yes);

        if (Math.Abs(1.0 - pe) < 1e-10)
            return 1.0;

        return (po - pe) / (1.0 - pe);
    }
}
