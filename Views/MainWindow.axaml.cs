// Views/MainWindow.axaml.cs
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CbetaTranslator.App.Infrastructure;
using CbetaTranslator.App.Models;
using CbetaTranslator.App.Services;
using CbetaTranslator.App.Text;

namespace CbetaTranslator.App.Views;

public partial class MainWindow : Window
{
    private Button? _btnToggleNav;
    private Button? _btnOpenRoot;
    private Button? _btnSettings;
    private Button? _btnSave;                 // optional: may not exist in XAML
    private Button? _btnLicenses;
    private Button? _btnMinimize;
    private Button? _btnMaximize;
    private Button? _btnClose;

    private Button? _btnAddCommunityNote;     // optional: may not exist in XAML

    private Border? _navPanel;
    private Border? _topBar;
    private TreeView? _navTree;
    private TextBox? _navSearch;
    private CheckBox? _chkNavOriginal;
    private CheckBox? _chkNavTranslated;
    private ComboBox? _cmbNavStatus;
    private ComboBox? _cmbNavContext;
    private ComboBox? _cmbOrganizeBy;

    private TextBlock? _txtRoot;
    private TextBlock? _txtCurrentFile;
    private TextBlock? _txtStatus;

    private TabControl? _tabs;

    private ReadableTabView? _readableView;
    private TranslationTabView? _translationView;
    private SearchTabView? _searchView;
    private GitTabView? _gitView;

    // optional: is commented out in your XAML right now
    private CheckBox? _chkNightMode;

    private readonly IFileService _fileService = new FileService();
    private readonly AppConfigService _configService = new AppConfigService();
    private readonly PdfExportInteropService _pdfExportService = new PdfExportInteropService();
    private readonly MarkdownTranslationService _markdownService = new MarkdownTranslationService();
    private AppConfig? _config;
    private bool _isDarkTheme = true; // Default to dark theme
    private readonly IndexCacheService _indexCacheService = new IndexCacheService();
    private readonly SearchIndexService _navSearchIndexService = new();
    private readonly RenderedDocumentCacheService _renderCache = new RenderedDocumentCacheService(maxEntries: 48);

    private string? _root;
    private string? _originalDir;
    private string? _translatedDir;

    private List<FileNavItem> _allItems = new();
    private List<FileNavItem> _filteredItems = new();
    private readonly Dictionary<string, FileNavItem> _allItemsByRel = new(StringComparer.OrdinalIgnoreCase);
    private CancellationTokenSource? _navSearchCts;

    private string? _currentRelPath;

    private string _rawOrigXml = "";
    private string _rawTranMarkdown = "";
    private string _rawTranXml = "";
    private string _rawTranXmlReadable = "";

    private CancellationTokenSource? _renderCts;
    private bool _suppressNavSelectionChanged;

    public MainWindow()
    {
        InitializeComponent();
        FindControls();
        WireEvents();
        WireChildViewEvents();
        InitNavControls();

        SetStatus("Ready.");
        UpdateSaveButtonState();

        // Load configuration and apply theme
        _ = LoadConfigAndApplyThemeAsync();

        _ = TryAutoLoadRootFromConfigAsync();
    }

    private void InitNavControls()
    {
        if (_cmbNavStatus != null)
        {
            _cmbNavStatus.ItemsSource = new[] { "All", "Red", "Yellow", "Green" };
            _cmbNavStatus.SelectedIndex = 0;
        }

        if (_cmbNavContext != null)
        {
            _cmbNavContext.ItemsSource = new[] { "20", "40", "80" };
            _cmbNavContext.SelectedIndex = 1;
        }

        if (_cmbOrganizeBy != null)
        {
            _cmbOrganizeBy.ItemsSource = new[]
            {
                "Tradition  ->  Dynasty",
                "Dynasty    ->  Tradition",
                "Geography  ->  Tradition"
            };
            _cmbOrganizeBy.SelectedIndex = 0;
        }
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private void FindControls()
    {
        // Top bar
        _btnToggleNav = this.FindControl<Button>("BtnToggleNav");
        _btnOpenRoot = this.FindControl<Button>("BtnOpenRoot");
        _btnSettings = this.FindControl<Button>("BtnSettings");
        _btnSave = this.FindControl<Button>("BtnSave"); // may be null (not in XAML)
        _btnLicenses = this.FindControl<Button>("BtnLicenses");
        _btnAddCommunityNote = this.FindControl<Button>("BtnAddCommunityNote"); // may be null (not in XAML)
        _btnMinimize = this.FindControl<Button>("BtnMinimize");
        _btnMaximize = this.FindControl<Button>("BtnMaximize");
        _btnClose = this.FindControl<Button>("BtnClose");

        // Left nav
        _topBar = this.FindControl<Border>("TopBar");
        _navPanel = this.FindControl<Border>("NavPanel");
        _navTree = this.FindControl<TreeView>("NavTree");
        _navSearch = this.FindControl<TextBox>("NavSearch");
        _chkNavOriginal = this.FindControl<CheckBox>("ChkNavOriginal");
        _chkNavTranslated = this.FindControl<CheckBox>("ChkNavTranslated");
        _cmbNavStatus = this.FindControl<ComboBox>("CmbNavStatus");
        _cmbNavContext = this.FindControl<ComboBox>("CmbNavContext");
        _cmbOrganizeBy = this.FindControl<ComboBox>("CmbOrganizeBy");

        // Status labels
        _txtRoot = this.FindControl<TextBlock>("TxtRoot");
        _txtCurrentFile = this.FindControl<TextBlock>("TxtCurrentFile");
        _txtStatus = this.FindControl<TextBlock>("TxtStatus");

        // Tabs + views
        _tabs = this.FindControl<TabControl>("MainTabs");

        _readableView = this.FindControl<ReadableTabView>("ReadableView");
        _translationView = this.FindControl<TranslationTabView>("TranslationView");
        _searchView = this.FindControl<SearchTabView>("SearchView");
        _gitView = this.FindControl<GitTabView>("GitView");

        // Optional theme checkbox (currently commented out in XAML)
        _chkNightMode = this.FindControl<CheckBox>("ChkNightMode");
    }

    private void WireEvents()
    {
        if (_btnToggleNav != null) _btnToggleNav.Click += ToggleNav_Click;
        if (_btnOpenRoot != null)
            _btnOpenRoot.Click += (_, _) => OpenRoot_Click(null, null);

        if (_btnSettings != null)
            _btnSettings.Click += OnSettingsClicked;

        if (_btnLicenses != null)
            _btnLicenses.Click += (_, _) => Licenses_Click(null, null);
        if (_btnMinimize != null) _btnMinimize.Click += (_, _) => WindowState = WindowState.Minimized;
        if (_btnMaximize != null) _btnMaximize.Click += (_, _) =>
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
        };
        if (_btnClose != null) _btnClose.Click += (_, _) => Close();

        // Optional buttons: wire only if they exist in XAML
        if (_btnSave != null) _btnSave.Click += Save_Click;

        // IMPORTANT: unify add-note behavior to ONE handler
        if (_btnAddCommunityNote != null) _btnAddCommunityNote.Click += AddCommunityNote_Click;

        if (_navTree != null) _navTree.SelectionChanged += NavTree_SelectionChanged;
        if (_tabs != null) _tabs.SelectionChanged += (_, _) => UpdateSaveButtonState();

        if (_navSearch != null)
            _navSearch.TextChanged += async (_, _) => await ApplyFilterAsync();

        if (_chkNavOriginal != null) _chkNavOriginal.IsCheckedChanged += async (_, _) => await ApplyFilterAsync();
        if (_chkNavTranslated != null) _chkNavTranslated.IsCheckedChanged += async (_, _) => await ApplyFilterAsync();
        if (_cmbNavStatus != null) _cmbNavStatus.SelectionChanged += async (_, _) => await ApplyFilterAsync();
        if (_cmbNavContext != null) _cmbNavContext.SelectionChanged += async (_, _) => await ApplyFilterAsync();
        if (_cmbOrganizeBy != null) _cmbOrganizeBy.SelectionChanged += async (_, _) => await ApplyFilterAsync();

        if (_topBar != null)
            _topBar.DoubleTapped += (_, _) =>
            {
                WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            };

        // Optional theme checkbox (if you later un-comment it in XAML)
        // if (_chkNightMode != null)
        //     _chkNightMode.IsCheckedChanged += (_, _) => ApplyTheme(dark: _chkNightMode.IsChecked == true);
    }

    private void TopBar_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        var visual = e.Source as Visual;
        while (visual != null)
        {
            if (visual is Button or TextBox or CheckBox or ComboBox)
                return;
            visual = visual.GetVisualParent();
        }

        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
        {
            try { BeginMoveDrag(e); } catch { }
        }
    }

    private void WireChildViewEvents()
    {
        if (_readableView != null)
            _readableView.Status += (_, msg) => SetStatus(msg);

        if (_translationView != null)
        {
            _translationView.SaveRequested += async (_, _) => await SaveTranslatedFromTabAsync();
            _translationView.RevertRequested += async (_, _) => await RevertMarkdownFromOriginalAsync();
            _translationView.ExportPdfRequested += async (_, _) => await ExportCurrentPairToPdfAsync();
            _translationView.Status += (_, msg) => SetStatus(msg);
        }

        if (_readableView != null)
        {
            _readableView.CommunityNoteInsertRequested += async (_, req) =>
            {
                try
                {
                    if (!EnsureFileContextForNoteOps(out var _, out var _))
                        return;

                    if (string.IsNullOrWhiteSpace(_rawTranXml))
                        _rawTranXml = _rawOrigXml;

                    _rawTranXml = CommunityNoteXmlEditor.InsertCommunityNote(_rawTranXml, req.XmlIndex, req.NoteText, req.Resp);
                    _rawTranXmlReadable = _markdownService.CreateReadableInlineEnglishXml(_rawTranXml);
                    _rawTranMarkdown = _markdownService.ConvertTeiToMarkdown(_rawTranXml, Path.GetFileName(_currentRelPath));

                    if (_translatedDir != null && _currentRelPath != null)
                    {
                        await _fileService.WriteTranslatedAsync(_translatedDir, _currentRelPath, _rawTranXml);
                        SafeInvalidateRenderCache(Path.Combine(_translatedDir, _currentRelPath));
                        await RefreshFileStatusAsync(_currentRelPath);
                    }

                    _translationView?.SetXml(_rawOrigXml, _rawTranMarkdown);
                    await RefreshReadableFromRawAsync();
                    SetStatus("Community note inserted.");
                }
                catch (Exception ex)
                {
                    SetStatus("Add note failed: " + ex.Message);
                }
            };

            _readableView.CommunityNoteDeleteRequested += async (_, req) =>
            {
                try
                {
                    if (!EnsureFileContextForNoteOps(out var _, out var _))
                        return;

                    if (string.IsNullOrWhiteSpace(_rawTranXml))
                        _rawTranXml = _rawOrigXml;

                    _rawTranXml = CommunityNoteXmlEditor.DeleteSpan(_rawTranXml, req.XmlStart, req.XmlEndExclusive);
                    _rawTranXmlReadable = _markdownService.CreateReadableInlineEnglishXml(_rawTranXml);
                    _rawTranMarkdown = _markdownService.ConvertTeiToMarkdown(_rawTranXml, Path.GetFileName(_currentRelPath));

                    if (_translatedDir != null && _currentRelPath != null)
                    {
                        await _fileService.WriteTranslatedAsync(_translatedDir, _currentRelPath, _rawTranXml);
                        SafeInvalidateRenderCache(Path.Combine(_translatedDir, _currentRelPath));
                        await RefreshFileStatusAsync(_currentRelPath);
                    }

                    _translationView?.SetXml(_rawOrigXml, _rawTranMarkdown);
                    await RefreshReadableFromRawAsync();
                    SetStatus("Community note deleted.");
                }
                catch (Exception ex)
                {
                    SetStatus("Delete note failed: " + ex.Message);
                }
            };
        }

        if (_searchView != null)
        {
            _searchView.Status += (_, msg) => SetStatus(msg);
            _searchView.OpenFileRequested += async (_, rel) =>
            {
                await SelectInNavAsync(rel);
                await LoadPairAsync(rel);

                if (_tabs != null)
                    _tabs.SelectedIndex = 0;
            };
        }

        if (_gitView != null)
        {
            _gitView.Status += (_, msg) => SetStatus(msg);
            _gitView.EnsureTranslatedForSelectedRequested += async relPath =>
            {
                try
                {
                    return await EnsureTranslatedXmlForRelPathAsync(relPath, saveCurrentMarkdown: true);
                }
                catch (Exception ex)
                {
                    SetStatus("Prepare translated XML failed: " + ex.Message);
                    return false;
                }
            };

            _gitView.RootCloned += async (_, repoRoot) =>
            {
                try
                {
                    await LoadRootAsync(repoRoot, saveToConfig: true);

                    if (_tabs != null)
                        _tabs.SelectedIndex = 0;
                }
                catch (Exception ex)
                {
                    SetStatus("Failed to load cloned repo: " + ex.Message);
                }
            };
        }
    }

    // Ensures: translation view exists, current file exists, paths resolved, translation view has file paths.
    private bool EnsureFileContextForNoteOps(out string origAbs, out string tranAbs)
    {
        origAbs = "";
        tranAbs = "";

        if (_translationView == null)
        {
            SetStatus("Cannot modify notes: Translation view missing.");
            return false;
        }

        if (_currentRelPath == null || _originalDir == null || _translatedDir == null)
        {
            SetStatus("Cannot modify notes: no file loaded.");
            return false;
        }

        origAbs = Path.Combine(_originalDir, _currentRelPath);
        tranAbs = Path.Combine(_translatedDir, _currentRelPath);

        try { _translationView.SetCurrentFilePaths(origAbs, tranAbs); } catch { }

        return true;
    }

    private static async Task<string> SafeReadTextAsync(string path)
    {
        try
        {
            if (File.Exists(path))
                return await File.ReadAllTextAsync(path);
        }
        catch { }
        return "";
    }

    private void SafeInvalidateRenderCache(string tranAbs)
    {
        try { _renderCache.Invalidate(tranAbs); } catch { }
    }

    private void ToggleNav_Click(object? sender, RoutedEventArgs e)
    {
        if (_navPanel == null) return;
        _navPanel.IsVisible = !_navPanel.IsVisible;
    }

    private async void OpenRoot_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (StorageProvider is null)
            {
                SetStatus("StorageProvider not available.");
                return;
            }

            var picked = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select CBETA root folder (contains xml-p5; xml-p5t created if missing)"
            });

            var folder = picked.FirstOrDefault();
            if (folder is null) return;

            await LoadRootAsync(folder.Path.LocalPath, saveToConfig: true);
        }
        catch (Exception ex)
        {
            SetStatus("Open root failed: " + ex.Message);
        }
    }

    private async void Licenses_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            var win = new LicensesWindow(_root);
            await win.ShowDialog(this);
        }
        catch (Exception ex)
        {
            SetStatus("Failed to open licenses: " + ex.Message);
        }
    }

    // ONE add-note handler (used by optional global button if present)
    private async void AddCommunityNote_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (_readableView == null)
            {
                SetStatus("Add note: Readable view not available.");
                return;
            }

            if (_currentRelPath == null)
            {
                SetStatus("Add note: Select a file first.");
                return;
            }

            if (_tabs != null)
                _tabs.SelectedIndex = 0;

            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Loaded);
            var (ok, reason) = await _readableView.TryAddCommunityNoteAtSelectionOrCaretAsync();
            SetStatus(ok ? "Add note: OK (" + reason + ")" : "Add note: FAILED (" + reason + ")");
        }
        catch (Exception ex)
        {
            SetStatus("Add note failed: " + ex.Message);
        }
    }

    private void Save_Click(object? sender, RoutedEventArgs e)
    {
        _ = SaveTranslatedFromTabAsync();
    }

    private async Task TryAutoLoadRootFromConfigAsync()
    {
        var cfg = await _configService.TryLoadAsync();
        if (cfg?.TextRootPath is null) return;

        try
        {
            if (!Directory.Exists(cfg.TextRootPath))
                return;

            SetStatus("Auto-loading last root…");
            await LoadRootAsync(cfg.TextRootPath, saveToConfig: false);
        }
        catch
        {
            // ignore
        }
    }

    private async Task LoadRootAsync(string rootPath, bool saveToConfig)
    {
        _root = rootPath;
        _originalDir = AppPaths.GetOriginalDir(_root);
        _translatedDir = AppPaths.GetTranslatedDir(_root);

        _renderCache.Clear();

        if (_txtRoot != null) _txtRoot.Text = _root;

        if (!Directory.Exists(_originalDir))
        {
            SetStatus($"Original folder missing: {_originalDir}");
            return;
        }

        AppPaths.EnsureTranslatedDirExists(_root);

        _gitView?.SetCurrentRepoRoot(_root);
        _searchView?.SetRootContext(_root, _originalDir, _translatedDir);

        if (saveToConfig)
        {
            _config ??= new AppConfig();
            _config.TextRootPath = _root;
            await _configService.SaveAsync(_config);
        }

        await LoadFileListFromCacheOrBuildAsync();
    }

    private async Task LoadFileListFromCacheOrBuildAsync()
    {
        if (_root == null || _originalDir == null || _translatedDir == null || _navTree == null)
            return;

        ClearViews();

        void WireSearchTab()
        {
            if (_searchView == null) return;

            _searchView.SetContext(
                _root!,
                _originalDir!,
                _translatedDir!,
                fileMeta: relKey =>
                {
                    _allItemsByRel.TryGetValue(NormalizeRelForLogs(relKey), out var canon);

                    if (canon != null)
                        return (canon.DisplayShort, canon.Tooltip, canon.Status);

                    string rel = relKey;
                    return (rel, rel, null);
                });
        }

        var cache = await _indexCacheService.TryLoadAsync(_root);
        if (cache?.Entries is { Count: > 0 })
        {
            _allItems = cache.Entries;
            RebuildLookupAndFilters();
            await ApplyFilterAsync();
            WireSearchTab();
            SetStatus($"Loaded index cache: {_allItems.Count:n0} files.");
            return;
        }

        SetStatus("Building index cache… (first run will take a moment)");

        var progress = new Progress<(int done, int total)>(p =>
        {
            SetStatus($"Indexing files… {p.done:n0}/{p.total:n0}");
        });

        IndexCache built;
        try
        {
            built = await _indexCacheService.BuildAsync(_originalDir, _translatedDir, _root, progress);
        }
        catch (Exception ex)
        {
            SetStatus("Index build failed: " + ex.Message);
            return;
        }

        await _indexCacheService.SaveAsync(_root, built);

        _allItems = built.Entries ?? new List<FileNavItem>();
        RebuildLookupAndFilters();
        await ApplyFilterAsync();
        WireSearchTab();

        SetStatus($"Index cache created: {_allItems.Count:n0} files.");
    }

    private void RebuildLookupAndFilters()
    {
        _allItemsByRel.Clear();
        foreach (var item in _allItems)
            _allItemsByRel[NormalizeRelForLogs(item.RelPath)] = item;
    }

    private static string GetFirstTradition(FileNavItem item)
        => item.Traditions is { Count: > 0 } ? item.Traditions[0] : "Unknown Tradition";

    private (Func<FileNavItem, string> primary, Func<FileNavItem, string> secondary, string tint) GetHierarchy()
    {
        var choice = _cmbOrganizeBy?.SelectedItem as string ?? "Tradition  ->  Dynasty";

        if (string.Equals(choice, "Dynasty    ->  Tradition", StringComparison.Ordinal))
        {
            return (
                it => string.IsNullOrWhiteSpace(it.Period) ? "Unknown Dynasty" : it.Period,
                it => GetFirstTradition(it),
                "#144A90E2"
            );
        }

        if (string.Equals(choice, "Geography  ->  Tradition", StringComparison.Ordinal))
        {
            return (
                it => string.IsNullOrWhiteSpace(it.Origin) ? "Unknown Origin" : it.Origin,
                it => GetFirstTradition(it),
                "#14C97A1E"
            );
        }

        return (
            it => GetFirstTradition(it),
            it => string.IsNullOrWhiteSpace(it.Period) ? "Unknown Dynasty" : it.Period,
            "#143FA34D"
        );
    }

    private TranslationStatus? GetStatusFilter()
    {
        var selected = _cmbNavStatus?.SelectedItem as string ?? "All";
        return selected switch
        {
            "Red" => TranslationStatus.Red,
            "Yellow" => TranslationStatus.Yellow,
            "Green" => TranslationStatus.Green,
            _ => null
        };
    }

    private int GetContextWidth()
    {
        var selected = _cmbNavContext?.SelectedItem as string ?? "40";
        if (selected.StartsWith("20", StringComparison.Ordinal)) return 20;
        if (selected.StartsWith("80", StringComparison.Ordinal)) return 80;
        return 40;
    }

    private async Task<HashSet<string>?> ComputeFullTextMatchesAsync(string query, CancellationToken ct)
    {
        if (_root == null || _originalDir == null || _translatedDir == null || string.IsNullOrWhiteSpace(query))
            return null;

        var manifest = await _navSearchIndexService.TryLoadAsync(_root);
        if (manifest == null)
            return null;

        bool includeOriginal = _chkNavOriginal?.IsChecked != false;
        bool includeTranslated = _chkNavTranslated?.IsChecked == true;
        if (!includeOriginal && !includeTranslated)
            includeOriginal = true;

        var statusFilter = GetStatusFilter();
        var matches = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        await foreach (var group in _navSearchIndexService.SearchAllAsync(
                           _root,
                           _originalDir,
                           _translatedDir,
                           manifest,
                           query,
                           includeOriginal,
                           includeTranslated,
                           fileMeta: rel =>
                           {
                               _allItemsByRel.TryGetValue(NormalizeRelForLogs(rel), out var found);
                               var status = found?.Status;
                               if (statusFilter.HasValue && status.HasValue && status.Value != statusFilter.Value)
                                   return ("", "", status);
                               if (statusFilter.HasValue && !status.HasValue)
                                   return ("", "", null);
                               return (found?.DisplayShort ?? rel, found?.Tooltip ?? rel, status);
                           },
                           contextWidth: GetContextWidth(),
                           progress: null,
                           ct: ct))
        {
            if (!string.IsNullOrWhiteSpace(group.RelPath))
                matches.Add(NormalizeRelForLogs(group.RelPath));
        }

        return matches;
    }

    private static bool MatchesLocalText(FileNavItem it, string qLower)
    {
        if (qLower.Length == 0) return true;
        if (!string.IsNullOrEmpty(it.RelPath) && it.RelPath.ToLowerInvariant().Contains(qLower)) return true;
        if (!string.IsNullOrEmpty(it.FileName) && it.FileName.ToLowerInvariant().Contains(qLower)) return true;
        if (!string.IsNullOrEmpty(it.DisplayShort) && it.DisplayShort.ToLowerInvariant().Contains(qLower)) return true;
        if (!string.IsNullOrEmpty(it.Tooltip) && it.Tooltip.ToLowerInvariant().Contains(qLower)) return true;
        return false;
    }

    private List<NavTreeNode> BuildNavTree(List<FileNavItem> items)
    {
        var (primary, secondary, tint) = GetHierarchy();
        var groups = items
            .GroupBy(primary)
            .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase)
            .Select(g => new NavTreeNode
            {
                Header = $"{g.Key} ({g.Count():n0})",
                NodeType = "group",
                TintBrush = tint,
                Children = g.GroupBy(secondary)
                    .OrderBy(sg => sg.Key, StringComparer.OrdinalIgnoreCase)
                    .Select(sg => (object)new NavTreeNode
                    {
                        Header = $"{sg.Key} ({sg.Count():n0})",
                        NodeType = "subgroup",
                        TintBrush = "#10000000",
                        Children = sg
                            .OrderBy(x => x.DisplayShort, StringComparer.OrdinalIgnoreCase)
                            .Cast<object>()
                            .ToList()
                    })
                    .ToList()
            })
            .ToList();

        return groups;
    }

    private async Task ApplyFilterAsync()
    {
        if (_navTree == null)
            return;

        _navSearchCts?.Cancel();
        _navSearchCts = new CancellationTokenSource();
        var ct = _navSearchCts.Token;

        var q = (_navSearch?.Text ?? "").Trim();
        var qLower = q.ToLowerInvariant();
        var selectedRel = (_navTree.SelectedItem as FileNavItem)?.RelPath ?? _currentRelPath;

        var statusFilter = GetStatusFilter();

        HashSet<string>? fullTextMatches = null;
        if (q.Length > 0)
        {
            try
            {
                fullTextMatches = await ComputeFullTextMatchesAsync(q, ct);
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch
            {
                fullTextMatches = null;
            }
        }

        IEnumerable<FileNavItem> seq = _allItems;
        if (statusFilter.HasValue)
            seq = seq.Where(x => x.Status == statusFilter.Value);

        if (q.Length > 0)
        {
            seq = seq.Where(x =>
                MatchesLocalText(x, qLower) ||
                (fullTextMatches != null && fullTextMatches.Contains(NormalizeRelForLogs(x.RelPath))));
        }

        _filteredItems = seq.ToList();
        var treeNodes = BuildNavTree(_filteredItems);
        _navTree.ItemsSource = treeNodes;

        if (!string.IsNullOrWhiteSpace(selectedRel))
        {
            var match = _filteredItems.FirstOrDefault(x =>
                string.Equals(x.RelPath, selectedRel, StringComparison.OrdinalIgnoreCase));
            if (match != null)
                _navTree.SelectedItem = match;
        }
    }

    private async Task SelectInNavAsync(string relPath)
    {
        if (_navTree == null || string.IsNullOrWhiteSpace(relPath))
            return;

        var match = _filteredItems.FirstOrDefault(x =>
            string.Equals(x.RelPath, relPath, StringComparison.OrdinalIgnoreCase));

        if (match == null && _navSearch != null && !string.IsNullOrWhiteSpace(_navSearch.Text))
        {
            _navSearch.Text = "";
            await ApplyFilterAsync();
            match = _filteredItems.FirstOrDefault(x =>
                string.Equals(x.RelPath, relPath, StringComparison.OrdinalIgnoreCase));
        }

        if (match == null)
            return;

        try
        {
            _suppressNavSelectionChanged = true;
            _navTree.SelectedItem = match;
        }
        finally
        {
            _suppressNavSelectionChanged = false;
        }
    }

    private void ClearViews()
    {
        try { _navSearchCts?.Cancel(); } catch { }
        _navSearchCts = null;

        _renderCts?.Cancel();
        _renderCts = null;

        _rawOrigXml = "";
        _rawTranMarkdown = "";
        _rawTranXml = "";
        _rawTranXmlReadable = "";
        _currentRelPath = null;

        if (_txtCurrentFile != null) _txtCurrentFile.Text = "";

        _readableView?.Clear();
        _translationView?.Clear();
        _searchView?.Clear();
        if (_navTree != null) _navTree.ItemsSource = null;

        UpdateSaveButtonState();
        _gitView?.SetSelectedRelPath(null);
    }

    private async void NavTree_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_suppressNavSelectionChanged)
            return;

        if (_navTree?.SelectedItem is not FileNavItem item)
            return;

        if (string.IsNullOrWhiteSpace(item.RelPath))
            return;

        await LoadPairAsync(item.RelPath);
    }

    private async Task<(RenderedDocument ro, RenderedDocument rt)> RenderPairCachedAsync(string relPath, CancellationToken ct)
    {
        if (_originalDir == null || _translatedDir == null)
            return (RenderedDocument.Empty, RenderedDocument.Empty);

        var origAbs = Path.Combine(_originalDir, relPath);
        var tranAbs = Path.Combine(_translatedDir, relPath);

        var stampOrig = FileStamp.FromFile(origAbs);
        var stampTran = FileStamp.FromFile(tranAbs);

        RenderedDocument ro;
        if (!_renderCache.TryGet(stampOrig, out ro))
        {
            ct.ThrowIfCancellationRequested();
            ro = CbetaTeiRenderer.Render(_rawOrigXml);
            _renderCache.Put(stampOrig, ro);
        }

        RenderedDocument rt;
        if (!_renderCache.TryGet(stampTran, out rt))
        {
            ct.ThrowIfCancellationRequested();
            rt = CbetaTeiRenderer.Render(string.IsNullOrWhiteSpace(_rawTranXmlReadable) ? _rawTranXml : _rawTranXmlReadable);
            _renderCache.Put(stampTran, rt);
        }

        return (ro, rt);
    }

    private async Task LoadPairAsync(string relPath)
    {
        if (_originalDir == null || _translatedDir == null)
            return;

        _renderCts?.Cancel();
        _renderCts = new CancellationTokenSource();
        var ct = _renderCts.Token;

        _currentRelPath = relPath;

        if (_txtCurrentFile != null)
            _txtCurrentFile.Text = relPath;

        _gitView?.SetSelectedRelPath(_currentRelPath);

        SetStatus("Loading: " + relPath);

        var swTotal = Stopwatch.StartNew();
        var swRead = Stopwatch.StartNew();

        var (orig, tran) = await _fileService.ReadPairAsync(_originalDir, _translatedDir, relPath);

        swRead.Stop();

        _rawOrigXml = orig ?? "";
        _rawTranXml = string.IsNullOrWhiteSpace(tran) ? _rawOrigXml : (tran ?? "");

        try
        {
            _rawTranMarkdown = _markdownService.ConvertTeiToMarkdown(_rawTranXml, Path.GetFileName(relPath));
            _rawTranXmlReadable = _markdownService.CreateReadableInlineEnglishXml(_rawTranXml);
        }
        catch (MarkdownTranslationException ex)
        {
            SetStatus("Markdown materialization warning: " + ex.Message);
            _rawTranXml = _rawOrigXml;
            _rawTranXmlReadable = _rawOrigXml;
        }

        // Ensure TranslationTabView knows which exact files it's operating on.
        try
        {
            var origAbs = Path.Combine(_originalDir, relPath);
            var tranAbs = Path.Combine(_translatedDir, relPath);
            _translationView?.SetCurrentFilePaths(origAbs, tranAbs);
        }
        catch { }

        _translationView?.SetXml(_rawOrigXml, _rawTranMarkdown);
        UpdateSaveButtonState();

        SetStatus("Rendering readable view…");

        try
        {
            var swRender = Stopwatch.StartNew();

            var renderTask = Task.Run(async () =>
            {
                ct.ThrowIfCancellationRequested();
                return await RenderPairCachedAsync(relPath, ct);
            }, ct);

            var (renderOrig, renderTran) = await renderTask;

            swRender.Stop();

            if (ct.IsCancellationRequested)
                return;

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                _readableView?.SetRendered(renderOrig, renderTran);

                swTotal.Stop();
                SetStatus(
                    $"Loaded. Segments: O={renderOrig.Segments.Count:n0}, T={renderTran.Segments.Count:n0}. " +
                    $"Read={swRead.ElapsedMilliseconds:n0}ms Render={swRender.ElapsedMilliseconds:n0}ms Total={swTotal.ElapsedMilliseconds:n0}ms");
            });
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            SetStatus("Render failed: " + ex.Message);
        }
    }

    private async Task SaveTranslatedFromTabAsync()
    {
        try
        {
            if (_currentRelPath == null)
            {
                SetStatus("Nothing to save (no file selected).");
                return;
            }

            if (_translationView == null)
            {
                SetStatus("Translation view not available.");
                return;
            }

            var md = _translationView.GetTranslatedMarkdown();
            _rawTranMarkdown = md ?? "";
            try
            {
                var baseXml = string.IsNullOrWhiteSpace(_rawTranXml) ? _rawOrigXml : _rawTranXml;
                _rawTranXml = _markdownService.MergeMarkdownIntoTei(baseXml, _rawTranMarkdown, out _);
                _rawTranXmlReadable = _markdownService.CreateReadableInlineEnglishXml(_rawTranXml);

                if (_translatedDir != null && _currentRelPath != null)
                {
                    await _fileService.WriteTranslatedAsync(_translatedDir, _currentRelPath, _rawTranXml);
                    SafeInvalidateRenderCache(Path.Combine(_translatedDir, _currentRelPath));
                    await RefreshFileStatusAsync(_currentRelPath);
                }

                SetStatus("Saved translated XML: " + _currentRelPath);
            }
            catch (MarkdownTranslationException ex)
            {
                SetStatus("Save failed: markdown to TEI materialization error. " + ex.Message);
                return;
            }

            _renderCts?.Cancel();
            _renderCts = new CancellationTokenSource();
            var ct = _renderCts.Token;

            SetStatus("Re-rendering readable view…");

            var sw = Stopwatch.StartNew();

            var renderTask = Task.Run(async () =>
            {
                ct.ThrowIfCancellationRequested();
                return await RenderPairCachedAsync(_currentRelPath, ct);
            }, ct);

            var (renderOrig, renderTran) = await renderTask;

            sw.Stop();

            if (!ct.IsCancellationRequested)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    _readableView?.SetRendered(renderOrig, renderTran);
                    SetStatus($"Saved + readable view updated. Render={sw.ElapsedMilliseconds:n0}ms");
                });
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            SetStatus("Save failed: " + ex.Message);
        }
    }

    private async Task RevertMarkdownFromOriginalAsync()
    {
        try
        {
            if (_currentRelPath == null)
            {
                SetStatus("Nothing to revert (no file selected).");
                return;
            }

            var confirm = await ConfirmAsync(
                "Rebuild Markdown View",
                "Regenerate the editable markdown view from current translated TEI XML?\n\nThis does not modify TEI XML.",
                "Rebuild",
                "Cancel");
            if (!confirm)
                return;

            var source = string.IsNullOrWhiteSpace(_rawTranXml) ? _rawOrigXml : _rawTranXml;
            _rawTranMarkdown = _markdownService.ConvertTeiToMarkdown(source, Path.GetFileName(_currentRelPath));
            _translationView?.SetXml(_rawOrigXml, _rawTranMarkdown);

            if (string.IsNullOrWhiteSpace(_rawTranXmlReadable))
            {
                _rawTranXmlReadable = _markdownService.CreateReadableInlineEnglishXml(source);
            }
            SetStatus("Markdown view rebuilt from TEI XML.");
        }
        catch (Exception ex)
        {
            SetStatus("Revert failed: " + ex.Message);
        }
    }

    private async Task<bool> ConfirmAsync(string title, string message, string yesText, string noText)
    {
        var yes = new Button
        {
            Content = yesText,
            MinWidth = 90,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        var no = new Button
        {
            Content = noText,
            MinWidth = 90
        };

        var text = new TextBlock
        {
            Text = message,
            TextWrapping = TextWrapping.Wrap
        };

        var row = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        row.Children.Add(no);
        row.Children.Add(yes);

        var panel = new StackPanel
        {
            Margin = new Thickness(14),
            Spacing = 12
        };
        panel.Children.Add(text);
        panel.Children.Add(row);

        var win = new Window
        {
            Title = title,
            Width = 560,
            Height = 220,
            Content = panel,
            Background = new SolidColorBrush(Color.Parse("#FFFEF5D0")),
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        bool result = false;
        yes.Click += (_, _) => { result = true; win.Close(); };
        no.Click += (_, _) => { result = false; win.Close(); };

        await win.ShowDialog(this);
        return result;
    }

    private async Task<bool> EnsureTranslatedXmlForRelPathAsync(string relPath, bool saveCurrentMarkdown)
    {
        if (_originalDir == null || _translatedDir == null)
            return false;

        var origPath = Path.Combine(_originalDir, relPath);
        if (!File.Exists(origPath))
            return false;

        var tranPath = Path.Combine(_translatedDir, relPath);
        var origXml = await SafeReadTextAsync(origPath);
        var tranXmlDisk = await SafeReadTextAsync(tranPath);
        if (string.IsNullOrWhiteSpace(tranXmlDisk))
            tranXmlDisk = origXml;

        if (saveCurrentMarkdown && _translationView != null && string.Equals(_currentRelPath, relPath, StringComparison.OrdinalIgnoreCase))
        {
            var currentMd = _translationView.GetTranslatedMarkdown();
            _rawTranMarkdown = currentMd ?? "";
            try
            {
                tranXmlDisk = _markdownService.MergeMarkdownIntoTei(tranXmlDisk, _rawTranMarkdown, out _);
                await _fileService.WriteTranslatedAsync(_translatedDir, relPath, tranXmlDisk);
            }
            catch (MarkdownTranslationException ex)
            {
                SetStatus("Markdown materialization warning: " + ex.Message);
                return false;
            }
        }

        if (!File.Exists(tranPath))
        {
            await _fileService.WriteTranslatedAsync(_translatedDir, relPath, tranXmlDisk);
        }

        if (string.Equals(_currentRelPath, relPath, StringComparison.OrdinalIgnoreCase))
        {
            _rawOrigXml = origXml;
            _rawTranXml = tranXmlDisk;
            _rawTranXmlReadable = _markdownService.CreateReadableInlineEnglishXml(_rawTranXml);
            _rawTranMarkdown = _markdownService.ConvertTeiToMarkdown(_rawTranXml, Path.GetFileName(relPath));
        }

        try
        {
            _renderCache.Invalidate(Path.Combine(_translatedDir, relPath));
        }
        catch { }

        // Update navigation status for this file
        await RefreshFileStatusAsync(relPath);

        return true;
    }

    private async Task RefreshFileStatusAsync(string relPath)
    {
        if (_originalDir == null || _translatedDir == null || _root == null)
            return;

        try
        {
            var origAbs = Path.Combine(_originalDir, relPath);
            var tranAbs = Path.Combine(_translatedDir, relPath);
            var relKey = NormalizeRelForLogs(relPath);

            // Compute new status
            var newStatus = _indexCacheService.ComputeStatusForPairLive(
                origAbs, tranAbs, _root, relKey, verboseLog: false);

            // Update the cached item if it exists
            if (_allItemsByRel.TryGetValue(relKey, out var existingItem))
            {
                var oldStatus = existingItem.Status;
                existingItem.Status = newStatus;

                // Refresh the navigation tree if status changed
                if (oldStatus != newStatus)
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        // Trigger a refresh of the navigation tree
                        if (_navTree != null)
                        {
                            _navTree.ItemsSource = null;
                            _navTree.ItemsSource = _filteredItems;
                        }
                    });
                }
            }
        }
        catch (Exception ex)
        {
            // Silently fail to avoid interrupting the save flow
            Debug.WriteLine($"Failed to refresh file status: {ex.Message}");
        }
    }

    // ✅ Re-render readable view from the *current* raw strings (no disk re-read, no editor race)
    private async Task RefreshReadableFromRawAsync()
    {
        if (_readableView == null)
            return;

        // Cancel any in-flight render
        _renderCts?.Cancel();
        _renderCts = new CancellationTokenSource();
        var ct = _renderCts.Token;

        try
        {
            SetStatus("Re-rendering readable view…");

            var sw = Stopwatch.StartNew();

            var renderTask = Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();
                var ro = CbetaTeiRenderer.Render(_rawOrigXml ?? "");
                var rt = CbetaTeiRenderer.Render(string.IsNullOrWhiteSpace(_rawTranXmlReadable) ? (_rawTranXml ?? "") : _rawTranXmlReadable);
                return (ro, rt);
            }, ct);

            var (renderOrig, renderTran) = await renderTask;

            sw.Stop();

            if (!ct.IsCancellationRequested)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    _readableView.SetRendered(renderOrig, renderTran);
                    SetStatus($"Readable view updated. Render={sw.ElapsedMilliseconds:n0}ms");
                });
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            SetStatus("Re-render failed: " + ex.Message);
        }
    }

    private void UpdateSaveButtonState()
    {
        // If you don't have a global save button in XAML, nothing to enable/disable here.
        if (_btnSave != null)
        {
            bool hasFile = _currentRelPath != null;
            bool translationTabSelected = _tabs?.SelectedIndex == 1;
            _btnSave.IsEnabled = hasFile && translationTabSelected;
        }

        if (_btnAddCommunityNote != null)
            _btnAddCommunityNote.IsEnabled = _currentRelPath != null;
    }

    private void SetStatus(string msg)
    {
        if (_txtStatus != null)
            _txtStatus.Text = msg;
    }

    private void ApplyTheme(bool dark)
    {
        string p = dark ? "Night_" : "Light_";

        void Map(string tokenKey, string sourceKey)
        {
            if (this.TryGetResource(sourceKey, null, out var v) && v != null)
                Resources[tokenKey] = v;
        }

        Map("AppBg", p + "AppBg");
        Map("BarBg", p + "BarBg");
        Map("NavBg", p + "NavBg");

        Map("TextFg", p + "TextFg");
        Map("TextMutedFg", p + "TextMutedFg");

        Map("ControlBg", p + "ControlBg");
        Map("ControlBgHover", p + "ControlBgHover");
        Map("ControlBgFocus", p + "ControlBgFocus");

        Map("BorderBrush", p + "BorderBrush");

        Map("BtnBg", p + "BtnBg");
        Map("BtnBgHover", p + "BtnBgHover");
        Map("BtnBgPressed", p + "BtnBgPressed");
        Map("BtnFg", p + "BtnFg");

        Map("TabBg", p + "TabBg");
        Map("TabBgSelected", p + "TabBgSelected");
        Map("TabFgSelected", p + "TabFgSelected");

        Map("TooltipBg", p + "TooltipBg");
        Map("TooltipBorder", p + "TooltipBorder");
        Map("TooltipFg", p + "TooltipFg");

        Map("SelectionBg", p + "SelectionBg");
        Map("SelectionFg", p + "SelectionFg");

        Map("XmlViewerBg", p + "XmlViewerBg");
        Map("XmlViewerBorder", p + "XmlViewerBorder");
    }

    private async Task LoadConfigAndApplyThemeAsync()
    {
        try
        {
            _config = await _configService.TryLoadAsync();
            if (_config != null)
            {
                _isDarkTheme = _config.IsDarkTheme;
                ApplyTheme(_isDarkTheme);
                _translationView?.SetPdfQuickSettings(_config);
            }
            else
            {
                _translationView?.SetPdfQuickSettings(new AppConfig { IsDarkTheme = _isDarkTheme });
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to load config: {ex.Message}");
            // Fallback to dark theme
            ApplyTheme(true);
        }
    }

    private async void OnSettingsClicked(object? sender, RoutedEventArgs e)
    {
        try
        {
            var current = _config ?? new AppConfig { IsDarkTheme = _isDarkTheme };
            var settingsWindow = new SettingsWindow(current);
            var result = await settingsWindow.ShowDialog<AppConfig?>(this);

            if (result == null)
                return;

            _config = result;
            _isDarkTheme = _config.IsDarkTheme;
            ApplyTheme(_isDarkTheme);
            _translationView?.SetPdfQuickSettings(_config);
            await SaveConfigAsync();
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to open settings: {ex.Message}");
        }
    }

    private async Task SaveConfigAsync()
    {
        try
        {
            _config ??= new AppConfig();
            _config.IsDarkTheme = _isDarkTheme;
            if (!string.IsNullOrWhiteSpace(_root))
                _config.TextRootPath = _root;

            await _configService.SaveAsync(_config);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to save config: {ex.Message}");
        }
    }

    private async Task ExportCurrentPairToPdfAsync()
    {
        try
        {
            if (_currentRelPath == null)
            {
                SetStatus("Select a file before exporting PDF.");
                return;
            }

            _config ??= new AppConfig { IsDarkTheme = _isDarkTheme };

            // Markdown is a view/edit representation; TEI remains primary persisted source.
            if (_translationView != null)
                _rawTranMarkdown = _translationView.GetTranslatedMarkdown() ?? "";

            List<string> chinese;
            List<string> english;
            if (_markdownService.TryExtractPdfSectionsFromMarkdown(_rawTranMarkdown, out var zhMd, out var enMd, out var mdErr))
            {
                chinese = zhMd.Select(NormalizePdfText).ToList();
                english = enMd.Select(NormalizePdfText).ToList();
            }
            else
            {
                // Fallback for malformed markdown.
                chinese = ExtractSectionsFromXml(_rawOrigXml);
                english = ExtractSectionsFromXml(_rawTranXml);
                SetStatus("PDF: markdown parse warning, using XML fallback. " + (mdErr ?? "Unknown"));
            }

            if (!_config.PdfIncludeEnglish)
            {
                english = Enumerable.Repeat(string.Empty, chinese.Count).ToList();
            }
            else
            {
                int count = Math.Max(chinese.Count, english.Count);
                if (count == 0)
                {
                    SetStatus("No text content available for PDF export.");
                    return;
                }

                while (chinese.Count < count) chinese.Add(string.Empty);
                while (english.Count < count) english.Add(string.Empty);
            }

            if (chinese.Count == 0)
            {
                SetStatus("No Chinese content available for PDF export.");
                return;
            }

            var defaultFileName = Path.GetFileNameWithoutExtension(_currentRelPath) + ".pdf";
            var pick = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Export PDF",
                SuggestedFileName = defaultFileName,
                DefaultExtension = "pdf",
                FileTypeChoices = new[]
                {
                    new FilePickerFileType("PDF Document")
                    {
                        Patterns = new[] { "*.pdf" },
                        MimeTypes = new[] { "application/pdf" }
                    }
                }
            });

            if (pick == null)
                return;

            var outputPath = pick.Path.LocalPath;
            SetStatus("Exporting PDF...");
            await SaveConfigAsync();

            var effectiveConfig = new AppConfig
            {
                TextRootPath = _config.TextRootPath,
                LastSelectedRelPath = _config.LastSelectedRelPath,
                IsDarkTheme = _config.IsDarkTheme,
                PdfLayoutMode = _config.PdfLayoutMode,
                PdfIncludeEnglish = _config.PdfIncludeEnglish,
                PdfLineSpacing = _config.PdfLineSpacing,
                PdfTrackingChinese = _config.PdfTrackingChinese,
                PdfTrackingEnglish = _config.PdfTrackingEnglish,
                PdfParagraphSpacing = _config.PdfParagraphSpacing,
                PdfAutoScaleFonts = _config.PdfAutoScaleFonts,
                PdfTargetFillRatio = _config.PdfTargetFillRatio,
                PdfMinFontSize = _config.PdfMinFontSize,
                PdfMaxFontSize = _config.PdfMaxFontSize,
                PdfLockBilingualFontSize = _config.PdfLockBilingualFontSize,
                Version = _config.Version
            };

            if (_pdfExportService.TryGeneratePdf(chinese, english, outputPath, effectiveConfig, out var error))
            {
                SetStatus("PDF exported: " + outputPath + " | DLL=" + PdfExportInteropService.NativeDllDiagnostics);
            }
            else
            {
                SetStatus("PDF export failed: " + error);
            }
        }
        catch (Exception ex)
        {
            SetStatus("PDF export failed: " + ex.Message);
        }
    }

    private static readonly Regex XmlTagStripRegex = new("<[^>]+>", RegexOptions.Compiled);
    private static readonly Regex MultiWhitespaceRegex = new(@"\s+", RegexOptions.Compiled);
    private static readonly Regex SuperscriptRegex = new(@"[\u00B9\u00B2\u00B3\u2070-\u209F]+", RegexOptions.Compiled);

    private static List<string> ExtractSectionsFromXml(string xml)
    {
        var rendered = CbetaTeiRenderer.Render(xml ?? "");
        var sections = new List<string>();

        if (rendered.Segments.Count > 0 && !string.IsNullOrEmpty(rendered.Text))
        {
            foreach (var seg in rendered.Segments)
            {
                int start = Math.Clamp(seg.Start, 0, rendered.Text.Length);
                int end = Math.Clamp(seg.EndExclusive, start, rendered.Text.Length);
                if (end <= start)
                    continue;

                var content = NormalizePdfText(rendered.Text.Substring(start, end - start));
                if (!string.IsNullOrWhiteSpace(content))
                    sections.Add(content);
            }
        }

        if (sections.Count > 0)
            return sections;

        var fallback = NormalizePdfText(WebUtility.HtmlDecode(XmlTagStripRegex.Replace(xml ?? "", " ")));
        if (!string.IsNullOrWhiteSpace(fallback))
            sections.Add(fallback);
        return sections;
    }

    private static string NormalizePdfText(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        var noMarkers = SuperscriptRegex.Replace(value, "");
        var collapsed = MultiWhitespaceRegex.Replace(noMarkers, " ");
        return collapsed.Trim();
    }

    private static string NormalizeRelForLogs(string p)
        => (p ?? "").Replace('\\', '/').TrimStart('/');
}
