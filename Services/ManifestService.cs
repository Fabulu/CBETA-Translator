// Services/ManifestService.cs
// Loads and caches OpenZenTexts manifest.json files for provenance display.
// Cache key is (directory path, mtime ticks) — same pattern as LicenseMetadataService.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;
using ReadZen.App.Models;

namespace ReadZen.App.Services;

public interface IManifestService
{
    /// <summary>
    /// Attempts to load manifest.json from the same directory as the given XML file.
    /// Returns null if no manifest exists or if parsing fails (graceful degradation).
    /// </summary>
    ManifestInfo? TryLoad(string xmlAbsPath);
}

public sealed class ManifestService : IManifestService
{
    private readonly ConcurrentDictionary<string, (long mtimeTicks, ManifestInfo? info)> _cache
        = new(StringComparer.OrdinalIgnoreCase);

    public ManifestInfo? TryLoad(string xmlAbsPath)
    {
        if (string.IsNullOrWhiteSpace(xmlAbsPath))
            return null;

        string? dir;
        try { dir = Path.GetDirectoryName(xmlAbsPath); }
        catch { return null; }

        if (string.IsNullOrEmpty(dir))
            return null;

        var manifestPath = Path.Combine(dir, "manifest.json");

        if (!File.Exists(manifestPath))
            return null;

        long ticks = GetMtimeTicks(manifestPath);

        if (_cache.TryGetValue(dir, out var entry) && entry.mtimeTicks == ticks)
            return entry.info;

        try
        {
            var json = File.ReadAllText(manifestPath);
            var info = JsonSerializer.Deserialize<ManifestInfo>(json);
            _cache[dir] = (ticks, info);
            return info;
        }
        catch
        {
            _cache[dir] = (ticks, null);
            return null;
        }
    }

    private static long GetMtimeTicks(string path)
    {
        try { return new FileInfo(path).LastWriteTimeUtc.Ticks; }
        catch { return 0L; }
    }
}
