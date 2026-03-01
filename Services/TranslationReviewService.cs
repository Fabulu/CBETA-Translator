using CbetaTranslator.App.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CbetaTranslator.App.Services;

public sealed class TranslationReviewService
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    private sealed class TmRow
    {
        public string SourceText { get; set; } = "";
        public string TargetText { get; set; } = "";
        public string RelPath { get; set; } = "";
        public string ReviewStatus { get; set; } = "";
        public string Translator { get; set; } = "";
    }

    public static string GetLedgerPath(string root)
        => Path.Combine(root, "translation-review.jsonl");

    public static string GetApprovedTmPath(string root)
        => Path.Combine(root, "translation-memory.approved.jsonl");

    public async Task AppendReviewAsync(
        string root,
        CurrentSegmentContext ctx,
        string status,
        string reviewer = "User",
        string? comment = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(root))
            throw new ArgumentException("Root is required.", nameof(root));

        if (ctx == null)
            throw new ArgumentNullException(nameof(ctx));

        status = TranslationReviewStatuses.Normalize(status);

        string zh = NormalizeLine(ctx.ZhText);
        string en = NormalizeLine(ctx.EnText);

        if (string.IsNullOrWhiteSpace(zh))
            throw new InvalidOperationException("Cannot review a segment with empty ZH text.");

        if (status == TranslationReviewStatuses.Approved && string.IsNullOrWhiteSpace(en))
            throw new InvalidOperationException("Cannot approve a segment while EN is empty.");

        if (!string.IsNullOrWhiteSpace(en) && (en.Contains('<') || en.Contains('>')))
            throw new InvalidOperationException("Cannot review this segment because EN contains '<' or '>'.");

        Directory.CreateDirectory(root);

        var row = new TranslationReviewEntry
        {
            SegmentKey = BuildSegmentKey(ctx.RelPath, ctx.Mode, ctx.BlockNumber),
            RelPath = NormalizeRel(ctx.RelPath),
            TextId = (ctx.TextId ?? "").Trim(),
            Mode = ctx.Mode.ToString(),
            BlockNumber = ctx.BlockNumber,
            ZhText = zh,
            EnText = en,
            Status = status,
            Reviewer = string.IsNullOrWhiteSpace(reviewer) ? "User" : reviewer.Trim(),
            Comment = (comment ?? "").Trim(),
            ReviewedUtc = DateTime.UtcNow,
            ZhHash = Hash(zh),
            EnHash = Hash(en)
        };

        var json = JsonSerializer.Serialize(row, JsonOpts) + Environment.NewLine;
        await File.AppendAllTextAsync(GetLedgerPath(root), json, new UTF8Encoding(false), ct);
    }

    public async Task<Dictionary<string, TranslationReviewEntry>> LoadLatestEntriesAsync(
        string root,
        CancellationToken ct = default)
    {
        var result = new Dictionary<string, TranslationReviewEntry>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(root))
            return result;

        var path = GetLedgerPath(root);
        if (!File.Exists(path))
            return result;

        using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete);
        using var sr = new StreamReader(fs, Encoding.UTF8);

        while (!sr.EndOfStream)
        {
            ct.ThrowIfCancellationRequested();

            var line = await sr.ReadLineAsync();
            if (string.IsNullOrWhiteSpace(line))
                continue;

            try
            {
                var row = JsonSerializer.Deserialize<TranslationReviewEntry>(line, JsonOpts);
                if (row == null)
                    continue;

                row.Status = TranslationReviewStatuses.Normalize(row.Status);
                row.RelPath = NormalizeRel(row.RelPath);
                row.SegmentKey = string.IsNullOrWhiteSpace(row.SegmentKey)
                    ? BuildSegmentKey(row.RelPath, row.Mode, row.BlockNumber)
                    : row.SegmentKey;

                result[row.SegmentKey] = row;
            }
            catch
            {
                // ignore broken rows
            }
        }

        return result;
    }

    public async Task<TranslationReviewEntry?> GetLatestEntryAsync(
        string root,
        CurrentSegmentContext ctx,
        CancellationToken ct = default)
    {
        if (ctx == null)
            return null;

        var latest = await LoadLatestEntriesAsync(root, ct);
        latest.TryGetValue(BuildSegmentKey(ctx.RelPath, ctx.Mode, ctx.BlockNumber), out var entry);
        return entry;
    }

    public async Task<int> RebuildApprovedTranslationMemoryAsync(
        string root,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(root))
            throw new ArgumentException("Root is required.", nameof(root));

        Directory.CreateDirectory(root);

        var latest = await LoadLatestEntriesAsync(root, ct);

        var approved = latest.Values
            .Where(x => x.Status == TranslationReviewStatuses.Approved)
            .Where(x => IsUsableApprovedPair(x.ZhText, x.EnText))
            .OrderBy(x => x.RelPath, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Mode, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.BlockNumber)
            .ToList();

        var path = GetApprovedTmPath(root);

        await using var fs = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.ReadWrite);
        await using var writer = new StreamWriter(fs, new UTF8Encoding(false));

        foreach (var entry in approved)
        {
            ct.ThrowIfCancellationRequested();

            var row = new TmRow
            {
                SourceText = NormalizeLine(entry.ZhText),
                TargetText = NormalizeLine(entry.EnText),
                RelPath = entry.RelPath,
                ReviewStatus = "Approved",
                Translator = string.IsNullOrWhiteSpace(entry.Reviewer) ? "User" : entry.Reviewer
            };

            var json = JsonSerializer.Serialize(row, JsonOpts);
            await writer.WriteLineAsync(json);
        }

        await writer.FlushAsync();
        await fs.FlushAsync(ct);

        return approved.Count;
    }

    public static string BuildSegmentKey(string? relPath, TranslationEditMode mode, int blockNumber)
        => BuildSegmentKey(relPath, mode.ToString(), blockNumber);

    public static string BuildSegmentKey(string? relPath, string? mode, int blockNumber)
        => $"{NormalizeRel(relPath)}|{(mode ?? "").Trim()}|{blockNumber}";

    private static bool IsUsableApprovedPair(string zh, string en)
    {
        zh = NormalizeLine(zh);
        en = NormalizeLine(en);

        if (string.IsNullOrWhiteSpace(zh) || string.IsNullOrWhiteSpace(en))
            return false;

        if (!ContainsChineseChar(zh))
            return false;

        if (zh == en)
            return false;

        return true;
    }

    private static string NormalizeRel(string? p)
        => (p ?? "").Replace('\\', '/').TrimStart('/').Trim();

    private static string NormalizeLine(string? s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return "";

        s = s.Normalize(NormalizationForm.FormKC);
        s = s.Replace("\u3000", " ");
        s = s.Replace("\r", " ").Replace("\n", " ").Replace("\t", " ");

        while (s.Contains("  ", StringComparison.Ordinal))
            s = s.Replace("  ", " ");

        return s.Trim();
    }

    private static string Hash(string s)
    {
        using var sha = SHA256.Create();
        var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(s ?? ""));
        return Convert.ToHexString(bytes);
    }

    private static bool ContainsChineseChar(string? s)
    {
        if (string.IsNullOrEmpty(s))
            return false;

        foreach (char ch in s)
        {
            if ((ch >= '\u3400' && ch <= '\u4DBF') ||
                (ch >= '\u4E00' && ch <= '\u9FFF') ||
                (ch >= '\uF900' && ch <= '\uFAFF'))
            {
                return true;
            }
        }

        return false;
    }
}