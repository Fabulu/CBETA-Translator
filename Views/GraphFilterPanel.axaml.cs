using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ReadZen.App.Views;

public partial class GraphFilterPanel : UserControl
{
    public event EventHandler? FiltersChanged;

    public bool ShowPassages => ChkPassages.IsChecked == true;
    public bool ShowConcepts => ChkConcepts.IsChecked == true;
    public bool ShowMasters => ChkMasters.IsChecked == true;
    public bool ShowTerms => ChkTerms.IsChecked == true;
    public bool ShowCollections => ChkCollections.IsChecked == true;
    public bool ShowLinks => ChkLinks.IsChecked == true;

    public GraphFilterPanel()
    {
        InitializeComponent();

        ChkPassages.IsCheckedChanged += OnFilterChanged;
        ChkConcepts.IsCheckedChanged += OnFilterChanged;
        ChkMasters.IsCheckedChanged += OnFilterChanged;
        ChkTerms.IsCheckedChanged += OnFilterChanged;
        ChkCollections.IsCheckedChanged += OnFilterChanged;
        ChkLinks.IsCheckedChanged += OnFilterChanged;
        BtnShowAll.Click += OnShowAllClick;
    }

    private void OnFilterChanged(object? sender, RoutedEventArgs e)
    {
        FiltersChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OnShowAllClick(object? sender, RoutedEventArgs e)
    {
        ChkPassages.IsChecked = true;
        ChkConcepts.IsChecked = true;
        ChkMasters.IsChecked = true;
        ChkTerms.IsChecked = true;
        ChkCollections.IsChecked = true;
        ChkLinks.IsChecked = true;
        FiltersChanged?.Invoke(this, EventArgs.Empty);
    }
}
