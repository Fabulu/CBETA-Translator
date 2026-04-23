using System;
using System.Collections.Generic;
using System.Linq;
using ReadZen.App.Models;

namespace ReadZen.App.Services;

public sealed class TypeaheadService
{
    private List<(ZenMasterRecord Record, string Blob)> _masterBlobs = new();
    private List<(FileNavItem Item, string Blob, string ZhTitle, string EnTitle)> _titleBlobs = new();

    public void Initialize(ZenMasterCatalog? catalog, IReadOnlyList<FileNavItem>? fileIndex)
    {
        // Only update the parts that are provided (don't overwrite with empty)
        if (catalog == null && fileIndex == null) return;

        if (catalog != null)
            _masterBlobs = catalog.Records
                .Select(r =>
                {
                    var parts = new List<string> { r.CanonicalName };
                    parts.AddRange(r.Aliases);
                    return (r, string.Join(" ", parts).ToLowerInvariant());
                })
                .ToList();

        if (fileIndex != null)
        _titleBlobs = fileIndex
            .Select(f =>
            {
                // Tooltip typically has "English Title · 中文標題" or just one
                var tooltip = f.Tooltip ?? "";
                var zhTitle = "";
                var enTitle = f.DisplayShort ?? f.FileName;
                var sep = tooltip.IndexOf(" · ", StringComparison.Ordinal);
                if (sep >= 0)
                {
                    enTitle = tooltip[..sep].Trim();
                    zhTitle = tooltip[(sep + 3)..].Trim();
                }
                else if (tooltip.Length > 0)
                {
                    // Check if tooltip is mostly CJK
                    bool isCjk = tooltip.Length > 0 && tooltip[0] >= '\u4E00';
                    if (isCjk) zhTitle = tooltip;
                    else enTitle = tooltip;
                }

                var blob = $"{f.DisplayShort} {f.Tooltip} {f.FileName} {f.RelPath}".ToLowerInvariant();
                return (f, blob, zhTitle, enTitle);
            })
            .ToList();
    }

    public List<TypeaheadDisplayItem> Query(string input)
    {
        var results = new List<TypeaheadDisplayItem>();
        if (string.IsNullOrWhiteSpace(input) || input.Length < 1)
            return results;

        var lq = input.Trim().ToLowerInvariant();

        // Masters — top 3, prefix-sorted
        var masterHits = _masterBlobs
            .Where(b => b.Blob.Contains(lq))
            .OrderBy(b => b.Blob.StartsWith(lq) ? 0 : 1)
            .Take(3)
            .ToList();

        if (masterHits.Count > 0)
        {
            results.Add(new TypeaheadDisplayItem { Kind = TypeaheadItemKind.SectionHeader, HeaderText = "Masters" });
            foreach (var (record, _) in masterHits)
            {
                var meta = string.Join(" \u00b7 ",
                    new[] { record.School, record.DatesSummary }
                        .Where(s => !string.IsNullOrWhiteSpace(s)));
                results.Add(new TypeaheadDisplayItem
                {
                    Kind = TypeaheadItemKind.Master,
                    Master = record,
                    DisplayName = record.CanonicalName,
                    Meta = meta
                });
            }
        }

        // Titles — top 5, prefix-sorted for consistency
        var titleHits = _titleBlobs
            .Where(b => b.Blob.Contains(lq))
            .OrderBy(b => b.Blob.StartsWith(lq) ? 0 : 1)
            .ThenBy(b => b.Item.DisplayShort, StringComparer.OrdinalIgnoreCase)
            .Take(5)
            .ToList();

        if (titleHits.Count > 0)
        {
            results.Add(new TypeaheadDisplayItem { Kind = TypeaheadItemKind.SectionHeader, HeaderText = "Texts" });
            foreach (var (item, _, zhTitle, enTitle) in titleHits)
            {
                results.Add(new TypeaheadDisplayItem
                {
                    Kind = TypeaheadItemKind.Title,
                    FileItem = item,
                    ZhTitle = zhTitle,
                    EnTitle = enTitle,
                    DisplayName = item.DisplayShort
                });
            }
        }

        // Full-text action — always
        results.Add(new TypeaheadDisplayItem
        {
            Kind = TypeaheadItemKind.FullTextAction,
            Query = input.Trim()
        });

        return results;
    }
}
