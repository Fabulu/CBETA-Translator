// Infrastructure/ReaderLayoutStrategy.cs
//
// Pure mapping from a requested reader ReadingLayoutMode to the render surface it
// produces. Extracted verbatim from ReadableTabView.axaml.cs (MVVM renovation P8)
// so the strategy mapping can be unit tested and reused off the UI thread.
//
// Like RowGridBuilder and ReaderLbGeometry, this class is deliberately PURE — no
// Avalonia, no state, no I/O — it operates only on the ReadingLayoutMode value.
// Callers: the reading-layout apply path, the grid-sync fast path, and the
// view-mode column-collapse path.

using ReadZen.App.Models;

namespace ReadZen.App.Infrastructure;

/// <summary>The render surfaces the reader can produce.</summary>
public enum RenderStrategy
{
    /// <summary>Per-lb page layout (unsuppressed line breaks).</summary>
    PageTwoPane,
    /// <summary>Merged two-pane surface (suppressed non-leading line breaks).</summary>
    MergedTwoPane,
    /// <summary>
    /// Per-line two-pane surface with always-on viewport scroll-sync by shared line id
    /// (the SPA "flow" mode). Same UNSUPPRESSED per-line density as <see cref="PageTwoPane"/>
    /// — NOT paragraph-healed like <see cref="MergedTwoPane"/>, NOT row-height-locked like
    /// the AlignedLines row grid — but the sync is the mode's defining feature (engaged via
    /// <see cref="ReadZen.App.ViewModels.BilingualScrollSyncViewModel.ModeForcesSync"/>,
    /// independent of the config toggle).
    /// </summary>
    SyncedTwoPane,
    /// <summary>Row-grid reading surface (virtualized ListBox of RowVm rows). Wave C.</summary>
    RowGrid
}

/// <summary>
/// Pure mapping helpers from <see cref="ReadingLayoutMode"/> to render strategy and
/// two-column-grid classification. Stateless and deterministic; safe to call off the
/// UI thread.
/// </summary>
public static class ReaderLayoutStrategy
{
    /// <summary>
    /// Maps a requested <see cref="ReadingLayoutMode"/> to its render strategy. Page → page;
    /// MergedFlow → paragraph-healed merged two-pane; SyncedPanes → per-line synced two-pane
    /// (its own strategy — NOT aliased to MergedFlow); AlignedLines/AlignedBlocks/Interleaved/
    /// MergedStacked → row grid.
    /// </summary>
    public static RenderStrategy For(ReadingLayoutMode mode) => mode switch
    {
        ReadingLayoutMode.Page => RenderStrategy.PageTwoPane,
        ReadingLayoutMode.AlignedLines => RenderStrategy.RowGrid,       // Wave C1 → row grid
        ReadingLayoutMode.AlignedBlocks => RenderStrategy.RowGrid,      // Wave C → two-column block-aligned row grid
        ReadingLayoutMode.MergedFlow => RenderStrategy.MergedTwoPane,
        ReadingLayoutMode.SyncedPanes => RenderStrategy.SyncedTwoPane,  // per-line, viewport scroll-synced by shared line id
        ReadingLayoutMode.Interleaved => RenderStrategy.RowGrid,        // Wave C → single-column row grid
        ReadingLayoutMode.MergedStacked => RenderStrategy.RowGrid,      // Wave C → single-column merged-stacked grid
        _ => RenderStrategy.PageTwoPane
    };

    /// <summary>True for the TWO-COLUMN grid modes whose ZH|EN row surface honors the ZH/Both/EN
    /// view filter by collapsing a column. Single-column grid modes (Interleaved/MergedStacked)
    /// suppress the toggle (SPA passage.js:499) and are deliberately excluded.</summary>
    public static bool IsTwoColumnGridMode(ReadingLayoutMode mode)
        => mode is ReadingLayoutMode.AlignedLines or ReadingLayoutMode.AlignedBlocks;
}
