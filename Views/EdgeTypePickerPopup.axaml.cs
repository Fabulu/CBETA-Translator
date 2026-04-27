// Views/EdgeTypePickerPopup.axaml.cs
using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using ReadZen.App.Models;

namespace ReadZen.App.Views;

public partial class EdgeTypePickerPopup : Window
{
    private readonly List<EdgeTypeItemVm> _items = new();

    /// <summary>
    /// The edge type selected by the user, or null if cancelled.
    /// </summary>
    public EdgeTypeDefinition? SelectedType { get; private set; }

    public EdgeTypePickerPopup()
    {
        InitializeComponent();
    }

    public EdgeTypePickerPopup(ScholarNodeType fromType, ScholarNodeType toType) : this()
    {
        var validTypes = EdgeTypeRegistry.GetValidTypes(fromType, toType);
        for (int i = 0; i < validTypes.Count; i++)
        {
            _items.Add(new EdgeTypeItemVm
            {
                Definition = validTypes[i],
                DisplayName = validTypes[i].DisplayName,
                Description = validTypes[i].Description,
                ColorHex = new SolidColorBrush(Color.Parse(validTypes[i].ColorHex)),
                ShortcutHint = i < 9 ? $"[{i + 1}]" : ""
            });
        }

        if (_items.Count == 0)
        {
            // No valid types for this pair -- defer close until after ShowDialog
            Opened += (_, _) => Close(null);
            return;
        }

        var listBox = this.FindControl<ListBox>("TypeList");
        if (listBox != null)
        {
            listBox.ItemsSource = _items;
            if (_items.Count > 0)
                listBox.SelectedIndex = 0;

            listBox.DoubleTapped += OnListDoubleTapped;
        }

        KeyDown += OnKeyDown;
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            SelectedType = null;
            Close(null as object);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Enter || e.Key == Key.Return)
        {
            ConfirmSelection();
            e.Handled = true;
            return;
        }

        // Number keys 1-9 for quick selection
        int index = e.Key switch
        {
            Key.D1 => 0,
            Key.D2 => 1,
            Key.D3 => 2,
            Key.D4 => 3,
            Key.D5 => 4,
            Key.D6 => 5,
            Key.D7 => 6,
            Key.D8 => 7,
            Key.D9 => 8,
            Key.NumPad1 => 0,
            Key.NumPad2 => 1,
            Key.NumPad3 => 2,
            Key.NumPad4 => 3,
            Key.NumPad5 => 4,
            Key.NumPad6 => 5,
            Key.NumPad7 => 6,
            Key.NumPad8 => 7,
            Key.NumPad9 => 8,
            _ => -1
        };

        if (index >= 0 && index < _items.Count)
        {
            SelectedType = _items[index].Definition;
            Close(SelectedType);
            e.Handled = true;
        }
    }

    private void OnListDoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        ConfirmSelection();
    }

    private void ConfirmSelection()
    {
        var listBox = this.FindControl<ListBox>("TypeList");
        if (listBox?.SelectedItem is EdgeTypeItemVm item)
        {
            SelectedType = item.Definition;
            Close(SelectedType);
        }
    }
}

/// <summary>
/// View model for a single row in the edge type picker list.
/// </summary>
public sealed class EdgeTypeItemVm
{
    public EdgeTypeDefinition Definition { get; set; } = null!;
    public string DisplayName { get; set; } = "";
    public string Description { get; set; } = "";
    public ISolidColorBrush ColorHex { get; set; } = Brushes.Gray;
    public string ShortcutHint { get; set; } = "";
}
