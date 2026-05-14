// Services/SegmentMapService.cs
// Loads and caches .segments.jsonl files for semantic segment overlays.
// Cache key is (file path, mtime ticks) — same mtime-caching pattern as
// CommentaryService and ApparatusService.
//
// Path convention: given an XML source path like
//   .../CbetaZenTexts/xml-p5/T/T47/T47n1987A.xml
// the segment map lives at
//   .../CbetaZenTranslations/segments/T/T47/T47n1987A.segments.jsonl
//
// Discovery: we find the "xml-p5" (or "xml-open") path component, extract
// the relative portion after it, walk up to the parent of both repos,
// discover the translations repo root via AppPaths, and build the path.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using ReadZen.App.Infrastructure;
using ReadZen.App.Models;

namespace ReadZen.App.Services;

public sealed class SegmentMapService : ISegmentMapService
{
    private readonly ConcurrentDictionary<string, (long mtimeTicks, SegmentMap? map)> _cache
        = new(StringComparer.OrdinalIgnoreCase);

    public SegmentMap? TryLoad(string xmlAbsPath)
    {
        if (string.IsNullOrWhiteSpace(xmlAbsPath))
            return null;

        var jsonlPath = ResolveJsonlPath(xmlAbsPath);
        if (jsonlPath == null || !File.Exists(jsonlPath))
            return null;

        long ticks = GetMtimeTicks(jsonlPath);
        string cacheKey = jsonlPath;

        if (_cache.TryGetValue(cacheKey, out var entry) && entry.mtimeTicks == ticks)
            return entry.map;

        try
        {
            var map = ParseJsonl(jsonlPath);
            _cache[cacheKey] = (ticks, map);
            return map;
        }
        catch
        {
            _cache[cacheKey] = (ticks, null);
            return null;
        }
    }

    /// <summary>
    /// Resolves the .segments.jsonl path from an XML source path.
    /// Searches for a known originals folder component ("xml-p5" or "xml-open")
    /// in the path, extracts the relative portion, discovers the translations
    /// repo root, and builds the segments path.
    /// </summary>
    internal static string? ResolveJsonlPath(string xmlAbsPath)
    {
        if (string.IsNullOrWhiteSpace(xmlAbsPath))
            return null;

        try
        {
            // Normalize to forward slashes for consistent splitting
            var normalized = xmlAbsPath.Replace('\\', '/');

            // Try each known originals folder name
            string[] origFolders = { AppPaths.OriginalFolderName, AppPaths.OpenOriginalFolderName };

            foreach (var origFolder in origFolders)
            {
                var marker = "/" + origFolder + "/";
                int idx = normalized.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
                if (idx < 0) continue;

                // Everything before the originals folder is the originals repo root
                // e.g., .../CbetaZenTexts
                var originalsRepoRoot = xmlAbsPath.Substring(0, idx);

                // The parent of both repos
                var parentRoot = Path.GetDirectoryName(originalsRepoRoot);
                if (string.IsNullOrEmpty(parentRoot)) continue;

                // Discover the translations repo root
                var translationsRepoRoot = AppPaths.GetTranslationRepoRoot(parentRoot);
                if (string.IsNullOrEmpty(translationsRepoRoot)) continue;

                // Extract relative path after the originals folder
                // e.g., "T/T47/T47n1987A.xml"
                var relativeAfterOrig = normalized.Substring(idx + marker.Length);

                // Replace .xml extension with .segments.jsonl
                if (relativeAfterOrig.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                {
                    relativeAfterOrig = relativeAfterOrig.Substring(0, relativeAfterOrig.Length - 4)
                        + ".segments.jsonl";
                }
                else
                {
                    // Not an XML file — cannot resolve
                    continue;
                }

                return Path.Combine(translationsRepoRoot, "segments", relativeAfterOrig);
            }
        }
        catch
        {
            // Graceful degradation — path parsing failed
        }

        return null;
    }

    /// <summary>
    /// Parses a .segments.jsonl file line-by-line into a SegmentMap.
    /// Malformed lines are silently skipped.
    /// </summary>
    internal static SegmentMap? ParseJsonl(string jsonlPath)
    {
        var segments = new List<SegmentInfo>();
        var byLbId = new Dictionary<string, SegmentInfo>(StringComparer.Ordinal);

        foreach (var line in File.ReadLines(jsonlPath))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            SegmentInfo? info;
            try
            {
                info = JsonSerializer.Deserialize<SegmentInfo>(line);
            }
            catch
            {
                // Skip malformed lines gracefully
                continue;
            }

            if (info == null)
                continue;

            segments.Add(info);

            // Index every lb-ID in the range to this segment
            if (info.LbRange != null)
            {
                foreach (var lbId in info.LbRange)
                {
                    if (!string.IsNullOrWhiteSpace(lbId))
                        byLbId[lbId] = info;
                }
            }
        }

        return segments.Count > 0
            ? new SegmentMap(segments, byLbId)
            : null;
    }

    private static long GetMtimeTicks(string path)
    {
        try { return new FileInfo(path).LastWriteTimeUtc.Ticks; }
        catch { return 0L; }
    }
}
