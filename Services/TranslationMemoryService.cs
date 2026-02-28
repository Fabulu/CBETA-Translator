using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CbetaTranslator.App.Models;

namespace CbetaTranslator.App.Services;

public sealed class TranslationMemoryService
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private sealed class TmRow
    {
        public string SourceText { get; set; } = "";
        public string TargetText { get; set; } = "";
        public string RelPath { get; set; } = "";
        public string ReviewStatus { get; set; } = "";
        public string Translator { get; set; } = "";
    }

    public Task<List<TranslationTmMatch>> FindApprovedMatchesAsync(
        CurrentSegmentContext ctx,
        string? root,
        string? translatedDir,
        CancellationToken ct = default)
    {
        return LoadAndMatchAsync(
            ctx,
            root,
            "translation-memory.approved.jsonl",
            TranslationResourceTrust.Approved,
            ct);
    }

    public Task<List<TranslationTmMatch>> FindReferenceMatchesAsync(
        CurrentSegmentContext ctx,
        string? root,
        string? translatedDir,
        CancellationToken ct = default)
    {
        return LoadAndMatchAsync(
            ctx,
            root,
            "translation-memory.reference.jsonl",
            TranslationResourceTrust.AiReference,
            ct);
    }

    private async Task<List<TranslationTmMatch>> LoadAndMatchAsync(
        CurrentSegmentContext ctx,
        string? root,
        string fileName,
        TranslationResourceTrust trust,
        CancellationToken ct)
    {
        var result = new List<TranslationTmMatch>();

        if (string.IsNullOrWhiteSpace(root))
            return result;

        var path = Path.Combine(root, fileName);
        if (!File.Exists(path))
            return result;

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

                var line = await sr.ReadLineAsync();
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
                    // ignore bad jsonl rows
                }
            }
        }
        catch
        {
            return result;
        }

        if (rows.Count == 0)
            return result;

        string zh = Normalize(ctx.ZhText);
        string currentRel = NormalizeRel(ctx.RelPath);

        int minLen = 3;
        double minScore = trust == TranslationResourceTrust.Approved ? 60 : 35;

        result = rows
            .Where(r => !string.IsNullOrWhiteSpace(r.SourceText))
            .Where(r => Normalize(r.SourceText).Length >= minLen)
            .Where(r =>
                trust != TranslationResourceTrust.AiReference ||
                !string.Equals(NormalizeRel(r.RelPath), currentRel, StringComparison.OrdinalIgnoreCase))
            .Select(r =>
            {
                string sourceNorm = Normalize(r.SourceText);

                return new TranslationTmMatch
                {
                    SourceText = r.SourceText,
                    TargetText = r.TargetText,
                    RelPath = r.RelPath,
                    ReviewStatus = r.ReviewStatus,
                    Translator = r.Translator,
                    Trust = trust,
                    Score = Score(zh, sourceNorm)
                };
            })
            .Where(x => x.Score >= minScore)
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => Normalize(x.SourceText).Length)
            .ThenBy(x => x.RelPath, StringComparer.OrdinalIgnoreCase)
            .Take(8)
            .ToList();

        return result;
    }

    private static string Normalize(string? s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return "";

        s = s.Normalize(NormalizationForm.FormKC);
        s = s.Replace("\u3000", " ");

        return s.Trim()
                .Replace(" ", "")
                .Replace("\t", "")
                .Replace("\r", "")
                .Replace("\n", "");
    }

    private static string NormalizeRel(string? p)
    {
        return (p ?? "").Replace('\\', '/').TrimStart('/');
    }

    private static double Score(string a, string b)
    {
        if (string.Equals(a, b, StringComparison.Ordinal))
            return 100;

        if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b))
            return 0;

        int minLen = Math.Min(a.Length, b.Length);
        int maxLen = Math.Max(a.Length, b.Length);

        if (a.Contains(b, StringComparison.Ordinal) || b.Contains(a, StringComparison.Ordinal))
        {
            double coverage = 100.0 * minLen / maxLen;
            return Math.Max(coverage, Math.Min(85, 40 + (10.0 * minLen)));
        }

        int common = LongestCommonSubstringLength(a, b);
        if (common <= 0)
            return 0;

        double longestCoverage = 100.0 * common / maxLen;
        double shorterCoverage = 100.0 * common / minLen;

        return (longestCoverage * 0.7) + (shorterCoverage * 0.3);
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