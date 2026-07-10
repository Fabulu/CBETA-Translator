using ReadZen.App.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace ReadZen.App.Services;

/// <summary>
/// Handles sort+dedup of community-shared data files (approved TM and termbase),
/// including merge-from-upstream for local CRDT-style sync.
/// </summary>
public sealed class CommunityDataService : ICommunityDataService
{
    private static readonly JsonSerializerOptions ReadOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly JsonSerializerOptions WriteOpts = new()
    {
        WriteIndented = false,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private static readonly JsonSerializerOptions TermbaseWriteOpts = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    // Shared TmRow shape across all three TM services — must match their serialized fields.
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
    /// Local entries always win when the same key exists in both local and upstream,
    /// preserving the user's translations. Upstream entries are only added when no
    /// local entry exists for that key.
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

        // Build a set of keys present in local data
        var localKeys = new HashSet<string>(
            local.Select(r => MakeTmKey(r)),
            StringComparer.Ordinal);

        // Only add upstream rows whose key is NOT already in local
        var combined = local.Concat(upstream.Where(r => !localKeys.Contains(MakeTmKey(r))));

        var merged = DedupTmRows(combined);
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
        // Safety: never overwrite a non-empty file with empty data
        if (rows.Count == 0 && File.Exists(path) && new FileInfo(path).Length > 10)
            return;

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        var sb = new StringBuilder();
        foreach (var row in rows)
        {
            ct.ThrowIfCancellationRequested();
            // Historical format: this writer always emitted "BlockNumber":0 for rows
            // that carry none (the shared TmRow omits null) - keep the bytes stable.
            row.BlockNumber ??= 0;
            sb.AppendLine(JsonSerializer.Serialize(row, WriteOpts));
        }

        var tmpPath = path + ".tmp";
        await File.WriteAllTextAsync(tmpPath, sb.ToString(), new UTF8Encoding(false), ct);
        File.Move(tmpPath, path, overwrite: true);
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
    /// Local entries always win when the same SourceTerm exists in both local and
    /// upstream, preserving the user's terminology (including AlternateTargets).
    /// Upstream entries are only added when no local entry exists for that term.
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

        // Build a set of source terms present in local data
        var localTerms = new HashSet<string>(
            local.Where(e => !string.IsNullOrWhiteSpace(e.SourceTerm))
                 .Select(e => e.SourceTerm.Trim()),
            StringComparer.Ordinal);

        // Only add upstream entries whose SourceTerm is NOT already in local
        var combined = local.Concat(
            upstream.Where(e => !string.IsNullOrWhiteSpace(e.SourceTerm)
                             && !localTerms.Contains(e.SourceTerm.Trim())));

        var merged = DedupTermbase(combined);
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
        // Safety: never overwrite a non-empty file with empty data
        if (entries.Count == 0 && File.Exists(path) && new FileInfo(path).Length > 10)
            return;

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        var json = JsonSerializer.Serialize(entries, TermbaseWriteOpts);
        var tmpPath = path + ".tmp";
        await File.WriteAllTextAsync(tmpPath, json, new UTF8Encoding(false), ct);
        File.Move(tmpPath, path, overwrite: true);
    }

    // -----------------------------------------------------------------------
    // Scholar Collections
    // -----------------------------------------------------------------------

    private const string ScholarCollectionsFileName = "scholar-collections.json";

    /// <summary>
    /// Sort + dedup scholar collections in place.
    /// Passages deduped by Id (keep newest by ModifiedUtc ?? AddedUtc).
    /// Collections deduped by Id (keep newest by ModifiedUtc ?? CreatedUtc).
    /// Returns the number of kept collections.
    /// </summary>
    public async Task<int> SortAndDedupScholarCollectionsAsync(string root, CancellationToken ct = default)
    {
        var path = Path.Combine(root, ScholarCollectionsFileName);
        if (!File.Exists(path))
            return 0;

        var collections = await LoadScholarCollectionsAsync(path, ct);
        var deduped = DedupScholarCollections(collections);

        await WriteScholarCollectionsAsync(path, deduped, ct);
        return deduped.Count;
    }

    /// <summary>
    /// Merge upstream scholar collections into local.
    /// When collections share the same Id, local (user) metadata is always preserved
    /// (StudyNotes, GraphLayout, Concepts, NodeAnnotations, Edges, etc.), while
    /// Passages and Links are unioned from both sources and deduped.
    /// Upstream-only collections are added as-is.
    /// Returns the number of kept collections.
    /// </summary>
    public async Task<int> MergeScholarCollectionsFromAsync(
        string localRoot,
        string upstreamPath,
        CancellationToken ct = default)
    {
        var localPath = Path.Combine(localRoot, ScholarCollectionsFileName);

        var local = File.Exists(localPath)
            ? await LoadScholarCollectionsAsync(localPath, ct)
            : new List<Models.ScholarCollection>();

        var upstream = File.Exists(upstreamPath)
            ? await LoadScholarCollectionsAsync(upstreamPath, ct)
            : new List<Models.ScholarCollection>();

        // Index local collections by Id — local is the authoritative source for metadata
        var byId = new Dictionary<string, Models.ScholarCollection>(StringComparer.Ordinal);

        foreach (var c in local)
        {
            if (string.IsNullOrWhiteSpace(c.Id))
                continue;

            c.Passages = DedupPassages(c.Passages);
            c.Links = DedupLinks(c.Links);
            byId[c.Id] = c;
        }

        // Merge upstream: union Passages/Links but preserve local metadata when both exist
        foreach (var c in upstream)
        {
            if (string.IsNullOrWhiteSpace(c.Id))
                continue;

            if (byId.TryGetValue(c.Id, out var localCol))
            {
                // Union passages and links from upstream into local's collection
                localCol.Passages = DedupPassages(localCol.Passages.Concat(c.Passages));
                localCol.Links = DedupLinks(localCol.Links.Concat(c.Links));
                // All other metadata (StudyNotes, GraphLayout, Concepts, NodeAnnotations,
                // Edges, CustomEdgeTypes, CollectionRefs, EdgePreferences, ExtraMasters,
                // SuppressedAutoNodeIds, SuppressedAutoEdgeIds, LinkNodes, Name,
                // Description, Tags) stays from local.
            }
            else
            {
                // Upstream-only collection — add it
                c.Passages = DedupPassages(c.Passages);
                c.Links = DedupLinks(c.Links);
                byId[c.Id] = c;
            }
        }

        var result = byId.Values
            .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(c => c.Id, StringComparer.Ordinal)
            .ToList();

        await WriteScholarCollectionsAsync(localPath, result, ct);
        return result.Count;
    }

    private static async Task<List<Models.ScholarCollection>> LoadScholarCollectionsAsync(string path, CancellationToken ct)
    {
        try
        {
            var json = await File.ReadAllTextAsync(path, Encoding.UTF8, ct);
            if (string.IsNullOrWhiteSpace(json))
                return new List<Models.ScholarCollection>();

            return JsonSerializer.Deserialize<List<Models.ScholarCollection>>(json, ReadOpts)
                ?? new List<Models.ScholarCollection>();
        }
        catch
        {
            return new List<Models.ScholarCollection>();
        }
    }

    private static List<Models.ScholarCollection> DedupScholarCollections(List<Models.ScholarCollection> collections)
    {
        return collections
            .Where(c => !string.IsNullOrWhiteSpace(c.Id))
            .GroupBy(c => c.Id, StringComparer.Ordinal)
            .Select(g =>
            {
                var winner = g.OrderByDescending(c => GetCollectionTimestamp(c)).First();
                // Merge passages from all duplicates
                winner.Passages = DedupPassages(g.SelectMany(c => c.Passages));
                return winner;
            })
            .OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(c => c.Id, StringComparer.Ordinal)
            .ToList();
    }

    private static List<Models.ScholarPassage> DedupPassages(IEnumerable<Models.ScholarPassage> passages)
    {
        return passages
            .Where(p => !string.IsNullOrWhiteSpace(p.Id))
            .GroupBy(p => p.Id, StringComparer.Ordinal)
            .Select(g => g.OrderByDescending(p => p.ModifiedUtc ?? p.AddedUtc).First())
            .OrderBy(p => p.SourceRelPath, StringComparer.OrdinalIgnoreCase)
            .ThenBy(p => p.AddedUtc)
            .ToList();
    }

    private static List<Models.PassageLink> DedupLinks(IEnumerable<Models.PassageLink> links)
    {
        return links
            .Where(l => !string.IsNullOrWhiteSpace(l.Id))
            .GroupBy(l => l.Id, StringComparer.Ordinal)
            .Select(g => g.OrderByDescending(l => l.CreatedUtc).First())
            .OrderBy(l => l.FromPassageId, StringComparer.Ordinal)
            .ThenBy(l => l.ToPassageId, StringComparer.Ordinal)
            .ToList();
    }

    private static DateTimeOffset GetCollectionTimestamp(Models.ScholarCollection c)
        => c.ModifiedUtc ?? c.CreatedUtc;

    private static async Task WriteScholarCollectionsAsync(string path, List<Models.ScholarCollection> collections, CancellationToken ct)
    {
        // Safety: never overwrite a non-empty file with empty data
        if (collections.Count == 0 && File.Exists(path) && new FileInfo(path).Length > 10)
            return;

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        var json = JsonSerializer.Serialize(collections, TermbaseWriteOpts);
        var tmpPath = path + ".tmp";
        await File.WriteAllTextAsync(tmpPath, json, new UTF8Encoding(false), ct);
        File.Move(tmpPath, path, overwrite: true);
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static string NormalizeRel(string? p) => ReadZen.App.Infrastructure.RelPath.Normalize(p);
}
