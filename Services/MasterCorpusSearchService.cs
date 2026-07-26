// Services/MasterCorpusSearchService.cs
// Scans the CBETA and OpenZen corpora for zen master name mentions.
// Builds a cached index of primary (author) and secondary (mentioned) appearances.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
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

    // ── Sharded manifest layout (GitHub's 50 MB single-file limit) ─────────────────────
    // The on-disk file <see cref="CacheFileName"/> is a small MANIFEST (metadata + a shard
    // list, no inline appearances); the appearances array lives across sibling shard files
    // each byte-budgeted well under the limit. Shard filenames are
    // "master-corpus-index.appearances.{i}.json". These consts are the single source of
    // truth for that naming (AppPaths mirrors the glob for bundle enumeration).
    internal const string ShardPrefix = "master-corpus-index.appearances.";
    internal const string ShardGlobPattern = "master-corpus-index.appearances.*.json";
    internal static string ShardFileName(int index) => $"{ShardPrefix}{index}.json";

    // Byte budget per shard: ~25 MB, so today's ~57 MB payload yields ~3 shards and no single
    // file ever approaches GitHub's 50 MB limit. Grows shard COUNT (never file size) as the
    // corpus grows.
    private const long ShardByteBudget = 25L * 1024 * 1024;

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
    /// Composite v2 CORPUS half of the freshness stamp over all discovered corpus dirs:
    /// <c>v2;corpus=files={N};bytes={SUM};pathsig={P16};titles={T16}</c>. Every input is
    /// path/size/content — NO mtime — so identical content yields an identical stamp on
    /// any machine or clone (mtime-immune; a git clone/pull that rewrites working-tree
    /// mtimes does NOT flip it, which is what lets a shipped bundle read fresh on a new
    /// install). Stat-only over corpus xml (Length, no content read); titles.jsonl is a
    /// small (~1.6 MB) metadata file that IS content-hashed (T16). Null when no corpus
    /// dirs exist. The ROSTER half is supplied separately by
    /// <see cref="ComputeRosterIdentity"/>; the two are joined with ';' to form the
    /// full stamp stored in <see cref="MasterCorpusIndex.CorpusStamp"/>. See SPEC §1.2.
    /// </summary>
    public static string? ComputeCorpusStamp(string parentRoot)
    {
        var corpusDirs = DiscoverCorpusDirs(parentRoot);
        if (corpusDirs.Count == 0) return null;

        int files = 0;
        long totalBytes = 0;
        var pathLines = new List<string>();
        foreach (var (_, dir) in corpusDirs)
        {
            try
            {
                foreach (var f in Directory.EnumerateFiles(dir, "*.xml", SearchOption.AllDirectories))
                {
                    long size;
                    try { size = new FileInfo(f).Length; }
                    catch { continue; /* vanished between enumerate and stat */ }
                    files++;
                    totalBytes += size;
                    var relKey = Path.GetRelativePath(dir, f).Replace('\\', '/');
                    pathLines.Add($"{relKey}:{size}\n");
                }
            }
            catch { /* unreadable dir → reflected by lower count; stamp still differs */ }
        }

        // P16: catches renames/moves and same-total redistributions that files+bytes miss.
        pathLines.Sort(StringComparer.Ordinal);
        var pathSig = Sha256Hex16(string.Concat(pathLines));

        // T16: displayed titles come from titles.jsonl (not xml, not roster) — a title
        // edit must invalidate the cache (same class as the 279 stale-cache bug).
        var titlesHash = ComputeTitlesHash16(parentRoot);

        return $"v2;corpus=files={files};bytes={totalBytes};pathsig={pathSig};titles={titlesHash}";
    }

    /// <summary>
    /// ROSTER half of the composite stamp, derived from the MERGED catalog (base roster +
    /// per-user community overlay) so a community edit cannot serve a stale index:
    /// <c>roster=count={M};hash={R16}</c>. R16 = first 16 hex of SHA256 over sorted
    /// per-record lines <c>"{CanonicalName}|{sorted aliases}|{primary variant dates}"</c>,
    /// so it flips on any add/rename/alias/date change but is stable under record reorder.
    /// See SPEC §1.2.
    /// </summary>
    public static string ComputeRosterIdentity(ZenMasterCatalog catalog)
    {
        int count = catalog.Records.Count;
        var lines = new List<string>(count);
        foreach (var record in catalog.Records)
        {
            var aliasPart = string.Join(",", record.Aliases.OrderBy(a => a, StringComparer.Ordinal));
            var pv = record.PrimaryVariant;
            var dates = pv != null ? $"{pv.Floruit}-{pv.Death}" : "-";
            lines.Add($"{record.CanonicalName}|{aliasPart}|{dates}\n");
        }
        lines.Sort(StringComparer.Ordinal);
        var hash = Sha256Hex16(string.Concat(lines));
        return $"roster=count={count};hash={hash}";
    }

    /// <summary>
    /// Full composite v2 stamp = corpus half + ';' + roster half, or null when no corpus
    /// dirs exist (preserving the legacy "no corpus ⇒ null ⇒ freshness not enforced"
    /// behavior). Stored in <see cref="MasterCorpusIndex.CorpusStamp"/> at build time and
    /// recomputed live for the freshness comparison in <see cref="TryLoadAsync"/>.
    /// </summary>
    public static string? ComputeCompositeStamp(string parentRoot, ZenMasterCatalog catalog)
    {
        var corpusStamp = ComputeCorpusStamp(parentRoot);
        if (corpusStamp == null) return null;
        return $"{corpusStamp};{ComputeRosterIdentity(catalog)}";
    }

    /// <summary>
    /// T16: first 16 hex chars of SHA256 over the concatenated bytes of every discovered
    /// corpus's <c>titles.jsonl</c>, ordinal-sorted by path (an absent/unreadable file
    /// contributes zero bytes). Deduped so a shared translations root is not double-hashed.
    /// </summary>
    private static string ComputeTitlesHash16(string parentRoot)
    {
        var titlePaths = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var layout in AppPaths.DiscoverAllCorpora(parentRoot))
        {
            if (!string.IsNullOrEmpty(layout.TranslationsRepoRoot))
                titlePaths.Add(Path.Combine(layout.TranslationsRepoRoot, "titles.jsonl"));
        }

        using var ih = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach (var p in titlePaths)
        {
            try
            {
                if (File.Exists(p))
                    ih.AppendData(File.ReadAllBytes(p));
            }
            catch { /* unreadable → treated as absent (zero bytes contributed) */ }
        }
        var hash = ih.GetHashAndReset();
        return Convert.ToHexString(hash).ToLowerInvariant()[..16];
    }

    private static string Sha256Hex16(string s)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(s));
        return Convert.ToHexString(hash).ToLowerInvariant()[..16];
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
            CorpusStamp = ComputeCompositeStamp(parentRoot, catalog),
        };

        int totalFiles = 0;
        foreach (var (label, dir) in corpusDirs)
        {
            var index = await BuildIndexAsync(dir, label, masters, progress, ct);
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

        // PR-M1: titles are NOT baked into the appearance records; populate the in-memory
        // TextTitle/TextTitleZh via the SAME load-time join TryLoadAsync uses, so the freshly
        // built index feeds the SPA export and desktop UI with titles while the shards that
        // SaveAsync writes stay title-free (byte-stable across a title-only edit).
        JoinTitles(combined, parentRoot);

        return combined;
    }

    /// <summary>
    /// PR-M1 title-source join: merges every discovered corpus's <c>titles.jsonl</c> into one
    /// map keyed by the '/'-normalized rel path (the same key the appearance records carry).
    /// A shared translations root contributes once; a later corpus wins on a key collision
    /// (rel-path namespaces are disjoint in practice, so this is not reached for the shipped
    /// single-corpus asset).
    /// </summary>
    public static Dictionary<string, (string? Zh, string? En)> LoadAllTitles(string parentRoot)
    {
        var merged = new Dictionary<string, (string? Zh, string? En)>(StringComparer.Ordinal);
        var seenRoots = new HashSet<string>(StringComparer.Ordinal);
        foreach (var layout in AppPaths.DiscoverAllCorpora(parentRoot))
        {
            if (string.IsNullOrEmpty(layout.TranslationsRepoRoot)) continue;
            if (!seenRoots.Add(layout.TranslationsRepoRoot)) continue;
            foreach (var kv in LoadTitles(layout.TranslationsRepoRoot))
                merged[kv.Key] = kv.Value;
        }
        return merged;
    }

    /// <summary>
    /// PR-M1: populates each appearance's in-memory <see cref="MasterTextAppearance.TextTitle"/>/
    /// <see cref="MasterTextAppearance.TextTitleZh"/> from the live title source, keyed by rel
    /// path. An appearance whose rel path is absent from the title source keeps null titles
    /// (graceful — the rel path remains as the stable identity, no crash). A no-op when
    /// <paramref name="parentRoot"/> is null/empty.
    /// </summary>
    public static void JoinTitles(MasterCorpusIndex index, string? parentRoot)
    {
        if (string.IsNullOrEmpty(parentRoot)) return;
        var titles = LoadAllTitles(parentRoot);
        if (titles.Count == 0) return;
        foreach (var a in index.Appearances)
        {
            var key = a.RelPath.Replace('\\', '/');
            if (titles.TryGetValue(key, out var t))
            {
                a.TextTitle = t.En;
                a.TextTitleZh = t.Zh;
            }
            else
            {
                a.TextTitle = null;
                a.TextTitleZh = null;
            }
        }
    }

    // PR-M1: a legacy (pre-M1) v2 stamp embeds a "titles=<T16>" component; the M1 stamp keeps
    // it too (for drift DETECTION — ComputeCorpusStamp still flips on a title edit), but the
    // freshness/adoption DECISION ignores it: a title-only difference must NOT force an
    // appearance rebuild (titles are re-joined at load instead). Stripping the token from both
    // sides before comparison is what makes a title edit zero-rebuild while a committed
    // title-embedded bundle still matches a fresh install's stamp.
    private static readonly System.Text.RegularExpressions.Regex TitlesTokenRegex =
        new(@";titles=[0-9a-f]{16}",
            System.Text.RegularExpressions.RegexOptions.Compiled
            | System.Text.RegularExpressions.RegexOptions.CultureInvariant);

    internal static string? StripTitlesToken(string? stamp)
        => stamp == null ? null : TitlesTokenRegex.Replace(stamp, "");

    /// <summary>
    /// Builds the master corpus index by scanning all XML files in the given directory.
    /// This is CPU-intensive and should run in the background.
    /// </summary>
    public async Task<MasterCorpusIndex> BuildIndexAsync(
        string originalDir,
        string corpus,
        List<(string CanonicalName, List<string> ChineseNames)> masters,
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

        // An alias held by more than one distinct master is ambiguous: a bare epithet like 弘覺禪師 or
        // a common dharma name like 智通 is shared by several masters. Matching on it would misattribute
        // mentions (or double-count them across masters), so exclude ambiguous aliases and match each
        // master only on the names unique to it. A master whose specific names remain still resolves;
        // one left with nothing but shared aliases correctly gets no (unattributable) appearances.
        var aliasOwners = new Dictionary<string, HashSet<string>>(StringComparer.Ordinal);
        foreach (var (canonicalName, chineseNames) in masters)
            foreach (var cn in chineseNames)
                if (cn.Length >= MinNameLength)
                {
                    if (!aliasOwners.TryGetValue(cn, out var owners))
                        aliasOwners[cn] = owners = new HashSet<string>(StringComparer.Ordinal);
                    owners.Add(canonicalName);
                }
        bool IsUnambiguous(string alias) =>
            aliasOwners.TryGetValue(alias, out var owners) && owners.Count == 1;

        // Build search patterns: canonical name -> list of Chinese names (sorted longest first)
        var searchPatterns = new List<(string CanonicalName, string ChineseName)>();
        foreach (var (canonicalName, chineseNames) in masters)
        {
            foreach (var cn in chineseNames.OrderByDescending(n => n.Length))
            {
                if (cn.Length >= MinNameLength && IsUnambiguous(cn))
                    searchPatterns.Add((canonicalName, cn));
            }
        }

        // Build lookup of canonicalName -> all chinese names, used for concept-name disambiguation.
        var namesByCanonical = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var (canonicalName, chineseNames) in masters)
        {
            if (!namesByCanonical.ContainsKey(canonicalName))
                namesByCanonical[canonicalName] = chineseNames
                    .Where(n => n.Length >= MinNameLength && IsUnambiguous(n))
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

                    // PR-M1: rel path is the stable identity; text titles are joined at LOAD
                    // time (JoinTitles), not baked here. relKey stays for the ManualPrimary
                    // /-normalized prefix match below.
                    var relKey = relPath.Replace('\\', '/');

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

                    // Count occurrences and snippet over a DISPLAY-SCOPED copy that strips exactly
                    // what the reader's TEI parser (SPA lib/tei.js, desktop preview) suppresses:
                    // <teiHeader>, <note> (incl. place="inline"), <cb:mulu> (a nav duplicate of <head>),
                    // and <rdg> variant readings. Without this the baked mention count exceeds the
                    // passages the reader can actually surface (e.g. a mulu+head pair counts 2, shows 1).
                    // The raw `content`/`header` below are still used for author-field/primary detection.
                    var displayContent = BuildDisplayContent(content);

                    // Search for each master's Chinese names
                    var foundMasters = new Dictionary<string, (string MatchedName, int Count, string? Snippet, bool IsPrimary)>();

                    foreach (var (canonicalName, chineseName) in searchPatterns)
                    {
                        if (foundMasters.ContainsKey(canonicalName)) continue; // already found by a longer name

                        int count = CountOccurrences(displayContent, chineseName);
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

                        // Extract a snippet around the first body occurrence (display-scoped, header already stripped)
                        string? snippet = ExtractSnippet(displayContent, chineseName, 0);

                        foundMasters[canonicalName] = (chineseName, count, snippet, isPrimary);
                    }

                    foreach (var (masterName, (matchedName, count, snippet, isPrimary)) in foundMasters)
                    {
                        allAppearances.Add(new MasterTextAppearance
                        {
                            MasterName = masterName,
                            MatchedName = matchedName,
                            RelPath = relPath,
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

    /// <summary>
    /// Saves the index as a small MANIFEST (<see cref="CacheFileName"/>) plus byte-budgeted
    /// <c>appearances</c> shard files, so no single file ever nears GitHub's 50 MB limit
    /// (SHARD_MASTER_INDEX). The manifest keeps ALL top-level metadata
    /// (built_utc/corpus/corpus_stamp/file_count/master_count) — corpus_stamp stays the 3rd
    /// property so <see cref="ReadCorpusStampCheap"/> reads it without touching a shard — and
    /// adds <c>appearance_shards</c> + the ordered <c>shards</c> filename list. Each shard is a
    /// JSON array of a contiguous run of appearances (records byte-identical to the inline form).
    /// Any stale shard files from a prior, larger save are removed so the on-disk set matches
    /// the manifest exactly. Used by BOTH the app's local rebuild and the CI/asset bake.
    /// </summary>
    public async Task SaveAsync(string cacheDir, MasterCorpusIndex index, CancellationToken ct = default)
    {
        Directory.CreateDirectory(cacheDir);

        // 1. Serialize each appearance once, grouping into shards by a UTF-8 byte budget.
        var shardBodies = new List<string>();           // each shard's inner "rec,\nrec,\n..." body
        var current = new StringBuilder();
        long currentBytes = 0;
        int currentCount = 0;
        foreach (var appearance in index.Appearances)
        {
            ct.ThrowIfCancellationRequested();
            var rec = JsonSerializer.Serialize(appearance, JsonOpts);
            long recBytes = Encoding.UTF8.GetByteCount(rec) + 2; // + ",\n" separator

            if (currentCount > 0 && currentBytes + recBytes > ShardByteBudget)
            {
                shardBodies.Add(current.ToString());
                current.Clear();
                currentBytes = 0;
                currentCount = 0;
            }

            if (currentCount > 0) current.Append(",\n");
            current.Append(rec);
            currentBytes += recBytes;
            currentCount++;
        }
        if (currentCount > 0) shardBodies.Add(current.ToString());

        // 2. Write the shard files (array of appearances each). Manifest is written LAST so a
        //    crash mid-write never leaves a manifest pointing at an incomplete shard set.
        var shardNames = new List<string>(shardBodies.Count);
        for (int i = 0; i < shardBodies.Count; i++)
        {
            var name = ShardFileName(i);
            shardNames.Add(name);
            var shardJson = "[\n" + shardBodies[i] + "\n]";
            await File.WriteAllTextAsync(Path.Combine(cacheDir, name), shardJson, new UTF8Encoding(false), ct);
        }

        // 3. Manifest: all metadata + shard list, NO inline appearances. corpus_stamp stays 3rd.
        var manifest = new
        {
            built_utc = index.BuiltUtc,
            corpus = index.Corpus,
            corpus_stamp = index.CorpusStamp,
            file_count = index.FileCount,
            master_count = index.MasterCount,
            appearance_shards = shardNames.Count,
            shards = shardNames,
        };
        var manifestJson = JsonSerializer.Serialize(manifest, JsonOpts);
        await File.WriteAllTextAsync(Path.Combine(cacheDir, CacheFileName), manifestJson, new UTF8Encoding(false), ct);

        // 4. Drop any stale higher-index shard files left by a previous, larger save so the
        //    on-disk shard set matches the manifest exactly (no orphan cruft, no confusion).
        foreach (var stale in EnumerateShardFiles(cacheDir))
        {
            var fn = Path.GetFileName(stale);
            if (!shardNames.Contains(fn, StringComparer.Ordinal))
                try { File.Delete(stale); } catch { /* best-effort */ }
        }
    }

    /// <summary>Enumerates the appearance shard files (siblings of the manifest) in a dir.</summary>
    private static IEnumerable<string> EnumerateShardFiles(string dir)
    {
        if (!Directory.Exists(dir)) return Array.Empty<string>();
        try { return Directory.EnumerateFiles(dir, ShardGlobPattern).ToList(); }
        catch { return Array.Empty<string>(); }
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
            var shardParent = Path.GetDirectoryName(shardPath);
            if (!string.IsNullOrEmpty(shardParent)) Directory.CreateDirectory(shardParent); // defensive
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
               .Replace("/", "")       // strip path separators: a name like "A / B" must not
               .Replace("\\", "")      // slug into a nested shard path (DirectoryNotFound on export)
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
    /// <paramref name="parentRootForFreshness"/> is given, the cache is refused as stale
    /// unless its recorded composite v2 stamp matches the live corpus+roster (SPEC §1.2):
    /// the CORPUS half is recomputed here (files/bytes/pathsig/titles), the ROSTER half is
    /// supplied precomputed via <paramref name="rosterIdentity"/> (the caller already holds
    /// the merged catalog). A legacy v1 stamp, a corpus change, a titles edit, or a roster
    /// edit therefore all come back null → caller rebuilds (fixes the "279 of 944"
    /// stale-cache class, and now also catches title/roster edits).
    /// </summary>
    public async Task<MasterCorpusIndex?> TryLoadAsync(
        string cacheDir, CancellationToken ct = default,
        string? parentRootForFreshness = null, string? rosterIdentity = null,
        string? parentRootForTitles = null)
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
                var corpusStamp = ComputeCorpusStamp(parentRootForFreshness);
                // Null corpus stamp (no corpus dirs) ⇒ freshness not enforced, as before.
                if (corpusStamp != null)
                {
                    var live = $"{corpusStamp};{rosterIdentity}";
                    // PR-M1: compare with the titles component stripped from BOTH sides — a
                    // title-only edit re-joins at load (below) instead of forcing a rebuild.
                    // A corpus or roster change still flips the stripped stamp → null → rebuild.
                    if (StripTitlesToken(index.CorpusStamp) != StripTitlesToken(live))
                        return null; // stale (or unstamped/v1 legacy cache) → caller rebuilds
                }
            }

            // Sharded manifest: concatenate all shard arrays into the appearances list. A
            // missing/corrupt shard makes the whole cache unusable → null → caller rebuilds.
            // Legacy single-file caches (no shards list, inline appearances) fall through
            // unchanged. Done AFTER the freshness gate so a stale cache never reads its shards.
            if (index.Shards is { Count: > 0 })
            {
                var all = new List<MasterTextAppearance>();
                foreach (var shardName in index.Shards)
                {
                    var shardPath = Path.Combine(cacheDir, shardName);
                    if (!File.Exists(shardPath)) return null;
                    var shardJson = await File.ReadAllTextAsync(shardPath, Encoding.UTF8, ct);
                    var recs = JsonSerializer.Deserialize<List<MasterTextAppearance>>(shardJson, JsonOpts);
                    if (recs == null) return null;
                    all.AddRange(recs);
                }
                index.Appearances = all;
            }

            // PR-M1: titles live in titles.jsonl, not the shards — join them onto the loaded
            // records so every consumer (desktop appearance list, co-occurrence, SPA export)
            // sees current titles with zero rebuild. Uses the titles root if given, else the
            // freshness root (MainWindow supplies the latter). Null both ⇒ display-only /
            // freshness-off call sites keep loading title-free, no crash.
            JoinTitles(index, parentRootForTitles ?? parentRootForFreshness);

            return index;
        }
        catch { return null; }
    }

    /// <summary>
    /// Adopts the shipped exe-adjacent master-corpus bundle into the on-disk cache when it
    /// IS the live index and the local cache is absent or stale, so the immediately
    /// following <see cref="TryLoadAsync"/> serves it with ZERO rebuild (SPEC §2.3).
    ///
    /// Decision (adopt ⇔ ALL hold): a non-empty <paramref name="liveCompositeStamp"/> is
    /// supplied; the bundle exists and its <c>corpus_stamp</c> == live; AND the local cache
    /// is absent OR its stamp ≠ live. A stamp read is done CHEAPLY via a bounded-prefix
    /// <see cref="Utf8JsonReader"/> scan — never a full ~57 MB deserialize. When the bundle
    /// stamp differs from live (diverged corpus/roster/titles), or the bundle is absent or
    /// corrupt, this is a no-op returning false and the caller's build-and-save fallback
    /// (rows 3/6) runs unchanged. The copy is atomic (tmp in the cache dir + rename), and no
    /// re-homing is needed because the cache embeds no absolute root path (contrast search).
    /// Returns true only when a copy was actually performed.
    /// </summary>
    public async Task<bool> TryAdoptBundleAsync(
        string cacheDir, string bundlePath, string? liveCompositeStamp,
        CancellationToken ct = default)
    {
        // No live stamp (e.g. no corpus dirs ⇒ ComputeCorpusStamp null) ⇒ nothing meaningful
        // to match the bundle against; never adopt (mirrors TryLoadAsync's "freshness not
        // enforced" branch, and a real bundle's v2 stamp could never equal null anyway).
        if (string.IsNullOrEmpty(liveCompositeStamp)) return false;
        if (string.IsNullOrEmpty(bundlePath) || !File.Exists(bundlePath)) return false;

        // Adopt only when the bundle IS the live index. Cheap stamp-only read. PR-M1: compare
        // with the titles component stripped so a committed title-embedded bundle still adopts
        // against a fresh install whose live stamp carries a different (or, later, absent)
        // titles token — titles are re-joined at load, never a reason to rebuild/re-bake.
        var bundleStamp = ReadCorpusStampCheap(bundlePath);
        if (StripTitlesToken(bundleStamp) != StripTitlesToken(liveCompositeStamp)) return false;

        // Local cache already fresh (== live, titles aside) ⇒ keep it, no copy (row 1).
        var cachePath = Path.Combine(cacheDir, CacheFileName);
        var localStamp = ReadCorpusStampCheap(cachePath); // null when absent/corrupt/unstamped
        if (StripTitlesToken(localStamp) == StripTitlesToken(liveCompositeStamp)) return false;

        // Local absent or stale AND bundle == live ⇒ adopt the manifest + ALL its shard files.
        // Sharded bundles (today's asset) ship the manifest next to its
        // "master-corpus-index.appearances.*.json" siblings; a legacy single-file bundle has no
        // siblings and this degrades to exactly the old one-file copy. Shard discovery is a
        // sibling GLOB (never a full deserialize — the malformed-tail/large-bundle guarantee),
        // so a stamp-matching bundle with a broken appearances tail still adopts by byte copy.
        var bundleDir = Path.GetDirectoryName(bundlePath) ?? "";
        var shardSrcs = EnumerateShardFiles(bundleDir);
        var shardNames = new HashSet<string>(StringComparer.Ordinal);

        var staged = new List<(string Tmp, string Dst)>(); // (tmp, final) pairs, shards then manifest
        var tmpSuffix = ".tmp-" + Guid.NewGuid().ToString("N");
        try
        {
            Directory.CreateDirectory(cacheDir);

            // Stage every shard, then the manifest, into sibling tmp files (same volume as the
            // finals ⇒ each rename is atomic).
            foreach (var shardSrc in shardSrcs)
            {
                var name = Path.GetFileName(shardSrc);
                shardNames.Add(name);
                var dst = Path.Combine(cacheDir, name);
                var tmp = dst + tmpSuffix;
                await CopyFileAsync(shardSrc, tmp, ct);
                staged.Add((tmp, dst));
            }
            var manifestTmp = cachePath + tmpSuffix;
            await CopyFileAsync(bundlePath, manifestTmp, ct);

            // Rename shards FIRST, manifest LAST: TryLoadAsync keys on the manifest, so it only
            // ever observes a complete shard set (a mid-sequence failure leaves the old/absent
            // manifest, which never reads as fresh).
            foreach (var (tmp, dst) in staged)
                File.Move(tmp, dst, overwrite: true);
            File.Move(manifestTmp, cachePath, overwrite: true);
            staged.Clear();

            // Drop any stale local shard files the adopted manifest does not reference (e.g. a
            // prior larger adopt, or adopting a legacy single-file bundle over a sharded cache).
            foreach (var stale in EnumerateShardFiles(cacheDir))
            {
                var fn = Path.GetFileName(stale);
                if (!shardNames.Contains(fn))
                    try { File.Delete(stale); } catch { /* best-effort */ }
            }
            return true;
        }
        catch
        {
            // Copy/rename failure (or cancellation) leaves the cache untouched; best-effort
            // remove every staged tmp so the fallback rebuild starts clean.
            foreach (var (tmp, _) in staged)
                try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
            try { var mt = cachePath + tmpSuffix; if (File.Exists(mt)) File.Delete(mt); } catch { }
            return false;
        }
    }

    /// <summary>Copies a file via a cancellable stream into a fresh tmp path.</summary>
    private static async Task CopyFileAsync(string src, string tmp, CancellationToken ct)
    {
        using var s = new FileStream(src, FileMode.Open, FileAccess.Read, FileShare.Read);
        using var d = new FileStream(tmp, FileMode.CreateNew, FileAccess.Write, FileShare.None);
        await s.CopyToAsync(d, ct);
    }

    /// <summary>
    /// Reads ONLY the top-level <c>corpus_stamp</c> string from a MasterCorpusIndex JSON
    /// file without deserializing the (up to ~57 MB) <c>appearances</c> array. The stamp is
    /// the 3rd property, so a bounded-prefix read + <see cref="Utf8JsonReader"/> scan finds
    /// it well before the array begins. Returns null when the file is absent, unreadable,
    /// unstamped, or the stamp is not a string.
    /// </summary>
    private static string? ReadCorpusStampCheap(string path)
    {
        try
        {
            if (!File.Exists(path)) return null;

            const int PrefixBytes = 256 * 1024;
            var buffer = new byte[PrefixBytes];
            int total = 0;
            using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
            {
                int n;
                while (total < buffer.Length && (n = fs.Read(buffer, total, buffer.Length - total)) > 0)
                    total += n;
            }

            // isFinalBlock only when we consumed the whole (small) file; otherwise a truncated
            // trailing token at the prefix edge is tolerated (Read() returns false, we stop).
            bool isFinal = total < buffer.Length;
            var reader = new Utf8JsonReader(
                new ReadOnlySpan<byte>(buffer, 0, total), isFinalBlock: isFinal, state: default);

            int depth = 0;
            while (reader.Read())
            {
                switch (reader.TokenType)
                {
                    case JsonTokenType.StartObject:
                    case JsonTokenType.StartArray:
                        depth++;
                        break;
                    case JsonTokenType.EndObject:
                    case JsonTokenType.EndArray:
                        depth--;
                        break;
                    case JsonTokenType.PropertyName:
                        if (depth == 1 && reader.ValueTextEquals("corpus_stamp"))
                        {
                            if (!reader.Read()) return null;
                            return reader.TokenType == JsonTokenType.String ? reader.GetString() : null;
                        }
                        break;
                }
            }
            return null;
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

    /// <summary>Gets top co-occurring masters for a specific master — the masters that
    /// share the most texts with this one.</summary>
    /// <remarks>
    /// Computed DIRECTLY for the single requested master (scanning only the files it
    /// appears in), NOT by materializing <see cref="GetCoOccurrenceMatrix"/> for the
    /// whole corpus. The full matrix is O(Σ masters-per-file²) across every text, and it
    /// was being rebuilt from scratch on every master selection — a pure waste (the index
    /// never changes) that froze the UI for many seconds on a full corpus index whenever a
    /// master was picked (including a lineage-chart node click, which mirrors its selection
    /// onto <c>SelectedMaster</c>). This variant is O(appearances) and returns an identical
    /// top-N.
    /// </remarks>
    public static List<(string MasterName, int SharedTexts)> GetTopCoOccurrences(
        MasterCorpusIndex index, string masterName, int limit = 10)
    {
        // 1. The set of files in which THIS master appears.
        var myFiles = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var a in index.Appearances)
            if (string.Equals(a.MasterName, masterName, StringComparison.OrdinalIgnoreCase))
                myFiles.Add(a.RelPath);
        if (myFiles.Count == 0) return new();

        // 2. For each such file, the OTHER distinct masters that co-appear there.
        var perFile = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        foreach (var a in index.Appearances)
        {
            if (!myFiles.Contains(a.RelPath)) continue;
            if (string.Equals(a.MasterName, masterName, StringComparison.OrdinalIgnoreCase)) continue;
            if (!perFile.TryGetValue(a.RelPath, out var set))
                perFile[a.RelPath] = set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            set.Add(a.MasterName);
        }

        // 3. Shared-text count per peer = number of those files it also appears in
        //    (parity with matrix[masterName][peer]).
        var peers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var set in perFile.Values)
            foreach (var peer in set)
            {
                peers.TryGetValue(peer, out var count);
                peers[peer] = count + 1;
            }

        return peers
            .OrderByDescending(kv => kv.Value)
            .Take(limit)
            .Select(kv => (kv.Key, kv.Value))
            .ToList();
    }

    // Elements the reader's TEI parser suppresses, so counting/snippeting over what remains matches
    // the passages the SPA and desktop reader actually display. Non-greedy, DOTALL; approximates the
    // DOM parse closely enough for name-occurrence counting (names never straddle these boundaries).
    private static readonly System.Text.RegularExpressions.Regex NonDisplayRegex =
        new System.Text.RegularExpressions.Regex(
            @"<teiHeader\b[^>]*>.*?</teiHeader>|<note\b[^>]*>.*?</note>|<(?:cb:)?mulu\b[^>]*>.*?</(?:cb:)?mulu>|<rdg\b[^>]*>.*?</rdg>",
            System.Text.RegularExpressions.RegexOptions.Singleline | System.Text.RegularExpressions.RegexOptions.Compiled);

    /// <summary>Strip teiHeader/note/cb:mulu/rdg so baked counts and snippets match what the reader can display.</summary>
    internal static string BuildDisplayContent(string content) => NonDisplayRegex.Replace(content, "");

    internal static int CountOccurrences(string text, string pattern)
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
