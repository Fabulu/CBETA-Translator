// Infrastructure/TextDiff.cs
// Simple word-level diff for comparing two text versions.
// Returns a list of spans marking additions and deletions.

using System;
using System.Collections.Generic;

namespace ReadZen.App.Infrastructure;

public enum DiffKind { Equal, Added, Removed }

public readonly record struct DiffSpan(int Start, int Length, DiffKind Kind);

public static class TextDiff
{
    /// <summary>
    /// Computes word-level diff spans for the "current" text compared against "historical" text.
    /// Returns spans relative to the "current" text:
    /// - Added: text present in current but not in historical (green)
    /// - Removed: (not shown inline — would need a separate gutter; skip for now)
    /// - Equal: unchanged text (no highlight)
    ///
    /// For simplicity, this uses a token-level LCS approach on whitespace-split words.
    /// </summary>
    public static List<DiffSpan> ComputeAddedSpans(string current, string historical)
    {
        var result = new List<DiffSpan>();
        if (string.IsNullOrEmpty(current) || string.IsNullOrEmpty(historical))
            return result;

        var curTokens = Tokenize(current);
        var histTokens = Tokenize(historical);

        // Build a set of historical token sequences for fast lookup
        var histSet = new HashSet<string>(StringComparer.Ordinal);
        foreach (var t in histTokens)
            histSet.Add(t.Text);

        // Simple approach: mark tokens in current that don't appear in historical
        // This is a bag-of-words diff — not perfect but fast and good enough for visual highlighting
        var histBag = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var t in histTokens)
        {
            histBag.TryGetValue(t.Text, out var count);
            histBag[t.Text] = count + 1;
        }

        // Clone the bag for consumption
        var available = new Dictionary<string, int>(histBag, StringComparer.Ordinal);

        foreach (var t in curTokens)
        {
            if (available.TryGetValue(t.Text, out var count) && count > 0)
            {
                available[t.Text] = count - 1;
                // Equal — no highlight
            }
            else
            {
                // Added in current (not in historical, or exhausted)
                result.Add(new DiffSpan(t.Start, t.Length, DiffKind.Added));
            }
        }

        return result;
    }

    private readonly record struct Token(string Text, int Start, int Length);

    private static List<Token> Tokenize(string text)
    {
        var tokens = new List<Token>();
        int i = 0;
        while (i < text.Length)
        {
            // Skip whitespace
            while (i < text.Length && char.IsWhiteSpace(text[i])) i++;
            if (i >= text.Length) break;

            int start = i;
            // Consume non-whitespace
            while (i < text.Length && !char.IsWhiteSpace(text[i])) i++;

            tokens.Add(new Token(text[start..i], start, i - start));
        }
        return tokens;
    }
}
