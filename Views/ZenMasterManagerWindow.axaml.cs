using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ReadZen.App.Models;
using ReadZen.App.Services;
using ReadZen.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using ReadZen.App.Infrastructure;

namespace ReadZen.App.Views;

public partial class ZenMasterManagerWindow : Window
{
    private readonly string? _repoRoot;
    private readonly string? _parentRoot;
    private readonly string _baseFilePath;
    private MasterDatesEditorDialog? _editorWindow;
    private string? _pendingLandingName;
    private string? _pendingLandingUser;
    private bool _loaded;

    /// <summary>Fired when the user double-clicks a corpus text to navigate to it in the reader.</summary>
    public event EventHandler<NavigationRequest>? CorpusNavigationRequested;

    public ZenMasterManagerWindowViewModel ViewModel { get; }

    public ZenMasterManagerWindow() : this(null, null, Path.Combine(AppContext.BaseDirectory, "Assets", "Data", "master-dates.json"))
    {
    }

    public ZenMasterManagerWindow(string? repoRoot, string? parentRoot = null, string? baseFilePath = null)
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
                try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
                catch { }
                e.Handled = true;
            }
        }, RoutingStrategies.Tunnel);

        Opened += async (_, _) =>
        {
            if (!_loaded)
            {
                await ViewModel.LoadAsync();
                _loaded = true;

                // Build lineage graph after catalog loads
                BuildLineageGraph();
            }

            ApplyPendingLanding();
        };
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private void WireEvents()
    {
        var btnClose = this.FindControl<Button>("BtnClose");
        if (btnClose != null)
            btnClose.Click += (_, _) => Close();

        var btnCopyLink = this.FindControl<Button>("BtnCopyLink");
        if (btnCopyLink != null)
            btnCopyLink.Click += (_, _) => AsyncGuard.Run(async () => await CopyLinkAsync(), "ZenMasterManagerWindow.btnCopyLink.Click");

        var btnEditDates = this.FindControl<Button>("BtnEditDates");
        if (btnEditDates != null)
            btnEditDates.Click += (_, _) => AsyncGuard.Run(async () => await OpenEditorAsync(), "ZenMasterManagerWindow.btnEditDates.Click");

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
            mnuCopyLink.Click += (_, _) => AsyncGuard.Run(async () => await CopyMasterLinkAsync(isReddit: false), "ZenMasterManagerWindow.mnuCopyLink.Click");

        var mnuCopyReddit = this.FindControl<MenuItem>("MnuCopyMasterRedditLink");
        if (mnuCopyReddit != null)
            mnuCopyReddit.Click += (_, _) => AsyncGuard.Run(async () => await CopyMasterLinkAsync(isReddit: true), "ZenMasterManagerWindow.mnuCopyReddit.Click");

        var mnuCiteMaster = this.FindControl<MenuItem>("MnuCiteMaster");
        if (mnuCiteMaster != null)
            mnuCiteMaster.Click += (_, _) => AsyncGuard.Run(async () => await CopyMasterCitationAsync(), "ZenMasterManagerWindow.mnuCiteMaster.Click");

        // Corpus search tab
        var btnBuildIndex = this.FindControl<Button>("BtnBuildCorpusIndex");
        if (btnBuildIndex != null)
            btnBuildIndex.Click += (_, _) => AsyncGuard.Run(async () => await ViewModel.BuildCorpusIndexAsync(), "ZenMasterManagerWindow.btnBuildIndex.Click");

        var btnCancelScan = this.FindControl<Button>("BtnCancelCorpusScan");
        if (btnCancelScan != null)
            btnCancelScan.Click += (_, _) => ViewModel.CancelCorpusScan();

        // Double-click on corpus result -> navigate to text
        var lstPrimary = this.FindControl<ListBox>("LstCorpusPrimary");
        var lstSecondary = this.FindControl<ListBox>("LstCorpusSecondary");

        WireCorpusListDoubleClick(lstPrimary);
        WireCorpusListDoubleClick(lstSecondary);
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
            vm.FocusNode(match);          // selects + raises NodeFocusRequested (centres + list-sync)
            chart.SetZoom(1.2);           // close enough to read labels
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

    private void SelectTab(int index)
    {
        var tabs = this.FindControl<TabControl>("TabMain");
        if (tabs != null) tabs.SelectedIndex = index;
    }

    private void OpenExternalUrl(string url)
    {
        try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
        catch { /* a bad link must never crash the window */ }
    }

    private void NavigateCorpusPath(string teiPath)
    {
        if (string.IsNullOrWhiteSpace(teiPath)) return;
        CorpusNavigationRequested?.Invoke(this, new NavigationRequest
        {
            RelPath = teiPath,
            Side = SearchSide.Original,
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
        if (!string.IsNullOrEmpty(master.CanonicalName) &&
            _chartVm.Graph.ByName.TryGetValue(master.CanonicalName, out var byCanon))
            return byCanon;
        foreach (var alias in master.Aliases)
            if (!string.IsNullOrEmpty(alias) && _chartVm.Graph.ByName.TryGetValue(alias, out var byAlias))
                return byAlias;
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
        var citation = $"{name} ({dates}). 301 Zen Master Database, Read Zen. https://readzen.app/masters/{Uri.EscapeDataString(name)}";

        var top = TopLevel.GetTopLevel(this);
        if (top?.Clipboard != null)
            await top.Clipboard.SetTextAsync(citation);

        ViewModel.StatusText = $"Citation copied for {name}.";
    }

    private async Task OpenEditorAsync()
    {
        if (await ShowMasterDatesEditorDialogAsync(this, _baseFilePath, _repoRoot, ViewModel.SelectedMaster?.CanonicalName))
        {
            var current = ViewModel.SelectedMaster?.CanonicalName;
            await ViewModel.LoadAsync();
            ViewModel.ApplyLanding(current, null);
            ViewModel.StatusText = "Master dates updated.";
        }
    }

    protected virtual async Task<bool> ShowMasterDatesEditorDialogAsync(Window owner, string baseFilePath, string? repoRoot, string? landingName)
    {
        var dlg = new MasterDatesEditorDialog(baseFilePath, repoRoot)
        {
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            RequestedThemeVariant = ActualThemeVariant
        };
        dlg.ApplyLanding(landingName);
        _editorWindow = dlg;
        await dlg.ShowDialog(owner);
        return dlg.Saved;
    }
}