// Services/MasterCorpusSearchService.cs
// Scans the CBETA and OpenZen corpora for zen master name mentions.
// Builds a cached index of primary (author) and secondary (mentioned) appearances.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ReadZen.App.Infrastructure;
using ReadZen.App.Models;

namespace ReadZen.App.Services;

public sealed class MasterCorpusSearchService
{
    private const string CacheFileName = "master-corpus-index.json";
    private const int MinNameLength = 2; // minimum CJK chars for matching

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>
    /// Extracts (CanonicalName, ChineseNames) pairs from a ZenMasterCatalog.
    /// </summary>
    public static List<(string CanonicalName, List<string> ChineseNames)> ExtractMasterNames(ZenMasterCatalog catalog)
    {
        var result = new List<(string, List<string>)>();
        foreach (var record in catalog.Records)
        {
            var chineseNames = record.Aliases
                .Where(a => a.Length >= MinNameLength && MasterDatesService.ContainsCjk(a))
                .OrderByDescending(a => a.Length)
                .ToList();

            if (chineseNames.Count > 0)
                result.Add((record.CanonicalName ?? "(unnamed)", chineseNames));
        }
        return result;
    }

    /// <summary>
    /// Loads titles.jsonl from a repo root (same format as IndexCacheService).
    /// </summary>
    public static Dictionary<string, (string? Zh, string? En)> LoadTitles(string repoRoot)
    {
        var dict = new Dictionary<string, (string? Zh, string? En)>(StringComparer.OrdinalIgnoreCase);
        var titlesPath = Path.Combine(repoRoot, "titles.jsonl");
        if (!File.Exists(titlesPath)) return dict;

        foreach (var line in File.ReadLines(titlesPath, Encoding.UTF8))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            try
            {
                using var doc = JsonDocument.Parse(line);
                var r = doc.RootElement;
                if (!r.TryGetProperty("path", out var pathEl)) continue;
                var path = pathEl.GetString();
                if (string.IsNullOrWhiteSpace(path)) continue;

                var key = path.Replace('\\', '/').TrimStart('/');
                string? zh = r.TryGetProperty("zh", out var zhEl) ? zhEl.GetString() : null;
                string? en = r.TryGetProperty("en", out var enEl) ? enEl.GetString() : null;
                dict[key] = (zh, en);
            }
            catch { /* skip bad lines */ }
        }
        return dict;
    }

    /// <summary>
    /// Discovers all corpus original directories using AppPaths.
    /// Returns (corpusLabel, originalDir) pairs.
    /// </summary>
    public static List<(string Label, string OriginalDir)> DiscoverCorpusDirs(string parentRoot)
    {
        var results = new List<(string, string)>();
        var corpora = AppPaths.DiscoverAllCorpora(parentRoot);
        foreach (var layout in corpora)
        {
            if (Directory.Exists(layout.OriginalDir))
                results.Add((layout.Kind.ToString(), layout.OriginalDir));
        }
        return results;
    }

    /// <summary>
    /// Builds a combined index across all discovered corpora.
    /// </summary>
    public async Task<MasterCorpusIndex> BuildFullIndexAsync(
        string parentRoot,
        ZenMasterCatalog catalog,
        IProgress<(int done, int total, string status)>? progress = null,
        CancellationToken ct = default)
    {
        var masters = ExtractMasterNames(catalog);
        var corpusDirs = DiscoverCorpusDirs(parentRoot);

        var combined = new MasterCorpusIndex
        {
            BuiltUtc = DateTime.UtcNow.ToString("o"),
            Corpus = string.Join("+", corpusDirs.Select(c => c.Label)),
        };

        int totalFiles = 0;
        foreach (var (label, dir) in corpusDirs)
        {
            // Try loading titles from sibling translations repo
            var titles = TryLoadTitlesForCorpus(parentRoot, label);

            var index = await BuildIndexAsync(dir, label, masters, titles, progress, ct);
            combined.Appearances.AddRange(index.Appearances);
            totalFiles += index.FileCount;
        }

        combined.FileCount = totalFiles;
        combined.Appearances = combined.Appearances
            .OrderBy(a => a.MasterName)
            .ThenByDescending(a => a.AppearanceType == "primary" ? 0 : 1)
            .ThenByDescending(a => a.MentionCount)
            .ToList();
        combined.MasterCount = combined.Appearances.Select(a => a.MasterName).Distinct().Count();

        return combined;
    }

    private static Dictionary<string, (string? Zh, string? En)> TryLoadTitlesForCorpus(string parentRoot, string corpusLabel)
    {
        // Try to find the translations repo to load titles from
        var corpora = AppPaths.DiscoverAllCorpora(parentRoot);
        foreach (var layout in corpora)
        {
            if (layout.Kind.ToString() == corpusLabel && !string.IsNullOrEmpty(layout.TranslationsRepoRoot))
                return LoadTitles(layout.TranslationsRepoRoot);
        }
        return new();
    }

    /// <summary>
    /// Builds the master corpus index by scanning all XML files in the given directory.
    /// This is CPU-intensive and should run in the background.
    /// </summary>
    public async Task<MasterCorpusIndex> BuildIndexAsync(
        string originalDir,
        string corpus,
        List<(string CanonicalName, List<string> ChineseNames)> masters,
        Dictionary<string, (string? Zh, string? En)> titles,
        IProgress<(int done, int total, string status)>? progress = null,
        CancellationToken ct = default)
    {
        var index = new MasterCorpusIndex
        {
            Corpus = corpus,
            BuiltUtc = DateTime.UtcNow.ToString("o"),
        };

        if (!Directory.Exists(originalDir))
            return index;

        // Build search patterns: canonical name -> list of Chinese names (sorted longest first)
        var searchPatterns = new List<(string CanonicalName, string ChineseName)>();
        foreach (var (canonicalName, chineseNames) in masters)
        {
            foreach (var cn in chineseNames.OrderByDescending(n => n.Length))
            {
                if (cn.Length >= MinNameLength)
                    searchPatterns.Add((canonicalName, cn));
            }
        }

        var xmlFiles = Directory.EnumerateFiles(originalDir, "*.xml", SearchOption.AllDirectories).ToList();
        index.FileCount = xmlFiles.Count;

        var allAppearances = new ConcurrentBag<MasterTextAppearance>();
        int done = 0;

        await Task.Run(() =>
        {
            Parallel.ForEach(xmlFiles, new ParallelOptions { MaxDegreeOfParallelism = 4, CancellationToken = ct }, file =>
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    var content = File.ReadAllText(file, Encoding.UTF8);
                    var relPath = Path.GetRelativePath(originalDir, file);

                    // Get title
                    var relKey = relPath.Replace('\\', '/');
                    titles.TryGetValue(relKey, out var titleInfo);

                    // Check TEI header for author (primary detection)
                    var headerEnd = content.IndexOf("</teiHeader>", StringComparison.Ordinal);
                    var header = headerEnd > 0 ? content[..headerEnd] : "";

                    // Search for each master's Chinese names
                    var foundMasters = new Dictionary<string, (string MatchedName, int Count, string? Snippet, bool IsPrimary)>();

                    foreach (var (canonicalName, chineseName) in searchPatterns)
                    {
                        if (foundMasters.ContainsKey(canonicalName)) continue; // already found by a longer name

                        int count = CountOccurrences(content, chineseName);
                        if (count == 0) continue;

                        // Check if this master is the author (primary)
                        bool isPrimary = header.Contains(chineseName, StringComparison.Ordinal);

                        // Extract a snippet around the first body occurrence
                        string? snippet = ExtractSnippet(content, chineseName, headerEnd > 0 ? headerEnd : 0);

                        foundMasters[canonicalName] = (chineseName, count, snippet, isPrimary);
                    }

                    foreach (var (masterName, (matchedName, count, snippet, isPrimary)) in foundMasters)
                    {
                        allAppearances.Add(new MasterTextAppearance
                        {
                            MasterName = masterName,
                            MatchedName = matchedName,
                            RelPath = relPath,
                            TextTitle = titleInfo.En,
                            TextTitleZh = titleInfo.Zh,
                            AppearanceType = isPrimary ? "primary" : "secondary",
                            MentionCount = count,
                            Snippet = snippet,
                        });
                    }
                }
                catch { /* skip unreadable files */ }

                var d = Interlocked.Increment(ref done);
                if (d % 100 == 0 || d == xmlFiles.Count)
                    progress?.Report((d, xmlFiles.Count, $"Scanning {d}/{xmlFiles.Count}..."));
            });
        }, ct);

        index.Appearances = allAppearances
            .OrderBy(a => a.MasterName)
            .ThenByDescending(a => a.AppearanceType == "primary" ? 0 : 1)
            .ThenByDescending(a => a.MentionCount)
            .ToList();

        index.MasterCount = index.Appearances.Select(a => a.MasterName).Distinct().Count();

        return index;
    }

    /// <summary>Saves the index to a cache file.</summary>
    public async Task SaveAsync(string cacheDir, MasterCorpusIndex index, CancellationToken ct = default)
    {
        Directory.CreateDirectory(cacheDir);
        var path = Path.Combine(cacheDir, CacheFileName);
        var json = JsonSerializer.Serialize(index, JsonOpts);
        await File.WriteAllTextAsync(path, json, new UTF8Encoding(false), ct);
    }

    /// <summary>Loads the cached index, or null if not available.</summary>
    public async Task<MasterCorpusIndex?> TryLoadAsync(string cacheDir, CancellationToken ct = default)
    {
        var path = Path.Combine(cacheDir, CacheFileName);
        if (!File.Exists(path)) return null;

        try
        {
            var json = await File.ReadAllTextAsync(path, Encoding.UTF8, ct);
            return JsonSerializer.Deserialize<MasterCorpusIndex>(json, JsonOpts);
        }
        catch { return null; }
    }

    /// <summary>Gets the default cache directory for master corpus data.</summary>
    public static string GetCacheDir(string parentRoot)
    {
        return Path.Combine(parentRoot, ".readzen-cache");
    }

    /// <summary>Gets appearances for a specific master, split into primary and secondary.</summary>
    public static (List<MasterTextAppearance> Primary, List<MasterTextAppearance> Secondary)
        GetAppearancesForMaster(MasterCorpusIndex index, string masterName)
    {
        var all = index.Appearances
            .Where(a => string.Equals(a.MasterName, masterName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        return (
            all.Where(a => a.AppearanceType == "primary").ToList(),
            all.Where(a => a.AppearanceType == "secondary").ToList()
        );
    }

    /// <summary>Gets co-occurrence stats: which masters appear together in the same texts.</summary>
    public static Dictionary<string, Dictionary<string, int>> GetCoOccurrenceMatrix(MasterCorpusIndex index)
    {
        var matrix = new Dictionary<string, Dictionary<string, int>>(StringComparer.OrdinalIgnoreCase);

        // Group by text file
        var byFile = index.Appearances.GroupBy(a => a.RelPath).ToList();

        foreach (var group in byFile)
        {
            var mastersInFile = group.Select(a => a.MasterName).Distinct().ToList();

            for (int i = 0; i < mastersInFile.Count; i++)
            {
                for (int j = i + 1; j < mastersInFile.Count; j++)
                {
                    var a = mastersInFile[i];
                    var b = mastersInFile[j];

                    if (!matrix.ContainsKey(a)) matrix[a] = new(StringComparer.OrdinalIgnoreCase);
                    if (!matrix.ContainsKey(b)) matrix[b] = new(StringComparer.OrdinalIgnoreCase);

                    matrix[a].TryGetValue(b, out var countAB);
                    matrix[a][b] = countAB + 1;

                    matrix[b].TryGetValue(a, out var countBA);
                    matrix[b][a] = countBA + 1;
                }
            }
        }

        return matrix;
    }

    /// <summary>Gets top co-occurring masters for a specific master.</summary>
    public static List<(string MasterName, int SharedTexts)> GetTopCoOccurrences(
        MasterCorpusIndex index, string masterName, int limit = 10)
    {
        var matrix = GetCoOccurrenceMatrix(index);
        if (!matrix.TryGetValue(masterName, out var peers))
            return new();

        return peers
            .OrderByDescending(kv => kv.Value)
            .Take(limit)
            .Select(kv => (kv.Key, kv.Value))
            .ToList();
    }

    private static int CountOccurrences(string text, string pattern)
    {
        int count = 0;
        int idx = 0;
        while ((idx = text.IndexOf(pattern, idx, StringComparison.Ordinal)) >= 0)
        {
            count++;
            idx += pattern.Length;
        }
        return count;
    }

    private static string? ExtractSnippet(string text, string pattern, int startAfter)
    {
        var idx = text.IndexOf(pattern, Math.Max(0, startAfter), StringComparison.Ordinal);
        if (idx < 0) idx = text.IndexOf(pattern, StringComparison.Ordinal);
        if (idx < 0) return null;

        int snippetStart = Math.Max(0, idx - 30);
        int snippetEnd = Math.Min(text.Length, idx + pattern.Length + 30);

        // Clean up XML tags from snippet
        var raw = text[snippetStart..snippetEnd];
        var clean = System.Text.RegularExpressions.Regex.Replace(raw, "<[^>]+>", "").Trim();
        return clean.Length > 80 ? clean[..77] + "..." : clean;
    }
}
