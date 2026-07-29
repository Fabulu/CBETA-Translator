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

public sealed class TermbaseStorageService : ITermbaseStorageService
{
    private static readonly JsonSerializerOptions ReadOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly JsonSerializerOptions WriteOpts = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public async Task<List<TermbaseEntry>> LoadAsync(string root, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(root))
            throw new ArgumentException("Root is required.", nameof(root));

        var path = GetPath(root);
        if (!File.Exists(path))
            return GetSeedEntries();

        string json = await File.ReadAllTextAsync(path, Encoding.UTF8, ct);
        if (string.IsNullOrWhiteSpace(json))
            return new List<TermbaseEntry>();

        var entries = JsonSerializer.Deserialize<List<TermbaseEntry>>(json, ReadOpts) ?? new List<TermbaseEntry>();

        foreach (var entry in entries)
        {
            entry.SourceTerm = entry.SourceTerm?.Trim() ?? "";
            entry.PreferredTarget = entry.PreferredTarget?.Trim() ?? "";
            entry.Status = string.IsNullOrWhiteSpace(entry.Status) ? "preferred" : entry.Status.Trim();
            entry.Note = entry.Note?.Trim() ?? "";
            entry.AlternateTargets ??= new List<string>();
            entry.AlternateTargets = entry.AlternateTargets
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToList();
        }

        return entries
            .OrderBy(x => x.SourceTerm, StringComparer.Ordinal)
            .ToList();
    }

    public async Task SaveAsync(string root, IEnumerable<TermbaseEntry> entries, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(root))
            throw new ArgumentException("Root is required.", nameof(root));

        if (entries == null)
            throw new ArgumentNullException(nameof(entries));

        var path = GetPath(root);
        Directory.CreateDirectory(root);

        var clean = entries
            .Select(e => new TermbaseEntry
            {
                SourceTerm = e.SourceTerm?.Trim() ?? "",
                PreferredTarget = e.PreferredTarget?.Trim() ?? "",
                Status = string.IsNullOrWhiteSpace(e.Status) ? "preferred" : e.Status.Trim(),
                Note = e.Note?.Trim() ?? "",
                AlternateTargets = (e.AlternateTargets ?? new List<string>())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.Trim())
                    .Distinct(StringComparer.Ordinal)
                    .ToList(),
                CreatedBy = e.CreatedBy,
                WrittenUtc = e.WrittenUtc
            })
            .Where(e => !string.IsNullOrWhiteSpace(e.SourceTerm))
            .OrderBy(e => e.SourceTerm, StringComparer.Ordinal)
            .ToList();

        // Safety: never overwrite a non-empty file with empty data
        if (clean.Count == 0 && File.Exists(path) && new FileInfo(path).Length > 10)
            return;

        var json = JsonSerializer.Serialize(clean, WriteOpts);
        var tmpPath = path + ".tmp";
        await File.WriteAllTextAsync(tmpPath, json, new UTF8Encoding(false), ct);
        File.Move(tmpPath, path, overwrite: true);
    }

    // WriteUserJsonlAsync / LoadAllCommunityJsonlAsync removed: personal termbase entries
    // are local-only. They are no longer published to community/termbases/{login}.jsonl,
    // nor are other users' community termbases read/rendered.

    public async Task<List<TermbaseEntry>> LoadUserAsync(string root, string username, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(root))
            throw new ArgumentException("Root is required.", nameof(root));
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username is required.", nameof(username));

        var userPath = ITermbaseStorageService.GetUserPath(root, username);
        if (File.Exists(userPath))
        {
            var json = await File.ReadAllTextAsync(userPath, Encoding.UTF8, ct);
            if (!string.IsNullOrWhiteSpace(json))
            {
                var entries = JsonSerializer.Deserialize<List<TermbaseEntry>>(json, ReadOpts) ?? new();
                return entries
                    .OrderBy(x => x.SourceTerm, StringComparer.Ordinal)
                    .ToList();
            }
        }

        // Fall back to shared termbase
        return await LoadAsync(root, ct);
    }

    public async Task SaveUserAsync(string root, string username, IEnumerable<TermbaseEntry> entries, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(root))
            throw new ArgumentException("Root is required.", nameof(root));
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username is required.", nameof(username));
        if (entries == null)
            throw new ArgumentNullException(nameof(entries));

        var dir = ITermbaseStorageService.GetCommunityTermbasesDir(root);
        Directory.CreateDirectory(dir);
        var path = ITermbaseStorageService.GetUserPath(root, username);

        var clean = entries
            .Select(e => new TermbaseEntry
            {
                SourceTerm = e.SourceTerm?.Trim() ?? "",
                PreferredTarget = e.PreferredTarget?.Trim() ?? "",
                Status = string.IsNullOrWhiteSpace(e.Status) ? "preferred" : e.Status.Trim(),
                Note = e.Note?.Trim() ?? "",
                AlternateTargets = (e.AlternateTargets ?? new List<string>())
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Select(x => x.Trim())
                    .Distinct(StringComparer.Ordinal)
                    .ToList(),
                CreatedBy = e.CreatedBy,
                WrittenUtc = e.WrittenUtc
            })
            .Where(e => !string.IsNullOrWhiteSpace(e.SourceTerm))
            .OrderBy(e => e.SourceTerm, StringComparer.Ordinal)
            .ToList();

        // Safety: never overwrite a non-empty file with empty data
        if (clean.Count == 0 && File.Exists(path) && new FileInfo(path).Length > 10)
            return;

        var json = JsonSerializer.Serialize(clean, WriteOpts);
        var tmpPath = path + ".tmp";
        await File.WriteAllTextAsync(tmpPath, json, new UTF8Encoding(false), ct);
        File.Move(tmpPath, path, overwrite: true);
    }

    public static string GetCommunityTermbasesDir(string repoRoot)
        => Path.Combine(repoRoot, "community", "termbases");

    public static string GetPath(string root)
    {
        return Path.Combine(root, "termbase.json");
    }

    private static List<TermbaseEntry> GetSeedEntries() => new()
    {
        new TermbaseEntry
        {
            SourceTerm = "狗",
            PreferredTarget = "dog",
            Status = "preferred",
            Note = "Example entry. Often appears in koans (e.g., Zhaozhou's 'Does a dog have Buddha-nature?').",
            WrittenUtc = DateTimeOffset.UtcNow
        },
        new TermbaseEntry
        {
            SourceTerm = "佛性",
            PreferredTarget = "Buddha-nature",
            AlternateTargets = new List<string> { "Buddha nature", "buddhadhātu" },
            Status = "preferred",
            Note = "Core Mahāyāna concept. The innate potential for awakening present in all sentient beings.",
            WrittenUtc = DateTimeOffset.UtcNow
        }
    };
}