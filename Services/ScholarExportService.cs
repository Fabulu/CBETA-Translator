using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ReadZen.App.Infrastructure;
using ReadZen.App.Models;

namespace ReadZen.App.Services;

public sealed class ScholarExportService : IScholarExportService
{
    public async Task ExportAsync(string filePath, ScholarCollection collection, ScholarExportFormat format, CitationStyle citationStyle = CitationStyle.Chicago, CancellationToken ct = default)
    {
        if (format == ScholarExportFormat.ReaderTagTsv)
        {
            var exportData = BuildReaderTagExportData(collection);
            var tsv = BuildReaderTagTsv(exportData);
            await File.WriteAllTextAsync(filePath, tsv, Encoding.UTF8, ct);

            var vocabularyPath = BuildReaderTagVocabularySidecarPath(filePath);
            var vocabularyJson = BuildReaderTagVocabularyJson(collection, exportData);
            await File.WriteAllTextAsync(vocabularyPath, vocabularyJson, Encoding.UTF8, ct);
            return;
        }

        var content = format switch
        {
            ScholarExportFormat.Html => BuildHtml(collection, citationStyle),
            ScholarExportFormat.Markdown => BuildMarkdown(collection, citationStyle),
            ScholarExportFormat.PlainText => BuildPlainText(collection),
            ScholarExportFormat.Csv => BuildDelimited(collection, ",", citationStyle),
            ScholarExportFormat.Tsv => BuildDelimited(collection, "	", citationStyle),
            ScholarExportFormat.ReaderTagBundle => BuildReaderTagBundle(collection),
            ScholarExportFormat.BibTex => BuildBibTex(collection),
            ScholarExportFormat.CslJson => BuildCslJson(collection),
            ScholarExportFormat.PaperDraft => BuildPaperDraft(collection, citationStyle),
            ScholarExportFormat.Ris => BuildRis(collection),
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
        "share_url",
        "formatted_citation",
        "summary",
        "reading_status",
        "importance",
        "annotation_type",
    };
    private static readonly string[] ReaderTagHeaders =
    {
        "rel_path",
        "from_lb",
        "to_lb",
        "tag_id",
        "tag_name",
        "created_by",
        "created_utc",
        "modified_utc",
        "source_collection_id",
        "source_collection_name",
        "source_passage_id",
        "zh_preview",
        "en_preview",
        "zen_link",
        "share_url"
    };

    private sealed record ReaderTagVocabularyEntry(
        string Id,
        string Name,
        string? ParentId,
        string Color,
        string? Description,
        int SortOrder,
        string CreatedUtc,
        bool Synthesized);

    private sealed record ReaderTagDocumentRecord(
        string Id,
        string RelPath,
        string FromLb,
        string ToLb,
        string TagId,
        string TagName,
        string? CreatedBy,
        string CreatedUtc,
        string? ModifiedUtc,
        string SourceCollectionId,
        string SourceCollectionName,
        string SourcePassageId,
        string? ZhPreview,
        string? EnPreview,
        string? ZenLink,
        string? ShareUrl,
        bool SynthesizedTagId);

    private sealed record ReaderTagSkippedItem(
        string SourcePassageId,
        string? TagName,
        string Reason,
        string? SourceRelPath,
        string? FromLb,
        string? ToLb);

    private sealed record ReaderTagExportData(
        List<ReaderTagVocabularyEntry> VocabularyTags,
        List<ReaderTagDocumentRecord> DocumentTags,
        List<ReaderTagSkippedItem> SkippedItems);
    private static string BuildReaderTagBundle(ScholarCollection collection)
    {
        var exportData = BuildReaderTagExportData(collection);
        var payload = new Dictionary<string, object?>
        {
            ["format"] = "readzen-reader-tags-bundle/v1",
            ["exported_utc"] = FormatIsoTimestamp(DateTimeOffset.UtcNow),
            ["source"] = new Dictionary<string, object?>
            {
                ["kind"] = "scholar-collection",
                ["collection_id"] = collection.Id,
                ["collection_name"] = collection.Name,
            },
            ["summary"] = new Dictionary<string, object?>
            {
                ["document_tag_count"] = exportData.DocumentTags.Count,
                ["vocabulary_tag_count"] = exportData.VocabularyTags.Count,
                ["skipped_item_count"] = exportData.SkippedItems.Count,
            },
            ["vocabulary"] = new Dictionary<string, object?>
            {
                ["tags"] = exportData.VocabularyTags.Select(tag => new Dictionary<string, object?>
                {
                    ["id"] = tag.Id,
                    ["name"] = tag.Name,
                    ["parent_id"] = tag.ParentId,
                    ["color"] = tag.Color,
                    ["description"] = tag.Description,
                    ["sort_order"] = tag.SortOrder,
                    ["created_utc"] = tag.CreatedUtc,
                    ["synthesized"] = tag.Synthesized,
                }).ToList(),
                ["pages"] = new Dictionary<string, object?>(),
            },
            ["document_tags"] = exportData.DocumentTags.Select(tag => new Dictionary<string, object?>
            {
                ["id"] = tag.Id,
                ["rel_path"] = tag.RelPath,
                ["from_lb"] = tag.FromLb,
                ["to_lb"] = tag.ToLb,
                ["tag_id"] = tag.TagId,
                ["tag_name"] = tag.TagName,
                ["created_by"] = tag.CreatedBy,
                ["created_utc"] = tag.CreatedUtc,
                ["modified_utc"] = tag.ModifiedUtc,
                ["source_collection_id"] = tag.SourceCollectionId,
                ["source_collection_name"] = tag.SourceCollectionName,
                ["source_passage_id"] = tag.SourcePassageId,
                ["zh_preview"] = tag.ZhPreview,
                ["en_preview"] = tag.EnPreview,
                ["zen_link"] = tag.ZenLink,
                ["share_url"] = tag.ShareUrl,
                ["synthesized_tag_id"] = tag.SynthesizedTagId,
            }).ToList(),
            ["skipped_items"] = exportData.SkippedItems.Select(item => new Dictionary<string, object?>
            {
                ["source_passage_id"] = item.SourcePassageId,
                ["tag_name"] = item.TagName,
                ["reason"] = item.Reason,
                ["source_rel_path"] = item.SourceRelPath,
                ["from_lb"] = item.FromLb,
                ["to_lb"] = item.ToLb,
            }).ToList(),
        };

        return JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
    }

    private static ReaderTagExportData BuildReaderTagExportData(ScholarCollection collection)
    {
        var vocabulary = new Dictionary<string, ReaderTagVocabularyEntry>(StringComparer.OrdinalIgnoreCase);
        var documentTags = new List<ReaderTagDocumentRecord>();
        var skippedItems = new List<ReaderTagSkippedItem>();
        var sortOrder = 0;

        foreach (var passage in collection.Passages)
        {
            if (passage.Tags == null || passage.Tags.Count == 0)
            {
                skippedItems.Add(new ReaderTagSkippedItem(
                    passage.Id,
                    null,
                    "no_tags",
                    NullIfWhiteSpace(passage.SourceRelPath),
                    passage.FromLb,
                    passage.ToLb));
                continue;
            }

            if (string.IsNullOrWhiteSpace(passage.SourceRelPath))
            {
                foreach (var tagName in passage.Tags.Where(static t => !string.IsNullOrWhiteSpace(t)).DefaultIfEmpty(string.Empty))
                {
                    skippedItems.Add(new ReaderTagSkippedItem(
                        passage.Id,
                        NullIfWhiteSpace(tagName),
                        "missing_source_rel_path",
                        null,
                        passage.FromLb,
                        passage.ToLb));
                }
                continue;
            }

            if (string.IsNullOrWhiteSpace(passage.FromLb))
            {
                foreach (var tagName in passage.Tags.Where(static t => !string.IsNullOrWhiteSpace(t)).DefaultIfEmpty(string.Empty))
                {
                    skippedItems.Add(new ReaderTagSkippedItem(
                        passage.Id,
                        NullIfWhiteSpace(tagName),
                        "missing_from_lb",
                        passage.SourceRelPath,
                        passage.FromLb,
                        passage.ToLb));
                }
                continue;
            }

            var toLb = string.IsNullOrWhiteSpace(passage.ToLb) ? passage.FromLb! : passage.ToLb!;
            foreach (var rawTagName in passage.Tags.Distinct(StringComparer.OrdinalIgnoreCase))
            {
                var tagName = rawTagName?.Trim();
                if (string.IsNullOrWhiteSpace(tagName))
                {
                    skippedItems.Add(new ReaderTagSkippedItem(
                        passage.Id,
                        null,
                        "blank_tag_name",
                        passage.SourceRelPath,
                        passage.FromLb,
                        toLb));
                    continue;
                }

                var tagId = BuildUniqueReaderTagId(tagName, vocabulary);
                if (!vocabulary.ContainsKey(tagId))
                {
                    sortOrder++;
                    vocabulary[tagId] = new ReaderTagVocabularyEntry(
                        tagId,
                        tagName,
                        null,
                        "#3498DB",
                        "Synthesized from Scholar passage tags for Reader interchange.",
                        sortOrder,
                        FormatIsoTimestamp(ResolveReaderTagCreatedUtc(collection, passage)),
                        true);
                }

                documentTags.Add(new ReaderTagDocumentRecord(
                    $"readzen-scholar-{SanitizeBibTexKeySegment(collection.Id)}-{SanitizeBibTexKeySegment(passage.Id)}-{SanitizeBibTexKeySegment(tagId)}",
                    passage.SourceRelPath,
                    passage.FromLb!,
                    toLb,
                    tagId,
                    tagName,
                    passage.CreatedBy ?? collection.CreatedBy,
                    FormatIsoTimestamp(ResolveReaderTagCreatedUtc(collection, passage)),
                    FormatIsoTimestamp(passage.ModifiedUtc),
                    collection.Id,
                    collection.Name,
                    passage.Id,
                    BuildPreview(passage.ZhText),
                    BuildPreview(passage.EnText),
                    BuildZenLink(passage),
                    BuildShareUrl(passage),
                    true));
            }
        }

        return new ReaderTagExportData(
            vocabulary.Values.OrderBy(v => v.SortOrder).ToList(),
            documentTags,
            skippedItems);
    }

    private static string BuildReaderTagTsv(ReaderTagExportData exportData)
    {
        var sb = new StringBuilder();
        sb.AppendLine(string.Join("\t", ReaderTagHeaders.Select(header => EscapeDelimited(header, "\t"))));

        foreach (var tag in exportData.DocumentTags)
        {
            var row = new[]
            {
                tag.RelPath,
                tag.FromLb,
                tag.ToLb,
                tag.TagId,
                tag.TagName,
                tag.CreatedBy ?? string.Empty,
                tag.CreatedUtc,
                tag.ModifiedUtc ?? string.Empty,
                tag.SourceCollectionId,
                tag.SourceCollectionName,
                tag.SourcePassageId,
                tag.ZhPreview ?? string.Empty,
                tag.EnPreview ?? string.Empty,
                tag.ZenLink ?? string.Empty,
                tag.ShareUrl ?? string.Empty,
            };

            sb.AppendLine(string.Join("\t", row.Select(value => EscapeDelimited(value, "\t"))));
        }

        return sb.ToString();
    }

    private static string BuildReaderTagVocabularyJson(ScholarCollection collection, ReaderTagExportData exportData)
    {
        var payload = new Dictionary<string, object?>
        {
            ["format"] = "readzen-reader-tag-vocabulary/v1",
            ["exported_utc"] = FormatIsoTimestamp(DateTimeOffset.UtcNow),
            ["source"] = new Dictionary<string, object?>
            {
                ["kind"] = "scholar-collection",
                ["collection_id"] = collection.Id,
                ["collection_name"] = collection.Name,
            },
            ["tags"] = exportData.VocabularyTags.Select(tag => new Dictionary<string, object?>
            {
                ["id"] = tag.Id,
                ["name"] = tag.Name,
                ["parent_id"] = tag.ParentId,
                ["color"] = tag.Color,
                ["description"] = tag.Description,
                ["sort_order"] = tag.SortOrder,
                ["created_utc"] = tag.CreatedUtc,
                ["synthesized"] = tag.Synthesized,
            }).ToList(),
            ["pages"] = new Dictionary<string, object?>(),
        };

        return JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
    }

    private static string BuildReaderTagVocabularySidecarPath(string filePath)
    {
        var directory = Path.GetDirectoryName(filePath) ?? string.Empty;
        var fileName = Path.GetFileNameWithoutExtension(filePath);
        return Path.Combine(directory, fileName + ".vocabulary.json");
    }

    private static DateTimeOffset ResolveReaderTagCreatedUtc(ScholarCollection collection, ScholarPassage passage)
    {
        if (passage.AddedUtc != default)
            return passage.AddedUtc;
        if (collection.CreatedUtc != default)
            return collection.CreatedUtc;
        return DateTimeOffset.UtcNow;
    }

    private static string BuildUniqueReaderTagId(string tagName, Dictionary<string, ReaderTagVocabularyEntry> vocabulary)
    {
        var baseId = BuildReaderTagId(tagName);
        if (!vocabulary.TryGetValue(baseId, out var existing) || string.Equals(existing.Name, tagName, StringComparison.OrdinalIgnoreCase))
            return baseId;

        var suffix = 2;
        while (true)
        {
            var candidate = $"{baseId}-{suffix}";
            if (!vocabulary.TryGetValue(candidate, out existing) || string.Equals(existing.Name, tagName, StringComparison.OrdinalIgnoreCase))
                return candidate;
            suffix++;
        }
    }
    private static string BuildReaderTagId(string tagName)
    {
        var normalized = tagName.Trim().ToLowerInvariant();
        var sb = new StringBuilder(normalized.Length);
        var previousWasSeparator = false;

        foreach (var c in normalized)
        {
            if (char.IsLetterOrDigit(c))
            {
                sb.Append(c);
                previousWasSeparator = false;
            }
            else if (!previousWasSeparator)
            {
                sb.Append('-');
                previousWasSeparator = true;
            }
        }

        var result = sb.ToString().Trim('-');
        return string.IsNullOrWhiteSpace(result) ? "scholar-tag" : result;
    }

    private static string? NullIfWhiteSpace(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;

    private static string? BuildPreview(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return null;

        var collapsed = CollapseWhitespace(value);
        return collapsed.Length <= 120 ? collapsed : collapsed[..120] + "...";
    }

    private static string BuildHtml(ScholarCollection collection, CitationStyle citationStyle = CitationStyle.Chicago)
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

            // Citation line (3C)
            var citationLine = BuildPassageCitationLine(p);
            if (!string.IsNullOrWhiteSpace(citationLine))
                sb.AppendLine($"<div class=\"citation-box\"><span class=\"citation-label\">Cite:</span> {Esc(citationLine)}</div>");

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

        // Works Cited section (3C)
        var worksCitedHtml = BuildWorksCitedHtml(collection);
        if (!string.IsNullOrWhiteSpace(worksCitedHtml))
        {
            sb.AppendLine("<hr>");
            sb.AppendLine("<h2>Works Cited</h2>");
            sb.AppendLine(worksCitedHtml);
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
.citation-box {
    font-size: 0.82em;
    color: #9BA3C7;
    background: #1E1E2E;
    border: 1px solid #3A3A4A;
    border-radius: 4px;
    padding: 6px 10px;
    margin-top: 8px;
}
.citation-label { font-weight: 600; color: #C6D0F5; }
";

    private static string BuildMarkdown(ScholarCollection collection, CitationStyle citationStyle = CitationStyle.Chicago)
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

            // Citation line (3C)
            var mdCitationLine = BuildPassageCitationLine(p);
            if (!string.IsNullOrWhiteSpace(mdCitationLine))
                sb.AppendLine($"**Cite:** {mdCitationLine}");

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

        // Works Cited section (3C)
        var worksCitedMd = BuildWorksCitedMarkdown(collection);
        if (!string.IsNullOrWhiteSpace(worksCitedMd))
        {
            sb.AppendLine("## Works Cited");
            sb.AppendLine();
            sb.AppendLine(worksCitedMd);
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

        AddMetadata(items, "Summary", passage.Summary);
        AddMetadata(items, "Reading Status", passage.ReadingStatus);
        if (passage.Importance.HasValue && passage.Importance > 0)
            AddMetadata(items, "Importance", $"{passage.Importance}/5");
        AddMetadata(items, "Annotation Type", passage.AnnotationType);

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

    private static string FormatIsoTimestamp(DateTimeOffset value) =>
        value == default ? string.Empty : value.ToUniversalTime().ToString("O");

    private static string? FormatIsoTimestamp(DateTimeOffset? value) =>
        value.HasValue ? FormatIsoTimestamp(value.Value) : null;
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
        return ZenUriParser.BuildUri(
            passage.SourceRelPath,
            fromLb: passage.FromLb,
            toLb: passage.ToLb,
            blockNumber: block);
    }

    private static string? BuildShareUrl(ScholarPassage passage)
    {
        if (string.IsNullOrWhiteSpace(passage.SourceRelPath))
            return null;

        return ZenUriParser.BuildShareableUrl(
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
        var fileId = ExtractSourceTitle(passage.SourceRelPath);
        TryParseCbetaFromFileId(fileId, out var canon, out var volume, out _);
        bool hasCbeta = !string.IsNullOrWhiteSpace(canon) && volume.HasValue;

        var entryType = hasCbeta ? "incollection" : "misc";
        sb.AppendLine($"@{entryType}{{{key},");
        AppendBibTexField(sb, "title", BuildBibTexTitle(passage, index), true);

        if (passage.MasterNames.Count > 0)
            AppendBibTexField(sb, "author", passage.MasterNames[0]);

        if (hasCbeta)
        {
            AppendBibTexField(sb, "booktitle", "Taish\\={o} shinsh\\={u} daiz\\={o}ky\\={o}");
            sb.AppendLine($"  editor = {{Takakusu Junjir\\=o and Watanabe Kaikyoku}},");
            sb.AppendLine($"  volume = {{{volume!.Value}}},");
        }

        AppendBibTexField(sb, "howpublished", BuildZenLink(passage));
        AppendBibTexField(sb, "url", BuildShareUrl(passage));
        AppendBibTexField(sb, "keywords", BuildBibTexKeywords(collection, passage));
        AppendBibTexField(sb, "note", BuildBibTexNote(collection, passage));
        AppendBibTexField(sb, "abstract", BuildBibTexAbstract(passage));
        sb.AppendLine($"  publisher = {{CBETA}},");
        sb.AppendLine("}");
    }

    /// <summary>
    /// Delegates to <see cref="CbetaReferenceHelper.TryParseCbetaFromFileId"/>.
    /// </summary>
    private static void TryParseCbetaFromFileId(string fileId, out string? canon, out int? volume, out string? number)
        => CbetaReferenceHelper.TryParseCbetaFromFileId(fileId, out canon, out volume, out number);

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

    /// <summary>
    /// Delegates to <see cref="CbetaReferenceHelper.EscapeBibTeX"/>.
    /// </summary>
    private static string EscapeBibTex(string value)
        => CbetaReferenceHelper.EscapeBibTeX(value);

    private static string SanitizeBibTexKeySegment(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "unknown";

        var cleaned = new string(value.Where(c => char.IsLetterOrDigit(c) || c is ':' or '-' or '_' or '.').ToArray());
        return string.IsNullOrWhiteSpace(cleaned) ? "unknown" : cleaned;
    }

    private static string CollapseWhitespace(string value) =>
        string.Join(" ", value.Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));

    private static string BuildRis(ScholarCollection collection)
    {
        var sb = new StringBuilder();
        for (int i = 0; i < collection.Passages.Count; i++)
        {
            if (i > 0) sb.Append("\r\n");
            AppendRisEntry(sb, collection, collection.Passages[i]);
        }
        return sb.ToString();
    }

    private static void AppendRisEntry(StringBuilder sb, ScholarCollection collection, ScholarPassage passage)
    {
        sb.Append("TY  - BOOK\r\n");

        // Title
        var title = ExtractSourceTitle(passage.SourceRelPath);
        sb.Append("TI  - ").Append(title).Append("\r\n");

        // Author from master names
        if (passage.MasterNames.Count > 0)
            sb.Append("AU  - ").Append(passage.MasterNames[0]).Append("\r\n");

        // Volume / pages from lb values
        if (!string.IsNullOrWhiteSpace(passage.FromLb))
            sb.Append("SP  - ").Append(passage.FromLb).Append("\r\n");
        if (!string.IsNullOrWhiteSpace(passage.ToLb))
            sb.Append("EP  - ").Append(passage.ToLb).Append("\r\n");

        sb.Append("PB  - CBETA / Read Zen\r\n");

        var shareUrl = BuildShareUrl(passage);
        if (!string.IsNullOrWhiteSpace(shareUrl))
            sb.Append("UR  - ").Append(shareUrl).Append("\r\n");

        sb.Append("DB  - CBETA\r\n");

        // Keywords from tags
        var keywords = collection.Tags
            .Concat(passage.Tags)
            .Concat(passage.MasterNames)
            .Where(v => !string.IsNullOrWhiteSpace(v))
            .Distinct(StringComparer.OrdinalIgnoreCase);
        foreach (var kw in keywords)
            sb.Append("KW  - ").Append(kw).Append("\r\n");

        // CBETA ref in M1 and N1
        var fileId = ExtractSourceTitle(passage.SourceRelPath);
        sb.Append("M1  - ").Append(fileId).Append("\r\n");
        sb.Append("N1  - CBETA ").Append(fileId).Append("\r\n");

        sb.Append("ER  - \r\n");
    }

    private static string BuildCslJson(ScholarCollection collection)
    {
        var items = new List<Dictionary<string, object?>>();

        for (int i = 0; i < collection.Passages.Count; i++)
        {
            var passage = collection.Passages[i];
            var item = new Dictionary<string, object?>
            {
                ["id"] = BuildBibTexKey(collection, passage, i + 1),
                ["type"] = "manuscript",
                ["title"] = BuildBibTexTitle(passage, i + 1),
                ["URL"] = BuildShareUrl(passage),
                ["abstract"] = string.IsNullOrWhiteSpace(BuildBibTexAbstract(passage)) ? null : BuildBibTexAbstract(passage),
                ["keyword"] = BuildBibTexKeywords(collection, passage),
                ["note"] = BuildBibTexNote(collection, passage),
                ["container-title"] = collection.Name,
                ["collection-title"] = collection.Name,
                ["source"] = ExtractSourceTitle(passage.SourceRelPath),
                ["readzen:collectionId"] = collection.Id,
                ["readzen:passageId"] = passage.Id,
                ["readzen:sourceRelPath"] = passage.SourceRelPath,
                ["readzen:fromLb"] = passage.FromLb,
                ["readzen:toLb"] = passage.ToLb,
                ["readzen:startBlock"] = passage.StartBlockNumber,
                ["readzen:endBlock"] = passage.EndBlockNumber,
                ["readzen:zenUrl"] = BuildZenLink(passage),
                ["readzen:createdBy"] = passage.CreatedBy,
                ["readzen:addedUtc"] = FormatIsoTimestamp(passage.AddedUtc),
                ["readzen:modifiedUtc"] = FormatIsoTimestamp(passage.ModifiedUtc),
                ["readzen:collectionCreatedBy"] = collection.CreatedBy,
                ["readzen:collectionCreatedUtc"] = FormatIsoTimestamp(collection.CreatedUtc),
                ["readzen:collectionModifiedUtc"] = FormatIsoTimestamp(collection.ModifiedUtc),
                ["readzen:tags"] = passage.Tags.Count > 0 ? passage.Tags : null,
                ["readzen:masterNames"] = passage.MasterNames.Count > 0 ? passage.MasterNames : null,
                ["readzen:linkedTexts"] = passage.LinkedTexts.Count > 0 ? passage.LinkedTexts : null,
                ["readzen:doctrinalTopic"] = passage.DoctrinalTopic,
                ["readzen:literaryForm"] = passage.LiteraryForm,
                ["readzen:lineage"] = passage.Lineage,
                ["readzen:rhetoricalFunction"] = passage.RhetoricalFunction,
                ["readzen:zhText"] = string.IsNullOrWhiteSpace(passage.ZhText) ? null : passage.ZhText,
                ["readzen:enText"] = string.IsNullOrWhiteSpace(passage.EnText) ? null : passage.EnText,
            };

            if (passage.AddedUtc != default)
            {
                item["issued"] = new Dictionary<string, object?>
                {
                    ["raw"] = FormatIsoTimestamp(passage.AddedUtc)
                };
            }

            items.Add(item.Where(kvp => kvp.Value != null && (!(kvp.Value is string s) || !string.IsNullOrWhiteSpace(s))).ToDictionary(kvp => kvp.Key, kvp => kvp.Value));
        }

        var options = new JsonSerializerOptions
        {
            WriteIndented = true
        };
        return JsonSerializer.Serialize(items, options);
    }
    private static string BuildPaperDraft(ScholarCollection collection, CitationStyle citationStyle = CitationStyle.Chicago)
    {
        var sb = new StringBuilder();
        var footnotes = new List<PaperFootnote>();

        // --- YAML frontmatter ---
        sb.AppendLine("---");
        sb.AppendLine($"title: \"{EscapeYaml(collection.Name)}\"");
        if (!string.IsNullOrWhiteSpace(collection.CreatedBy))
            sb.AppendLine($"author: \"{EscapeYaml(collection.CreatedBy)}\"");
        sb.AppendLine($"date: \"{FormatIsoTimestamp(DateTimeOffset.UtcNow)}\"");
        sb.AppendLine("export_format: \"paper-draft\"");
        sb.AppendLine("---");
        sb.AppendLine();

        // --- Abstract ---
        sb.AppendLine("# Abstract");
        sb.AppendLine();
        sb.AppendLine("[TODO: Write abstract summarizing argument, sources, and conclusions.]");
        sb.AppendLine();

        // --- Introduction ---
        sb.AppendLine("# Introduction");
        sb.AppendLine();
        if (!string.IsNullOrWhiteSpace(collection.Description))
        {
            sb.AppendLine(collection.Description);
            sb.AppendLine();
        }

        var uniqueTexts = collection.Passages
            .Select(p => CbetaReferenceHelper.ExtractFileIdFromRelPath(p.SourceRelPath))
            .Where(id => !string.IsNullOrEmpty(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        sb.AppendLine($"This study draws on {collection.Passages.Count} passage(s) from {uniqueTexts.Count} source text(s) in the Chinese Buddhist canon.");
        sb.AppendLine();
        sb.AppendLine("[TODO: Describe methodology and analytical framework.]");
        sb.AppendLine();

        // --- Body sections (grouped) ---
        var groups = collection.Passages
            .Select((passage, index) => new DraftPassageGroup(Group: GetDraftGroupLabel(passage), Passage: passage, OriginalIndex: index))
            .GroupBy(x => x.Group)
            .OrderBy(g => string.Equals(g.Key, "Untagged Passages", StringComparison.Ordinal) ? 1 : 0)
            .ThenBy(g => g.Key, StringComparer.OrdinalIgnoreCase);

        foreach (var group in groups)
        {
            sb.AppendLine($"# {group.Key}");
            sb.AppendLine();

            foreach (var entry in group.OrderBy(x => x.Passage.SourceRelPath, StringComparer.OrdinalIgnoreCase)
                                       .ThenBy(x => x.Passage.FromLb ?? string.Empty, StringComparer.OrdinalIgnoreCase)
                                       .ThenBy(x => x.Passage.StartBlockNumber ?? int.MaxValue)
                                       .ThenBy(x => x.OriginalIndex))
            {
                var footnote = AppendPaperDraftPassage(sb, entry.Passage, footnotes.Count + 1);
                footnotes.Add(footnote);
            }
        }

        // --- Conclusion ---
        sb.AppendLine("# Conclusion");
        sb.AppendLine();
        sb.AppendLine("[TODO: Summarize findings, restate thesis, suggest further research.]");
        sb.AppendLine();

        // --- Abbreviations ---
        var canonsUsed = collection.Passages
            .Select(p => ExtractCanonCode(p.SourceRelPath))
            .Where(c => !string.IsNullOrEmpty(c))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(c => c, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (canonsUsed.Count > 0)
        {
            sb.AppendLine("# Abbreviations");
            sb.AppendLine();
            foreach (var canon in canonsUsed)
            {
                var fullName = GetCanonFullName(canon);
                sb.AppendLine($"- **{canon}** = {fullName}");
            }
            sb.AppendLine();
        }

        // --- Primary Sources (bibliography) ---
        var primarySources = collection.Passages
            .Select(p => new { FileId = CbetaReferenceHelper.ExtractFileIdFromRelPath(p.SourceRelPath), RelPath = p.SourceRelPath })
            .Where(x => !string.IsNullOrEmpty(x.FileId))
            .GroupBy(x => x.FileId, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.First())
            .OrderBy(x => x.FileId, StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (primarySources.Count > 0)
        {
            sb.AppendLine("# Primary Sources");
            sb.AppendLine();
            foreach (var src in primarySources)
            {
                var parsed = ParseFileIdComponents(src.FileId);
                sb.AppendLine($"- *{src.FileId}*. {parsed.Canon} no. {parsed.Number}, {parsed.Volume}. CBETA. https://readzen.pages.dev/{src.FileId}");
            }
            sb.AppendLine();
        }

        // --- Notes (footnote definitions) ---
        if (footnotes.Count > 0)
        {
            sb.AppendLine("# Notes");
            sb.AppendLine();
            foreach (var fn in footnotes)
            {
                sb.AppendLine($"[^{fn.Index}]: {fn.Reference}");
            }
            sb.AppendLine();
        }

        return sb.ToString();
    }

    /// <summary>
    /// Appends a single passage as a blockquote with footnote marker. Returns footnote data.
    /// </summary>
    private static PaperFootnote AppendPaperDraftPassage(StringBuilder sb, ScholarPassage passage, int footnoteIndex)
    {
        if (!string.IsNullOrWhiteSpace(passage.ZhText))
        {
            foreach (var line in passage.ZhText.Split('\n'))
                sb.AppendLine($"> {line.TrimEnd('\r')}");
        }

        if (!string.IsNullOrWhiteSpace(passage.EnText))
        {
            if (!string.IsNullOrWhiteSpace(passage.ZhText))
                sb.AppendLine(">");
            foreach (var line in passage.EnText.Split('\n'))
                sb.AppendLine($"> {line.TrimEnd('\r')}");
        }

        sb.AppendLine();
        sb.AppendLine($"[^{footnoteIndex}]");
        sb.AppendLine();
        sb.AppendLine("[TODO: Analysis of this passage.]");
        sb.AppendLine();

        // Build footnote reference
        var fileId = CbetaReferenceHelper.ExtractFileIdFromRelPath(passage.SourceRelPath);
        var reference = BuildCbetaFootnoteReference(fileId, passage.FromLb);

        return new PaperFootnote(footnoteIndex, reference);
    }

    /// <summary>
    /// Builds a CBETA-style footnote reference string.
    /// </summary>
    private static string BuildCbetaFootnoteReference(string fileId, string? fromLb)
    {
        if (string.IsNullOrEmpty(fileId))
            return "(source unavailable)";

        var parsed = ParseFileIdComponents(fileId);
        var formattedLb = FormatLbValue(fromLb);
        var lbSuffix = !string.IsNullOrEmpty(formattedLb) ? $": {formattedLb}" : "";
        var urlAnchor = !string.IsNullOrEmpty(fromLb) ? $"/{fromLb}" : "";

        return $"{fileId}, {parsed.Canon} no. {parsed.Number}, {parsed.Volume}{lbSuffix}. CBETA. https://readzen.pages.dev/{fileId}{urlAnchor}";
    }

    /// <summary>
    /// Parses a FileId like "T48n2005" into canon code, volume, and number.
    /// </summary>
    private static (string Canon, string Volume, string Number) ParseFileIdComponents(string fileId)
    {
        if (string.IsNullOrEmpty(fileId))
            return ("?", "?", "?");

        int i = 0;
        while (i < fileId.Length && char.IsLetter(fileId[i])) i++;
        var canon = fileId[..i];

        int volStart = i;
        while (i < fileId.Length && char.IsDigit(fileId[i])) i++;
        var volume = i > volStart ? fileId[volStart..i] : "?";

        // skip 'n'
        if (i < fileId.Length && fileId[i] == 'n') i++;

        var number = i < fileId.Length ? fileId[i..] : "?";

        return (canon, volume, number);
    }

    /// <summary>
    /// Formats an lb value by stripping leading zeros from the page number.
    /// E.g. "0292c18" becomes "292c18".
    /// </summary>
    private static string FormatLbValue(string? lb)
    {
        if (string.IsNullOrWhiteSpace(lb))
            return "";

        int i = 0;
        while (i < lb.Length - 1 && lb[i] == '0') i++;
        return lb[i..];
    }

    /// <summary>
    /// Extracts the canon code letter(s) from a SourceRelPath.
    /// </summary>
    private static string ExtractCanonCode(string relPath)
    {
        var fileId = CbetaReferenceHelper.ExtractFileIdFromRelPath(relPath);
        if (string.IsNullOrEmpty(fileId))
            return "";

        int i = 0;
        while (i < fileId.Length && char.IsLetter(fileId[i])) i++;
        return i > 0 ? fileId[..i] : "";
    }

    /// <summary>
    /// Returns the full scholarly name for a canon abbreviation.
    /// </summary>
    private static string GetCanonFullName(string canonCode)
    {
        return canonCode.ToUpperInvariant() switch
        {
            "T" => "Taish\u014d shinsh\u016b daiz\u014dky\u014d",
            "X" => "Xuzangjing (Wan Xu zangjing)",
            "J" => "Jiaxing Canon",
            "B" => "Supplement to the Canon",
            "L" => "Qianlong Canon (L\u00f3ng z\u00e0ng)",
            "K" => "Tripi\u1e6daka Koreana",
            "S" => "Song Canon",
            "P" => "P\u00ecnqi\u00e9 Canon",
            "GA" => "Gandh\u0101ran Buddhist Texts",
            _ => $"({canonCode})"
        };
    }

    private static string GetDraftGroupLabel(ScholarPassage passage)
    {
        if (!string.IsNullOrWhiteSpace(passage.DoctrinalTopic))
            return passage.DoctrinalTopic;
        if (passage.Tags.Count > 0 && !string.IsNullOrWhiteSpace(passage.Tags[0]))
            return passage.Tags[0];
        if (passage.MasterNames.Count > 0 && !string.IsNullOrWhiteSpace(passage.MasterNames[0]))
            return passage.MasterNames[0];
        return "Untagged Passages";
    }

    private static string EscapeYaml(string value) => value.Replace("\\", "\\\\").Replace("\"", "\\\"");

    private readonly record struct DraftPassageGroup(string Group, ScholarPassage Passage, int OriginalIndex);

    private readonly record struct PaperFootnote(int Index, string Reference);
    private static string BuildDelimited(ScholarCollection collection, string delimiter, CitationStyle citationStyle = CitationStyle.Chicago)
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
                BuildPassageCitationLine(passage) ?? string.Empty,  // 3D: formatted_citation (31st column)
                passage.Summary ?? string.Empty,
                passage.ReadingStatus ?? string.Empty,
                passage.Importance?.ToString() ?? string.Empty,
                passage.AnnotationType ?? string.Empty,
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

    // ---------------------------------------------------------------
    // Citation helpers (3C / 3D)
    // ---------------------------------------------------------------

    /// <summary>
    /// Build a short Chicago-note-form citation for a single passage.
    /// Format: "{Canon} no. {Number}, {Volume}: {PageRef}. CBETA. {ShareUrl}"
    /// Returns null when there is insufficient CBETA metadata to form a reference.
    /// </summary>
    private static string? BuildPassageCitationLine(ScholarPassage passage)
    {
        var fileId = ExtractSourceTitle(passage.SourceRelPath);
        CbetaReferenceHelper.TryParseCbetaFromFileId(fileId, out var canon, out var vol, out var number);
        var cbetaRef = CbetaReferenceHelper.FormatCbetaReference(passage.FromLb, canon, vol, number);
        if (cbetaRef == null) return null;

        var shareUrl = BuildShareUrl(passage);
        return string.IsNullOrWhiteSpace(shareUrl)
            ? $"{cbetaRef}. CBETA."
            : $"{cbetaRef}. CBETA. {shareUrl}";
    }

    /// <summary>
    /// Build a deduplicated Works Cited block in HTML for all passages in the collection.
    /// One &lt;p&gt; entry per unique source text (keyed by FileId).
    /// </summary>
    private static string BuildWorksCitedHtml(ScholarCollection collection)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var sb = new StringBuilder();
        foreach (var p in collection.Passages)
        {
            var fileId = ExtractSourceTitle(p.SourceRelPath);
            if (!seen.Add(fileId)) continue;
            CbetaReferenceHelper.TryParseCbetaFromFileId(fileId, out var canon, out var vol, out var number);
            if (canon == null || !vol.HasValue || number == null) continue;

            var author = p.MasterNames.Count > 0 ? p.MasterNames[0] : null;
            var entry = BuildPrimarySourceEntryText(fileId, author, canon, vol.Value, number);
            sb.AppendLine($"<p>{Esc(entry)}</p>");
        }
        return sb.ToString();
    }

    /// <summary>
    /// Build a deduplicated Works Cited block in Markdown for all passages in the collection.
    /// One entry per unique source text (keyed by FileId).
    /// </summary>
    private static string BuildWorksCitedMarkdown(ScholarCollection collection)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var sb = new StringBuilder();
        foreach (var p in collection.Passages)
        {
            var fileId = ExtractSourceTitle(p.SourceRelPath);
            if (!seen.Add(fileId)) continue;
            CbetaReferenceHelper.TryParseCbetaFromFileId(fileId, out var canon, out var vol, out var number);
            if (canon == null || !vol.HasValue || number == null) continue;

            var author = p.MasterNames.Count > 0 ? p.MasterNames[0] : null;
            var entry = BuildPrimarySourceEntryText(fileId, author, canon, vol.Value, number);
            sb.AppendLine(entry);
            sb.AppendLine();
        }
        return sb.ToString();
    }

    /// <summary>
    /// Format a single primary-source bibliography entry.
    /// Example: "Wumen Huikai. Wumenguan. T no. 2005, vol. 48."
    /// </summary>
    private static string BuildPrimarySourceEntryText(
        string fileId, string? author, string canon, int volume, string number)
    {
        var sb = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(author))
            sb.Append(author).Append(". ");
        sb.Append(fileId);
        sb.Append($". {canon} no. {number}, vol. {volume}.");
        return sb.ToString();
    }

    private static string Esc(string s) => WebUtility.HtmlEncode(s);
}
