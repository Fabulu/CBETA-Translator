using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CbetaTranslator.App.Models;

namespace CbetaTranslator.App.Services;

public sealed class TranslationMemoryService
{
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
        => LoadAndMatchAsync(ctx, root, "translation-memory.approved.jsonl", TranslationResourceTrust.Approved, ct);

    public Task<List<TranslationTmMatch>> FindReferenceMatchesAsync(
        CurrentSegmentContext ctx,
        string? root,
        string? translatedDir,
        CancellationToken ct = default)
        => LoadAndMatchAsync(ctx, root, "translation-memory.reference.jsonl", TranslationResourceTrust.AiReference, ct);

    private async Task<List<TranslationTmMatch>> LoadAndMatchAsync(
        CurrentSegmentContext ctx,
        string? root,
        string fileName,
        TranslationResourceTrust trust,
        CancellationToken ct)
    {
        var result = new List<TranslationTmMatch>();
        if (string.IsNullOrWhiteSpace(root)) return result;

        var path = Path.Combine(root, fileName);
        if (!File.Exists(path)) return result;

        var rows = new List<TmRow>();
        foreach (var line in await File.ReadAllLinesAsync(path, ct))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;

            try
            {
                var row = JsonSerializer.Deserialize<TmRow>(line);
                if (row != null) rows.Add(row);
            }
            catch
            {
            }
        }

        string zh = Normalize(ctx.ZhText);

        result = rows
            .Select(r => new TranslationTmMatch
            {
                SourceText = r.SourceText,
                TargetText = r.TargetText,
                RelPath = r.RelPath,
                ReviewStatus = r.ReviewStatus,
                Translator = r.Translator,
                Trust = trust,
                Score = Score(zh, Normalize(r.SourceText))
            })
            .Where(x => x.Score >= 60)
            .OrderByDescending(x => x.Trust == TranslationResourceTrust.Approved ? 1 : 0)
            .ThenByDescending(x => x.Score)
            .Take(8)
            .ToList();

        return result;
    }

    private static string Normalize(string s)
        => (s ?? "").Trim().Replace(" ", "");

    private static double Score(string a, string b)
    {
        if (string.Equals(a, b, StringComparison.Ordinal))
            return 100;

        if (string.IsNullOrWhiteSpace(a) || string.IsNullOrWhiteSpace(b))
            return 0;

        if (a.Contains(b, StringComparison.Ordinal) || b.Contains(a, StringComparison.Ordinal))
            return 90;

        int common = LongestCommonSubstringLength(a, b);
        return 100.0 * common / Math.Max(a.Length, b.Length);
    }

    private static int LongestCommonSubstringLength(string a, string b)
    {
        if (a.Length == 0 || b.Length == 0) return 0;
        var dp = new int[b.Length + 1];
        int best = 0;

        for (int i = 1; i <= a.Length; i++)
        {
            for (int j = b.Length; j >= 1; j--)
            {
                if (a[i - 1] == b[j - 1])
                {
                    dp[j] = dp[j - 1] + 1;
                    if (dp[j] > best) best = dp[j];
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