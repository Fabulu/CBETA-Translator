using System.Collections.Generic;
using System.Linq;
using ReadZen.App.Models;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

/// <summary>
/// Tests for OnboardingTourService: state machine transitions, step definitions,
/// event firing, and edge cases.
/// </summary>
public class OnboardingTourServiceTests
{
    private readonly OnboardingTourService _svc = new() { DebounceMs = 0 };

    // ---- 1. Start â€” sets IsActive, CurrentIndex=0, fires StepChanged ----

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

    // ---- 2. Next â€” advances to next step, fires StepChanged ----

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

    // ---- 3. Next on last step â€” calls Complete, fires TourCompleted ----

    [Fact]
    public void Next_OnLastStep_CallsComplete_And_FiresTourCompleted()
    {
        // Start the feature tour (skipping mandatory setup) so Next() can
        // advance through the full step list.
        _svc.StartFeatureTour();

        // Advance to last step
        int featureSteps = _svc.FeatureTourStepCount;
        for (int i = 0; i < featureSteps - 1; i++)
            _svc.Next();

        Assert.Equal(_svc.Steps.Count - 1, _svc.CurrentIndex);

        bool completed = false;
        _svc.TourCompleted += (_, _) => completed = true;

        _svc.Next();

        Assert.True(completed);
        Assert.False(_svc.IsActive);
    }

    // ---- 4. Previous â€” goes back, fires StepChanged ----

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

    // ---- 5. Previous on step 0 â€” stays at 0 (no underflow) ----

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

    // ---- 6. Skip on a non-mandatory step â€” sets IsActive=false, fires TourSkipped ----

    [Fact]
    public void Skip_SetsIsActiveFalse_And_FiresTourSkipped()
    {
        // Use StartFeatureTour to reach non-mandatory steps (Next() now
        // fires SetupPhaseCompleted instead of advancing past mandatory).
        _svc.StartFeatureTour();
        Assert.True(_svc.IsActive);
        Assert.False(_svc.IsCurrentStepMandatory);

        bool skipped = false;
        _svc.TourSkipped += (_, _) => skipped = true;

        _svc.Skip();

        Assert.False(_svc.IsActive);
        Assert.True(skipped);
    }

    // ---- 7. Complete â€” sets IsActive=false, fires TourCompleted ----

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

    // ---- 8. AdvanceIfWaitingFor â€” advances when event matches ----

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

    // ---- 9. AdvanceIfWaitingFor â€” does nothing when event doesn't match ----

    [Fact]
    public void AdvanceIfWaitingFor_DoesNothing_WhenEventDoesNotMatch()
    {
        _svc.Start();
        Assert.Equal(0, _svc.CurrentIndex);

        _svc.AdvanceIfWaitingFor("nonexistent-event");

        Assert.Equal(0, _svc.CurrentIndex);
    }

    // ---- 10. Steps count: 61 after Phase 2 (Masters + Witness expansion + Wave 3-5 features) ----

    [Fact]
    public void Steps_Count_Is56()
    {
        Assert.Equal(61, _svc.Steps.Count);
    }

    // ---- Mandatory steps cannot be skipped ----

    [Fact]
    public void Skip_OnMandatoryStep_DoesNotEndTour()
    {
        _svc.Start();
        Assert.True(_svc.IsActive);
        Assert.True(_svc.CurrentStep!.IsMandatory, "Step 0 (welcome) should be mandatory");

        bool skipped = false;
        _svc.TourSkipped += (_, _) => skipped = true;

        _svc.Skip();

        Assert.True(_svc.IsActive, "Tour must remain active when Skip is called on a mandatory step");
        Assert.False(skipped, "TourSkipped should NOT fire on a mandatory step");
    }

    [Fact]
    public void Skip_OnFirstNonMandatoryStep_EndsTour()
    {
        // StartFeatureTour lands on the first non-mandatory step directly.
        _svc.StartFeatureTour();

        Assert.False(_svc.CurrentStep!.IsMandatory, "First feature-tour step should NOT be mandatory");

        bool skipped = false;
        _svc.TourSkipped += (_, _) => skipped = true;

        _svc.Skip();

        Assert.False(_svc.IsActive);
        Assert.True(skipped);
    }

    [Fact]
    public void MandatorySteps_AreExactlyTheFirstFour()
    {
        // welcome / git-check / download-texts / building-index are mandatory.
        // Everything else (sidebar onward) is opt-in.
        for (int i = 0; i < 4; i++)
        {
            Assert.True(_svc.Steps[i].IsMandatory,
                $"Step {i} ({_svc.Steps[i].Id}) should be mandatory");
        }
        for (int i = 4; i < _svc.Steps.Count; i++)
        {
            Assert.False(_svc.Steps[i].IsMandatory,
                $"Step {i} ({_svc.Steps[i].Id}) should NOT be mandatory");
        }
    }

    [Fact]
    public void IsCurrentStepMandatory_ReflectsCurrentStep()
    {
        _svc.Start();
        Assert.True(_svc.IsCurrentStepMandatory, "Step 0 should be mandatory");

        // StartFeatureTour to reach non-mandatory steps
        _svc.StartFeatureTour();
        Assert.False(_svc.IsCurrentStepMandatory, "Feature tour step should NOT be mandatory");
    }

    // ---- Tour split: setup phase ends, then optional feature tour starts ----

    [Fact]
    public void Next_OnLastMandatoryStep_FiresSetupPhaseCompleted_NotAdvance()
    {
        _svc.Start();
        // Walk through mandatory steps until the last one
        for (int i = 0; i < _svc.SetupStepCount - 1; i++) _svc.Next();

        bool setupCompleted = false;
        _svc.SetupPhaseCompleted += (_, _) => setupCompleted = true;

        _svc.Next(); // should fire SetupPhaseCompleted, NOT advance

        Assert.True(setupCompleted, "SetupPhaseCompleted should fire");
        Assert.False(_svc.IsActive, "Service should be inactive after setup completes");
    }

    [Fact]
    public void StartFeatureTour_BeginsAtFirstNonMandatoryStep()
    {
        _svc.StartFeatureTour();
        Assert.True(_svc.IsActive);
        Assert.False(_svc.IsInSetupPhase);
        Assert.Equal(_svc.SetupStepCount, _svc.CurrentIndex);
        Assert.False(_svc.CurrentStep!.IsMandatory);
    }

    [Fact]
    public void PhaseRelativeIndex_ShowsSetupProgress_DuringSetup()
    {
        _svc.Start();
        Assert.Equal(0, _svc.PhaseRelativeIndex);
        Assert.Equal(_svc.SetupStepCount, _svc.PhaseStepCount);
        // "Step 1 of 4" — not "Step 1 of 56"

        _svc.Next();
        Assert.Equal(1, _svc.PhaseRelativeIndex);
    }

    [Fact]
    public void PhaseRelativeIndex_ShowsTourProgress_DuringFeatureTour()
    {
        _svc.StartFeatureTour();
        Assert.Equal(0, _svc.PhaseRelativeIndex);
        Assert.Equal(_svc.FeatureTourStepCount, _svc.PhaseStepCount);

        _svc.Next();
        Assert.Equal(1, _svc.PhaseRelativeIndex);
    }

    // ---- New v4.x feature steps exist ----

    [Fact]
    public void Tutorial_Includes_MastersTabStep()
    {
        var step = Assert.Single(_svc.Steps, s => s.Id == "masters-tab");
        Assert.Equal("BtnOpenMasters", step.TargetControlName);
        Assert.Contains("301", step.Body);
        Assert.Contains("Masters tab", step.Body);
        Assert.Equal(5, step.SwitchToTabIndex);
    }

    [Fact]
    public void Tutorial_Includes_MastersListStep()
    {
        var step = Assert.Single(_svc.Steps, s => s.Id == "masters-list");
        Assert.Contains("Copy Link", step.Body);
        Assert.Contains("Edit Dates", step.Body);
    }

    [Fact]
    public void Tutorial_Includes_MastersCorpusStep()
    {
        var step = Assert.Single(_svc.Steps, s => s.Id == "masters-corpus");
        Assert.Contains("primary", step.Body);
        Assert.Contains("secondary", step.Body);
        Assert.Contains("concept-name filter", step.Body);
    }

    [Fact]
    public void Tutorial_Includes_MastersLineageStep()
    {
        var step = Assert.Single(_svc.Steps, s => s.Id == "masters-lineage");
        Assert.Contains("zoom slider", step.Body);
        Assert.Contains("Y-axis", step.Body);
    }

    [Fact]
    public void Tutorial_Includes_MastersWebProfileStep()
    {
        var step = Assert.Single(_svc.Steps, s => s.Id == "masters-web-profile");
        Assert.Contains("readzen.pages.dev/master/", step.Body);
        Assert.Contains("Linji_Yixuan", step.Body);
    }

    [Fact]
    public void Tutorial_Includes_WitnessComparisonStep()
    {
        var step = Assert.Single(_svc.Steps, s => s.Id == "witness-comparison");
        Assert.Contains("Edition Process", step.Body);
        Assert.Contains("Compare witnesses", step.Body);
        Assert.Contains("differing readings", step.Body);
    }

    [Fact]
    public void Tutorial_Includes_WitnessTextViewerStep()
    {
        var step = Assert.Single(_svc.Steps, s => s.Id == "witness-text-viewer");
        Assert.Contains("Witness Text Viewer", step.Body);
        Assert.Contains("witnesses.json", step.Body);
        Assert.Contains("wumenguan-1632", step.Body);
    }

    [Fact]
    public void Tutorial_SidebarStep_DescribesTextLibrary()
    {
        // After the tour split, the sidebar step is the first feature-tour
        // step. It describes the text library, not setup completion.
        var step = Assert.Single(_svc.Steps, s => s.Id == "sidebar");
        Assert.Contains("text library", step.Body);
        Assert.Contains("Red", step.Body);
    }

    [Fact]
    public void Tutorial_Includes_TwoCollectionsStep()
    {
        var step = Assert.Single(_svc.Steps, s => s.Id == "two-collections");
        Assert.Contains("CBETA", step.Body);
        Assert.Contains("OpenZen", step.Body);
        Assert.Contains("CC0", step.Body);
    }

    [Fact]
    public void Tutorial_Includes_CorpusSwitcherStep()
    {
        var step = Assert.Single(_svc.Steps, s => s.Id == "corpus-switcher");
        Assert.Equal("BtnCorpusBadge", step.TargetControlName);
        Assert.Contains("switch", step.Body, System.StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Tutorial_Includes_LicenseChipStep()
    {
        var step = Assert.Single(_svc.Steps, s => s.Id == "license-chip");
        Assert.Equal("BtnLicenseChipTopBar", step.TargetControlName);
        Assert.Contains("CC0", step.Body);
        Assert.Contains("non-commercial", step.Body);
    }

    [Fact]
    public void Tutorial_WelcomeStep_MentionsBothCorpora()
    {
        var step = Assert.Single(_svc.Steps, s => s.Id == "welcome");
        Assert.Contains("OpenZen", step.Body);
        Assert.Contains("CBETA", step.Body);
    }

    [Fact]
    public void Tutorial_DoesNotMention_GitTab()
    {
        foreach (var step in _svc.Steps)
        {
            Assert.DoesNotContain("Git tab", step.Title);
            Assert.DoesNotContain("Git tab", step.Body);
        }
    }

    [Fact]
    public void Tutorial_Includes_ScholarSharedStep()
    {
        var step = Assert.Single(_svc.Steps, s => s.Id == "scholar-shared");
        Assert.Contains("Shared", step.Title);
        Assert.Contains("Adopt", step.Body);
    }


    [Fact]
    public void Tutorial_SearchResultsStep_CoversBilingualResultsAndScholarAction()
    {
        var step = Assert.Single(_svc.Steps, s => s.Id == "search-results");
        Assert.Contains("hit count badges", step.Body);
        Assert.Contains("Double-click", step.Body);
        Assert.Contains("Scholar", step.Body);
    }

    [Fact]
    public void Tutorial_Includes_ReaderDictionaryAndCompareSteps()
    {
        var dictStep = Assert.Single(_svc.Steps, s => s.Id == "reader-dictionary-button");
        Assert.Contains("Dict button", dictStep.Body);

        var compareStep = Assert.Single(_svc.Steps, s => s.Id == "reader-compare-tools");
        Assert.Contains("Compare Translations", compareStep.Body);
        Assert.Contains("tag layers", compareStep.Body);
    }

    [Fact]
    public void Tutorial_SaveTranslationStep_ExplainsOneLinePerBlockRule()
    {
        var step = Assert.Single(_svc.Steps, s => s.Id == "save-translation");
        Assert.Contains("one English translation", step.Body);
        Assert.Contains("Batch pastes", step.Body);
    }

    [Fact]
    public void Tutorial_Includes_ZenMasterManagerStep()
    {
        // After Phase 2, the zen-master-manager step covers the deep-link entry path
        // (the Masters tab's main features moved to dedicated masters-* steps).
        var step = Assert.Single(_svc.Steps, s => s.Id == "zen-master-manager");
        Assert.Contains("Zen Master Manager", step.Body);
        Assert.Contains("links shared by other users", step.Body);
    }

    [Fact]
    public void Tutorial_Includes_ProvenancePanelStep()
    {
        var step = Assert.Single(_svc.Steps, s => s.Id == "provenance-panel");
        Assert.Equal("ChkProvenance", step.TargetControlName);
        Assert.Contains("Provenance", step.Body);
        Assert.Contains("SHA-256", step.Body);
    }

    [Fact]
    public void Tutorial_Includes_AutoFillTmStep()
    {
        var step = Assert.Single(_svc.Steps, s => s.Id == "auto-fill-tm");
        Assert.Equal("AssistantPane", step.TargetControlName);
        Assert.Contains("Translation Memory", step.Title);
        Assert.Contains("100%", step.Body);
    }

    [Fact]
    public void Tutorial_Includes_FreshStartStep()
    {
        var step = Assert.Single(_svc.Steps, s => s.Id == "fresh-start");
        Assert.Equal("BtnFreshStart", step.TargetControlName);
        Assert.Contains("reset", step.Body);
        Assert.Contains("untranslated", step.Body);
    }

    [Fact]
    public void Tutorial_Includes_SearchExportStep()
    {
        var step = Assert.Single(_svc.Steps, s => s.Id == "search-export");
        Assert.Equal("BtnExport", step.TargetControlName);
        Assert.Contains("CSV", step.Body);
        Assert.Contains("BibTeX", step.Body);
    }

    [Fact]
    public void Tutorial_Includes_MultiCorpusSyncStep()
    {
        var step = Assert.Single(_svc.Steps, s => s.Id == "multi-corpus-sync");
        Assert.Equal("BtnGitSync", step.TargetControlName);
        Assert.Contains("CBETA", step.Body);
        Assert.Contains("OpenZen", step.Body);
    }

    [Fact]
    public void Tutorial_Includes_TranslationPrStep()
    {
        var step = Assert.Single(_svc.Steps, s => s.Id == "translation-pr");
        Assert.Equal(TourPlacement.Center, step.Placement);
        Assert.Contains("pull request", step.Body);
        Assert.Contains("GitHub", step.Body);
    }

    [Fact]
    public void Tutorial_CommunityStep_MentionsSeparateSyncFlows()
    {
        var step = Assert.Single(_svc.Steps, s => s.Id == "git-tab");
        Assert.Contains("shared separately", step.Body);
        Assert.Contains("downloading texts", step.Body);
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

    // ---- 13. Steps with SwitchToTabIndex have valid tab indices (0-5) ----
    // Masters tab is index 5 — added to MainWindow in v4.1.

    [Fact]
    public void Steps_WithSwitchToTabIndex_HaveValidIndices()
    {
        var stepsWithTab = _svc.Steps.Where(s => s.SwitchToTabIndex.HasValue).ToList();

        Assert.NotEmpty(stepsWithTab);

        foreach (var step in stepsWithTab)
        {
            Assert.InRange(step.SwitchToTabIndex!.Value, 0, 5);
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



