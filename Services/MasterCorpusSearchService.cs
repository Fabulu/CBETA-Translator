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

    // Names that are also common Buddhist concepts. When matching these, require
    // the master's longer name to also appear in the same file, otherwise skip.
    private static readonly HashSet<string> ConceptNames = new()
    {
        "法眼",   // Fayan = "Dharma Eye"
        "無門",   // Wumen = "Gateless"
        "大慧",   // Dahui = "Great Wisdom"
        "國師",   // National Teacher (too generic)
        "六祖",   // Sixth Patriarch (too generic)
        "延壽",   // Yanshou = "extend longevity" (matches Medicine Buddha texts)
    };

    // Manual primary-text overrides: the concept-name filter cannot detect these
    // because the TEI <author> field and body text don't contain the full compound
    // name. Key is canonical master name → set of /-normalized RelPath prefixes
    // (matched against relKey, NOT the platform-separator relPath — the old
    // backslash prefix silently never matched on Linux/macOS; audit R3-L5).
    private static readonly Dictionary<string, HashSet<string>> ManualPrimary = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Wumen Huikai"] = new(StringComparer.OrdinalIgnoreCase) { "T/T48/T48n2005" },
    };

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
    /// Stat-stamp over all discovered corpus dirs: xml file count + newest write time.
    /// Cheap (stat-only) staleness signal for the cached index — same pattern as the
    /// termbase community cache (audit P4.6). Null when no corpus dirs exist.
    /// </summary>
    public static string? ComputeCorpusStamp(string parentRoot)
    {
        var corpusDirs = DiscoverCorpusDirs(parentRoot);
        if (corpusDirs.Count == 0) return null;

        int files = 0;
        long maxTicks = 0;
        foreach (var (_, dir) in corpusDirs)
        {
            try
            {
                foreach (var f in Directory.EnumerateFiles(dir, "*.xml", SearchOption.AllDirectories))
                {
                    files++;
                    var t = File.GetLastWriteTimeUtc(f).Ticks;
                    if (t > maxTicks) maxTicks = t;
                }
            }
            catch { /* unreadable dir → reflected by lower count; stamp still differs */ }
        }
        return $"files={files};maxTicks={maxTicks}";
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
            CorpusStamp = ComputeCorpusStamp(parentRoot),
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

        // Build lookup of canonicalName -> all chinese names, used for concept-name disambiguation.
        var namesByCanonical = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var (canonicalName, chineseNames) in masters)
        {
            if (!namesByCanonical.ContainsKey(canonicalName))
                namesByCanonical[canonicalName] = chineseNames
                    .Where(n => n.Length >= MinNameLength)
                    .Distinct()
                    .ToList();
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

                    // Extract <author> element text for targeted matching.
                    // Short concept names (e.g. 延壽) are safe to match in this
                    // controlled metadata field even though they'd be ambiguous
                    // in body text.
                    string authorField = "";
                    if (header.Length > 0)
                    {
                        var authStart = header.IndexOf("<author>", StringComparison.Ordinal);
                        if (authStart >= 0)
                        {
                            authStart += "<author>".Length;
                            var authEnd = header.IndexOf("</author>", authStart, StringComparison.Ordinal);
                            if (authEnd > authStart)
                                authorField = header[authStart..authEnd];
                        }
                    }

                    // Search for each master's Chinese names
                    var foundMasters = new Dictionary<string, (string MatchedName, int Count, string? Snippet, bool IsPrimary)>();

                    foreach (var (canonicalName, chineseName) in searchPatterns)
                    {
                        if (foundMasters.ContainsKey(canonicalName)) continue; // already found by a longer name

                        int count = CountOccurrences(content, chineseName);
                        if (count == 0) continue;

                        // Concept-name disambiguation: if the matched name is also a common
                        // Buddhist concept, require a longer non-concept alias of the same
                        // master to also appear in the file. Otherwise the match is too noisy.
                        // Exception: if the concept name appears in the <author> field, it
                        // genuinely refers to the person (e.g. <author>宋 延壽集</author>).
                        bool foundInAuthorField = ConceptNames.Contains(chineseName)
                            && authorField.Length > 0
                            && authorField.Contains(chineseName, StringComparison.Ordinal);

                        // Manual override bypasses concept-name disambiguation
                        // (relKey is /-normalized; the prefixes are too)
                        bool isManualOverride = ManualPrimary.TryGetValue(canonicalName, out var manualPaths)
                            && manualPaths.Any(p => relKey.StartsWith(p, StringComparison.OrdinalIgnoreCase));

                        if (ConceptNames.Contains(chineseName) && !foundInAuthorField && !isManualOverride)
                        {
                            if (!namesByCanonical.TryGetValue(canonicalName, out var allNames))
                                continue;

                            bool hasCorroboratingName = allNames.Any(n =>
                                n.Length > chineseName.Length
                                && !ConceptNames.Contains(n)
                                && content.IndexOf(n, StringComparison.Ordinal) >= 0);

                            if (!hasCorroboratingName) continue;
                        }

                        // Primary detection: prefer a non-concept alias in the header (strong signal).
                        // Fallback: if the matched name is a concept, it can still count as primary
                        // when the concept-filter passed (guaranteeing a longer non-concept alias
                        // exists in the body) AND the concept appears in the header.
                        // Special case: concept name found in <author> field is a strong primary signal.
                        bool isPrimary = foundInAuthorField;
                        if (!isPrimary && namesByCanonical.TryGetValue(canonicalName, out var aliasesForPrimary))
                        {
                            isPrimary = aliasesForPrimary.Any(n =>
                                !ConceptNames.Contains(n)
                                && header.Contains(n, StringComparison.Ordinal));
                        }
                        if (!isPrimary && ConceptNames.Contains(chineseName))
                            isPrimary = header.Contains(chineseName, StringComparison.Ordinal);

                        // Manual primary override for texts the heuristic can't detect
                        if (!isPrimary && isManualOverride)
                            isPrimary = true;

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

    /// <summary>
    /// Exports a web-friendly masters.json with all master profiles.
    /// Output format matches what views/master.js in the SPA expects.
    /// </summary>
    public static async Task ExportMastersJsonAsync(
        string outputDir,
        Models.ZenMasterCatalog catalog,
        CancellationToken ct = default)
    {
        Directory.CreateDirectory(outputDir);
        var path = Path.Combine(outputDir, "masters.json");

        var masters = new List<Dictionary<string, object?>>();
        foreach (var record in catalog.Records)
        {
            var entry = new Dictionary<string, object?>();
            entry["names"] = record.Aliases;
            var pv = record.PrimaryVariant;
            if (pv != null)
            {
                if (pv.Floruit > 0) entry["floruit"] = pv.Floruit;
                if (pv.Death > 0) entry["death"] = pv.Death;
            }
            if (!string.IsNullOrWhiteSpace(record.School)) entry["school"] = record.School;
            if (!string.IsNullOrWhiteSpace(record.Teacher)) entry["teacher"] = record.Teacher;
            if (record.Students.Count > 0) entry["students"] = record.Students;
            if (!string.IsNullOrWhiteSpace(record.Notes)) entry["notes"] = record.Notes;
            if (!string.IsNullOrWhiteSpace(record.Region)) entry["region"] = record.Region;
            if (record.HasLinks)
            {
                entry["links"] = record.Links.Select(l => new Dictionary<string, string>
                {
                    ["label"] = l.Label,
                    ["url"] = l.Url
                }).ToList();
            }
            masters.Add(entry);
        }

        var output = new Dictionary<string, object>
        {
            ["version"] = 1,
            ["count"] = masters.Count,
            ["masters"] = masters,
        };

        var json = JsonSerializer.Serialize(output, JsonOpts);
        await File.WriteAllTextAsync(path, json, new UTF8Encoding(false), ct);
    }

    /// <summary>
    /// Exports a web-friendly corpus index with all per-master text appearances.
    /// Sorted by mention count descending. Matches format expected by SPA.
    /// </summary>
    public static async Task ExportMasterCorpusJsonAsync(
        string outputDir,
        MasterCorpusIndex index,
        CancellationToken ct = default)
    {
        Directory.CreateDirectory(outputDir);
        var path = Path.Combine(outputDir, "master-corpus.json");

        var byMaster = new Dictionary<string, (List<MasterTextAppearance> Primary, List<MasterTextAppearance> Secondary, int TotalMentions)>();
        foreach (var a in index.Appearances)
        {
            if (!byMaster.TryGetValue(a.MasterName, out var bucket))
                bucket = (new List<MasterTextAppearance>(), new List<MasterTextAppearance>(), 0);
            if (a.AppearanceType == "primary") bucket.Primary.Add(a);
            else bucket.Secondary.Add(a);
            bucket.TotalMentions += a.MentionCount;
            byMaster[a.MasterName] = bucket;
        }

        var masters = new Dictionary<string, object>();
        foreach (var (name, (primary, secondary, total)) in byMaster)
        {
            var primaryTop = primary.OrderByDescending(a => a.MentionCount)
                .Select(a => SerializeAppearance(a)).ToList();
            var secondaryTop = secondary.OrderByDescending(a => a.MentionCount)
                .Select(a => SerializeAppearance(a)).ToList();

            masters[name] = new Dictionary<string, object>
            {
                ["primary_count"] = primary.Count,
                ["secondary_count"] = secondary.Count,
                ["total_mentions"] = total,
                ["primary"] = primaryTop,
                ["secondary"] = secondaryTop,
            };
        }

        var output = new Dictionary<string, object>
        {
            ["version"] = 1,
            ["corpus"] = index.Corpus ?? "",
            ["file_count"] = index.FileCount,
            ["master_count"] = masters.Count,
            ["masters"] = masters,
        };

        var json = JsonSerializer.Serialize(output, JsonOpts);
        await File.WriteAllTextAsync(path, json, new UTF8Encoding(false), ct);
    }

    /// <summary>
    /// Exports per-master JSON shards into corpus/masters/ with an _index.json manifest.
    /// Each master gets its own file (slug.json) for efficient lazy loading by the SPA.
    /// </summary>
    public static async Task ExportMasterCorpusShardedAsync(
        string outputDir,
        MasterCorpusIndex index,
        CancellationToken ct = default)
    {
        var mastersDir = Path.Combine(outputDir, "corpus", "masters");
        Directory.CreateDirectory(mastersDir);

        // Group appearances by master
        var byMaster = index.Appearances
            .GroupBy(a => a.MasterName)
            .ToDictionary(g => g.Key, g => g.ToList());

        var indexEntries = new Dictionary<string, object>();

        foreach (var (masterName, appearances) in byMaster)
        {
            ct.ThrowIfCancellationRequested();

            var primary = appearances
                .Where(a => a.AppearanceType == "primary")
                .OrderByDescending(a => a.MentionCount)
                .ToList();
            var secondary = appearances
                .Where(a => a.AppearanceType == "secondary")
                .OrderByDescending(a => a.MentionCount)
                .ToList();

            var shard = new
            {
                master = masterName,
                primary_count = primary.Count,
                secondary_count = secondary.Count,
                total_mentions = appearances.Sum(a => a.MentionCount),
                primary = primary.Select(a => SerializeAppearance(a)).ToList(),
                secondary = secondary.Select(a => SerializeAppearance(a)).ToList(),
            };

            var slug = Slugify(masterName);
            var shardPath = Path.Combine(mastersDir, slug + ".json");
            var json = JsonSerializer.Serialize(shard, JsonOpts);
            await File.WriteAllTextAsync(shardPath, json, new UTF8Encoding(false), ct);

            indexEntries[masterName] = new
            {
                slug,
                p = primary.Count,
                s = secondary.Count,
                m = appearances.Sum(a => a.MentionCount),
            };
        }

        // Write index file
        var indexObj = new
        {
            version = 2,
            corpus = index.Corpus ?? "Cbeta",
            file_count = index.FileCount,
            master_count = byMaster.Count,
            built_utc = index.BuiltUtc ?? DateTime.UtcNow.ToString("o"),
            masters = indexEntries,
        };

        var indexPath = Path.Combine(mastersDir, "_index.json");
        var indexJson = JsonSerializer.Serialize(indexObj, JsonOpts);
        await File.WriteAllTextAsync(indexPath, indexJson, new UTF8Encoding(false), ct);
    }

    private static string Slugify(string name)
        => name.ToLowerInvariant()
               .Replace("\u2019", "")  // right single quote
               .Replace("'", "")
               .Replace(' ', '_');

    private static Dictionary<string, object?> SerializeAppearance(MasterTextAppearance a)
    {
        var dict = new Dictionary<string, object?>
        {
            ["path"] = a.RelPath,
            ["mentions"] = a.MentionCount,
        };
        if (!string.IsNullOrWhiteSpace(a.TextTitle)) dict["title"] = a.TextTitle;
        if (!string.IsNullOrWhiteSpace(a.TextTitleZh)) dict["title_zh"] = a.TextTitleZh;
        if (!string.IsNullOrWhiteSpace(a.Snippet)) dict["snippet"] = a.Snippet;
        return dict;
    }

    /// <summary>
    /// Loads the cached index, or null if not available. When
    /// <paramref name="parentRootForFreshness"/> is given, the cache is also refused
    /// as stale unless its recorded corpus stamp matches the live corpus (caches from
    /// older builds carry no stamp and are treated as stale) — audit P4.6: the index
    /// previously never noticed corpus changes.
    /// </summary>
    public async Task<MasterCorpusIndex?> TryLoadAsync(
        string cacheDir, CancellationToken ct = default, string? parentRootForFreshness = null)
    {
        var path = Path.Combine(cacheDir, CacheFileName);
        if (!File.Exists(path)) return null;

        try
        {
            var json = await File.ReadAllTextAsync(path, Encoding.UTF8, ct);
            var index = JsonSerializer.Deserialize<MasterCorpusIndex>(json, JsonOpts);
            if (index == null) return null;

            if (parentRootForFreshness != null)
            {
                var live = ComputeCorpusStamp(parentRootForFreshness);
                if (live != null && index.CorpusStamp != live)
                    return null; // stale (or unstamped legacy cache) → caller rebuilds
            }

            return index;
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

        // 1. Take a wide raw window so we can safely strip tags and orphan fragments at the edges.
        const int RawHalfWindow = 300;
        const int CleanHalfWindow = 40;

        int rawStart = Math.Max(0, idx - RawHalfWindow);
        int rawEnd = Math.Min(text.Length, idx + pattern.Length + RawHalfWindow);
        var raw = text[rawStart..rawEnd];

        // 2. Strip complete tags (use * to handle <> as well).
        var cleaned = System.Text.RegularExpressions.Regex.Replace(raw, "<[^>]*>", "");
        // 3. Strip a leading orphan tag fragment: anything up to the first '>' if no '<' precedes it.
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, "^[^<]*?>", "");
        // 4. Strip a trailing orphan tag fragment: anything from the last '<' if no '>' follows it.
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, "<[^>]*$", "");
        // 5. Collapse whitespace.
        cleaned = System.Text.RegularExpressions.Regex.Replace(cleaned, @"\s+", " ").Trim();

        if (cleaned.Length == 0) return null;

        // 6. Find the pattern in the cleaned text.
        int cleanIdx = cleaned.IndexOf(pattern, StringComparison.Ordinal);
        if (cleanIdx < 0)
        {
            // Pattern was lost during cleaning (e.g. it straddled a tag). Fall back to a centered slice.
            return cleaned.Length > 80 ? cleaned[..77] + "..." : cleaned;
        }

        // 7. Extract a tight window around the match in the cleaned text.
        int snippetStart = Math.Max(0, cleanIdx - CleanHalfWindow);
        int snippetEnd = Math.Min(cleaned.Length, cleanIdx + pattern.Length + CleanHalfWindow);
        var snippet = cleaned[snippetStart..snippetEnd];

        // 8. Add ellipsis to indicate truncation.
        if (snippetStart > 0) snippet = "..." + snippet;
        if (snippetEnd < cleaned.Length) snippet += "...";

        return snippet;
    }
}
