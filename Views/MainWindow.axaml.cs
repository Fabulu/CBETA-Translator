using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
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
    // Named controls
    private Button? _btnToggleNav;
    private Button? _btnOpenRoot;
    private Button? _btnSave;

    private Border? _navPanel;
    private ListBox? _filesList;
    private TextBox? _navSearch;
    private CheckBox? _chkShowFilenames;

    private TextBlock? _txtRoot;
    private TextBlock? _txtCurrentFile;
    private TextBlock? _txtStatus;

    private TabControl? _tabs;

    // Child views
    private ReadableTabView? _readableView;
    private TranslationTabView? _translationView;

    // Services
    private readonly IFileService _fileService = new FileService();
    private readonly AppConfigService _configService = new AppConfigService();
    private readonly IndexCacheService _indexCacheService = new IndexCacheService();

    // State
    private string? _root;
    private string? _originalDir;
    private string? _translatedDir;

    // This is the canonical list loaded from cache/build — NEVER mutate items inside it.
    private List<FileNavItem> _allItems = new();

    // This is what the ListBox shows (projected items with DisplayShort depending on toggle)
    private List<FileNavItem> _filteredItems = new();

    private string? _currentRelPath;

    // Raw XML
    private string _rawOrigXml = "";
    private string _rawTranXml = "";

    // Cancel background work when user switches files quickly
    private CancellationTokenSource? _renderCts;

    public MainWindow()
    {
        InitializeComponent();
        FindControls();
        WireEvents();

        SetStatus("Ready.");
        UpdateSaveButtonState();

        _ = TryAutoLoadRootFromConfigAsync();
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private void FindControls()
    {
        _btnToggleNav = this.FindControl<Button>("BtnToggleNav");
        _btnOpenRoot = this.FindControl<Button>("BtnOpenRoot");
        _btnSave = this.FindControl<Button>("BtnSave");

        _navPanel = this.FindControl<Border>("NavPanel");
        _filesList = this.FindControl<ListBox>("FilesList");
        _navSearch = this.FindControl<TextBox>("NavSearch");
        _chkShowFilenames = this.FindControl<CheckBox>("ChkShowFilenames");

        _txtRoot = this.FindControl<TextBlock>("TxtRoot");
        _txtCurrentFile = this.FindControl<TextBlock>("TxtCurrentFile");
        _txtStatus = this.FindControl<TextBlock>("TxtStatus");

        _tabs = this.FindControl<TabControl>("MainTabs");

        _readableView = this.FindControl<ReadableTabView>("ReadableView");
        _translationView = this.FindControl<TranslationTabView>("TranslationView");

        if (_translationView != null)
        {
            _translationView.SaveRequested += async (_, _) => await SaveTranslatedFromTabAsync();
            _translationView.Status += (_, msg) => SetStatus(msg);
        }
    }

    private void WireEvents()
    {
        if (_btnToggleNav != null) _btnToggleNav.Click += ToggleNav_Click;
        if (_btnOpenRoot != null) _btnOpenRoot.Click += OpenRoot_Click;
        if (_btnSave != null) _btnSave.Click += Save_Click;

        if (_filesList != null) _filesList.SelectionChanged += FilesList_SelectionChanged;

        if (_tabs != null) _tabs.SelectionChanged += (_, _) => UpdateSaveButtonState();

        if (_navSearch != null)
            _navSearch.TextChanged += (_, _) => ApplyFilter();

        if (_chkShowFilenames != null)
            _chkShowFilenames.IsCheckedChanged += (_, _) => ApplyFilter();
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
            // ignore on startup
        }
    }

    private async Task LoadRootAsync(string rootPath, bool saveToConfig)
    {
        _root = rootPath;
        _originalDir = AppPaths.GetOriginalDir(_root);
        _translatedDir = AppPaths.GetTranslatedDir(_root);

        if (_txtRoot != null) _txtRoot.Text = _root;

        if (!Directory.Exists(_originalDir))
        {
            SetStatus($"Original folder missing: {_originalDir}");
            return;
        }

        AppPaths.EnsureTranslatedDirExists(_root);

        if (saveToConfig)
        {
            await _configService.SaveAsync(new AppConfig
            {
                TextRootPath = _root
            });
        }

        await LoadFileListFromCacheOrBuildAsync();
    }

    private async Task LoadFileListFromCacheOrBuildAsync()
    {
        if (_root == null || _originalDir == null || _translatedDir == null || _filesList == null)
            return;

        ClearViews();

        var cache = await _indexCacheService.TryLoadAsync(_root);
        if (cache != null && cache.Entries.Count > 0)
        {
            _allItems = cache.Entries ?? new List<FileNavItem>();
            ApplyFilter();
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

        SetStatus($"Index cache created: {_allItems.Count:n0} files.");
    }

    private void ApplyFilter()
    {
        if (_filesList == null)
            return;

        string q = (_navSearch?.Text ?? "").Trim();
        bool showFilenames = _chkShowFilenames?.IsChecked == true;

        // preserve selection by relpath
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

        // IMPORTANT: do not mutate the cache items.
        // Project items for UI with DisplayShort computed from toggle.
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

        // restore selection if possible
        if (!string.IsNullOrWhiteSpace(selectedRel))
        {
            var match = _filteredItems.FirstOrDefault(x =>
                string.Equals(x.RelPath, selectedRel, StringComparison.OrdinalIgnoreCase));

            if (match != null)
                _filesList.SelectedItem = match;
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

        UpdateSaveButtonState();
    }

    private async void FilesList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_filesList?.SelectedItem is not FileNavItem item)
            return;

        if (string.IsNullOrWhiteSpace(item.RelPath))
            return;

        await LoadPairAsync(item.RelPath);
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

        SetStatus("Loading: " + relPath);

        var (orig, tran) = await _fileService.ReadPairAsync(_originalDir, _translatedDir, relPath);

        _rawOrigXml = orig ?? "";
        _rawTranXml = tran ?? "";

        _translationView?.SetXml(_rawOrigXml, _rawTranXml);
        UpdateSaveButtonState();

        SetStatus("Rendering readable view…");

        try
        {
            var renderTask = Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();
                var ro = CbetaTeiRenderer.Render(_rawOrigXml);
                ct.ThrowIfCancellationRequested();
                var rt = CbetaTeiRenderer.Render(_rawTranXml);
                return (ro, rt);
            }, ct);

            var (renderOrig, renderTran) = await renderTask;

            if (ct.IsCancellationRequested)
                return;

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                _readableView?.SetRendered(renderOrig, renderTran);
                SetStatus($"Loaded. Readable segments: O={renderOrig.Segments.Count:n0}, T={renderTran.Segments.Count:n0}.");
            });
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            SetStatus("Render failed: " + ex.Message);
        }
    }

    private void Save_Click(object? sender, RoutedEventArgs e)
    {
        _ = SaveTranslatedFromTabAsync();
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

            // --- Live status refresh (red/yellow/green) ---
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

                    // best-effort persist cache
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
            catch
            {
                // ignore
            }

            _rawTranXml = xml ?? "";

            _renderCts?.Cancel();
            _renderCts = new CancellationTokenSource();
            var ct = _renderCts.Token;

            SetStatus("Re-rendering readable view…");

            var renderTask = Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();
                var ro = CbetaTeiRenderer.Render(_rawOrigXml);
                ct.ThrowIfCancellationRequested();
                var rt = CbetaTeiRenderer.Render(_rawTranXml);
                return (ro, rt);
            }, ct);

            var (renderOrig, renderTran) = await renderTask;

            if (!ct.IsCancellationRequested)
            {
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    _readableView?.SetRendered(renderOrig, renderTran);
                    SetStatus("Saved + readable view updated.");
                });
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            SetStatus("Save failed: " + ex.Message);
        }
    }

    private void UpdateSaveButtonState()
    {
        if (_btnSave == null) return;

        bool hasFile = _currentRelPath != null;
        bool translationTabSelected = _tabs?.SelectedIndex == 1;

        _btnSave.IsEnabled = hasFile && translationTabSelected;
    }

    private void SetStatus(string msg)
    {
        if (_txtStatus != null)
            _txtStatus.Text = msg;
    }

    private static string NormalizeRelForLogs(string p)
        => (p ?? "").Replace('\\', '/').TrimStart('/');
}
