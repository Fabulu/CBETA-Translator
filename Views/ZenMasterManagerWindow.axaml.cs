using System;
using System.IO;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using ReadZen.App.Models;
using ReadZen.App.Services;
using ReadZen.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace ReadZen.App.Views;

public partial class ZenMasterManagerWindow : Window
{
    private readonly string? _repoRoot;
    private readonly string _baseFilePath;
    private MasterDatesEditorDialog? _editorWindow;
    private string? _pendingLandingName;
    private string? _pendingLandingUser;
    private bool _loaded;

    public ZenMasterManagerWindowViewModel ViewModel { get; }

    public ZenMasterManagerWindow() : this(null, Path.Combine(AppContext.BaseDirectory, "Assets", "Data", "master-dates.json"))
    {
    }

    public ZenMasterManagerWindow(string? repoRoot, string? baseFilePath = null)
    {
        _repoRoot = repoRoot;
        _baseFilePath = string.IsNullOrWhiteSpace(baseFilePath)
            ? Path.Combine(AppContext.BaseDirectory, "Assets", "Data", "master-dates.json")
            : baseFilePath;

        InitializeComponent();

        var service = new ZenMasterManagerService(App.Services.GetRequiredService<IMasterDatesService>());
        ViewModel = new ZenMasterManagerWindowViewModel(service, repoRoot, _baseFilePath);
        DataContext = ViewModel;

        WireEvents();
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
        var selected = ViewModel.SelectedMaster;
        if (selected == null)
            return;

        var uri = ZenUriParser.BuildMasterUri(selected.CanonicalName);
        var top = TopLevel.GetTopLevel(this);
        if (top?.Clipboard != null)
            await top.Clipboard.SetTextAsync(uri);
        ViewModel.StatusText = $"Copied link for {selected.CanonicalName}.";
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