using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using CbetaTranslator.App.Services;

namespace CbetaTranslator.App.Views;

public partial class VocabularyAnalysisDialog : Window
{
    private readonly List<VocabularyItem> _allItems;

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
            list.ItemsSource = _allItems;

        var txtFilter = this.FindControl<TextBox>("TxtFilter");
        if (txtFilter != null)
            txtFilter.TextChanged += OnFilterChanged;
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private void OnFilterChanged(object? sender, TextChangedEventArgs e)
    {
        var filter = (sender as TextBox)?.Text?.Trim() ?? "";
        var list = this.FindControl<ListBox>("VocabList");
        if (list == null) return;

        if (string.IsNullOrEmpty(filter))
        {
            list.ItemsSource = _allItems;
        }
        else
        {
            list.ItemsSource = _allItems
                .Where(v => v.Phrase.Contains(filter, StringComparison.Ordinal))
                .ToList();
        }
    }

    private void OnCloseClick(object? sender, RoutedEventArgs e)
    {
        Close();
    }
}
