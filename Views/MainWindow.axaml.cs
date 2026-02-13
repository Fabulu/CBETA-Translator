// Views/MainWindow.axaml.cs
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using CbetaTranslator.App.Infrastructure;
using CbetaTranslator.App.Models;
using CbetaTranslator.App.Services;
using CbetaTranslator.App.Text;

namespace CbetaTranslator.App.Views;

public partial class MainWindow : Window
{
    private Button? _btnToggleNav;
    private Button? _btnOpenRoot;
    private Button? _btnSave;                 // optional: may not exist in XAML
    private Button? _btnLicenses;

    private Button? _btnAddCommunityNote;     // optional: may not exist in XAML

    private Border? _navPanel;
    private ListBox? _filesList;
    private TextBox? _navSearch;
    private CheckBox? _chkShowFilenames;

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
        if (_tabs != null) _tabs.SelectionChanged += (_, _) => UpdateSaveButtonState();

        if (_navSearch != null)
            _navSearch.TextChanged += (_, _) => ApplyFilter();

        if (_chkShowFilenames != null)
            _chkShowFilenames.IsCheckedChanged += (_, _) => ApplyFilter();

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
        // This kills the "first note only appears after second / after tab switch / after restart" symptom.
        if (_readableView != null)
        {
            _readableView.CommunityNoteInsertRequested += async (_, req) =>
            {
                try
                {
                    if (!EnsureFileContextForNoteOps(out var origAbs, out var tranAbs))
                        return;

                    // Let TranslationTabView do its file-backed mutation
                    await _translationView!.HandleCommunityNoteInsertAsync(req.XmlIndex, req.NoteText, req.Resp);

                    // Always re-read translated XML from disk to avoid any "editor text stale" timing issues
                    _rawTranXml = await SafeReadTextAsync(tranAbs);

                    // Keep the XML tab and internal state consistent immediately
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        _translationView!.SetXml(_rawOrigXml, _rawTranXml);
                    }, DispatcherPriority.Background);

                    // Invalidate cache for translated file so next cached renders can't resurrect old markers
                    SafeInvalidateRenderCache(tranAbs);

                    // Re-render readable from the raw strings (no disk race)
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

                    // Always re-read translated XML from disk so the UI can't lag behind
                    _rawTranXml = await SafeReadTextAsync(tranAbs);

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        _translationView!.SetXml(_rawOrigXml, _rawTranXml);
                    }, DispatcherPriority.Background);

                    SafeInvalidateRenderCache(tranAbs);

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
                SelectInNav(rel);
                await LoadPairAsync(rel);

                if (_tabs != null)
                    _tabs.SelectedIndex = 0;
            };
        }

        if (_gitView != null)
        {
            _gitView.Status += (_, msg) => SetStatus(msg);

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
                _tabs.SelectedIndex = 0;

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
            await _configService.SaveAsync(new AppConfig { TextRootPath = _root });

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

        string? selectedRel =
            (_filesList.SelectedItem as FileNavItem)?.RelPath
            ?? _currentRelPath;

        IEnumerable<FileNavItem> seq = _allItems;

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

        if (_txtCurrentFile != null) _txtCurrentFile.Text = "";

        _readableView?.Clear();
        _translationView?.Clear();
        _searchView?.Clear();

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
}
