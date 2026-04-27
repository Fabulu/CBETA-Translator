// Views/ConceptCreationDialog.axaml.cs
using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using ReadZen.App.Models;

namespace ReadZen.App.Views;

public partial class ConceptCreationDialog : Window
{
    private static readonly List<(string Name, string Hex)> PresetColors = new()
    {
        ("Coral", "#FF8A65"),
        ("Blue", "#64B5F6"),
        ("Green", "#81C784"),
        ("Purple", "#AB47BC"),
        ("Gold", "#FFB347"),
        ("Cyan", "#4DD0E1"),
    };

    private string _selectedColorHex = "#FF8A65";
    private readonly List<Border> _swatchBorders = new();

    /// <summary>
    /// The created concept node, or null if cancelled.
    /// </summary>
    public ConceptNode? Result { get; private set; }

    public ConceptCreationDialog()
    {
        InitializeComponent();
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
            btnCancel.Click += (_, _) =>
            {
                Result = null;
                Close(null as object);
            };
    }

    private void OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Escape)
        {
            Result = null;
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
        var txtDesc = this.FindControl<TextBox>("TxtDescription");

        var name = txtName?.Text?.Trim() ?? "";
        if (string.IsNullOrWhiteSpace(name))
        {
            // Focus the name field if empty
            txtName?.Focus();
            return;
        }

        Result = new ConceptNode
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = name,
            Description = txtDesc?.Text?.Trim() ?? "",
            ColorHex = _selectedColorHex,
            CreatedUtc = DateTimeOffset.UtcNow,
            Status = ConceptStatus.Active
        };

        Close(Result);
    }
}
