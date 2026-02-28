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

public sealed class TermbaseStorageService
{
    private static readonly JsonSerializerOptions ReadOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly JsonSerializerOptions WriteOpts = new()
    {
        WriteIndented = true
    };

    public async Task<List<TermbaseEntry>> LoadAsync(string root, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(root))
            throw new ArgumentException("Root is required.", nameof(root));

        var path = GetPath(root);
        if (!File.Exists(path))
            return new List<TermbaseEntry>();

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
                    .ToList()
            })
            .Where(e => !string.IsNullOrWhiteSpace(e.SourceTerm))
            .OrderBy(e => e.SourceTerm, StringComparer.Ordinal)
            .ToList();

        var json = JsonSerializer.Serialize(clean, WriteOpts);
        await File.WriteAllTextAsync(path, json, new UTF8Encoding(false), ct);
    }

    public static string GetPath(string root)
    {
        return Path.Combine(root, "termbase.json");
    }
}