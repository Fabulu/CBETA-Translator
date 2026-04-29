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
    private readonly ScholarNodeType _fromType;
    private readonly ScholarNodeType _toType;

    /// <summary>
    /// Sentinel item representing "Custom..." at the bottom of the list.
    /// </summary>
    private static readonly EdgeTypeDefinition CustomSentinel = new()
    {
        Id = "__custom__", DisplayName = "Custom...", Description = "Define a custom edge type",
        ColorHex = "#9E9E9E", IsBuiltIn = false, IsDirectional = true
    };

    /// <summary>
    /// The edge type selected by the user, or null if cancelled.
    /// </summary>
    public EdgeTypeDefinition? SelectedType { get; private set; }

    public EdgeTypePickerPopup()
    {
        InitializeComponent();
    }

    public EdgeTypePickerPopup(ScholarNodeType fromType, ScholarNodeType toType,
        IEnumerable<EdgeTypeDefinition>? customTypes = null) : this()
    {
        _fromType = fromType;
        _toType = toType;

        var validTypes = EdgeTypeRegistry.GetValidTypes(fromType, toType, customTypes);
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

        // Always add the "Custom..." sentinel at the end
        _items.Add(new EdgeTypeItemVm
        {
            Definition = CustomSentinel,
            DisplayName = "Custom\u2026",
            Description = "Define a custom edge type",
            ColorHex = new SolidColorBrush(Color.Parse("#9E9E9E")),
            ShortcutHint = ""
        });

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
            _ = ConfirmSelectionAsync();
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
        _ = ConfirmSelectionAsync();
    }

    private async System.Threading.Tasks.Task ConfirmSelectionAsync()
    {
        var listBox = this.FindControl<ListBox>("TypeList");
        if (listBox?.SelectedItem is EdgeTypeItemVm item)
        {
            if (item.Definition.Id == CustomSentinel.Id)
            {
                var dialog = new CustomEdgeTypeDialog(_fromType, _toType);
                var result = await dialog.ShowDialog<EdgeTypeDefinition?>(this);
                if (result != null)
                {
                    SelectedType = result;
                    Close(SelectedType);
                }
            }
            else
            {
                SelectedType = item.Definition;
                Close(SelectedType);
            }
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
