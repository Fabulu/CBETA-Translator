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

public sealed class TermbaseService : ITermbaseService
{
    private readonly ITermbaseStorageService _storage;
    private string? _username;

    public TermbaseService() : this(new TermbaseStorageService()) { }
    public TermbaseService(ITermbaseStorageService storage) { _storage = storage; }

    /// <summary>
    /// Sets the current username so FindTermsAsync can resolve the per-user termbase file.
    /// </summary>
    public void SetUsername(string? username)
    {
        _username = string.IsNullOrWhiteSpace(username) ? null : username.Trim();
    }

    // In-memory termbase caches — avoid re-reading JSON on every block change.
    // Auto-invalidate when the source files' last-write timestamps change.
    // Each cache lives in ONE immutable slot swapped by reference: this singleton is
    // hit concurrently (warmup + queries), and separate path/time/rows fields could
    // tear into a stale mix (audit P2.6 / R3-M10; pattern matches
    // TranslationReviewService._aggregationCache).
    private sealed record PersonalTermsCache(string Path, DateTime LastWriteUtc, List<TermRow> Rows);
    private PersonalTermsCache? _personalCache;

    // Community termbases previously re-read EVERY community/termbases/*.jsonl from
    // disk on every segment change (audit P2.6 / R3-M8); now cached against a
    // (file count, newest mtime) stamp — same idea as the personal cache above.
    private sealed record CommunityTermsCache(string Dir, int FileCount, long MaxWriteTicks, Dictionary<string, List<TermbaseEntry>> Data);
    private CommunityTermsCache? _communityCache;

    public void InvalidateCache()
    {
        _personalCache = null;
        _communityCache = null;
    }

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private sealed class TermRow
    {
        public string SourceTerm { get; set; } = "";
        public string PreferredTarget { get; set; } = "";
        public List<string> AlternateTargets { get; set; } = new();
        public string Status { get; set; } = "";
        public string Note { get; set; } = "";
    }

    public async Task<List<TermHit>> FindTermsAsync(
        CurrentSegmentContext ctx,
        string? root,
        CancellationToken ct = default)
    {
        var result = new List<TermHit>();

        if (string.IsNullOrWhiteSpace(root))
            return result;

        // Try user's personal termbase first, fall back to shared
        string path;
        if (_username != null)
        {
            var userPath = ITermbaseStorageService.GetUserPath(root, _username);
            path = File.Exists(userPath) ? userPath : Path.Combine(root, "termbase.json");
        }
        else
        {
            path = Path.Combine(root, "termbase.json");
        }

        if (!File.Exists(path))
            return result;

        var rows = await LoadTermsCachedAsync(path, ct).ConfigureAwait(false);

        if (rows == null || rows.Count == 0)
            return result;

        string zh = NormalizeForMatch(ctx.ZhText ?? "");

        return rows
            .Where(t =>
                !string.IsNullOrWhiteSpace(t.SourceTerm) &&
                zh.Contains(NormalizeForMatch(t.SourceTerm), StringComparison.Ordinal))
            .OrderByDescending(t => t.SourceTerm.Length)
            .Select(t => new TermHit
            {
                SourceTerm = t.SourceTerm,
                PreferredTarget = t.PreferredTarget,
                AlternateTargets = t.AlternateTargets ?? new List<string>(),
                Status = t.Status,
                Note = t.Note
            })
            .ToList();
    }

    /// <inheritdoc />
    public async Task WarmupCacheAsync(string root, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(root)) return;

        var sharedPath = Path.Combine(root, "termbase.json");
        if (File.Exists(sharedPath))
            await LoadTermsCachedAsync(sharedPath, ct).ConfigureAwait(false);
    }

    private async Task<List<TermRow>?> LoadTermsCachedAsync(string path, CancellationToken ct)
    {
        try
        {
            var lastWrite = File.GetLastWriteTimeUtc(path);
            var cache = _personalCache; // single read: consistent (path, time, rows) triple
            if (cache != null &&
                string.Equals(cache.Path, path, StringComparison.OrdinalIgnoreCase) &&
                lastWrite == cache.LastWriteUtc)
            {
                return cache.Rows;
            }
        }
        catch { /* fall through to disk read */ }

        string rawJson;
        try
        {
            rawJson = await File.ReadAllTextAsync(path, ct).ConfigureAwait(false);
        }
        catch
        {
            return null;
        }

        List<TermRow>? rows;
        try
        {
            rows = JsonSerializer.Deserialize<List<TermRow>>(rawJson, JsonOpts);
        }
        catch
        {
            return null;
        }

        // Update cache (one atomic reference swap)
        try
        {
            if (rows != null)
                _personalCache = new PersonalTermsCache(path, File.GetLastWriteTimeUtc(path), rows);
        }
        catch { /* non-critical */ }

        return rows;
    }

    private static string NormalizeForMatch(string s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return "";

        s = s.Normalize(NormalizationForm.FormKC);

        s = s.Replace("\u3000", "")
             .Replace(" ", "")
             .Replace("\t", "")
             .Replace("\r", "")
             .Replace("\n", "");

        return s.Trim();
    }

    public async Task<List<TermHit>> FindCommunityTermsAsync(
        CurrentSegmentContext ctx, string? root, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(root) || string.IsNullOrWhiteSpace(ctx.ZhText))
            return new();

        var communityDir = TermbaseStorageService.GetCommunityTermbasesDir(root);
        if (!Directory.Exists(communityDir)) return new();

        var allCommunity = await LoadCommunityCachedAsync(communityDir, ct).ConfigureAwait(false);
        var zh = NormalizeForMatch(ctx.ZhText);
        var results = new List<TermHit>();

        foreach (var (username, entries) in allCommunity)
        {
            foreach (var entry in entries)
            {
                if (string.IsNullOrWhiteSpace(entry.SourceTerm)) continue;
                if (!zh.Contains(NormalizeForMatch(entry.SourceTerm), StringComparison.Ordinal)) continue;

                results.Add(new TermHit
                {
                    SourceTerm = entry.SourceTerm,
                    PreferredTarget = entry.PreferredTarget,
                    AlternateTargets = entry.AlternateTargets ?? new(),
                    Status = entry.Status,
                    Note = entry.Note,
                    CreatedBy = entry.CreatedBy ?? username
                });
            }
        }

        return results
            .GroupBy(t => t.SourceTerm, StringComparer.Ordinal)
            .Select(g => g.First())
            .OrderByDescending(t => t.SourceTerm.Length)
            .ToList();
    }

    /// <summary>
    /// Stat-only staleness check: re-reads the community jsonl files only when a file
    /// was added/removed or the newest last-write time moved. This method is called on
    /// every segment change via the assistant snapshot (audit P2.6 / R3-M8).
    /// The cached dictionary is treated as immutable — this class only iterates it.
    /// </summary>
    private async Task<Dictionary<string, List<TermbaseEntry>>> LoadCommunityCachedAsync(
        string communityDir, CancellationToken ct)
    {
        int fileCount = 0;
        long maxTicks = 0;
        try
        {
            foreach (var f in Directory.EnumerateFiles(communityDir, "*.jsonl"))
            {
                fileCount++;
                var t = File.GetLastWriteTimeUtc(f).Ticks;
                if (t > maxTicks) maxTicks = t;
            }
        }
        catch
        {
            // stat failed — fall back to a plain load below
            fileCount = -1;
        }

        var cache = _communityCache; // single read: consistent slot
        if (cache != null &&
            fileCount >= 0 &&
            string.Equals(cache.Dir, communityDir, StringComparison.OrdinalIgnoreCase) &&
            cache.FileCount == fileCount &&
            cache.MaxWriteTicks == maxTicks)
        {
            return cache.Data;
        }

        var data = await _storage.LoadAllCommunityJsonlAsync(communityDir, ct).ConfigureAwait(false);
        if (fileCount >= 0)
            _communityCache = new CommunityTermsCache(communityDir, fileCount, maxTicks, data);
        return data;
    }

    public async Task<List<TermHit>> GetAllTermsAsync(string? root, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(root))
            return new();

        string path;
        if (_username != null)
        {
            var userPath = ITermbaseStorageService.GetUserPath(root, _username);
            path = File.Exists(userPath) ? userPath : Path.Combine(root, "termbase.json");
        }
        else
        {
            path = Path.Combine(root, "termbase.json");
        }

        if (!File.Exists(path))
            return new();

        var rows = await LoadTermsCachedAsync(path, ct).ConfigureAwait(false);
        if (rows == null || rows.Count == 0)
            return new();

        return rows
            .Where(t => !string.IsNullOrWhiteSpace(t.SourceTerm))
            .Select(t => new TermHit
            {
                SourceTerm = t.SourceTerm,
                PreferredTarget = t.PreferredTarget,
                AlternateTargets = t.AlternateTargets ?? new(),
                Status = t.Status,
                Note = t.Note
            })
            .OrderBy(t => t.SourceTerm)
            .ToList();
    }
}