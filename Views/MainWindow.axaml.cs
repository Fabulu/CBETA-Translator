// Views/MainWindow.axaml.cs
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CbetaTranslator.App.Infrastructure;
using CbetaTranslator.App.Models;
using CbetaTranslator.App.Services;
using CbetaTranslator.App.Text;

namespace CbetaTranslator.App.Views;

public partial class MainWindow : Window
{
    private const string AppTitleBase = "CBETA Translator";

    private Button? _btnToggleNav;
    private Button? _btnOpenRoot;
    private Button? _btnSave;                 // optional: may not exist in XAML
    private Button? _btnLicenses;

    private Button? _btnAddCommunityNote;     // optional: may not exist in XAML

    private Border? _navPanel;
    private ListBox? _filesList;
    private TextBox? _navSearch;
    private CheckBox? _chkShowFilenames;
    private CheckBox? _chkZenOnly;            // ✅ NEW

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
    private readonly IndexCacheService _indexCacheService = new IndexCacheService();
    private readonly RenderedDocumentCacheService _renderCache = new RenderedDocumentCacheService(maxEntries: 48);

    private string? _root;
    private string? _originalDir;
    private string? _translatedDir;

    private List<FileNavItem> _allItems = new();
    private List<FileNavItem> _filteredItems = new();

    private string? _currentRelPath;

    private string _rawOrigXml = "";
    private string _rawTranXml = "";

    private CancellationTokenSource? _renderCts;
    private bool _suppressNavSelectionChanged;

    private readonly ZenTextsService _zenTexts = new ZenTextsService();

    // ✅ Config persistence
    private AppConfig? _cfg;
    private bool _suppressConfigSaves;

    // ============================================================
    // ✅ "KEEP TEXT ON TAB SWITCH" + DIRTY TRACKING + WARNINGS
    // ============================================================

    // Baseline = last saved/loaded translated XML hash
    private string _baselineTranSha1 = "";
    private bool _dirty;

    // Periodic dirty detector (no need to change TranslationTabView)
    private DispatcherTimer? _dirtyTimer;
    private string _lastSeenTranSha1 = "";

    // Track tab transitions to capture/restore text and warn when leaving
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

        // Force night mode (your current desired state)
        ApplyTheme(dark: true);

        // Start dirty polling (cheap SHA1 on current editor text)
        StartDirtyTimer();

        Closing += async (_, e) =>
        {
            // block close if user cancels
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
        _btnSave = this.FindControl<Button>("BtnSave"); // may be null (not in XAML)
        _btnLicenses = this.FindControl<Button>("BtnLicenses");
        _btnAddCommunityNote = this.FindControl<Button>("BtnAddCommunityNote"); // may be null (not in XAML)

        // Left nav
        _navPanel = this.FindControl<Border>("NavPanel");
        _filesList = this.FindControl<ListBox>("FilesList");
        _navSearch = this.FindControl<TextBox>("NavSearch");
        _chkShowFilenames = this.FindControl<CheckBox>("ChkShowFilenames");
        _chkZenOnly = this.FindControl<CheckBox>("ChkZenOnly"); // ✅ NEW

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
        if (_btnOpenRoot != null) _btnOpenRoot.Click += OpenRoot_Click;
        if (_btnLicenses != null) _btnLicenses.Click += Licenses_Click;

        // Optional buttons: wire only if they exist in XAML
        if (_btnSave != null) _btnSave.Click += Save_Click;

        // IMPORTANT: unify add-note behavior to ONE handler
        if (_btnAddCommunityNote != null) _btnAddCommunityNote.Click += AddCommunityNote_Click;

        if (_filesList != null) _filesList.SelectionChanged += FilesList_SelectionChanged;

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

        // Search applies live
        if (_navSearch != null)
            _navSearch.TextChanged += (_, _) => ApplyFilter();

        // Show filenames applies live
        if (_chkShowFilenames != null)
            _chkShowFilenames.IsCheckedChanged += (_, _) => ApplyFilter();

        // ✅ Zen-only applies live + persist
        if (_chkZenOnly != null)
            _chkZenOnly.IsCheckedChanged += async (_, _) =>
            {
                ApplyFilter();
                await SaveUiStateAsync();
            };

        // Optional theme checkbox (if you later un-comment it in XAML)
        // if (_chkNightMode != null)
        //     _chkNightMode.IsCheckedChanged += (_, _) => ApplyTheme(dark: _chkNightMode.IsChecked == true);
    }

    private void WireChildViewEvents()
    {
        if (_readableView != null)
            _readableView.Status += (_, msg) => SetStatus(msg);

        if (_translationView != null)
        {
            _translationView.SaveRequested += async (_, _) => await SaveTranslatedFromTabAsync();
            _translationView.Status += (_, msg) => SetStatus(msg);
        }

        // ✅ CRITICAL FIX: after insert/delete, refresh from disk and force BOTH tabs to update immediately.
        if (_readableView != null)
        {
            _readableView.CommunityNoteInsertRequested += async (_, req) =>
            {
                try
                {
                    if (!EnsureFileContextForNoteOps(out var origAbs, out var tranAbs))
                        return;

                    await _translationView!.HandleCommunityNoteInsertAsync(req.XmlIndex, req.NoteText, req.Resp);

                    _rawTranXml = await SafeReadTextAsync(tranAbs);

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        _translationView!.SetXml(_rawOrigXml, _rawTranXml);
                    }, DispatcherPriority.Background);

                    SafeInvalidateRenderCache(tranAbs);

                    // ✅ Note ops always produce disk changes -> update baseline/dirty
                    SetBaselineFromCurrentTranslated();
                    UpdateDirtyStateFromEditor(forceUi: true);

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
                    if (!EnsureFileContextForNoteOps(out var origAbs, out var tranAbs))
                        return;

                    await _translationView!.HandleCommunityNoteDeleteAsync(req.XmlStart, req.XmlEndExclusive);

                    _rawTranXml = await SafeReadTextAsync(tranAbs);

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        _translationView!.SetXml(_rawOrigXml, _rawTranXml);
                    }, DispatcherPriority.Background);

                    SafeInvalidateRenderCache(tranAbs);

                    // ✅ Note ops always produce disk changes -> update baseline/dirty
                    SetBaselineFromCurrentTranslated();
                    UpdateDirtyStateFromEditor(forceUi: true);

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

                    // ✅ If Zen-only filter is active, refreshing the list makes the item appear/disappear immediately
                    ApplyFilter();
                }
                catch (Exception ex)
                {
                    SetStatus("Zen toggle failed: " + ex.Message);
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
            if (!await ConfirmNavigateIfDirtyAsync("open a different root"))
                return;

            if (StorageProvider is null)
            {
                SetStatus("StorageProvider not available.");
                return;
            }

            var picked = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select CBETA root folder (contains xml-p5; xml-p5t will be created if missing)"
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
            SetStatus("Add note: clicked.");

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

            // Ensure readable tab is active
            if (_tabs != null)
            {
                _suppressTabEvents = true;
                try { _tabs.SelectedIndex = 0; }
                finally { _suppressTabEvents = false; }
            }

            // Let layout settle
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
        _cfg = await _configService.TryLoadAsync();
        if (_cfg?.TextRootPath is null) return;

        try
        {
            if (!Directory.Exists(_cfg.TextRootPath))
                return;

            // ✅ restore ZenOnly before list loads so filtering is correct immediately
            _suppressConfigSaves = true;
            try
            {
                if (_chkZenOnly != null)
                    _chkZenOnly.IsChecked = _cfg.ZenOnly;
            }
            finally
            {
                _suppressConfigSaves = false;
            }

            SetStatus("Auto-loading last root…");
            await LoadRootAsync(_cfg.TextRootPath, saveToConfig: false);

            // ✅ auto-open last file (after root + index loaded)
            if (!string.IsNullOrWhiteSpace(_cfg.LastSelectedRelPath))
            {
                var rel = NormalizeRelForLogs(_cfg.LastSelectedRelPath);

                // best-effort select in nav (may fail if filtered out)
                SelectInNav(rel);

                // always try to load it anyway
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
        if (_suppressConfigSaves)
            return;

        try
        {
            if (_root == null)
                return;

            var cfg = _cfg ?? new AppConfig();

            cfg.TextRootPath = _root;
            cfg.LastSelectedRelPath = _currentRelPath;
            cfg.ZenOnly = _chkZenOnly?.IsChecked == true;
            cfg.Version = Math.Max(cfg.Version, 2);

            _cfg = cfg;

            await _configService.SaveAsync(cfg);
        }
        catch
        {
            // ignore (config saving must never break UX)
        }
    }

    private async Task LoadRootAsync(string rootPath, bool saveToConfig)
    {
        _root = rootPath;
        _originalDir = AppPaths.GetOriginalDir(_root);
        _translatedDir = AppPaths.GetTranslatedDir(_root);

        _renderCache.Clear();

        // ✅ load zen list early; resolver reads live from service
        try
        {
            await _zenTexts.LoadAsync(_root);

            // Make sure search tab has the resolver even before SetContext happens.
            _searchView?.SetZenResolver(rel => _zenTexts.IsZen(rel));
        }
        catch { /* ignore */ }

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
            // Preserve existing config fields if we have them
            var cfg = _cfg ?? new AppConfig();
            cfg.TextRootPath = _root;
            cfg.ZenOnly = _chkZenOnly?.IsChecked == true;
            cfg.Version = Math.Max(cfg.Version, 2);
            _cfg = cfg;

            await _configService.SaveAsync(cfg);
        }

        await LoadFileListFromCacheOrBuildAsync();
    }

    private async Task LoadFileListFromCacheOrBuildAsync()
    {
        if (_root == null || _originalDir == null || _translatedDir == null || _filesList == null)
            return;

        // ⚠ ClearViews() calls SearchTabView.Clear(), which nulls the Zen resolver.
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
                    var canon = _allItems.FirstOrDefault(x =>
                        string.Equals(
                            NormalizeRelForLogs(x.RelPath),
                            NormalizeRelForLogs(relKey),
                            StringComparison.OrdinalIgnoreCase));

                    if (canon != null)
                        return (canon.DisplayShort, canon.Tooltip, canon.Status);

                    string rel = relKey;
                    return (rel, rel, null);
                });

            // ✅ CRITICAL: re-attach Zen resolver AFTER Clear()
            _searchView.SetZenResolver(rel => _zenTexts.IsZen(rel));
        }

        var cache = await _indexCacheService.TryLoadAsync(_root);
        if (cache?.Entries is { Count: > 0 })
        {
            _allItems = cache.Entries;
            ApplyFilter();
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
        ApplyFilter();
        WireSearchTab();

        SetStatus($"Index cache created: {_allItems.Count:n0} files.");
    }

    private void ApplyFilter()
    {
        if (_filesList == null)
            return;

        string q = (_navSearch?.Text ?? "").Trim();
        bool showFilenames = _chkShowFilenames?.IsChecked == true;
        bool zenOnly = _chkZenOnly?.IsChecked == true; // ✅ NEW

        string? selectedRel =
            (_filesList.SelectedItem as FileNavItem)?.RelPath
            ?? _currentRelPath;

        IEnumerable<FileNavItem> seq = _allItems;

        // ✅ Zen-only filter first (cheap)
        if (zenOnly)
        {
            seq = seq.Where(it =>
                !string.IsNullOrWhiteSpace(it.RelPath) &&
                _zenTexts.IsZen(it.RelPath));
        }

        if (q.Length > 0)
        {
            var qLower = q.ToLowerInvariant();

            seq = seq.Where(it =>
            {
                if (!string.IsNullOrEmpty(it.RelPath) && it.RelPath.ToLowerInvariant().Contains(qLower)) return true;
                if (!string.IsNullOrEmpty(it.FileName) && it.FileName.ToLowerInvariant().Contains(qLower)) return true;
                if (!string.IsNullOrEmpty(it.DisplayShort) && it.DisplayShort.ToLowerInvariant().Contains(qLower)) return true;
                if (!string.IsNullOrEmpty(it.Tooltip) && it.Tooltip.ToLowerInvariant().Contains(qLower)) return true;
                return false;
            });
        }

        _filteredItems = seq.Select(it =>
        {
            var label =
                showFilenames
                    ? (string.IsNullOrWhiteSpace(it.FileName) ? it.RelPath : it.FileName)
                    : (string.IsNullOrWhiteSpace(it.DisplayShort)
                        ? (string.IsNullOrWhiteSpace(it.FileName) ? it.RelPath : it.FileName)
                        : it.DisplayShort);

            return new FileNavItem
            {
                RelPath = it.RelPath,
                FileName = it.FileName,
                DisplayShort = label,
                Tooltip = it.Tooltip,
                Status = it.Status
            };
        }).ToList();

        _filesList.ItemsSource = _filteredItems;

        if (!string.IsNullOrWhiteSpace(selectedRel))
        {
            var match = _filteredItems.FirstOrDefault(x =>
                string.Equals(x.RelPath, selectedRel, StringComparison.OrdinalIgnoreCase));

            if (match != null)
                _filesList.SelectedItem = match;
        }
    }

    private void SelectInNav(string relPath)
    {
        if (_filesList == null) return;
        if (string.IsNullOrWhiteSpace(relPath)) return;

        var match = _filteredItems.FirstOrDefault(x =>
            string.Equals(x.RelPath, relPath, StringComparison.OrdinalIgnoreCase));

        if (match == null)
        {
            if (_navSearch != null && !string.IsNullOrWhiteSpace(_navSearch.Text))
            {
                _navSearch.Text = "";
                ApplyFilter();

                match = _filteredItems.FirstOrDefault(x =>
                    string.Equals(x.RelPath, relPath, StringComparison.OrdinalIgnoreCase));
            }
        }

        if (match == null)
            return;

        try
        {
            _suppressNavSelectionChanged = true;
            _filesList.SelectedItem = match;
            _filesList.ScrollIntoView(match);
        }
        finally
        {
            _suppressNavSelectionChanged = false;
        }
    }

    private void ClearViews()
    {
        _renderCts?.Cancel();
        _renderCts = null;

        _rawOrigXml = "";
        _rawTranXml = "";
        _currentRelPath = null;

        // reset dirty/baseline
        _baselineTranSha1 = "";
        _lastSeenTranSha1 = "";
        _dirty = false;

        if (_txtCurrentFile != null) _txtCurrentFile.Text = "";

        _readableView?.Clear();
        _translationView?.Clear();

        // ⚠ This clears _isZen inside SearchTabView; we MUST rewire via WireSearchTab() later.
        _searchView?.Clear();

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

        // ✅ Warn on dirty before leaving current file
        if (_currentRelPath != null && !string.Equals(_currentRelPath, item.RelPath, StringComparison.OrdinalIgnoreCase))
        {
            if (!await ConfirmNavigateIfDirtyAsync($"switch files ({_currentRelPath} → {item.RelPath})"))
            {
                // revert UI selection back
                Dispatcher.UIThread.Post(() =>
                {
                    try
                    {
                        _suppressNavSelectionChanged = true;
                        var back = _filteredItems.FirstOrDefault(x =>
                            string.Equals(x.RelPath, _currentRelPath, StringComparison.OrdinalIgnoreCase));
                        if (back != null) _filesList.SelectedItem = back;
                    }
                    finally
                    {
                        _suppressNavSelectionChanged = false;
                    }
                });
                return;
            }
        }

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
            rt = CbetaTeiRenderer.Render(_rawTranXml);
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
        _rawTranXml = tran ?? "";

        // ✅ Ensure TranslationTabView knows which exact files it's operating on
        try
        {
            var origAbs = Path.Combine(_originalDir, relPath);
            var tranAbs = Path.Combine(_translatedDir, relPath);
            _translationView?.SetCurrentFilePaths(origAbs, tranAbs);
        }
        catch { }

        _translationView?.SetXml(_rawOrigXml, _rawTranXml);

        // ✅ establish baseline at load
        _baselineTranSha1 = Sha1Hex(_rawTranXml ?? "");
        _lastSeenTranSha1 = _baselineTranSha1;
        _dirty = false;
        UpdateWindowTitle();

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

            try
            {
                bool isZen = _root != null && _zenTexts.IsZen(relPath);
                _readableView?.SetZenContext(relPath, isZen);
            }
            catch { }

            // ✅ persist "where user was"
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
            if (_translatedDir == null || _currentRelPath == null)
            {
                SetStatus("Nothing to save (no file selected).");
                return;
            }

            if (_translationView == null)
            {
                SetStatus("Translation view not available.");
                return;
            }

            var xml = _translationView.GetTranslatedXml();

            await _fileService.WriteTranslatedAsync(_translatedDir, _currentRelPath, xml);
            SetStatus("Saved translated XML: " + _currentRelPath);

            try
            {
                var tranAbs = Path.Combine(_translatedDir, _currentRelPath);
                _renderCache.Invalidate(tranAbs);
            }
            catch { }

            try
            {
                if (_root != null && _originalDir != null && _translatedDir != null && _currentRelPath != null)
                {
                    var origAbs = Path.Combine(_originalDir, _currentRelPath);
                    var tranAbs = Path.Combine(_translatedDir, _currentRelPath);

                    var newStatus = _indexCacheService.ComputeStatusForPairLive(
                        origAbs,
                        tranAbs,
                        _root,
                        NormalizeRelForLogs(_currentRelPath),
                        verboseLog: true);

                    var canon = _allItems.FirstOrDefault(x =>
                        string.Equals(x.RelPath, _currentRelPath, StringComparison.OrdinalIgnoreCase));

                    if (canon != null)
                        canon.Status = newStatus;

                    ApplyFilter();

                    var cache = new IndexCache
                    {
                        Version = 2,
                        RootPath = _root,
                        BuiltUtc = DateTime.UtcNow,
                        Entries = _allItems
                    };
                    await _indexCacheService.SaveAsync(_root, cache);
                }
            }
            catch { }

            _rawTranXml = xml ?? "";

            // ✅ baseline becomes current after save
            _baselineTranSha1 = Sha1Hex(_rawTranXml ?? "");
            _lastSeenTranSha1 = _baselineTranSha1;
            _dirty = false;
            UpdateWindowTitle();

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
                var rt = CbetaTeiRenderer.Render(_rawTranXml ?? "");
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
    }

    private static string NormalizeRelForLogs(string p)
        => (p ?? "").Replace('\\', '/').TrimStart('/');

    // ============================================================
    // ✅ TAB SWITCH: keep text + warn on "scary dirty" when leaving Translation tab
    // ============================================================

    private async Task OnTabSelectionChangedAsync()
    {
        if (_tabs == null) return;

        int newIdx = _tabs.SelectedIndex;
        int oldIdx = _lastTabIndex;
        _lastTabIndex = newIdx;

        // Translation tab index is 1 in your UI logic.
        bool leavingTranslation = oldIdx == 1 && newIdx != 1;
        bool enteringTranslation = oldIdx != 1 && newIdx == 1;

        if (leavingTranslation)
        {
            // capture edits into _rawTranXml so readable re-render uses latest when you save later
            CaptureTranslationEditsToRaw();

            // warn only if dirty + structurally scary
            if (_dirty && IsScaryDirty(out var scaryMsg))
            {
                // if user cancels, bounce back to translation tab
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

        if (enteringTranslation)
        {
            // If for any reason the editor got reset, restore from _raw* cache.
            // (TranslationTabView already does caching, but this makes it bulletproof.)
            if (_translationView != null)
            {
                var current = _translationView.GetTranslatedXml() ?? "";
                if (current.Length == 0 && (_rawTranXml?.Length ?? 0) > 0)
                {
                    _translationView.SetXml(_rawOrigXml ?? "", _rawTranXml ?? "");
                }
            }
        }

        UpdateSaveButtonState();
        UpdateDirtyStateFromEditor(forceUi: true);
    }

    // ============================================================
    // ✅ DIRTY TRACKING (unsaved indicator)
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
        // Only track when a file is loaded
        if (_currentRelPath == null || _translationView == null)
            return;

        // Light touch: SHA1 of current editor text
        string cur = "";
        try { cur = _translationView.GetTranslatedXml() ?? ""; }
        catch { return; }

        string sha = Sha1Hex(cur);

        // Avoid doing UI work if nothing changed since last tick
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
        try { cur = _translationView.GetTranslatedXml() ?? ""; }
        catch { cur = ""; }

        bool dirtyNow = Sha1Hex(cur) != (_baselineTranSha1 ?? "");

        if (dirtyNow == _dirty && !forceUi)
            return;

        _dirty = dirtyNow;
        UpdateWindowTitle();
    }

    private void SetBaselineFromCurrentTranslated()
    {
        if (_translationView == null) return;
        string cur = "";
        try { cur = _translationView.GetTranslatedXml() ?? ""; } catch { cur = ""; }
        _baselineTranSha1 = Sha1Hex(cur);
        _lastSeenTranSha1 = _baselineTranSha1;
        _dirty = false;
        UpdateWindowTitle();
    }

    private void CaptureTranslationEditsToRaw()
    {
        if (_translationView == null) return;
        try { _rawTranXml = _translationView.GetTranslatedXml() ?? ""; }
        catch { }
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

    // ============================================================
    // ✅ WARNINGS: "scary dirt problems"
    // - We interpret "scary" as: dirty + hacky structure check FAILS
    //   (tag-count mismatch ignoring community notes, or lb mismatch/signature mismatch)
    // ============================================================

    private async Task<bool> ConfirmNavigateIfDirtyAsync(string action)
    {
        // Always capture latest editor contents before deciding
        CaptureTranslationEditsToRaw();
        UpdateDirtyStateFromEditor(forceUi: true);

        if (!_dirty)
            return true;

        // If it's scary, show scary message; else simple unsaved prompt.
        if (IsScaryDirty(out var scaryMsg))
        {
            return await ShowYesNoAsync(
                "Unsaved changes + structural issues",
                $"You have unsaved changes AND the translation looks structurally broken.\n\n{scaryMsg}\n\n" +
                $"Proceed to {action}? (Choosing Yes may lose edits if you later reload)");
        }

        return await ShowYesNoAsync(
            "Unsaved changes",
            $"You have unsaved changes.\n\nProceed to {action}?");
    }

    private bool IsScaryDirty(out string message)
    {
        message = "";

        if (!_dirty) return false;

        // We need original + current translated (from editor, not _rawTranXml snapshot)
        if (_translationView == null) return false;

        string orig = _rawOrigXml ?? "";
        string tran = "";
        try { tran = _translationView.GetTranslatedXml() ?? ""; }
        catch { return false; }

        if (string.IsNullOrEmpty(orig))
            return false;

        var (ok, msg, _, _, _, _) = VerifyXmlHacky(orig, tran);

        if (ok)
            return false;

        message = msg;
        return true;
    }

    // ============================================================
    // ✅ POPUPS (no extra XAML needed)
    // ============================================================

    private async Task<bool> ShowYesNoAsync(string title, string message)
    {
        var owner = this;

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

        var panel = new StackPanel
        {
            Margin = new Thickness(16),
            Spacing = 10
        };
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

        var tcs = new TaskCompletionSource<bool>();

        btnYes.Click += (_, _) => { win.Close(); tcs.TrySetResult(true); };
        btnNo.Click += (_, _) => { win.Close(); tcs.TrySetResult(false); };

        await win.ShowDialog(owner);
        return await tcs.Task;
    }

    // ============================================================
    // ✅ HACKY STRUCTURE CHECK (copied from TranslationTabView, minimal)
    // ============================================================

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

    // ============================================================
    // ✅ SHA1 helper
    // ============================================================

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
}
