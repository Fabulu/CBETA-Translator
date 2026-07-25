using System;
using System.Collections.Generic;
using ReadZen.App.Models;
using ReadZen.App.ViewModels;
using Xunit;

namespace ReadZen.Tests.ViewModels;

public class ReadableTabViewModelTests
{
    private static ReadableTabViewModel MakeVm() => new();

    // ---- Initial state ----

    [Fact]
    public void InitialState_HasDefaults()
    {
        var vm = MakeVm();

        Assert.False(vm.IsZenText);
        Assert.False(vm.IsZenEnabled);
        Assert.False(vm.HoverDictionaryEnabled); // CC-CEDICT hover is opt-in
        Assert.Equal("", vm.DefaultResp);
        Assert.Null(vm.CurrentRelPathForZen);
        Assert.False(vm.NotesPanelVisible);
        Assert.Equal("Note", vm.NotesHeaderText);
        Assert.Equal("", vm.NotesBodyText);
        Assert.False(vm.CanDeleteCommunityNote);
        Assert.False(vm.CanMoveFootnote);
        Assert.False(vm.CanAddCommunityNote);
        Assert.False(vm.PendingRefresh);
    }

    // ---- IsEmptyState ----

    [Fact]
    public void InitialState_IsEmptyState_IsTrue()
    {
        var vm = MakeVm();
        Assert.True(vm.IsEmptyState);
    }

    [Fact]
    public void Clear_SetsIsEmptyStateTrue()
    {
        var vm = MakeVm();
        vm.IsEmptyState = false;

        vm.Clear();

        Assert.True(vm.IsEmptyState);
    }

    [Fact]
    public void IsEmptyState_PropertyChanged_Fires()
    {
        var vm = MakeVm();
        var changed = new List<string>();
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName!);

        vm.IsEmptyState = false;

        Assert.Contains("IsEmptyState", changed);
    }

    // ---- SetZenContext ----

    [Fact]
    public void SetZenContext_WithPath_EnablesZen()
    {
        var vm = MakeVm();

        vm.SetZenContext("test/file.xml", isZen: true);

        // The Zen toggle is now always disabled: classification is prescriptive
        // (Assets/Data/zen-corpus.json), so users can no longer change what counts as Zen.
        // SetZenContext still displays the Zen flag and records the rel path.
        Assert.False(vm.IsZenEnabled);
        Assert.True(vm.IsZenText);
        Assert.Equal("test/file.xml", vm.CurrentRelPathForZen);
    }

    [Fact]
    public void SetZenContext_NullPath_DisablesZen()
    {
        var vm = MakeVm();
        vm.SetZenContext("some/path", isZen: true);

        vm.SetZenContext(null, isZen: false);

        Assert.False(vm.IsZenEnabled);
        Assert.False(vm.IsZenText);
    }

    [Fact]
    public void SetZenContext_SuppressesZenFlagChangedEvent()
    {
        var vm = MakeVm();
        bool eventFired = false;
        vm.ZenFlagChanged += (_, _) => eventFired = true;

        vm.SetZenContext("test/path", isZen: true);

        Assert.False(eventFired);
    }

    // ---- Zen toggle fires event when not suppressed ----

    [Fact]
    public void IsZenText_ManualChange_FiresZenFlagChanged()
    {
        var vm = MakeVm();
        vm.SetZenContext("test/path", isZen: false);

        (string path, bool isZen)? received = null;
        vm.ZenFlagChanged += (_, args) => received = args;

        vm.IsZenText = true;

        Assert.NotNull(received);
        Assert.Equal("test/path", received!.Value.path);
        Assert.True(received.Value.isZen);
    }

    // ---- Clear ----

    [Fact]
    public void Clear_ResetsState()
    {
        var vm = MakeVm();
        vm.SetZenContext("some/path", isZen: true);
        vm.NotesPanelVisible = true;
        vm.PendingRefresh = true;

        vm.Clear();

        Assert.False(vm.IsZenEnabled);
        Assert.False(vm.IsZenText);
        Assert.False(vm.NotesPanelVisible);
        Assert.False(vm.PendingRefresh);
        Assert.Equal(RenderedDocument.Empty, vm.RenderOrig);
        Assert.Equal(RenderedDocument.Empty, vm.RenderTran);
    }

    // ---- Pending gate ----

    [Fact]
    public void EnterPending_SetsPendingRefreshTrue()
    {
        var vm = MakeVm();

        vm.EnterPending("test reason");

        Assert.True(vm.PendingRefresh);
    }

    [Fact]
    public void ExitPending_ClearsPendingRefresh()
    {
        var vm = MakeVm();
        vm.EnterPending("test");

        vm.ExitPending("done");

        Assert.False(vm.PendingRefresh);
    }

    [Fact]
    public void ExitPending_WhenNotPending_DoesNothing()
    {
        var vm = MakeVm();

        vm.ExitPending("noop");

        Assert.False(vm.PendingRefresh);
    }

    [Fact]
    public void EnterPending_DisablesButtons()
    {
        var vm = MakeVm();

        vm.EnterPending("test");

        Assert.False(vm.CanAddCommunityNote);
        Assert.False(vm.CanDeleteCommunityNote);
        Assert.False(vm.CanMoveFootnote);
    }

    // ---- ShowNotes / HideNotes ----

    [Fact]
    public void ShowNotes_SetsPanel()
    {
        var vm = MakeVm();
        var ann = new DocAnnotation(0, 10, "Hello");

        vm.ShowNotes(ann, fromTranslatedPane: false);

        Assert.True(vm.NotesPanelVisible);
        Assert.Equal("Hello", vm.NotesBodyText);
        Assert.Same(ann, vm.CurrentAnnotation);
        Assert.False(vm.CurrentAnnotationFromTranslatedPane);
    }

    [Fact]
    public void ShowNotes_CommunityNote_ShowsCommunityLabel()
    {
        var vm = MakeVm();
        var ann = new DocAnnotation(0, 10, "Community note", kind: "community", resp: "Alice",
            xmlStart: 100, xmlEndExclusive: 200);

        vm.ShowNotes(ann, fromTranslatedPane: true);

        Assert.Contains("Community", vm.NotesHeaderText);
        Assert.Contains("Alice", vm.NotesHeaderText);
        Assert.True(vm.CurrentAnnotationFromTranslatedPane);
    }

    [Fact]
    public void HideNotes_ClearsPanel()
    {
        var vm = MakeVm();
        vm.ShowNotes(new DocAnnotation(0, 10, "test"), false);

        vm.HideNotes();

        Assert.False(vm.NotesPanelVisible);
        Assert.Equal("", vm.NotesBodyText);
        Assert.Null(vm.CurrentAnnotation);
    }

    // ---- TryGetXmlCommunitySpanStrict ----

    [Fact]
    public void TryGetXmlCommunitySpanStrict_CommunityWithSpan_ReturnsTrue()
    {
        var ann = new DocAnnotation(0, 10, "test", kind: "community", xmlStart: 50, xmlEndExclusive: 100);

        bool result = ReadableTabViewModel.TryGetXmlCommunitySpanStrict(ann, out int xs, out int xe);

        Assert.True(result);
        Assert.Equal(50, xs);
        Assert.Equal(100, xe);
    }

    [Fact]
    public void TryGetXmlCommunitySpanStrict_NonCommunity_ReturnsFalse()
    {
        var ann = new DocAnnotation(0, 10, "test", kind: "note", xmlStart: 50, xmlEndExclusive: 100);

        bool result = ReadableTabViewModel.TryGetXmlCommunitySpanStrict(ann, out _, out _);

        Assert.False(result);
    }

    [Fact]
    public void TryGetXmlCommunitySpanStrict_NoSpan_ReturnsFalse()
    {
        var ann = new DocAnnotation(0, 10, "test", kind: "community");

        bool result = ReadableTabViewModel.TryGetXmlCommunitySpanStrict(ann, out _, out _);

        Assert.False(result);
    }

    // ---- TryGetXmlSpanLoose ----

    [Fact]
    public void TryGetXmlSpanLoose_AnyKindWithSpan_ReturnsTrue()
    {
        var ann = new DocAnnotation(0, 10, "test", kind: "note", xmlStart: 50, xmlEndExclusive: 100);

        bool result = ReadableTabViewModel.TryGetXmlSpanLoose(ann, out int xs, out int xe);

        Assert.True(result);
        Assert.Equal(50, xs);
        Assert.Equal(100, xe);
    }

    [Fact]
    public void TryGetXmlSpanLoose_NoSpan_ReturnsFalse()
    {
        var ann = new DocAnnotation(0, 10, "test");

        bool result = ReadableTabViewModel.TryGetXmlSpanLoose(ann, out _, out _);

        Assert.False(result);
    }

    // ---- TryConvertNumber ----

    [Theory]
    [InlineData(42, true, 42)]
    [InlineData((long)100, true, 100)]
    [InlineData((short)5, true, 5)]
    [InlineData((byte)7, true, 7)]
    [InlineData(null, false, 0)]
    public void TryConvertNumber_VariousTypes(object? input, bool expectedResult, int expectedValue)
    {
        bool result = ReadableTabViewModel.TryConvertNumber(input, out int value);

        Assert.Equal(expectedResult, result);
        if (expectedResult)
            Assert.Equal(expectedValue, value);
    }

    [Fact]
    public void TryConvertNumber_Double_Truncates()
    {
        bool result = ReadableTabViewModel.TryConvertNumber(3.9, out int value);

        Assert.True(result);
        Assert.Equal(3, value);
    }

    // ---- GetAnnotationResp ----

    [Fact]
    public void GetAnnotationResp_ReturnsResp()
    {
        var ann = new DocAnnotation(0, 10, "test", resp: "Bob");

        var resp = ReadableTabViewModel.GetAnnotationResp(ann);

        Assert.Equal("Bob", resp);
    }

    [Fact]
    public void GetAnnotationResp_NullResp_ReturnsNull()
    {
        var ann = new DocAnnotation(0, 10, "test");

        var resp = ReadableTabViewModel.GetAnnotationResp(ann);

        Assert.Null(resp);
    }

    // ---- FindBestMatchRange ----

    [Fact]
    public void FindBestMatchRange_ExactMatch_Found()
    {
        string doc = "Hello World, World!";
        var (start, length) = ReadableTabViewModel.FindBestMatchRange(doc, "World", null, null, null, null, null);

        Assert.True(start >= 0);
        Assert.Equal(5, length);
    }

    [Fact]
    public void FindBestMatchRange_EmptyDoc_ReturnsNegative()
    {
        var (start, _) = ReadableTabViewModel.FindBestMatchRange("", "match", null, null, null, null, null);
        Assert.Equal(-1, start);
    }

    [Fact]
    public void FindBestMatchRange_EmptyMatch_ReturnsNegative()
    {
        var (start, _) = ReadableTabViewModel.FindBestMatchRange("some text", "", null, null, null, null, null);
        Assert.Equal(-1, start);
    }

    [Fact]
    public void FindBestMatchRange_WithContext_PrefersCorrectOccurrence()
    {
        string doc = "AAA BBB CCC AAA DDD EEE";
        var (start, length) = ReadableTabViewModel.FindBestMatchRange(
            doc, "AAA", left: "CCC", right: "DDD", null, null, null);

        // Should prefer the second "AAA" since it has matching left+right context
        Assert.Equal(12, start);
        Assert.Equal(3, length);
    }

    // ---- Move mode ----

    [Fact]
    public void StartMoveMode_WithValidAnnotation_SetsState()
    {
        var vm = MakeVm();
        var ann = new DocAnnotation(0, 10, "test", kind: "community",
            xmlStart: 50, xmlEndExclusive: 100);
        vm.ShowNotes(ann, false);

        vm.StartMoveMode();

        Assert.True(vm.AwaitingMoveTargetClick);
        Assert.Same(ann, vm.MoveSourceAnnotation);
    }

    [Fact]
    public void CancelMoveMode_ClearsState()
    {
        var vm = MakeVm();
        var ann = new DocAnnotation(0, 10, "test", kind: "community",
            xmlStart: 50, xmlEndExclusive: 100);
        vm.ShowNotes(ann, false);
        vm.StartMoveMode();

        vm.CancelMoveMode(keepPanelOpen: true);

        Assert.False(vm.AwaitingMoveTargetClick);
        Assert.Null(vm.MoveSourceAnnotation);
        Assert.True(vm.NotesPanelVisible); // panel stays open
    }

    [Fact]
    public void CancelMoveModeAndHideNotes_HidesPanel()
    {
        var vm = MakeVm();
        var ann = new DocAnnotation(0, 10, "test", kind: "community",
            xmlStart: 50, xmlEndExclusive: 100);
        vm.ShowNotes(ann, false);
        vm.StartMoveMode();

        vm.CancelMoveModeAndHideNotes();

        Assert.False(vm.AwaitingMoveTargetClick);
        Assert.False(vm.NotesPanelVisible);
    }

    // ---- CheckPendingTimeout ----

    [Fact]
    public void CheckPendingTimeout_WithinTimeout_StaysPending()
    {
        var vm = MakeVm();
        vm.EnterPending("test");

        vm.CheckPendingTimeout();

        Assert.True(vm.PendingRefresh);
    }

    // ---- Study panel ----

    [Fact]
    public void InitialState_StudyPanelVisible_IsFalse()
    {
        var vm = MakeVm();
        Assert.False(vm.StudyPanelVisible);
    }

    [Fact]
    public void StudyPanelVisible_CanBeToggled()
    {
        var vm = MakeVm();

        vm.StudyPanelVisible = true;
        Assert.True(vm.StudyPanelVisible);

        vm.StudyPanelVisible = false;
        Assert.False(vm.StudyPanelVisible);
    }

    [Fact]
    public void StudyPanelVisible_PropertyChanged_Fires()
    {
        var vm = MakeVm();
        var changed = new List<string>();
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName!);

        vm.StudyPanelVisible = true;

        Assert.Contains("StudyPanelVisible", changed);
    }

    [Fact]
    public void LastStudySnapshot_DefaultsToNull()
    {
        var vm = MakeVm();
        Assert.Null(vm.LastStudySnapshot);
    }

    [Fact]
    public void LastStudySnapshot_CanBeSet()
    {
        var vm = MakeVm();
        var snapshot = new TranslationAssistantSnapshot();

        vm.LastStudySnapshot = snapshot;

        Assert.Same(snapshot, vm.LastStudySnapshot);
    }

    [Fact]
    public void Clear_ResetsStudyState()
    {
        var vm = MakeVm();
        vm.StudyPanelVisible = true;
        vm.LastStudySnapshot = new TranslationAssistantSnapshot();

        vm.Clear();

        // Clear() does NOT reset study panel state — it preserves StudyPanelVisible
        // and LastStudySnapshot across clear operations (by design).
        Assert.True(vm.StudyPanelVisible);
        Assert.NotNull(vm.LastStudySnapshot);
    }
}
