using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CbetaTranslator.App.Models;

namespace CbetaTranslator.App.Services;

public sealed class SearchExportService : ISearchExportService
{
    private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);

    public async Task ExportAsync(string filePath, SearchExportSnapshot snapshot, SearchExportFormat format, CancellationToken ct = default)
    {
        var content = format switch
        {
            SearchExportFormat.Html => BuildHtml(snapshot),
            SearchExportFormat.Markdown => BuildMarkdown(snapshot),
            SearchExportFormat.PlainText => BuildPlainText(snapshot),
            SearchExportFormat.Csv => BuildDelimited(snapshot, ","),
            SearchExportFormat.Tsv => BuildDelimited(snapshot, "\t"),
            SearchExportFormat.Json => BuildJson(snapshot),
            _ => throw new ArgumentOutOfRangeException(nameof(format)),
        };

        await File.WriteAllTextAsync(filePath, content, Utf8NoBom, ct);
    }

    private static string BuildHtml(SearchExportSnapshot snapshot)
    {
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html lang=\"en\">");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset=\"UTF-8\">");
        sb.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1.0\">");
        sb.AppendLine("<title>Search Results</title>");
        sb.AppendLine("<style>");
        sb.AppendLine("body { font-family: system-ui, sans-serif; margin: 2rem; color: #1f2937; background: #fff; }");
        sb.AppendLine("h1, h2 { margin: 0 0 0.75rem 0; }");
        sb.AppendLine(".meta { list-style: none; padding: 0; display: grid; gap: 0.25rem; margin: 0 0 1rem 0; }");
        sb.AppendLine(".group { border: 1px solid #d1d5db; border-radius: 10px; padding: 1rem; margin: 1rem 0; }");
        sb.AppendLine(".hit { padding: 0.5rem 0; border-top: 1px solid #e5e7eb; }");
        sb.AppendLine(".hit:first-of-type { border-top: none; }");
        sb.AppendLine(".row { white-space: pre-wrap; font-family: ui-monospace, SFMono-Regular, Consolas, monospace; }");
        sb.AppendLine(".muted { color: #6b7280; }");
        sb.AppendLine("</style>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("<h1>Search Results</h1>");
        AppendHtmlMetadata(sb, snapshot);

        foreach (var group in snapshot.Groups)
        {
            sb.AppendLine("<section class=\"group\">");
            sb.AppendLine($"<h2>{Esc(group.HeaderText)}</h2>");
            if (!string.IsNullOrWhiteSpace(group.Tooltip))
                sb.AppendLine($"<div class=\"muted\">{Esc(group.Tooltip)}</div>");

            foreach (var child in group.Children)
            {
                sb.AppendLine("<div class=\"hit\">");
                sb.AppendLine($"<div class=\"row\">{Esc(child.PrimaryDisplayText)}</div>");
                if (child.HasSecondaryDisplayText)
                    sb.AppendLine($"<div class=\"row muted\">{Esc(child.SecondaryDisplayText)}</div>");
                sb.AppendLine($"<div class=\"muted\">{Esc(child.RelPath)} · {Esc(child.RowText)}</div>");
                sb.AppendLine("</div>");
            }

            sb.AppendLine("</section>");
        }

        sb.AppendLine("</body>");
        sb.AppendLine("</html>");
        return sb.ToString();
    }

    private static string BuildMarkdown(SearchExportSnapshot snapshot)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# Search Results");
        sb.AppendLine();
        AppendMarkdownMetadata(sb, snapshot);

        foreach (var group in snapshot.Groups)
        {
            sb.AppendLine($"## {group.HeaderText}");
            if (!string.IsNullOrWhiteSpace(group.Tooltip))
                sb.AppendLine($"_{group.Tooltip}_");
            sb.AppendLine();

            foreach (var child in group.Children)
            {
                sb.AppendLine($"- {child.PrimaryDisplayText}");
                if (child.HasSecondaryDisplayText)
                    sb.AppendLine($"  - {child.SecondaryDisplayText}");
                sb.AppendLine($"  - {child.RelPath}");
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static string BuildPlainText(SearchExportSnapshot snapshot)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Search Results");
        sb.AppendLine("===============");
        sb.AppendLine();
        AppendPlainTextMetadata(sb, snapshot);

        foreach (var group in snapshot.Groups)
        {
            sb.AppendLine(group.HeaderText);
            if (!string.IsNullOrWhiteSpace(group.Tooltip))
                sb.AppendLine(group.Tooltip);

            foreach (var child in group.Children)
            {
                sb.AppendLine(child.PrimaryDisplayText);
                if (child.HasSecondaryDisplayText)
                    sb.AppendLine(child.SecondaryDisplayText);
                sb.AppendLine(child.RelPath);
            }

            sb.AppendLine();
        }

        return sb.ToString();
    }

    private static string BuildDelimited(SearchExportSnapshot snapshot, string delimiter)
    {
        var sb = new StringBuilder();
        var headers = new[]
        {
            "query",
            "search_original",
            "search_translated",
            "zen_only",
            "status_filter",
            "tag_filter",
            "context",
            "group_rel_path",
            "group_display_name",
            "group_status",
            "side",
            "match_index",
            "left",
            "match",
            "right",
            "secondary_left",
            "secondary_match",
            "secondary_right",
            "row_text",
            "bilingual_row_text"
        };

        sb.AppendLine(string.Join(delimiter, headers.Select(h => EscapeDelimited(h, delimiter))));

        foreach (var group in snapshot.Groups)
        {
            foreach (var child in group.Children)
            {
                var row = new[]
                {
                    snapshot.Query,
                    snapshot.SearchOriginal ? "true" : "false",
                    snapshot.SearchTranslated ? "true" : "false",
                    snapshot.ZenOnly ? "true" : "false",
                    snapshot.StatusFilter,
                    snapshot.TagFilter,
                    snapshot.ContextLabel,
                    group.RelPath,
                    group.DisplayName,
                    group.Status?.ToString() ?? "",
                    child.Side == SearchSide.Original ? "O" : "T",
                    child.Hit.Index.ToString(),
                    child.Hit.Left,
                    child.Hit.Match,
                    child.Hit.Right,
                    child.SecondaryLeftText,
                    child.SecondaryMatchText,
                    child.SecondaryRightText,
                    child.RowText,
                    child.BilingualRowText
                };

                sb.AppendLine(string.Join(delimiter, row.Select(value => EscapeDelimited(value, delimiter))));
            }
        }

        return sb.ToString();
    }

    private static string BuildJson(SearchExportSnapshot snapshot)
    {
        var payload = new Dictionary<string, object?>
        {
            ["format"] = "search-results/v1",
            ["exported_utc"] = snapshot.ExportedUtc.ToUniversalTime().ToString("O"),
            ["state"] = new Dictionary<string, object?>
            {
                ["query"] = snapshot.Query,
                ["search_original"] = snapshot.SearchOriginal,
                ["search_translated"] = snapshot.SearchTranslated,
                ["zen_only"] = snapshot.ZenOnly,
                ["status_filter"] = snapshot.StatusFilter,
                ["tag_filter"] = snapshot.TagFilter,
                ["context"] = snapshot.ContextLabel,
            },
            ["summary"] = new Dictionary<string, object?>
            {
                ["group_count"] = snapshot.Groups.Count,
                ["hit_count"] = snapshot.Groups.Sum(g => g.Children.Count),
            },
            ["groups"] = snapshot.Groups.Select(group => new Dictionary<string, object?>
            {
                ["rel_path"] = group.RelPath,
                ["display_name"] = group.DisplayName,
                ["tooltip"] = group.Tooltip,
                ["status"] = group.Status?.ToString(),
                ["hits_original"] = group.HitsOriginal,
                ["hits_translated"] = group.HitsTranslated,
                ["header_text"] = group.HeaderText,
                ["children"] = group.Children.Select(child => new Dictionary<string, object?>
                {
                    ["rel_path"] = child.RelPath,
                    ["side"] = child.Side.ToString(),
                    ["match_index"] = child.Hit.Index,
                    ["left"] = child.Hit.Left,
                    ["match"] = child.Hit.Match,
                    ["right"] = child.Hit.Right,
                    ["secondary_hit"] = child.SecondaryHit == null ? null : new Dictionary<string, object?>
                    {
                        ["left"] = child.SecondaryLeftText,
                        ["match"] = child.SecondaryMatchText,
                        ["right"] = child.SecondaryRightText
                    },
                    ["row_text"] = child.RowText,
                    ["bilingual_row_text"] = child.BilingualRowText
                }).ToList()
            }).ToList()
        };

        return JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
    }

    private static void AppendHtmlMetadata(StringBuilder sb, SearchExportSnapshot snapshot)
    {
        sb.AppendLine("<ul class=\"meta\">");
        AppendHtmlMetaItem(sb, "Query", snapshot.Query);
        AppendHtmlMetaItem(sb, "Original", snapshot.SearchOriginal ? "Yes" : "No");
        AppendHtmlMetaItem(sb, "Translated", snapshot.SearchTranslated ? "Yes" : "No");
        AppendHtmlMetaItem(sb, "Zen only", snapshot.ZenOnly ? "Yes" : "No");
        AppendHtmlMetaItem(sb, "Status", snapshot.StatusFilter);
        AppendHtmlMetaItem(sb, "Tag", snapshot.TagFilter);
        AppendHtmlMetaItem(sb, "Context", snapshot.ContextLabel);
        AppendHtmlMetaItem(sb, "Exported", snapshot.ExportedUtc.ToUniversalTime().ToString("u"));
        AppendHtmlMetaItem(sb, "Groups", snapshot.Groups.Count.ToString());
        AppendHtmlMetaItem(sb, "Hits", snapshot.Groups.Sum(g => g.Children.Count).ToString());
        sb.AppendLine("</ul>");
    }

    private static void AppendHtmlMetaItem(StringBuilder sb, string label, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return;

        sb.AppendLine($"<li><strong>{Esc(label)}:</strong> {Esc(value)}</li>");
    }

    private static void AppendMarkdownMetadata(StringBuilder sb, SearchExportSnapshot snapshot)
    {
        AppendMarkdownMetaItem(sb, "Query", snapshot.Query);
        AppendMarkdownMetaItem(sb, "Original", snapshot.SearchOriginal ? "Yes" : "No");
        AppendMarkdownMetaItem(sb, "Translated", snapshot.SearchTranslated ? "Yes" : "No");
        AppendMarkdownMetaItem(sb, "Zen only", snapshot.ZenOnly ? "Yes" : "No");
        AppendMarkdownMetaItem(sb, "Status", snapshot.StatusFilter);
        AppendMarkdownMetaItem(sb, "Tag", snapshot.TagFilter);
        AppendMarkdownMetaItem(sb, "Context", snapshot.ContextLabel);
        AppendMarkdownMetaItem(sb, "Exported", snapshot.ExportedUtc.ToUniversalTime().ToString("u"));
        AppendMarkdownMetaItem(sb, "Groups", snapshot.Groups.Count.ToString());
        AppendMarkdownMetaItem(sb, "Hits", snapshot.Groups.Sum(g => g.Children.Count).ToString());
        sb.AppendLine();
    }

    private static void AppendMarkdownMetaItem(StringBuilder sb, string label, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            sb.AppendLine($"- **{label}:** {value}");
    }

    private static void AppendPlainTextMetadata(StringBuilder sb, SearchExportSnapshot snapshot)
    {
        AppendPlainTextMetaItem(sb, "Query", snapshot.Query);
        AppendPlainTextMetaItem(sb, "Original", snapshot.SearchOriginal ? "Yes" : "No");
        AppendPlainTextMetaItem(sb, "Translated", snapshot.SearchTranslated ? "Yes" : "No");
        AppendPlainTextMetaItem(sb, "Zen only", snapshot.ZenOnly ? "Yes" : "No");
        AppendPlainTextMetaItem(sb, "Status", snapshot.StatusFilter);
        AppendPlainTextMetaItem(sb, "Tag", snapshot.TagFilter);
        AppendPlainTextMetaItem(sb, "Context", snapshot.ContextLabel);
        AppendPlainTextMetaItem(sb, "Exported", snapshot.ExportedUtc.ToUniversalTime().ToString("u"));
        AppendPlainTextMetaItem(sb, "Groups", snapshot.Groups.Count.ToString());
        AppendPlainTextMetaItem(sb, "Hits", snapshot.Groups.Sum(g => g.Children.Count).ToString());
        sb.AppendLine();
    }

    private static void AppendPlainTextMetaItem(StringBuilder sb, string label, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            sb.AppendLine($"{label}: {value}");
    }

    private static string EscapeDelimited(string? value, string delimiter)
    {
        value ??= string.Empty;
        var needsQuotes = value.Contains(delimiter, StringComparison.Ordinal)
                          || value.Contains('"')
                          || value.Contains('\r')
                          || value.Contains('\n');

        var escaped = value.Replace("\"", "\"\"");
        return needsQuotes ? $"\"{escaped}\"" : escaped;
    }

    private static string Esc(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
}