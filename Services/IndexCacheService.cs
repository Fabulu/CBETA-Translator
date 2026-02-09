using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CbetaTranslator.App.Models;

namespace CbetaTranslator.App.Services;

public sealed class IndexCacheService
{
    private const string CacheFileName = "index.cache.json";

    // Bump this string whenever you want to force rebuild even if cache exists.
    // (Useful when you change status logic and want to ensure the cache isn't stale.)
    private const string CacheBuildGuid = "phase2-status-v1";

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
        try { Console.WriteLine(message); } catch { /* ignore */ }

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
    }

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

            // Version gate (keep your v2 baseline)
            if (cache.Version < 2)
                return null;

            // Extra invalidation lever: cache must match our build guid.
            // If your IndexCache model doesn't have this field yet, add it (string? BuildGuid).
            // If it doesn't exist, this line will not compile — see note below.
            if (!string.Equals(cache.BuildGuid, CacheBuildGuid, StringComparison.Ordinal))
                return null;

            return cache;
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
        cache.Version = 2;
        cache.BuildGuid = CacheBuildGuid;

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

    private static readonly Regex CjkRegex = new Regex(
        @"[\u3400-\u4DBF\u4E00-\u9FFF\uF900-\uFAFF]",
        RegexOptions.Compiled);

    private static bool FilesEqualFast(string a, string b)
    {
        var fa = new FileInfo(a);
        var fb = new FileInfo(b);
        if (fa.Length != fb.Length) return false;

        const int Buf = 1024 * 64;
        byte[] ba = new byte[Buf];
        byte[] bb = new byte[Buf];

        using var sa = File.OpenRead(a);
        using var sb = File.OpenRead(b);

        while (true)
        {
            int ra = sa.Read(ba, 0, Buf);
            int rb = sb.Read(bb, 0, Buf);
            if (ra != rb) return false;
            if (ra == 0) return true;

            for (int i = 0; i < ra; i++)
                if (ba[i] != bb[i]) return false;
        }
    }

    private static string ExtractBodyInnerXml(string xml)
    {
        if (string.IsNullOrEmpty(xml)) return "";

        int iBody = xml.IndexOf("<body", StringComparison.OrdinalIgnoreCase);
        if (iBody < 0) return "";

        int iStart = xml.IndexOf('>', iBody);
        if (iStart < 0) return "";

        int iEnd = xml.IndexOf("</body>", iStart + 1, StringComparison.OrdinalIgnoreCase);
        if (iEnd < 0) return "";

        return xml.Substring(iStart + 1, iEnd - (iStart + 1));
    }

    private static TranslationStatus ComputeStatus(string origPath, string tranPath, string rootForLogs, string relKeyForLogs, bool verboseLog)
    {
        // missing translated => red
        if (!File.Exists(tranPath))
        {
            if (verboseLog)
                Log(rootForLogs, $"STATUS RED (missing tran) rel={relKeyForLogs} tranPath={tranPath}");
            return TranslationStatus.Red;
        }

        // identical bytes => red
        bool same;
        try
        {
            same = FilesEqualFast(origPath, tranPath);
        }
        catch (Exception ex)
        {
            same = false;
            if (verboseLog)
                Log(rootForLogs, $"COMPARE FAILED rel={relKeyForLogs} ex={ex.GetType().Name}:{ex.Message}");
        }

        if (same)
        {
            if (verboseLog)
                Log(rootForLogs, $"STATUS RED (bytes identical) rel={relKeyForLogs}");
            return TranslationStatus.Red;
        }

        // different => yellow unless body has zero CJK => green
        try
        {
            var tranXml = File.ReadAllText(tranPath, Utf8NoBom);
            var body = ExtractBodyInnerXml(tranXml);

            bool hasCjk = CjkRegex.IsMatch(body);

            if (!hasCjk)
            {
                if (verboseLog)
                    Log(rootForLogs, $"STATUS GREEN (body has 0 CJK) rel={relKeyForLogs} bodyLen={body.Length}");
                return TranslationStatus.Green;
            }
        }
        catch (Exception ex)
        {
            if (verboseLog)
                Log(rootForLogs, $"BODY CHECK FAILED rel={relKeyForLogs} ex={ex.GetType().Name}:{ex.Message}");
        }

        if (verboseLog)
            Log(rootForLogs, $"STATUS YELLOW (diff but body still has CJK) rel={relKeyForLogs}");

        return TranslationStatus.Yellow;
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

                var status = ComputeStatus(origAbs, tranAbs, root, relKey, verbose);

                entries.Add(new FileNavItem
                {
                    RelPath = rel,
                    FileName = fileName,
                    DisplayShort = shortLabel,
                    Tooltip = tooltip,
                    Status = status
                });

                if (progress != null && (i % 50 == 0 || i == total - 1))
                    progress.Report((i + 1, total));
            }

            entries.Sort((a, b) => string.Compare(a.RelPath, b.RelPath, StringComparison.OrdinalIgnoreCase));

            Log(root, $"BUILD DONE: entries={entries.Count:n0}");

            return new IndexCache
            {
                Version = 2,
                RootPath = root,
                BuiltUtc = DateTime.UtcNow,
                BuildGuid = CacheBuildGuid,
                Entries = entries
            };
        }, ct);
    }
}
