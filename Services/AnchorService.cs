// Services/AnchorService.cs
// Loads and caches anchor-base-register.jsonl and anchor-event-log.jsonl
// from provenance directories. Cache key is (file path, mtime ticks) —
// same pattern as ApparatusService / ManifestService.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using ReadZen.App.Models;

namespace ReadZen.App.Services;

public sealed class AnchorService
{
    private readonly ConcurrentDictionary<string, (long mtimeTicks, List<AnchorBase>? bases)> _baseCache
        = new(StringComparer.OrdinalIgnoreCase);
    private readonly ConcurrentDictionary<string, (long mtimeTicks, List<AnchorEvent>? events)> _eventCache
        = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Attempts to load anchor-base-register.jsonl from the edition directory
    /// or provenance path relative to the given XML file.
    /// Returns null if no file exists or if parsing fails (graceful degradation).
    /// </summary>
    public List<AnchorBase>? TryLoadBases(string xmlAbsPath)
    {
        if (string.IsNullOrWhiteSpace(xmlAbsPath))
            return null;

        string? dir;
        try { dir = Path.GetDirectoryName(xmlAbsPath); }
        catch { return null; }

        if (string.IsNullOrEmpty(dir))
            return null;

        // Check provenance path relative to edition dir
        // anchor files live in provenance/faith-in-mind/process/ relative to xml-open/ce/faith-in-mind/
        var paths = new[]
        {
            Path.Combine(dir, "anchor-base-register.jsonl"),
            Path.Combine(dir, "..", "..", "..", "provenance", Path.GetFileName(dir), "process", "anchor-base-register.jsonl")
        };

        foreach (var path in paths)
        {
            var fullPath = Path.GetFullPath(path);
            if (!File.Exists(fullPath)) continue;

            var ticks = File.GetLastWriteTimeUtc(fullPath).Ticks;
            if (_baseCache.TryGetValue(fullPath, out var cached) && cached.mtimeTicks == ticks)
                return cached.bases;

            try
            {
                var bases = ParseJsonl<AnchorBase>(fullPath);
                _baseCache[fullPath] = (ticks, bases);
                return bases;
            }
            catch
            {
                _baseCache[fullPath] = (ticks, null);
                return null;
            }
        }
        return null;
    }

    /// <summary>
    /// Attempts to load anchor-event-log.jsonl from the edition directory
    /// or provenance path relative to the given XML file.
    /// Returns null if no file exists or if parsing fails (graceful degradation).
    /// </summary>
    public List<AnchorEvent>? TryLoadEvents(string xmlAbsPath)
    {
        if (string.IsNullOrWhiteSpace(xmlAbsPath))
            return null;

        string? dir;
        try { dir = Path.GetDirectoryName(xmlAbsPath); }
        catch { return null; }

        if (string.IsNullOrEmpty(dir))
            return null;

        var paths = new[]
        {
            Path.Combine(dir, "anchor-event-log.jsonl"),
            Path.Combine(dir, "..", "..", "..", "provenance", Path.GetFileName(dir), "process", "anchor-event-log.jsonl")
        };

        foreach (var path in paths)
        {
            var fullPath = Path.GetFullPath(path);
            if (!File.Exists(fullPath)) continue;

            var ticks = File.GetLastWriteTimeUtc(fullPath).Ticks;
            if (_eventCache.TryGetValue(fullPath, out var cached) && cached.mtimeTicks == ticks)
                return cached.events;

            try
            {
                var events = ParseJsonl<AnchorEvent>(fullPath);
                // Sort by event_id for consistent ordering
                events?.Sort((a, b) => string.Compare(a.EventId, b.EventId, StringComparison.Ordinal));
                _eventCache[fullPath] = (ticks, events);
                return events;
            }
            catch
            {
                _eventCache[fullPath] = (ticks, null);
                return null;
            }
        }
        return null;
    }

    /// <summary>
    /// Finds a single anchor base by its anchor ID, or null if not found.
    /// </summary>
    public AnchorBase? GetAnchorById(List<AnchorBase>? bases, string anchorId)
    {
        if (bases == null) return null;
        foreach (var b in bases)
            if (b.AnchorId == anchorId) return b;
        return null;
    }

    /// <summary>
    /// Returns all anchor events matching the given locus ID (may be empty, never null).
    /// </summary>
    public List<AnchorEvent> GetEventsForLocus(List<AnchorEvent>? events, string locusId)
    {
        var result = new List<AnchorEvent>();
        if (events == null) return result;
        foreach (var e in events)
            if (e.LocusId == locusId) result.Add(e);
        return result;
    }

    private static List<T>? ParseJsonl<T>(string path)
    {
        var result = new List<T>();
        foreach (var line in File.ReadLines(path))
        {
            var trimmed = line.Trim();
            if (trimmed.Length == 0) continue;
            var item = JsonSerializer.Deserialize<T>(trimmed);
            if (item != null) result.Add(item);
        }
        return result.Count > 0 ? result : null;
    }
}
