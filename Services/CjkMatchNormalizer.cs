using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ReadZen.App.Services;

/// <summary>
/// Shared CJK matching policy for Translation Assistant and Search:
/// - ignore whitespace/line breaks and editorial punctuation
/// - preserve mapping from normalized indices back to raw text indices
/// </summary>
public static class CjkMatchNormalizer
{
    public readonly record struct RawRange(int Start, int Length);

    public sealed class NormalizedText
    {
        public string Raw { get; init; } = "";
        public string Normalized { get; init; } = "";
        public int[] RawIndexByNormalizedIndex { get; init; } = Array.Empty<int>();
    }

    public static bool ContainsCjk(string? s)
    {
        if (string.IsNullOrEmpty(s))
            return false;

        foreach (char c in s)
            if (c >= '\u4E00' && c <= '\u9FFF')
                return true;
        return false;
    }

    public static string Normalize(string? raw) => NormalizeWithMap(raw).Normalized;

    public static NormalizedText NormalizeWithMap(string? raw)
    {
        raw ??= "";
        raw = raw.Replace('\u3000', ' ');

        var sb = new StringBuilder(raw.Length);
        var map = new List<int>(raw.Length);

        for (int i = 0; i < raw.Length; i++)
        {
            char c = raw[i];
            if (IsStrippedForMatch(c))
                continue;

            sb.Append(c);
            map.Add(i);
        }

        return new NormalizedText
        {
            Raw = raw,
            Normalized = sb.ToString(),
            RawIndexByNormalizedIndex = map.ToArray()
        };
    }

    public static int RawIndexFromNormalizedPos(NormalizedText normalized, int normalizedPos)
    {
        if (normalized == null)
            return 0;

        if (normalizedPos <= 0)
            return 0;

        if (normalizedPos >= normalized.RawIndexByNormalizedIndex.Length)
            return normalized.Raw.Length;

        return normalized.RawIndexByNormalizedIndex[normalizedPos];
    }

    public static IReadOnlyList<RawRange> FindSharedRawRanges(
        string? haystackRaw,
        string? needleRaw,
        int minPhraseLen = 2,
        int maxPhraseLen = 10)
    {
        var hay = NormalizeWithMap(haystackRaw);
        var needle = NormalizeWithMap(needleRaw);

        if (hay.Normalized.Length == 0 || needle.Normalized.Length == 0)
            return Array.Empty<RawRange>();

        var phrases = ExtractDistinctPhrases(needle.Normalized, minPhraseLen, maxPhraseLen);
        var used = new bool[hay.Normalized.Length];
        var ranges = new List<RawRange>();

        foreach (var phrase in phrases)
        {
            int from = 0;
            while (from < hay.Normalized.Length)
            {
                int ix = hay.Normalized.IndexOf(phrase, from, StringComparison.Ordinal);
                if (ix < 0)
                    break;

                bool overlaps = false;
                int endIx = ix + phrase.Length;
                for (int i = ix; i < endIx; i++)
                {
                    if (used[i])
                    {
                        overlaps = true;
                        break;
                    }
                }

                if (!overlaps)
                {
                    for (int i = ix; i < endIx; i++)
                        used[i] = true;

                    var rr = ToRawRange(hay, ix, phrase.Length);
                    if (rr.Length > 0)
                        ranges.Add(rr);
                }

                from = ix + Math.Max(1, phrase.Length);
            }
        }

        if (ranges.Count == 0)
        {
            var lcs = TryLongestCommonSubstringRange(hay.Normalized, needle.Normalized, minPhraseLen);
            if (lcs.HasValue)
            {
                var rr = ToRawRange(hay, lcs.Value.start, lcs.Value.len);
                if (rr.Length > 0)
                    ranges.Add(rr);
            }
        }

        ranges.Sort((a, b) => a.Start != b.Start ? a.Start.CompareTo(b.Start) : a.Length.CompareTo(b.Length));
        return MergeRanges(ranges);
    }

    private static RawRange ToRawRange(NormalizedText hay, int normalizedStart, int normalizedLength)
    {
        if (normalizedLength <= 0 || hay.Raw.Length == 0)
            return new RawRange(0, 0);

        int rawStart = RawIndexFromNormalizedPos(hay, normalizedStart);
        int rawEndExclusive = RawIndexFromNormalizedPos(hay, normalizedStart + normalizedLength);
        rawStart = Math.Clamp(rawStart, 0, hay.Raw.Length);
        rawEndExclusive = Math.Clamp(rawEndExclusive, rawStart, hay.Raw.Length);

        return new RawRange(rawStart, rawEndExclusive - rawStart);
    }

    private static IReadOnlyList<string> ExtractDistinctPhrases(string s, int minPhraseLen, int maxPhraseLen)
    {
        var set = new HashSet<string>(StringComparer.Ordinal);
        int max = Math.Min(maxPhraseLen, s.Length);

        for (int len = max; len >= minPhraseLen; len--)
        {
            for (int i = 0; i + len <= s.Length; i++)
                set.Add(s.Substring(i, len));
        }

        return set.OrderByDescending(x => x.Length).ToList();
    }

    private static (int start, int len)? TryLongestCommonSubstringRange(string a, string b, int minLen)
    {
        if (a.Length == 0 || b.Length == 0)
            return null;

        var dp = new int[b.Length + 1];
        int bestLen = 0;
        int bestEndA = -1;

        for (int i = 1; i <= a.Length; i++)
        {
            for (int j = b.Length; j >= 1; j--)
            {
                if (a[i - 1] == b[j - 1])
                {
                    dp[j] = dp[j - 1] + 1;
                    if (dp[j] > bestLen)
                    {
                        bestLen = dp[j];
                        bestEndA = i;
                    }
                }
                else
                {
                    dp[j] = 0;
                }
            }
        }

        if (bestLen < minLen || bestEndA <= 0)
            return null;

        return (bestEndA - bestLen, bestLen);
    }

    private static IReadOnlyList<RawRange> MergeRanges(List<RawRange> ranges)
    {
        if (ranges.Count <= 1)
            return ranges;

        var merged = new List<RawRange> { ranges[0] };
        for (int i = 1; i < ranges.Count; i++)
        {
            var last = merged[^1];
            var cur = ranges[i];
            int lastEnd = last.Start + last.Length;
            int curEnd = cur.Start + cur.Length;

            if (cur.Start <= lastEnd)
            {
                merged[^1] = new RawRange(last.Start, Math.Max(lastEnd, curEnd) - last.Start);
            }
            else
            {
                merged.Add(cur);
            }
        }

        return merged;
    }

    private static bool IsStrippedForMatch(char c) =>
        char.IsWhiteSpace(c)
        || c == '\u3001' || c == '\u3002'
        || c == '\uFF01' || c == '\uFF0C' || c == '\uFF1A'
        || c == '\uFF1B' || c == '\uFF1F'
        || c == '\uFF08' || c == '\uFF09'
        || c == '\u300A' || c == '\u300B'
        || c == '\u3008' || c == '\u3009'
        || c == '\u300C' || c == '\u300D'
        || c == '\u300E' || c == '\u300F'
        || c == '\u3010' || c == '\u3011'
        || c == '\u2014' || c == '\u2026'
        || c == '\u00B7' || c == '\u30FB'
        // Superscript digits (annotation markers from AnnotationMarkerInserter: ⁰¹²³⁴⁵⁶⁷⁸⁹)
        || c == '\u2070' || c == '\u00B9' || c == '\u00B2' || c == '\u00B3'
        || (c >= '\u2074' && c <= '\u2079')
        // Supplementary PUA surrogates (annotation icons like U+F1598).
        // CJK Extension B uses U+D840-U+D869; PUA starts at U+DB00+.
        || (char.IsSurrogate(c) && c >= '\uDB00');
}
