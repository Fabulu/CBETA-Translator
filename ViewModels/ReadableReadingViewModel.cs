using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReadZen.App.Models;

namespace ReadZen.App.ViewModels;

/// <summary>
/// One bookmark row rendered in the reader's bookmark flyout. Immutable display wrapper
/// over the persisted <see cref="Bookmark"/> plus the two per-row commands. Navigate and
/// Remove delegate straight to the callbacks the parent supplies when the list is built,
/// so the <c>ItemsControl</c> template can bind <c>NavigateCommand</c>/<c>RemoveCommand</c>
/// directly (no parent-traversal binding).
/// </summary>
public sealed partial class ReadableBookmarkItem : ObservableObject
{
    private readonly Action<ReadableBookmarkItem> _navigate;
    private readonly Action<ReadableBookmarkItem> _remove;

    /// <summary>The underlying persisted bookmark.</summary>
    public Bookmark Model { get; }

    /// <summary>The label shown on the navigate button (already file-prefixed for cross-file rows).</summary>
    public string DisplayLabel { get; }

    /// <summary>True when this bookmark targets the currently open file.</summary>
    public bool SameFile { get; }

    /// <summary>Cross-file rows render slightly dimmed (preserves the pre-MVVM styling).</summary>
    public double LabelOpacity => SameFile ? 1.0 : 0.8;

    public ReadableBookmarkItem(
        Bookmark model,
        string displayLabel,
        bool sameFile,
        Action<ReadableBookmarkItem> navigate,
        Action<ReadableBookmarkItem> remove)
    {
        Model = model;
        DisplayLabel = displayLabel;
        SameFile = sameFile;
        _navigate = navigate;
        _remove = remove;
    }

    [RelayCommand]
    private void Navigate() => _navigate(this);

    [RelayCommand]
    private void Remove() => _remove(this);
}

/// <summary>
/// The reader's reading-surface state, extracted from <see cref="Views.ReadableTabView"/>'s
/// code-behind toward the GitTabView MVVM shape (audit ratchet R2.1). Owns the bound layout
/// mode, the bookmark collection, and the reading-progress text. The view remains the
/// authority for the side-effecting operations (re-rendering panes for a layout change,
/// resolving an lb to a caret, persisting via <see cref="Services.ReaderStateService"/>);
/// this VM only holds bindable state and surfaces user intents as events/commands the
/// code-behind adapter fulfils.
/// </summary>
public partial class ReadableReadingViewModel : ObservableObject
{
    // -------------------------
    // Layout mode
    // -------------------------

    /// <summary>
    /// The reading layout the toolbar ComboBox shows. A user change raises
    /// <see cref="LayoutModeChangeRequested"/>; the view applies it (re-render + persist)
    /// and echoes the achieved mode back through <see cref="SetLayoutModeQuietly"/>, so
    /// this property never fights the view's gated/seq-guarded apply logic.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LayoutModeIndex))]
    private ReadingLayoutMode _layoutMode = ReadingLayoutMode.Page;

    private bool _suppressLayoutRequest;

    /// <summary>Raised when the user picks a different layout in the ComboBox.</summary>
    public event EventHandler<ReadingLayoutMode>? LayoutModeChangeRequested;

    partial void OnLayoutModeChanged(ReadingLayoutMode value)
    {
        if (_suppressLayoutRequest) return;
        LayoutModeChangeRequested?.Invoke(this, value);
    }

    /// <summary>
    /// Zero-based ComboBox index mirror of <see cref="LayoutMode"/> (Page = 0,
    /// MergedFlow = 1). Two-way bound so the enum stays the single source of truth
    /// without an enum-to-int converter in XAML.
    /// </summary>
    public int LayoutModeIndex
    {
        get => (int)LayoutMode;
        set => LayoutMode = value == (int)ReadingLayoutMode.MergedFlow
            ? ReadingLayoutMode.MergedFlow
            : ReadingLayoutMode.Page;
    }

    /// <summary>Echoes the view's achieved layout mode without re-raising the request event.</summary>
    public void SetLayoutModeQuietly(ReadingLayoutMode mode)
    {
        if (LayoutMode == mode) return;
        _suppressLayoutRequest = true;
        try { LayoutMode = mode; }
        finally { _suppressLayoutRequest = false; }
    }

    // -------------------------
    // Reading progress
    // -------------------------

    /// <summary>The "Line n/total (pct%)" caption bound to the toolbar TextBlock.</summary>
    [ObservableProperty]
    private string _readingProgressText = "";

    // -------------------------
    // Bookmarks
    // -------------------------

    /// <summary>The bookmark rows bound to the flyout ItemsControl.</summary>
    public ObservableCollection<ReadableBookmarkItem> Bookmarks { get; } = new();

    /// <summary>True when at least one bookmark exists (drives the empty-state hint).</summary>
    public bool HasBookmarks => Bookmarks.Count > 0;

    /// <summary>Raised when the user clicks "+ Bookmark here"; the view captures the caret.</summary>
    public event EventHandler? AddBookmarkRequested;

    [RelayCommand]
    private void AddBookmark() => AddBookmarkRequested?.Invoke(this, EventArgs.Empty);

    /// <summary>Replaces the bookmark list wholesale (the view builds the display rows).</summary>
    public void SetBookmarks(IEnumerable<ReadableBookmarkItem> items)
    {
        Bookmarks.Clear();
        foreach (var it in items)
            Bookmarks.Add(it);
        OnPropertyChanged(nameof(HasBookmarks));
    }
}
