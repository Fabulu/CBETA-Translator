using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.VisualTree;
using CbetaTranslator.App.Models;
using CbetaTranslator.App.ViewModels;

namespace CbetaTranslator.App.Views;

public partial class ComparePassagesWindow : Window
{
    private static readonly IBrush HighlightBrush =
        new SolidColorBrush(Color.FromArgb(80, 100, 180, 255));

    public ComparePassagesWindow()
    {
        InitializeComponent();
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
            var textBlock = FindChildByName<TextBlock>(container, "ZhTextBlock");
            if (textBlock == null) continue;

            BuildHighlightedInlines(textBlock, item.Passage.ZhText, item.SharedZhRanges);
        }
    }

    private static void BuildHighlightedInlines(
        TextBlock textBlock,
        string text,
        List<(int Start, int Length)> ranges)
    {
        textBlock.Inlines?.Clear();
        if (textBlock.Inlines == null) return;

        if (string.IsNullOrEmpty(text))
            return;

        if (ranges.Count == 0)
        {
            textBlock.Inlines.Add(new Run(text));
            return;
        }

        int pos = 0;
        foreach (var (start, length) in ranges.OrderBy(r => r.Start))
        {
            int rangeStart = start;
            int rangeEnd = start + length;

            // Clamp to text bounds
            if (rangeStart >= text.Length) break;
            if (rangeEnd > text.Length) rangeEnd = text.Length;

            // Text before highlight
            if (rangeStart > pos)
            {
                textBlock.Inlines.Add(new Run(text.Substring(pos, rangeStart - pos)));
            }

            // Highlighted text
            textBlock.Inlines.Add(new Run(text.Substring(rangeStart, rangeEnd - rangeStart))
            {
                Background = HighlightBrush,
            });

            pos = rangeEnd;
        }

        // Remaining text
        if (pos < text.Length)
        {
            textBlock.Inlines.Add(new Run(text.Substring(pos)));
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
