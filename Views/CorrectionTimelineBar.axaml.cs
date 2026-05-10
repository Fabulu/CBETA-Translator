// Views/CorrectionTimelineBar.axaml.cs
// Segmented slider with playback controls for scrubbing through
// a critical edition's correction history (correction-log.md).

using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using ReadZen.App.Services;
using ReadZen.App.Models;

namespace ReadZen.App.Views;

/// <summary>
/// A compact transport bar for stepping through correction history.
/// Layout: [Prev] ---slider--- [Next] [Play/Pause] [Speed] [Progress]
/// </summary>
public partial class CorrectionTimelineBar : UserControl
{
    private Slider? _slider;
    private Button? _btnPrev;
    private Button? _btnNext;
    private Button? _btnPlayPause;
    private TextBlock? _txtSpeed;
    private TextBlock? _txtProgress;

    private List<CorrectionEntry> _corrections = new();
    private List<AnchorEvent>? _anchorEvents;
    private bool _isAnchorMode;
    private bool _suppressEvents;

    // Playback state
    private readonly DispatcherTimer _playbackTimer;
    private bool _isPlaying;
    private int _speedIndex = 1; // index into SpeedMultipliers
    private static readonly double[] SpeedMultipliers = { 0.5, 1.0, 2.0, 5.0 };
    private static readonly string[] SpeedLabels = { "0.5x", "1x", "2x", "5x" };

    /// <summary>Fires when the current step changes (user drag or playback).</summary>
    public event EventHandler<int>? StepChanged;

    /// <summary>Fires when auto-advance reaches the last correction.</summary>
    public event EventHandler? PlaybackCompleted;

    /// <summary>Fires when the user clicks "Return to Present" to exit time-travel mode.</summary>
    public event EventHandler? ReturnToPresent;

    /// <summary>Current step position (0 = raw OCR, Count = fully corrected).</summary>
    public int CurrentStep
    {
        get => _slider != null ? (int)_slider.Value : 0;
        set
        {
            if (_slider == null) return;
            var max = _isAnchorMode
                ? Math.Max((_anchorEvents?.Count ?? 1) - 1, 0)
                : _corrections.Count;
            _slider.Value = Math.Clamp(value, 0, max);
        }
    }

    /// <summary>Whether auto-advance playback is active.</summary>
    public bool IsPlaying => _isPlaying;

    public CorrectionTimelineBar()
    {
        InitializeComponent();

        _slider = this.FindControl<Slider>("SliderStep");
        _btnPrev = this.FindControl<Button>("BtnPrev");
        _btnNext = this.FindControl<Button>("BtnNext");
        _btnPlayPause = this.FindControl<Button>("BtnPlayPause");
        _txtSpeed = this.FindControl<TextBlock>("TxtSpeed");
        _txtProgress = this.FindControl<TextBlock>("TxtProgress");

        // Wire events
        if (_slider != null)
        {
            _slider.PropertyChanged += (_, e) =>
            {
                if (e.Property.Name == "Value" && !_suppressEvents)
                {
                    UpdateProgressText();
                    StepChanged?.Invoke(this, (int)_slider.Value);
                }
            };
        }

        if (_btnPrev != null)
            _btnPrev.Click += (_, _) => StepBy(-1);

        if (_btnNext != null)
            _btnNext.Click += (_, _) => StepBy(+1);

        if (_btnPlayPause != null)
            _btnPlayPause.Click += (_, _) => TogglePlayback();

        if (_txtSpeed != null)
            _txtSpeed.PointerPressed += OnSpeedClicked;

        var btnReturn = this.FindControl<Button>("BtnReturnToPresent");
        if (btnReturn != null)
            btnReturn.Click += (_, _) => OnReturnToPresent();

        // Playback timer (default 1 correction/second)
        _playbackTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1.0) };
        _playbackTimer.Tick += OnPlaybackTick;

        // Stop the timer when the control is removed from the visual tree
        // to prevent stale events firing after navigation away.
        DetachedFromVisualTree += (_, _) => StopPlayback();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    /// <summary>
    /// Loads a correction list and configures the slider range.
    /// The bar remains hidden until the parent sets IsVisible = true.
    /// </summary>
    public void SetCorrections(List<CorrectionEntry> corrections)
    {
        _suppressEvents = true;
        try
        {
            _corrections = corrections ?? new();
            _anchorEvents = null;
            _isAnchorMode = false;
            StopPlayback();

            if (_btnPrev != null) ToolTip.SetTip(_btnPrev, "Previous correction");
            if (_btnNext != null) ToolTip.SetTip(_btnNext, "Next correction");

            if (_slider != null)
            {
                _slider.Maximum = _corrections.Count;
                _slider.Value = _corrections.Count; // start at fully-corrected
            }

            UpdateProgressText();
        }
        finally
        {
            _suppressEvents = false;
        }
    }

    /// <summary>
    /// Loads anchor events and configures the slider for anchor mode.
    /// In anchor mode each step corresponds to one AnchorEvent (no "raw OCR" step 0).
    /// </summary>
    public void SetAnchorEvents(List<AnchorEvent> events)
    {
        _suppressEvents = true;
        try
        {
            _anchorEvents = events;
            _isAnchorMode = true;
            StopPlayback();

            if (_slider != null)
            {
                _slider.Maximum = Math.Max(events.Count - 1, 0);
                _slider.Value = 0;
            }

            UpdateProgressText();
            IsVisible = events.Count > 0;

            if (_btnPrev != null) ToolTip.SetTip(_btnPrev, "Previous event");
            if (_btnNext != null) ToolTip.SetTip(_btnNext, "Next event");
        }
        finally
        {
            _suppressEvents = false;
        }
    }

    /// <summary>Clears all state and stops playback.</summary>
    public void Clear()
    {
        StopPlayback();
        _corrections = new();
        _anchorEvents = null;
        _isAnchorMode = false;

        if (_slider != null)
        {
            _slider.Maximum = 0;
            _slider.Value = 0;
        }

        if (_btnPrev != null) ToolTip.SetTip(_btnPrev, "Previous correction");
        if (_btnNext != null) ToolTip.SetTip(_btnNext, "Next correction");

        UpdateProgressText();
        IsVisible = false;
    }

    // ── Navigation ──────────────────────────────────────────────

    private void StepBy(int delta)
    {
        if (_slider == null) return;

        var max = _isAnchorMode
            ? Math.Max((_anchorEvents?.Count ?? 1) - 1, 0)
            : _corrections.Count;
        if (max == 0) return;

        var next = Math.Clamp((int)_slider.Value + delta, 0, max);
        if ((int)_slider.Value == next) return;

        _slider.Value = next;
        // StepChanged fires via PropertyChanged handler
    }

    // ── Playback ────────────────────────────────────────────────

    private void TogglePlayback()
    {
        if (_isPlaying)
            StopPlayback();
        else
            StartPlayback();
    }

    private void StartPlayback()
    {
        var max = _isAnchorMode
            ? Math.Max((_anchorEvents?.Count ?? 1) - 1, 0)
            : _corrections.Count;
        if (max == 0) return;

        // If at the end, wrap to the beginning
        if (_slider != null && (int)_slider.Value >= max)
            _slider.Value = 0;

        _isPlaying = true;
        UpdatePlayPauseButton();
        ApplySpeedToTimer();
        _playbackTimer.Start();
    }

    private void StopPlayback()
    {
        _isPlaying = false;
        _playbackTimer.Stop();
        UpdatePlayPauseButton();
    }

    private void OnPlaybackTick(object? sender, EventArgs e)
    {
        if (_slider == null) return;

        var max = _isAnchorMode
            ? Math.Max((_anchorEvents?.Count ?? 1) - 1, 0)
            : _corrections.Count;

        var next = (int)_slider.Value + 1;
        if (next > max)
        {
            // Reached the end: auto-pause
            StopPlayback();
            PlaybackCompleted?.Invoke(this, EventArgs.Empty);
            return;
        }

        _slider.Value = next;
    }

    private void UpdatePlayPauseButton()
    {
        if (_btnPlayPause == null) return;
        // U+25B6 = play triangle, U+23F8 = pause
        _btnPlayPause.Content = _isPlaying ? "\u23F8\uFE0E" : "\u25B6\uFE0E";
        ToolTip.SetTip(_btnPlayPause,
            _isPlaying ? "Pause auto-advance" : "Play auto-advance");
    }

    // ── Speed control ───────────────────────────────────────────

    private void OnSpeedClicked(object? sender, PointerPressedEventArgs e)
    {
        _speedIndex = (_speedIndex + 1) % SpeedMultipliers.Length;
        if (_txtSpeed != null)
            _txtSpeed.Text = SpeedLabels[_speedIndex];
        ApplySpeedToTimer();
    }

    private void ApplySpeedToTimer()
    {
        var multiplier = SpeedMultipliers[_speedIndex];
        // Base rate is 1 correction/second; higher multiplier = shorter interval
        _playbackTimer.Interval = TimeSpan.FromSeconds(1.0 / multiplier);
    }

    // ── Return to Present ──────────────────────────────────────

    private void OnReturnToPresent()
    {
        StopPlayback();
        _suppressEvents = true;
        try
        {
            if (_slider != null)
            {
                var max = _isAnchorMode
                    ? Math.Max((_anchorEvents?.Count ?? 1) - 1, 0)
                    : _corrections.Count;
                _slider.Value = max; // set to max (fully corrected / last event)
            }
            UpdateProgressText();
        }
        finally
        {
            _suppressEvents = false;
        }
        ReturnToPresent?.Invoke(this, EventArgs.Empty);
    }

    // ── UI helpers ──────────────────────────────────────────────

    private void UpdateProgressText()
    {
        if (_txtProgress == null) return;
        var current = _slider != null ? (int)_slider.Value : 0;

        if (_isAnchorMode && _anchorEvents != null)
        {
            var total = _anchorEvents.Count;
            if (total == 0)
            {
                _txtProgress.Text = "No events";
                return;
            }
            var evt = current < _anchorEvents.Count ? _anchorEvents[current] : null;
            var label = evt?.ChangeTypeDisplay ?? "?";
            var locus = evt?.LocusId;
            var display = locus != null ? $"{label} @ {locus}" : label;
            _txtProgress.Text = $"Event {current + 1} of {total} — {display}";
        }
        else
        {
            var total = _corrections.Count;
            _txtProgress.Text = current == 0 ? "Raw OCR" : $"Correction {current} of {total}";
        }
    }
}
