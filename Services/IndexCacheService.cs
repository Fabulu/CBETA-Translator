using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ReadZen.App.Models;

namespace ReadZen.App.Services;

public sealed class IndexCacheService : IIndexCacheService
{
    private readonly ITranslationStatusService _statusService;

    public IndexCacheService(ITranslationStatusService statusService)
    {
        _statusService = statusService ?? throw new ArgumentNullException(nameof(statusService));
    }

    private const string CacheFileName = "index.cache.json";

    // On-disk schema version. Bumping forces one full rescan on upgrade
    // (older caches lack the v4 per-entry stat fields + TitlesHash).
    private const int CacheVersion = 4;

    // Bump this string whenever you want to force rebuild even if cache exists.
    // (Useful when you change status logic and want to ensure the cache isn't stale.)
    private const string CacheBuildGuid = "phase4-nav-v5-content-gate";

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true
    };

    private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);

    public string GetCachePath(string root)
        => Path.Combine(root, CacheFileName);

    public async Task<IndexCache?> TryLoadAsync(string root)
    {
        try
        {
            var path = GetCachePath(root);
            if (!File.Exists(path))
                return null;

            var json = await File.ReadAllTextAsync(path);
            if (string.IsNullOrWhiteSpace(json))
                return null;

            var cache = JsonSerializer.Deserialize<IndexCache>(json, JsonOpts);
            if (cache == null)
                return null;

            if (!string.Equals(Path.GetFullPath(cache.RootPath), Path.GetFullPath(root), StringComparison.OrdinalIgnoreCase))
                return null;

            // Reject empty caches
            if (cache.Entries == null || cache.Entries.Count == 0)
                return null;

            // Version gate — pre-v4 caches lack the per-entry stat fields and
            // TitlesHash the content gate needs, so they are rebuilt once.
            if (cache.Version < CacheVersion)
                return null;

            if (!string.Equals(cache.BuildGuid, CacheBuildGuid, StringComparison.Ordinal))
                return null;

            // NOTE: the git-HEAD freshness gate was removed in v4. Freshness is
            // now decided by the content gate in RefreshAsync (TitlesHash +
            // per-entry file stats), which recomputes only genuinely-changed
            // entries instead of discarding the whole cache on any commit.
            return cache;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Reads the current HEAD commit SHA of the git repo rooted at
    /// <paramref name="repoRoot"/>, without invoking the git binary.
    /// Handles the three on-disk forms libgit2 / git itself can produce:
    ///   1. <c>.git/HEAD</c> contains a literal SHA  (detached HEAD)
    ///   2. <c>.git/HEAD</c> contains <c>ref: refs/heads/{name}</c>
    ///      and the resolved file <c>.git/{ref}</c> exists
    ///   3. The ref is packed in <c>.git/packed-refs</c>
    /// Returns null on any failure (no .git dir, malformed file, race).
    /// Cheap enough to call on every cache load.
    /// </summary>
    public static string? TryGetGitHead(string repoRoot)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(repoRoot)) return null;
            var gitDir = Path.Combine(repoRoot, ".git");
            if (!Directory.Exists(gitDir)) return null;

            var headFile = Path.Combine(gitDir, "HEAD");
            if (!File.Exists(headFile)) return null;

            var head = File.ReadAllText(headFile).Trim();
            if (string.IsNullOrEmpty(head)) return null;

            // Detached HEAD: HEAD contains the SHA directly.
            if (!head.StartsWith("ref:", StringComparison.Ordinal))
                return head;

            // Symbolic ref: resolve to the underlying ref file.
            var refPath = head.Substring(4).Trim();
            if (string.IsNullOrEmpty(refPath)) return null;

            var refFile = Path.Combine(gitDir, refPath.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(refFile))
            {
                var sha = File.ReadAllText(refFile).Trim();
                if (!string.IsNullOrEmpty(sha)) return sha;
            }

            // Packed-refs fallback: scan for the matching ref line.
            var packedRefs = Path.Combine(gitDir, "packed-refs");
            if (File.Exists(packedRefs))
            {
                foreach (var line in File.ReadLines(packedRefs))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    if (line.StartsWith("#", StringComparison.Ordinal)) continue;
                    if (line.StartsWith("^", StringComparison.Ordinal)) continue;
                    var sp = line.IndexOf(' ');
                    if (sp <= 0 || sp >= line.Length - 1) continue;
                    var name = line.Substring(sp + 1).Trim();
                    if (string.Equals(name, refPath, StringComparison.Ordinal))
                    {
                        var sha = line.Substring(0, sp).Trim();
                        if (!string.IsNullOrEmpty(sha)) return sha;
                    }
                }
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    public async Task SaveAsync(string root, IndexCache cache)
    {
        cache.RootPath = root;
        cache.BuiltUtc = DateTime.UtcNow;
        cache.Version = CacheVersion;
        cache.BuildGuid = CacheBuildGuid;
        cache.TitlesHash = ComputeTitlesHash(root);

        var path = GetCachePath(root);
        var json = JsonSerializer.Serialize(cache, JsonOpts);
        await File.WriteAllTextAsync(path, json, Utf8NoBom);
    }

    /// <summary>
    /// SHA256 (hex) of the shipped <c>titles.jsonl</c> bytes — the single content
    /// input (beyond per-entry file stats) the nav cache depends on. Returns a
    /// stable sentinel when the file is absent or unreadable so that its
    /// appearance/disappearance flips the hash and forces a rebuild. Hashing
    /// ~1.6 MB once per launch is cheap and explicitly sanctioned (SPEC §10).
    /// </summary>
    private static string ComputeTitlesHash(string root)
    {
        try
        {
            var titlesPath = Path.Combine(root, "titles.jsonl");
            if (!File.Exists(titlesPath))
                return "no-titles";
            using var fs = File.OpenRead(titlesPath);
            var hash = SHA256.HashData(fs);
            return Convert.ToHexString(hash);
        }
        catch
        {
            return "titles-error";
        }
    }

    // ----------------------------
    // Phase 2: titles + status
    // ----------------------------

    private sealed class TitleInfo
    {
        public string? Zh { get; set; }
        public string? En { get; set; }
        public string? EnShort { get; set; }
    }

    private static string NormalizePathKey(string p)
        => (p ?? "").Replace('\\', '/').TrimStart('/');

    private static Dictionary<string, TitleInfo> LoadTitlesMap(string root)
    {
        var titlesPath = Path.Combine(root, "titles.jsonl");
        var dict = new Dictionary<string, TitleInfo>(StringComparer.OrdinalIgnoreCase);

        if (!File.Exists(titlesPath))
            return dict;

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

                var key = NormalizePathKey(path);

                string? zh = r.TryGetProperty("zh", out var zhEl) ? zhEl.GetString() : null;
                string? en = r.TryGetProperty("en", out var enEl) ? enEl.GetString() : null;
                string? enShort = r.TryGetProperty("enShort", out var esEl) ? esEl.GetString() : null;

                dict[key] = new TitleInfo { Zh = zh, En = en, EnShort = enShort };
            }
            catch
            {
                // ignore bad lines
            }
        }

        return dict;
    }

    public TranslationStatus ComputeStatusForPairLive(
        string origAbs,
        string tranAbs,
        string rootForLogs,
        string relKeyForLogs,
        bool verboseLog = true)
    {
        return _statusService.ComputeStatusForPairLive(origAbs, tranAbs, rootForLogs, relKeyForLogs, verboseLog);
    }

    /// <summary>
    /// Resolves the translated file for <paramref name="rel"/>: the canonical
    /// xml-p5t path when it exists, otherwise the first matching
    /// community/translations/{user}/ file, otherwise the canonical (possibly
    /// non-existent) path. Shared verbatim by build and incremental refresh so
    /// both agree on which file the status is computed from (OpenZen fallback:
    /// xml-open-t/ empty but community/translations/{user}/ has files).
    /// </summary>
    private static string ResolveTranslatedPath(string root, string translatedDir, string rel)
    {
        var tranAbs = Path.Combine(translatedDir, rel);
        if (File.Exists(tranAbs))
            return tranAbs;

        var communityTransDir = Path.Combine(root, "community", "translations");
        if (Directory.Exists(communityTransDir))
        {
            foreach (var userDir in Directory.GetDirectories(communityTransDir))
            {
                var communityPath = Path.Combine(userDir, rel);
                if (File.Exists(communityPath))
                    return communityPath;
            }
        }

        return tranAbs;
    }

    /// <summary>
    /// Builds a fresh <see cref="FileNavItem"/> for one original file: display
    /// fields from <paramref name="titles"/>, resolved translation + status, and
    /// the full per-entry stat set the content gate compares on the next launch.
    /// </summary>
    private FileNavItem BuildEntry(
        string origAbs,
        string originalDir,
        string translatedDir,
        string root,
        Dictionary<string, TitleInfo> titles)
    {
        var rel = Path.GetRelativePath(originalDir, origAbs);
        var relKey = NormalizePathKey(rel);
        var fileName = Path.GetFileName(rel);

        titles.TryGetValue(relKey, out var ti);

        var shortLabel = !string.IsNullOrWhiteSpace(ti?.EnShort) ? ti!.EnShort! : fileName;

        var tooltipParts = new List<string>();
        if (!string.IsNullOrWhiteSpace(ti?.En)) tooltipParts.Add(ti!.En!);
        if (!string.IsNullOrWhiteSpace(ti?.Zh)) tooltipParts.Add(ti!.Zh!);
        if (tooltipParts.Count == 0) tooltipParts.Add(rel);
        var tooltip = string.Join("\n", tooltipParts);

        long origSize = 0, origTicks = 0;
        try { var fi = new FileInfo(origAbs); origSize = fi.Length; origTicks = fi.LastWriteTimeUtc.Ticks; }
        catch { }

        var tranAbs = ResolveTranslatedPath(root, translatedDir, rel);

        var status = _statusService.ComputeStatusForPairLive(origAbs, tranAbs, root, relKey, verboseLog: false);

        long tranSize = 0, tranTicks = 0;
        if (File.Exists(tranAbs))
        {
            try { var fi = new FileInfo(tranAbs); tranSize = fi.Length; tranTicks = fi.LastWriteTimeUtc.Ticks; }
            catch { }
        }

        return new FileNavItem
        {
            RelPath = rel,
            FileName = fileName,
            DisplayShort = shortLabel,
            Tooltip = tooltip,
            Status = status,
            TranslatedMtimeTicks = tranTicks,
            OrigSizeBytes = origSize,
            OrigMtimeTicks = origTicks,
            TranSizeBytes = tranSize,
            TranResolvedPath = tranAbs,
        };
    }

    public Task<IndexCache> BuildAsync(
        string originalDir,
        string translatedDir,
        string root,
        IProgress<(int done, int total)>? progress = null,
        CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            var titles = LoadTitlesMap(root);

            var files = Directory.EnumerateFiles(originalDir, "*.xml", SearchOption.AllDirectories).ToList();
            int total = files.Count;

            var entries = new List<FileNavItem>(capacity: total);

            for (int i = 0; i < total; i++)
            {
                ct.ThrowIfCancellationRequested();

                entries.Add(BuildEntry(files[i], originalDir, translatedDir, root, titles));

                if (progress != null && (i % 50 == 0 || i == total - 1))
                    progress.Report((i + 1, total));
            }

            entries.Sort((a, b) => string.Compare(a.RelPath, b.RelPath, StringComparison.OrdinalIgnoreCase));

            return new IndexCache
            {
                Version = CacheVersion,
                RootPath = root,
                BuiltUtc = DateTime.UtcNow,
                BuildGuid = CacheBuildGuid,
                TitlesHash = ComputeTitlesHash(root),
                Entries = entries
            };
        }, ct);
    }

    public Task<IndexCache> RefreshAsync(
        IndexCache oldCache,
        string originalDir,
        string translatedDir,
        string root,
        IProgress<(int done, int total)>? progress = null,
        CancellationToken ct = default)
    {
        return Task.Run(async () =>
        {
            // Structural gate — a titles.jsonl change alters display fields for
            // every entry, and a guid/version/root drift means the stored stats
            // are not comparable. Any of these ⇒ wholesale discard + full build.
            var titlesHash = ComputeTitlesHash(root);
            bool structurallyReusable =
                oldCache is { Entries.Count: > 0 }
                && oldCache.Version >= CacheVersion
                && string.Equals(oldCache.BuildGuid, CacheBuildGuid, StringComparison.Ordinal)
                && string.Equals(oldCache.TitlesHash, titlesHash, StringComparison.Ordinal)
                && string.Equals(
                    Path.GetFullPath(oldCache.RootPath), Path.GetFullPath(root),
                    StringComparison.OrdinalIgnoreCase);

            if (!structurallyReusable)
            {
                var full = await BuildAsync(originalDir, translatedDir, root, progress, ct);
                await SaveAsync(root, full);
                return full;
            }

            var titles = LoadTitlesMap(root);

            var oldByRel = new Dictionary<string, FileNavItem>(StringComparer.OrdinalIgnoreCase);
            foreach (var e in oldCache.Entries)
                oldByRel[NormalizePathKey(e.RelPath)] = e;

            var files = Directory.EnumerateFiles(originalDir, "*.xml", SearchOption.AllDirectories).ToList();

            var entries = new List<FileNavItem>(capacity: files.Count);
            var recompute = new List<string>();
            bool changed = false;

            // Diff pass — stat-only, no file opens. Reuse an entry untouched when
            // both sides' (size, ticks) and the resolved translation path all
            // match; otherwise queue a status recompute for that entry only.
            foreach (var origAbs in files)
            {
                ct.ThrowIfCancellationRequested();

                var rel = Path.GetRelativePath(originalDir, origAbs);
                var relKey = NormalizePathKey(rel);

                long origSize = 0, origTicks = 0;
                try { var fi = new FileInfo(origAbs); origSize = fi.Length; origTicks = fi.LastWriteTimeUtc.Ticks; }
                catch { }

                var tranAbs = ResolveTranslatedPath(root, translatedDir, rel);
                long tranSize = 0, tranTicks = 0;
                if (File.Exists(tranAbs))
                {
                    try { var fi = new FileInfo(tranAbs); tranSize = fi.Length; tranTicks = fi.LastWriteTimeUtc.Ticks; }
                    catch { }
                }

                if (oldByRel.TryGetValue(relKey, out var old)
                    && old.OrigSizeBytes == origSize
                    && old.OrigMtimeTicks == origTicks
                    && old.TranSizeBytes == tranSize
                    && old.TranslatedMtimeTicks == tranTicks
                    && string.Equals(old.TranResolvedPath, tranAbs, StringComparison.OrdinalIgnoreCase))
                {
                    entries.Add(old);
                }
                else
                {
                    recompute.Add(origAbs);
                    changed = true;
                }
            }

            // Removed originals: any old entry not seen this sweep is dropped
            // (entries holds only reused + recompute). Count drift ⇒ save.
            if (entries.Count + recompute.Count != oldCache.Entries.Count)
                changed = true;

            // Recompute pass — progress reported over the recompute set only, so
            // an ordinary launch shows nothing or "3/3", never the full corpus.
            for (int i = 0; i < recompute.Count; i++)
            {
                ct.ThrowIfCancellationRequested();
                entries.Add(BuildEntry(recompute[i], originalDir, translatedDir, root, titles));
                if (progress != null)
                    progress.Report((i + 1, recompute.Count));
            }

            entries.Sort((a, b) => string.Compare(a.RelPath, b.RelPath, StringComparison.OrdinalIgnoreCase));

            var refreshed = new IndexCache
            {
                Version = CacheVersion,
                RootPath = root,
                BuiltUtc = DateTime.UtcNow,
                BuildGuid = CacheBuildGuid,
                TitlesHash = titlesHash,
                Entries = entries
            };

            if (changed)
                await SaveAsync(root, refreshed);

            return refreshed;
        }, ct);
    }
}
