using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReadZen.App.Models;

namespace ReadZen.App.ViewModels;

/// <summary>
/// One row in the reading-layout selector. Normal rows carry a real <see cref="Mode"/>;
/// header rows (<see cref="IsHeader"/> = true) are non-selectable group captions and
/// their <see cref="Mode"/> is ignored.
/// </summary>
public sealed record LayoutOption(ReadingLayoutMode Mode, string Label, bool IsHeader = false);

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
    [NotifyPropertyChangedFor(nameof(SelectedLayoutOption))]
    private ReadingLayoutMode _layoutMode = ReadingLayoutMode.MergedFlow;

    private bool _suppressLayoutRequest;

    /// <summary>Raised when the user picks a different layout in the ComboBox.</summary>
    public event EventHandler<ReadingLayoutMode>? LayoutModeChangeRequested;

    partial void OnLayoutModeChanged(ReadingLayoutMode value)
    {
        if (_suppressLayoutRequest) return;
        LayoutModeChangeRequested?.Invoke(this, value);
    }

    /// <summary>
    /// The reading-layout selector rows: two group headers ("Two-pane", "Single-column")
    /// with their member modes. Item-bound so the display order is decoupled from the
    /// wire-frozen enum values. Header rows are non-selectable (see
    /// <see cref="SelectedLayoutOption"/>).
    /// </summary>
    public IReadOnlyList<LayoutOption> LayoutModeOptions { get; } = new[]
    {
        new LayoutOption(ReadingLayoutMode.Page, "Two-pane", IsHeader: true),
        new LayoutOption(ReadingLayoutMode.Page, "Page layout"),
        new LayoutOption(ReadingLayoutMode.MergedFlow, "Merged flow"),
        new LayoutOption(ReadingLayoutMode.SyncedPanes, "Synced panes"),
        new LayoutOption(ReadingLayoutMode.AlignedLines, "Aligned lines"),
        new LayoutOption(ReadingLayoutMode.AlignedBlocks, "Aligned blocks"),
        new LayoutOption(ReadingLayoutMode.Interleaved, "Single-column", IsHeader: true),
        new LayoutOption(ReadingLayoutMode.Interleaved, "Interleaved (ZH→EN)"),
        new LayoutOption(ReadingLayoutMode.MergedStacked, "Merged, ZH then EN"),
    };

    /// <summary>
    /// The selected non-header option, mirroring <see cref="LayoutMode"/>. Setting a
    /// real option maps to <see cref="LayoutMode"/> (raising
    /// <see cref="LayoutModeChangeRequested"/> unless suppressed); setting a header row
    /// or null is ignored and the selection snaps back to the current mode.
    /// </summary>
    public LayoutOption? SelectedLayoutOption
    {
        get => LayoutModeOptions.FirstOrDefault(o => !o.IsHeader && o.Mode == LayoutMode);
        set
        {
            if (value is null || value.IsHeader)
            {
                // Header rows are not selectable — snap the ComboBox back to the real mode.
                OnPropertyChanged();
                return;
            }
            LayoutMode = value.Mode; // raises LayoutModeChangeRequested unless suppressed
            OnPropertyChanged();
        }
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
    // View mode (ZH / Both / EN)
    // -------------------------

    /// <summary>
    /// Which language pane(s) show: 0 = ZH, 1 = Both, 2 = EN. Two-way bound to the
    /// toolbar view selector; a user change raises <see cref="ViewModeChangeRequested"/>
    /// and the view collapses/expands the pane columns.
    /// </summary>
    [ObservableProperty]
    private int _viewModeIndex = 1; // default Both

    private bool _suppressViewModeRequest;

    /// <summary>Raised when the user changes the ZH/Both/EN view selector.</summary>
    public event EventHandler<ReaderViewMode>? ViewModeChangeRequested;

    partial void OnViewModeIndexChanged(int value)
    {
        if (_suppressViewModeRequest) return;
        ViewModeChangeRequested?.Invoke(this, ToViewMode(value));
    }

    private static ReaderViewMode ToViewMode(int index) => index switch
    {
        0 => ReaderViewMode.Zh,
        2 => ReaderViewMode.En,
        _ => ReaderViewMode.Both
    };

    /// <summary>Echoes the achieved view mode without re-raising the request event.</summary>
    public void SetViewModeQuietly(int index)
    {
        if (ViewModeIndex == index) return;
        _suppressViewModeRequest = true;
        try { ViewModeIndex = index; }
        finally { _suppressViewModeRequest = false; }
    }

    // -------------------------
    // Line-id gutter toggle
    // -------------------------

    /// <summary>
    /// Whether the reader shows lb (line-id) prefixes. Bound to a toolbar checkbox; a
    /// change raises <see cref="ShowLineIdsChangeRequested"/>. In Wave A this is a no-op
    /// stub for two-pane modes; Wave C consumes it for single-column projections.
    /// </summary>
    [ObservableProperty]
    private bool _showLineIds;

    /// <summary>Raised when the user toggles the line-id checkbox.</summary>
    public event EventHandler<bool>? ShowLineIdsChangeRequested;

    private bool _suppressShowLineIdsRequest;

    partial void OnShowLineIdsChanged(bool value)
    {
        if (_suppressShowLineIdsRequest) return;
        ShowLineIdsChangeRequested?.Invoke(this, value);
    }

    /// <summary>Echoes the achieved line-id state without re-raising the request event.</summary>
    public void SetShowLineIdsQuietly(bool value)
    {
        if (ShowLineIds == value) return;
        _suppressShowLineIdsRequest = true;
        try { ShowLineIds = value; }
        finally { _suppressShowLineIdsRequest = false; }
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
