using CbetaTranslator.App.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CbetaTranslator.App.Services;

public sealed class TranslationAssistantBuildService
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = false
    };

    private readonly IndexedTranslationService _indexedTranslation = new();

    private sealed class TmRow
    {
        public string SourceText { get; set; } = "";
        public string TargetText { get; set; } = "";
        public string RelPath { get; set; } = "";
        public string ReviewStatus { get; set; } = "";
        public string Translator { get; set; } = "";
    }

    public async Task<int> BuildReferenceTranslationMemoryAsync(
        string root,
        string originalDir,
        string translatedDir,
        Func<string, bool> isZen,
        IProgress<(int done, int total, string status)>? progress = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(root))
            throw new ArgumentException("Root is required.", nameof(root));

        if (string.IsNullOrWhiteSpace(originalDir) || !Directory.Exists(originalDir))
            throw new DirectoryNotFoundException("Original directory not found: " + originalDir);

        if (string.IsNullOrWhiteSpace(translatedDir) || !Directory.Exists(translatedDir))
            throw new DirectoryNotFoundException("Translated directory not found: " + translatedDir);

        if (isZen == null)
            throw new ArgumentNullException(nameof(isZen));

        var outputPath = Path.Combine(root, "translation-memory.reference.jsonl");

        var relPaths = Directory.EnumerateFiles(translatedDir, "*.xml", SearchOption.AllDirectories)
            .Select(abs => NormalizeRel(Path.GetRelativePath(translatedDir, abs)))
            .Where(rel => isZen(rel))
            .OrderBy(rel => rel, StringComparer.OrdinalIgnoreCase)
            .ToList();

        int total = relPaths.Count;
        int done = 0;
        int written = 0;

        Directory.CreateDirectory(root);

        await using var fs = new FileStream(
            outputPath,
            FileMode.Create,
            FileAccess.Write,
            FileShare.ReadWrite);

        await using var writer = new StreamWriter(fs, new UTF8Encoding(false));

        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var relPath in relPaths)
        {
            ct.ThrowIfCancellationRequested();

            var origAbs = Path.Combine(originalDir, relPath.Replace('/', Path.DirectorySeparatorChar));
            var tranAbs = Path.Combine(translatedDir, relPath.Replace('/', Path.DirectorySeparatorChar));

            done++;
            progress?.Report((done, total, "Scanning " + relPath));

            if (!File.Exists(origAbs) || !File.Exists(tranAbs))
                continue;

            string originalXml;
            string translatedXml;

            try
            {
                originalXml = await File.ReadAllTextAsync(origAbs, Encoding.UTF8, ct);
                translatedXml = await File.ReadAllTextAsync(tranAbs, Encoding.UTF8, ct);
            }
            catch
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(originalXml) || string.IsNullOrWhiteSpace(translatedXml))
                continue;

            IndexedTranslationDocument doc;
            try
            {
                doc = _indexedTranslation.BuildIndex(originalXml, translatedXml);
            }
            catch
            {
                continue;
            }

            var units = doc.Units
                .Where(u => u.Kind == TranslationUnitKind.Body)
                .OrderBy(u => u.Index)
                .ThenBy(u => u.LineNumber)
                .ToList();

            foreach (var unit in units)
            {
                ct.ThrowIfCancellationRequested();

                var zh = NormalizeLine(unit.Zh);
                var en = NormalizeLine(unit.En);

                if (!IsUsableReferencePair(zh, en))
                    continue;

                var row = new TmRow
                {
                    SourceText = zh,
                    TargetText = en,
                    RelPath = relPath,
                    ReviewStatus = "AI baseline",
                    Translator = "AutoImport"
                };

                var dedupeKey = BuildDedupeKey(row);
                if (!seen.Add(dedupeKey))
                    continue;

                var json = JsonSerializer.Serialize(row, JsonOpts);
                await writer.WriteLineAsync(json);
                written++;
            }
        }

        await writer.FlushAsync();
        await fs.FlushAsync(ct);

        progress?.Report((total, total, $"Built reference TM: {written:n0} rows"));
        return written;
    }

    public async Task AppendApprovedEntryAsync(
        string root,
        CurrentSegmentContext ctx,
        string reviewStatus = "Approved",
        string translator = "User",
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(root))
            throw new ArgumentException("Root is required.", nameof(root));

        if (ctx == null)
            throw new ArgumentNullException(nameof(ctx));

        var zh = NormalizeLine(ctx.ZhText);
        var en = NormalizeLine(ctx.EnText);

        if (string.IsNullOrWhiteSpace(zh))
            throw new InvalidOperationException("Cannot append approved entry: ZH is empty.");

        if (string.IsNullOrWhiteSpace(en))
            throw new InvalidOperationException("Cannot append approved entry: EN is empty.");

        var row = new TmRow
        {
            SourceText = zh,
            TargetText = en,
            RelPath = ctx.RelPath ?? "",
            ReviewStatus = string.IsNullOrWhiteSpace(reviewStatus) ? "Approved" : reviewStatus,
            Translator = string.IsNullOrWhiteSpace(translator) ? "User" : translator
        };

        var path = Path.Combine(root, "translation-memory.approved.jsonl");
        Directory.CreateDirectory(root);

        var json = JsonSerializer.Serialize(row, JsonOpts) + Environment.NewLine;
        await File.AppendAllTextAsync(path, json, new UTF8Encoding(false), ct);
    }

    private static bool IsUsableReferencePair(string zh, string en)
    {
        if (string.IsNullOrWhiteSpace(zh) || string.IsNullOrWhiteSpace(en))
            return false;

        if (!ContainsChineseChar(zh))
            return false;

        if (ContainsChineseChar(en))
            return false;

        if (string.Equals(zh, en, StringComparison.Ordinal))
            return false;

        if (zh.Length < 2 || en.Length < 2)
            return false;

        return true;
    }

    private static string BuildDedupeKey(TmRow row)
    {
        return string.Concat(
            NormalizeLine(row.SourceText), "\n",
            NormalizeLine(row.TargetText), "\n",
            row.RelPath ?? "");
    }

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

    private static string NormalizeRel(string p)
    {
        return (p ?? "").Replace('\\', '/').TrimStart('/');
    }
}