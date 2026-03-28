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

public sealed class TermbaseStorageService : ITermbaseStorageService
{
    private static readonly JsonSerializerOptions ReadOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly JsonSerializerOptions WriteOpts = new()
    {
        WriteIndented = true
    };

    private static readonly JsonSerializerOptions CompactOpts = new()
    {
        WriteIndented = false
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
                    .ToList(),
                CreatedBy = e.CreatedBy,
                WrittenUtc = e.WrittenUtc
            })
            .Where(e => !string.IsNullOrWhiteSpace(e.SourceTerm))
            .OrderBy(e => e.SourceTerm, StringComparer.Ordinal)
            .ToList();

        var json = JsonSerializer.Serialize(clean, WriteOpts);
        await File.WriteAllTextAsync(path, json, new UTF8Encoding(false), ct);
    }

    public async Task WriteUserJsonlAsync(string communityDir, string username, List<TermbaseEntry> entries, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(communityDir))
            throw new ArgumentException("Community directory is required.", nameof(communityDir));
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username is required.", nameof(username));
        if (entries == null)
            throw new ArgumentNullException(nameof(entries));

        Directory.CreateDirectory(communityDir);

        // Sanitize username to prevent path traversal
        var safeUsername = SanitizeFilename(username);
        var path = Path.Combine(communityDir, safeUsername + ".jsonl");
        var fullPath = Path.GetFullPath(path);
        var fullDir = Path.GetFullPath(communityDir);
        if (!fullPath.StartsWith(fullDir, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Username produces a path outside the community directory.", nameof(username));

        var sb = new StringBuilder();

        foreach (var e in entries)
        {
            sb.AppendLine(JsonSerializer.Serialize(e, CompactOpts));
        }

        await File.WriteAllTextAsync(path, sb.ToString(), new UTF8Encoding(false), ct);
    }

    public async Task<Dictionary<string, List<TermbaseEntry>>> LoadAllCommunityJsonlAsync(string communityDir, CancellationToken ct = default)
    {
        var result = new Dictionary<string, List<TermbaseEntry>>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(communityDir) || !Directory.Exists(communityDir))
            return result;

        foreach (var file in Directory.GetFiles(communityDir, "*.jsonl"))
        {
            ct.ThrowIfCancellationRequested();

            var username = Path.GetFileNameWithoutExtension(file);
            var entries = new List<TermbaseEntry>();

            var lines = await File.ReadAllLinesAsync(file, Encoding.UTF8, ct);
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                try
                {
                    var e = JsonSerializer.Deserialize<TermbaseEntry>(line, ReadOpts);
                    if (e != null)
                        entries.Add(e);
                }
                catch
                {
                    // Skip malformed lines
                }
            }

            if (entries.Count > 0)
                result[username] = entries;
        }

        return result;
    }

    public static string GetCommunityTermbasesDir(string repoRoot)
        => Path.Combine(repoRoot, "community", "termbases");

    private static string SanitizeFilename(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var sb = new StringBuilder(name.Length);
        foreach (var ch in name)
        {
            if (Array.IndexOf(invalid, ch) < 0 && ch != '.' && ch != ' ')
                sb.Append(ch);
        }
        return sb.Length > 0 ? sb.ToString() : "unknown";
    }

    public static string GetPath(string root)
    {
        return Path.Combine(root, "termbase.json");
    }
}