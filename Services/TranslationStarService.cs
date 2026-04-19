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

public sealed class TranslationStarService : ITranslationStarService
{
    private static readonly JsonSerializerOptions ReadOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly JsonSerializerOptions WriteOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    /// <summary>Aggregated star counts: (FileId, Translator) → count across all users.</summary>
    private Dictionary<(string FileId, string Translator), int> _counts = new();

    /// <summary>Per-user star entries keyed by username.</summary>
    private Dictionary<string, List<StarEntry>> _userStars = new(StringComparer.OrdinalIgnoreCase);

    public async Task LoadAllStarsAsync(string communityStarsDir, CancellationToken ct)
    {
        var counts = new Dictionary<(string, string), int>();
        var userStars = new Dictionary<string, List<StarEntry>>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(communityStarsDir) || !Directory.Exists(communityStarsDir))
        {
            _counts = counts;
            _userStars = userStars;
            return;
        }

        foreach (var file in Directory.GetFiles(communityStarsDir, "*.jsonl"))
        {
            ct.ThrowIfCancellationRequested();

            var username = Path.GetFileNameWithoutExtension(file);
            var entries = new List<StarEntry>();

            var lines = await File.ReadAllLinesAsync(file, Encoding.UTF8, ct);
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                try
                {
                    var e = JsonSerializer.Deserialize<StarEntry>(line, ReadOpts);
                    if (e != null && !string.IsNullOrWhiteSpace(e.FileId) && !string.IsNullOrWhiteSpace(e.Translator))
                        entries.Add(e);
                }
                catch
                {
                    // Skip malformed lines
                }
            }

            if (entries.Count > 0)
                userStars[username] = entries;

            foreach (var e in entries)
            {
                var key = (e.FileId, e.Translator);
                counts.TryGetValue(key, out int current);
                counts[key] = current + 1;
            }
        }

        _counts = counts;
        _userStars = userStars;
    }

    public int GetStarCount(string fileId, string translator)
    {
        _counts.TryGetValue((fileId, translator), out int count);
        return count;
    }

    public string? GetMostStarredTranslator(string fileId)
    {
        string? best = null;
        int bestCount = 0;

        foreach (var kvp in _counts)
        {
            if (!string.Equals(kvp.Key.FileId, fileId, StringComparison.Ordinal))
                continue;

            if (kvp.Value > bestCount)
            {
                bestCount = kvp.Value;
                best = kvp.Key.Translator;
            }
        }

        return best;
    }

    public bool IsStarredByUser(string fileId, string translator, string username)
    {
        if (!_userStars.TryGetValue(username, out var entries))
            return false;

        return entries.Any(e =>
            string.Equals(e.FileId, fileId, StringComparison.Ordinal) &&
            string.Equals(e.Translator, translator, StringComparison.Ordinal));
    }

    public async Task SetStarAsync(string communityStarsDir, string username, string fileId, string translator, bool starred, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username is required.", nameof(username));
        if (string.IsNullOrWhiteSpace(fileId))
            throw new ArgumentException("FileId is required.", nameof(fileId));
        if (string.IsNullOrWhiteSpace(translator))
            throw new ArgumentException("Translator is required.", nameof(translator));

        // Ensure user entries list exists
        if (!_userStars.TryGetValue(username, out var entries))
        {
            entries = new List<StarEntry>();
            _userStars[username] = entries;
        }

        bool alreadyStarred = entries.Any(e =>
            string.Equals(e.FileId, fileId, StringComparison.Ordinal) &&
            string.Equals(e.Translator, translator, StringComparison.Ordinal));

        var key = (fileId, translator);

        if (starred && !alreadyStarred)
        {
            entries.Add(new StarEntry
            {
                FileId = fileId,
                Translator = translator,
                StarredUtc = DateTimeOffset.UtcNow.ToString("o")
            });

            _counts.TryGetValue(key, out int current);
            _counts[key] = current + 1;
        }
        else if (!starred && alreadyStarred)
        {
            entries.RemoveAll(e =>
                string.Equals(e.FileId, fileId, StringComparison.Ordinal) &&
                string.Equals(e.Translator, translator, StringComparison.Ordinal));

            if (_counts.TryGetValue(key, out int current))
            {
                if (current <= 1)
                    _counts.Remove(key);
                else
                    _counts[key] = current - 1;
            }
        }
        else
        {
            return; // No change needed
        }

        await WriteUserStarsJsonlAsync(communityStarsDir, username, ct);
    }

    public async Task WriteUserStarsJsonlAsync(string communityStarsDir, string username, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(communityStarsDir))
            throw new ArgumentException("Community stars directory is required.", nameof(communityStarsDir));
        if (string.IsNullOrWhiteSpace(username))
            throw new ArgumentException("Username is required.", nameof(username));

        Directory.CreateDirectory(communityStarsDir);

        var safeUsername = SanitizeFilename(username);
        var path = Path.Combine(communityStarsDir, safeUsername + ".jsonl");

        // Path traversal guard
        var fullPath = Path.GetFullPath(path);
        var fullDir = Path.GetFullPath(communityStarsDir);
        if (!fullPath.StartsWith(fullDir, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Username produces a path outside the community directory.", nameof(username));

        if (!_userStars.TryGetValue(username, out var entries) || entries.Count == 0)
        {
            // Remove file if no stars remain
            if (File.Exists(path))
                File.Delete(path);
            return;
        }

        var sb = new StringBuilder();
        foreach (var e in entries)
        {
            sb.AppendLine(JsonSerializer.Serialize(e, WriteOpts));
        }

        var tmpPath = path + ".tmp";
        await File.WriteAllTextAsync(tmpPath, sb.ToString(), new UTF8Encoding(false), ct);
        File.Move(tmpPath, path, overwrite: true);
    }

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
}
