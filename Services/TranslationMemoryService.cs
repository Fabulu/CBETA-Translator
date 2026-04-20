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

public sealed class TranslationMemoryService : ITranslationMemoryService
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    // In-memory TM file cache — avoids re-reading JSONL on every block change.
    // Auto-invalidates when the file's last-write timestamp changes.
    private string? _approvedCachePath;
    private DateTime _approvedCacheTime;
    private List<TmRow>? _approvedCacheRows;

    private string? _referenceCachePath;
    private DateTime _referenceCacheTime;
    private List<TmRow>? _referenceCacheRows;

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

    public Task<List<TranslationTmMatch>> FindApprovedMatchesAsync(
        CurrentSegmentContext ctx,
        string? root,
        string? translatedDir,
        CancellationToken ct = default,
        int maxResults = 8)
    {
        return LoadAndMatchAsync(
            ctx,
            root,
            "translation-memory.approved.jsonl",
            TranslationResourceTrust.Approved,
            ct,
            maxResults);
    }

    public Task<List<TranslationTmMatch>> FindReferenceMatchesAsync(
        CurrentSegmentContext ctx,
        string? root,
        string? translatedDir,
        CancellationToken ct = default,
        int maxResults = 8)
    {
        return LoadAndMatchAsync(
            ctx,
            root,
            "translation-memory.reference.jsonl",
            TranslationResourceTrust.AiReference,
            ct,
            maxResults);
    }

    private async Task<List<TranslationTmMatch>> LoadAndMatchAsync(
        CurrentSegmentContext ctx,
        string? root,
        string fileName,
        TranslationResourceTrust trust,
        CancellationToken ct,
        int maxResults = 8)
    {
        var result = new List<TranslationTmMatch>();

        if (string.IsNullOrWhiteSpace(root))
            return result;

        var path = Path.Combine(root, fileName);
        if (!File.Exists(path))
            return result;

        var rows = await LoadRowsCachedAsync(path, trust, ct).ConfigureAwait(false);

        if (rows.Count == 0)
            return result;

        // Score against BOTH the single current block and the wider context (prev+current+next).
        // Taking the maximum preserves single-block exact-match ranking (still 100) while also
        // finding entries that span lb-tag boundaries via the context score.
        string currentZhRaw = ctx.ZhText ?? "";
        string contextZhRaw = !string.IsNullOrEmpty(ctx.ZhContextText) ? ctx.ZhContextText : currentZhRaw;
        string zhSingle = CjkMatchNormalizer.Normalize(currentZhRaw);
        string zhContext = CjkMatchNormalizer.Normalize(contextZhRaw);
        string zhExact   = zhSingle;   // used only for self-exclusion
        string currentRel = NormalizeRel(ctx.RelPath);
        int currentBlock = ctx.BlockNumber;

        int minLen = 2;
        double minScore = trust == TranslationResourceTrust.Approved ? 18 : 30;

        result = await Task.Run(() => rows
            .Where(r => !string.IsNullOrWhiteSpace(r.SourceText))
            .Where(r => CjkMatchNormalizer.Normalize(r.SourceText).Length >= minLen)
            .Where(r => !IsExactCurrentSegment(r, trust, currentRel, currentBlock, zhExact))
            .Select(r =>
            {
                string sourceNorm = CjkMatchNormalizer.Normalize(r.SourceText);
                double singleScore = Score(zhSingle, sourceNorm);
                double contextScore = Score(zhContext, sourceNorm);
                double score = CombineSingleAndContextScores(singleScore, contextScore);
                bool hasExplainableOverlap = CjkMatchNormalizer
                    .FindSharedRawRanges(r.SourceText, currentZhRaw, minPhraseLen: 2)
                    .Count > 0;

                return new
                {
                    Row = r,
                    Score = score,
                    HasExplainableOverlap = hasExplainableOverlap
                };
            })
            .Where(x => x.Score >= minScore && x.HasExplainableOverlap)
            .Select(x => new TranslationTmMatch
            {
                SourceText = x.Row.SourceText,
                TargetText = x.Row.TargetText,
                RelPath = x.Row.RelPath,
                BlockNumber = x.Row.BlockNumber,
                ReviewStatus = x.Row.ReviewStatus,
                Translator = x.Row.Translator,
                Trust = trust,
                Score = x.Score
            })
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => CjkMatchNormalizer.Normalize(x.SourceText).Length)
            .ThenBy(x => x.RelPath, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.BlockNumber)
            .Take(maxResults)
            .ToList(), ct).ConfigureAwait(false);

        return result;
    }

    /// <inheritdoc />
    public async Task WarmupCacheAsync(string root, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(root)) return;

        var approvedPath = Path.Combine(root, "translation-memory.approved.jsonl");
        var referencePath = Path.Combine(root, "translation-memory.reference.jsonl");

        if (File.Exists(approvedPath))
            await LoadRowsCachedAsync(approvedPath, TranslationResourceTrust.Approved, ct).ConfigureAwait(false);
        if (File.Exists(referencePath))
            await LoadRowsCachedAsync(referencePath, TranslationResourceTrust.AiReference, ct).ConfigureAwait(false);
    }

    private async Task<List<TmRow>> LoadRowsCachedAsync(
        string path, TranslationResourceTrust trust, CancellationToken ct)
    {
        bool isApproved = trust == TranslationResourceTrust.Approved;

        // Check cache — pick the right slot based on trust level
        string? cachedPath = isApproved ? _approvedCachePath : _referenceCachePath;
        DateTime cachedTime = isApproved ? _approvedCacheTime : _referenceCacheTime;
        List<TmRow>? cachedRows = isApproved ? _approvedCacheRows : _referenceCacheRows;

        try
        {
            var lastWrite = File.GetLastWriteTimeUtc(path);
            if (cachedRows != null &&
                string.Equals(cachedPath, path, StringComparison.OrdinalIgnoreCase) &&
                lastWrite == cachedTime)
            {
                return cachedRows;
            }
        }
        catch { /* fall through to disk read */ }

        var rows = new List<TmRow>();

        try
        {
            using var fs = new FileStream(
                path,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite | FileShare.Delete);

            using var sr = new StreamReader(fs, Encoding.UTF8);

            while (!sr.EndOfStream)
            {
                ct.ThrowIfCancellationRequested();

                var line = await sr.ReadLineAsync().ConfigureAwait(false);
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                try
                {
                    var row = JsonSerializer.Deserialize<TmRow>(line, JsonOpts);
                    if (row != null)
                        rows.Add(row);
                }
                catch
                {
                    // ignore bad rows
                }
            }
        }
        catch
        {
            return rows;
        }

        // Update cache
        try
        {
            var writeTime = File.GetLastWriteTimeUtc(path);
            if (isApproved)
            {
                _approvedCachePath = path;
                _approvedCacheTime = writeTime;
                _approvedCacheRows = rows;
            }
            else
            {
                _referenceCachePath = path;
                _referenceCacheTime = writeTime;
                _referenceCacheRows = rows;
            }
        }
        catch { /* non-critical */ }

        return rows;
    }

    private static bool IsExactCurrentSegment(
        TmRow row,
        TranslationResourceTrust trust,
        string currentRel,
        int currentBlock,
        string currentZh)
    {
        string rowRel = NormalizeRel(row.RelPath);
        string rowZh = CjkMatchNormalizer.Normalize(row.SourceText);

        if (trust == TranslationResourceTrust.AiReference &&
            !string.IsNullOrWhiteSpace(currentRel) &&
            string.Equals(rowRel, currentRel, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (trust == TranslationResourceTrust.Approved)
        {
            if (!string.IsNullOrWhiteSpace(currentRel) &&
                string.Equals(rowRel, currentRel, StringComparison.OrdinalIgnoreCase) &&
                row.BlockNumber > 0 &&
                currentBlock > 0 &&
                row.BlockNumber == currentBlock)
            {
                return true;
            }
        }

        if (!string.IsNullOrWhiteSpace(currentRel) &&
            string.Equals(rowRel, currentRel, StringComparison.OrdinalIgnoreCase) &&
            !string.IsNullOrWhiteSpace(currentZh) &&
            string.Equals(rowZh, currentZh, StringComparison.Ordinal))
        {
            return true;
        }

        return false;
    }

    private static string NormalizeRel(string? p)
    {
        return (p ?? "").Replace('\\', '/').TrimStart('/');
    }

    private static double CombineSingleAndContextScores(double singleScore, double contextScore)
    {
        // Context helps phrases split across neighboring tags, but single-line relevance remains primary.
        double contextCapped = Math.Min(contextScore, singleScore + 18);
        return Math.Max(singleScore, contextCapped);
    }

    private static double Score(string a, string b)
    {
        if (string.Equals(a, b, StringComparison.Ordinal))
            return 100;

        if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b))
            return 0;

        int minLen = Math.Min(a.Length, b.Length);
        int maxLen = Math.Max(a.Length, b.Length);

        double exactish = 0;

        if (a.Contains(b, StringComparison.Ordinal) || b.Contains(a, StringComparison.Ordinal))
        {
            double coverage = 100.0 * minLen / maxLen;
            exactish = Math.Max(coverage, Math.Min(85, 40 + (10.0 * minLen)));
        }
        else
        {
            int common = LongestCommonSubstringLength(a, b);
            if (common > 0)
            {
                double longestCoverage = 100.0 * common / maxLen;
                double shorterCoverage = 100.0 * common / minLen;
                exactish = (longestCoverage * 0.7) + (shorterCoverage * 0.3);
            }
        }

        double phrase = SharedPhraseScore(a, b);

        double score = Math.Max(exactish, phrase);

        if (phrase > 0 && exactish > 0)
            score = Math.Max(score, (exactish * 0.55) + (phrase * 0.75));

        return Math.Min(100, score);
    }

    private static double SharedPhraseScore(string a, string b)
    {
        var aPhrases = ExtractChinesePhrases(a, minLen: 2, maxLen: 6);
        var bSet = ExtractChinesePhrases(b, minLen: 2, maxLen: 6);

        if (aPhrases.Count == 0 || bSet.Count == 0)
            return 0;

        var shared = aPhrases
            .Where(p => bSet.Contains(p))
            .Distinct(StringComparer.Ordinal)
            .OrderByDescending(x => x.Length)
            .ToList();

        if (shared.Count == 0)
            return 0;

        double points = 0;

        foreach (var phrase in shared)
        {
            points += phrase.Length switch
            {
                >= 6 => 34,
                5 => 28,
                4 => 23,
                3 => 18,
                2 => 12,
                _ => 0
            };
        }

        points += Math.Min(12, shared.Count * 2);

        string longest = shared[0];
        if (longest.Length >= 2)
        {
            double aCoverage = 100.0 * longest.Length / Math.Max(1, a.Length);
            double bCoverage = 100.0 * longest.Length / Math.Max(1, b.Length);
            points += (aCoverage * 0.18) + (bCoverage * 0.18);
        }

        return Math.Min(92, points);
    }

    private static HashSet<string> ExtractChinesePhrases(string s, int minLen, int maxLen)
    {
        var result = new HashSet<string>(StringComparer.Ordinal);

        if (string.IsNullOrWhiteSpace(s))
            return result;

        var runs = new List<string>();
        var sb = new StringBuilder();

        foreach (char ch in s)
        {
            if (IsChineseChar(ch))
            {
                sb.Append(ch);
            }
            else if (sb.Length > 0)
            {
                runs.Add(sb.ToString());
                sb.Clear();
            }
        }

        if (sb.Length > 0)
            runs.Add(sb.ToString());

        foreach (var run in runs)
        {
            int upper = Math.Min(maxLen, run.Length);

            for (int len = upper; len >= minLen; len--)
            {
                for (int i = 0; i + len <= run.Length; i++)
                {
                    result.Add(run.Substring(i, len));
                }
            }
        }

        return result;
    }

    private static bool IsChineseChar(char ch)
    {
        return (ch >= '\u3400' && ch <= '\u4DBF')
            || (ch >= '\u4E00' && ch <= '\u9FFF')
            || (ch >= '\uF900' && ch <= '\uFAFF');
    }

    private static int LongestCommonSubstringLength(string a, string b)
    {
        if (a.Length == 0 || b.Length == 0)
            return 0;

        var dp = new int[b.Length + 1];
        int best = 0;

        for (int i = 1; i <= a.Length; i++)
        {
            for (int j = b.Length; j >= 1; j--)
            {
                if (a[i - 1] == b[j - 1])
                {
                    dp[j] = dp[j - 1] + 1;
                    if (dp[j] > best)
                        best = dp[j];
                }
                else
                {
                    dp[j] = 0;
                }
            }
        }

        return best;
    }
}
