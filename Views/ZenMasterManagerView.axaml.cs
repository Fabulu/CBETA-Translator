// Views/ZenMasterManagerView.axaml.cs
//
// The reusable Zen Master explorer surface: the Browse / Corpus / Lineage-Web
// TabControl plus the interactive lineage chart and its detail side-panel, all
// driven by ZenMasterManagerWindowViewModel. This UserControl is the single
// implementation shared by two hosts:
//   - the embedded Lineage tab in MainWindow (the DEFAULT path), and
//   - the floating ZenMasterManagerWindow (a thin host, kept for deep-link /
//     future pop-out).
//
// It is fully self-contained: services come from App.Services (DI) and the
// ctor params (repoRoot / parentRoot / baseFilePath) it already took as a
// window. Activation is LAZY and idempotent (EnsureActivatedAsync) so the host
// only pays the catalog-load + graph-build cost when the tab is first shown,
// not at app startup.
//
// PERF FIXES PRESERVED FROM THE WINDOW (do not regress):
//   - default-to-graph-tab (TabMain SelectedIndex=2 in XAML; activation only
//     switches to Browse when an explicit landing is pending)
//   - lazy corpus-index load (EnsureCorpusIndexLoadedAsync when the Corpus tab
//     is first shown, guarded on the TabControl being the event source)
//   - lazy CJK text shaping lives in LineageChartControl (untouched here)
//   - graph build no longer waits behind the corpus index

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ReadZen.App.Messages;
using ReadZen.App.Models;
using ReadZen.App.Services;
using ReadZen.App.ViewModels;
using CommunityToolkit.Mvvm.Messaging;
using Microsoft.Extensions.DependencyInjection;
using ReadZen.App.Infrastructure;

namespace ReadZen.App.Views;

public partial class ZenMasterManagerView : UserControl
{
    private readonly string? _repoRoot;
    private readonly string? _parentRoot;
    private readonly string _baseFilePath;
    private MasterDatesEditorDialog? _editorWindow;
    private string? _pendingLandingName;
    private string? _pendingLandingUser;
    private bool _loaded;
    private Task? _activation;
    private readonly DateTime _ctorTime = DateTime.UtcNow;

    // Tab indices of the inner TabControl in ZenMasterManagerView.axaml.
    private const int TAB_BROWSE = 0;
    private const int TAB_CORPUS = 1;
    private const int TAB_LINEAGE = 2;

    /// <summary>Fired when the user double-clicks a corpus text to navigate to it in the reader.</summary>
    public event EventHandler<NavigationRequest>? CorpusNavigationRequested;

    public ZenMasterManagerWindowViewModel ViewModel { get; }

    public ZenMasterManagerView()
        : this(null, null, Path.Combine(AppContext.BaseDirectory, "Assets", "Data", "master-dates.json"))
    {
    }

    public ZenMasterManagerView(string? repoRoot, string? parentRoot = null, string? baseFilePath = null)
    {
        _repoRoot = repoRoot;
        _parentRoot = parentRoot ?? repoRoot;
        _baseFilePath = string.IsNullOrWhiteSpace(baseFilePath)
            ? Path.Combine(AppContext.BaseDirectory, "Assets", "Data", "master-dates.json")
            : baseFilePath;

        InitializeComponent();

        var service = App.Services.GetRequiredService<ZenMasterManagerService>();
        ViewModel = new ZenMasterManagerWindowViewModel(service, repoRoot, _parentRoot, _baseFilePath);
        DataContext = ViewModel;

        WireEvents();

        // Handle link clicks (TextBlocks with Name="LinkItem" and Tag=URL)
        AddHandler(InputElement.PointerPressedEvent, (_, e) =>
        {
            if (e.Source is TextBlock tb && tb.Name == "LinkItem" && tb.Tag is string url
                && !string.IsNullOrWhiteSpace(url))
            {
                OpenExternalUrl(url);
                e.Handled = true;
            }
        }, RoutingStrategies.Tunnel);

        // In embedded (main-tab) mode there is no window to "Close"; only the
        // dedicated floating ZenMasterManagerWindow shows that button.
        AttachedToVisualTree += (_, _) =>
        {
            var btnClose = this.FindControl<Button>("BtnClose");
            if (btnClose != null)
                btnClose.IsVisible = HostWindow is ZenMasterManagerWindow;
        };
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    /// <summary>The window that currently hosts this control (MainWindow when embedded,
    /// ZenMasterManagerWindow when floating). Null until attached to the visual tree.</summary>
    private Window? HostWindow => TopLevel.GetTopLevel(this) as Window;

    /// <summary>
    /// Lazily load the catalog and build the lineage graph the first time the control
    /// is actually shown. Idempotent and re-entrant safe: the shared activation Task
    /// is created once, so overlapping callers (tab-selection handler + deep-link
    /// routing) await the same load rather than racing. Replaces the window's Opened
    /// handler.
    /// </summary>
    public Task EnsureActivatedAsync() => _activation ??= ActivateCoreAsync();

    private async Task ActivateCoreAsync()
    {
        var sw = Stopwatch.StartNew();
        ZenMasterManagerWindowViewModel.LogLineageTiming(
            "View.ctor->shown", (long)(DateTime.UtcNow - _ctorTime).TotalMilliseconds);

        // Pick the landing tab BEFORE the (potentially slow) catalog load so the
        // control paints on the RIGHT tab from frame one. The graph is the entry
        // point; an explicit landing (a specific master clicked elsewhere) opens on
        // the Browse profile so that master is front-and-centre. The XAML default is
        // already TAB_LINEAGE; this keeps it correct when a landing is pending.
        SelectTab(string.IsNullOrWhiteSpace(_pendingLandingName) ? TAB_LINEAGE : TAB_BROWSE);

        if (!_loaded)
        {
            var swLoad = Stopwatch.StartNew();
            await ViewModel.LoadAsync();
            ZenMasterManagerWindowViewModel.LogLineageTiming("View.LoadAsync", swLoad.ElapsedMilliseconds);
            _loaded = true;

            // Build lineage graph after catalog loads. This no longer waits behind
            // the corpus index (moved off the open path — see the VM), so the graph
            // is built as soon as the roster is in hand.
            var swGraph = Stopwatch.StartNew();
            BuildLineageGraph();
            ZenMasterManagerWindowViewModel.LogLineageTiming("View.BuildLineageGraph", swGraph.ElapsedMilliseconds);
        }

        ApplyPendingLanding();
        ZenMasterManagerWindowViewModel.LogLineageTiming("View.Activate.total", sw.ElapsedMilliseconds);
    }

    private void WireEvents()
    {
        var btnClose = this.FindControl<Button>("BtnClose");
        if (btnClose != null)
            btnClose.Click += (_, _) => (HostWindow as ZenMasterManagerWindow)?.Close();

        var btnCopyLink = this.FindControl<Button>("BtnCopyLink");
        if (btnCopyLink != null)
            btnCopyLink.Click += (_, _) => AsyncGuard.Run(async () => await CopyLinkAsync(), "ZenMasterManagerView.btnCopyLink.Click");

        var btnEditDates = this.FindControl<Button>("BtnEditDates");
        if (btnEditDates != null)
            btnEditDates.Click += (_, _) => AsyncGuard.Run(async () => await OpenEditorAsync(), "ZenMasterManagerView.btnEditDates.Click");

        // Lineage Web tab wiring happens in WireLineage(), once the chart VM is
        // constructed in BuildLineageGraph() (the VM is not in DI — see D3).

        // Teacher link click
        var btnJumpTeacher = this.FindControl<Button>("BtnJumpTeacher");
        if (btnJumpTeacher != null)
            btnJumpTeacher.Click += (_, _) =>
            {
                var teacher = ViewModel.SelectedMaster?.Teacher;
                if (!string.IsNullOrWhiteSpace(teacher))
                    ViewModel.ApplyLanding(teacher);
            };

        // Student link click (delegated via ItemsControl)
        var studentsList = this.FindControl<ItemsControl>("StudentLinksList");
        if (studentsList != null)
        {
            studentsList.AddHandler(Button.ClickEvent, (object? _, RoutedEventArgs e) =>
            {
                if (e.Source is Button btn && btn.Classes.Contains("student-link") && btn.Content is string name)
                {
                    if (!string.IsNullOrWhiteSpace(name))
                        ViewModel.ApplyLanding(name);
                }
            });
        }

        // Master list context menu
        var mnuCopyLink = this.FindControl<MenuItem>("MnuCopyMasterLink");
        if (mnuCopyLink != null)
            mnuCopyLink.Click += (_, _) => AsyncGuard.Run(async () => await CopyMasterLinkAsync(isReddit: false), "ZenMasterManagerView.mnuCopyLink.Click");

        var mnuCopyReddit = this.FindControl<MenuItem>("MnuCopyMasterRedditLink");
        if (mnuCopyReddit != null)
            mnuCopyReddit.Click += (_, _) => AsyncGuard.Run(async () => await CopyMasterLinkAsync(isReddit: true), "ZenMasterManagerView.mnuCopyReddit.Click");

        var mnuCiteMaster = this.FindControl<MenuItem>("MnuCiteMaster");
        if (mnuCiteMaster != null)
            mnuCiteMaster.Click += (_, _) => AsyncGuard.Run(async () => await CopyMasterCitationAsync(), "ZenMasterManagerView.mnuCiteMaster.Click");

        // Corpus search tab
        var btnBuildIndex = this.FindControl<Button>("BtnBuildCorpusIndex");
        if (btnBuildIndex != null)
            btnBuildIndex.Click += (_, _) => AsyncGuard.Run(async () => await ViewModel.BuildCorpusIndexAsync(), "ZenMasterManagerView.btnBuildIndex.Click");

        var btnCancelScan = this.FindControl<Button>("BtnCancelCorpusScan");
        if (btnCancelScan != null)
            btnCancelScan.Click += (_, _) => ViewModel.CancelCorpusScan();

        // Double-click on corpus result -> navigate to text
        var lstPrimary = this.FindControl<ListBox>("LstCorpusPrimary");
        var lstSecondary = this.FindControl<ListBox>("LstCorpusSecondary");

        WireCorpusListDoubleClick(lstPrimary);
        WireCorpusListDoubleClick(lstSecondary);

        // Lazy corpus-index load: the big index is only needed by the Corpus tab, so
        // load it the first time that tab is shown rather than on activation (where it
        // used to stall the graph). Guard on the event Source so an inner ListBox's own
        // SelectionChanged bubbling up to the TabControl does not trigger a spurious
        // load; EnsureCorpusIndexLoadedAsync is idempotent regardless.
        var tabs = this.FindControl<TabControl>("TabMain");
        if (tabs != null)
            tabs.SelectionChanged += (_, e) =>
            {
                if (ReferenceEquals(e.Source, tabs) && tabs.SelectedIndex == TAB_CORPUS)
                    AsyncGuard.Run(async () => await ViewModel.EnsureCorpusIndexLoadedAsync(),
                        "ZenMasterManagerView.corpusTabShown");
            };
    }

    private void WireCorpusListDoubleClick(ListBox? listBox)
    {
        if (listBox == null) return;

        listBox.DoubleTapped += (_, e) =>
        {
            if (listBox.SelectedItem is MasterTextAppearance appearance)
            {
                CorpusNavigationRequested?.Invoke(this, new NavigationRequest
                {
                    RelPath = appearance.RelPath,
                    Side = SearchSide.Original,
                    MatchText = appearance.MatchedName,
                });
            }
        };
    }

    private LineageChartViewModel? _chartVm;
    private bool _syncingSelection;

    public void ApplyLanding(string? name, string? user)
    {
        _pendingLandingName = name;
        _pendingLandingUser = user;

        // REUSE PATH: when the control is re-shown already loaded (e.g. the Lineage tab
        // is re-selected, or a second deep-link arrives), EnsureActivatedAsync is a
        // no-op, so select the correct tab here too — a no-landing re-open jumps to the
        // graph (the entry point), an explicit landing to the Browse profile. On first
        // construction _loaded is false and ActivateCoreAsync owns the tab choice, so we
        // leave it alone here to avoid double-selecting.
        if (_loaded)
            SelectTab(string.IsNullOrWhiteSpace(name) ? TAB_LINEAGE : TAB_BROWSE);

        ApplyPendingLanding();
    }

    private void ApplyPendingLanding()
    {
        if (!_loaded || string.IsNullOrWhiteSpace(_pendingLandingName))
            return;

        ViewModel.ApplyLanding(_pendingLandingName, _pendingLandingUser);
        _pendingLandingName = null;
        _pendingLandingUser = null;
    }

    private void BuildLineageGraph()
    {
        // The NEW tidy-forest chart (plan PR-L6). Its VM is not registered in DI —
        // construct it here, mirroring how the old code new'd up its VM. It loads the
        // rich 609-record roster (ILineageRosterService, resolved from DI) and runs
        // the pure build + layout in its constructor.
        var chart = this.FindControl<LineageChartControl>("LineageGraph");
        if (chart == null) return;

        _chartVm = new LineageChartViewModel(App.Services.GetRequiredService<ILineageRosterService>());
        if (!_chartVm.IsLoaded) return;

        chart.SetViewModel(_chartVm);

        // The detail side-panel binds directly to the chart VM (its own x:DataType).
        var panel = this.FindControl<LineageDetailPanel>("LineagePanel");
        if (panel != null) panel.DataContext = _chartVm;

        var txtInfo = this.FindControl<TextBlock>("TxtLineageInfo");
        if (txtInfo != null)
            txtInfo.Text = $"{_chartVm.Nodes.Count} nodes, {_chartVm.Edges.Count} links";

        WireLineage(chart);

        // Collapsed-until-clicked: nothing is selected on load, so fold the detail panel
        // now (saving its designed 340px width) — it re-expands on the first node click and
        // folds again when selection clears. This gives the graph the full width by default.
        SetLineagePanelCollapsed(true);
    }

    /// <summary>Wire the new chart control + VM: search, zoom, list-sync, focus, activation.</summary>
    private void WireLineage(LineageChartControl chart)
    {
        var vm = _chartVm;
        if (vm == null) return;

        // ── panel-driven interactions (kept off the headless VM) ──
        vm.OpenUrlHandler = OpenExternalUrl;
        vm.NavigateCorpusHandler = NavigateCorpusPath;
        vm.OpenProfileHandler = node => { SyncListToNode(node); SelectTab(0); };
        vm.OpenCorpusSearchHandler = node => { SyncListToNode(node); SelectTab(1); };
        vm.NodeFocusRequested += node => { chart.CenterOn(node); SyncListToNode(node); };

        // ── search box → highlight; Enter / "Go to" → centre on the first hit ──
        var txtSearch = this.FindControl<TextBox>("TxtLineageSearch");
        var btnCenter = this.FindControl<Button>("BtnLineageCenter");
        var zoomSlider = this.FindControl<Slider>("SliderLineageZoom");

        void GoToFirstMatch()
        {
            var match = vm.Nodes.FirstOrDefault(n => vm.SearchHitIds.Contains(n.Id));
            if (match == null) return;
            // FocusNode raises NodeFocusRequested → CenterOn, which now FLIES to the
            // node (SPA flyTo parity) and raises the zoom to a readable floor itself.
            // A SetZoom here would cancel that flight mid-air and de-centre the node.
            vm.FocusNode(match);
        }

        if (txtSearch != null)
        {
            txtSearch.TextChanged += (_, _) =>
            {
                vm.SearchText = txtSearch.Text ?? "";
                chart.InvalidateVisual();
            };
            txtSearch.KeyDown += (_, e) =>
            {
                if (e.Key == Key.Enter) { GoToFirstMatch(); e.Handled = true; }
            };
        }
        if (btnCenter != null)
            btnCenter.Click += (_, _) => GoToFirstMatch();

        // ── zoom slider ↔ control (guarded against feedback) ──
        if (zoomSlider != null)
        {
            zoomSlider.PropertyChanged += (_, args) =>
            {
                if (args.Property.Name == "Value" && !_syncingZoom)
                    chart.SetZoom(zoomSlider.Value / 100.0);
            };
            chart.ZoomChanged += z =>
            {
                _syncingZoom = true;
                try { zoomSlider.Value = Math.Clamp(z * 100.0, zoomSlider.Minimum, zoomSlider.Maximum); }
                finally { _syncingZoom = false; }
            };
        }

        // ── double-click a node → open it in the List tab (parity with the old control) ──
        chart.NodeActivated += node =>
        {
            SyncListToNode(node);
            SelectTab(0);
        };

        // ── chart selection → List tab (user clicked a node) ──
        vm.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(LineageChartViewModel.SelectedNode))
            {
                chart.InvalidateVisual();
                if (!_syncingSelection) SyncListToNode(vm.SelectedNode);
                // The detail panel is collapsed-until-clicked in ALL modes (embedded and
                // fullscreen): it appears only when a node is selected and folds away when
                // selection clears, so the graph keeps the full width until you ask for detail.
                SetLineagePanelCollapsed(vm.SelectedNode == null);
            }
        };

        // ── fullscreen toggle (SPA parity: chart fills the screen, Esc exits) ──
        var btnFull = this.FindControl<Button>("BtnLineageFullscreen");
        if (btnFull != null)
            btnFull.Click += (_, _) => SetLineageFullscreen(!_lineageFullscreen, chart);
        this.KeyDown += (_, e) =>
        {
            if (_lineageFullscreen && e.Key == Key.Escape)
            {
                SetLineageFullscreen(false, chart);
                e.Handled = true;
            }
        };

        // ── List tab selection → chart (user picked a master in the list) ──
        ViewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(ViewModel.SelectedMaster) && !_syncingSelection)
                SyncChartToMaster(ViewModel.SelectedMaster);
        };
    }

    private bool _syncingZoom;
    private bool _lineageFullscreen;
    private WindowState _prevWindowState = WindowState.Normal;
    private GridLength _savedLineageSplitterW = new GridLength(8);
    private GridLength _savedLineagePanelW = new GridLength(340);

    /// <summary>Enter/exit fullscreen for the Lineage Web chart (SPA parity: the chart fills the
    /// screen, Esc exits, the search/zoom toolbar stays on top; the detail panel collapses out of
    /// the way unless a master is selected). Operates on the host window — MainWindow when the
    /// control is embedded, the floating ZenMasterManagerWindow when popped out.
    ///
    /// When embedded in MainWindow the FullScreen window state alone leaves the app chrome (top
    /// bar + nested TabStrip + status bar) visible around the tab content, so a
    /// <see cref="LineageFullscreenRequestedMessage"/> is broadcast for MainWindow to hide/restore
    /// that chrome. The floating ZenMasterManagerWindow is chrome-less, so it keeps the original
    /// window-state-only behavior (and the message is NOT sent, so a still-open MainWindow's chrome
    /// is left untouched).</summary>
    private void SetLineageFullscreen(bool on, LineageChartControl chart)
    {
        if (on == _lineageFullscreen) return;
        var host = HostWindow;
        if (host == null) return;

        // Only the embedded (MainWindow) host has app chrome to hide; gate the message on it so a
        // pop-out ZenMasterManagerWindow never toggles a background MainWindow's chrome.
        bool embedded = host is MainWindow;

        if (on)
        {
            SelectTab(2);                       // ensure the Lineage tab is the visible one
            _prevWindowState = host.WindowState;
            host.WindowState = WindowState.FullScreen;
            _lineageFullscreen = true;
            if (embedded)
                WeakReferenceMessenger.Default.Send(new LineageFullscreenRequestedMessage(true));
            SetLineagePanelCollapsed(_chartVm?.SelectedNode == null);
        }
        else
        {
            _lineageFullscreen = false;
            // Collapsed-until-clicked holds outside fullscreen too, so restore the panel
            // only if a node is actually selected; otherwise it stays folded.
            SetLineagePanelCollapsed(_chartVm?.SelectedNode == null);
            if (embedded)
                WeakReferenceMessenger.Default.Send(new LineageFullscreenRequestedMessage(false));
            host.WindowState = _prevWindowState;
        }

        var btnFull = this.FindControl<Button>("BtnLineageFullscreen");
        if (btnFull != null) btnFull.Content = on ? "Exit Fullscreen" : "Fullscreen";

        chart.FitToView();                      // re-fit once the new surface size settles
    }

    /// <summary>Collapse or restore the lineage detail side-panel (used in fullscreen to give the
    /// chart the whole surface), preserving any user-resized panel width.</summary>
    private void SetLineagePanelCollapsed(bool collapsed)
    {
        var grid = this.FindControl<Grid>("LineageBodyGrid");
        if (grid == null || grid.ColumnDefinitions.Count < 3) return;
        var splitter = this.FindControl<GridSplitter>("LineageSplitter");
        var panel = this.FindControl<LineageDetailPanel>("LineagePanel");

        if (collapsed)
        {
            if (grid.ColumnDefinitions[2].Width.Value > 0)   // remember a real (un-collapsed) width
            {
                _savedLineageSplitterW = grid.ColumnDefinitions[1].Width;
                _savedLineagePanelW = grid.ColumnDefinitions[2].Width;
            }
            grid.ColumnDefinitions[1].Width = new GridLength(0);
            grid.ColumnDefinitions[2].Width = new GridLength(0);
            if (splitter != null) splitter.IsVisible = false;
            if (panel != null) panel.IsVisible = false;
        }
        else
        {
            grid.ColumnDefinitions[1].Width = _savedLineageSplitterW;
            grid.ColumnDefinitions[2].Width = _savedLineagePanelW;
            if (splitter != null) splitter.IsVisible = true;
            if (panel != null) panel.IsVisible = true;
        }
    }

    private void SelectTab(int index)
    {
        var tabs = this.FindControl<TabControl>("TabMain");
        if (tabs != null) tabs.SelectedIndex = index;
    }

    private void OpenExternalUrl(string url)
    {
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
        catch { /* a bad link must never crash the view */ }
    }

    private void NavigateCorpusPath(string teiPath, string? lb)
    {
        if (string.IsNullOrWhiteSpace(teiPath)) return;
        CorpusNavigationRequested?.Invoke(this, new NavigationRequest
        {
            RelPath = teiPath,
            Side = SearchSide.Original,
            // Carry the stele line anchor (the SPA's ?pos=<lb>) so the reader lands on
            // the quoted line via segment-key lookup, not the top of the document.
            FromLb = string.IsNullOrWhiteSpace(lb) ? null : lb,
        });
    }

    /// <summary>Mirror a chart node onto the List-tab selection (matched by name/alias).</summary>
    private void SyncListToNode(LineageNode? node)
    {
        if (node == null || node.IsSource || _syncingSelection) return;
        var record = FindListRecord(node);
        if (record == null || ReferenceEquals(record, ViewModel.SelectedMaster)) return;
        _syncingSelection = true;
        try { ViewModel.SelectedMaster = record; }
        finally { _syncingSelection = false; }
    }

    /// <summary>Mirror the List-tab selection onto the chart (matched by name/alias).</summary>
    private void SyncChartToMaster(ZenMasterRecord? master)
    {
        if (master == null || _chartVm == null || _syncingSelection) return;
        var node = FindChartNode(master);
        if (node == null || ReferenceEquals(node, _chartVm.SelectedNode)) return;
        _syncingSelection = true;
        try
        {
            _chartVm.FocusNode(node);   // selects + centres via NodeFocusRequested
        }
        finally { _syncingSelection = false; }
    }

    private ZenMasterRecord? FindListRecord(LineageNode node)
    {
        foreach (var name in node.Names)
        {
            var hit = ViewModel.Masters.FirstOrDefault(m =>
                string.Equals(m.CanonicalName, name, StringComparison.OrdinalIgnoreCase) ||
                m.Aliases.Any(a => string.Equals(a, name, StringComparison.OrdinalIgnoreCase)));
            if (hit != null) return hit;
        }
        return null;
    }

    private LineageNode? FindChartNode(ZenMasterRecord master)
    {
        if (_chartVm == null) return null;
        // Case-INSENSITIVE, to match FindListRecord's OrdinalIgnoreCase tolerance —
        // otherwise list→chart sync silently misses on a casing difference.
        var byCanon = _chartVm.NodeByNameInsensitive(master.CanonicalName);
        if (byCanon != null) return byCanon;
        foreach (var alias in master.Aliases)
        {
            var hit = _chartVm.NodeByNameInsensitive(alias);
            if (hit != null) return hit;
        }
        return null;
    }

    private async Task CopyLinkAsync()
    {
        await CopyMasterLinkAsync(isReddit: false);
    }

    private async Task CopyMasterLinkAsync(bool isReddit)
    {
        var selected = ViewModel.SelectedMaster;
        if (selected == null)
            return;

        var text = isReddit
            ? ZenUriParser.BuildShareableMasterUrl(selected.CanonicalName)
            : ZenUriParser.BuildShareableMasterUrl(selected.CanonicalName);

        var top = TopLevel.GetTopLevel(this);
        if (top?.Clipboard != null)
            await top.Clipboard.SetTextAsync(text);

        ViewModel.StatusText = isReddit
            ? $"Reddit link copied for {selected.CanonicalName}."
            : $"Copied link for {selected.CanonicalName}.";
    }

    private async Task CopyMasterCitationAsync()
    {
        var master = ViewModel.SelectedMaster;
        if (master == null) return;

        var name = master.CanonicalName;
        var dates = master.DatesSummary;
        // Web citation of the master's public profile page. Uses the real
        // shareable URL (readzen.pages.dev/master/{slug} — the old
        // readzen.app/masters/ form pointed at a domain that never existed)
        // and carries an access date because web content is mutable.
        var url = ZenUriParser.BuildShareableMasterUrl(name);
        var accessed = CitationDates.DayMonthYear(CitationDates.Today);
        var citation = $"{name} ({dates}). Zen Master Database, Read Zen. {url}. Accessed {accessed}.";

        var top = TopLevel.GetTopLevel(this);
        if (top?.Clipboard != null)
            await top.Clipboard.SetTextAsync(citation);

        ViewModel.StatusText = $"Citation copied for {name}.";
    }

    private async Task OpenEditorAsync()
    {
        if (await ShowMasterDatesEditorDialogAsync(HostWindow, _baseFilePath, _repoRoot, ViewModel.SelectedMaster?.CanonicalName))
        {
            var current = ViewModel.SelectedMaster?.CanonicalName;
            await ViewModel.LoadAsync();
            ViewModel.ApplyLanding(current, null);
            ViewModel.StatusText = "Master dates updated.";
        }
    }

    protected virtual async Task<bool> ShowMasterDatesEditorDialogAsync(Window? owner, string baseFilePath, string? repoRoot, string? landingName)
    {
        var dlg = new MasterDatesEditorDialog(baseFilePath, repoRoot)
        {
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            RequestedThemeVariant = ActualThemeVariant
        };
        dlg.ApplyLanding(landingName);
        _editorWindow = dlg;
        if (owner != null)
            await dlg.ShowDialog(owner);
        else
            dlg.Show();
        return dlg.Saved;
    }
}
