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

public sealed class TermbaseService : ITermbaseService
{
    private readonly ITermbaseStorageService _storage;

    public TermbaseService() : this(new TermbaseStorageService()) { }
    public TermbaseService(ITermbaseStorageService storage) { _storage = storage; }

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

        var path = Path.Combine(root, "termbase.json");
        if (!File.Exists(path))
            return result;

        string rawJson;
        try
        {
            rawJson = await File.ReadAllTextAsync(path, ct).ConfigureAwait(false);
        }
        catch
        {
            return result;
        }

        List<TermRow>? rows;
        try
        {
            rows = JsonSerializer.Deserialize<List<TermRow>>(rawJson, JsonOpts);
        }
        catch
        {
            return result;
        }

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

        var allCommunity = await _storage.LoadAllCommunityJsonlAsync(communityDir, ct).ConfigureAwait(false);
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
}