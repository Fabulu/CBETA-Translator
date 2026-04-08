using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using ReadZen.App.Models;
using ReadZen.App.Services;

namespace ReadZen.App.ViewModels;

public sealed class ComparePassageItem
{
    public ScholarPassage Passage { get; set; } = new();
    public string SourceTitle { get; set; } = "";
    public List<(int Start, int Length)> SharedZhRanges { get; set; } = new();
}

public sealed class ComparePassagesViewModel : ViewModelBase
{
    public ObservableCollection<ComparePassageItem> Items { get; } = new();

    public ComparePassagesViewModel(List<ScholarPassage> passages)
    {
        if (passages == null || passages.Count == 0)
            return;

        // Build items with source titles
        var items = passages.Select(p => new ComparePassageItem
        {
            Passage = p,
            SourceTitle = ExtractSourceTitle(p.SourceRelPath),
        }).ToList();

        // Compute pairwise shared ranges and merge per passage
        for (int i = 0; i < items.Count; i++)
        {
            var merged = new List<(int Start, int Length)>();

            for (int j = 0; j < items.Count; j++)
            {
                if (i == j) continue;

                var rawRanges = CjkMatchNormalizer.FindSharedRawRanges(
                    items[i].Passage.ZhText,
                    items[j].Passage.ZhText);

                foreach (var r in rawRanges)
                    merged.Add((r.Start, r.Length));
            }

            items[i].SharedZhRanges = MergeRanges(merged);
        }

        foreach (var item in items)
            Items.Add(item);
    }

    private static string ExtractSourceTitle(string relPath)
    {
        if (string.IsNullOrEmpty(relPath))
            return "(unknown)";

        var fileName = relPath;
        int lastSlash = relPath.LastIndexOfAny(new[] { '/', '\\' });
        if (lastSlash >= 0 && lastSlash < relPath.Length - 1)
            fileName = relPath.Substring(lastSlash + 1);

        int dotIdx = fileName.LastIndexOf('.');
        if (dotIdx > 0)
            fileName = fileName.Substring(0, dotIdx);

        return fileName;
    }

    private static List<(int Start, int Length)> MergeRanges(List<(int Start, int Length)> ranges)
    {
        if (ranges.Count <= 1)
            return ranges;

        ranges.Sort((a, b) => a.Start != b.Start ? a.Start.CompareTo(b.Start) : a.Length.CompareTo(b.Length));

        var merged = new List<(int Start, int Length)> { ranges[0] };
        for (int i = 1; i < ranges.Count; i++)
        {
            var last = merged[^1];
            var cur = ranges[i];
            int lastEnd = last.Start + last.Length;
            int curEnd = cur.Start + cur.Length;

            if (cur.Start <= lastEnd)
            {
                merged[^1] = (last.Start, Math.Max(lastEnd, curEnd) - last.Start);
            }
            else
            {
                merged.Add(cur);
            }
        }

        return merged;
    }
}
