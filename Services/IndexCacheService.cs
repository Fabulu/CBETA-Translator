using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

    // Bump this string whenever you want to force rebuild even if cache exists.
    // (Useful when you change status logic and want to ensure the cache isn't stale.)
    private const string CacheBuildGuid = "phase3-nav-v4-community-fallback";

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true
    };

    public string GetCachePath(string root)
        => Path.Combine(root, CacheFileName);

    private static string GetDebugLogPath(string root)
        => Path.Combine(root, "index.debug.log");

    private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);

    private static void Log(string root, string message)
    {
        // You said console output works — we do both.
       // try { Console.WriteLine(message); } catch { /* ignore */ }
       /*
        try
        {
            File.AppendAllText(GetDebugLogPath(root),
                $"[{DateTime.Now:O}] {message}{Environment.NewLine}",
                Utf8NoBom);
        }
        catch
        {
            // ignore logging failures
        }
       */
    }

    public async Task<IndexCache?> TryLoadAsync(string root, string? originalsRepoRoot = null)
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

            // Version gate
            if (cache.Version < 3)
                return null;

            if (!string.Equals(cache.BuildGuid, CacheBuildGuid, StringComparison.Ordinal))
                return null;

            // Git HEAD gate — translations repo.
            var liveHead = TryGetGitHead(root);
            if (!string.IsNullOrEmpty(cache.GitHead)
                && !string.IsNullOrEmpty(liveHead)
                && !string.Equals(cache.GitHead, liveHead, StringComparison.Ordinal))
            {
                return null;
            }

            // Git HEAD gate — originals repo. The file list is built from
            // the originals dir, so when new texts are added there the
            // cache is stale even if the translations repo hasn't changed.
            if (!string.IsNullOrEmpty(originalsRepoRoot))
            {
                var liveOriginalsHead = TryGetGitHead(originalsRepoRoot);
                if (!string.IsNullOrEmpty(cache.OriginalsGitHead)
                    && !string.IsNullOrEmpty(liveOriginalsHead)
                    && !string.Equals(cache.OriginalsGitHead, liveOriginalsHead, StringComparison.Ordinal))
                {
                    return null;
                }
            }

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

    public async Task SaveAsync(string root, IndexCache cache, string? originalsRepoRoot = null)
    {
        cache.RootPath = root;
        cache.BuiltUtc = DateTime.UtcNow;
        cache.Version = 3;
        cache.BuildGuid = CacheBuildGuid;
        // Snapshot the current HEAD so the next load can detect drift.
        // Null is fine — TryLoadAsync only gates when both sides have one.
        cache.GitHead = TryGetGitHead(root);
        cache.OriginalsGitHead = !string.IsNullOrEmpty(originalsRepoRoot)
            ? TryGetGitHead(originalsRepoRoot)
            : null;

        var path = GetCachePath(root);
        var json = JsonSerializer.Serialize(cache, JsonOpts);
        await File.WriteAllTextAsync(path, json, Utf8NoBom);
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

    // Match both T047n1987A.xml and T47n1987A.xml and any similar “T*47*...n1987A...” path.
    private static bool IsDebugTarget(string relKey, string fileName)
    {
        if (fileName.Equals("T047n1987A.xml", StringComparison.OrdinalIgnoreCase)) return true;
        if (fileName.Equals("T47n1987A.xml", StringComparison.OrdinalIgnoreCase)) return true;

        // broad match: contains n1987A and starts with T0 or T
        if (relKey.IndexOf("n1987A", StringComparison.OrdinalIgnoreCase) >= 0 &&
            relKey.StartsWith("T/", StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
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

    public Task<IndexCache> BuildAsync(
        string originalDir,
        string translatedDir,
        string root,
        IProgress<(int done, int total)>? progress = null,
        CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            // Start a fresh debug log every build
            try
            {
                File.WriteAllText(GetDebugLogPath(root),
                    $"Index build started {DateTime.Now:O}{Environment.NewLine}" +
                    $"root={root}{Environment.NewLine}" +
                    $"originalDir={originalDir}{Environment.NewLine}" +
                    $"translatedDir={translatedDir}{Environment.NewLine}" +
                    $"CacheBuildGuid={CacheBuildGuid}{Environment.NewLine}",
                    Utf8NoBom);
            }
            catch { /* ignore */ }

            Log(root, $"BUILD: Enumerating files under {originalDir}");

            var titles = LoadTitlesMap(root);

            var files = Directory.EnumerateFiles(originalDir, "*.xml", SearchOption.AllDirectories).ToList();
            int total = files.Count;

            Log(root, $"BUILD: Found {total:n0} XML files");

            var entries = new List<FileNavItem>(capacity: total);

            // Log the first N files for sanity (existence + computed translated path)
            const int LogFirstN = 25;

            for (int i = 0; i < total; i++)
            {
                ct.ThrowIfCancellationRequested();

                var origAbs = files[i];
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

                // IMPORTANT: this is the translated file path your app will check
                var tranAbs = Path.Combine(translatedDir, rel);

                // Fallback: if canonical translation doesn't exist, check community translations.
                // Handles OpenZen where xml-open-t/ is empty but community/translations/{user}/ has files.
                if (!File.Exists(tranAbs))
                {
                    var communityTransDir = Path.Combine(root, "community", "translations");
                    if (Directory.Exists(communityTransDir))
                    {
                        foreach (var userDir in Directory.GetDirectories(communityTransDir))
                        {
                            var communityPath = Path.Combine(userDir, rel);
                            if (File.Exists(communityPath))
                            {
                                tranAbs = communityPath;
                                break;
                            }
                        }
                    }
                }

                bool verbose =
                    i < LogFirstN ||
                    IsDebugTarget(relKey, fileName);

                if (verbose)
                {
                    Log(root, $"FILE[{i + 1}/{total}] relKey={relKey}");
                    Log(root, $"  origAbs={origAbs}");
                    Log(root, $"  tranAbs={tranAbs}");
                    Log(root, $"  tranExists={File.Exists(tranAbs)}");
                    try
                    {
                        Log(root, $"  origLen={new FileInfo(origAbs).Length}");
                        if (File.Exists(tranAbs))
                            Log(root, $"  tranLen={new FileInfo(tranAbs).Length}");
                    }
                    catch { /* ignore */ }
                }

                var status = _statusService.ComputeStatusForPairLive(origAbs, tranAbs, root, relKey, verbose);

                long mtimeTicks = 0;
                if (File.Exists(tranAbs))
                {
                    try { mtimeTicks = File.GetLastWriteTimeUtc(tranAbs).Ticks; }
                    catch { }
                }

                entries.Add(new FileNavItem
                {
                    RelPath = rel,
                    FileName = fileName,
                    DisplayShort = shortLabel,
                    Tooltip = tooltip,
                    Status = status,
                    TranslatedMtimeTicks = mtimeTicks,
                });

                if (progress != null && (i % 50 == 0 || i == total - 1))
                    progress.Report((i + 1, total));
            }

            entries.Sort((a, b) => string.Compare(a.RelPath, b.RelPath, StringComparison.OrdinalIgnoreCase));

            Log(root, $"BUILD DONE: entries={entries.Count:n0}");

            return new IndexCache
            {
                Version = 3,
                RootPath = root,
                BuiltUtc = DateTime.UtcNow,
                BuildGuid = CacheBuildGuid,
                GitHead = TryGetGitHead(root),
                Entries = entries
            };
        }, ct);
    }
}
