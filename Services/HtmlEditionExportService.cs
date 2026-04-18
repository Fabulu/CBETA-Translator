// Services/HtmlEditionExportService.cs
// Generates a standalone HTML critical-edition export.

using System.Collections.Generic;
using System.Text;
using System.Web;
using ReadZen.App.Models;

namespace ReadZen.App.Services;

public static class HtmlEditionExportService
{
    public static string ExportHtml(
        string title, string author, string baseText,
        ApparatusInfo? apparatus, WitnessTextRegistry? witnesses)
    {
        var sb = new StringBuilder(8192);
        var safeTitle = Esc(title);
        var entries = apparatus?.Entries;

        sb.Append("<!DOCTYPE html>\n<html lang=\"zh\">\n<head>\n<meta charset=\"UTF-8\">\n<title>");
        sb.Append(safeTitle);
        sb.Append("</title>\n<style>\n");
        sb.Append(Css);
        sb.Append("\n</style>\n</head>\n<body>\n");

        // Header
        sb.Append("<header>\n<h1>").Append(safeTitle).Append("</h1>\n");
        sb.Append("<p class=\"author\">").Append(Esc(author)).Append("</p>\n</header>\n\n");

        // Witness legend
        if (witnesses?.Witnesses is { Count: > 0 } wits)
        {
            sb.Append("<section class=\"witness-legend\">\n<h2>Witnesses</h2>\n<table>\n");
            sb.Append("<tr><th>Siglum</th><th>Label</th><th>Role</th></tr>\n");
            foreach (var w in wits)
            {
                sb.Append("<tr><td>").Append(Esc(w.Siglum)).Append("</td><td>")
                  .Append(Esc(w.Label)).Append("</td><td>").Append(Esc(w.Role))
                  .Append("</td></tr>\n");
            }
            sb.Append("</table>\n</section>\n\n");
        }

        // Build locus-to-entry index for superscript refs
        var locusIndex = new Dictionary<string, List<int>>();
        if (entries is { Count: > 0 })
        {
            for (int i = 0; i < entries.Count; i++)
            {
                var loc = entries[i].LocusId ?? "";
                if (!locusIndex.TryGetValue(loc, out var list))
                    locusIndex[loc] = list = new List<int>();
                list.Add(i + 1);
            }
        }

        // Text body
        sb.Append("<section class=\"text\">\n");
        var lines = baseText.Split('\n');
        for (int n = 0; n < lines.Length; n++)
        {
            var raw = lines[n];
            if (string.IsNullOrWhiteSpace(raw)) continue;

            // Try to extract a locus id from a leading marker like [lb-0001]
            string locus = "", lineText = raw;
            if (raw.StartsWith("[lb-"))
            {
                int close = raw.IndexOf(']');
                if (close > 0)
                {
                    locus = raw.Substring(4, close - 4);
                    lineText = raw.Substring(close + 1).TrimStart();
                }
            }

            var id = locus.Length > 0 ? locus : (n + 1).ToString();
            sb.Append("<div class=\"line\" id=\"lb-").Append(Esc(id)).Append("\">");
            sb.Append("<span class=\"line-num\">").Append(Esc(id)).Append("</span>");
            sb.Append("<span class=\"text\">").Append(Esc(lineText));

            if (locus.Length > 0 && locusIndex.TryGetValue(locus, out var refs))
            {
                foreach (var r in refs)
                    sb.Append("<sup class=\"app-ref\">").Append(r).Append("</sup>");
            }
            sb.Append("</span></div>\n");
        }
        sb.Append("</section>\n\n");

        // Apparatus criticus
        if (entries is { Count: > 0 })
        {
            sb.Append("<section class=\"apparatus\">\n<h2>Apparatus Criticus</h2>\n");
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                sb.Append("<div class=\"entry\">");
                sb.Append("<span class=\"ref\">").Append(i + 1).Append(")</span> ");
                sb.Append("<span class=\"lemma\">").Append(Esc(e.Lemma)).Append("</span> ] ");

                if (e.Readings is { Count: > 0 })
                {
                    for (int r = 0; r < e.Readings.Count; r++)
                    {
                        if (r > 0) sb.Append(" | ");
                        var rd = e.Readings[r];
                        sb.Append("<span class=\"reading\">").Append(Esc(rd.Reading))
                          .Append(" <i>").Append(Esc(rd.WitnessId)).Append("</i></span>");
                    }
                }
                sb.Append("</div>\n");
            }
            sb.Append("</section>\n\n");
        }

        sb.Append("</body>\n</html>");
        return sb.ToString();
    }

    private static string Esc(string? s) => HttpUtility.HtmlEncode(s ?? "");

    private const string Css = @"
:root { --fg: #1a1a1a; --bg: #faf9f6; --muted: #888; --border: #ccc; --accent: #8b0000; }
@media (prefers-color-scheme: dark) {
  :root { --fg: #d4d4d4; --bg: #1e1e1e; --muted: #777; --border: #444; --accent: #e87070; }
}
* { margin: 0; padding: 0; box-sizing: border-box; }
body { font: 18px/1.8 'Noto Serif CJK SC','Source Han Serif',serif;
       color: var(--fg); background: var(--bg); max-width: 800px; margin: 0 auto; padding: 2rem 1rem; }
header { text-align: center; margin-bottom: 2rem; border-bottom: 1px solid var(--border); padding-bottom: 1rem; }
h1 { font-size: 1.6rem; } h2 { font-size: 1.2rem; margin: 1.5rem 0 .75rem; }
.author { color: var(--muted); font-style: italic; }
.witness-legend table { width: 100%; border-collapse: collapse; font-size: .9rem; }
.witness-legend th, .witness-legend td { text-align: left; padding: .25rem .5rem; border-bottom: 1px solid var(--border); }
.line { display: flex; gap: .75rem; align-items: baseline; }
.line-num { min-width: 3rem; text-align: right; font-size: .75rem; color: var(--muted); flex-shrink: 0; }
.text { flex: 1; }
.app-ref { color: var(--accent); font-size: .7rem; margin-left: 1px; cursor: default; }
.apparatus { border-top: 1px solid var(--border); padding-top: 1rem; font-size: .9rem; }
.entry { padding-left: 2rem; text-indent: -2rem; margin-bottom: .35rem; }
.ref { font-weight: bold; color: var(--accent); }
.lemma { font-weight: bold; }
.reading i { color: var(--muted); font-size: .85em; }
@media print { body { max-width: 100%; font-size: 11pt; } .apparatus { page-break-before: always; } }";
}
