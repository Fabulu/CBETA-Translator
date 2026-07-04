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
            btnCopyLink.Click += (_, _) => AsyncGuard.Run(async () => await CopyLinkAsync(), "ZenMasterManagerWindow.btnCopyLink.Click");

        var btnEditDates = this.FindControl<Button>("BtnEditDates");
        if (btnEditDates != null)
            btnEditDates.Click += (_, _) => AsyncGuard.Run(async () => await OpenEditorAsync(), "ZenMasterManagerWindow.btnEditDates.Click");

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

        // Search: highlight on text change, center + zoom on Enter or "Go to" click
        void GoToFirstMatch()
        {
            if (_lineageGraphVm == null || lineageGraph == null) return;
            var match = _lineageGraphVm.Nodes.FirstOrDefault(n => n.IsHighlighted);
            if (match == null) return;

            // Select + focus the match
            foreach (var n in _lineageGraphVm.Nodes) n.IsSelected = false;
            match.IsSelected = true;
            _lineageGraphVm.SelectedNode = match;
            _lineageGraphVm.FocusOn(match);

            // Center + zoom so the match fills a comfortable portion of the viewport
            lineageGraph.CenterOnNode(match);
            lineageGraph.SetZoom(1.2); // close enough to read labels clearly

            // Sync the zoom slider
            var slider = this.FindControl<Slider>("SliderLineageZoom");
            if (slider != null) slider.Value = 120;

            lineageGraph.InvalidateVisual();
        }

        if (txtSearch != null)
        {
            txtSearch.TextChanged += (_, _) =>
            {
                _lineageGraphVm?.HighlightSearch(txtSearch.Text);
                lineageGraph?.InvalidateVisual();
            };

            txtSearch.KeyDown += (_, e) =>
            {
                if (e.Key == Avalonia.Input.Key.Enter)
                {
                    GoToFirstMatch();
                    e.Handled = true;
                }
            };
        }

        if (btnCenter != null)
        {
            btnCenter.Click += (_, _) => GoToFirstMatch();
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