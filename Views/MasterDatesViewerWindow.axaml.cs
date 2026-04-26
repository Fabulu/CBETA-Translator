using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using ReadZen.App.Models;
using ReadZen.App.ViewModels;

namespace ReadZen.App.Views;

public partial class MasterDatesViewerWindow : Window
{
    private readonly MasterDatesViewerViewModel _vm;

    public MasterDatesViewerWindow()
    {
        InitializeComponent();
        _vm = new MasterDatesViewerViewModel(new List<MasterDateEntry>());
        DataContext = _vm;
    }

    public MasterDatesViewerWindow(List<MasterDateEntry> masters)
    {
        InitializeComponent();
        _vm = new MasterDatesViewerViewModel(masters);
        DataContext = _vm;
    }

    private void FilterBox_KeyUp(object? sender, KeyEventArgs e)
    {
        _vm.FilterText = FilterBox.Text ?? "";
        CountLabel.Text = $"{_vm.FilteredCount} masters";
    }

    private void TimelineCountSlider_ValueChanged(object? sender, Avalonia.Controls.Primitives.RangeBaseValueChangedEventArgs e)
    {
        if (_vm == null) return;
        var count = (int)e.NewValue;
        _vm.RebuildTimeline(count);
        if (TimelineCountLabel != null)
            TimelineCountLabel.Text = count.ToString();
        if (TimelineChart != null)
            TimelineChart.MinHeight = System.Math.Max(600, count * 24 + 40);
    }
}
