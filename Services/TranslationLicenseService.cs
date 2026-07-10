// Services/TranslationLicenseService.cs
// Loads and saves per-user, per-file translation license choices.
// Storage: community/translation-licenses/{username}.jsonl in the translations repo.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ReadZen.App.Models;

namespace ReadZen.App.Services;

public sealed class TranslationLicenseService
{
    private const string CommunitySubdir = "community/translation-licenses";

    private static readonly JsonSerializerOptions ReadOpts = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly JsonSerializerOptions CompactOpts = new()
    {
        WriteIndented = false,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    // In-memory cache: relPath → latest license (loaded from current user's JSONL)
    private readonly ConcurrentDictionary<string, TranslationLicenseInfo> _cache = new(StringComparer.OrdinalIgnoreCase);
    private string? _loadedUsername;
    private string? _loadedRepoRoot;

    /// <summary>
    /// Returns the community/translation-licenses directory for a given repo root.
    /// </summary>
    public static string GetLicenseDir(string repoRoot)
        => Path.Combine(repoRoot, CommunitySubdir.Replace('/', Path.DirectorySeparatorChar));

    /// <summary>
    /// Loads all license choices for a specific user from their JSONL file.
    /// Caches in memory for fast lookups.
    /// </summary>
    public async Task LoadUserLicensesAsync(string repoRoot, string username, CancellationToken ct = default)
    {
        _cache.Clear();
        _loadedUsername = username;
        _loadedRepoRoot = repoRoot;

        var dir = GetLicenseDir(repoRoot);
        if (!Directory.Exists(dir)) return;

        var safeName = SanitizeFilename(username);
        var path = Path.Combine(dir, safeName + ".jsonl");
        if (!File.Exists(path)) return;

        var lines = await File.ReadAllLinesAsync(path, Encoding.UTF8, ct);
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            try
            {
                var info = JsonSerializer.Deserialize<TranslationLicenseInfo>(line, ReadOpts);
                if (info?.RelPath != null)
                    _cache[NormalizeRel(info.RelPath)] = info;
            }
            catch { /* skip malformed lines */ }
        }
    }

    /// <summary>
    /// Gets the license for a specific file, or null if not yet chosen.
    /// </summary>
    public TranslationLicenseInfo? GetLicense(string relPath)
    {
        _cache.TryGetValue(NormalizeRel(relPath), out var info);
        return info;
    }

    /// <summary>
    /// Gets the effective license — chosen by the user, or the default for the source.
    /// Never returns null; falls back to a sensible default.
    /// </summary>
    public TranslationLicenseInfo GetEffectiveLicense(string relPath, string? sourceLicense, CorpusKind corpus)
    {
        var chosen = GetLicense(relPath);
        if (chosen != null) return chosen;

        // Auto-default for CBETA
        var defaultOpt = LicenseCatalog.GetDefault(sourceLicense, corpus);
        if (defaultOpt != null)
        {
            return new TranslationLicenseInfo
            {
                RelPath = relPath,
                License = defaultOpt.Id,
                LicenseUrl = defaultOpt.Url,
                CommercialUseAllowed = defaultOpt.CommercialOk,
                AttributionRequired = defaultOpt.AttributionRequired,
                ShareAlikeRequired = defaultOpt.ShareAlikeRequired,
            };
        }

        // No default, no choice → no license info
        return new TranslationLicenseInfo { RelPath = relPath };
    }

    /// <summary>
    /// Saves a license choice for a specific file. Appends to the user's JSONL.
    /// </summary>
    public async Task SaveLicenseAsync(string repoRoot, string username, TranslationLicenseInfo license, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(license.RelPath)) return;

        license.Username = username;
        license.ChosenUtc = DateTime.UtcNow.ToString("o");

        var dir = GetLicenseDir(repoRoot);
        Directory.CreateDirectory(dir);

        var safeName = SanitizeFilename(username);
        var path = Path.Combine(dir, safeName + ".jsonl");

        // Security: verify path stays inside the directory
        var fullPath = Path.GetFullPath(path);
        var fullDir = Path.GetFullPath(dir);
        if (!fullPath.StartsWith(fullDir, StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Username produces a path outside the license directory.", nameof(username));

        // Update cache
        _cache[NormalizeRel(license.RelPath)] = license;

        // Rewrite the full file (replace existing entry for same relPath, append new)
        var allEntries = _cache.Values.OrderBy(e => e.RelPath, StringComparer.OrdinalIgnoreCase).ToList();
        var sb = new StringBuilder();
        foreach (var entry in allEntries)
            sb.AppendLine(JsonSerializer.Serialize(entry, CompactOpts));

        await File.WriteAllTextAsync(path, sb.ToString(), new UTF8Encoding(false), ct);
    }

    /// <summary>
    /// Loads ALL users' license data (for display when viewing other people's translations).
    /// Returns a dictionary keyed by (normalizedRelPath, username).
    /// </summary>
    public async Task<Dictionary<(string RelPath, string Username), TranslationLicenseInfo>> LoadAllCommunityLicensesAsync(
        string repoRoot, CancellationToken ct = default)
    {
        var result = new Dictionary<(string, string), TranslationLicenseInfo>();
        var dir = GetLicenseDir(repoRoot);
        if (!Directory.Exists(dir)) return result;

        foreach (var file in Directory.EnumerateFiles(dir, "*.jsonl"))
        {
            ct.ThrowIfCancellationRequested();
            var username = Path.GetFileNameWithoutExtension(file);
            try
            {
                var lines = await File.ReadAllLinesAsync(file, Encoding.UTF8, ct);
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    try
                    {
                        var info = JsonSerializer.Deserialize<TranslationLicenseInfo>(line, ReadOpts);
                        if (info?.RelPath != null)
                            result[(NormalizeRel(info.RelPath), username)] = info;
                    }
                    catch { }
                }
            }
            catch { }
        }

        return result;
    }

    // Intentionally NOT routed to RelPath.Normalize: this variant is Replace-only
    // (no null guard, no TrimStart) and must preserve leading slashes in cache keys.
    private static string NormalizeRel(string rel) => rel.Replace('\\', '/');

    private static string SanitizeFilename(string name)
        => ReadZen.App.Infrastructure.FileNameSanitizer.Lenient(name);
}
