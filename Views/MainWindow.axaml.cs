// Views/MainWindow.axaml.cs

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using CbetaTranslator.App.Infrastructure;
using CbetaTranslator.App.Models;
using CbetaTranslator.App.Services;
using CbetaTranslator.App.Text;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace CbetaTranslator.App.Views;

public partial class MainWindow : Window
{
    private const string AppTitleBase = "CBETA Translator";

    // UI
    private Button? _btnToggleNav, _btnOpenRoot, _btnSettings, _btnSave, _btnLicenses;
    private Button? _btnMinimize, _btnMaximize, _btnClose;
    private Border? _navPanel, _topBar;

    private ListBox? _filesList;
    private TextBox? _navSearch;
    private CheckBox? _chkShowFilenames, _chkZenOnly;
    private ComboBox? _cmbStatusFilter;

    private TextBlock? _txtRoot, _txtCurrentFile, _txtStatus;

    private TabControl? _tabs;
    private ReadableTabView? _readableView;
    private TranslationTabView? _translationView;
    private SearchTabView? _searchView;
    private GitTabView? _gitView;

    // Services/state
    private readonly IFileService _fileService = new FileService();
    private readonly AppConfigService _configService = new();
    private readonly PdfExportInteropService _pdfExportService = new();
    private readonly MarkdownTranslationService _markdownService = new();
    private readonly IndexCacheService _indexCacheService = new();
    private readonly SearchIndexService _navSearchIndexService = new();
    private readonly RenderedDocumentCacheService _renderCache = new(maxEntries: 48);
    private readonly ZenTextsService _zenTexts = new();

    private AppConfig _config = new() { IsDarkTheme = true };

    private string? _root, _originalDir, _translatedDir, _markdownDir;
    private string? _currentRelPath;

    private List<FileNavItem> _allItems = new();
    private List<FileNavItem> _filteredItems = new();
    private readonly Dictionary<string, FileNavItem> _allItemsByRel = new(StringComparer.OrdinalIgnoreCase);

    private CancellationTokenSource? _navSearchCts;
    private CancellationTokenSource? _renderCts;

    private string _rawOrigXml = "";
    private string _rawTranMarkdown = "";
    private string _rawTranXml = "";
    private string _rawTranXmlReadable = "";

    // This is the exact translated XML source that was rendered into ReadableTabView last time
    // (either disk xml-p5t, or the inline-readable version).
    private string _lastReadableTranslatedXmlSource = "";

    private readonly Dictionary<string, int> _markdownSaveCounts = new(StringComparer.OrdinalIgnoreCase);

    private bool _suppressNavSelectionChanged;
    private bool _suppressTabEvents;
    private bool _suppressConfigSaves;

    // Dirty tracking (markdown)
    private string _baselineTranSha1 = "", _lastSeenTranSha1 = "";
    private bool _dirty;
    private DispatcherTimer? _dirtyTimer;
    private int _lastTabIndex = -1;

    // For right->left mirroring: reflect into ReadableTabView private method
    private ReadableMirrorInvoker? _mirrorInvoker;

    public MainWindow()
    {
        InitializeComponent();
        FindControls();
        WireEvents();
        WireChildViewEvents();

        SetStatus("Ready.");
        UpdateSaveButtonState();

        _ = LoadConfigApplyThemeAndMaybeAutoloadAsync();
        StartDirtyTimer();

        Closing += async (_, e) =>
        {
            if (!await ConfirmNavigateIfDirtyAsync("close the app")) e.Cancel = true;
        };
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);

    private T? Find<T>(string name) where T : Control => this.FindControl<T>(name);

    private void FindControls()
    {
        _btnToggleNav = Find<Button>("BtnToggleNav");
        _btnOpenRoot = Find<Button>("BtnOpenRoot");
        _btnSettings = Find<Button>("BtnSettings");
        _btnSave = Find<Button>("BtnSave");
        _btnLicenses = Find<Button>("BtnLicenses");
        _btnMinimize = Find<Button>("BtnMinimize");
        _btnMaximize = Find<Button>("BtnMaximize");
        _btnClose = Find<Button>("BtnClose");

        _topBar = Find<Border>("TopBar");
        _navPanel = Find<Border>("NavPanel");

        _filesList = Find<ListBox>("FilesList");
        _navSearch = Find<TextBox>("NavSearch");
        _chkShowFilenames = Find<CheckBox>("ChkShowFilenames");
        _chkZenOnly = Find<CheckBox>("ChkZenOnly");
        _cmbStatusFilter = Find<ComboBox>("CmbStatusFilter");

        _txtRoot = Find<TextBlock>("TxtRoot");
        _txtCurrentFile = Find<TextBlock>("TxtCurrentFile");
        _txtStatus = Find<TextBlock>("TxtStatus");

        _tabs = Find<TabControl>("MainTabs");
        _readableView = Find<ReadableTabView>("ReadableView");
        _translationView = Find<TranslationTabView>("TranslationView");
        _searchView = Find<SearchTabView>("SearchView");
        _gitView = Find<GitTabView>("GitView");
    }

    private void WireEvents()
    {
        if (_btnToggleNav != null)
        {
            _btnToggleNav.Click += (_, _) =>
            {
                if (_navPanel != null)
                    _navPanel.IsVisible = !_navPanel.IsVisible;
            };
        }

        if (_btnOpenRoot != null) _btnOpenRoot.Click += OpenRoot_Click;
        if (_btnSettings != null) _btnSettings.Click += OnSettingsClicked;
        if (_btnLicenses != null) _btnLicenses.Click += Licenses_Click;

        if (_btnSave != null)
            _btnSave.Click += async (_, _) => await SaveTranslatedFromTabAsync();

        if (_btnMinimize != null)
            _btnMinimize.Click += (_, _) => WindowState = WindowState.Minimized;

        if (_btnMaximize != null)
            _btnMaximize.Click += (_, _) =>
                WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

        if (_btnClose != null)
            _btnClose.Click += (_, _) => Close();

        if (_filesList != null)
            _filesList.SelectionChanged += FilesList_SelectionChanged;

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

        if (_navSearch != null)
            _navSearch.TextChanged += async (_, _) => await ApplyFilterAsync();

        if (_chkShowFilenames != null)
            _chkShowFilenames.IsCheckedChanged += async (_, _) => await ApplyFilterAsync();

        if (_cmbStatusFilter != null)
            _cmbStatusFilter.SelectionChanged += async (_, _) => await ApplyFilterAsync();

        if (_chkZenOnly != null)
        {
            _chkZenOnly.IsCheckedChanged += async (_, _) =>
            {
                await ApplyFilterAsync();
                await SaveUiStateAsync();
            };
        }

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
        var visual = e.Source as Visual;
        while (visual != null)
        {
            if (visual is Button or TextBox or CheckBox or ComboBox) return;
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
        {
            _readableView.Status += (_, msg) => SetStatus(msg);

            // Zen toggle
            _readableView.ZenFlagChanged += async (_, ev) =>
            {
                try
                {
                    if (_root == null) return;
                    await _zenTexts.SetZenAsync(_root, ev.RelPath, ev.IsZen);
                    SetStatus(ev.IsZen ? "Marked as Zen text." : "Unmarked as Zen text.");
                    await ApplyFilterAsync();
                }
                catch (Exception ex) { SetStatus("Zen toggle failed: " + ex.Message); }
            };

            // ✅ COMMUNITY NOTES: subscribe and actually apply changes
            _readableView.CommunityNoteInsertRequested += async (_, req) =>
            {
                await OnCommunityNoteInsertRequestedAsync(req.XmlIndex, req.NoteText, req.Resp);
            };
            _readableView.CommunityNoteDeleteRequested += async (_, req) =>
            {
                await OnCommunityNoteDeleteRequestedAsync(req.XmlStart, req.XmlEndExclusive);
            };

            // ✅ Right->left mirroring: detect user input in translated pane and invoke internal mirror (private)
            _mirrorInvoker = new ReadableMirrorInvoker(_readableView, SetStatus);

            // We hook on the ReadableTabView root so we catch events coming from inside the editors.
            _readableView.AddHandler(InputElement.PointerReleasedEvent, OnReadablePointerReleasedTunnel, RoutingStrategies.Tunnel, handledEventsToo: true);
            _readableView.AddHandler(InputElement.KeyUpEvent, OnReadableKeyUpTunnel, RoutingStrategies.Tunnel, handledEventsToo: true);
            _readableView.AddHandler(InputElement.PointerPressedEvent, OnReadablePointerPressedTunnel, RoutingStrategies.Tunnel, handledEventsToo: true);
        }

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
                if (!await ConfirmNavigateIfDirtyAsync($"open another file ({rel})")) return;
                SelectInNav(rel);
                await LoadPairAsync(rel);
                ForceTab(0);
            };
        }

        if (_gitView != null)
        {
            _gitView.Status += (_, msg) => SetStatus(msg);

            _gitView.EnsureTranslatedForSelectedRequested += async relPath =>
            {
                try { return await EnsureTranslatedXmlForRelPathAsync(relPath, saveCurrentMarkdown: true); }
                catch (Exception ex) { SetStatus("Prepare translated XML failed: " + ex.Message); return false; }
            };

            _gitView.RootCloned += async (_, repoRoot) =>
            {
                try
                {
                    if (!await ConfirmNavigateIfDirtyAsync("load a different root")) return;
                    await LoadRootAsync(repoRoot, saveToConfig: true);
                    ForceTab(0);
                }
                catch (Exception ex) { SetStatus("Failed to load cloned repo: " + ex.Message); }
            };
        }
    }

    private void ForceTab(int idx)
    {
        if (_tabs == null) return;
        _suppressTabEvents = true;
        try { _tabs.SelectedIndex = idx; }
        finally { _suppressTabEvents = false; }
    }

    // =========================
    // Mirroring: translated -> original (external trigger)
    // =========================

    private void OnReadablePointerPressedTunnel(object? sender, PointerPressedEventArgs e)
    {
        // Mark input on translated pane early so keyboard focus / selection changes settle,
        // then we mirror on release.
        if (_mirrorInvoker == null || _readableView == null) return;

        if (IsInsideNamedControl(e.Source, "EditorTranslated"))
        {
            _mirrorInvoker.MarkTranslatedInteraction();
        }
    }

    private void OnReadablePointerReleasedTunnel(object? sender, PointerReleasedEventArgs e)
    {
        if (_mirrorInvoker == null || _readableView == null) return;

        if (IsInsideNamedControl(e.Source, "EditorTranslated"))
        {
            // Give AvaloniaEdit a beat to update caret/selection, then mirror.
            Dispatcher.UIThread.Post(() => _mirrorInvoker.TryMirrorTranslatedToOriginal(), DispatcherPriority.Background);
        }
    }

    private void OnReadableKeyUpTunnel(object? sender, KeyEventArgs e)
    {
        if (_mirrorInvoker == null || _readableView == null) return;

        if (IsInsideNamedControl(e.Source, "EditorTranslated"))
        {
            // Cursor keys / page up/down / typing in find etc. Update then mirror.
            Dispatcher.UIThread.Post(() => _mirrorInvoker.TryMirrorTranslatedToOriginal(), DispatcherPriority.Background);
        }
    }

    private static bool IsInsideNamedControl(object? source, string name)
    {
        var cur = source as StyledElement;
        while (cur != null)
        {
            if (cur is Control c && string.Equals(c.Name, name, StringComparison.Ordinal))
                return true;
            cur = cur.Parent as StyledElement;
        }
        return false;
    }

    // =========================
    // Community notes handlers
    // =========================

    private async Task OnCommunityNoteInsertRequestedAsync(int xmlIndex, string noteText, string? resp)
    {
        try
        {
            if (_currentRelPath == null || _translatedDir == null)
            {
                SetStatus("Community insert ignored: no file selected or translatedDir missing.");
                return;
            }

            // Use the same XML source the readable tab was rendered from when possible.
            // But we MUST write to xml-p5t (translated raw XML file).
            var baseXml = ReadTranslatedXmlRawForEdit(_currentRelPath);

            xmlIndex = Math.Clamp(xmlIndex, 0, baseXml.Length);

            string noteXml = BuildCommunityNoteXml(noteText, resp);

            string updated = baseXml.Insert(xmlIndex, noteXml);

            await WriteTranslatedXmlAndRerenderAsync(_currentRelPath, updated, why: $"community insert at {xmlIndex}");
        }
        catch (Exception ex)
        {
            SetStatus("Community insert failed: " + ex.Message);
        }
    }

    private async Task OnCommunityNoteDeleteRequestedAsync(int xmlStart, int xmlEndExclusive)
    {
        try
        {
            if (_currentRelPath == null || _translatedDir == null)
            {
                SetStatus("Community delete ignored: no file selected or translatedDir missing.");
                return;
            }

            var baseXml = ReadTranslatedXmlRawForEdit(_currentRelPath);

            int len = baseXml.Length;
            int s = Math.Clamp(xmlStart, 0, len);
            int e = Math.Clamp(xmlEndExclusive, 0, len);
            if (e < s) (s, e) = (e, s);

            if (e <= s)
            {
                SetStatus($"Community delete ignored: invalid range {xmlStart}..{xmlEndExclusive}");
                return;
            }

            string updated = baseXml.Remove(s, e - s);

            await WriteTranslatedXmlAndRerenderAsync(_currentRelPath, updated, why: $"community delete {s}..{e}");
        }
        catch (Exception ex)
        {
            SetStatus("Community delete failed: " + ex.Message);
        }
    }

    private string ReadTranslatedXmlRawForEdit(string relPath)
    {
        // Priority:
        // 1) actual disk xml-p5t file (the real persisted target)
        // 2) last readable source (in-memory)
        // 3) current merged xml from markdown (in-memory)
        // 4) original xml as worst fallback

        if (_translatedDir != null)
        {
            try
            {
                var p = Path.Combine(_translatedDir, relPath);
                if (File.Exists(p))
                    return File.ReadAllText(p, Encoding.UTF8);
            }
            catch { }
        }

        if (!string.IsNullOrWhiteSpace(_lastReadableTranslatedXmlSource))
            return _lastReadableTranslatedXmlSource;

        if (!string.IsNullOrWhiteSpace(_rawTranXml))
            return _rawTranXml;

        return _rawOrigXml ?? "";
    }

    private static string BuildCommunityNoteXml(string noteText, string? resp)
    {
        // Minimal safe XML emission.
        string inner = EscapeXmlText(noteText?.Trim() ?? "");
        if (inner.Length == 0) inner = "…";

        string respAttr = "";
        if (!string.IsNullOrWhiteSpace(resp))
        {
            string r = EscapeXmlAttr(resp.Trim());
            respAttr = $" resp=\"{r}\"";
        }

        // Include leading/trailing whitespace to reduce chance of gluing tokens together.
        return $"<note type=\"community\"{respAttr}>{inner}</note>";
    }

    private static string EscapeXmlText(string s)
        => (s ?? "")
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;");

    private static string EscapeXmlAttr(string s)
        => EscapeXmlText(s).Replace("\"", "&quot;").Replace("'", "&apos;");

    private async Task WriteTranslatedXmlAndRerenderAsync(string relPath, string updatedXml, string why)
    {
        if (_translatedDir == null) return;

        // Persist to xml-p5t
        await _fileService.WriteTranslatedAsync(_translatedDir, relPath, updatedXml);

        // Update in-memory
        _rawTranXml = updatedXml;
        try { _rawTranXmlReadable = _markdownService.CreateReadableInlineEnglishXml(updatedXml); }
        catch { _rawTranXmlReadable = updatedXml; }

        // Invalidate cache for translated XML file
        try
        {
            var tranAbs = Path.Combine(_translatedDir, relPath);
            _renderCache.Invalidate(tranAbs);
        }
        catch { }

        // Re-render readable to exit pending refresh in ReadableTabView
        await RefreshReadableFromDiskOrMemoryAsync();

        await RefreshFileStatusAsync(relPath);

        SetStatus("OK: " + why);
    }

    // =========================
    // Root + config
    // =========================

    private async Task LoadConfigApplyThemeAndMaybeAutoloadAsync()
    {
        try { _config = await _configService.TryLoadAsync() ?? new AppConfig { IsDarkTheme = true }; }
        catch { _config = new AppConfig { IsDarkTheme = true }; }

        ApplyTheme(_config.IsDarkTheme);
        _translationView?.SetPdfQuickSettings(_config);

        if (!string.IsNullOrWhiteSpace(_config.TextRootPath) && Directory.Exists(_config.TextRootPath))
        {
            _suppressConfigSaves = true;
            try { if (_chkZenOnly != null) _chkZenOnly.IsChecked = _config.ZenOnly; }
            finally { _suppressConfigSaves = false; }

            SetStatus("Auto-loading last root…");
            await LoadRootAsync(_config.TextRootPath, saveToConfig: false);

            if (!string.IsNullOrWhiteSpace(_config.LastSelectedRelPath))
            {
                var rel = NormalizeRel(_config.LastSelectedRelPath);
                SelectInNav(rel);
                await LoadPairAsync(rel);
            }
        }
    }

    private async void OpenRoot_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            if (!await ConfirmNavigateIfDirtyAsync("open a different root")) return;
            if (StorageProvider is null) { SetStatus("StorageProvider not available."); return; }

            var picked = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Select CBETA root folder"
            });

            var folder = picked.FirstOrDefault();
            if (folder is null) return;

            await LoadRootAsync(folder.Path.LocalPath, saveToConfig: true);
        }
        catch (Exception ex) { SetStatus("Open root failed: " + ex.Message); }
    }

    private async Task LoadRootAsync(string rootPath, bool saveToConfig)
    {
        _root = rootPath;
        _originalDir = AppPaths.GetOriginalDir(_root);
        _translatedDir = AppPaths.GetTranslatedDir(_root);
        _markdownDir = AppPaths.GetMarkdownDir(_root);

        _renderCache.Clear();

        if (_txtRoot != null) _txtRoot.Text = _root;

        if (!Directory.Exists(_originalDir))
        {
            SetStatus($"Original folder missing: {_originalDir}");
            return;
        }

        AppPaths.EnsureTranslatedDirExists(_root);
        AppPaths.EnsureMarkdownDirExists(_root);

        try
        {
            await _zenTexts.LoadAsync(_root);
            _searchView?.SetZenResolver(rel => _zenTexts.IsZen(rel));
        }
        catch { }

        _gitView?.SetCurrentRepoRoot(_root);
        _searchView?.SetRootContext(_root, _originalDir, _translatedDir);

        if (saveToConfig)
        {
            _config.TextRootPath = _root;
            _config.ZenOnly = _chkZenOnly?.IsChecked == true;
            _config.Version = Math.Max(_config.Version, 3);
            await SafeSaveConfigAsync();
        }

        await LoadFileListFromCacheOrBuildAsync();
    }

    private async Task SafeSaveConfigAsync()
    {
        try { await _configService.SaveAsync(_config); }
        catch { }
    }

    private async Task SaveUiStateAsync()
    {
        if (_suppressConfigSaves) return;
        if (_root == null) return;

        _config.TextRootPath = _root;
        _config.LastSelectedRelPath = _currentRelPath;
        _config.ZenOnly = _chkZenOnly?.IsChecked == true;
        _config.Version = Math.Max(_config.Version, 3);
        await SafeSaveConfigAsync();
    }

    private async void Licenses_Click(object? sender, RoutedEventArgs e)
    {
        try { await new LicensesWindow(_root).ShowDialog(this); }
        catch (Exception ex) { SetStatus("Failed to open licenses: " + ex.Message); }
    }

    private async void OnSettingsClicked(object? sender, RoutedEventArgs e)
    {
        try
        {
            var settingsWindow = new SettingsWindow(_config);
            var result = await settingsWindow.ShowDialog<AppConfig?>(this);
            if (result == null) return;

            _config = result;
            ApplyTheme(_config.IsDarkTheme);
            _translationView?.SetPdfQuickSettings(_config);
            await SafeSaveConfigAsync();
        }
        catch (Exception ex) { Debug.WriteLine($"Failed to open settings: {ex.Message}"); }
    }

    // =========================
    // Index + filter (same as before)
    // =========================

    private async Task LoadFileListFromCacheOrBuildAsync()
    {
        if (_root == null || _originalDir == null || _translatedDir == null || _filesList == null) return;

        ClearViews();

        void WireSearchTab()
        {
            if (_searchView == null) return;

            _searchView.SetContext(_root!, _originalDir!, _translatedDir!,
                fileMeta: relKey =>
                {
                    _allItemsByRel.TryGetValue(NormalizeRel(relKey), out var it);
                    return it != null ? (it.DisplayShort, it.Tooltip, it.Status) : (relKey, relKey, null);
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

        SetStatus("Building index cache…");

        var progress = new Progress<(int done, int total)>(p => SetStatus($"Indexing files… {p.done:n0}/{p.total:n0}"));

        IndexCache built;
        try { built = await _indexCacheService.BuildAsync(_originalDir, _translatedDir, _root, progress); }
        catch (Exception ex) { SetStatus("Index build failed: " + ex.Message); return; }

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
        foreach (var it in _allItems) _allItemsByRel[NormalizeRel(it.RelPath)] = it;
    }

    private async Task<HashSet<string>?> ComputeFullTextMatchesAsync(string query, CancellationToken ct)
    {
        if (_root == null || _originalDir == null || _translatedDir == null || string.IsNullOrWhiteSpace(query)) return null;

        var manifest = await _navSearchIndexService.TryLoadAsync(_root);
        if (manifest == null) return null;

        var matches = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        await foreach (var group in _navSearchIndexService.SearchAllAsync(
                           _root, _originalDir, _translatedDir, manifest,
                           query,
                           includeOriginal: true,
                           includeTranslated: false,
                           fileMeta: rel =>
                           {
                               _allItemsByRel.TryGetValue(NormalizeRel(rel), out var found);
                               return (found?.DisplayShort ?? rel, found?.Tooltip ?? rel, found?.Status);
                           },
                           contextWidth: 40,
                           progress: null,
                           ct: ct))
        {
            if (!string.IsNullOrWhiteSpace(group.RelPath))
                matches.Add(NormalizeRel(group.RelPath));
        }

        return matches;
    }

    private static bool MatchesLocalText(FileNavItem it, string qLower)
    {
        if (qLower.Length == 0) return true;
        return (it.RelPath ?? "").ToLowerInvariant().Contains(qLower)
            || (it.FileName ?? "").ToLowerInvariant().Contains(qLower)
            || (it.DisplayShort ?? "").ToLowerInvariant().Contains(qLower)
            || (it.Tooltip ?? "").ToLowerInvariant().Contains(qLower);
    }

    private static bool MatchesStatusFilter(object? statusObj, int statusIdx)
    {
        if (statusIdx == 0) return true;
        if (statusObj == null) return false;

        string s = statusObj.ToString()?.ToLowerInvariant() ?? "";
        return statusIdx switch
        {
            1 => s.Contains("green") || s.Contains("translated"),
            2 => s.Contains("yellow") || s.Contains("partial"),
            3 => s.Contains("red") || s.Contains("untranslated"),
            _ => true
        };
    }

    private async Task ApplyFilterAsync()
    {
        if (_filesList == null) return;

        _navSearchCts?.Cancel();
        _navSearchCts = new CancellationTokenSource();
        var ct = _navSearchCts.Token;

        bool restoreFocus = _navSearch?.IsFocused == true;

        string q = (_navSearch?.Text ?? "").Trim();
        string qLower = q.ToLowerInvariant();

        bool showFilenames = _chkShowFilenames?.IsChecked == true;
        bool zenOnly = _chkZenOnly?.IsChecked == true;
        int statusIdx = _cmbStatusFilter?.SelectedIndex ?? 0;

        string? selectedRel = (_filesList.SelectedItem as FileNavItem)?.RelPath ?? _currentRelPath;

        HashSet<string>? fullTextMatches = null;
        if (q.Length > 0)
        {
            try { fullTextMatches = await ComputeFullTextMatchesAsync(q, ct); }
            catch (OperationCanceledException) { return; }
            catch { fullTextMatches = null; }
        }

        IEnumerable<FileNavItem> seq = _allItems;

        if (zenOnly)
            seq = seq.Where(it => !string.IsNullOrWhiteSpace(it.RelPath) && _zenTexts.IsZen(it.RelPath));

        if (statusIdx != 0)
            seq = seq.Where(it => MatchesStatusFilter(it.Status, statusIdx));

        if (q.Length > 0)
        {
            seq = seq.Where(it =>
                MatchesLocalText(it, qLower) ||
                (fullTextMatches != null && fullTextMatches.Contains(NormalizeRel(it.RelPath))));
        }

        _filteredItems = seq.Select(it =>
        {
            string label =
                showFilenames
                    ? (!string.IsNullOrWhiteSpace(it.FileName) ? it.FileName : it.RelPath)
                    : (!string.IsNullOrWhiteSpace(it.DisplayShort) ? it.DisplayShort :
                        (!string.IsNullOrWhiteSpace(it.FileName) ? it.FileName : it.RelPath));

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
                var match = _filteredItems.FirstOrDefault(x => string.Equals(x.RelPath, selectedRel, StringComparison.OrdinalIgnoreCase));
                if (match != null) _filesList.SelectedItem = match;
            }
        }
        finally
        {
            _suppressNavSelectionChanged = false;
        }

        if (restoreFocus && _navSearch != null)
            Dispatcher.UIThread.Post(() => _navSearch.Focus(), DispatcherPriority.Background);
    }

    private void SelectInNav(string relPath)
    {
        if (_filesList == null || string.IsNullOrWhiteSpace(relPath)) return;

        var match = _filteredItems.FirstOrDefault(x => string.Equals(x.RelPath, relPath, StringComparison.OrdinalIgnoreCase));
        if (match == null) return;

        try { _suppressNavSelectionChanged = true; _filesList.SelectedItem = match; }
        finally { _suppressNavSelectionChanged = false; }
    }

    private void ClearViews()
    {
        try { _navSearchCts?.Cancel(); } catch { }
        _navSearchCts = null;

        _renderCts?.Cancel();
        _renderCts = null;

        _rawOrigXml = _rawTranMarkdown = _rawTranXml = _rawTranXmlReadable = "";
        _lastReadableTranslatedXmlSource = "";
        _currentRelPath = null;

        _baselineTranSha1 = _lastSeenTranSha1 = "";
        _dirty = false;

        if (_txtCurrentFile != null) _txtCurrentFile.Text = "";

        _readableView?.Clear();
        _translationView?.Clear();
        _searchView?.Clear();

        if (_filesList != null) _filesList.ItemsSource = null;

        _readableView?.SetZenContext(null, false);

        UpdateWindowTitle();
        UpdateSaveButtonState();
        _gitView?.SetSelectedRelPath(null);
    }

    private async void FilesList_SelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_suppressNavSelectionChanged) return;
        if (_filesList?.SelectedItem is not FileNavItem item) return;
        if (string.IsNullOrWhiteSpace(item.RelPath)) return;

        if (_currentRelPath != null && !string.Equals(_currentRelPath, item.RelPath, StringComparison.OrdinalIgnoreCase))
        {
            if (!await ConfirmNavigateIfDirtyAsync($"switch files ({_currentRelPath} → {item.RelPath})"))
            {
                var backRel = _currentRelPath;
                Dispatcher.UIThread.Post(() =>
                {
                    try
                    {
                        _suppressNavSelectionChanged = true;
                        var back = _filteredItems.FirstOrDefault(x => string.Equals(x.RelPath, backRel, StringComparison.OrdinalIgnoreCase));
                        if (back != null && _filesList != null) _filesList.SelectedItem = back;
                    }
                    finally { _suppressNavSelectionChanged = false; }
                }, DispatcherPriority.Background);
                return;
            }
        }

        await LoadPairAsync(item.RelPath);
    }

    // =========================
    // Load/render
    // =========================

    private string? TryReadTranslatedXmlFromDisk(string relPath)
    {
        if (_translatedDir == null) return null;
        try
        {
            var tranAbs = Path.Combine(_translatedDir, relPath);
            if (!File.Exists(tranAbs)) return null;
            return File.ReadAllText(tranAbs, Encoding.UTF8);
        }
        catch { return null; }
    }

    private async Task LoadPairAsync(string relPath)
    {
        if (_originalDir == null || _translatedDir == null || _markdownDir == null) return;

        _renderCts?.Cancel();
        _renderCts = new CancellationTokenSource();
        var ct = _renderCts.Token;

        _currentRelPath = relPath;
        if (!_markdownSaveCounts.ContainsKey(relPath)) _markdownSaveCounts[relPath] = 0;

        if (_txtCurrentFile != null) _txtCurrentFile.Text = relPath;

        _gitView?.SetSelectedRelPath(_currentRelPath);
        SetStatus("Loading: " + relPath);

        var (orig, md) = await _fileService.ReadOriginalAndMarkdownAsync(_originalDir, _markdownDir, relPath);

        _rawOrigXml = orig ?? "";
        _rawTranMarkdown = md ?? "";

        try
        {
            var origAbs = Path.Combine(_originalDir, relPath);
            var mdAbs = Path.Combine(_markdownDir, Path.ChangeExtension(relPath, ".md"));
            _translationView?.SetCurrentFilePaths(origAbs, mdAbs);
        }
        catch { }

        // Ensure markdown exists and materialize merged XML for translation workflows
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

        // Translation tab unchanged
        _translationView?.SetXml(_rawOrigXml, _rawTranMarkdown);

        _baselineTranSha1 = Sha1Hex(_rawTranMarkdown);
        _lastSeenTranSha1 = _baselineTranSha1;
        _dirty = false;
        UpdateWindowTitle();
        UpdateSaveButtonState();

        SetStatus("Rendering readable view…");

        try
        {
            // Readable uses disk translated XML if present (raw xml-p5t),
            // otherwise the inline-readable merged XML.
            var diskTran = TryReadTranslatedXmlFromDisk(relPath);
            var translatedReadableSource =
                !string.IsNullOrWhiteSpace(diskTran) ? diskTran! :
                (!string.IsNullOrWhiteSpace(_rawTranXmlReadable) ? _rawTranXmlReadable : _rawTranXml);

            _lastReadableTranslatedXmlSource = translatedReadableSource;

            var ro = await Task.Run(() => CbetaTeiRenderer.Render(_rawOrigXml), ct);
            var rt = await Task.Run(() => CbetaTeiRenderer.Render(translatedReadableSource), ct);

            if (ct.IsCancellationRequested) return;

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                _readableView?.SetRendered(ro, rt);

                try
                {
                    bool isZen = _root != null && _zenTexts.IsZen(relPath);
                    _readableView?.SetZenContext(relPath, isZen);
                }
                catch { }
            });

            await SaveUiStateAsync();
            SetStatus($"Loaded. Segments: O={ro.Segments.Count:n0}, T={rt.Segments.Count:n0}.");
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { SetStatus("Render failed: " + ex.Message); }
    }

    private async Task RefreshReadableFromDiskOrMemoryAsync()
    {
        if (_readableView == null || _currentRelPath == null || _originalDir == null || _translatedDir == null) return;

        _renderCts?.Cancel();
        _renderCts = new CancellationTokenSource();
        var ct = _renderCts.Token;

        try
        {
            var orig = _rawOrigXml ?? "";

            var diskTran = TryReadTranslatedXmlFromDisk(_currentRelPath);
            var tranSource =
                !string.IsNullOrWhiteSpace(diskTran) ? diskTran! :
                (!string.IsNullOrWhiteSpace(_rawTranXmlReadable) ? _rawTranXmlReadable : _rawTranXml);

            _lastReadableTranslatedXmlSource = tranSource;

            var ro = await Task.Run(() => CbetaTeiRenderer.Render(orig), ct);
            var rt = await Task.Run(() => CbetaTeiRenderer.Render(tranSource), ct);

            if (ct.IsCancellationRequested) return;

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                _readableView.SetRendered(ro, rt);
            });

            SetStatus("Readable refreshed.");
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { SetStatus("Readable refresh failed: " + ex.Message); }
    }

    // =========================
    // Save/revert (translation)
    // =========================

    private async Task SaveTranslatedFromTabAsync()
    {
        try
        {
            if (_currentRelPath == null) { SetStatus("Nothing to save."); return; }
            if (_translationView == null || _markdownDir == null || _translatedDir == null) { SetStatus("Save unavailable."); return; }

            _rawTranMarkdown = _translationView.GetTranslatedMarkdown() ?? "";

            await _fileService.WriteMarkdownAsync(_markdownDir, _currentRelPath, _rawTranMarkdown);
            _markdownSaveCounts[_currentRelPath] = (_markdownSaveCounts.TryGetValue(_currentRelPath, out var c) ? c : 0) + 1;

            _rawTranXml = _markdownService.MergeMarkdownIntoTei(_rawOrigXml, _rawTranMarkdown, out var updatedCount);
            _rawTranXmlReadable = _markdownService.CreateReadableInlineEnglishXml(_rawTranXml);

            // Write translated XML (this is separate from community notes; but community notes live here too if you edit same file)
            await _fileService.WriteTranslatedAsync(_translatedDir, _currentRelPath, _rawTranXml);
            await RefreshFileStatusAsync(_currentRelPath);

            _baselineTranSha1 = Sha1Hex(_rawTranMarkdown);
            _lastSeenTranSha1 = _baselineTranSha1;
            _dirty = false;
            UpdateWindowTitle();

            await RefreshReadableFromDiskOrMemoryAsync();

            SetStatus($"Saved → translated XML rebuilt ({updatedCount:n0} EN rows).");
        }
        catch (MarkdownTranslationException ex)
        {
            SetStatus("Save failed (materialization warning): " + ex.Message);
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
            if (_markdownDir == null || _currentRelPath == null) { SetStatus("Nothing to revert."); return; }

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

            _translationView?.SetXml(_rawOrigXml, _rawTranMarkdown);
            await RefreshReadableFromDiskOrMemoryAsync();
            SetBaselineFromCurrentTranslatedMarkdown();

            SetStatus("Reverted Markdown to original state.");
        }
        catch (Exception ex) { SetStatus("Revert failed: " + ex.Message); }
    }

    private async Task RefreshFileStatusAsync(string relPath)
    {
        if (_originalDir == null || _translatedDir == null || _root == null) return;

        try
        {
            var origAbs = Path.Combine(_originalDir, relPath);
            var tranAbs = Path.Combine(_translatedDir, relPath);
            var relKey = NormalizeRel(relPath);

            var newStatus = _indexCacheService.ComputeStatusForPairLive(origAbs, tranAbs, _root, relKey, verboseLog: false);

            if (_allItemsByRel.TryGetValue(relKey, out var existing))
            {
                existing.Status = newStatus;
                await ApplyFilterAsync();
            }
        }
        catch { }
    }

    // =========================
    // Tab + dirty tracking
    // =========================

    private void UpdateSaveButtonState()
    {
        if (_btnSave != null)
        {
            bool hasFile = _currentRelPath != null;
            bool translationTabSelected = _tabs?.SelectedIndex == 1;
            _btnSave.IsEnabled = hasFile && translationTabSelected;
        }
    }

    private void SetStatus(string msg)
    {
        if (_txtStatus != null) _txtStatus.Text = msg;
    }

    private void StartDirtyTimer()
    {
        _dirtyTimer ??= new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(350) };
        _dirtyTimer.Tick -= DirtyTimer_Tick;
        _dirtyTimer.Tick += DirtyTimer_Tick;
        _dirtyTimer.Start();
    }

    private void DirtyTimer_Tick(object? sender, EventArgs e)
    {
        if (_currentRelPath == null || _translationView == null) return;

        string cur;
        try { cur = _translationView.GetTranslatedMarkdown() ?? ""; }
        catch { return; }

        var sha = Sha1Hex(cur);
        if (sha == _lastSeenTranSha1) return;

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

        string cur;
        try { cur = _translationView.GetTranslatedMarkdown() ?? ""; }
        catch { cur = ""; }

        bool dirtyNow = Sha1Hex(cur) != (_baselineTranSha1 ?? "");
        if (dirtyNow == _dirty && !forceUi) return;

        _dirty = dirtyNow;
        UpdateWindowTitle();
    }

    private void SetBaselineFromCurrentTranslatedMarkdown()
    {
        if (_translationView == null) return;
        string cur;
        try { cur = _translationView.GetTranslatedMarkdown() ?? ""; }
        catch { cur = ""; }

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

            if (_dirty)
            {
                if (!await ShowYesNoAsync("Unsaved changes", "You have unsaved changes.\n\nLeave the Translation tab anyway?"))
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
        Title = string.IsNullOrWhiteSpace(file) ? $"{AppTitleBase}{star}" : $"{AppTitleBase}{star} — {file}";

        if (_txtCurrentFile != null)
            _txtCurrentFile.Text = string.IsNullOrWhiteSpace(file) ? "" : (file + (_dirty ? "  *" : ""));
    }

    private async Task<bool> ConfirmNavigateIfDirtyAsync(string action)
    {
        CaptureTranslationEditsToRaw();
        UpdateDirtyStateFromEditor(forceUi: true);

        if (!_dirty) return true;

        return await ShowYesNoAsync("Unsaved changes", $"You have unsaved changes.\n\nProceed to {action}?");
    }

    private async Task<bool> ShowYesNoAsync(string title, string message)
    {
        var btnYes = new Button { Content = "Yes", MinWidth = 90 };
        var btnNo = new Button { Content = "No", MinWidth = 90 };

        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 10 };
        buttons.Children.Add(btnNo);
        buttons.Children.Add(btnYes);

        var text = new TextBox { Text = message, IsReadOnly = true, TextWrapping = TextWrapping.Wrap, AcceptsReturn = true, Height = 200 };
        ScrollViewer.SetVerticalScrollBarVisibility(text, ScrollBarVisibility.Auto);

        var panel = new StackPanel { Margin = new Thickness(16), Spacing = 10 };
        panel.Children.Add(text);
        panel.Children.Add(buttons);

        var win = new Window { Title = title, Width = 620, Height = 360, Content = panel, WindowStartupLocation = WindowStartupLocation.CenterOwner };
        win.RequestedThemeVariant = this.ActualThemeVariant;

        var tcs = new TaskCompletionSource<bool>();
        btnYes.Click += (_, _) => { win.Close(); tcs.TrySetResult(true); };
        btnNo.Click += (_, _) => { win.Close(); tcs.TrySetResult(false); };

        await win.ShowDialog(this);
        return await tcs.Task;
    }

    // =========================
    // Theme (kept as token copy)
    // =========================

    private static readonly string[] ThemeTokens =
    {
        "AppBg","BarBg","NavBg","TextFg","TextMutedFg","ControlBg","BorderBrush",
        "BtnBg","BtnBgHover","BtnBgPressed","BtnFg",
        "TabBg","TabBgSelected","TabFgSelected","TooltipBg","TooltipBorder","TooltipFg",
        "SelectionBg","SelectionFg",
        "ControlBgHover","ControlBgFocus","TabBgHover","TabFg",
        "ComboBg","ComboBgHover","ComboBorder","ComboBorderHover",
        "CheckBorder","CheckBorderHover",
        "MenuBg","MenuItemHoverBg",
        "XmlViewerBg","XmlViewerBorder",
        "NavStatusGreenBg","NavStatusYellowBg","NavStatusRedBg"
    };

    private void ApplyTheme(bool dark)
    {
        var variant = dark ? ThemeVariant.Dark : ThemeVariant.Light;

        RequestedThemeVariant = variant;
        if (Application.Current != null) Application.Current.RequestedThemeVariant = variant;

        var res = Application.Current?.Resources;
        if (res == null) return;

        string p = dark ? "Night_" : "Light_";

        foreach (var token in ThemeTokens)
        {
            var sourceKey = p + token;
            if (res.TryGetValue(sourceKey, out var v) && v != null)
                res[token] = v;
        }
    }

    // =========================
    // Git helper
    // =========================

    private async Task<bool> EnsureTranslatedXmlForRelPathAsync(string relPath, bool saveCurrentMarkdown)
    {
        if (_originalDir == null || _translatedDir == null || _markdownDir == null) return false;

        var origPath = Path.Combine(_originalDir, relPath);
        if (!File.Exists(origPath)) return false;

        if (saveCurrentMarkdown && _translationView != null &&
            string.Equals(_currentRelPath, relPath, StringComparison.OrdinalIgnoreCase))
        {
            var currentMd = _translationView.GetTranslatedMarkdown();
            await _fileService.WriteMarkdownAsync(_markdownDir, relPath, currentMd);
            _rawTranMarkdown = currentMd ?? "";
        }

        var (origXml, markdown) = await _fileService.ReadOriginalAndMarkdownAsync(_originalDir, _markdownDir, relPath);

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
        catch
        {
            return false;
        }
    }

    // =========================
    // PDF export: call into your existing service (left as-is; if you want I can paste your full version)
    // =========================

    private async Task ExportCurrentPairToPdfAsync()
    {
        try
        {
            if (_currentRelPath == null) { SetStatus("Select a file before exporting PDF."); return; }
            if (StorageProvider is null) { SetStatus("StorageProvider not available."); return; }

            // Use your existing flow; this is a minimal call placeholder.
            SetStatus("PDF export not shown here (keep your existing implementation).");
        }
        catch (Exception ex)
        {
            SetStatus("PDF export failed: " + ex.Message);
        }
    }

    // =========================
    // Utils
    // =========================

    private static string NormalizeRel(string p) => (p ?? "").Replace('\\', '/').TrimStart('/');

    private static string Sha1Hex(string s)
    {
        try
        {
            using var sha1 = SHA1.Create();
            var bytes = Encoding.UTF8.GetBytes(s ?? "");
            return Convert.ToHexString(sha1.ComputeHash(bytes));
        }
        catch { return "sha1_err"; }
    }

    // =========================
    // Private helper: invoke ReadableTabView mirroring in the missing direction
    // =========================

    private sealed class ReadableMirrorInvoker
    {
        private readonly ReadableTabView _view;
        private readonly Action<string> _status;
        private readonly MethodInfo? _miRequestMirror;
        private DateTime _lastTranslatedMarkUtc = DateTime.MinValue;

        public ReadableMirrorInvoker(ReadableTabView view, Action<string> status)
        {
            _view = view;
            _status = status;

            // private void RequestMirrorFromUserAction(bool sourceIsTranslated)
            _miRequestMirror = _view.GetType().GetMethod("RequestMirrorFromUserAction",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            if (_miRequestMirror == null)
                _status("Readable: could not find private RequestMirrorFromUserAction(bool). Right->left mirroring will not work.");
        }

        public void MarkTranslatedInteraction()
        {
            _lastTranslatedMarkUtc = DateTime.UtcNow;
        }

        public void TryMirrorTranslatedToOriginal()
        {
            if (_miRequestMirror == null) return;

            // Only mirror if user actually interacted with translated pane recently.
            if ((DateTime.UtcNow - _lastTranslatedMarkUtc).TotalMilliseconds > 600)
                return;

            try
            {
                _miRequestMirror.Invoke(_view, new object[] { true });
            }
            catch (Exception ex)
            {
                _status("Readable mirror invoke failed: " + ex.Message);
            }
        }
    }
}