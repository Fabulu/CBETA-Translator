using CbetaTranslator.App.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CbetaTranslator.App.Services;

/// <summary>
/// Handles sort+dedup of community-shared data files (approved TM and termbase),
/// including merge-from-upstream for local CRDT-style sync.
/// </summary>
public sealed class CommunityDataService
{
    private static readonly JsonSerializerOptions ReadOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly JsonSerializerOptions WriteOpts = new()
    {
        WriteIndented = false
    };

    private static readonly JsonSerializerOptions TermbaseWriteOpts = new()
    {
        WriteIndented = true
    };

    // Shared TmRow shape across all three TM services — must match their serialized fields.
    private sealed class TmRow
    {
        public string SourceText { get; set; } = "";
        public string TargetText { get; set; } = "";
        public string RelPath { get; set; } = "";
        public int BlockNumber { get; set; }
        public string ReviewStatus { get; set; } = "";
        public string Translator { get; set; } = "";
        public DateTimeOffset? WrittenUtc { get; set; }
    }

    // -----------------------------------------------------------------------
    // Approved TM
    // -----------------------------------------------------------------------

    private const string TmFileName = "translation-memory.approved.jsonl";

    /// <summary>
    /// Sort + dedup <paramref name="root"/>/translation-memory.approved.jsonl in place.
    /// Key = (RelPath, SourceText). Last-write-wins by WrittenUtc.
    /// Returns the number of kept rows.
    /// </summary>
    public async Task<int> SortAndDedupApprovedTmAsync(string root, CancellationToken ct = default)
    {
        var path = Path.Combine(root, TmFileName);
        if (!File.Exists(path))
            return 0;

        var rows = await LoadTmRowsAsync(path, ct);
        var deduped = DedupTmRows(rows);

        await WriteTmRowsAsync(path, deduped, ct);
        return deduped.Count;
    }

    /// <summary>
    /// Merge the approved TM from <paramref name="upstreamTmPath"/> into
    /// <paramref name="localRoot"/>/translation-memory.approved.jsonl.
    /// Local entries that are newer (by WrittenUtc) win; otherwise upstream wins.
    /// Returns the number of kept rows.
    /// </summary>
    public async Task<int> MergeApprovedTmFromAsync(
        string localRoot,
        string upstreamTmPath,
        CancellationToken ct = default)
    {
        var localPath = Path.Combine(localRoot, TmFileName);

        var local = File.Exists(localPath)
            ? await LoadTmRowsAsync(localPath, ct)
            : new List<TmRow>();

        var upstream = File.Exists(upstreamTmPath)
            ? await LoadTmRowsAsync(upstreamTmPath, ct)
            : new List<TmRow>();

        var merged = DedupTmRows(local.Concat(upstream));
        await WriteTmRowsAsync(localPath, merged, ct);
        return merged.Count;
    }

    private static async Task<List<TmRow>> LoadTmRowsAsync(string path, CancellationToken ct)
    {
        var rows = new List<TmRow>();

        using var fs = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.ReadWrite | FileShare.Delete);

        using var sr = new StreamReader(fs, Encoding.UTF8);

        while (!sr.EndOfStream)
        {
            ct.ThrowIfCancellationRequested();

            var line = await sr.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line))
                continue;

            try
            {
                var row = JsonSerializer.Deserialize<TmRow>(line, ReadOpts);
                if (row != null && !string.IsNullOrWhiteSpace(row.SourceText))
                    rows.Add(row);
            }
            catch
            {
                // skip malformed lines
            }
        }

        return rows;
    }

    private static List<TmRow> DedupTmRows(IEnumerable<TmRow> rows)
    {
        return rows
            .GroupBy(r => MakeTmKey(r), StringComparer.Ordinal)
            .Select(g => g.OrderByDescending(r => r.WrittenUtc ?? DateTimeOffset.MinValue).First())
            .OrderBy(r => NormalizeRel(r.RelPath), StringComparer.OrdinalIgnoreCase)
            .ThenBy(r => r.BlockNumber)
            .ThenBy(r => r.SourceText, StringComparer.Ordinal)
            .ToList();
    }

    private static async Task WriteTmRowsAsync(string path, List<TmRow> rows, CancellationToken ct)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        var sb = new StringBuilder();
        foreach (var row in rows)
        {
            ct.ThrowIfCancellationRequested();
            sb.AppendLine(JsonSerializer.Serialize(row, WriteOpts));
        }

        await File.WriteAllTextAsync(path, sb.ToString(), new UTF8Encoding(false), ct);
    }

    private static string MakeTmKey(TmRow r)
        => $"{NormalizeRel(r.RelPath)}|{r.SourceText.Trim()}";

    // -----------------------------------------------------------------------
    // Termbase
    // -----------------------------------------------------------------------

    private const string TermbaseFileName = "termbase.json";

    /// <summary>
    /// Sort + dedup <paramref name="root"/>/termbase.json in place.
    /// Key = SourceTerm. Last-write-wins by WrittenUtc.
    /// Returns the number of kept entries.
    /// </summary>
    public async Task<int> SortAndDedupTermbaseAsync(string root, CancellationToken ct = default)
    {
        var path = Path.Combine(root, TermbaseFileName);
        if (!File.Exists(path))
            return 0;

        var entries = await LoadTermbaseAsync(path, ct);
        var deduped = DedupTermbase(entries);

        await WriteTermbaseAsync(path, deduped, ct);
        return deduped.Count;
    }

    /// <summary>
    /// Merge the termbase from <paramref name="upstreamTermbasePath"/> into
    /// <paramref name="localRoot"/>/termbase.json.
    /// Last-write-wins by WrittenUtc.
    /// Returns the number of kept entries.
    /// </summary>
    public async Task<int> MergeTermbaseFromAsync(
        string localRoot,
        string upstreamTermbasePath,
        CancellationToken ct = default)
    {
        var localPath = Path.Combine(localRoot, TermbaseFileName);

        var local = File.Exists(localPath)
            ? await LoadTermbaseAsync(localPath, ct)
            : new List<TermbaseEntry>();

        var upstream = File.Exists(upstreamTermbasePath)
            ? await LoadTermbaseAsync(upstreamTermbasePath, ct)
            : new List<TermbaseEntry>();

        var merged = DedupTermbase(local.Concat(upstream));
        await WriteTermbaseAsync(localPath, merged, ct);
        return merged.Count;
    }

    private static async Task<List<TermbaseEntry>> LoadTermbaseAsync(string path, CancellationToken ct)
    {
        try
        {
            var json = await File.ReadAllTextAsync(path, Encoding.UTF8, ct);
            if (string.IsNullOrWhiteSpace(json))
                return new List<TermbaseEntry>();

            return JsonSerializer.Deserialize<List<TermbaseEntry>>(json, ReadOpts)
                ?? new List<TermbaseEntry>();
        }
        catch
        {
            return new List<TermbaseEntry>();
        }
    }

    private static List<TermbaseEntry> DedupTermbase(IEnumerable<TermbaseEntry> entries)
    {
        return entries
            .Where(e => !string.IsNullOrWhiteSpace(e.SourceTerm))
            .GroupBy(e => e.SourceTerm.Trim(), StringComparer.Ordinal)
            .Select(g => g.OrderByDescending(e => e.WrittenUtc ?? DateTimeOffset.MinValue).First())
            .OrderBy(e => e.SourceTerm, StringComparer.Ordinal)
            .ToList();
    }

    private static async Task WriteTermbaseAsync(string path, List<TermbaseEntry> entries, CancellationToken ct)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        var json = JsonSerializer.Serialize(entries, TermbaseWriteOpts);
        await File.WriteAllTextAsync(path, json, new UTF8Encoding(false), ct);
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static string NormalizeRel(string? p)
        => (p ?? "").Replace('\\', '/').TrimStart('/');
}
