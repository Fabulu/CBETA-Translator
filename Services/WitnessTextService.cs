// Services/WitnessTextService.cs
// Loads and caches witness-texts.json for locus-based witness comparison.
// Follows the same cache pattern as ManifestService.

using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text.Json;
using ReadZen.App.Models;

namespace ReadZen.App.Services;

public sealed class WitnessTextService
{
    private readonly ConcurrentDictionary<string, (long mtimeTicks, WitnessTextRegistry? info)> _cache
        = new(StringComparer.OrdinalIgnoreCase);

    public WitnessTextRegistry? TryLoad(string xmlAbsPath)
    {
        if (string.IsNullOrWhiteSpace(xmlAbsPath)) return null;

        string? dir;
        try { dir = Path.GetDirectoryName(xmlAbsPath); }
        catch { return null; }
        if (string.IsNullOrEmpty(dir)) return null;

        var filePath = Path.Combine(dir, "witness-texts.json");
        if (!File.Exists(filePath)) return null;

        long ticks;
        try { ticks = new FileInfo(filePath).LastWriteTimeUtc.Ticks; }
        catch { return null; }

        if (_cache.TryGetValue(filePath, out var entry) && entry.mtimeTicks == ticks)
            return entry.info;

        try
        {
            var json = File.ReadAllText(filePath);
            var info = JsonSerializer.Deserialize<WitnessTextRegistry>(json);
            _cache[filePath] = (ticks, info);
            return info;
        }
        catch
        {
            _cache[filePath] = (ticks, null);
            return null;
        }
    }

    /// <summary>
    /// Gets the reading for a specific witness at a specific locus.
    /// Returns null if the witness doesn't have a reading for that locus.
    /// </summary>
    public static string? GetWitnessReading(WitnessTextRegistry? registry, string witnessId, string locusId)
    {
        if (registry?.Witnesses == null) return null;

        foreach (var w in registry.Witnesses)
        {
            if (string.Equals(w.WitnessId, witnessId, StringComparison.OrdinalIgnoreCase) &&
                w.Readings != null &&
                w.Readings.TryGetValue(locusId, out var reading))
            {
                return reading;
            }
        }

        return null;
    }
}
