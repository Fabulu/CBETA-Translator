using CbetaTranslator.App.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace CbetaTranslator.App.Services;

public sealed class ScholarCollectionsService : IScholarCollectionsService
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

    private static readonly JsonSerializerOptions CompactOpts = new()
    {
        WriteIndented = false,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public async Task<List<ScholarCollection>> LoadAsync(string root, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(root))
            throw new ArgumentException("Root is required.", nameof(root));

        var path = GetPath(root);
        if (!File.Exists(path))
            return new List<ScholarCollection>();

        string json = await File.ReadAllTextAsync(path, Encoding.UTF8, ct);
        if (string.IsNullOrWhiteSpace(json))
            return new List<ScholarCollection>();

        var collections = JsonSerializer.Deserialize<List<ScholarCollection>>(json, ReadOpts)
                          ?? new List<ScholarCollection>();

        return collections;
    }

    public async Task SaveAsync(string root, List<ScholarCollection> collections, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(root))
            throw new ArgumentException("Root is required.", nameof(root));

        if (collections == null)
            throw new ArgumentNullException(nameof(collections));

        var path = GetPath(root);
        Directory.CreateDirectory(root);

        var json = JsonSerializer.Serialize(collections, WriteOpts);
        var tmpPath = path + ".tmp";
        await File.WriteAllTextAsync(tmpPath, json, new UTF8Encoding(false), ct);
        File.Move(tmpPath, path, overwrite: true);
    }

    public async Task ExportAsync(string filePath, List<ScholarCollection> collections, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path is required.", nameof(filePath));

        if (collections == null)
            throw new ArgumentNullException(nameof(collections));

        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrWhiteSpace(dir))
            Directory.CreateDirectory(dir);

        var json = JsonSerializer.Serialize(collections, WriteOpts);
        var tmpPath = filePath + ".tmp";
        await File.WriteAllTextAsync(tmpPath, json, new UTF8Encoding(false), ct);
        File.Move(tmpPath, filePath, overwrite: true);
    }

    public async Task<List<ScholarCollection>> ImportAsync(string filePath, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            throw new ArgumentException("File path is required.", nameof(filePath));

        if (!File.Exists(filePath))
            return new List<ScholarCollection>();

        string json = await File.ReadAllTextAsync(filePath, Encoding.UTF8, ct);
        if (string.IsNullOrWhiteSpace(json))
            return new List<ScholarCollection>();

        return JsonSerializer.Deserialize<List<ScholarCollection>>(json, ReadOpts)
               ?? new List<ScholarCollection>();
    }

    public async Task WriteUserJsonlAsync(string communityDir, string username, List<ScholarCollection> collections, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(communityDir))
            throw new ArgumentException("Community directory is required.", nameof(communityDir));
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username is required.", nameof(username));
        if (collections == null)
            throw new ArgumentNullException(nameof(collections));

        Directory.CreateDirectory(communityDir);

        // Sanitize username to prevent path traversal
        var safeUsername = SanitizeFilename(username);
        var path = Path.Combine(communityDir, safeUsername + ".jsonl");
        var fullPath = Path.GetFullPath(path);
        var fullDir = Path.GetFullPath(communityDir);
        if (!fullPath.StartsWith(fullDir, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Username produces a path outside the community directory.", nameof(username));
        var sb = new StringBuilder();

        foreach (var c in collections)
        {
            sb.AppendLine(JsonSerializer.Serialize(c, CompactOpts));
        }

        var tmpPath = path + ".tmp";
        await File.WriteAllTextAsync(tmpPath, sb.ToString(), new UTF8Encoding(false), ct);
        File.Move(tmpPath, path, overwrite: true);
    }

    public async Task<Dictionary<string, List<ScholarCollection>>> LoadAllCommunityJsonlAsync(string communityDir, CancellationToken ct = default)
    {
        var result = new Dictionary<string, List<ScholarCollection>>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(communityDir) || !Directory.Exists(communityDir))
            return result;

        foreach (var file in Directory.GetFiles(communityDir, "*.jsonl"))
        {
            ct.ThrowIfCancellationRequested();

            var username = Path.GetFileNameWithoutExtension(file);
            var collections = new List<ScholarCollection>();

            var lines = await File.ReadAllLinesAsync(file, Encoding.UTF8, ct);
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                try
                {
                    var c = JsonSerializer.Deserialize<ScholarCollection>(line, ReadOpts);
                    if (c != null)
                        collections.Add(c);
                }
                catch
                {
                    // Skip malformed lines
                }
            }

            if (collections.Count > 0)
                result[username] = collections;
        }

        return result;
    }

    public static string GetCommunityCollectionsDir(string repoRoot)
        => Path.Combine(repoRoot, "community", "collections");

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
        return Path.Combine(root, "scholar-collections.json");
    }
}
