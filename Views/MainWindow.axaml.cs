// Views/MainWindow.axaml.cs
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net; // WebUtility.HtmlDecode
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avalonia.Styling;

using CbetaTranslator.App.Infrastructure;
using CbetaTranslator.App.Models;
using CbetaTranslator.App.Services;
using CbetaTranslator.App.Text;

namespace CbetaTranslator.App.Views;

public partial class MainWindow : Window
{
    private const string AppTitleBase = "CBETA Translator";

    // Top bar buttons
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

    // ✅ Your XAML uses ListBox now
    private ListBox? _filesList;
    private TextBox? _navSearch;

    // New convenience controls (in your XAML)
    private CheckBox? _chkShowFilenames;
    private CheckBox? _chkZenOnly;
    private ComboBox? _cmbStatusFilter;

    // Status labels
    private TextBlock? _txtRoot;
    private TextBlock? _txtCurrentFile;
    private TextBlock? _txtStatus;

    private TabControl? _tabs;

    private ReadableTabView? _readableView;
    private TranslationTabView? _translationView;
    private SearchTabView? _searchView;
    private GitTabView? _gitView;

    // optional theme checkbox (currently commented out in your XAML)
    private CheckBox? _chkNightMode;

    private readonly IFileService _fileService = new FileService();
    private readonly AppConfigService _configService = new AppConfigService();
    private readonly PdfExportInteropService _pdfExportService = new PdfExportInteropService();
    private readonly MarkdownTranslationService _markdownService = new MarkdownTranslationService();

    private AppConfig? _config;
    private AppConfig? _cfg;

    private bool _isDarkTheme = true;

    private readonly IndexCacheService _indexCacheService = new IndexCacheService();
    private readonly SearchIndexService _navSearchIndexService = new();
    private readonly RenderedDocumentCacheService _renderCache = new RenderedDocumentCacheService(maxEntries: 48);

    private string? _root;
    private string? _originalDir;
    private string? _translatedDir;
    private string? _markdownDir;

    private List<FileNavItem> _allItems = new();
    private List<FileNavItem> _filteredItems = new();
    private readonly Dictionary<string, FileNavItem> _allItemsByRel = new(StringComparer.OrdinalIgnoreCase);

    private CancellationTokenSource? _navSearchCts;

    private string? _currentRelPath;

    private string _rawOrigXml = "";
    private string _rawTranMarkdown = "";
    private string _rawTranXml = "";
    private string _rawTranXmlReadable = "";
    private readonly Dictionary<string, int> _markdownSaveCounts = new(StringComparer.OrdinalIgnoreCase);

    private CancellationTokenSource? _renderCts;
    private bool _suppressNavSelectionChanged;

    private readonly ZenTextsService _zenTexts = new ZenTextsService();

    // ✅ UI state persistence
    private bool _suppressConfigSaves;

    // ============================================================
    // ✅ "KEEP TEXT ON TAB SWITCH" + DIRTY TRACKING (MARKDOWN)
    // ============================================================

    private string _baselineTranSha1 = "";
    private bool _dirty;

    private DispatcherTimer? _dirtyTimer;
    private string _lastSeenTranSha1 = "";

    private int _lastTabIndex = -1;
    private bool _suppressTabEvents;

    public MainWindow()
    {
        InitializeComponent();
        FindControls();
        WireEvents();
        WireChildViewEvents();

        SetStatus("Ready.");
        UpdateSaveButtonState();

        _ = LoadConfigAndApplyThemeAsync();
        StartDirtyTimer();

        Closing += async (_, e) =>
        {
            if (!await ConfirmNavigateIfDirtyAsync("close the app"))
                e.Cancel = true;
        };

        _ = TryAutoLoadRootFromConfigAsync();
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

        _topBar = this.FindControl<Border>("TopBar");
        _navPanel = this.FindControl<Border>("NavPanel");

        // ✅ Nav list is ListBox in your XAML
        _filesList = this.FindControl<ListBox>("FilesList");
        _navSearch = this.FindControl<TextBox>("NavSearch");

        // New controls
        _chkShowFilenames = this.FindControl<CheckBox>("ChkShowFilenames");
        _chkZenOnly = this.FindControl<CheckBox>("ChkZenOnly");
        _cmbStatusFilter = this.FindControl<ComboBox>("CmbStatusFilter");

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

        _chkNightMode = this.FindControl<CheckBox>("ChkNightMode");
    }

    private void WireEvents()
    {
        // Top bar buttons
        if (_btnToggleNav != null) _btnToggleNav.Click += ToggleNav_Click;

        if (_btnOpenRoot != null)
            _btnOpenRoot.Click += OpenRoot_Click;

        if (_btnSettings != null)
            _btnSettings.Click += OnSettingsClicked;

        if (_btnLicenses != null)
            _btnLicenses.Click += Licenses_Click;

        if (_btnMinimize != null)
            _btnMinimize.Click += (_, _) => WindowState = WindowState.Minimized;

        if (_btnMaximize != null)
            _btnMaximize.Click += (_, _) =>
            {
                WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            };

        if (_btnClose != null)
            _btnClose.Click += (_, _) => Close();

        if (_btnSave != null)
            _btnSave.Click += Save_Click;

        if (_btnAddCommunityNote != null)
            _btnAddCommunityNote.Click += AddCommunityNote_Click;

        // ✅ Nav selection (ListBox)
        if (_filesList != null)
            _filesList.SelectionChanged += FilesList_SelectionChanged;

        // Tabs
        if (_tabs != null)
        {
            _tabs.SelectionChanged += async (_, _) =>
            {
                if (_suppressTabEvents) return;
                await OnTabSelectionChangedAsync();
                UpdateSaveButtonState();
            };

            _lastTabIndex = _tabs.SelectedIndex;
        }

        // Nav search
        if (_navSearch != null)
            _navSearch.TextChanged += async (_, _) => await ApplyFilterAsync();

        // New toggles
        if (_chkShowFilenames != null)
            _chkShowFilenames.IsCheckedChanged += async (_, _) => await ApplyFilterAsync();

        if (_chkZenOnly != null)
            _chkZenOnly.IsCheckedChanged += async (_, _) =>
            {
                await ApplyFilterAsync();
                await SaveUiStateAsync();
            };

        if (_cmbStatusFilter != null)
            _cmbStatusFilter.SelectionChanged += async (_, _) => await ApplyFilterAsync();

        // Window chrome behavior
        if (_topBar != null)
        {
            _topBar.PointerPressed += TopBar_PointerPressed;
            _topBar.DoubleTapped += (_, _) =>
            {
                WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            };
        }
    }

    private void TopBar_PointerPressed(object? sender, PointerPressedEventArgs e)
    {
        // Ignore drag when clicking interactive controls
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

        if (_searchView != null)
        {
            _searchView.Status += (_, msg) => SetStatus(msg);
            _searchView.OpenFileRequested += async (_, rel) =>
            {
                if (!await ConfirmNavigateIfDirtyAsync($"open another file ({rel})"))
                    return;

                SelectInNav(rel);
                await LoadPairAsync(rel);

                if (_tabs != null)
                {
                    _suppressTabEvents = true;
                    try { _tabs.SelectedIndex = 0; }
                    finally { _suppressTabEvents = false; }
                }
            };
        }

        if (_gitView != null)
        {
            _gitView.Status += (_, msg) => SetStatus(msg);

            _gitView.EnsureTranslatedForSelectedRequested += async relPath =>
            {
                try { return await EnsureTranslatedXmlForRelPathAsync(relPath, saveCurrentMarkdown: true); }
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
                    if (!await ConfirmNavigateIfDirtyAsync("load a different root"))
                        return;

                    await LoadRootAsync(repoRoot, saveToConfig: true);

                    if (_tabs != null)
                    {
                        _suppressTabEvents = true;
                        try { _tabs.SelectedIndex = 0; }
                        finally { _suppressTabEvents = false; }
                    }
                }
                catch (Exception ex)
                {
                    SetStatus("Failed to load cloned repo: " + ex.Message);
                }
            };
        }

        if (_readableView != null)
        {
            _readableView.ZenFlagChanged += async (_, ev) =>
            {
                try
                {
                    if (_root == null) return;

                    await _zenTexts.SetZenAsync(_root, ev.RelPath, ev.IsZen);
                    SetStatus(ev.IsZen ? "Marked as Zen text." : "Unmarked as Zen text.");
                    await ApplyFilterAsync();
                }
                catch (Exception ex)
                {
                    SetStatus("Zen toggle failed: " + ex.Message);
                }
            };
        }
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
            if (!await ConfirmNavigateIfDirtyAsync("open a different root"))
                return;

            if (StorageProvider is null)
            {
                SetStatus("StorageProvider not available.");
                return;
            }

            var picked = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select CBETA root folder (contains xml-p5; md-p5t/xml-p5t created if missing)"
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

    private async void AddCommunityNote_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (_readableView == null)
            {
                SetStatus("Add note failed: Readable view not available.");
                return;
            }

            if (string.IsNullOrWhiteSpace(_currentRelPath))
            {
                SetStatus("Add note: select a file first.");
                return;
            }

            if (_tabs != null && _tabs.SelectedIndex != 0)
            {
                _suppressTabEvents = true;
                try { _tabs.SelectedIndex = 0; }
                finally { _suppressTabEvents = false; }
            }

            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Loaded);

            var (ok, reason) = await _readableView.TryAddCommunityNoteAtSelectionOrCaretAsync();
            SetStatus(ok ? $"Community note: added ({reason})" : $"Community note: not added ({reason})");
        }
        catch (Exception ex)
        {
            SetStatus("Add note failed: " + ex.Message);
        }
    }

    private void Save_Click(object? sender, RoutedEventArgs e) => _ = SaveTranslatedFromTabAsync();

    private async Task TryAutoLoadRootFromConfigAsync()
    {
        _config = await _configService.TryLoadAsync();
        _cfg = _config;

        if (_config?.TextRootPath is null) return;

        // Apply saved theme immediately
        _isDarkTheme = _config.IsDarkTheme;
        ApplyTheme(_isDarkTheme);
        _translationView?.SetPdfQuickSettings(_config);

        try
        {
            if (!Directory.Exists(_config.TextRootPath))
                return;

            _suppressConfigSaves = true;
            try
            {
                if (_chkZenOnly != null)
                    _chkZenOnly.IsChecked = _config.ZenOnly;
            }
            finally
            {
                _suppressConfigSaves = false;
            }

            SetStatus("Auto-loading last root…");
            await LoadRootAsync(_config.TextRootPath, saveToConfig: false);

            if (!string.IsNullOrWhiteSpace(_config.LastSelectedRelPath))
            {
                var rel = NormalizeRelForLogs(_config.LastSelectedRelPath);
                SelectInNav(rel);
                await LoadPairAsync(rel);
            }
        }
        catch
        {
            // ignore
        }
    }

    private async Task SaveUiStateAsync()
    {
        if (_suppressConfigSaves) return;

        try
        {
            if (_root == null) return;

            // ✅ Always save ONE unified config object, preserving theme + pdf settings
            _config ??= _cfg ?? new AppConfig();

            _config.TextRootPath = _root;
            _config.LastSelectedRelPath = _currentRelPath;
            _config.ZenOnly = _chkZenOnly?.IsChecked == true;

            // ✅ Preserve theme
            _config.IsDarkTheme = _isDarkTheme;

            // Keep version monotonic
            _config.Version = Math.Max(_config.Version, 3);

            _cfg = _config;

            await _configService.SaveAsync(_config);
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
        _markdownDir = AppPaths.GetMarkdownDir(_root);

        SetStatus($"origDir={_originalDir} tranDir={_translatedDir}");

        _renderCache.Clear();

        try
        {
            await _zenTexts.LoadAsync(_root);
            _searchView?.SetZenResolver(rel => _zenTexts.IsZen(rel));
        }
        catch { }

        if (_txtRoot != null) _txtRoot.Text = _root;

        if (!Directory.Exists(_originalDir))
        {
            SetStatus($"Original folder missing: {_originalDir}");
            return;
        }

        AppPaths.EnsureTranslatedDirExists(_root);
        AppPaths.EnsureMarkdownDirExists(_root);

        _gitView?.SetCurrentRepoRoot(_root);
        _searchView?.SetRootContext(_root, _originalDir, _translatedDir);

        if (saveToConfig)
        {
            try
            {
                _config ??= new AppConfig { IsDarkTheme = _isDarkTheme };
                _config.TextRootPath = _root;
                _config.ZenOnly = _chkZenOnly?.IsChecked == true;
                _config.Version = Math.Max(_config.Version, 3);
                await _configService.SaveAsync(_config);
                _cfg = _config;
            }
            catch { }
        }

        await LoadFileListFromCacheOrBuildAsync();
    }

    private async Task LoadFileListFromCacheOrBuildAsync()
    {
        if (_root == null || _originalDir == null || _translatedDir == null || _filesList == null)
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
                    if (canon != null) return (canon.DisplayShort, canon.Tooltip, canon.Status);
                    return (relKey, relKey, null);
                });

            _searchView.SetZenResolver(rel => _zenTexts.IsZen(rel));
        }

        var cache = await _indexCacheService.TryLoadAsync(_root);
        if (cache?.Entries is { Count: > 0 })
        {
            _allItems = cache.Entries;
            RebuildLookup();
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
        RebuildLookup();
        await ApplyFilterAsync();
        WireSearchTab();

        SetStatus($"Index cache created: {_allItems.Count:n0} files.");
    }

    private void RebuildLookup()
    {
        _allItemsByRel.Clear();
        foreach (var item in _allItems)
            _allItemsByRel[NormalizeRelForLogs(item.RelPath)] = item;
    }

    private async Task<HashSet<string>?> ComputeFullTextMatchesAsync(string query, CancellationToken ct)
    {
        if (_root == null || _originalDir == null || _translatedDir == null || string.IsNullOrWhiteSpace(query))
            return null;

        var manifest = await _navSearchIndexService.TryLoadAsync(_root);
        if (manifest == null)
            return null;

        // Keep current behavior: original included by default, translated optional
        bool includeOriginal = true;
        bool includeTranslated = false;

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
                               return (found?.DisplayShort ?? rel, found?.Tooltip ?? rel, found?.Status);
                           },
                           contextWidth: 40,
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

    private static bool MatchesStatusFilter(object? statusObj, int statusIdx)
    {
        // 1 = Translated (Green), 2 = Partial (Yellow), 3 = Untranslated (Red)
        if (statusIdx == 0) return true;
        if (statusObj == null) return false;

        string s = statusObj.ToString()?.ToLowerInvariant() ?? "";
        return statusIdx switch
        {
            1 => s.Contains("green") || s.Contains("translated"),
            2 => s.Contains("yellow") || s.Contains("partial") || s.Contains("partially"),
            3 => s.Contains("red") || s.Contains("untranslated"),
            _ => true
        };
    }

    private async Task ApplyFilterAsync()
    {
        if (_filesList == null)
            return;

        _navSearchCts?.Cancel();
        _navSearchCts = new CancellationTokenSource();
        var ct = _navSearchCts.Token;

        bool shouldRestoreSearchFocus = _navSearch?.IsFocused == true;

        string q = (_navSearch?.Text ?? "").Trim();
        string qLower = q.ToLowerInvariant();

        bool showFilenames = _chkShowFilenames?.IsChecked == true;
        bool zenOnly = _chkZenOnly?.IsChecked == true;
        int statusIdx = _cmbStatusFilter?.SelectedIndex ?? 0;

        string? selectedRel =
            (_filesList.SelectedItem as FileNavItem)?.RelPath
            ?? _currentRelPath;

        HashSet<string>? fullTextMatches = null;
        if (q.Length > 0)
        {
            try { fullTextMatches = await ComputeFullTextMatchesAsync(q, ct); }
            catch (OperationCanceledException) { return; }
            catch { fullTextMatches = null; }
        }

        IEnumerable<FileNavItem> seq = _allItems;

        if (zenOnly)
        {
            seq = seq.Where(it =>
                !string.IsNullOrWhiteSpace(it.RelPath) &&
                _zenTexts.IsZen(it.RelPath));
        }

        if (statusIdx != 0)
            seq = seq.Where(it => MatchesStatusFilter(it.Status, statusIdx));

        if (q.Length > 0)
        {
            seq = seq.Where(it =>
                MatchesLocalText(it, qLower) ||
                (fullTextMatches != null && fullTextMatches.Contains(NormalizeRelForLogs(it.RelPath))));
        }

        _filteredItems = seq.Select(it =>
        {
            string label =
                showFilenames
                    ? (!string.IsNullOrWhiteSpace(it.FileName) ? it.FileName : it.RelPath)
                    : (!string.IsNullOrWhiteSpace(it.DisplayShort)
                        ? it.DisplayShort
                        : (!string.IsNullOrWhiteSpace(it.FileName) ? it.FileName : it.RelPath));

            return new FileNavItem
            {
                RelPath = it.RelPath,
                FileName = it.FileName,
                DisplayShort = label,
                Tooltip = it.Tooltip,
                Status = it.Status,
            };
        }).ToList();

        try
        {
            _suppressNavSelectionChanged = true;

            _filesList.ItemsSource = _filteredItems;

            if (!string.IsNullOrWhiteSpace(selectedRel))
            {
                var match = _filteredItems.FirstOrDefault(x =>
                    string.Equals(x.RelPath, selectedRel, StringComparison.OrdinalIgnoreCase));
                if (match != null)
                    _filesList.SelectedItem = match;
            }
        }
        finally
        {
            _suppressNavSelectionChanged = false;
        }

        if (shouldRestoreSearchFocus && _navSearch != null)
            Dispatcher.UIThread.Post(() => _navSearch.Focus(), DispatcherPriority.Background);
    }

    private void SelectInNav(string relPath)
    {
        if (_filesList == null || string.IsNullOrWhiteSpace(relPath))
            return;

        var match = _filteredItems.FirstOrDefault(x =>
            string.Equals(x.RelPath, relPath, StringComparison.OrdinalIgnoreCase));

        if (match == null)
            return;

        try
        {
            _suppressNavSelectionChanged = true;
            _filesList.SelectedItem = match;
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

        _baselineTranSha1 = "";
        _lastSeenTranSha1 = "";
        _dirty = false;

        if (_txtCurrentFile != null) _txtCurrentFile.Text = "";

        _readableView?.Clear();
        _translationView?.Clear();
        _searchView?.Clear();

        if (_filesList != null)
            _filesList.ItemsSource = null;

        _readableView?.SetZenContext(null, false);

        UpdateWindowTitle();
        UpdateSaveButtonState();
        _gitView?.SetSelectedRelPath(null);
    }

    private async void FilesList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_suppressNavSelectionChanged)
            return;

        if (_filesList?.SelectedItem is not FileNavItem item)
            return;

        if (string.IsNullOrWhiteSpace(item.RelPath))
            return;

        if (_currentRelPath != null &&
            !string.Equals(_currentRelPath, item.RelPath, StringComparison.OrdinalIgnoreCase))
        {
            if (!await ConfirmNavigateIfDirtyAsync($"switch files ({_currentRelPath} → {item.RelPath})"))
            {
                var backRel = _currentRelPath;

                Dispatcher.UIThread.Post(() =>
                {
                    try
                    {
                        _suppressNavSelectionChanged = true;
                        var back = _filteredItems.FirstOrDefault(x =>
                            string.Equals(x.RelPath, backRel, StringComparison.OrdinalIgnoreCase));
                        if (back != null && _filesList != null)
                            _filesList.SelectedItem = back;
                    }
                    finally
                    {
                        _suppressNavSelectionChanged = false;
                    }
                }, DispatcherPriority.Background);

                return;
            }
        }

        await LoadPairAsync(item.RelPath);
    }

    // ============================================================
    // ✅ FIX: cache render must not read mutable fields inside Task.Run
    // ============================================================

    private async Task<(RenderedDocument ro, RenderedDocument rt)> RenderPairCachedAsync(
        string relPath,
        string origXml,
        string tranXmlReadableOrFallback,
        CancellationToken ct)
    {
        if (_originalDir == null || _translatedDir == null || _markdownDir == null)
            return (RenderedDocument.Empty, RenderedDocument.Empty);

        var origAbs = Path.Combine(_originalDir, relPath);
        var mdAbs = Path.Combine(_markdownDir, Path.ChangeExtension(relPath, ".md"));

        var stampOrig = FileStamp.FromFile(origAbs);
        var stampTran = FileStamp.FromFile(mdAbs);

        RenderedDocument ro;
        if (!_renderCache.TryGet(stampOrig, out ro))
        {
            ct.ThrowIfCancellationRequested();
            ro = CbetaTeiRenderer.Render(origXml ?? "");
            _renderCache.Put(stampOrig, ro);
        }

        RenderedDocument rt;
        if (!_renderCache.TryGet(stampTran, out rt))
        {
            ct.ThrowIfCancellationRequested();
            rt = CbetaTeiRenderer.Render(tranXmlReadableOrFallback ?? "");
            _renderCache.Put(stampTran, rt);
        }

        return (ro, rt);
    }

    private async Task LoadPairAsync(string relPath)
    {
        if (_originalDir == null || _translatedDir == null || _markdownDir == null)
            return;

        _renderCts?.Cancel();
        _renderCts = new CancellationTokenSource();
        var ct = _renderCts.Token;

        _currentRelPath = relPath;

        if (!_markdownSaveCounts.ContainsKey(relPath))
            _markdownSaveCounts[relPath] = 0;

        if (_txtCurrentFile != null)
            _txtCurrentFile.Text = relPath;

        _gitView?.SetSelectedRelPath(_currentRelPath);

        SetStatus("Loading: " + relPath);

        var swTotal = Stopwatch.StartNew();
        var swRead = Stopwatch.StartNew();

        var (orig, md) = await _fileService.ReadOriginalAndMarkdownAsync(_originalDir, _markdownDir, relPath);

        swRead.Stop();

        _rawOrigXml = orig ?? "";
        _rawTranMarkdown = md ?? "";

        // Ensure TranslationTabView knows which files it is operating on
        try
        {
            var origAbs = Path.Combine(_originalDir, relPath);
            var mdAbs = Path.Combine(_markdownDir, Path.ChangeExtension(relPath, ".md"));
            _translationView?.SetCurrentFilePaths(origAbs, mdAbs);
        }
        catch { }

        // Ensure markdown exists and is current format
        try
        {
            if (string.IsNullOrWhiteSpace(_rawTranMarkdown) || !_markdownService.IsCurrentMarkdownFormat(_rawTranMarkdown))
            {
                _rawTranMarkdown = _markdownService.ConvertTeiToMarkdown(_rawOrigXml, Path.GetFileName(relPath));
                await _fileService.WriteMarkdownAsync(_markdownDir, relPath, _rawTranMarkdown);
            }

            _rawTranXml = _markdownService.MergeMarkdownIntoTei(_rawOrigXml, _rawTranMarkdown, out _);
            _rawTranXmlReadable = _markdownService.CreateReadableInlineEnglishXml(_rawTranXml);
        }
        catch (MarkdownTranslationException ex)
        {
            SetStatus("Markdown materialization warning: " + ex.Message);
            _rawTranXml = _rawOrigXml;
            _rawTranXmlReadable = _rawOrigXml;
        }
        catch (Exception ex)
        {
            SetStatus("Materialization failed: " + ex.Message);
            _rawTranXml = _rawOrigXml;
            _rawTranXmlReadable = _rawOrigXml;
        }

        // ✅ Translation tab is MARKDOWN-backed
        _translationView?.SetXml(_rawOrigXml, _rawTranMarkdown);

        // ✅ Dirty baseline = markdown
        _baselineTranSha1 = Sha1Hex(_rawTranMarkdown ?? "");
        _lastSeenTranSha1 = _baselineTranSha1;
        _dirty = false;
        UpdateWindowTitle();

        UpdateSaveButtonState();

        SetStatus("Rendering readable view…");

        try
        {
            var swRender = Stopwatch.StartNew();

            // ✅ capture locals so Task.Run can't read mutated fields
            var origXmlLocal = _rawOrigXml ?? "";
            var tranReadableLocal = !string.IsNullOrWhiteSpace(_rawTranXmlReadable) ? _rawTranXmlReadable : (_rawTranXml ?? "");

            var renderTask = Task.Run(async () =>
            {
                ct.ThrowIfCancellationRequested();
                return await RenderPairCachedAsync(relPath, origXmlLocal, tranReadableLocal, ct);
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

            try
            {
                bool isZen = _root != null && _zenTexts.IsZen(relPath);
                _readableView?.SetZenContext(relPath, isZen);
            }
            catch { }

            await SaveUiStateAsync();
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
            if (_markdownDir == null || _currentRelPath == null)
            {
                SetStatus("Nothing to save (no file selected).");
                return;
            }

            if (_translationView == null)
            {
                SetStatus("Translation view not available.");
                return;
            }

            // 1) Get Markdown from editor
            var md = _translationView.GetTranslatedMarkdown();
            _rawTranMarkdown = md ?? "";

            // 2) Write Markdown
            await _fileService.WriteMarkdownAsync(_markdownDir, _currentRelPath, _rawTranMarkdown);

            if (_markdownSaveCounts.TryGetValue(_currentRelPath, out var count))
                _markdownSaveCounts[_currentRelPath] = count + 1;
            else
                _markdownSaveCounts[_currentRelPath] = 1;

            // 3) Materialize TEI XML from Markdown + write translated XML (for Git/other tools)
            try
            {
                _rawTranXml = _markdownService.MergeMarkdownIntoTei(_rawOrigXml, _rawTranMarkdown, out _);
                _rawTranXmlReadable = _markdownService.CreateReadableInlineEnglishXml(_rawTranXml);

                if (_translatedDir != null)
                {
                    await _fileService.WriteTranslatedAsync(_translatedDir, _currentRelPath, _rawTranXml);
                    await RefreshFileStatusAsync(_currentRelPath);
                }

                SetStatus("Saved Markdown + XML: " + Path.ChangeExtension(_currentRelPath, ".md"));
            }
            catch (MarkdownTranslationException ex)
            {
                _rawTranXml = _rawOrigXml;
                _rawTranXmlReadable = _rawOrigXml;
                SetStatus("Saved Markdown (materialization warning): " + ex.Message);
            }

            // 4) Invalidate render cache for md stamp
            try
            {
                var mdAbs = Path.Combine(_markdownDir, Path.ChangeExtension(_currentRelPath, ".md"));
                _renderCache.Invalidate(mdAbs);
            }
            catch { }

            // 5) Reset dirty baseline (markdown)
            _baselineTranSha1 = Sha1Hex(_rawTranMarkdown ?? "");
            _lastSeenTranSha1 = _baselineTranSha1;
            _dirty = false;
            UpdateWindowTitle();

            // 6) Re-render readable view
            await RefreshReadableFromRawAsync();
        }
        catch (Exception ex)
        {
            SetStatus("Save failed: " + ex.Message);
        }
    }

    private async Task RevertMarkdownFromOriginalAsync()
    {
        try
        {
            if (_markdownDir == null || _currentRelPath == null)
            {
                SetStatus("Nothing to revert (no file selected).");
                return;
            }

            int saveCount = _markdownSaveCounts.TryGetValue(_currentRelPath, out var c) ? c : 0;

            if (saveCount > 1)
            {
                var confirm = await ConfirmAsync(
                    "Revert Markdown",
                    $"This file has been saved {saveCount} times in this session.\n\nRevert Markdown to original generated state?",
                    "Revert",
                    "Cancel");

                if (!confirm)
                    return;
            }

            _rawTranMarkdown = _markdownService.ConvertTeiToMarkdown(_rawOrigXml, Path.GetFileName(_currentRelPath));
            await _fileService.WriteMarkdownAsync(_markdownDir, _currentRelPath, _rawTranMarkdown);

            try
            {
                _rawTranXml = _markdownService.MergeMarkdownIntoTei(_rawOrigXml, _rawTranMarkdown, out _);
                _rawTranXmlReadable = _markdownService.CreateReadableInlineEnglishXml(_rawTranXml);

                if (_translatedDir != null)
                {
                    await _fileService.WriteTranslatedAsync(_translatedDir, _currentRelPath, _rawTranXml);
                    await RefreshFileStatusAsync(_currentRelPath);
                }
            }
            catch (MarkdownTranslationException)
            {
                _rawTranXml = _rawOrigXml;
                _rawTranXmlReadable = _rawOrigXml;
            }

            _markdownSaveCounts[_currentRelPath] = 0;

            _translationView?.SetXml(_rawOrigXml, _rawTranMarkdown);
            await RefreshReadableFromRawAsync();

            SetBaselineFromCurrentTranslatedMarkdown();

            SetStatus("Reverted Markdown to original state.");
        }
        catch (Exception ex)
        {
            SetStatus("Revert failed: " + ex.Message);
        }
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

            var newStatus = _indexCacheService.ComputeStatusForPairLive(
                origAbs, tranAbs, _root, relKey, verboseLog: false);

            if (_allItemsByRel.TryGetValue(relKey, out var existingItem))
            {
                existingItem.Status = newStatus;
                await ApplyFilterAsync();
            }
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to refresh file status: {ex.Message}");
        }
    }

    private async Task RefreshReadableFromRawAsync()
    {
        if (_readableView == null)
            return;

        _renderCts?.Cancel();
        _renderCts = new CancellationTokenSource();
        var ct = _renderCts.Token;

        try
        {
            SetStatus("Re-rendering readable view…");

            var sw = Stopwatch.StartNew();

            // ✅ capture locals (same race fix)
            var origLocal = _rawOrigXml ?? "";
            var tranLocal = !string.IsNullOrWhiteSpace(_rawTranXmlReadable) ? _rawTranXmlReadable : (_rawTranXml ?? "");

            var renderTask = Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();
                var ro = CbetaTeiRenderer.Render(origLocal);
                var rt = CbetaTeiRenderer.Render(tranLocal);
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
        if (_btnSave != null)
        {
            bool hasFile = _currentRelPath != null;
            bool translationTabSelected = _tabs?.SelectedIndex == 1;
            _btnSave.IsEnabled = hasFile && translationTabSelected;
        }

        // keep your current behavior
        if (_btnAddCommunityNote != null)
            _btnAddCommunityNote.IsEnabled = false;
    }

    private void SetStatus(string msg)
    {
        if (_txtStatus != null)
            _txtStatus.Text = msg;
    }

    private void ApplyTheme(bool dark)
    {
        var variant = dark ? ThemeVariant.Dark : ThemeVariant.Light;

        RequestedThemeVariant = variant;

        if (Application.Current != null)
            Application.Current.RequestedThemeVariant = variant;

        var appRes = Application.Current?.Resources;
        if (appRes == null)
            return;

        string p = dark ? "Night_" : "Light_";

        void Map(string tokenKey)
        {
            var sourceKey = p + tokenKey;
            if (appRes.TryGetValue(sourceKey, out var v) && v != null)
                appRes[tokenKey] = v;
        }

        // Core tokens
        Map("AppBg");
        Map("BarBg");
        Map("NavBg");
        Map("TextFg");
        Map("TextMutedFg");
        Map("ControlBg");
        Map("BorderBrush");
        Map("BtnBg");
        Map("BtnBgHover");
        Map("BtnBgPressed");
        Map("BtnFg");
        Map("TabBg");
        Map("TabBgSelected");
        Map("TabFgSelected");
        Map("TooltipBg");
        Map("TooltipBorder");
        Map("TooltipFg");
        Map("SelectionBg");
        Map("SelectionFg");

        // Extras referenced by styles
        Map("ControlBgHover");
        Map("ControlBgFocus");

        Map("TabBgHover");
        Map("TabFg");

        Map("ComboBg");
        Map("ComboBgHover");
        Map("ComboBorder");
        Map("ComboBorderHover");

        Map("CheckBorder");
        Map("CheckBorderHover");

        Map("MenuBg");
        Map("MenuItemHoverBg");

        Map("XmlViewerBg");
        Map("XmlViewerBorder");

        Map("NavStatusGreenBg");
        Map("NavStatusYellowBg");
        Map("NavStatusRedBg");
    }

    private async Task LoadConfigAndApplyThemeAsync()
    {
        try
        {
            _config = await _configService.TryLoadAsync() ?? new AppConfig { IsDarkTheme = _isDarkTheme };
            _cfg = _config;

            _isDarkTheme = _config.IsDarkTheme;
            ApplyTheme(_isDarkTheme);
            _translationView?.SetPdfQuickSettings(_config);
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Failed to load config: {ex.Message}");
            _isDarkTheme = true;
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

    // ============================================================
    // DIRTY TRACKING IS MARKDOWN-BASED
    // ============================================================

    private void StartDirtyTimer()
    {
        _dirtyTimer ??= new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(350) };
        _dirtyTimer.Tick -= DirtyTimer_Tick;
        _dirtyTimer.Tick += DirtyTimer_Tick;
        _dirtyTimer.Start();
    }

    private void DirtyTimer_Tick(object? sender, EventArgs e)
    {
        if (_currentRelPath == null || _translationView == null)
            return;

        string cur = "";
        try { cur = _translationView.GetTranslatedMarkdown() ?? ""; }
        catch { return; }

        string sha = Sha1Hex(cur);

        if (sha == _lastSeenTranSha1)
            return;

        _lastSeenTranSha1 = sha;
        UpdateDirtyStateFromEditor(forceUi: true);
    }

    private void UpdateDirtyStateFromEditor(bool forceUi)
    {
        if (_translationView == null || _currentRelPath == null)
        {
            if (forceUi) UpdateWindowTitle();
            return;
        }

        string cur = "";
        try { cur = _translationView.GetTranslatedMarkdown() ?? ""; }
        catch { cur = ""; }

        bool dirtyNow = Sha1Hex(cur) != (_baselineTranSha1 ?? "");

        if (dirtyNow == _dirty && !forceUi)
            return;

        _dirty = dirtyNow;
        UpdateWindowTitle();
    }

    private void SetBaselineFromCurrentTranslatedMarkdown()
    {
        if (_translationView == null) return;
        string cur = "";
        try { cur = _translationView.GetTranslatedMarkdown() ?? ""; } catch { cur = ""; }
        _baselineTranSha1 = Sha1Hex(cur);
        _lastSeenTranSha1 = _baselineTranSha1;
        _dirty = false;
        UpdateWindowTitle();
    }

    private void CaptureTranslationEditsToRaw()
    {
        if (_translationView == null) return;
        try { _rawTranMarkdown = _translationView.GetTranslatedMarkdown() ?? ""; }
        catch { }
    }

    private async Task OnTabSelectionChangedAsync()
    {
        if (_tabs == null) return;

        int newIdx = _tabs.SelectedIndex;
        int oldIdx = _lastTabIndex;
        _lastTabIndex = newIdx;

        bool leavingTranslation = oldIdx == 1 && newIdx != 1;

        if (leavingTranslation)
        {
            CaptureTranslationEditsToRaw();
            UpdateDirtyStateFromEditor(forceUi: true);

            if (_dirty && IsScaryDirty(out var scaryMsg))
            {
                bool ok = await ShowYesNoAsync(
                    "Unsaved + structural problems",
                    scaryMsg + "\n\nLeave the Translation tab anyway?");

                if (!ok)
                {
                    _suppressTabEvents = true;
                    try { _tabs.SelectedIndex = 1; }
                    finally { _suppressTabEvents = false; }
                    _lastTabIndex = 1;
                    return;
                }
            }
        }

        UpdateSaveButtonState();
        UpdateDirtyStateFromEditor(forceUi: true);
    }

    private void UpdateWindowTitle()
    {
        var file = _currentRelPath ?? "";
        var star = _dirty ? "*" : "";
        Title = string.IsNullOrWhiteSpace(file)
            ? $"{AppTitleBase}{star}"
            : $"{AppTitleBase}{star} — {file}";

        if (_txtCurrentFile != null)
            _txtCurrentFile.Text = string.IsNullOrWhiteSpace(file) ? "" : (file + (_dirty ? "  *" : ""));
    }

    private async Task<bool> ConfirmNavigateIfDirtyAsync(string action)
    {
        CaptureTranslationEditsToRaw();
        UpdateDirtyStateFromEditor(forceUi: true);

        if (!_dirty)
            return true;

        if (IsScaryDirty(out var scaryMsg))
        {
            return await ShowYesNoAsync(
                "Unsaved changes + structural issues",
                $"You have unsaved changes AND the translation looks structurally broken.\n\n{scaryMsg}\n\nProceed to {action}?");
        }

        return await ShowYesNoAsync(
            "Unsaved changes",
            $"You have unsaved changes.\n\nProceed to {action}?");
    }

    private bool IsScaryDirty(out string message)
    {
        message = "";

        if (!_dirty) return false;
        if (_translationView == null) return false;

        string orig = _rawOrigXml ?? "";
        if (string.IsNullOrEmpty(orig)) return false;

        // ✅ Compare STRUCTURE using materialized XML from current markdown
        string md;
        try { md = _translationView.GetTranslatedMarkdown() ?? ""; }
        catch { return false; }

        string tranXml;
        try
        {
            tranXml = _markdownService.MergeMarkdownIntoTei(orig, md, out _);
        }
        catch
        {
            message = "Markdown materialization failed. The translation may be structurally broken.";
            return true;
        }

        var (ok, msg, _, _, _, _) = VerifyXmlHacky(orig, tranXml);
        if (ok) return false;

        message = msg;
        return true;
    }

    // ============================================================
    // Dialog helpers
    // ============================================================

    private async Task<bool> ConfirmAsync(string title, string message, string yesText, string noText)
    {
        var yes = new Button { Content = yesText, MinWidth = 90, HorizontalAlignment = HorizontalAlignment.Right };
        var no = new Button { Content = noText, MinWidth = 90 };

        var text = new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap };

        var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right };
        row.Children.Add(no);
        row.Children.Add(yes);

        var panel = new StackPanel { Margin = new Thickness(14), Spacing = 12 };
        panel.Children.Add(text);
        panel.Children.Add(row);

        var win = new Window
        {
            Title = title,
            Width = 560,
            Height = 220,
            Content = panel,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        ApplyWindowTheme(win);

        bool result = false;
        yes.Click += (_, _) => { result = true; win.Close(); };
        no.Click += (_, _) => { result = false; win.Close(); };

        await win.ShowDialog(this);
        return result;
    }

    private async Task<bool> ShowYesNoAsync(string title, string message)
    {
        var btnYes = new Button { Content = "Yes", MinWidth = 90 };
        var btnNo = new Button { Content = "No", MinWidth = 90 };

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 10
        };
        buttons.Children.Add(btnNo);
        buttons.Children.Add(btnYes);

        var text = new TextBox
        {
            Text = message,
            IsReadOnly = true,
            TextWrapping = TextWrapping.Wrap,
            AcceptsReturn = true,
            Height = 250
        };
        ScrollViewer.SetVerticalScrollBarVisibility(text, ScrollBarVisibility.Auto);
        ScrollViewer.SetHorizontalScrollBarVisibility(text, ScrollBarVisibility.Disabled);

        var panel = new StackPanel { Margin = new Thickness(16), Spacing = 10 };
        panel.Children.Add(text);
        panel.Children.Add(buttons);

        var win = new Window
        {
            Title = title,
            Width = 720,
            Height = 420,
            Content = panel,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };

        ApplyWindowTheme(win);

        var tcs = new TaskCompletionSource<bool>();
        btnYes.Click += (_, _) => { win.Close(); tcs.TrySetResult(true); };
        btnNo.Click += (_, _) => { win.Close(); tcs.TrySetResult(false); };

        await win.ShowDialog(this);
        return await tcs.Task;
    }

    private void ApplyWindowTheme(Window win)
    {
        win.RequestedThemeVariant = this.ActualThemeVariant;

        if (Application.Current?.TryGetResource("ControlBg", null, out var bg) == true && bg is IBrush b)
            win.Background = b;

        if (Application.Current?.TryGetResource("TextFg", null, out var fg) == true && fg is IBrush f)
            win.Foreground = f;
    }

    // ============================================================
    // PDF Export + XML structural verify helpers (unchanged)
    // ============================================================

    private async Task ExportCurrentPairToPdfAsync()
    {
        try
        {
            if (_currentRelPath == null)
            {
                SetStatus("Select a file before exporting PDF.");
                return;
            }

            if (StorageProvider is null)
            {
                SetStatus("StorageProvider not available.");
                return;
            }

            _config ??= new AppConfig { IsDarkTheme = _isDarkTheme };

            // Persist current editor markdown before export.
            if (_markdownDir != null && _translationView != null)
            {
                var mdNow = _translationView.GetTranslatedMarkdown();
                await _fileService.WriteMarkdownAsync(_markdownDir, _currentRelPath, mdNow);
                _rawTranMarkdown = mdNow ?? "";
            }

            List<string> chinese;
            List<string> english;
            if (_markdownService.TryExtractPdfSectionsFromMarkdown(_rawTranMarkdown, out var zhMd, out var enMd, out var mdErr))
            {
                chinese = zhMd.Select(NormalizePdfText).ToList();
                english = enMd.Select(NormalizePdfText).ToList();
            }
            else
            {
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

    private static readonly Regex XmlTagRegex = new Regex(@"<[^>]+>", RegexOptions.Compiled);

    private static readonly Regex CommunityNoteBlockRegex = new Regex(
        @"<note\b(?<attrs>[^>]*)\btype\s*=\s*""community""(?<attrs2>[^>]*)>(?<inner>[\s\S]*?)</note>",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static string StripCommunityNotes(string xml)
    {
        if (string.IsNullOrEmpty(xml)) return xml ?? string.Empty;
        return CommunityNoteBlockRegex.Replace(xml, "");
    }

    private static readonly Regex LbTagRegex = new Regex(
        @"<lb\b(?<attrs>[^>]*)\/?>",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static readonly Regex AttrRegex = new Regex(
        @"\b(?<name>n|ed)\s*=\s*""(?<val>[^""]*)""",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    private static (int totalLb, Dictionary<string, int> sigCounts) CollectLbSignatures(string xml)
    {
        int total = 0;
        var dict = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (Match m in LbTagRegex.Matches(xml))
        {
            total++;

            string attrs = m.Groups["attrs"].Value;

            string? nVal = null;
            string? edVal = null;

            foreach (Match am in AttrRegex.Matches(attrs))
            {
                var name = am.Groups["name"].Value;
                var val = am.Groups["val"].Value;

                if (name.Equals("n", StringComparison.OrdinalIgnoreCase)) nVal = val;
                else if (name.Equals("ed", StringComparison.OrdinalIgnoreCase)) edVal = val;
            }

            string sig = $"n={nVal ?? "<missing>"}|ed={edVal ?? "<missing>"}";

            if (dict.TryGetValue(sig, out int c)) dict[sig] = c + 1;
            else dict[sig] = 1;
        }

        return (total, dict);
    }

    private static (bool ok, string message, int origTags, int tranTags, int origLb, int tranLb) VerifyXmlHacky(string orig, string tran)
    {
        if (string.IsNullOrEmpty(orig))
            return (false, "Original XML is empty. Nothing to compare.", 0, 0, 0, 0);

        tran ??= "";

        string tranStripped = StripCommunityNotes(tran);

        int origTagCount = XmlTagRegex.Matches(orig).Count;
        int tranTagCount = XmlTagRegex.Matches(tran).Count;
        int tranTagCountStripped = XmlTagRegex.Matches(tranStripped).Count;

        var (origLbTotal, origSigs) = CollectLbSignatures(orig);
        var (tranLbTotal, tranSigs) = CollectLbSignatures(tranStripped);

        var missing = origSigs.Keys.Where(k => !tranSigs.ContainsKey(k)).ToList();
        var extra = tranSigs.Keys.Where(k => !origSigs.ContainsKey(k)).ToList();

        var countDiffs = new List<string>();
        foreach (var k in origSigs.Keys.Intersect(tranSigs.Keys))
        {
            int a = origSigs[k];
            int b = tranSigs[k];
            if (a != b)
                countDiffs.Add($"{k}  original={a}  translated={b}");
        }

        var problems = new List<string>();

        if (origTagCount != tranTagCountStripped)
            problems.Add(
                $"TAG COUNT MISMATCH (ignoring community notes):\n" +
                $"  original={origTagCount:n0}\n" +
                $"  translated_stripped={tranTagCountStripped:n0}\n" +
                $"  translated_raw={tranTagCount:n0}");

        if (origLbTotal != tranLbTotal)
            problems.Add($"LB TOTAL MISMATCH:\n  original={origLbTotal:n0}\n  translated={tranLbTotal:n0}");

        if (missing.Count > 0)
            problems.Add($"MISSING <lb> SIGNATURES in translated: {missing.Count:n0}\n(showing up to 15)\n- {string.Join("\n- ", missing.Take(15))}");

        if (extra.Count > 0)
            problems.Add($"EXTRA <lb> SIGNATURES in translated: {extra.Count:n0}\n(showing up to 15)\n- {string.Join("\n- ", extra.Take(15))}");

        if (countDiffs.Count > 0)
            problems.Add($"<lb> SIGNATURE COUNT DIFFERENCES: {countDiffs.Count:n0}\n(showing up to 15)\n- {string.Join("\n- ", countDiffs.Take(15))}");

        if (problems.Count == 0)
        {
            int removed = tranTagCount - tranTagCountStripped;

            string okMsg =
                $"OK ✅\n\n" +
                $"Tag count matches (ignoring community notes): {tranTagCountStripped:n0}\n" +
                $"<lb> count matches: {tranLbTotal:n0}\n" +
                $"All <lb n=... ed=...> signatures match.\n" +
                (removed > 0 ? $"\nCommunity-note tags ignored during check: {removed:n0}\n" : "\n") +
                $"(Hacky structural check only; not a full XML validator.)";

            return (true, okMsg, origTagCount, tranTagCount, origLbTotal, tranLbTotal);
        }

        return (false, string.Join("\n\n", problems), origTagCount, tranTagCount, origLbTotal, tranLbTotal);
    }

    private static string Sha1Hex(string s)
    {
        try
        {
            using var sha1 = SHA1.Create();
            var bytes = Encoding.UTF8.GetBytes(s ?? "");
            var hash = sha1.ComputeHash(bytes);
            return Convert.ToHexString(hash);
        }
        catch
        {
            return "sha1_err";
        }
    }

    // ============================================================
    // Git helper: ensure translated XML exists (unchanged logic)
    // ============================================================

    private async Task<bool> EnsureTranslatedXmlForRelPathAsync(string relPath, bool saveCurrentMarkdown)
    {
        if (_originalDir == null || _translatedDir == null || _markdownDir == null)
            return false;

        var origPath = Path.Combine(_originalDir, relPath);
        if (!File.Exists(origPath))
            return false;

        if (saveCurrentMarkdown &&
            _translationView != null &&
            string.Equals(_currentRelPath, relPath, StringComparison.OrdinalIgnoreCase))
        {
            var currentMd = _translationView.GetTranslatedMarkdown();
            await _fileService.WriteMarkdownAsync(_markdownDir, relPath, currentMd);
            _rawTranMarkdown = currentMd ?? "";
        }

        var (origXml, markdown) =
            await _fileService.ReadOriginalAndMarkdownAsync(_originalDir, _markdownDir, relPath);

        if (string.IsNullOrWhiteSpace(markdown) || !_markdownService.IsCurrentMarkdownFormat(markdown))
        {
            markdown = _markdownService.ConvertTeiToMarkdown(origXml, Path.GetFileName(relPath));
            await _fileService.WriteMarkdownAsync(_markdownDir, relPath, markdown);
        }

        try
        {
            var mergedXml = _markdownService.MergeMarkdownIntoTei(origXml, markdown, out _);
            await _fileService.WriteTranslatedAsync(_translatedDir, relPath, mergedXml);
            await RefreshFileStatusAsync(relPath);
            return true;
        }
        catch (MarkdownTranslationException ex)
        {
            SetStatus("Markdown materialization warning: " + ex.Message);
            return false;
        }
    }
}