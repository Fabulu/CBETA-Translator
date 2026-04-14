// Views/TimelineSlider.cs
// Visual timeline slider for browsing git history of a translation.
// Each notch = one commit. Drag to time travel. Rightmost = present.

using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using ReadZen.App.Models;

namespace ReadZen.App.Views;

public partial class TimelineSlider : UserControl
{
    private Canvas? _canvas;
    private Border? _badge;
    private TextBlock? _txtBadge;
    private List<GitCommitEntry> _commits = new();
    private int _selectedIndex; // 0 = present (newest), _commits.Count = oldest
    private readonly List<Ellipse> _notches = new();
    private Ellipse? _thumb;
    private bool _suppressEvents;

    /// <summary>Fired when user selects a version. Value is commit hash or null for "(current)".</summary>
    public event EventHandler<string?>? VersionChanged;

    public TimelineSlider()
    {
        InitializeComponent();
        _canvas = this.FindControl<Canvas>("TrackCanvas");
        _badge = this.FindControl<Border>("BadgeTimeTraveling");
        _txtBadge = this.FindControl<TextBlock>("TxtBadge");

        if (_canvas != null)
        {
            _canvas.PointerPressed += OnTrackPointerPressed;
            _canvas.SizeChanged += (_, _) => Redraw();
        }
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    /// <summary>
    /// Sets the commit list and redraws. Commits should be newest-first.
    /// </summary>
    public void SetCommits(List<GitCommitEntry> commits)
    {
        _suppressEvents = true;
        try
        {
            _commits = commits ?? new();
            _selectedIndex = 0; // present
            IsVisible = _commits.Count > 0;
            Redraw();
            UpdateBadge();
        }
        finally
        {
            _suppressEvents = false;
        }
    }

    public void Clear()
    {
        _commits = new();
        _selectedIndex = 0;
        IsVisible = false;
        _canvas?.Children.Clear();
        _notches.Clear();
        _thumb = null;
        UpdateBadge();
    }

    private void Redraw()
    {
        if (_canvas == null || _commits.Count == 0) return;

        _canvas.Children.Clear();
        _notches.Clear();

        var w = _canvas.Bounds.Width;
        var h = _canvas.Bounds.Height;
        if (w < 20) return;

        var totalPoints = _commits.Count + 1; // +1 for "(current)"
        var spacing = w / Math.Max(totalPoints - 1, 1);

        // Draw track line
        var trackLine = new Line
        {
            StartPoint = new Point(0, h / 2),
            EndPoint = new Point(w, h / 2),
            Stroke = new SolidColorBrush(Color.FromArgb(60, 255, 255, 255)),
            StrokeThickness = 2,
        };
        _canvas.Children.Add(trackLine);

        // Draw notches: index 0 = "(current)" on the right, last = oldest on the left
        for (int i = 0; i < totalPoints; i++)
        {
            var x = w - (i * spacing);
            var isSelected = i == _selectedIndex;
            var isCurrent = i == 0;

            var notch = new Ellipse
            {
                Width = isSelected ? 12 : (isCurrent ? 8 : 6),
                Height = isSelected ? 12 : (isCurrent ? 8 : 6),
                Fill = isSelected
                    ? new SolidColorBrush(Color.FromRgb(100, 160, 255))
                    : isCurrent
                        ? new SolidColorBrush(Color.FromRgb(0, 180, 0))
                        : new SolidColorBrush(Color.FromArgb(120, 255, 255, 255)),
            };

            Canvas.SetLeft(notch, x - notch.Width / 2);
            Canvas.SetTop(notch, (h - notch.Height) / 2);

            // Tooltip
            if (isCurrent)
                ToolTip.SetTip(notch, "(current version)");
            else if (i - 1 < _commits.Count)
            {
                var c = _commits[i - 1];
                ToolTip.SetTip(notch, $"{c.Date:yyyy-MM-dd HH:mm}\n{c.Author}\n{c.Subject}");
            }

            _canvas.Children.Add(notch);
            _notches.Add(notch);
        }

        // Thumb indicator (larger selected notch is already drawn above)
        _thumb = _notches.Count > _selectedIndex ? _notches[_selectedIndex] : null;
    }

    private void OnTrackPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (_suppressEvents || _canvas == null || _commits.Count == 0) return;

        var pos = e.GetPosition(_canvas);
        var w = _canvas.Bounds.Width;
        if (w < 20) return;

        var totalPoints = _commits.Count + 1;
        var spacing = w / Math.Max(totalPoints - 1, 1);

        // Find nearest notch
        var fromRight = w - pos.X;
        var nearestIndex = (int)Math.Round(fromRight / spacing);
        nearestIndex = Math.Clamp(nearestIndex, 0, _commits.Count);

        if (nearestIndex == _selectedIndex) return;

        _selectedIndex = nearestIndex;
        Redraw();
        UpdateBadge();

        // Fire event
        if (_selectedIndex == 0)
            VersionChanged?.Invoke(this, null); // "(current)"
        else if (_selectedIndex - 1 < _commits.Count)
            VersionChanged?.Invoke(this, _commits[_selectedIndex - 1].Hash);
    }

    private void UpdateBadge()
    {
        if (_badge == null || _txtBadge == null) return;

        if (_selectedIndex == 0 || _commits.Count == 0)
        {
            _badge.IsVisible = false;
            return;
        }

        _badge.IsVisible = true;
        if (_selectedIndex - 1 < _commits.Count)
        {
            var c = _commits[_selectedIndex - 1];
            _txtBadge.Text = $"\u23f3 Viewing version from {c.DateDisplay} by {c.Author}";
        }
    }
}
