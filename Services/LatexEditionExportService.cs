// Services/LatexEditionExportService.cs
// Exports a critical edition as a compilable LaTeX file using the reledmac package.

using System.Collections.Generic;
using System.Linq;
using System.Text;
using ReadZen.App.Models;

namespace ReadZen.App.Services;

/// <summary>
/// Generates a compilable LaTeX document with inline critical apparatus using reledmac.
/// </summary>
public static class LatexEditionExportService
{
    /// <summary>
    /// Produces a complete .tex file with the base text annotated by apparatus entries.
    /// Each locus in the base text that has an apparatus entry is wrapped in
    /// <c>\edtext{lemma}{\Afootnote{variant W1 | variant2 W2}}</c>.
    /// </summary>
    public static string ExportLatex(
        string baseText,
        ApparatusInfo apparatus,
        string title,
        string author)
    {
        var sb = new StringBuilder();

        // Preamble
        sb.AppendLine(@"\documentclass{article}");
        sb.AppendLine(@"\usepackage{reledmac}");
        sb.AppendLine(@"\usepackage{xeCJK}");
        sb.AppendLine(@"\setCJKmainfont{Noto Sans CJK SC}");
        sb.Append(@"\title{");
        sb.Append(LatexEscape(title));
        sb.AppendLine("}");
        sb.Append(@"\author{");
        sb.Append(LatexEscape(author));
        sb.AppendLine("}");
        sb.AppendLine();
        sb.AppendLine(@"\begin{document}");
        sb.AppendLine(@"\maketitle");
        sb.AppendLine();
        sb.AppendLine(@"\beginnumbering");

        // Build a lookup from lemma text to apparatus entries for inline annotation.
        var entryByLemma = BuildLemmaLookup(apparatus);

        // Process base text paragraph by paragraph.
        var paragraphs = baseText.Split('\n');
        foreach (var para in paragraphs)
        {
            var trimmed = para.TrimEnd('\r');
            if (string.IsNullOrWhiteSpace(trimmed)) continue;

            sb.AppendLine(@"\pstart");
            sb.AppendLine(AnnotateParagraph(trimmed, entryByLemma));
            sb.AppendLine(@"\pend");
        }

        sb.AppendLine(@"\endnumbering");
        sb.AppendLine(@"\end{document}");

        return sb.ToString();
    }

    private static Dictionary<string, ApparatusEntry> BuildLemmaLookup(ApparatusInfo apparatus)
    {
        var dict = new Dictionary<string, ApparatusEntry>();
        if (apparatus.Entries == null) return dict;

        foreach (var entry in apparatus.Entries)
        {
            if (!string.IsNullOrEmpty(entry.Lemma) && !dict.ContainsKey(entry.Lemma))
                dict[entry.Lemma] = entry;
        }

        return dict;
    }

    private static string AnnotateParagraph(
        string text,
        Dictionary<string, ApparatusEntry> entryByLemma)
    {
        if (entryByLemma.Count == 0)
            return LatexEscape(text);

        var sb = new StringBuilder();
        int pos = 0;

        while (pos < text.Length)
        {
            // Try to match any lemma at the current position (longest match first).
            ApparatusEntry? matched = null;
            string? matchedLemma = null;

            foreach (var kvp in entryByLemma.OrderByDescending(k => k.Key.Length))
            {
                if (pos + kvp.Key.Length <= text.Length &&
                    text.Substring(pos, kvp.Key.Length) == kvp.Key)
                {
                    matched = kvp.Value;
                    matchedLemma = kvp.Key;
                    break;
                }
            }

            if (matched != null && matchedLemma != null)
            {
                sb.Append(@"\edtext{");
                sb.Append(LatexEscape(matchedLemma));
                sb.Append(@"}{\Afootnote{");
                sb.Append(FormatFootnote(matched));
                sb.Append("}}");
                pos += matchedLemma.Length;
            }
            else
            {
                // Emit one character (escape if needed).
                sb.Append(LatexEscapeChar(text[pos]));
                pos++;
            }
        }

        return sb.ToString();
    }

    private static string FormatFootnote(ApparatusEntry entry)
    {
        if (entry.Readings is not { Count: > 0 })
            return "";

        var parts = entry.Readings.Select(r =>
        {
            var reading = LatexEscape(r.Reading ?? "—");
            var wit = r.WitnessId ?? "";
            return string.IsNullOrEmpty(wit) ? reading : $"{reading} {LatexEscape(wit)}";
        });

        return string.Join(" | ", parts);
    }

    private static string LatexEscape(string text)
    {
        var sb = new StringBuilder(text.Length);
        foreach (var ch in text)
            sb.Append(LatexEscapeChar(ch));
        return sb.ToString();
    }

    private static string LatexEscapeChar(char ch) => ch switch
    {
        '#' => @"\#",
        '$' => @"\$",
        '%' => @"\%",
        '&' => @"\&",
        '_' => @"\_",
        '{' => @"\{",
        '}' => @"\}",
        '~' => @"\textasciitilde{}",
        '^' => @"\textasciicircum{}",
        '\\' => @"\textbackslash{}",
        _ => ch.ToString(),
    };
}
