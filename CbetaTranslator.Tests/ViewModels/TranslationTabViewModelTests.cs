using System;
using System.Collections.Generic;
using System.Linq;
using CbetaTranslator.App.Models;
using CbetaTranslator.App.Services;
using CbetaTranslator.App.ViewModels;
using Xunit;

namespace CbetaTranslator.Tests.ViewModels;

public class TranslationTabViewModelTests
{
    private static TranslationTabViewModel MakeVm() => new();

    // ---- Initial state ----

    [Fact]
    public void InitialState_HasDefaults()
    {
        var vm = MakeVm();

        Assert.Equal(TranslationEditMode.Body, vm.CurrentMode);
        Assert.Equal("Translation Editor", vm.ModeInfoText);
        Assert.Equal("", vm.QuickInfoText);
        Assert.Equal("Unreviewed", vm.ReviewStateText);
        Assert.Equal("", vm.ProgressText);
        Assert.True(vm.HoverDictionaryEnabled);
        Assert.Equal("", vm.CurrentProjection);
    }

    // ---- SwitchMode ----

    [Fact]
    public void SwitchMode_Head_CoercesToBody()
    {
        var vm = MakeVm();
        TranslationEditMode? received = null;
        vm.ModeChanged += (_, mode) => received = mode;

        vm.SwitchMode(TranslationEditMode.Head);

        Assert.Equal(TranslationEditMode.Body, vm.CurrentMode);
        Assert.False(vm.IsModeBodyEnabled);
        Assert.True(vm.IsModeNotesEnabled);
        Assert.Null(received);
    }

    [Fact]
    public void SwitchMode_SameMode_DoesNotFireEvent()
    {
        var vm = MakeVm(); // starts at Body
        bool eventFired = false;
        vm.ModeChanged += (_, _) => eventFired = true;

        vm.SwitchMode(TranslationEditMode.Body);

        Assert.False(eventFired);
    }

    [Fact]
    public void SwitchMode_Notes_UpdatesModeInfo()
    {
        var vm = MakeVm();

        vm.SwitchMode(TranslationEditMode.Notes);

        Assert.Contains("Notes", vm.ModeInfoText);
        Assert.Equal(TranslationEditMode.Notes, vm.CurrentMode);
    }

    // ---- SetCurrentFilePaths ----

    [Fact]
    public void SetCurrentFilePaths_UpdatesModeInfoWithFilename()
    {
        var vm = MakeVm();

        vm.SetCurrentFilePaths("/orig/T2076_.xml", "/tran/T2076_.xml");

        Assert.Contains("T2076_.xml", vm.ModeInfoText);
    }

    // ---- SetCurrentReviewState ----

    [Fact]
    public void SetCurrentReviewState_Null_ShowsUnreviewed()
    {
        var vm = MakeVm();

        vm.SetCurrentReviewState(null, null, null);

        Assert.Equal("Unreviewed", vm.ReviewStateText);
    }

    [Fact]
    public void SetCurrentReviewState_Approved_ShowsApproved()
    {
        var vm = MakeVm();

        vm.SetCurrentReviewState("approved", "Alice", new DateTime(2026, 1, 15, 10, 30, 0, DateTimeKind.Utc));

        Assert.Contains("Approved", vm.ReviewStateText);
        Assert.Contains("Alice", vm.ReviewStateText);
    }

    [Fact]
    public void SetCurrentReviewState_NeedsWork_ShowsNeedsWork()
    {
        var vm = MakeVm();

        vm.SetCurrentReviewState("needs-work", null, null);

        Assert.Contains("Needs work", vm.ReviewStateText);
    }

    [Fact]
    public void SetCurrentReviewState_WithReviewerNoDate_ShowsReviewerOnly()
    {
        var vm = MakeVm();

        vm.SetCurrentReviewState("approved", "Bob", null);

        Assert.Contains("Bob", vm.ReviewStateText);
        Assert.DoesNotContain("2026", vm.ReviewStateText);
    }

    // ---- SetProgressStats ----

    [Fact]
    public void SetProgressStats_ValidValues_FormatsCorrectly()
    {
        var vm = MakeVm();

        vm.SetProgressStats(5, 2, 10);

        Assert.Equal("5/10 approved \u00b7 2 needs work", vm.ProgressText);
    }

    [Fact]
    public void SetProgressStats_ZeroTotal_ReturnsEmpty()
    {
        var vm = MakeVm();

        vm.SetProgressStats(0, 0, 0);

        Assert.Equal("", vm.ProgressText);
    }

    // ---- Clear ----

    [Fact]
    public void Clear_ResetsAllState()
    {
        var vm = MakeVm();
        vm.SetCurrentFilePaths("/orig/file.xml", "/tran/file.xml");
        vm.CurrentProjection = "some text";
        vm.SetCurrentReviewState("approved", "Alice", DateTime.UtcNow);

        vm.Clear();

        Assert.Equal("", vm.CurrentProjection);
        Assert.Equal("Unreviewed", vm.ReviewStateText);
        Assert.Equal("", vm.QuickInfoText);
        Assert.Null(vm.LastAssistantSnapshot);
    }

    // ---- Event raisers ----

    [Fact]
    public void RaiseSaveRequested_FiresEvent()
    {
        var vm = MakeVm();
        bool fired = false;
        vm.SaveRequested += (_, _) => fired = true;

        vm.RaiseSaveRequested();

        Assert.True(fired);
    }

    [Fact]
    public void RaiseRevertRequested_FiresEvent()
    {
        var vm = MakeVm();
        bool fired = false;
        vm.RevertRequested += (_, _) => fired = true;

        vm.RaiseRevertRequested();

        Assert.True(fired);
    }

    [Fact]
    public void RaiseReviewAction_FiresEventWithAction()
    {
        var vm = MakeVm();
        string? received = null;
        vm.ReviewActionRequested += (_, action) => received = action;

        vm.RaiseReviewAction("approved");

        Assert.Equal("approved", received);
    }

    [Fact]
    public void RaiseNavigationRequested_FiresEvent()
    {
        var vm = MakeVm();
        NavigationRequest? received = null;
        vm.NavigationRequested += (_, req) => received = req;

        var request = new NavigationRequest { RelPath = "test.xml" };
        vm.RaiseNavigationRequested(request);

        Assert.Same(request, received);
    }

    // ---- ParseProjectionBlocksWithOffsets ----

    [Fact]
    public void ParseProjectionBlocksWithOffsets_ValidText_ParsesBlocks()
    {
        string text = "<1>\nZH: \u4f60\u597d\nEN: Hello\n<2>\nZH: \u4e16\u754c\nEN: World\n";

        var blocks = TranslationTabViewModel.ParseProjectionBlocksWithOffsets(text);

        Assert.Equal(2, blocks.Count);
        Assert.Equal(1, blocks[0].BlockNumber);
        Assert.Equal("\u4f60\u597d", blocks[0].Zh);
        Assert.Equal("Hello", blocks[0].En);
        Assert.Equal(2, blocks[1].BlockNumber);
        Assert.Equal("\u4e16\u754c", blocks[1].Zh);
        Assert.Equal("World", blocks[1].En);
    }

    [Fact]
    public void ParseProjectionBlocksWithOffsets_EmptyText_ReturnsEmpty()
    {
        var blocks = TranslationTabViewModel.ParseProjectionBlocksWithOffsets("");
        Assert.Empty(blocks);
    }

    [Fact]
    public void ParseProjectionBlocksWithOffsets_Null_ReturnsEmpty()
    {
        var blocks = TranslationTabViewModel.ParseProjectionBlocksWithOffsets(null!);
        Assert.Empty(blocks);
    }

    [Fact]
    public void ParseProjectionBlocksWithOffsets_TracksOffsets()
    {
        string text = "<1>\nZH: A\nEN: B\n";
        var blocks = TranslationTabViewModel.ParseProjectionBlocksWithOffsets(text);

        Assert.Single(blocks);
        Assert.Equal(0, blocks[0].BlockStartOffset);
        Assert.True(blocks[0].EnValueStartOffset > 0);
        Assert.Equal(1, blocks[0].EnValueLength);
    }

    // ---- FindBlockIndexAtOrAfterCaret ----

    [Fact]
    public void FindBlockIndexAtOrAfterCaret_EmptyBlocks_ReturnsNegative()
    {
        int idx = TranslationTabViewModel.FindBlockIndexAtOrAfterCaret(new List<TranslationTabViewModel.ProjectionBlockInfo>(), 0);
        Assert.Equal(-1, idx);
    }

    [Fact]
    public void FindBlockIndexAtOrAfterCaret_CaretBeforeFirst_ReturnsFirst()
    {
        string text = "<1>\nZH: A\nEN: B\n<2>\nZH: C\nEN: D\n";
        var blocks = TranslationTabViewModel.ParseProjectionBlocksWithOffsets(text);

        int idx = TranslationTabViewModel.FindBlockIndexAtOrAfterCaret(blocks, 0);

        Assert.Equal(0, idx);
    }

    // ---- ContainsChineseChar ----

    [Theory]
    [InlineData("\u4f60\u597d", true)]
    [InlineData("Hello", false)]
    [InlineData("", false)]
    [InlineData(null, false)]
    [InlineData("mixed\u4e16ç•Œ", true)]
    public void ContainsChineseChar_VariousInputs(string? input, bool expected)
    {
        Assert.Equal(expected, TranslationTabViewModel.ContainsChineseChar(input));
    }

    // ---- IsSkippableForCopyOrJump ----

    [Fact]
    public void IsSkippableForCopyOrJump_EmptyBlock_ReturnsTrue()
    {
        var block = new TranslationTabViewModel.ProjectionBlockInfo { Zh = "", En = "" };
        Assert.True(TranslationTabViewModel.IsSkippableForCopyOrJump(block));
    }

    [Fact]
    public void IsSkippableForCopyOrJump_NoChinese_ReturnsTrue()
    {
        var block = new TranslationTabViewModel.ProjectionBlockInfo { Zh = "English only", En = "" };
        Assert.True(TranslationTabViewModel.IsSkippableForCopyOrJump(block));
    }

    [Fact]
    public void IsSkippableForCopyOrJump_WithChinese_ReturnsFalse()
    {
        var block = new TranslationTabViewModel.ProjectionBlockInfo { Zh = "\u4f60\u597d", En = "" };
        Assert.False(TranslationTabViewModel.IsSkippableForCopyOrJump(block));
    }

    // ---- ShouldJumpToUntranslated ----

    [Fact]
    public void ShouldJumpToUntranslated_EmptyEn_WithChinese_ReturnsTrue()
    {
        var block = new TranslationTabViewModel.ProjectionBlockInfo { Zh = "\u4f60\u597d", En = "" };
        Assert.True(TranslationTabViewModel.ShouldJumpToUntranslated(block));
    }

    [Fact]
    public void ShouldJumpToUntranslated_NonEmptyEn_ReturnsFalse()
    {
        var block = new TranslationTabViewModel.ProjectionBlockInfo { Zh = "\u4f60\u597d", En = "Hello" };
        Assert.False(TranslationTabViewModel.ShouldJumpToUntranslated(block));
    }

    // ---- ValidateEnglish ----

    [Fact]
    public void ValidateEnglish_Normal_DoesNotThrow()
    {
        TranslationTabViewModel.ValidateEnglish("Hello World", 1);
    }

    [Fact]
    public void ValidateEnglish_WithAngleBrackets_Throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            TranslationTabViewModel.ValidateEnglish("Hello <world>", 1));
    }

    [Fact]
    public void ValidateEnglish_NullInput_DoesNotThrow()
    {
        TranslationTabViewModel.ValidateEnglish(null!, 1);
    }

    // ---- BuildPrompt ----

    [Fact]
    public void BuildPrompt_ContainsProjection()
    {
        string result = TranslationTabViewModel.BuildPrompt("<1>\nZH: test\nEN: ");

        Assert.Contains("<1>", result);
        Assert.Contains("ZH: test", result);
        Assert.Contains("STRICT RULES", result);
    }

    // ---- ExtractCodeBlockOrRaw ----

    [Fact]
    public void ExtractCodeBlockOrRaw_WithCodeBlock_ExtractsContent()
    {
        string input = "```markdown\nHello World\n```";
        string result = TranslationTabViewModel.ExtractCodeBlockOrRaw(input);
        Assert.Equal("Hello World", result);
    }

    [Fact]
    public void ExtractCodeBlockOrRaw_NoCodeBlock_ReturnsTrimmed()
    {
        string input = "  Hello World  ";
        string result = TranslationTabViewModel.ExtractCodeBlockOrRaw(input);
        Assert.Equal("Hello World", result);
    }

    // ---- FirstChars / LastChars ----

    [Theory]
    [InlineData("Hello World", 5, "Hello")]
    [InlineData("Hi", 10, "Hi")]
    [InlineData("", 5, "")]
    [InlineData(null, 5, "")]
    public void FirstChars_VariousInputs(string? input, int count, string expected)
    {
        Assert.Equal(expected, TranslationTabViewModel.FirstChars(input!, count));
    }

    [Theory]
    [InlineData("Hello World", 5, "World")]
    [InlineData("Hi", 10, "Hi")]
    [InlineData("", 5, "")]
    [InlineData(null, 5, "")]
    public void LastChars_VariousInputs(string? input, int count, string expected)
    {
        Assert.Equal(expected, TranslationTabViewModel.LastChars(input!, count));
    }

    // ---- MergeRanges ----

    [Fact]
    public void MergeRanges_OverlappingRanges_Merged()
    {
        var ranges = new List<TranslationTabViewModel.TextRange>
        {
            new(0, 5),
            new(3, 5),
            new(10, 3)
        };

        var result = TranslationTabViewModel.MergeRanges(ranges);

        Assert.Equal(2, result.Count);
        Assert.Equal(0, result[0].Start);
        Assert.Equal(8, result[0].Length);
        Assert.Equal(10, result[1].Start);
        Assert.Equal(3, result[1].Length);
    }

    [Fact]
    public void MergeRanges_NonOverlapping_Unchanged()
    {
        var ranges = new List<TranslationTabViewModel.TextRange>
        {
            new(0, 3),
            new(10, 3)
        };

        var result = TranslationTabViewModel.MergeRanges(ranges);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void MergeRanges_Empty_ReturnsEmpty()
    {
        var result = TranslationTabViewModel.MergeRanges(new List<TranslationTabViewModel.TextRange>());
        Assert.Empty(result);
    }

    // ---- SetModeProjectionState ----

    [Fact]
    public void SetModeProjectionState_SetsAllState()
    {
        var vm = MakeVm();

        vm.SetModeProjectionState(TranslationEditMode.Head, "projection text");

        Assert.Equal(TranslationEditMode.Body, vm.CurrentMode);
        Assert.Equal("projection text", vm.CurrentProjection);
        Assert.Contains("Body", vm.ModeInfoText);
    }

    // ---- ResolveAssistantTitle ----

    [Fact]
    public void ResolveAssistantTitle_NullPath_ReturnsEmpty()
    {
        var vm = MakeVm();
        Assert.Equal("", vm.ResolveAssistantTitle(null));
    }

    [Fact]
    public void ResolveAssistantTitle_WithResolver_UsesResolver()
    {
        var vm = MakeVm();
        vm.SetAssistantTitleResolver(rel => "Resolved: " + rel);

        string result = vm.ResolveAssistantTitle("test.xml");

        Assert.Equal("Resolved: test.xml", result);
    }

    [Fact]
    public void ResolveAssistantTitle_NoResolver_ReturnsRelPath()
    {
        var vm = MakeVm();

        string result = vm.ResolveAssistantTitle("test.xml");

        Assert.Equal("test.xml", result);
    }

    // ---- PropertyChanged ----

    [Fact]
    public void PropertyChanged_FiredForCurrentMode()
    {
        var vm = MakeVm();
        var changed = new List<string>();
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName!);

        vm.SwitchMode(TranslationEditMode.Notes);

        Assert.Contains("CurrentMode", changed);
    }

    [Fact]
    public void PropertyChanged_FiredForProgressText()
    {
        var vm = MakeVm();
        var changed = new List<string>();
        vm.PropertyChanged += (_, e) => changed.Add(e.PropertyName!);

        vm.ProgressText = "5/10";

        Assert.Contains("ProgressText", changed);
    }

    // ---- SetCurrentReviewState with aggregation ----

    [Fact]
    public void SetCurrentReviewState_WithAggregation_ShowsMultiUserFormat()
    {
        var vm = MakeVm();

        var agg = new SegmentReviewAggregation
        {
            SegmentKey = "test|Body|1",
            ByReviewer = new Dictionary<string, TranslationReviewEntry>(StringComparer.OrdinalIgnoreCase)
            {
                ["alice"] = new TranslationReviewEntry { Status = "approved", Reviewer = "alice" },
                ["bob"] = new TranslationReviewEntry { Status = "needs-work", Reviewer = "bob" }
            }
        };

        vm.SetCurrentReviewState("approved", "alice", DateTime.UtcNow, agg);

        Assert.Contains("Approved", vm.ReviewStateText);
        Assert.Contains("alice", vm.ReviewStateText);
        Assert.Contains("Needs work", vm.ReviewStateText);
        Assert.Contains("bob", vm.ReviewStateText);
    }

    [Fact]
    public void SetCurrentReviewState_NullAggregation_FallsBackToSingleUserFormat()
    {
        var vm = MakeVm();

        vm.SetCurrentReviewState("approved", "alice", new DateTime(2026, 1, 15, 10, 30, 0, DateTimeKind.Utc), null);

        Assert.Contains("Approved", vm.ReviewStateText);
        Assert.Contains("alice", vm.ReviewStateText);
        // Should NOT contain multi-user format markers like parenthesized counts
        Assert.DoesNotContain("(1)", vm.ReviewStateText);
    }

    [Fact]
    public void SetCurrentReviewState_EmptyAggregation_ShowsUnreviewed()
    {
        var vm = MakeVm();

        // Aggregation with no reviewers
        var agg = new SegmentReviewAggregation { SegmentKey = "test|Body|1" };

        // With only 0 reviewers in ByReviewer, Count <= 1, so it falls through
        // to the single-user path. With null status, shows "Unreviewed".
        vm.SetCurrentReviewState(null, null, null, agg);

        Assert.Equal("Unreviewed", vm.ReviewStateText);
    }

    [Fact]
    public void SetCurrentReviewState_SingleReviewerAggregation_FallsBackToSingleUserFormat()
    {
        var vm = MakeVm();

        // Aggregation with exactly 1 reviewer â€” should NOT use multi-user format
        var agg = new SegmentReviewAggregation
        {
            SegmentKey = "test|Body|1",
            ByReviewer = new Dictionary<string, TranslationReviewEntry>(StringComparer.OrdinalIgnoreCase)
            {
                ["alice"] = new TranslationReviewEntry { Status = "approved", Reviewer = "alice" }
            }
        };

        vm.SetCurrentReviewState("approved", "alice", new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc), agg);

        // Single reviewer path: "Approved â€” alice â€” date"
        Assert.Contains("Approved", vm.ReviewStateText);
        Assert.Contains("alice", vm.ReviewStateText);
        // Should NOT use the multi-user "(1):" format
        Assert.DoesNotContain("(1):", vm.ReviewStateText);
    }
}
