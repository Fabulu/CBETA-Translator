using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CbetaTranslator.App.Models;

namespace CbetaTranslator.App.Services;

public sealed class ScholarExportService : IScholarExportService
{
    public async Task ExportAsync(string filePath, ScholarCollection collection, ScholarExportFormat format, CancellationToken ct = default)
    {
        var content = format switch
        {
            ScholarExportFormat.Html => BuildHtml(collection),
            ScholarExportFormat.Markdown => BuildMarkdown(collection),
            ScholarExportFormat.PlainText => BuildPlainText(collection),
            ScholarExportFormat.Csv => BuildDelimited(collection, ","),
            ScholarExportFormat.Tsv => BuildDelimited(collection, "	"),
            ScholarExportFormat.BibTex => BuildBibTex(collection),
            _ => throw new ArgumentOutOfRangeException(nameof(format)),
        };

        await File.WriteAllTextAsync(filePath, content, Encoding.UTF8, ct);
    }

    private static readonly string[] DelimitedHeaders =
    {
        "collection_id",
        "collection_name",
        "collection_description",
        "collection_tags",
        "collection_created_by",
        "collection_created_utc",
        "collection_modified_utc",
        "study_notes",
        "passage_id",
        "source_title",
        "source_rel_path",
        "zh_text",
        "en_text",
        "notes",
        "tags",
        "master_names",
        "doctrinal_topic",
        "literary_form",
        "lineage",
        "rhetorical_function",
        "linked_texts",
        "from_lb",
        "to_lb",
        "start_block",
        "end_block",
        "created_by",
        "added_utc",
        "modified_utc",
        "zen_link",
        "share_url"
    };
    private static string BuildHtml(ScholarCollection collection)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset=\"UTF-8\">");
        sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        sb.AppendLine($"<title>{Esc(collection.Name)}</title>");
        sb.AppendLine("<style>");
        sb.AppendLine(HtmlCss);
        sb.AppendLine("</style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");

        sb.AppendLine($"<h1>{Esc(collection.Name)}</h1>");
        if (!string.IsNullOrWhiteSpace(collection.Description))
            sb.AppendLine($"<p class=\"subtitle\">{Esc(collection.Description)}</p>");

        AppendHtmlMetadataList(sb, BuildCollectionMetadata(collection), "collection-meta");

        if (collection.Tags.Count > 0)
        {
            sb.AppendLine("<div class=\"tags\">");
            foreach (var t in collection.Tags)
                sb.AppendLine($"<span class=\"chip tag-chip\">{Esc(t)}</span>");
            sb.AppendLine("</div>");
        }

        sb.AppendLine("<hr>");

        for (int i = 0; i < collection.Passages.Count; i++)
        {
            var p = collection.Passages[i];
            sb.AppendLine($"<div class=\"card\" id=\"passage-{Esc(p.Id)}\">");
            sb.AppendLine($"<div class=\"card-header\">Passage {i + 1}</div>");
            sb.AppendLine($"<div class=\"source\">{Esc(ExtractSourceTitle(p.SourceRelPath))}</div>");
            AppendHtmlMetadataList(sb, BuildPassageMetadata(p), "meta-list");

            if (!string.IsNullOrWhiteSpace(p.ZhText))
                sb.AppendLine($"<div class=\"zh\">{Esc(p.ZhText)}</div>");

            if (!string.IsNullOrWhiteSpace(p.EnText))
                sb.AppendLine($"<div class=\"en\">{Esc(p.EnText)}</div>");

            if (p.Tags.Count > 0)
            {
                sb.AppendLine("<div class=\"tags\">");
                foreach (var t in p.Tags)
                    sb.AppendLine($"<span class=\"chip tag-chip\">{Esc(t)}</span>");
                sb.AppendLine("</div>");
            }

            if (p.MasterNames.Count > 0)
            {
                sb.AppendLine("<div class=\"tags\">");
                foreach (var m in p.MasterNames)
                    sb.AppendLine($"<span class=\"chip master-chip\">{Esc(m)}</span>");
                sb.AppendLine("</div>");
            }

            if (!string.IsNullOrWhiteSpace(p.Notes))
                sb.AppendLine($"<div class=\"notes\">{Esc(p.Notes)}</div>");

            var cats = BuildCategoryList(p);
            if (cats.Count > 0)
            {
                sb.AppendLine("<div class=\"tags\">");
                foreach (var c in cats)
                    sb.AppendLine($"<span class=\"chip category-chip\">{Esc(c)}</span>");
                sb.AppendLine("</div>");
            }

            sb.AppendLine("</div>");
        }

        var links = collection.Links?.Where(l => IsValidLink(collection, l)).ToList();
        if (links != null && links.Count > 0)
        {
            sb.AppendLine("<hr>");
            sb.AppendLine("<h2>Cross-References</h2>");

            if (collection.Passages.Count <= 20)
                sb.AppendLine(BuildLinkSvg(collection));
            else
                sb.AppendLine(BuildLinkTable(collection));

            sb.AppendLine("<div class=\"links-list\">");
            foreach (var link in links)
            {
                var fromLabel = FindPassageLabel(collection, link.FromPassageId);
                var toLabel = FindPassageLabel(collection, link.ToPassageId);
                sb.AppendLine("<div class=\"link-entry\">");
                sb.AppendLine($"<a href=\"#passage-{Esc(link.FromPassageId)}\">{Esc(fromLabel)}</a>");
                sb.AppendLine($" <span class=\"relation\">{Esc(link.RelationType)}</span> ");
                sb.AppendLine($"<a href=\"#passage-{Esc(link.ToPassageId)}\">{Esc(toLabel)}</a>");
                if (!string.IsNullOrWhiteSpace(link.Note))
                    sb.AppendLine($" <span class=\"link-note\">({Esc(link.Note)})</span>");
                sb.AppendLine("</div>");
            }
            sb.AppendLine("</div>");
        }

        sb.AppendLine("</body>");
        sb.AppendLine("</html>");
        return sb.ToString();
    }

    private static string BuildLinkSvg(ScholarCollection collection)
    {
        var passages = collection.Passages;
        var links = collection.Links ?? new List<PassageLink>();
        if (passages.Count == 0) return "";

        const int width = 600;
        const int height = 600;
        int cx = width / 2;
        int cy = height / 2;
        int radius = Math.Min(cx, cy) - 60;

        var idToIndex = new Dictionary<string, int>();
        for (int i = 0; i < passages.Count; i++)
            idToIndex[passages[i].Id] = i;

        var positions = new (double x, double y)[passages.Count];
        for (int i = 0; i < passages.Count; i++)
        {
            double angle = 2 * Math.PI * i / passages.Count - Math.PI / 2;
            positions[i] = (cx + radius * Math.Cos(angle), cy + radius * Math.Sin(angle));
        }

        var relationColors = new Dictionary<string, string>
        {
            ["quotes"] = "#4A90D9",
            ["alludes-to"] = "#7B68EE",
            ["comments-on"] = "#2ECC71",
            ["contradicts"] = "#E74C3C",
            ["parallels"] = "#F39C12",
            ["responds-to"] = "#1ABC9C",
        };

        var sb = new StringBuilder();
        sb.AppendLine($"<svg viewBox=\"0 0 {width} {height}\" width=\"{width}\" height=\"{height}\" xmlns=\"http://www.w3.org/2000/svg\" style=\"display:block;margin:20px auto;\">");

        foreach (var link in links)
        {
            if (!idToIndex.TryGetValue(link.FromPassageId, out int fi)) continue;
            if (!idToIndex.TryGetValue(link.ToPassageId, out int ti)) continue;

            string color = relationColors.GetValueOrDefault(link.RelationType, "#999");
            var (x1, y1) = positions[fi];
            var (x2, y2) = positions[ti];
            sb.AppendLine($"<line x1=\"{x1:F1}\" y1=\"{y1:F1}\" x2=\"{x2:F1}\" y2=\"{y2:F1}\" stroke=\"{color}\" stroke-width=\"2\" opacity=\"0.7\"/>");
        }

        for (int i = 0; i < passages.Count; i++)
        {
            var (x, y) = positions[i];
            string label = passages[i].ZhText.Length > 10 ? passages[i].ZhText[..10] : passages[i].ZhText;

            sb.AppendLine($"<circle cx=\"{x:F1}\" cy=\"{y:F1}\" r=\"22\" fill=\"#3A3F4B\" stroke=\"#888\" stroke-width=\"1.5\"/>");
            sb.AppendLine($"<text x=\"{x:F1}\" y=\"{y + 35:F1}\" text-anchor=\"middle\" font-size=\"11\" fill=\"#CCC\">{Esc(label)}</text>");
            sb.AppendLine($"<text x=\"{x:F1}\" y=\"{y + 4:F1}\" text-anchor=\"middle\" font-size=\"11\" fill=\"#FFF\">{i + 1}</text>");
        }

        int ly = 20;
        foreach (var kv in relationColors)
        {
            sb.AppendLine($"<rect x=\"10\" y=\"{ly}\" width=\"14\" height=\"14\" fill=\"{kv.Value}\" rx=\"2\"/>");
            sb.AppendLine($"<text x=\"30\" y=\"{ly + 12}\" font-size=\"11\" fill=\"#AAA\">{Esc(kv.Key)}</text>");
            ly += 20;
        }

        sb.AppendLine("</svg>");
        return sb.ToString();
    }

    private static string BuildLinkTable(ScholarCollection collection)
    {
        var links = collection.Links ?? new List<PassageLink>();
        var sb = new StringBuilder();
        sb.AppendLine("<table class=\"links-table\">");
        sb.AppendLine("<thead><tr><th>From</th><th>Relation</th><th>To</th><th>Note</th></tr></thead>");
        sb.AppendLine("<tbody>");
        foreach (var link in links)
        {
            var fromLabel = FindPassageLabel(collection, link.FromPassageId);
            var toLabel = FindPassageLabel(collection, link.ToPassageId);
            sb.AppendLine("<tr>");
            sb.AppendLine($"<td><a href=\"#passage-{Esc(link.FromPassageId)}\">{Esc(fromLabel)}</a></td>");
            sb.AppendLine($"<td>{Esc(link.RelationType)}</td>");
            sb.AppendLine($"<td><a href=\"#passage-{Esc(link.ToPassageId)}\">{Esc(toLabel)}</a></td>");
            sb.AppendLine($"<td>{Esc(link.Note ?? "")}</td>");
            sb.AppendLine("</tr>");
        }
        sb.AppendLine("</tbody></table>");
        return sb.ToString();
    }

    private static string FindPassageLabel(ScholarCollection collection, string passageId)
    {
        for (int i = 0; i < collection.Passages.Count; i++)
        {
            if (collection.Passages[i].Id == passageId)
            {
                var zh = collection.Passages[i].ZhText;
                string preview = zh.Length > 15 ? zh[..15] + "..." : zh;
                return $"#{i + 1} {preview}";
            }
        }

        return passageId;
    }

    private static bool IsValidLink(ScholarCollection collection, PassageLink link)
    {
        bool fromExists = collection.Passages.Any(p => p.Id == link.FromPassageId);
        bool toExists = collection.Passages.Any(p => p.Id == link.ToPassageId);
        return fromExists && toExists;
    }

    private const string HtmlCss = @"
* { margin: 0; padding: 0; box-sizing: border-box; }
body {
    font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Helvetica, Arial, sans-serif;
    background: #1E1E2E;
    color: #CDD6F4;
    max-width: 900px;
    margin: 0 auto;
    padding: 30px 20px;
    line-height: 1.6;
}
h1 { font-size: 1.8em; margin-bottom: 8px; color: #E8E8F0; }
h2 { font-size: 1.4em; margin: 16px 0 10px; color: #E8E8F0; }
.subtitle { font-size: 1em; color: #A0A0B8; margin-bottom: 12px; }
hr { border: none; border-top: 1px solid #444; margin: 20px 0; }
.card {
    background: #282838;
    border: 1px solid #3A3A4A;
    border-radius: 8px;
    padding: 18px;
    margin-bottom: 16px;
}
.card-header { font-weight: 600; font-size: 0.85em; color: #888; margin-bottom: 6px; }
.source { font-size: 0.85em; color: #8888AA; margin-bottom: 10px; }
.collection-meta,
.meta-list {
    list-style: none;
    margin: 0 0 10px 0;
    padding: 0;
}
.collection-meta li,
.meta-list li {
    font-size: 0.82em;
    color: #9BA3C7;
    margin: 2px 0;
}
.meta-label {
    color: #C6D0F5;
    font-weight: 600;
}
.meta-link {
    color: #7AABFF;
    text-decoration: none;
}
.meta-link:hover { text-decoration: underline; }
.zh {
    font-family: 'Noto Serif CJK SC', 'Source Han Serif SC', 'SimSun', serif;
    font-size: 1.3em;
    line-height: 1.8;
    color: #E0E0F0;
    margin-bottom: 12px;
    padding: 8px 0;
}
.en { font-size: 1em; line-height: 1.6; color: #C0C0D0; margin-bottom: 10px; }
.tags { margin: 6px 0; }
.chip {
    display: inline-block;
    padding: 2px 10px;
    border-radius: 12px;
    font-size: 0.8em;
    margin: 2px 4px 2px 0;
}
.tag-chip { background: #3A4A5A; color: #88BBEE; }
.master-chip { background: #4A3A5A; color: #CC88EE; }
.category-chip { background: #3A4A3A; color: #88EE88; }
.notes {
    font-size: 0.9em;
    color: #909098;
    font-style: italic;
    padding: 8px;
    background: #222232;
    border-radius: 4px;
    margin-top: 6px;
}
.links-list { margin: 12px 0; }
.link-entry { margin: 6px 0; font-size: 0.95em; }
.link-entry a { color: #7AABFF; text-decoration: none; }
.link-entry a:hover { text-decoration: underline; }
.relation {
    display: inline-block;
    padding: 1px 8px;
    background: #3A3A4A;
    border-radius: 8px;
    font-size: 0.85em;
    color: #AAA;
}
.link-note { color: #777; font-size: 0.85em; }
.links-table { width: 100%; border-collapse: collapse; margin: 12px 0; }
.links-table th, .links-table td { padding: 8px 12px; border: 1px solid #3A3A4A; text-align: left; }
.links-table th { background: #2A2A3A; font-size: 0.85em; color: #AAA; }
.links-table a { color: #7AABFF; text-decoration: none; }
.links-table a:hover { text-decoration: underline; }
";

    private static string BuildMarkdown(ScholarCollection collection)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# {collection.Name}");
        sb.AppendLine();

        if (!string.IsNullOrWhiteSpace(collection.Description))
        {
            sb.AppendLine(collection.Description);
            sb.AppendLine();
        }

        AppendMarkdownMetadata(sb, BuildCollectionMetadata(collection));
        if (sb.Length > 0)
            sb.AppendLine();

        sb.AppendLine("---");
        sb.AppendLine();

        for (int i = 0; i < collection.Passages.Count; i++)
        {
            var p = collection.Passages[i];
            sb.AppendLine($"## Passage {i + 1}");
            sb.AppendLine();
            sb.AppendLine($"**Source:** {ExtractSourceTitle(p.SourceRelPath)}");
            AppendMarkdownMetadata(sb, BuildPassageMetadata(p));
            sb.AppendLine();

            if (!string.IsNullOrWhiteSpace(p.ZhText))
            {
                foreach (var line in p.ZhText.Split('\n'))
                    sb.AppendLine($"> {line.TrimEnd('\r')}");
                sb.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(p.EnText))
            {
                sb.AppendLine(p.EnText);
                sb.AppendLine();
            }

            if (p.Tags.Count > 0)
                sb.AppendLine($"**Tags:** {string.Join(", ", p.Tags)}");

            if (p.MasterNames.Count > 0)
                sb.AppendLine($"**Masters:** {string.Join(", ", p.MasterNames)}");

            if (!string.IsNullOrWhiteSpace(p.Notes))
                sb.AppendLine($"**Notes:** {p.Notes}");

            var cats = BuildCategoryList(p);
            if (cats.Count > 0)
                sb.AppendLine($"**Categories:** {string.Join(" ? ", cats)}");

            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
        }

        var links = collection.Links?.Where(l => IsValidLink(collection, l)).ToList();
        if (links != null && links.Count > 0)
        {
            sb.AppendLine("## Cross-References");
            sb.AppendLine();

            foreach (var link in links)
            {
                var fromLabel = FindPassageLabel(collection, link.FromPassageId);
                var toLabel = FindPassageLabel(collection, link.ToPassageId);
                sb.Append($"- {fromLabel} **{link.RelationType}** {toLabel}");
                if (!string.IsNullOrWhiteSpace(link.Note))
                    sb.Append($" ({link.Note})");
                sb.AppendLine();
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static string BuildPlainText(ScholarCollection collection)
    {
        var sb = new StringBuilder();
        sb.AppendLine(collection.Name);
        sb.AppendLine(new string('=', collection.Name.Length));
        sb.AppendLine();

        if (!string.IsNullOrWhiteSpace(collection.Description))
        {
            sb.AppendLine(collection.Description);
            sb.AppendLine();
        }

        AppendPlainTextMetadata(sb, BuildCollectionMetadata(collection));
        if (sb.Length > 0)
            sb.AppendLine();

        for (int i = 0; i < collection.Passages.Count; i++)
        {
            var p = collection.Passages[i];
            sb.AppendLine($"Passage {i + 1}");
            sb.AppendLine($"Source: {ExtractSourceTitle(p.SourceRelPath)}");
            AppendPlainTextMetadata(sb, BuildPassageMetadata(p));
            sb.AppendLine();

            if (!string.IsNullOrWhiteSpace(p.ZhText))
            {
                sb.AppendLine(p.ZhText);
                sb.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(p.EnText))
            {
                sb.AppendLine(p.EnText);
                sb.AppendLine();
            }

            if (p.Tags.Count > 0)
                sb.AppendLine($"Tags: {string.Join(", ", p.Tags)}");

            if (p.MasterNames.Count > 0)
                sb.AppendLine($"Masters: {string.Join(", ", p.MasterNames)}");

            if (!string.IsNullOrWhiteSpace(p.Notes))
                sb.AppendLine($"Notes: {p.Notes}");

            var cats = BuildCategoryList(p);
            if (cats.Count > 0)
                sb.AppendLine($"Categories: {string.Join(", ", cats)}");

            sb.AppendLine();
        }

        var links = collection.Links?.Where(l => IsValidLink(collection, l)).ToList();
        if (links != null && links.Count > 0)
        {
            sb.AppendLine("Cross-References");
            sb.AppendLine(new string('-', 16));
            foreach (var link in links)
            {
                var fromLabel = FindPassageLabel(collection, link.FromPassageId);
                var toLabel = FindPassageLabel(collection, link.ToPassageId);
                sb.Append($"{fromLabel} -- {link.RelationType} --> {toLabel}");
                if (!string.IsNullOrWhiteSpace(link.Note))
                    sb.Append($" ({link.Note})");
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }

    private static List<string> BuildCategoryList(ScholarPassage p)
    {
        var cats = new List<string>();
        if (!string.IsNullOrWhiteSpace(p.DoctrinalTopic)) cats.Add($"Topic: {p.DoctrinalTopic}");
        if (!string.IsNullOrWhiteSpace(p.LiteraryForm)) cats.Add($"Form: {p.LiteraryForm}");
        if (!string.IsNullOrWhiteSpace(p.Lineage)) cats.Add($"Lineage: {p.Lineage}");
        if (!string.IsNullOrWhiteSpace(p.RhetoricalFunction)) cats.Add($"Function: {p.RhetoricalFunction}");
        return cats;
    }

    private static List<KeyValuePair<string, string>> BuildCollectionMetadata(ScholarCollection collection)
    {
        var items = new List<KeyValuePair<string, string>>();
        AddMetadata(items, "Created by", collection.CreatedBy);
        AddMetadata(items, "Created", FormatTimestamp(collection.CreatedUtc));
        AddMetadata(items, "Modified", FormatTimestamp(collection.ModifiedUtc));
        return items;
    }

    private static List<KeyValuePair<string, string>> BuildPassageMetadata(ScholarPassage passage)
    {
        var items = new List<KeyValuePair<string, string>>();
        AddMetadata(items, "Created by", passage.CreatedBy);
        AddMetadata(items, "Path", passage.SourceRelPath);
        AddMetadata(items, "Line breaks", FormatLineBreakRange(passage.FromLb, passage.ToLb));
        AddMetadata(items, "Blocks", FormatBlockRange(passage.StartBlockNumber, passage.EndBlockNumber));
        AddMetadata(items, "Added", FormatTimestamp(passage.AddedUtc));
        AddMetadata(items, "Modified", FormatTimestamp(passage.ModifiedUtc));

        var zenLink = BuildZenLink(passage);
        if (!string.IsNullOrWhiteSpace(zenLink))
            items.Add(new KeyValuePair<string, string>("Zen link", zenLink));

        var shareUrl = BuildShareUrl(passage);
        if (!string.IsNullOrWhiteSpace(shareUrl))
            items.Add(new KeyValuePair<string, string>("Share URL", shareUrl));

        return items;
    }

    private static void AppendHtmlMetadataList(StringBuilder sb, List<KeyValuePair<string, string>> items, string cssClass)
    {
        if (items.Count == 0)
            return;

        sb.AppendLine($"<ul class=\"{cssClass}\">");
        foreach (var item in items)
        {
            var isLink = item.Key.EndsWith("link", StringComparison.OrdinalIgnoreCase) || item.Key.EndsWith("URL", StringComparison.OrdinalIgnoreCase);
            var value = isLink
                ? $"<a class=\"meta-link\" href=\"{Esc(item.Value)}\">{Esc(item.Value)}</a>"
                : Esc(item.Value);
            sb.AppendLine($"<li><span class=\"meta-label\">{Esc(item.Key)}:</span> {value}</li>");
        }
        sb.AppendLine("</ul>");
    }

    private static void AppendMarkdownMetadata(StringBuilder sb, List<KeyValuePair<string, string>> items)
    {
        foreach (var item in items)
            sb.AppendLine($"**{item.Key}:** {item.Value}");
    }

    private static void AppendPlainTextMetadata(StringBuilder sb, List<KeyValuePair<string, string>> items)
    {
        foreach (var item in items)
            sb.AppendLine($"{item.Key}: {item.Value}");
    }

    private static void AddMetadata(List<KeyValuePair<string, string>> items, string label, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            items.Add(new KeyValuePair<string, string>(label, value));
    }

    private static string FormatTimestamp(DateTimeOffset value) =>
        value == default ? "" : value.ToUniversalTime().ToString("yyyy-MM-dd HH:mm:ss 'UTC'");

    private static string FormatTimestamp(DateTimeOffset? value) =>
        value.HasValue ? FormatTimestamp(value.Value) : "";

    private static string FormatLineBreakRange(string? fromLb, string? toLb)
    {
        if (string.IsNullOrWhiteSpace(fromLb))
            return "";
        if (string.IsNullOrWhiteSpace(toLb) || string.Equals(fromLb, toLb, StringComparison.Ordinal))
            return fromLb;
        return $"{fromLb} - {toLb}";
    }

    private static string FormatBlockRange(int? startBlockNumber, int? endBlockNumber)
    {
        if (!startBlockNumber.HasValue)
            return "";
        if (!endBlockNumber.HasValue || startBlockNumber.Value == endBlockNumber.Value)
            return startBlockNumber.Value.ToString();
        return $"{startBlockNumber.Value} - {endBlockNumber.Value}";
    }

    private static string? BuildZenLink(ScholarPassage passage)
    {
        if (string.IsNullOrWhiteSpace(passage.SourceRelPath))
            return null;

        var block = passage.StartBlockNumber ?? passage.EndBlockNumber;
        return CbetaUriParser.BuildUri(
            passage.SourceRelPath,
            fromLb: passage.FromLb,
            toLb: passage.ToLb,
            blockNumber: block);
    }

    private static string? BuildShareUrl(ScholarPassage passage)
    {
        if (string.IsNullOrWhiteSpace(passage.SourceRelPath))
            return null;

        return CbetaUriParser.BuildShareableUrl(
            passage.SourceRelPath,
            fromLb: passage.FromLb,
            toLb: passage.ToLb);
    }

    private static string BuildBibTex(ScholarCollection collection)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"% Read Zen Scholar export: {collection.Name}");
        if (!string.IsNullOrWhiteSpace(collection.Description))
            sb.AppendLine($"% Description: {collection.Description.Replace("\r", " ").Replace("\n", " ")}");
        sb.AppendLine();

        for (int i = 0; i < collection.Passages.Count; i++)
        {
            if (i > 0)
                sb.AppendLine();
            AppendBibTexEntry(sb, collection, collection.Passages[i], i + 1);
        }

        return sb.ToString();
    }

    private static void AppendBibTexEntry(StringBuilder sb, ScholarCollection collection, ScholarPassage passage, int index)
    {
        var key = BuildBibTexKey(collection, passage, index);
        sb.AppendLine($"@misc{{{key},");
        AppendBibTexField(sb, "title", BuildBibTexTitle(passage, index), true);
        AppendBibTexField(sb, "howpublished", BuildZenLink(passage));
        AppendBibTexField(sb, "url", BuildShareUrl(passage));
        AppendBibTexField(sb, "keywords", BuildBibTexKeywords(collection, passage));
        AppendBibTexField(sb, "note", BuildBibTexNote(collection, passage));
        AppendBibTexField(sb, "abstract", BuildBibTexAbstract(passage));
        sb.AppendLine("}");
    }

    private static void AppendBibTexField(StringBuilder sb, string name, string? value, bool required = false)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            if (!required)
                return;
            value = string.Empty;
        }

        sb.AppendLine($"  {name} = {{{EscapeBibTex(value)}}},");
    }

    private static string BuildBibTexKey(ScholarCollection collection, ScholarPassage passage, int index)
    {
        var fileId = SanitizeBibTexKeySegment(ExtractSourceTitle(passage.SourceRelPath));
        var range = passage.FromLb ?? passage.StartBlockNumber?.ToString() ?? passage.Id;
        return $"readzen:{SanitizeBibTexKeySegment(collection.Id)}:{fileId}:{SanitizeBibTexKeySegment(range)}:{index}";
    }

    private static string BuildBibTexTitle(ScholarPassage passage, int index)
    {
        var source = ExtractSourceTitle(passage.SourceRelPath);
        var range = FormatLineBreakRange(passage.FromLb, passage.ToLb);
        if (!string.IsNullOrWhiteSpace(range))
            return $"Passage from {source} {range}";
        var blocks = FormatBlockRange(passage.StartBlockNumber, passage.EndBlockNumber);
        if (!string.IsNullOrWhiteSpace(blocks))
            return $"Passage from {source} blocks {blocks}";
        return $"Passage from {source} #{index}";
    }

    private static string BuildBibTexKeywords(ScholarCollection collection, ScholarPassage passage)
    {
        var keywords = collection.Tags
            .Concat(passage.Tags)
            .Concat(passage.MasterNames)
            .Concat(BuildCategoryList(passage))
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Distinct(StringComparer.OrdinalIgnoreCase);
        return string.Join(", ", keywords);
    }

    private static string BuildBibTexNote(ScholarCollection collection, ScholarPassage passage)
    {
        var parts = new List<string>
        {
            $"Collection: {collection.Name}",
            $"Path: {passage.SourceRelPath}"
        };

        var lineBreaks = FormatLineBreakRange(passage.FromLb, passage.ToLb);
        if (!string.IsNullOrWhiteSpace(lineBreaks))
            parts.Add($"Line breaks: {lineBreaks}");

        var blocks = FormatBlockRange(passage.StartBlockNumber, passage.EndBlockNumber);
        if (!string.IsNullOrWhiteSpace(blocks))
            parts.Add($"Blocks: {blocks}");

        if (passage.MasterNames.Count > 0)
            parts.Add($"Masters: {string.Join(", ", passage.MasterNames)}");
        if (passage.Tags.Count > 0)
            parts.Add($"Tags: {string.Join(", ", passage.Tags)}");
        if (!string.IsNullOrWhiteSpace(passage.Notes))
            parts.Add($"Notes: {CollapseWhitespace(passage.Notes)}");

        return string.Join("; ", parts);
    }

    private static string BuildBibTexAbstract(ScholarPassage passage)
    {
        if (!string.IsNullOrWhiteSpace(passage.EnText))
            return CollapseWhitespace(passage.EnText);
        if (!string.IsNullOrWhiteSpace(passage.ZhText))
            return CollapseWhitespace(passage.ZhText);
        return string.Empty;
    }

    private static string EscapeBibTex(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("{", "\\{")
            .Replace("}", "\\}")
            .Replace("\r\n", " ")
            .Replace("\n", " ")
            .Replace("\r", " ");
    }

    private static string SanitizeBibTexKeySegment(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "unknown";

        var cleaned = new string(value.Where(c => char.IsLetterOrDigit(c) || c is ':' or '-' or '_' or '.').ToArray());
        return string.IsNullOrWhiteSpace(cleaned) ? "unknown" : cleaned;
    }

    private static string CollapseWhitespace(string value) =>
        string.Join(" ", value.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));
    private static string BuildDelimited(ScholarCollection collection, string delimiter)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join(delimiter, DelimitedHeaders.Select(h => EscapeDelimited(h, delimiter))));

        foreach (var passage in collection.Passages)
        {
            var row = new[]
            {
                collection.Id,
                collection.Name,
                collection.Description,
                string.Join(" | ", collection.Tags),
                collection.CreatedBy ?? string.Empty,
                FormatTimestamp(collection.CreatedUtc),
                FormatTimestamp(collection.ModifiedUtc),
                collection.StudyNotes,
                passage.Id,
                ExtractSourceTitle(passage.SourceRelPath),
                passage.SourceRelPath,
                passage.ZhText,
                passage.EnText,
                passage.Notes,
                string.Join(" | ", passage.Tags),
                string.Join(" | ", passage.MasterNames),
                passage.DoctrinalTopic ?? string.Empty,
                passage.LiteraryForm ?? string.Empty,
                passage.Lineage ?? string.Empty,
                passage.RhetoricalFunction ?? string.Empty,
                string.Join(" | ", passage.LinkedTexts),
                passage.FromLb ?? string.Empty,
                passage.ToLb ?? string.Empty,
                passage.StartBlockNumber?.ToString() ?? string.Empty,
                passage.EndBlockNumber?.ToString() ?? string.Empty,
                passage.CreatedBy ?? string.Empty,
                FormatTimestamp(passage.AddedUtc),
                FormatTimestamp(passage.ModifiedUtc),
                BuildZenLink(passage) ?? string.Empty,
                BuildShareUrl(passage) ?? string.Empty,
            };

            sb.AppendLine(string.Join(delimiter, row.Select(v => EscapeDelimited(v, delimiter))));
        }

        if (collection.Passages.Count == 0)
        {
            var emptyRow = new[]
            {
                collection.Id,
                collection.Name,
                collection.Description,
                string.Join(" | ", collection.Tags),
                collection.CreatedBy ?? string.Empty,
                FormatTimestamp(collection.CreatedUtc),
                FormatTimestamp(collection.ModifiedUtc),
                collection.StudyNotes
            }.Concat(Enumerable.Repeat(string.Empty, DelimitedHeaders.Length - 8));
            sb.AppendLine(string.Join(delimiter, emptyRow.Select(v => EscapeDelimited(v, delimiter))));
        }

        return sb.ToString();
    }

    private static string EscapeDelimited(string? value, string delimiter)
    {
        var text = value ?? string.Empty;
        var normalized = text.Replace("\r\n", "\n").Replace("\r", "\n");
        var needsQuotes = normalized.Contains('"') || normalized.Contains('\n') || normalized.Contains(delimiter, StringComparison.Ordinal);
        if (!needsQuotes)
            return normalized;
        return '"' + normalized.Replace("\"", "\"\"") + '"';
    }
    private static string ExtractSourceTitle(string relPath)
    {
        if (string.IsNullOrEmpty(relPath))
            return "(unknown)";

        var fileName = relPath;
        int lastSlash = relPath.LastIndexOfAny(new[] { '/', '\\' });
        if (lastSlash >= 0 && lastSlash < relPath.Length - 1)
            fileName = relPath[(lastSlash + 1)..];

        int dotIdx = fileName.LastIndexOf('.');
        if (dotIdx > 0)
            fileName = fileName[..dotIdx];

        return fileName;
    }

    private static string Esc(string s) => WebUtility.HtmlEncode(s);
}
