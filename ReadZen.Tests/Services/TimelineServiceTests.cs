using System.Collections.Generic;
using ReadZen.App.Models;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

public class TimelineServiceTests
{
    // ── ReconstructState ────────────────────────────────────────────

    [Fact]
    public void ReconstructState_EmptyEvents_ReturnsEmptyState()
    {
        var state = TimelineService.ReconstructState(new List<TimelineEvent>(), 100);
        Assert.Empty(state.AcceptedWitnesses);
        Assert.Empty(state.RejectedWitnesses);
        Assert.Equal(0, state.TotalEvents);
    }

    [Fact]
    public void ReconstructState_WitnessFound_AddsToAccepted()
    {
        var events = new List<TimelineEvent>
        {
            MakeEvent(1, "witness_found", "w1", "applied"),
            MakeEvent(2, "witness_found", "w2", "applied"),
        };
        var state = TimelineService.ReconstructState(events, 2);
        Assert.Contains("w1", state.AcceptedWitnesses);
        Assert.Contains("w2", state.AcceptedWitnesses);
        Assert.Equal(2, state.TotalEvents);
    }

    [Fact]
    public void ReconstructState_WitnessRejected_MovesFromAcceptedToRejected()
    {
        var events = new List<TimelineEvent>
        {
            MakeEvent(1, "witness_found", "w1", "applied"),
            MakeEvent(2, "witness_rejected", "w1", "applied"),
        };
        var state = TimelineService.ReconstructState(events, 2);
        Assert.DoesNotContain("w1", state.AcceptedWitnesses);
        Assert.Contains("w1", state.RejectedWitnesses);
    }

    [Fact]
    public void ReconstructState_SkipsRevertedEvents()
    {
        var events = new List<TimelineEvent>
        {
            MakeEvent(1, "witness_found", "w1", "applied"),
            MakeEvent(2, "witness_found", "w2", "reverted"),
        };
        var state = TimelineService.ReconstructState(events, 2);
        Assert.Contains("w1", state.AcceptedWitnesses);
        Assert.DoesNotContain("w2", state.AcceptedWitnesses);
    }

    [Fact]
    public void ReconstructState_RespectsUpToSequence()
    {
        var events = new List<TimelineEvent>
        {
            MakeEvent(1, "witness_found", "w1", "applied"),
            MakeEvent(2, "witness_found", "w2", "applied"),
            MakeEvent(3, "witness_found", "w3", "applied"),
        };
        var state = TimelineService.ReconstructState(events, 2);
        Assert.Contains("w1", state.AcceptedWitnesses);
        Assert.Contains("w2", state.AcceptedWitnesses);
        Assert.DoesNotContain("w3", state.AcceptedWitnesses);
    }

    [Fact]
    public void ReconstructState_CopyTextSelected_SetsField()
    {
        var events = new List<TimelineEvent>
        {
            MakeEvent(1, "copy_text_selected", "w1", "applied"),
        };
        var state = TimelineService.ReconstructState(events, 1);
        Assert.Equal("w1", state.CopyTextSelected);
    }

    [Fact]
    public void ReconstructState_OcrEvents_CountsCorrectly()
    {
        var events = new List<TimelineEvent>
        {
            MakeEvent(1, "ocr_started", "run1", "applied"),
            MakeEvent(2, "ocr_finished", "run1", "applied"),
            MakeEvent(3, "ocr_started", "run2", "applied"),
            MakeEvent(4, "ocr_failed", "run2", "applied"),
        };
        var state = TimelineService.ReconstructState(events, 4);
        Assert.Equal(2, state.OcrRunsStarted);
        Assert.Equal(1, state.OcrRunsCompleted);
        Assert.Equal(1, state.OcrRunsFailed);
    }

    [Fact]
    public void ReconstructState_UnresolvedOpenClose_TracksCorrectly()
    {
        var events = new List<TimelineEvent>
        {
            MakeEvent(1, "unresolved_opened", "locus1", "applied"),
            MakeEvent(2, "unresolved_opened", "locus2", "applied"),
            MakeEvent(3, "unresolved_closed", "locus1", "applied"),
        };
        var state = TimelineService.ReconstructState(events, 3);
        Assert.Single(state.UnresolvedLoci);
        Assert.Contains("locus2", state.UnresolvedLoci);
    }

    [Fact]
    public void ReconstructState_ApparatusAdded_Counts()
    {
        var events = new List<TimelineEvent>
        {
            MakeEvent(1, "apparatus_entry_added", "a1", "applied"),
            MakeEvent(2, "apparatus_entry_added", "a2", "applied"),
        };
        var state = TimelineService.ReconstructState(events, 2);
        Assert.Equal(2, state.ApparatusEntryCount);
    }

    [Fact]
    public void ReconstructState_NullEventType_DoesNotCrash()
    {
        var events = new List<TimelineEvent>
        {
            new() { Sequence = 1, EventType = null, Status = "applied" },
        };
        var state = TimelineService.ReconstructState(events, 1);
        Assert.Equal(1, state.TotalEvents);
    }

    // ── GetReadingPatchesAtPosition ──────────────────────────────────

    [Fact]
    public void GetReadingPatches_NoTextChangedEvents_ReturnsEmpty()
    {
        var timeline = new TimelineInfo
        {
            Readings = new Dictionary<string, List<string>>(),
            Events = new List<TimelineEvent>
            {
                MakeEvent(1, "witness_found", "w1", "applied"),
            },
        };
        var patches = TimelineService.GetReadingPatchesAtPosition(timeline, 0);
        Assert.Empty(patches);
    }

    [Fact]
    public void GetReadingPatches_AtLatestEvent_ReturnsEmpty()
    {
        var timeline = MakeTimelineWithTextChange();
        // Position at or past the last event — no patches needed
        var patches = TimelineService.GetReadingPatchesAtPosition(timeline, 2);
        Assert.Empty(patches);
    }

    [Fact]
    public void GetReadingPatches_BeforeTextChange_ReturnsOriginalReading()
    {
        var timeline = MakeTimelineWithTextChange();
        // Position before the text change (seq 2) — need to reverse it
        var patches = TimelineService.GetReadingPatchesAtPosition(timeline, 1);
        Assert.Single(patches);
        Assert.Equal("趙州", patches["locus1"]);
    }

    [Fact]
    public void GetReadingPatches_NullReadings_ReturnsEmpty()
    {
        var timeline = new TimelineInfo
        {
            Readings = null,
            Events = new List<TimelineEvent>(),
        };
        var patches = TimelineService.GetReadingPatchesAtPosition(timeline, 0);
        Assert.Empty(patches);
    }

    // ── GetReadingAtPosition ────────────────────────────────────────

    [Fact]
    public void GetReadingAtPosition_AtLatest_ReturnsFinalReading()
    {
        var timeline = MakeTimelineWithTextChange();
        var reading = TimelineService.GetReadingAtPosition(timeline, "locus1", 2);
        Assert.Equal("趙州從諗", reading);
    }

    [Fact]
    public void GetReadingAtPosition_BeforeChange_ReturnsOriginal()
    {
        var timeline = MakeTimelineWithTextChange();
        var reading = TimelineService.GetReadingAtPosition(timeline, "locus1", 1);
        Assert.Equal("趙州", reading);
    }

    // ── Filter ──────────────────────────────────────────────────────

    [Fact]
    public void Filter_ByStage_ReturnsOnlyMatchingEvents()
    {
        var events = new List<TimelineEvent>
        {
            MakeEvent(1, "witness_found", "w1", "applied", "witness_search"),
            MakeEvent(2, "ocr_started", "r1", "applied", "ocr"),
        };
        var filtered = TimelineService.Filter(events, stage: "ocr");
        Assert.Single(filtered);
        Assert.Equal("ocr_started", filtered[0].EventType);
    }

    [Fact]
    public void Filter_TextChangingOnly_ReturnsOnlyTextChanged()
    {
        var events = new List<TimelineEvent>
        {
            MakeEvent(1, "witness_found", "w1", "applied"),
            MakeEvent(2, "text_changed", "locus1", "applied"),
        };
        var filtered = TimelineService.Filter(events, textChangingOnly: true);
        Assert.Single(filtered);
        Assert.Equal("text_changed", filtered[0].EventType);
    }

    // ── Helpers ──────────────────────────────────────────────────────

    private static TimelineEvent MakeEvent(int seq, string type, string objectId, string status, string? stage = null)
    {
        return new TimelineEvent
        {
            EventId = $"evt_{seq:D3}",
            Sequence = seq,
            Timestamp = "2026-04-14T12:00:00Z",
            Stage = stage ?? "project_setup",
            EventType = type,
            ObjectType = "test",
            ObjectId = objectId,
            ActorType = "test",
            ActorId = "test",
            Status = status,
        };
    }

    private static TimelineInfo MakeTimelineWithTextChange()
    {
        return new TimelineInfo
        {
            Readings = new Dictionary<string, List<string>>
            {
                ["locus1"] = new() { "趙州", "趙州從諗" },
            },
            Events = new List<TimelineEvent>
            {
                MakeEvent(1, "witness_found", "w1", "applied"),
                new()
                {
                    EventId = "evt_002", Sequence = 2, Timestamp = "2026-04-14T13:00:00Z",
                    Stage = "reading_text", EventType = "text_changed",
                    ObjectType = "reading", ObjectId = "locus1",
                    ActorType = "human", ActorId = "curator", Status = "applied",
                    StateEffects = new Dictionary<string, object>
                    {
                        ["locus_id"] = "locus1",
                        ["reading_before"] = 0,
                        ["reading_after"] = 1,
                    },
                },
            },
        };
    }
}
