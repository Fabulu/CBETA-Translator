// Services/TimelineService.cs
// Loads timeline.json and supports state reconstruction at any event step.
// Cache key is (path, mtime ticks) — same pattern as ManifestService.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using ReadZen.App.Models;

namespace ReadZen.App.Services;

public sealed class TimelineService
{
    private readonly ConcurrentDictionary<string, (long mtimeTicks, TimelineInfo? info)> _cache
        = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Loads timeline.json from the same directory as the XML file, or via manifest pointer.
    /// Returns null if not found.
    /// </summary>
    public TimelineInfo? TryLoad(string xmlAbsPath)
    {
        if (string.IsNullOrWhiteSpace(xmlAbsPath)) return null;

        string? dir;
        try { dir = Path.GetDirectoryName(xmlAbsPath); }
        catch { return null; }
        if (string.IsNullOrEmpty(dir)) return null;

        var filePath = ResolveFilePath(dir, xmlAbsPath);
        if (filePath == null || !File.Exists(filePath)) return null;

        long ticks = GetMtimeTicks(filePath);
        if (_cache.TryGetValue(filePath, out var entry) && entry.mtimeTicks == ticks)
            return entry.info;

        try
        {
            var json = File.ReadAllText(filePath);
            var info = JsonSerializer.Deserialize<TimelineInfo>(json);

            // Sort events by sequence (authoritative ordering)
            info?.Events?.Sort((a, b) => a.Sequence.CompareTo(b.Sequence));

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
    /// Reconstructs the edition state by replaying events from sequence 1 to the target event (inclusive).
    /// </summary>
    public static EditionState ReconstructState(List<TimelineEvent> events, int upToSequence)
    {
        var state = new EditionState();

        foreach (var evt in events.Where(e => e.Sequence <= upToSequence && e.Status == "applied"))
        {
            state.TotalEvents++;
            state.CurrentStage = evt.Stage;

            switch (evt.EventType)
            {
                case "project_started":
                    state.EditionMaturity = "draft";
                    break;

                case "witness_found":
                    if (evt.ObjectId != null && !state.AcceptedWitnesses.Contains(evt.ObjectId))
                        state.AcceptedWitnesses.Add(evt.ObjectId);
                    break;

                case "witness_rejected":
                    if (evt.ObjectId != null)
                    {
                        state.AcceptedWitnesses.Remove(evt.ObjectId);
                        if (!state.RejectedWitnesses.Contains(evt.ObjectId))
                            state.RejectedWitnesses.Add(evt.ObjectId);
                    }
                    break;

                case "witness_tier_changed":
                    if (evt.ObjectId != null && evt.StateEffects != null)
                    {
                        if (evt.StateEffects.TryGetValue("tier", out var tierObj))
                            state.WitnessTiers[evt.ObjectId] = tierObj?.ToString() ?? "unknown";
                    }
                    break;

                case "copy_text_ranked":
                    if (evt.ObjectId != null) state.CopyTextCandidate = evt.ObjectId;
                    break;

                case "copy_text_selected":
                    if (evt.ObjectId != null) state.CopyTextSelected = evt.ObjectId;
                    break;

                case "ocr_started":
                    state.OcrRunsStarted++;
                    break;

                case "ocr_finished":
                    state.OcrRunsCompleted++;
                    break;

                case "ocr_failed":
                    state.OcrRunsFailed++;
                    break;

                case "apparatus_entry_added":
                    state.ApparatusEntryCount++;
                    break;

                case "unresolved_opened":
                    if (evt.ObjectId != null && !state.UnresolvedLoci.Contains(evt.ObjectId))
                        state.UnresolvedLoci.Add(evt.ObjectId);
                    break;

                case "unresolved_closed":
                    if (evt.ObjectId != null)
                        state.UnresolvedLoci.Remove(evt.ObjectId);
                    break;

                case "publication_check_changed":
                    if (evt.StateEffects?.TryGetValue("edition_maturity", out var matObj) == true)
                        state.EditionMaturity = matObj?.ToString();
                    break;
            }
        }

        return state;
    }

    /// <summary>
    /// Computes the reading text patches needed to show the text at a given timeline position.
    /// Returns a dictionary of locus_id → reading string to substitute.
    /// Works by reverse-patching: collects all text_changed events AFTER the target position
    /// and reverses them (swapping reading_after back to reading_before).
    /// </summary>
    public static Dictionary<string, string> GetReadingPatchesAtPosition(
        TimelineInfo timeline, int upToSequence)
    {
        var patches = new Dictionary<string, string>(StringComparer.Ordinal);
        if (timeline.Events == null || timeline.Readings == null) return patches;

        // Collect text_changed events AFTER the target position, in reverse order
        var toReverse = timeline.Events
            .Where(e => e.Sequence > upToSequence
                     && e.EventType == "text_changed"
                     && e.Status == "applied"
                     && e.StateEffects != null)
            .OrderByDescending(e => e.Sequence)
            .ToList();

        foreach (var evt in toReverse)
        {
            var effects = evt.StateEffects!;
            if (!effects.TryGetValue("locus_id", out var locusObj)) continue;
            var locusId = locusObj?.ToString();
            if (string.IsNullOrEmpty(locusId)) continue;

            if (!effects.TryGetValue("reading_before", out var beforeObj)) continue;

            int beforeIdx;
            if (beforeObj is System.Text.Json.JsonElement je)
                beforeIdx = je.GetInt32();
            else if (beforeObj is int bi)
                beforeIdx = bi;
            else if (int.TryParse(beforeObj?.ToString(), out var parsed))
                beforeIdx = parsed;
            else
                continue;

            // Look up the actual reading string from the readings table
            if (!timeline.Readings.TryGetValue(locusId, out var readings)) continue;
            if (beforeIdx < 0 || beforeIdx >= readings.Count) continue;

            // The latest reverse-applied event for each locus wins
            // (we process in reverse chronological order, so the first hit is correct)
            if (!patches.ContainsKey(locusId))
                patches[locusId] = readings[beforeIdx];
        }

        return patches;
    }

    /// <summary>
    /// Returns the reading at a specific locus at the given timeline position.
    /// Returns the final reading if no text_changed events affect it after the position.
    /// </summary>
    public static string? GetReadingAtPosition(
        TimelineInfo timeline, string locusId, int upToSequence)
    {
        var patches = GetReadingPatchesAtPosition(timeline, upToSequence);
        if (patches.TryGetValue(locusId, out var patched)) return patched;

        // No patch needed — return the final reading (last index in the readings table)
        if (timeline.Readings?.TryGetValue(locusId, out var readings) == true && readings.Count > 0)
            return readings[^1];

        return null;
    }

    /// <summary>Gets all distinct stages present in the event stream, in order of first occurrence.</summary>
    public static List<string> GetStages(List<TimelineEvent> events) =>
        events.Select(e => e.Stage ?? "").Where(s => s.Length > 0).Distinct().ToList();

    /// <summary>Gets all distinct event types present in the event stream.</summary>
    public static List<string> GetEventTypes(List<TimelineEvent> events) =>
        events.Select(e => e.EventType ?? "").Where(s => s.Length > 0).Distinct().OrderBy(s => s).ToList();

    /// <summary>Gets all distinct actor types present in the event stream.</summary>
    public static List<string> GetActorTypes(List<TimelineEvent> events) =>
        events.Select(e => e.ActorType ?? "").Where(s => s.Length > 0).Distinct().OrderBy(s => s).ToList();

    /// <summary>Filters events by optional criteria.</summary>
    public static List<TimelineEvent> Filter(
        List<TimelineEvent> events,
        string? stage = null,
        string? eventType = null,
        string? actorType = null,
        string? witnessId = null,
        bool textChangingOnly = false)
    {
        IEnumerable<TimelineEvent> seq = events;

        if (!string.IsNullOrEmpty(stage))
            seq = seq.Where(e => string.Equals(e.Stage, stage, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrEmpty(eventType))
            seq = seq.Where(e => string.Equals(e.EventType, eventType, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrEmpty(actorType))
            seq = seq.Where(e => string.Equals(e.ActorType, actorType, StringComparison.OrdinalIgnoreCase));
        if (!string.IsNullOrEmpty(witnessId))
            seq = seq.Where(e => string.Equals(e.ObjectId, witnessId, StringComparison.OrdinalIgnoreCase));
        if (textChangingOnly)
            seq = seq.Where(e => e.EventType == "text_changed");

        return seq.ToList();
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
                if (!string.IsNullOrEmpty(manifest?.TimelineFile))
                {
                    var pointed = Path.Combine(dir, manifest.TimelineFile);
                    if (File.Exists(pointed)) return pointed;
                }
            }
        }
        catch { }

        return Path.Combine(dir, "timeline.json");
    }

    private static long GetMtimeTicks(string path)
    {
        try { return new FileInfo(path).LastWriteTimeUtc.Ticks; }
        catch { return 0L; }
    }
}
