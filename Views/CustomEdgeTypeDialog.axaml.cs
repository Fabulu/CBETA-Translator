// Views/CustomEdgeTypeDialog.axaml.cs
using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using ReadZen.App.Models;

namespace ReadZen.App.Views;

/// <summary>
/// Dialog for creating a custom edge type with a name, color, and directionality.
/// </summary>
public partial class CustomEdgeTypeDialog : Window
{
    private static readonly List<(string Name, string Hex)> PresetColors = new()
    {
        ("Blue", "#59B3FF"),
        ("Green", "#51D996"),
        ("Orange", "#FF8A65"),
        ("Purple", "#AB47BC"),
        ("Red", "#FF6B6B"),
        ("Gold", "#FFB347"),
        ("Cyan", "#4DD0E1"),
        ("Gray", "#9E9E9E"),
    };

    private readonly ScholarNodeType _fromType;
    private readonly ScholarNodeType _toType;
    private string _selectedColorHex = "#59B3FF";
    private readonly List<Border> _swatchBorders = new();

    public CustomEdgeTypeDialog()
    {
        InitializeComponent();
    }

    public CustomEdgeTypeDialog(ScholarNodeType fromType, ScholarNodeType toType) : this()
    {
        _fromType = fromType;
        _toType = toType;
        BuildSwatches();
        WireButtons();
        KeyDown += OnKeyDown;

        Opened += (_, _) =>
        {
            var txtName = this.FindControl<TextBox>("TxtName");
            txtName?.Focus();
        };
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private void BuildSwatches()
    {
        var panel = this.FindControl<StackPanel>("SwatchPanel");
        if (panel == null) return;

        for (int i = 0; i < PresetColors.Count; i++)
        {
            var (name, hex) = PresetColors[i];
            var border = new Border
            {
                Width = 28,
                Height = 28,
                CornerRadius = new CornerRadius(4),
                Background = new SolidColorBrush(Color.Parse(hex)),
                BorderThickness = new Thickness(2),
                BorderBrush = i == 0 ? Brushes.White : Brushes.Transparent,
                Cursor = new Cursor(StandardCursorType.Hand),
                Tag = hex
            };
            ToolTip.SetTip(border, name);

            var capturedIndex = i;
            border.PointerPressed += (_, _) => SelectSwatch(capturedIndex);
            _swatchBorders.Add(border);
            panel.Children.Add(border);
        }
    }

    private void SelectSwatch(int index)
    {
        _selectedColorHex = PresetColors[index].Hex;
        for (int i = 0; i < _swatchBorders.Count; i++)
        {
            _swatchBorders[i].BorderBrush = i == index ? Brushes.White : Brushes.Transparent;
        }
    }

    private void WireButtons()
    {
        var btnOk = this.FindControl<Button>("BtnOk");
        var btnCancel = this.FindControl<Button>("BtnCancel");

        if (btnOk != null)
            btnOk.Click += (_, _) => TryConfirm();

        if (btnCancel != null)
            btnCancel.Click += (_, _) => Close(null as object);
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Close(null as object);
            e.Handled = true;
        }
        else if (e.Key == Key.Return || e.Key == Key.Enter)
        {
            TryConfirm();
            e.Handled = true;
        }
    }

    private void TryConfirm()
    {
        var txtName = this.FindControl<TextBox>("TxtName");
        var chkDirectional = this.FindControl<CheckBox>("ChkDirectional");

        var name = txtName?.Text?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(name))
        {
            txtName?.Focus();
            return;
        }

        // Generate a slug-style ID from the name
        var id = "custom-" + name.ToLowerInvariant()
            .Replace(' ', '-')
            .Replace('\t', '-');

        // Convert display name: capitalize first letter
        var displayName = char.ToUpperInvariant(name[0]) + name.Substring(1);

        var result = new EdgeTypeDefinition
        {
            Id = id,
            DisplayName = displayName,
            Description = $"Custom: {displayName}",
            AllowedFromTypes = new() { _fromType },
            AllowedToTypes = new() { _toType },
            ColorHex = _selectedColorHex,
            IsBuiltIn = false,
            IsDirectional = chkDirectional?.IsChecked ?? true
        };

        Close(result);
    }
}
