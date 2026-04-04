using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using CbetaTranslator.App.Infrastructure;
using CbetaTranslator.App.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CbetaTranslator.App.Views;

public partial class VocabularyAnalysisDialog : Window
{
    private readonly List<VocabularyItem> _allItems;
    private readonly ICedictDictionary _cedict = App.Services.GetRequiredService<ICedictDictionary>();
    private readonly IGrammarReferenceService _grammar = App.Services.GetRequiredService<IGrammarReferenceService>();
    private HoverDictionaryBehaviorTextBox? _hoverPreview;

    public VocabularyAnalysisDialog()
    {
        InitializeComponent();
        _allItems = new List<VocabularyItem>();
    }

    public VocabularyAnalysisDialog(List<VocabularyItem> items) : this()
    {
        _allItems = items ?? new List<VocabularyItem>();

        var list = this.FindControl<ListBox>("VocabList");
        if (list != null)
        {
            list.ItemsSource = _allItems;
            list.SelectionChanged += OnSelectionChanged;
            list.SelectedIndex = _allItems.Count > 0 ? 0 : -1;
        }

        var txtFilter = this.FindControl<TextBox>("TxtFilter");
        if (txtFilter != null)
            txtFilter.TextChanged += OnFilterChanged;

        SetupHoverPreview();
        UpdatePreview(_allItems.FirstOrDefault()?.Phrase ?? "");
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private void SetupHoverPreview()
    {
        var preview = this.FindControl<TextBox>("TxtPreviewPhrase");
        var overlay = this.FindControl<Canvas>("DictOverlayCanvas");
        if (preview == null || overlay == null) return;

        try { _hoverPreview?.Dispose(); } catch { }
        try { _hoverPreview = new HoverDictionaryBehaviorTextBox(preview, _cedict, _grammar, overlay); } catch { }
    }

    private void UpdatePreview(string phrase)
    {
        var preview = this.FindControl<TextBox>("TxtPreviewPhrase");
        if (preview != null)
            preview.Text = phrase ?? "";
    }

    private void OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        UpdatePreview((sender as ListBox)?.SelectedItem is VocabularyItem item ? item.Phrase : "");
    }

    private void OnFilterChanged(object? sender, TextChangedEventArgs e)
    {
        var filter = (sender as TextBox)?.Text?.Trim() ?? "";
        var list = this.FindControl<ListBox>("VocabList");
        if (list == null) return;

        var filtered = string.IsNullOrEmpty(filter)
            ? _allItems
            : _allItems.Where(v => v.Phrase.Contains(filter, StringComparison.Ordinal)).ToList();

        list.ItemsSource = filtered;
        list.SelectedIndex = filtered.Count > 0 ? 0 : -1;
        UpdatePreview(filtered.FirstOrDefault()?.Phrase ?? "");
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        try { _hoverPreview?.Dispose(); } catch { }
        Close();
    }
}