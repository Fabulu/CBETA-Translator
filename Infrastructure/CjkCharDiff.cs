// Infrastructure/CjkCharDiff.cs
// Character-level diff for CJK text. Produces insert/delete spans so the UI
// can highlight exactly which characters changed between two readings.

using System;
using System.Collections.Generic;

namespace ReadZen.App.Infrastructure;

/// <summary>
/// A single span in a character-level diff: either an insertion, deletion,
/// or unchanged run.
/// </summary>
public sealed class CharDiffSpan
{
    public CharDiffKind Kind { get; init; }
    public string Text { get; init; } = "";
}

public enum CharDiffKind { Equal, Insert, Delete }

/// <summary>
/// Computes a minimal character-level diff between two strings using a
/// simple LCS (longest common subsequence) approach. Optimized for short
/// CJK strings (typical locus: 5-30 characters) where O(n*m) is fine.
/// </summary>
public static class CjkCharDiff
{
    /// <summary>
    /// Returns an ordered list of diff spans showing how <paramref name="before"/>
    /// transforms into <paramref name="after"/>. Delete spans are from
    /// <paramref name="before"/>; insert spans are from <paramref name="after"/>;
    /// equal spans are shared.
    /// </summary>
    public static List<CharDiffSpan> Diff(string before, string after)
    {
        if (before == after)
            return new List<CharDiffSpan> { new() { Kind = CharDiffKind.Equal, Text = before } };

        if (string.IsNullOrEmpty(before))
            return new List<CharDiffSpan> { new() { Kind = CharDiffKind.Insert, Text = after } };

        if (string.IsNullOrEmpty(after))
            return new List<CharDiffSpan> { new() { Kind = CharDiffKind.Delete, Text = before } };

        // Standard LCS dynamic programming
        int n = before.Length, m = after.Length;
        var dp = new int[n + 1, m + 1];

        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                dp[i, j] = before[i - 1] == after[j - 1]
                    ? dp[i - 1, j - 1] + 1
                    : Math.Max(dp[i - 1, j], dp[i, j - 1]);
            }
        }

        // Backtrack to produce diff spans
        var spans = new List<CharDiffSpan>();
        int bi = n, ai = m;

        // We build spans in reverse, then reverse at the end
        var reverseSpans = new List<CharDiffSpan>();

        while (bi > 0 || ai > 0)
        {
            if (bi > 0 && ai > 0 && before[bi - 1] == after[ai - 1])
            {
                reverseSpans.Add(new CharDiffSpan { Kind = CharDiffKind.Equal, Text = before[bi - 1].ToString() });
                bi--; ai--;
            }
            else if (ai > 0 && (bi == 0 || dp[bi, ai - 1] >= dp[bi - 1, ai]))
            {
                reverseSpans.Add(new CharDiffSpan { Kind = CharDiffKind.Insert, Text = after[ai - 1].ToString() });
                ai--;
            }
            else
            {
                reverseSpans.Add(new CharDiffSpan { Kind = CharDiffKind.Delete, Text = before[bi - 1].ToString() });
                bi--;
            }
        }

        reverseSpans.Reverse();

        // Merge consecutive spans of the same kind
        foreach (var span in reverseSpans)
        {
            if (spans.Count > 0 && spans[^1].Kind == span.Kind)
            {
                var last = spans[^1];
                spans[^1] = new CharDiffSpan { Kind = last.Kind, Text = last.Text + span.Text };
            }
            else
            {
                spans.Add(span);
            }
        }

        return spans;
    }

    /// <summary>
    /// Formats a diff as a compact human-readable string:
    /// equal text stays plain, deletions in [-...], insertions in [+...].
    /// </summary>
    public static string FormatCompact(List<CharDiffSpan> spans)
    {
        var sb = new System.Text.StringBuilder();
        foreach (var span in spans)
        {
            switch (span.Kind)
            {
                case CharDiffKind.Equal: sb.Append(span.Text); break;
                case CharDiffKind.Delete: sb.Append("[-").Append(span.Text).Append(']'); break;
                case CharDiffKind.Insert: sb.Append("[+").Append(span.Text).Append(']'); break;
            }
        }
        return sb.ToString();
    }
}
