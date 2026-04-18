using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ReadZen.App.Services;

/// <summary>
/// Represents a fuzzy match of query text found within witness OCR output.
/// </summary>
public sealed class WitnessTextMatch
{
    public int StartIndex { get; set; }
    public int Length { get; set; }
    public double Confidence { get; set; }
    public string MatchedText { get; set; } = "";
    public string MatchType { get; set; } = "";
}

/// <summary>
/// Finds corrected-reading text within raw witness OCR output using a
/// 4-phase cascade: exact → normalized → partial (80/60/40%) → empty.
/// Reuses <see cref="CjkMatchNormalizer"/> for punctuation/whitespace stripping.
/// </summary>
public static class WitnessTextLocatorService
{
    public static List<WitnessTextMatch> FindInWitness(
        string queryText,
        string witnessText,
        int maxResults = 5)
    {
        if (string.IsNullOrEmpty(queryText) || string.IsNullOrEmpty(witnessText))
            return new List<WitnessTextMatch>();

        // Phase 1: Exact match
        var results = FindExact(queryText, witnessText, maxResults);
        if (results.Count > 0)
            return results;

        // Phase 2: Normalized match
        var queryNorm = CjkMatchNormalizer.NormalizeWithMap(queryText);
        var witnessNorm = CjkMatchNormalizer.NormalizeWithMap(witnessText);

        results = FindNormalized(queryNorm, witnessNorm, maxResults);
        if (results.Count > 0)
            return results;

        // Phase 3: Partial match — try center substrings at 80%, 60%, 40%
        var partialSpecs = new[] { (0.80, "partial_80", 0.7), (0.60, "partial_60", 0.5), (0.40, "partial_40", 0.3) };
        foreach (var (fraction, matchType, confidence) in partialSpecs)
        {
            results = FindPartial(queryNorm.Normalized, witnessNorm, fraction, matchType, confidence, maxResults);
            if (results.Count > 0)
                return results;
        }

        // Phase 4: No match
        return new List<WitnessTextMatch>();
    }

    private static List<WitnessTextMatch> FindExact(string query, string witness, int max)
    {
        var matches = new List<WitnessTextMatch>();
        int from = 0;
        while (from < witness.Length && matches.Count < max)
        {
            int ix = witness.IndexOf(query, from, StringComparison.Ordinal);
            if (ix < 0) break;
            matches.Add(new WitnessTextMatch
            {
                StartIndex = ix,
                Length = query.Length,
                Confidence = 1.0,
                MatchedText = witness.Substring(ix, query.Length),
                MatchType = "exact"
            });
            from = ix + 1;
        }
        return matches;
    }

    private static List<WitnessTextMatch> FindNormalized(
        CjkMatchNormalizer.NormalizedText queryNorm,
        CjkMatchNormalizer.NormalizedText witnessNorm,
        int max)
    {
        if (queryNorm.Normalized.Length == 0) return new List<WitnessTextMatch>();

        var matches = new List<WitnessTextMatch>();
        int from = 0;
        while (from < witnessNorm.Normalized.Length && matches.Count < max)
        {
            int ix = witnessNorm.Normalized.IndexOf(queryNorm.Normalized, from, StringComparison.Ordinal);
            if (ix < 0) break;

            int rawStart = MapToRaw(witnessNorm, ix);
            int rawEnd = MapToRaw(witnessNorm, ix + queryNorm.Normalized.Length);
            int rawLen = rawEnd - rawStart;

            matches.Add(new WitnessTextMatch
            {
                StartIndex = rawStart,
                Length = rawLen,
                Confidence = 0.9,
                MatchedText = witnessNorm.Raw.Substring(rawStart, rawLen),
                MatchType = "normalized"
            });
            from = ix + 1;
        }
        return matches;
    }

    private static List<WitnessTextMatch> FindPartial(
        string normalizedQuery,
        CjkMatchNormalizer.NormalizedText witnessNorm,
        double fraction,
        string matchType,
        double confidence,
        int max)
    {
        int subLen = (int)(normalizedQuery.Length * fraction);
        if (subLen < 2) return new List<WitnessTextMatch>();

        // Take the center substring of the normalized query
        int start = (normalizedQuery.Length - subLen) / 2;
        string sub = normalizedQuery.Substring(start, subLen);

        var matches = new List<WitnessTextMatch>();
        int from = 0;
        while (from < witnessNorm.Normalized.Length && matches.Count < max)
        {
            int ix = witnessNorm.Normalized.IndexOf(sub, from, StringComparison.Ordinal);
            if (ix < 0) break;

            int rawStart = MapToRaw(witnessNorm, ix);
            int rawEnd = MapToRaw(witnessNorm, ix + sub.Length);
            int rawLen = rawEnd - rawStart;

            matches.Add(new WitnessTextMatch
            {
                StartIndex = rawStart,
                Length = rawLen,
                Confidence = confidence,
                MatchedText = witnessNorm.Raw.Substring(rawStart, rawLen),
                MatchType = matchType
            });
            from = ix + 1;
        }
        return matches;
    }

    private static int MapToRaw(CjkMatchNormalizer.NormalizedText norm, int normalizedPos)
    {
        if (normalizedPos <= 0) return 0;
        if (normalizedPos >= norm.RawIndexByNormalizedIndex.Length) return norm.Raw.Length;
        return norm.RawIndexByNormalizedIndex[normalizedPos];
    }
}

/// <summary>
/// Loads witness OCR text files from the standard directory layout:
/// {ocrBaseDir}/{siglum}/ocr/{engine}/{siglum}-p{NNN}.txt
/// </summary>
public static class WitnessOcrLoader
{
    /// <summary>
    /// Loads OCR text for a specific witness page from a specific engine.
    /// </summary>
    public static string? LoadOcrText(string ocrBaseDir, string siglum, string pageId, string engine = "rapidocr")
    {
        var path = BuildPath(ocrBaseDir, siglum, pageId, engine);
        if (path == null || !File.Exists(path))
            return null;
        return File.ReadAllText(path);
    }

    /// <summary>
    /// Loads OCR text from ALL engines for a page. Returns dict of engine → text.
    /// </summary>
    public static Dictionary<string, string> LoadAllEngineTexts(string ocrBaseDir, string siglum, string pageId)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var ocrDir = Path.Combine(ocrBaseDir, siglum, "ocr");
        if (!Directory.Exists(ocrDir))
            return result;

        foreach (var engineDir in Directory.GetDirectories(ocrDir))
        {
            string engine = Path.GetFileName(engineDir);
            string file = Path.Combine(engineDir, $"{siglum}-{pageId}.txt");
            if (File.Exists(file))
                result[engine] = File.ReadAllText(file);
        }
        return result;
    }

    private static string? BuildPath(string ocrBaseDir, string siglum, string pageId, string engine)
    {
        if (string.IsNullOrEmpty(ocrBaseDir) || string.IsNullOrEmpty(siglum) || string.IsNullOrEmpty(pageId))
            return null;
        return Path.Combine(ocrBaseDir, siglum, "ocr", engine, $"{siglum}-{pageId}.txt");
    }
}
