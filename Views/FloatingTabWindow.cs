// Views/FloatingTabWindow.cs
//
// A thin, code-only floating window that hosts a single tab's live content control
// while it is popped out (POPOUT_TABS_DESIGN §4.3/§4.4). It carries NO tour wiring,
// NO MainWindowViewModel and NO tab strip — so, unlike the reader's secondary-shell
// pop-out (a full MainWindow with isSecondaryWindow:true), it can never start the
// onboarding tour. The tour-suppression requirement for secondary windows is therefore
// satisfied by construction here.
//
// Closing the window IS the dock-back gesture: OnClosing hands control back to the
// TabPopoutController, which reclaims the live content (so it is re-parented into the
// tab, never destroyed with the window) before the close completes.

using System;
using Avalonia;
using Avalonia.Controls;

namespace ReadZen.App.Views;

public sealed class FloatingTabWindow : Window
{
    private readonly Action _onCloseDockBack;

    /// <summary>
    /// True once the window has begun closing. The controller checks this to avoid
    /// re-closing a window that is already mid-close during dock-back re-entrancy.
    /// </summary>
    public bool ClosingInProgress { get; private set; }

    public FloatingTabWindow(string title, Size defaultSize, Action onCloseDockBack)
    {
        _onCloseDockBack = onCloseDockBack ?? throw new ArgumentNullException(nameof(onCloseDockBack));

        Title = title;
        Width = defaultSize.Width;
        Height = defaultSize.Height;
        MinWidth = 480;
        MinHeight = 360;
        // Modeless with NO owner (an owned window would always cover the MainWindow),
        // so center on screen rather than on a (non-existent) owner.
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        ShowInTaskbar = true;
    }

    protected override void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);

        // First close signal wins: mark closing, then let the controller reclaim the
        // live content back into its tab slot. Guard prevents re-entrant reclaim when
        // the controller itself calls Close().
        if (!ClosingInProgress)
        {
            ClosingInProgress = true;
            _onCloseDockBack();
        }
    }
}
