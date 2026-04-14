// Services/HumanLogService.cs
// Loads and caches human-log.md for the human-readable narrative log surface.
// The log is rendered as markdown in the EditionProcessDialog Log tab.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;
using ReadZen.App.Models;

namespace ReadZen.App.Services;

public sealed class HumanLogService
{
    private readonly ConcurrentDictionary<string, (long mtimeTicks, string? content)> _cache
        = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Loads the human-readable log markdown. Looks for human-log.md via process.json pointer
    /// or in standard provenance locations.
    /// </summary>
    public string? TryLoad(string xmlAbsPath)
    {
        if (string.IsNullOrWhiteSpace(xmlAbsPath)) return null;

        string? dir;
        try { dir = Path.GetDirectoryName(xmlAbsPath); }
        catch { return null; }
        if (string.IsNullOrEmpty(dir)) return null;

        var filePath = ResolveFilePath(dir, xmlAbsPath);
        if (filePath == null || !File.Exists(filePath)) return null;

        long ticks;
        try { ticks = new FileInfo(filePath).LastWriteTimeUtc.Ticks; }
        catch { return null; }

        if (_cache.TryGetValue(filePath, out var entry) && entry.mtimeTicks == ticks)
            return entry.content;

        try
        {
            var content = File.ReadAllText(filePath);
            _cache[filePath] = (ticks, content);
            return content;
        }
        catch
        {
            _cache[filePath] = (ticks, null);
            return null;
        }
    }

    private static string? ResolveFilePath(string dir, string xmlAbsPath)
    {
        // Try process.json pointer first
        try
        {
            var processPath = Path.Combine(dir, "process.json");
            if (File.Exists(processPath))
            {
                var processJson = File.ReadAllText(processPath);
                var process = JsonSerializer.Deserialize<ProcessInfo>(processJson);
                if (!string.IsNullOrEmpty(process?.HumanLogFile))
                {
                    var pointed = Path.Combine(dir, process.HumanLogFile);
                    if (File.Exists(pointed)) return pointed;
                }
            }
        }
        catch { }

        // Try standard provenance location
        try
        {
            var manifestPath = Path.Combine(dir, "manifest.json");
            if (File.Exists(manifestPath))
            {
                var manifestJson = File.ReadAllText(manifestPath);
                var manifest = JsonSerializer.Deserialize<ManifestInfo>(manifestJson);
                if (!string.IsNullOrEmpty(manifest?.TextId))
                {
                    var slug = manifest.TextId;
                    var dotIdx = slug.IndexOf('.');
                    if (dotIdx > 0) slug = slug[(dotIdx + 1)..];

                    var repoRoot = Path.GetFullPath(Path.Combine(dir, "..", "..", ".."));
                    var logPath = Path.Combine(repoRoot, "provenance", slug, "process", "human-log.md");
                    if (File.Exists(logPath)) return logPath;
                }
            }
        }
        catch { }

        return null;
    }
}
