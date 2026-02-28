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

public sealed class TermbaseService
{
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
        {
            result.Add(new TermHit
            {
                SourceTerm = "(debug)",
                PreferredTarget = "termbase.json NOT FOUND at: " + path,
                Status = "debug"
            });
            return result;
        }

        string rawJson;
        try
        {
            rawJson = await File.ReadAllTextAsync(path, ct);
        }
        catch (Exception ex)
        {
            result.Add(new TermHit
            {
                SourceTerm = "(debug)",
                PreferredTarget = "termbase read FAILED: " + ex.Message,
                Status = "debug"
            });
            return result;
        }

        List<TermRow>? rows;
        try
        {
            rows = JsonSerializer.Deserialize<List<TermRow>>(rawJson, JsonOpts);
        }
        catch (Exception ex)
        {
            result.Add(new TermHit
            {
                SourceTerm = "(debug)",
                PreferredTarget = "termbase load FAILED: " + ex.Message,
                Status = "debug"
            });
            return result;
        }

        if (rows == null)
        {
            result.Add(new TermHit
            {
                SourceTerm = "(debug)",
                PreferredTarget = "termbase deserialized to null",
                Status = "debug"
            });
            return result;
        }

        string zhRaw = ctx.ZhText ?? "";
        string zh = NormalizeForMatch(zhRaw);

        result.Add(new TermHit
        {
            SourceTerm = "(debug)",
            PreferredTarget = $"rows={rows.Count}, first sourceTerm='{rows.FirstOrDefault()?.SourceTerm ?? "(null)"}'",
            AlternateTargets = rows.Take(6).Select(r => $"'{r.SourceTerm}'").ToList(),
            Status = "debug",
            Note = "deserialization check"
        });

        var hits = rows
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

        if (hits.Count == 0)
        {
            result.Add(new TermHit
            {
                SourceTerm = "(debug)",
                PreferredTarget = "loaded OK, but no term matched current ZH",
                AlternateTargets = rows
                    .Where(x => !string.IsNullOrWhiteSpace(x.SourceTerm))
                    .Take(10)
                    .Select(x => x.SourceTerm)
                    .ToList(),
                Status = "debug",
                Note = "match check"
            });

            return result;
        }

        return hits;
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
}