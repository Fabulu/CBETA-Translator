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

public sealed class MasterDatesService : IMasterDatesService
{
    private static readonly JsonSerializerOptions ReadOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly JsonSerializerOptions CompactOpts = new()
    {
        WriteIndented = false,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public async Task WriteMasterDatesJsonlAsync(string communityDir, string username, List<MasterDateEntry> entries, CancellationToken ct = default)
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

    public async Task<Dictionary<string, List<MasterDateEntry>>> LoadAllCommunityMasterDatesAsync(string communityDir, CancellationToken ct = default)
    {
        var result = new Dictionary<string, List<MasterDateEntry>>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(communityDir) || !Directory.Exists(communityDir))
            return result;

        foreach (var file in Directory.GetFiles(communityDir, "*.jsonl"))
        {
            ct.ThrowIfCancellationRequested();

            var username = Path.GetFileNameWithoutExtension(file);
            var entries = new List<MasterDateEntry>();

            var lines = await File.ReadAllLinesAsync(file, Encoding.UTF8, ct);
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                try
                {
                    var e = JsonSerializer.Deserialize<MasterDateEntry>(line, ReadOpts);
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

    /// <summary>
    /// Loads the base master-dates.json and returns a set of all known base names (trimmed).
    /// </summary>
    public static HashSet<string> LoadBaseNameSet()
    {
        var names = new HashSet<string>(StringComparer.Ordinal);

        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Assets", "Data", "master-dates.json");
            if (!File.Exists(path))
                return names;

            var json = File.ReadAllText(path);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("masters", out var mastersEl))
                return names;

            foreach (var master in mastersEl.EnumerateArray())
            {
                if (master.TryGetProperty("names", out var namesEl))
                {
                    foreach (var nameEl in namesEl.EnumerateArray())
                    {
                        var name = nameEl.GetString()?.Trim();
                        if (!string.IsNullOrEmpty(name))
                            names.Add(name);
                    }
                }
            }
        }
        catch
        {
            // Ignore parse errors
        }

        return names;
    }

    /// <summary>
    /// Checks whether a community entry overlaps with a base entry by sharing any name.
    /// Uses exact comparison for names containing CJK characters (2+ CJK chars minimum),
    /// and case-insensitive comparison for pinyin names.
    /// </summary>
    public static bool OverlapsWithBase(MasterDateEntry entry, HashSet<string> baseNames)
    {
        foreach (var name in entry.Names)
        {
            var trimmed = name.Trim();
            if (string.IsNullOrEmpty(trimmed)) continue;

            if (ContainsCjk(trimmed))
            {
                // Require at least 2 CJK characters to avoid false positives
                int cjkCount = trimmed.Count(c => IsCjk(c));
                if (cjkCount < 2) continue;

                if (baseNames.Contains(trimmed))
                    return true;
            }
            else
            {
                // Pinyin: case-insensitive match
                foreach (var baseName in baseNames)
                {
                    if (string.Equals(trimmed, baseName, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Checks whether two community entries refer to the same master (share any name).
    /// </summary>
    public static bool SharesAnyName(MasterDateEntry a, MasterDateEntry b)
    {
        foreach (var nameA in a.Names)
        {
            var trimmedA = nameA.Trim();
            if (string.IsNullOrEmpty(trimmedA)) continue;

            foreach (var nameB in b.Names)
            {
                var trimmedB = nameB.Trim();
                if (string.IsNullOrEmpty(trimmedB)) continue;

                if (ContainsCjk(trimmedA) && ContainsCjk(trimmedB))
                {
                    int cjkCountA = trimmedA.Count(c => IsCjk(c));
                    int cjkCountB = trimmedB.Count(c => IsCjk(c));
                    if (cjkCountA < 2 || cjkCountB < 2) continue;

                    if (string.Equals(trimmedA, trimmedB, StringComparison.Ordinal))
                        return true;
                }
                else
                {
                    if (string.Equals(trimmedA, trimmedB, StringComparison.OrdinalIgnoreCase))
                        return true;
                }
            }
        }

        return false;
    }

    private static bool ContainsCjk(string s) => s.Any(c => IsCjk(c));

    private static bool IsCjk(char c) =>
        c >= '\u4E00' && c <= '\u9FFF' ||
        c >= '\u3400' && c <= '\u4DBF' ||
        c >= '\uF900' && c <= '\uFAFF';

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
