// Infrastructure/TabPopoutController.cs
//
// The tab pop-out reparenting primitive (POPOUT_TABS_DESIGN §4).
//
// A reusable, per-MainWindow controller that DETACHES a tab's live content control
// out of its Carousel slot into a thin floating Window, leaving a placeholder in the
// tab, and DOCKS it back when the float closes. The SAME control instance moves — its
// ViewModel, scroll, selection and all host wiring survive, because Avalonia already
// detaches non-selected Carousel pages from the visual tree today (that IS tab
// switching); pop-out is the same lifecycle event to a different parent.
//
// Design invariants honoured here:
//   * Carousel.Items is NEVER mutated. Each reparent-eligible tab is wrapped in a
//     permanent named-slot Decorator in MainWindow.axaml; pop-out only swaps
//     slot.Child. All the shell's hard-coded tab-index assumptions stay intact.
//   * One-parent rule: a control must be fully detached from its current parent
//     before it is attached elsewhere. slot.Child is replaced with the placeholder
//     (which detaches the view) BEFORE the view becomes the float's Content, and the
//     float's Content is cleared BEFORE the view returns to the slot.
//   * Re-entrancy: closing the float IS the dock-back gesture. The window's OnClosing
//     calls back into DockBack; a _docking guard + the window's own ClosingInProgress
//     flag prevent the reclaim from running twice or re-closing mid-close.
//
// Per-MainWindow (NOT a DI singleton) so secondary shells get independent controllers.

using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using ReadZen.App.Views;

namespace ReadZen.App.Infrastructure;

/// <summary>
/// Describes one tab that can be popped out via <see cref="TabPopoutController"/>.
/// </summary>
public sealed class TabPopoutDescriptor
{
    /// <summary>Carousel index of the tab (stable positional index).</summary>
    public required int TabIndex { get; init; }

    /// <summary>Human-facing title shown on the float and its placeholder.</summary>
    public required string Title { get; init; }

    /// <summary>The permanent named-slot Decorator hosting the tab's live content.</summary>
    public required Decorator Slot { get; init; }

    /// <summary>
    /// Optional per-tab veto (e.g. block while a specific in-flight operation runs).
    /// A global veto (tour active / empty state) is applied separately by the controller.
    /// </summary>
    public Func<bool>? CanPopOut { get; init; }

    /// <summary>Runs immediately before the control is detached (either direction).</summary>
    public Action? BeforeDetach { get; init; }

    /// <summary>Runs immediately after the control is re-attached (either direction).</summary>
    public Action? AfterAttach { get; init; }

    /// <summary>Initial size of the float.</summary>
    public Size DefaultSize { get; init; } = new(1100, 760);
}

/// <summary>
/// Reparents registered tabs between their Carousel slot and a floating window.
/// </summary>
public sealed class TabPopoutController
{
    private readonly Window _owner;
    private readonly Func<bool>? _globalCanPopOut;

    private readonly Dictionary<int, TabPopoutDescriptor> _descriptors = new();
    private readonly Dictionary<int, FloatingTabWindow> _floats = new();
    private readonly Dictionary<int, Control> _detached = new();
    private readonly HashSet<int> _docking = new();

    /// <summary>Fires (tabIndex, isPoppedOut) whenever a tab detaches or docks back.</summary>
    public event Action<int, bool>? PoppedChanged;

    /// <param name="owner">The MainWindow that owns these tabs (used for theme variant).</param>
    /// <param name="globalCanPopOut">
    /// Optional shell-wide gate evaluated at pop-out time (e.g. false while the tour is
    /// running or the empty-state overlay is up). Evaluated lazily on each PopOut.
    /// </param>
    public TabPopoutController(Window owner, Func<bool>? globalCanPopOut = null)
    {
        _owner = owner ?? throw new ArgumentNullException(nameof(owner));
        _globalCanPopOut = globalCanPopOut;

        // Keep floats in lockstep with the app-level theme (§4.7): the owner's
        // ActualThemeVariant changes when ApplyTheme flips light/dark, so mirror it
        // onto every open float. Owner and controller share a lifetime — no leak.
        _owner.ActualThemeVariantChanged += (_, _) =>
        {
            foreach (var win in _floats.Values)
                win.RequestedThemeVariant = _owner.ActualThemeVariant;
        };
    }

    /// <summary>Registers (or replaces) a poppable tab. Safe to call once at wire-up.</summary>
    public void Register(TabPopoutDescriptor descriptor)
    {
        if (descriptor == null) throw new ArgumentNullException(nameof(descriptor));
        _descriptors[descriptor.TabIndex] = descriptor;
    }

    /// <summary>True while the tab's content lives in a floating window.</summary>
    public bool IsPoppedOut(int tabIndex) => _floats.ContainsKey(tabIndex);

    /// <summary>
    /// Detaches the tab's live control into a new floating window, leaving a placeholder
    /// in the tab. If already popped out, just re-activates the existing float. No-op for
    /// unregistered tabs or when a veto (global or per-tab) is active.
    /// </summary>
    public void PopOut(int tabIndex)
    {
        if (!_descriptors.TryGetValue(tabIndex, out var d)) return;

        if (_floats.ContainsKey(tabIndex))
        {
            Activate(tabIndex);
            return;
        }

        if (_globalCanPopOut != null && !_globalCanPopOut()) return;
        if (d.CanPopOut != null && !d.CanPopOut()) return;

        if (d.Slot.Child is not Control view) return; // nothing live to detach

        d.BeforeDetach?.Invoke();

        // One-parent rule: replacing slot.Child detaches `view` from the Decorator first.
        d.Slot.Child = new PoppedOutPlaceholder(
            d.Title,
            onBringToFront: () => Activate(tabIndex),
            onDockBack: () => DockBack(tabIndex));

        var win = new FloatingTabWindow(d.Title, d.DefaultSize, () => DockBack(tabIndex))
        {
            RequestedThemeVariant = _owner.ActualThemeVariant,
            Content = view,
        };

        _floats[tabIndex] = win;
        _detached[tabIndex] = view;

        // Modeless, no owner (an owned window would always cover the MainWindow).
        win.Show();

        d.AfterAttach?.Invoke();
        PoppedChanged?.Invoke(tabIndex, true);
    }

    /// <summary>
    /// Returns the tab's control to its slot and closes the float. Reclaims content on
    /// window close as well as on the placeholder's "Dock back" button; the _docking
    /// guard makes both paths converge exactly once.
    /// </summary>
    public void DockBack(int tabIndex)
    {
        if (_docking.Contains(tabIndex)) return;
        if (!_floats.TryGetValue(tabIndex, out var win)) return;
        if (!_descriptors.TryGetValue(tabIndex, out var d)) return;

        _docking.Add(tabIndex);
        try
        {
            var view = _detached.TryGetValue(tabIndex, out var v) ? v : win.Content as Control;

            d.BeforeDetach?.Invoke();

            // One-parent rule in reverse: clear the float first, then re-home the view.
            win.Content = null;
            if (view != null)
                d.Slot.Child = view;

            _floats.Remove(tabIndex);
            _detached.Remove(tabIndex);

            // Close the now-empty float. When DockBack was itself triggered from the
            // window's OnClosing, ClosingInProgress is already set, so we do not re-close.
            if (!win.ClosingInProgress)
            {
                try { win.Close(); } catch { /* window already gone */ }
            }

            d.AfterAttach?.Invoke();
            PoppedChanged?.Invoke(tabIndex, false);
        }
        finally
        {
            _docking.Remove(tabIndex);
        }
    }

    /// <summary>Brings an open float to the front. No-op if the tab is docked.</summary>
    public void Activate(int tabIndex)
    {
        if (!_floats.TryGetValue(tabIndex, out var win)) return;
        try
        {
            win.Show();
            win.Activate();
        }
        catch { /* window closing */ }
    }

    /// <summary>
    /// Docks every open float back into its tab. Called on tour start (spotlight
    /// coordinates are MainWindow-relative) and on app shutdown (so persistence /
    /// dirty-close see the full live tree before the window goes away).
    /// </summary>
    public void DockAllBack()
    {
        foreach (var idx in _floats.Keys.ToArray())
            DockBack(idx);
    }
}
