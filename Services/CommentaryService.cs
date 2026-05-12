// Services/CommentaryService.cs
// Loads and caches commentary.json files for FiM / critical-edition
// commentary surfaces. Mirrors ApparatusService (mtime-cached, manifest
// pointer aware, graceful null on missing/parse failure), but adds an
// optional language whitelist applied per-call from the cached list.
//
// Reader call sites pass allowedLanguages (e.g. ["zh-Hant","zh-Hans"]);
// provenance/admin call sites pass null and see everything.
//
// Default-deny posture: entries with Language == null or "unknown" are
// NEVER returned when a non-empty allowedLanguages filter is in effect.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using ReadZen.App.Models;

namespace ReadZen.App.Services;

public interface ICommentaryService
{
    /// <summary>
    /// Attempts to load commentary.json sibling to <paramref name="xmlAbsPath"/>
    /// (or via <c>manifest.json.commentary_file</c> pointer when set).
    /// When <paramref name="allowedLanguages"/> is non-null AND non-empty,
    /// entries whose <c>Language</c> matches any tag in the whitelist
    /// (case-insensitive, BCP-47 prefix match) are returned. Entries with
    /// <c>Language == null</c> or <c>Language == "unknown"</c> are excluded
    /// from a filtered result (default-deny posture — items must be
    /// positively identified to surface).
    /// When <paramref name="allowedLanguages"/> is null or empty, all
    /// entries are returned unfiltered (provenance / admin / research path).
    /// Returns null when no commentary file exists or parsing fails
    /// (graceful degradation). Note: null means "edition has no commentary
    /// surface"; an empty <c>Entries</c> list means "edition opted in but
    /// no matching entries after filter."
    ///
    /// Side effect: entries whose <c>Language</c> field is null/empty on
    /// disk are classified via <see cref="CommentaryLanguageClassifier"/>
    /// before the filter is applied, and the inferred BCP-47 tag is
    /// written into the cached entry's <c>Language</c>. The full
    /// <see cref="LanguageTag"/> (with Source + Evidence) is stashed in
    /// an internal side map; admin surfaces can retrieve it via
    /// <see cref="GetInferenceTag"/>.
    /// </summary>
    CommentaryInfo? TryLoad(string xmlAbsPath, IEnumerable<string>? allowedLanguages = null);

    /// <summary>
    /// Returns the language-inference provenance for the entry with the
    /// given <paramref name="commentaryId"/>, or <c>null</c> if the entry's
    /// <c>Language</c> was explicit on disk (no inference needed) or the
    /// id is unknown to this service. Useful for admin surfaces that want
    /// to render "Language: ja (inferred from kana in body, count=147)".
    /// The map is rebuilt whenever the cache regenerates (mtime change).
    /// </summary>
    LanguageTag? GetInferenceTag(string commentaryId);
}

public sealed class CommentaryService : ICommentaryService
{
    private readonly ConcurrentDictionary<string, (long mtimeTicks, CommentaryInfo? info)> _cache
        = new(StringComparer.OrdinalIgnoreCase);

    // Side map: commentary_id → LanguageTag, populated by the classifier
    // pass on cache miss for entries whose Language field was missing on
    // disk. Rebuilt atomically when the cache regenerates (mtime change)
    // so its lifetime matches the underlying parsed CommentaryInfo.
    private readonly ConcurrentDictionary<string, LanguageTag> _inferenceTags
        = new(StringComparer.Ordinal);

    public CommentaryInfo? TryLoad(string xmlAbsPath, IEnumerable<string>? allowedLanguages = null)
    {
        if (string.IsNullOrWhiteSpace(xmlAbsPath))
            return null;

        string? dir;
        try { dir = Path.GetDirectoryName(xmlAbsPath); }
        catch { return null; }

        if (string.IsNullOrEmpty(dir))
            return null;

        var filePath = ResolveFilePath(dir);
        if (filePath == null || !File.Exists(filePath))
            return null;

        long ticks = GetMtimeTicks(filePath);
        string cacheKey = filePath;

        CommentaryInfo? unfiltered;
        if (_cache.TryGetValue(cacheKey, out var entry) && entry.mtimeTicks == ticks)
        {
            unfiltered = entry.info;
        }
        else
        {
            try
            {
                var json = File.ReadAllText(filePath);
                unfiltered = JsonSerializer.Deserialize<CommentaryInfo>(json);

                // Run the language classifier BEFORE caching so the cached
                // CommentaryInfo's entries have inferred Language values
                // filled in. The side map for this file is rebuilt now
                // (clearing stale tags from a prior mtime).
                ClassifyAndPopulateTags(unfiltered);

                _cache[cacheKey] = (ticks, unfiltered);
            }
            catch
            {
                _cache[cacheKey] = (ticks, null);
                return null;
            }
        }

        if (unfiltered == null)
            return null;

        return ApplyLanguageFilter(unfiltered, allowedLanguages);
    }

    public LanguageTag? GetInferenceTag(string commentaryId)
    {
        if (string.IsNullOrEmpty(commentaryId))
            return null;

        return _inferenceTags.TryGetValue(commentaryId, out var tag) ? tag : null;
    }

    private void ClassifyAndPopulateTags(CommentaryInfo? info)
    {
        // Clear the entire side map on cache regeneration. We don't have
        // a clean way to scope side-map entries per-file (commentary_id
        // is package-wide), so we accept that re-classification of one
        // file's commentary invalidates inference tags from other files.
        // In practice the desktop loads commentary one file at a time and
        // the side map is consulted with the same id space, so this is fine.
        _inferenceTags.Clear();

        if (info?.Entries == null)
            return;

        foreach (var entry in info.Entries)
        {
            if (entry == null)
                continue;

            // Only run the classifier when the entry has no explicit
            // language declaration. Entries with Language already set
            // skip classification entirely (per spec: GetInferenceTag
            // returns null for explicit-metadata entries).
            if (!string.IsNullOrWhiteSpace(entry.Language))
                continue;

            var tag = CommentaryLanguageClassifier.Classify(entry);

            // Mutate the cached entry's Language so the downstream filter
            // sees an inferred tag to compare against (the filter rejects
            // null/whitespace, so without this step inferred "ja" / "zh-Hant"
            // entries would silently fall out of every reader call).
            entry.Language = tag.Bcp47;

            if (!string.IsNullOrEmpty(entry.CommentaryId))
                _inferenceTags[entry.CommentaryId!] = tag;
        }
    }

    // Tags that may NEVER appear in a reader-side whitelist, regardless of what
    // an edition's manifest declares. Locked in 2026-05-12 per the user's "make
    // sure we don't show the Japanese commentary, only the Chinese" directive
    // and the Wave 4 QA foot-gun finding: a curator could otherwise accidentally
    // (or maliciously) surface Japanese commentary by adding "ja" to a manifest.
    // Compared case-insensitively against the FULL tag and against the subtag
    // prefix before any hyphen — so "ja", "JA", "ja-JP", "jpn", "ja-Latn-x-foo"
    // are all dropped.
    private static readonly string[] BlockedReaderLanguagePrimaryTags = { "ja", "jpn" };

    private static bool IsBlockedReaderLanguageTag(string tag)
    {
        if (string.IsNullOrWhiteSpace(tag)) return false;
        var trimmed = tag.Trim();
        var dash = trimmed.IndexOf('-');
        var primary = dash >= 0 ? trimmed.Substring(0, dash) : trimmed;
        foreach (var blocked in BlockedReaderLanguagePrimaryTags)
        {
            if (string.Equals(primary, blocked, StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private static CommentaryInfo ApplyLanguageFilter(CommentaryInfo source, IEnumerable<string>? allowedLanguages)
    {
        // Materialize whitelist once; null/empty → no filter (provenance/admin path).
        List<string>? whitelist = null;
        if (allowedLanguages != null)
        {
            whitelist = new List<string>();
            foreach (var tag in allowedLanguages)
            {
                if (string.IsNullOrWhiteSpace(tag)) continue;
                // Foot-gun guard: silently drop Japanese tags from any reader
                // whitelist (see BlockedReaderLanguagePrimaryTags above).
                if (IsBlockedReaderLanguageTag(tag)) continue;
                whitelist.Add(tag);
            }
            if (whitelist.Count == 0)
                whitelist = null;
        }

        if (whitelist == null)
            return source;

        var filtered = new List<CommentaryEntry>();
        if (source.Entries != null)
        {
            foreach (var e in source.Entries)
            {
                if (MatchesWhitelist(e.Language, whitelist))
                    filtered.Add(e);
            }
        }

        return new CommentaryInfo { Entries = filtered };
    }

    private static bool MatchesWhitelist(string? entryLanguage, List<string> whitelist)
    {
        // Default-deny posture: null and "unknown" are never matched
        // even if the whitelist somehow contains "unknown" — the filter
        // is intentionally allowlist-only for positively-identified tags.
        if (string.IsNullOrWhiteSpace(entryLanguage))
            return false;
        if (string.Equals(entryLanguage, "unknown", StringComparison.OrdinalIgnoreCase))
            return false;

        foreach (var filter in whitelist)
        {
            // BCP-47 strict subtag prefix match (case-insensitive):
            //   tag == filter   OR   tag.StartsWith(filter + "-")
            // e.g. filter "zh" matches "zh", "zh-Hant", "zh-Hans"; not "zha".
            if (entryLanguage.Equals(filter, StringComparison.OrdinalIgnoreCase))
                return true;
            if (entryLanguage.StartsWith(filter + "-", StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }

    private static string? ResolveFilePath(string dir)
    {
        try
        {
            var manifestPath = Path.Combine(dir, "manifest.json");
            if (File.Exists(manifestPath))
            {
                var manifestJson = File.ReadAllText(manifestPath);
                var manifest = JsonSerializer.Deserialize<ManifestInfo>(manifestJson);
                if (!string.IsNullOrEmpty(manifest?.CommentaryFile))
                {
                    var pointed = Path.Combine(dir, manifest.CommentaryFile);
                    if (File.Exists(pointed))
                        return pointed;
                }
            }
        }
        catch { /* fall through to default */ }

        return Path.Combine(dir, "commentary.json");
    }

    private static long GetMtimeTicks(string path)
    {
        try { return new FileInfo(path).LastWriteTimeUtc.Ticks; }
        catch { return 0L; }
    }
}
