// Services/WitnessTextService.cs
// Loads and caches witness-texts.json for locus-based witness comparison.
// Follows the same cache pattern as ManifestService.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

        // Try witnesses.json first (new spec name), fall back to witness-texts.json
        var filePath = Path.Combine(dir, "witnesses.json");
        if (!File.Exists(filePath))
            filePath = Path.Combine(dir, "witness-texts.json");
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
    /// Loads a witness's locus map companion file (.loci.json).
    /// </summary>
    public WitnessLocusMap? TryLoadLocusMap(string xmlAbsPath, WitnessTextEntry witness)
    {
        if (string.IsNullOrWhiteSpace(xmlAbsPath) || string.IsNullOrWhiteSpace(witness.LocusMapFile))
            return null;

        string? dir;
        try { dir = Path.GetDirectoryName(xmlAbsPath); }
        catch { return null; }
        if (string.IsNullOrEmpty(dir)) return null;

        var filePath = Path.Combine(dir, witness.LocusMapFile);
        if (!File.Exists(filePath)) return null;

        try
        {
            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<WitnessLocusMap>(json);
        }
        catch { return null; }
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

    /// <summary>
    /// Gets all witness readings at a given locus, grouped by reading text.
    /// Returns: list of (reading, witnesses[]) — differing readings first.
    /// The adopted/critical reading should be passed as lemma to identify agreements.
    /// </summary>
    public static List<WitnessReadingGroup> GetComparisonAtLocus(
        WitnessTextRegistry? registry, ApparatusInfo? apparatus, string locusId, string? lemma = null)
    {
        var groups = new Dictionary<string, List<WitnessTextEntry>>(StringComparer.Ordinal);

        // Collect from witness registry readings
        if (registry?.Witnesses != null)
        {
            foreach (var w in registry.Witnesses)
            {
                if (w.Readings != null && w.Readings.TryGetValue(locusId, out var reading))
                {
                    if (!groups.ContainsKey(reading)) groups[reading] = new();
                    groups[reading].Add(w);
                }
            }
        }

        // Also collect from apparatus entries
        if (apparatus?.Entries != null)
        {
            var entry = apparatus.Entries.Find(e =>
                string.Equals(e.LocusId, locusId, StringComparison.OrdinalIgnoreCase));

            if (entry?.Readings != null)
            {
                foreach (var r in entry.Readings)
                {
                    if (string.IsNullOrEmpty(r.Reading)) continue;
                    if (!groups.ContainsKey(r.Reading)) groups[r.Reading] = new();

                    // Add a stub entry if not already from registry
                    if (!groups[r.Reading].Any(w =>
                        string.Equals(w.WitnessId, r.WitnessId, StringComparison.OrdinalIgnoreCase)))
                    {
                        groups[r.Reading].Add(new WitnessTextEntry
                        {
                            WitnessId = r.WitnessId,
                            Siglum = r.WitnessId,
                            Confidence = r.Certainty,
                            HasOcr = r.IsOcrOnly == true,
                            HasHumanCheck = r.IsHumanChecked == true,
                        });
                    }
                }
            }
        }

        // Build result: differing readings first, agreements last
        var result = new List<WitnessReadingGroup>();

        foreach (var (reading, witnesses) in groups.OrderBy(g =>
            string.Equals(g.Key, lemma, StringComparison.Ordinal) ? 1 : 0)) // non-lemma first
        {
            result.Add(new WitnessReadingGroup
            {
                Reading = reading,
                IsLemma = string.Equals(reading, lemma, StringComparison.Ordinal),
                Witnesses = witnesses,
            });
        }

        return result;
    }
}

/// <summary>A group of witnesses sharing the same reading at a locus.</summary>
public sealed class WitnessReadingGroup
{
    public string Reading { get; set; } = "";
    public bool IsLemma { get; set; }
    public List<WitnessTextEntry> Witnesses { get; set; } = new();
}
