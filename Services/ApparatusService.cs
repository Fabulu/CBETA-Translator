// Services/ApparatusService.cs
// Loads and caches apparatus.json files for critical-apparatus display.
// Cache key is (file path, mtime ticks) — same pattern as ManifestService.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;
using ReadZen.App.Models;

namespace ReadZen.App.Services;

public interface IApparatusService
{
    /// <summary>
    /// Attempts to load apparatus.json from the same directory as the given XML file.
    /// Returns null if no file exists or if parsing fails (graceful degradation).
    /// </summary>
    ApparatusInfo? TryLoad(string xmlAbsPath);
}

public sealed class ApparatusService : IApparatusService
{
    private readonly ConcurrentDictionary<string, (long mtimeTicks, ApparatusInfo? info)> _cache
        = new(StringComparer.OrdinalIgnoreCase);

    public ApparatusInfo? TryLoad(string xmlAbsPath)
    {
        if (string.IsNullOrWhiteSpace(xmlAbsPath))
            return null;

        string? dir;
        try { dir = Path.GetDirectoryName(xmlAbsPath); }
        catch { return null; }

        if (string.IsNullOrEmpty(dir))
            return null;

        var filePath = ResolveFilePath(dir, xmlAbsPath);
        if (filePath == null || !File.Exists(filePath))
            return null;

        long ticks = GetMtimeTicks(filePath);
        string cacheKey = filePath;

        if (_cache.TryGetValue(cacheKey, out var entry) && entry.mtimeTicks == ticks)
            return entry.info;

        try
        {
            var json = File.ReadAllText(filePath);
            var info = JsonSerializer.Deserialize<ApparatusInfo>(json);
            _cache[cacheKey] = (ticks, info);
            return info;
        }
        catch
        {
            _cache[cacheKey] = (ticks, null);
            return null;
        }
    }

    private static string? ResolveFilePath(string dir, string xmlAbsPath)
    {
        try
        {
            var manifestPath = Path.Combine(dir, "manifest.json");
            if (File.Exists(manifestPath))
            {
                var manifestJson = File.ReadAllText(manifestPath);
                var manifest = JsonSerializer.Deserialize<ManifestInfo>(manifestJson);
                if (!string.IsNullOrEmpty(manifest?.ApparatusFile))
                {
                    var pointed = Path.Combine(dir, manifest.ApparatusFile);
                    if (File.Exists(pointed))
                        return pointed;
                }
            }
        }
        catch { /* fall through to default */ }

        return Path.Combine(dir, "apparatus.json");
    }

    private static long GetMtimeTicks(string path)
    {
        try { return new FileInfo(path).LastWriteTimeUtc.Ticks; }
        catch { return 0L; }
    }
}
