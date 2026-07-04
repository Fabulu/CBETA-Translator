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
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using ReadZen.App.Infrastructure;
using ReadZen.App.Models;

namespace ReadZen.App.Services;

public sealed class SegmentMapService : ISegmentMapService
{
    // Cache keys on BOTH the jsonl mtime AND the source XML mtime: a stale map whose
    // source changed underneath it (the jsonl file itself untouched) must be
    // re-verified, so the source mtime has to participate (audit P3.1b).
    private readonly ConcurrentDictionary<string, (long jsonlTicks, long sourceTicks, SegmentMap? map)> _cache
        = new(StringComparer.OrdinalIgnoreCase);

    public SegmentMap? TryLoad(string xmlAbsPath)
    {
        if (string.IsNullOrWhiteSpace(xmlAbsPath))
            return null;

        var jsonlPath = ResolveJsonlPath(xmlAbsPath);
        if (jsonlPath == null || !File.Exists(jsonlPath))
            return null;

        long jsonlTicks = GetMtimeTicks(jsonlPath);
        long sourceTicks = GetMtimeTicks(xmlAbsPath);
        string cacheKey = jsonlPath;

        if (_cache.TryGetValue(cacheKey, out var entry)
            && entry.jsonlTicks == jsonlTicks
            && entry.sourceTicks == sourceTicks)
            return entry.map;

        SegmentMap? map;
        try
        {
            map = ParseJsonl(jsonlPath);
        }
        catch
        {
            _cache[cacheKey] = (jsonlTicks, sourceTicks, null);
            return null;
        }

        // Staleness contract (audit P3.1b): a map that records the source hash it was
        // built from is refused when the current source XML no longer matches. Maps
        // without an embedded hash (older generator) load unchanged.
        if (map != null && IsMapStale(map, xmlAbsPath))
            map = null;

        _cache[cacheKey] = (jsonlTicks, sourceTicks, map);
        return map;
    }

    /// <summary>
    /// True when the map carries a source hash and the current source XML at
    /// <paramref name="xmlAbsPath"/> no longer matches it. A map with no embedded hash
    /// is never considered stale (backward compatibility).
    /// </summary>
    internal static bool IsMapStale(SegmentMap map, string xmlAbsPath)
    {
        if (string.IsNullOrEmpty(map.SourceSha256))
            return false;

        var current = TryComputeSourceHash(xmlAbsPath);
        return current != null
            && !string.Equals(current, map.SourceSha256, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// SHA-256 (lowercase hex) of the source XML's line-ending-normalized UTF-8
    /// content. Must stay byte-compatible with the generator's
    /// <c>sourceContentHash</c> in eng/tools/build-structural-segments.js — both
    /// normalize CRLF/CR to LF before hashing so line-ending drift is not treated as
    /// a source change. Returns null when the file cannot be read.
    /// </summary>
    internal static string? TryComputeSourceHash(string xmlAbsPath)
    {
        try
        {
            var text = File.ReadAllText(xmlAbsPath);
            var normalized = text.Replace("\r\n", "\n").Replace("\r", "\n");
            using var sha = SHA256.Create();
            var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(normalized));
            return Convert.ToHexString(bytes).ToLowerInvariant();
        }
        catch
        {
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
        string? sourceHash = null;
        bool firstContentLine = true;

        foreach (var line in File.ReadLines(jsonlPath))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            // The optional FIRST content line is a metadata header, e.g.
            // {"source_sha256":"...","schema":"seg-v1"} (audit P3.1b). It carries the
            // source-XML hash and is NOT a segment.
            if (firstContentLine)
            {
                firstContentLine = false;
                var meta = TryReadMetaHeader(line);
                if (meta != null)
                {
                    sourceHash = meta;
                    continue;
                }
            }

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
            ? new SegmentMap(segments, byLbId, sourceHash)
            : null;
    }

    /// <summary>
    /// If <paramref name="line"/> is the metadata header (has a <c>source_sha256</c>
    /// string and no <c>unit_id</c>), returns the hash; otherwise null (the line is a
    /// normal segment).
    /// </summary>
    private static string? TryReadMetaHeader(string line)
    {
        try
        {
            using var doc = JsonDocument.Parse(line);
            if (doc.RootElement.ValueKind == JsonValueKind.Object
                && doc.RootElement.TryGetProperty("source_sha256", out var h)
                && h.ValueKind == JsonValueKind.String
                && !doc.RootElement.TryGetProperty("unit_id", out _))
            {
                return h.GetString();
            }
        }
        catch
        {
            // Not JSON / not a header — treat as a normal (possibly malformed) line.
        }
        return null;
    }

    private static long GetMtimeTicks(string path)
    {
        try { return new FileInfo(path).LastWriteTimeUtc.Ticks; }
        catch { return 0L; }
    }
}
