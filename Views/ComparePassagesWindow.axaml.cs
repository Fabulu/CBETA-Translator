using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.VisualTree;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using ReadZen.App.Infrastructure;
using ReadZen.App.Models;
using ReadZen.App.Services;
using ReadZen.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace ReadZen.App.Views;

public partial class ComparePassagesWindow : Window
{
    private readonly ICedictDictionary _cedict = App.Services.GetRequiredService<ICedictDictionary>();
    private readonly IGrammarReferenceService _grammar = App.Services.GetRequiredService<IGrammarReferenceService>();
    private readonly List<IDisposable> _hoverBehaviors = new();
    private Canvas? _dictOverlayCanvas;

    private static readonly IBrush HighlightBrush =
        new SolidColorBrush(Color.FromArgb(80, 100, 180, 255));

    public ComparePassagesWindow()
    {
        InitializeComponent();
        _dictOverlayCanvas = this.FindControl<Canvas>("DictOverlayCanvas");
        Closed += (_, _) => DisposeHoverDictionary();
    }

    public ComparePassagesWindow(List<ScholarPassage> passages) : this()
    {
        var vm = new ComparePassagesViewModel(passages);
        DataContext = vm;

        // Render ZH highlights after the visual tree is ready
        Opened += (_, _) => RenderZhHighlights(vm);
    }

    private void RenderZhHighlights(ComparePassagesViewModel vm)
    {
        var itemsControl = this.FindControl<ItemsControl>("PassageItems");
        if (itemsControl == null) return;

        // Walk the container items and find each ZhTextBlock by Tag
        for (int i = 0; i < vm.Items.Count; i++)
        {
            var container = itemsControl.ContainerFromIndex(i);
            if (container == null) continue;

            var item = vm.Items[i];
            var editor = FindChildByName<TextEditor>(container, "ZhEditor");
            if (editor == null) continue;

            BuildHighlightedEditor(editor, item.Passage.ZhText ?? "", item.SharedZhRanges);
            AttachHoverDictionary(editor);
        }
    }

    private void BuildHighlightedEditor(
        TextEditor editor,
        string text,
        List<(int Start, int Length)> ranges)
    {
        editor.IsReadOnly = true;
        editor.ShowLineNumbers = false;
        editor.WordWrap = true;
        editor.Background = Brushes.Transparent;
        editor.Text = text ?? string.Empty;

        if (editor.TextArea?.TextView == null)
            return;

        var transformers = editor.TextArea.TextView.LineTransformers
            .OfType<ComparePassageHighlightTransformer>()
            .ToList();
        foreach (var transformer in transformers)
            editor.TextArea.TextView.LineTransformers.Remove(transformer);

        editor.TextArea.TextView.LineTransformers.Add(new ComparePassageHighlightTransformer(ranges));
        editor.TextArea.TextView.Redraw();
    }

    private void AttachHoverDictionary(TextEditor editor)
    {
        if (_dictOverlayCanvas == null)
            return;

        try
        {
            _hoverBehaviors.Add(new HoverDictionaryBehaviorEdit(editor, _cedict, _grammar, _dictOverlayCanvas));
        }
        catch { }
    }

    private void DisposeHoverDictionary()
    {
        foreach (var behavior in _hoverBehaviors)
        {
            try { behavior.Dispose(); } catch { }
        }
        _hoverBehaviors.Clear();
    }

    private sealed class ComparePassageHighlightTransformer : DocumentColorizingTransformer
    {
        private readonly List<(int Start, int Length)> _ranges;

        public ComparePassageHighlightTransformer(IEnumerable<(int Start, int Length)> ranges)
        {
            _ranges = ranges.OrderBy(r => r.Start).ToList();
        }

        protected override void ColorizeLine(DocumentLine line)
        {
            int lineStart = line.Offset;
            int lineEnd = line.EndOffset;

            foreach (var (start, length) in _ranges)
            {
                if (start >= lineEnd)
                    break;
                if (start + length <= lineStart)
                    continue;

                int s = Math.Max(start, lineStart);
                int e = Math.Min(start + length, lineEnd);
                if (e <= s)
                    continue;

                ChangeLinePart(s, e, el => el.TextRunProperties.SetBackgroundBrush(HighlightBrush));
            }
        }
    }

    private static T? FindChildByName<T>(Visual parent, string name) where T : Control
    {
        if (parent is T ctrl && ctrl.Name == name)
            return ctrl;

        foreach (var child in parent.GetVisualChildren())
        {
            if (child is Visual v)
            {
                var result = FindChildByName<T>(v, name);
                if (result != null) return result;
            }
        }

        return null;
    }

    private void OnCloseClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Close();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}
