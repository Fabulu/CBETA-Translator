using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using Avalonia.Controls;
using CbetaTranslator.App.ViewModels;
using CbetaTranslator.App.Views;
using Xunit;

namespace CbetaTranslator.Tests.Views;

public class TranslationTabViewInteractionTests
{
    private static TranslationTabView CreateViewShell(out TranslationTabViewModel vm, out ComboBox cmb, out TextBlock review, out TextBlock progress)
    {
        var view = (TranslationTabView)RuntimeHelpers.GetUninitializedObject(typeof(TranslationTabView));
        vm = new TranslationTabViewModel();
        cmb = new ComboBox();
        review = new TextBlock();
        progress = new TextBlock();

        SetField(typeof(TranslationTabView), view, "_vm", vm);
        SetField(typeof(TranslationTabView), view, "_cmbTranslationSource", cmb);
        SetField(typeof(TranslationTabView), view, "_txtReviewState", review);
        SetField(typeof(TranslationTabView), view, "_txtProgress", progress);
        return view;
    }

    private static void SetField(Type type, object target, string name, object? value)
    {
        var field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Missing field {name} on {type.Name}");
        field.SetValue(target, value);
    }


    [Fact]
    public void ParseProjectionBlocksWithOffsets_CrlfText_PreservesBlockSliceBoundaries()
    {
        const string text = "<1>\r\nZH: No. 1998A\r\nEN: \r\n\r\n<2>\r\nZH: ?????????\r\nEN: \r\n\r\n<3>\r\nZH: ?????????\r\nEN: \r\n";

        var parse = typeof(TranslationTabView).GetMethod("ParseProjectionBlocksWithOffsets", BindingFlags.Static | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException("Missing ParseProjectionBlocksWithOffsets");
        var blocks = (System.Collections.IList?)parse.Invoke(null, new object?[] { text })
            ?? throw new InvalidOperationException("Parser returned null");

        Assert.Equal(3, blocks.Count);

        var block2 = blocks[1]!;
        var startProp = block2.GetType().GetProperty("BlockStartOffset")
            ?? throw new InvalidOperationException("Missing BlockStartOffset");
        var endProp = block2.GetType().GetProperty("BlockEndOffsetExclusive")
            ?? throw new InvalidOperationException("Missing BlockEndOffsetExclusive");

        int start = (int)(startProp.GetValue(block2) ?? -1);
        int end = (int)(endProp.GetValue(block2) ?? -1);
        var slice = text.Substring(start, end - start).TrimEnd('\r', '\n');

        Assert.Equal("<2>\r\nZH: ?????????\r\nEN: ", slice);
    }    [Fact]
    public void SetTranslationSourceOptions_AndIndex_UpdateComboBox()
    {
        var view = CreateViewShell(out _, out var cmb, out _, out _);

        view.SetTranslationSourceOptions(new() { "Community", "My Translation", "alice" });
        view.SetTranslationSourceIndex(2);

        Assert.Equal(3, ((System.Collections.ICollection)cmb.ItemsSource!).Count);
        Assert.Equal(2, cmb.SelectedIndex);
    }

    [Fact]
    public void SetCurrentReviewState_UpdatesVisibleReviewText()
    {
        var view = CreateViewShell(out _, out _, out var review, out _);

        view.SetCurrentReviewState("Approved", "alice", new DateTime(2026, 4, 5, 7, 0, 0, DateTimeKind.Utc));

        Assert.False(string.IsNullOrWhiteSpace(review.Text));
        Assert.Contains("Approved", review.Text);
        Assert.Contains("alice", review.Text);
    }

    [Fact]
    public void SetProgressStats_UpdatesVisibleProgressText()
    {
        var view = CreateViewShell(out _, out _, out _, out var progress);

        view.SetProgressStats(3, 2, 10);

        Assert.False(string.IsNullOrWhiteSpace(progress.Text));
        Assert.Contains("3", progress.Text);
        Assert.Contains("10", progress.Text);
    }
}

