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

        var service = new ZenMasterManagerService(App.Services.GetRequiredService<IMasterDatesService>());
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
            btnCopyLink.Click += async (_, _) => await CopyLinkAsync();

        var btnEditDates = this.FindControl<Button>("BtnEditDates");
        if (btnEditDates != null)
            btnEditDates.Click += async (_, _) => await OpenEditorAsync();

        // Lineage Web tab
        var lineageGraph = this.FindControl<LineageWebControl>("LineageGraph");
        var txtSearch = this.FindControl<TextBox>("TxtLineageSearch");
        var txtInfo = this.FindControl<TextBlock>("TxtLineageInfo");
        var btnCenter = this.FindControl<Button>("BtnLineageCenter");

        if (lineageGraph != null)
        {
            lineageGraph.NodeClicked += (_, record) =>
            {
                ViewModel.SelectedMaster = record;
            };

            lineageGraph.NodeDoubleClicked += (_, record) =>
            {
                ViewModel.SelectedMaster = record;
                // Switch to List tab on double-click
                var tabs = this.FindControl<TabControl>("TabMain");
                if (tabs != null) tabs.SelectedIndex = 0;
            };
        }

        if (txtSearch != null)
        {
            txtSearch.TextChanged += (_, _) =>
            {
                _lineageGraphVm?.HighlightSearch(txtSearch.Text);
                lineageGraph?.InvalidateVisual();
            };
        }

        if (btnCenter != null)
        {
            btnCenter.Click += (_, _) =>
            {
                if (_lineageGraphVm?.SelectedNode != null && lineageGraph != null)
                    lineageGraph.CenterOnNode(_lineageGraphVm.SelectedNode);
            };
        }

        // Zoom slider
        var zoomSlider = this.FindControl<Slider>("SliderLineageZoom");
        if (zoomSlider != null && lineageGraph != null)
        {
            zoomSlider.Value = lineageGraph.Zoom * 100;
            zoomSlider.PropertyChanged += (_, args) =>
            {
                if (args.Property.Name == "Value")
                    lineageGraph.SetZoom(zoomSlider.Value / 100.0);
            };
        }

        // Sync graph selection when List tab selection changes
        ViewModel.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(ViewModel.SelectedMaster) && _lineageGraphVm != null)
            {
                var master = ViewModel.SelectedMaster;
                if (master != null)
                {
                    var graphNode = _lineageGraphVm.Nodes.Find(n => n.Record == master);
                    if (graphNode != null)
                    {
                        foreach (var n in _lineageGraphVm.Nodes) n.IsSelected = false;
                        graphNode.IsSelected = true;
                        _lineageGraphVm.SelectedNode = graphNode;
                        lineageGraph?.InvalidateVisual();
                    }
                }
            }
        };

        // Master list context menu
        var mnuCopyLink = this.FindControl<MenuItem>("MnuCopyMasterLink");
        if (mnuCopyLink != null)
            mnuCopyLink.Click += async (_, _) => await CopyMasterLinkAsync(isReddit: false);

        var mnuCopyReddit = this.FindControl<MenuItem>("MnuCopyMasterRedditLink");
        if (mnuCopyReddit != null)
            mnuCopyReddit.Click += async (_, _) => await CopyMasterLinkAsync(isReddit: true);

        // Corpus search tab
        var btnBuildIndex = this.FindControl<Button>("BtnBuildCorpusIndex");
        if (btnBuildIndex != null)
            btnBuildIndex.Click += async (_, _) => await ViewModel.BuildCorpusIndexAsync();

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

    private LineageGraphViewModel? _lineageGraphVm;

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
        var catalog = ViewModel.GetCatalog();
        if (catalog == null || catalog.Records.Count == 0) return;

        _lineageGraphVm = new LineageGraphViewModel();
        _lineageGraphVm.BuildGraph(catalog);
        _lineageGraphVm.RunLayeredLayout();

        var lineageGraph = this.FindControl<LineageWebControl>("LineageGraph");
        lineageGraph?.SetViewModel(_lineageGraphVm);

        var txtInfo = this.FindControl<TextBlock>("TxtLineageInfo");
        if (txtInfo != null)
            txtInfo.Text = $"{_lineageGraphVm.Nodes.Count} masters, {_lineageGraphVm.Edges.Count} links";
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
            : ZenUriParser.BuildMasterUri(selected.CanonicalName);

        var top = TopLevel.GetTopLevel(this);
        if (top?.Clipboard != null)
            await top.Clipboard.SetTextAsync(text);

        ViewModel.StatusText = isReddit
            ? $"Reddit link copied for {selected.CanonicalName}."
            : $"Copied link for {selected.CanonicalName}.";
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