using System;
using CommunityToolkit.Mvvm.ComponentModel;
using ReadZen.App.Infrastructure;
using ReadZen.App.Models;

namespace ReadZen.App.ViewModels;

/// <summary>
/// Owns the bilingual reader's cross-pane scroll-sync STATE and DECISION LOGIC, extracted
/// from <see cref="Views.ReadableTabView"/>'s code-behind per the MVVM ratchet (the
/// "Bilingual scroll sync" <c>// ====</c> section). The view keeps only the thin
/// AvaloniaEdit wiring — hooking the two panes' <c>ScrollViewer</c>s, translating a pixel
/// offset ↔ a document line, and applying the mapped offset to the follower pane. Every
/// decision ("is sync active", "which pane leads", "does the source have recent user
/// intent", "map the lead offset onto the follower") lives here so it is headlessly
/// testable — no Avalonia types cross this boundary.
/// <para>
/// Sync is engaged either by the global config toggle (<see cref="ConfigEnabled"/>, applied
/// to Page/MergedFlow) OR by the active mode when it makes scroll sync its defining feature
/// (<see cref="ReadingLayoutMode.SyncedPanes"/> — see <see cref="ModeForcesSync"/>).
/// </para>
/// </summary>
public partial class BilingualScrollSyncViewModel : ObservableObject
{
    /// <summary>
    /// The global config toggle (<c>AppConfig.EnableBilingualScrollSync</c>). Governs
    /// whether the ordinary two-pane modes (Page, MergedFlow) keep their peer aligned.
    /// Defaults to true, mirroring the pre-extraction field default.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsSyncActive))]
    private bool _configEnabled = true;

    /// <summary>
    /// The reader's achieved layout mode. <see cref="ReadingLayoutMode.SyncedPanes"/> makes
    /// always-on viewport scroll-sync its defining behavior, so it forces sync regardless of
    /// <see cref="ConfigEnabled"/>. Kept in step with the reading VM's LayoutMode by
    /// <see cref="ReadableReadingViewModel"/>.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ModeForcesSync))]
    [NotifyPropertyChangedFor(nameof(IsSyncForcedByMode))]
    [NotifyPropertyChangedFor(nameof(IsSyncActive))]
    private ReadingLayoutMode _layoutMode = ReadingLayoutMode.MergedFlow;

    /// <summary>
    /// Ping-pong guard: true while a programmatic scroll is in flight (our own peer-follow
    /// mutation, or a find-bar/bookmark/resume jump). Set by both the sync pass and the
    /// view's programmatic-scroll suppression; read by <see cref="ShouldSync"/> so our own
    /// induced <c>ScrollChanged</c> never triggers a second, opposing sync pass.
    /// </summary>
    public bool Suppressed { get; set; }

    /// <summary>
    /// Which pane led the pending sync pass: captured when a pane scroll schedules the
    /// debounce, read back when the debounce ticks. True = the original (ZH) pane led.
    /// </summary>
    public bool SourceIsOrig { get; set; }

    private DateTime _intentOrig = DateTime.MinValue;
    private DateTime _intentTran = DateTime.MinValue;

    /// <summary>
    /// User-intent window: sync (and resume capture) only fire when the source pane saw
    /// DIRECT user scrolling (wheel / pointer drag / nav keys) within this window before the
    /// <c>ScrollChanged</c>. Programmatic scrolls never stamp intent, so they never drag the
    /// peer along.
    /// </summary>
    public static readonly TimeSpan IntentWindow = TimeSpan.FromMilliseconds(600);

    /// <summary>True when the active mode makes scroll-sync its defining, always-on behavior.</summary>
    public bool ModeForcesSync => LayoutMode == ReadingLayoutMode.SyncedPanes;

    /// <summary>
    /// UI-facing signal for the reader's visible "linked scroll" affordance (a lock glyph /
    /// chip near the pane header). True exactly when the active mode makes always-on viewport
    /// scroll-sync its defining behavior (<see cref="ReadingLayoutMode.SyncedPanes"/>), so the
    /// user can SEE the mode is engaged — independent of, and overriding, the global
    /// <see cref="ConfigEnabled"/> toggle. Derived from <see cref="ModeForcesSync"/>; kept as a
    /// distinct name so the binding reads as intent ("sync forced by mode") rather than the
    /// internal gate. Change-notified via the <see cref="LayoutMode"/> observable.
    /// </summary>
    public bool IsSyncForcedByMode => ModeForcesSync;

    /// <summary>
    /// Whether scroll sync should run at all right now: the mode forces it, or the config
    /// enables it. Independent of the transient per-scroll guards (see <see cref="ShouldSync"/>).
    /// </summary>
    public bool IsSyncActive => ConfigEnabled || ModeForcesSync;

    /// <summary>Records direct-user-scroll intent for a pane (wheel / pointer / nav-key).</summary>
    public void StampIntent(bool sourceIsOrig, DateTime nowUtc)
    {
        if (sourceIsOrig) _intentOrig = nowUtc;
        else _intentTran = nowUtc;
    }

    /// <summary>True when the given pane saw direct user scrolling within <see cref="IntentWindow"/>.</summary>
    public bool HasRecentIntent(bool sourceIsOrig, DateTime nowUtc)
    {
        var intent = sourceIsOrig ? _intentOrig : _intentTran;
        return nowUtc - intent <= IntentWindow;
    }

    /// <summary>
    /// The full gate for scheduling a sync pass off a pane's <c>ScrollChanged</c>: sync must
    /// be active, the single-grid surface must not be up (scroll-sync is moot there), no
    /// programmatic scroll may be in flight, and the source pane must have recent user intent.
    /// </summary>
    public bool ShouldSync(bool sourceIsOrig, bool gridSurfaceActive, DateTime nowUtc)
        => IsSyncActive
           && !gridSurfaceActive
           && !Suppressed
           && HasRecentIntent(sourceIsOrig, nowUtc);

    /// <summary>
    /// Maps the lead pane's top-visible character offset onto the follower pane's offset by
    /// shared line id (delegates to the pure <see cref="BilingualScrollMapper"/>). Picks the
    /// source/target documents from <paramref name="sourceIsOrig"/> so the caller never has
    /// to orient them. Returns null when no lead segment at/before the offset has a
    /// counterpart in the follower (the caller then leaves the peer where it is).
    /// </summary>
    public int? MapLeadToFollow(bool sourceIsOrig, RenderedDocument orig, RenderedDocument tran, int srcOffset)
    {
        var srcDoc = sourceIsOrig ? orig : tran;
        var dstDoc = sourceIsOrig ? tran : orig;
        return BilingualScrollMapper.MapOffset(srcDoc, dstDoc, srcOffset);
    }
}
