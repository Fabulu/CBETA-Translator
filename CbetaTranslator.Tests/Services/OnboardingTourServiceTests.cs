using System.Collections.Generic;
using System.Linq;
using CbetaTranslator.App.Models;
using CbetaTranslator.App.Services;
using Xunit;

namespace CbetaTranslator.Tests.Services;

/// <summary>
/// Tests for OnboardingTourService: state machine transitions, step definitions,
/// event firing, and edge cases.
/// </summary>
public class OnboardingTourServiceTests
{
    private readonly OnboardingTourService _svc = new();

    // ---- 1. Start — sets IsActive, CurrentIndex=0, fires StepChanged ----

    [Fact]
    public void Start_SetsIsActive_And_CurrentIndexZero_And_FiresStepChanged()
    {
        TourStep? received = null;
        _svc.StepChanged += (_, step) => received = step;

        _svc.Start();

        Assert.True(_svc.IsActive);
        Assert.Equal(0, _svc.CurrentIndex);
        Assert.NotNull(received);
        Assert.Same(_svc.Steps[0], received);
    }

    // ---- 2. Next — advances to next step, fires StepChanged ----

    [Fact]
    public void Next_AdvancesToNextStep_And_FiresStepChanged()
    {
        _svc.Start();

        TourStep? received = null;
        _svc.StepChanged += (_, step) => received = step;

        _svc.Next();

        Assert.Equal(1, _svc.CurrentIndex);
        Assert.NotNull(received);
        Assert.Same(_svc.Steps[1], received);
    }

    // ---- 3. Next on last step — calls Complete, fires TourCompleted ----

    [Fact]
    public void Next_OnLastStep_CallsComplete_And_FiresTourCompleted()
    {
        _svc.Start();

        // Advance to last step
        for (int i = 0; i < _svc.Steps.Count - 1; i++)
            _svc.Next();

        Assert.Equal(_svc.Steps.Count - 1, _svc.CurrentIndex);

        bool completed = false;
        _svc.TourCompleted += (_, _) => completed = true;

        _svc.Next();

        Assert.True(completed);
        Assert.False(_svc.IsActive);
    }

    // ---- 4. Previous — goes back, fires StepChanged ----

    [Fact]
    public void Previous_GoesBack_And_FiresStepChanged()
    {
        _svc.Start();
        _svc.Next();
        _svc.Next();
        Assert.Equal(2, _svc.CurrentIndex);

        TourStep? received = null;
        _svc.StepChanged += (_, step) => received = step;

        _svc.Previous();

        Assert.Equal(1, _svc.CurrentIndex);
        Assert.NotNull(received);
        Assert.Same(_svc.Steps[1], received);
    }

    // ---- 5. Previous on step 0 — stays at 0 (no underflow) ----

    [Fact]
    public void Previous_OnStepZero_StaysAtZero_DoesNotFire()
    {
        _svc.Start();
        Assert.Equal(0, _svc.CurrentIndex);

        bool fired = false;
        _svc.StepChanged += (_, _) => fired = true;

        _svc.Previous();

        Assert.Equal(0, _svc.CurrentIndex);
        Assert.False(fired);
    }

    // ---- 6. Skip — sets IsActive=false, fires TourSkipped ----

    [Fact]
    public void Skip_SetsIsActiveFalse_And_FiresTourSkipped()
    {
        _svc.Start();
        Assert.True(_svc.IsActive);

        bool skipped = false;
        _svc.TourSkipped += (_, _) => skipped = true;

        _svc.Skip();

        Assert.False(_svc.IsActive);
        Assert.True(skipped);
    }

    // ---- 7. Complete — sets IsActive=false, fires TourCompleted ----

    [Fact]
    public void Complete_SetsIsActiveFalse_And_FiresTourCompleted()
    {
        _svc.Start();

        bool completed = false;
        _svc.TourCompleted += (_, _) => completed = true;

        _svc.Complete();

        Assert.False(_svc.IsActive);
        Assert.True(completed);
    }

    // ---- 8. AdvanceIfWaitingFor — advances when event matches ----

    [Fact]
    public void AdvanceIfWaitingFor_AdvancesWhenEventMatches()
    {
        _svc.Start();

        // Find first Wait step with a WaitForEvent
        int waitIndex = -1;
        for (int i = 0; i < _svc.Steps.Count; i++)
        {
            if (_svc.Steps[i].WaitForEvent != null)
            {
                waitIndex = i;
                break;
            }
        }

        Assert.True(waitIndex >= 0, "Expected at least one step with WaitForEvent");

        // Navigate to that step
        for (int i = 0; i < waitIndex; i++)
            _svc.Next();

        Assert.Equal(waitIndex, _svc.CurrentIndex);
        string eventId = _svc.Steps[waitIndex].WaitForEvent!;

        _svc.AdvanceIfWaitingFor(eventId);

        Assert.Equal(waitIndex + 1, _svc.CurrentIndex);
    }

    // ---- 9. AdvanceIfWaitingFor — does nothing when event doesn't match ----

    [Fact]
    public void AdvanceIfWaitingFor_DoesNothing_WhenEventDoesNotMatch()
    {
        _svc.Start();
        Assert.Equal(0, _svc.CurrentIndex);

        _svc.AdvanceIfWaitingFor("nonexistent-event");

        Assert.Equal(0, _svc.CurrentIndex);
    }

    // ---- 10. Steps count is 30 ----

    [Fact]
    public void Steps_Count_Is30()
    {
        Assert.Equal(30, _svc.Steps.Count);
    }

    // ---- 11. All steps have non-empty Title and Body ----

    [Fact]
    public void AllSteps_HaveNonEmpty_Title_And_Body()
    {
        foreach (var step in _svc.Steps)
        {
            Assert.False(string.IsNullOrWhiteSpace(step.Title),
                $"Step '{step.Id}' has empty Title");
            Assert.False(string.IsNullOrWhiteSpace(step.Body),
                $"Step '{step.Id}' has empty Body");
        }
    }

    // ---- 12. Setup steps (0-4) have correct WaitForEvent values ----

    [Fact]
    public void SetupSteps_HaveCorrectWaitForEventValues()
    {
        // Step 0 (welcome): no WaitForEvent
        Assert.Null(_svc.Steps[0].WaitForEvent);

        // Step 1 (git-check): "git-check-complete"
        Assert.Equal("git-check-complete", _svc.Steps[1].WaitForEvent);

        // Step 2 (download-texts): "root-cloned"
        Assert.Equal("root-cloned", _svc.Steps[2].WaitForEvent);

        // Step 3 (building-index): "index-built"
        Assert.Equal("index-built", _svc.Steps[3].WaitForEvent);

        // Step 4 (sidebar): no WaitForEvent
        Assert.Null(_svc.Steps[4].WaitForEvent);
    }

    // ---- 13. Steps with SwitchToTabIndex have valid tab indices (0-4) ----

    [Fact]
    public void Steps_WithSwitchToTabIndex_HaveValidIndices()
    {
        var stepsWithTab = _svc.Steps.Where(s => s.SwitchToTabIndex.HasValue).ToList();

        Assert.NotEmpty(stepsWithTab);

        foreach (var step in stepsWithTab)
        {
            Assert.InRange(step.SwitchToTabIndex!.Value, 0, 4);
        }
    }

    // ---- 14. CurrentStep is null when tour not started ----

    [Fact]
    public void CurrentStep_IsNull_WhenTourNotStarted()
    {
        Assert.Null(_svc.CurrentStep);
        Assert.Equal(-1, _svc.CurrentIndex);
        Assert.False(_svc.IsActive);
    }

    // ---- Additional edge case: All step Ids are unique ----

    [Fact]
    public void AllSteps_HaveUniqueIds()
    {
        var ids = _svc.Steps.Select(s => s.Id).ToList();
        Assert.Equal(ids.Count, ids.Distinct().Count());
    }

    // ---- Additional: All step Ids are non-empty ----

    [Fact]
    public void AllSteps_HaveNonEmptyIds()
    {
        foreach (var step in _svc.Steps)
        {
            Assert.False(string.IsNullOrWhiteSpace(step.Id),
                $"Step at index {_svc.Steps.IndexOf(step)} has empty Id");
        }
    }

    // ---- Additional: AdvanceIfWaitingFor on step with no WaitForEvent does nothing ----

    [Fact]
    public void AdvanceIfWaitingFor_OnPassiveStep_DoesNothing()
    {
        _svc.Start();

        // Step 0 is "welcome" with Type=Passive and no WaitForEvent
        Assert.Null(_svc.Steps[0].WaitForEvent);

        _svc.AdvanceIfWaitingFor("git-check-complete");

        Assert.Equal(0, _svc.CurrentIndex);
    }

    // ---- Additional: Multiple Next calls fire StepChanged each time ----

    [Fact]
    public void MultipleNext_FiresStepChanged_EachTime()
    {
        _svc.Start();

        var received = new List<int>();
        _svc.StepChanged += (_, step) => received.Add(_svc.CurrentIndex);

        _svc.Next();
        _svc.Next();
        _svc.Next();

        Assert.Equal(new[] { 1, 2, 3 }, received);
    }

    // ---- Additional: Skip does not fire TourCompleted ----

    [Fact]
    public void Skip_DoesNotFire_TourCompleted()
    {
        _svc.Start();

        bool completed = false;
        _svc.TourCompleted += (_, _) => completed = true;

        _svc.Skip();

        Assert.False(completed);
    }

    // ---- Additional: Complete does not fire TourSkipped ----

    [Fact]
    public void Complete_DoesNotFire_TourSkipped()
    {
        _svc.Start();

        bool skipped = false;
        _svc.TourSkipped += (_, _) => skipped = true;

        _svc.Complete();

        Assert.False(skipped);
    }
}
