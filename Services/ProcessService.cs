// Services/ProcessService.cs
// Loads and caches process.json files for critical-edition workflow display.
// Cache key is (directory path, mtime ticks) — same pattern as ManifestService.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;
using ReadZen.App.Models;

namespace ReadZen.App.Services;

public interface IProcessService
{
    /// <summary>
    /// Attempts to load process.json from the same directory as the given XML file.
    /// Returns null if no file exists or if parsing fails (graceful degradation).
    /// </summary>
    ProcessInfo? TryLoad(string xmlAbsPath);
}

public sealed class ProcessService : IProcessService
{
    private readonly ConcurrentDictionary<string, (long mtimeTicks, ProcessInfo? info)> _cache
        = new(StringComparer.OrdinalIgnoreCase);

    public ProcessInfo? TryLoad(string xmlAbsPath)
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
            var info = JsonSerializer.Deserialize<ProcessInfo>(json);
            _cache[cacheKey] = (ticks, info);
            return info;
        }
        catch
        {
            _cache[cacheKey] = (ticks, null);
            return null;
        }
    }

    /// <summary>
    /// Resolves the process.json path: first checks the manifest's process_file pointer,
    /// then falls back to process.json in the same directory.
    /// </summary>
    private static string? ResolveFilePath(string dir, string xmlAbsPath)
    {
        // Try manifest pointer first
        try
        {
            var manifestPath = Path.Combine(dir, "manifest.json");
            if (File.Exists(manifestPath))
            {
                var manifestJson = File.ReadAllText(manifestPath);
                var manifest = JsonSerializer.Deserialize<ManifestInfo>(manifestJson);
                if (!string.IsNullOrEmpty(manifest?.ProcessFile))
                {
                    var pointed = Path.Combine(dir, manifest.ProcessFile);
                    if (File.Exists(pointed))
                        return pointed;
                }
            }
        }
        catch { /* fall through to default */ }

        return Path.Combine(dir, "process.json");
    }

    private static long GetMtimeTicks(string path)
    {
        try { return new FileInfo(path).LastWriteTimeUtc.Ticks; }
        catch { return 0L; }
    }
}
