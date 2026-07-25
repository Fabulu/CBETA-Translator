// Text/HeadingDeduplicator.cs
using System.Collections.Generic;
using ReadZen.App.Models;

namespace ReadZen.App.Text;

/// <summary>
/// Render-layer dedupe for document headings (outline / TOC / navigation entries).
///
/// CBETA TEI legitimately repeats a section heading: e.g. T48n2005 (無門關) has two
/// consecutive &lt;cb:div type="xu"&gt; preface divisions, BOTH headed 禪宗無門關, so an
/// outline built straight from the &lt;head&gt; stream lists the same heading twice in a
/// row. The corpus XML is canonical and never edited, so we collapse the redundant
/// REPEAT at the render layer.
///
/// Rule (deliberately conservative — see the run SPEC):
///   Collapse a RUN of CONSECUTIVE heading entries that share the same Level AND
///   whitespace-normalized, ordinally-equal Text. The FIRST entry survives (keeping
///   its rendered offset / navigation target); the immediately-following identical
///   repeats are dropped. Two headings with the same text but a different level, or
///   separated by any different heading, are treated as distinct and BOTH kept — we
///   never merge two genuinely separate sections that merely share a title far apart.
///
/// Deterministic and locale-independent (Ordinal comparisons only; the project sets
/// InvariantGlobalization=true).
/// </summary>
public static class HeadingDeduplicator
{
    /// <summary>
    /// Returns a new list with adjacent identical headings collapsed to their first
    /// occurrence. Input order is preserved. A null/empty input yields an empty list.
    /// </summary>
    public static List<HeadingInfo> Dedupe(IReadOnlyList<HeadingInfo>? headings)
    {
        var result = new List<HeadingInfo>();
        if (headings == null || headings.Count == 0) return result;

        bool havePrev = false;
        string prevKey = "";
        int prevLevel = 0;

        foreach (var h in headings)
        {
            var key = NormalizeWhitespace(h.Text);
            if (havePrev && h.Level == prevLevel && string.Equals(key, prevKey, System.StringComparison.Ordinal))
                continue; // adjacent identical heading — drop the redundant repeat

            result.Add(h);
            havePrev = true;
            prevKey = key;
            prevLevel = h.Level;
        }

        return result;
    }

    /// <summary>Collapses internal whitespace runs to a single space and trims the ends.</summary>
    private static string NormalizeWhitespace(string? text)
    {
        if (string.IsNullOrEmpty(text)) return "";
        var sb = new System.Text.StringBuilder(text.Length);
        bool inRun = false;
        foreach (var ch in text)
        {
            if (ch == ' ' || ch == '\t' || ch == '\r' || ch == '\n' || ch == '　')
            {
                inRun = true;
                continue;
            }
            if (inRun && sb.Length > 0) sb.Append(' ');
            inRun = false;
            sb.Append(ch);
        }
        return sb.ToString();
    }
}
