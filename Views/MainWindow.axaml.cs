// Views/MainWindow.axaml.cs
//
// INDEXED PROJECTION MainWindow
// - Readable tab renders disk TEI only (original + translated).
// - Translation tab edits KEY/ZH/EN projection via IndexedTranslationService.
// - Save applies projection edits back into translated TEI (managed EN community notes).
// - Community notes are inserted/deleted directly in translated XML on disk.
// - If translated XML is missing, it is created from original XML.

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
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

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
    private readonly IndexCacheService _indexCacheService = new();
    private readonly RenderedDocumentCacheService _renderCache = new(maxEntries: 48);
    private readonly ZenTextsService _zenTexts = new();
    private readonly IndexedTranslationService _indexedTranslation = new();

    private IndexedTranslationDocument? _indexedDoc;
    private TranslationEditMode _translationMode = TranslationEditMode.Body;

    private AppConfig _config = new() { IsDarkTheme = true };

    private string? _root, _originalDir, _translatedDir;
    private string? _currentRelPath;

    private List<FileNavItem> _allItems = new();
    private List<FileNavItem> _filteredItems = new();
    private readonly Dictionary<string, FileNavItem> _allItemsByRel = new(StringComparer.OrdinalIgnoreCase);

    private CancellationTokenSource? _navSearchCts;
    private CancellationTokenSource? _renderCts;

    // Nav filter performance / race control
    private DispatcherTimer? _navFilterDebounce;
    private int _navFilterVersion;

    private DispatcherTimer? _indexCacheSaveDebounce;
    private bool _indexCacheDirty;

    private string _rawOrigXml = "";
    private string _rawTranXml = "";

    private bool _suppressNavSelectionChanged;
    private bool _suppressTabEvents;
    private bool _suppressConfigSaves;

    // Dirty tracking (projection text hash)
    private string _baselineTranSha1 = "", _lastSeenTranSha1 = "";
    private bool _dirty;
    private DispatcherTimer? _dirtyTimer;
    private int _lastTabIndex = -1;

    // Translation assistand
    private readonly TranslationAssistantService _translationAssistant = new();
    private CancellationTokenSource? _assistantCts;

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
                if (_navPanel != null) _navPanel.IsVisible = !_navPanel.IsVisible;
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
            _navSearch.TextChanged += (_, _) => ScheduleApplyFilter(debounce: true);

        if (_chkShowFilenames != null)
            _chkShowFilenames.IsCheckedChanged += (_, _) => ScheduleApplyFilter(debounce: false);

        if (_cmbStatusFilter != null)
            _cmbStatusFilter.SelectionChanged += (_, _) => ScheduleApplyFilter(debounce: false);

        if (_chkZenOnly != null)
        {
            _chkZenOnly.IsCheckedChanged += async (_, _) =>
            {
                ScheduleApplyFilter(debounce: false);
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
            if (visual is Button || visual is TextBox || visual is CheckBox || visual is ComboBox) return;
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

            _readableView.ZenFlagChanged += async (_, ev) =>
            {
                try
                {
                    if (_root == null) return;
                    await _zenTexts.SetZenAsync(_root, ev.RelPath, ev.IsZen);
                    SetStatus(ev.IsZen ? "Marked as Zen text." : "Unmarked as Zen text.");
                    await ApplyFilterSafeAsync();
                }
                catch (Exception ex)
                {
                    SetStatus("Zen toggle failed: " + ex.Message);
                }
            };

            _readableView.CommunityNoteInsertRequested += async (_, req) =>
            {
                await OnCommunityNoteInsertRequestedAsync(req.XmlIndex, req.NoteText, req.Resp);
            };

            _readableView.CommunityNoteDeleteRequested += async (_, req) =>
            {
                await OnCommunityNoteDeleteRequestedAsync(req.XmlStart, req.XmlEndExclusive);
            };

            // NEW: move existing note/footnote by cloning its XML span and removing the old span
            _readableView.FootnoteMoveRequested += async (_, req) =>
            {
                await OnFootnoteMoveRequestedAsync(req);
            };

            _translationView.CurrentSegmentChanged += async (_, ev) =>
            {
                await RefreshAssistantForCurrentSegmentAsync(ev);
            };
        }

        if (_translationView != null)
        {
            _translationView.SaveRequested += async (_, _) => await SaveTranslatedFromTabAsync();
            _translationView.RevertRequested += async (_, _) => await RevertTranslatedXmlFromDiskAsync();
            _translationView.Status += (_, msg) => SetStatus(msg);

            _translationView.ModeChanged += (_, mode) =>
            {
                try
                {
                    if (_indexedDoc == null) return;

                    var currentProjection = GetTranslationProjectionText();
                    _indexedTranslation.ApplyProjectionEdits(_indexedDoc, _translationMode, currentProjection);

                    _translationMode = mode;

                    var nextProjection = _indexedTranslation.RenderProjection(_indexedDoc, _translationMode);
                    SetTranslationProjection(_translationMode, nextProjection);

                    SetBaselineFromCurrentTranslatedEditorText();
                    SetStatus("Translation mode: " + _translationMode);
                }
                catch (Exception ex)
                {
                    SetStatus("Mode switch failed: " + ex.Message);
                }
            };
        }

        if (_searchView != null)
        {
            _searchView.Status += (_, msg) => SetStatus(msg);
            _searchView.OpenFileRequested += async (_, rel) =>
            {
                if (!await ConfirmNavigateIfDirtyAsync("open another file (" + rel + ")")) return;
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
                try { return await EnsureTranslatedXmlForRelPathAsync(relPath, saveCurrentEditor: true); }
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
                catch (Exception ex)
                {
                    SetStatus("Failed to load cloned repo: " + ex.Message);
                }
            };
        }
    }

    private async Task RefreshAssistantForCurrentSegmentAsync(TranslationTabView.CurrentProjectionSegmentChangedEventArgs ev)
    {
        try
        {
            if (_translationView == null) return;
            if (_currentRelPath == null) return;

            try { _assistantCts?.Cancel(); } catch { }
            try { _assistantCts?.Dispose(); } catch { }
            _assistantCts = new CancellationTokenSource();
            var ct = _assistantCts.Token;

            var ctx = new CurrentSegmentContext
            {
                RelPath = _currentRelPath,
                BlockNumber = ev.BlockNumber,
                ZhText = ev.Zh,
                EnText = ev.En,
                ProjectionOffsetStart = ev.BlockStartOffset,
                ProjectionOffsetEndExclusive = ev.BlockEndOffsetExclusive,
                Mode = ev.Mode
            };

            var snapshot = await _translationAssistant.BuildSnapshotAsync(
                ctx,
                _root,
                _originalDir,
                _translatedDir,
                ct);

            if (ct.IsCancellationRequested) return;

            _translationView.SetAssistantSnapshot(snapshot);
        }
        catch
        {
            // assistant errors must never break translation
        }
    }
    private async Task OnFootnoteMoveRequestedAsync(ReadableTabView.MoveFootnoteRequest req)
    {
        try
        {
            if (_currentRelPath == null || _translatedDir == null)
            {
                SetStatus("Move footnote ignored: no file selected.");
                return;
            }

            await EnsureTranslatedXmlExistsForCurrentAsync();

            var tranAbs = Path.Combine(_translatedDir, _currentRelPath);
            var baseXml = ReadAllTextUtf8Strict(tranAbs);

            int len = baseXml.Length;

            int oldS = Math.Clamp(req.OldXmlStart, 0, len);
            int oldE = Math.Clamp(req.OldXmlEndExclusive, 0, len);
            if (oldE < oldS) (oldS, oldE) = (oldE, oldS);

            if (oldE <= oldS)
            {
                SetStatus($"Move footnote ignored: invalid old span {req.OldXmlStart}..{req.OldXmlEndExclusive}");
                return;
            }

            int newIndex = Math.Clamp(req.NewXmlIndex, 0, len);

            // If the click maps INSIDE the old <note> span, that’s fine.
            // We *interpret* it as “insert at the note’s anchor point”, i.e. the start of the element.
            // (Inserting into note markup is never desirable.)
            if (newIndex >= oldS && newIndex <= oldE)
                newIndex = oldS;

            // Extract original note XML snippet
            string noteXml = baseXml.Substring(oldS, oldE - oldS);

            // Sanity check: avoid deleting arbitrary XML if spans ever go wrong
            if (!LooksLikeNoteElement(noteXml))
            {
                SetStatus("Move footnote refused: source span does not look like a <note> element.");
                return;
            }

            // Remove old note first
            string withoutOld = baseXml.Remove(oldS, oldE - oldS);

            // Adjust insertion index AFTER the removal
            int removedLen = oldE - oldS;
            int adjustedNewIndex =
                (newIndex <= oldS)
                    ? newIndex
                    : newIndex - removedLen;

            adjustedNewIndex = Math.Clamp(adjustedNewIndex, 0, withoutOld.Length);

            // Insert the cloned note element at the new location
            string updated = withoutOld.Insert(adjustedNewIndex, noteXml);

            await WriteTranslatedDiskAndRerenderAsync(
                _currentRelPath,
                updated,
                $"moved footnote {oldS}..{oldE} -> {adjustedNewIndex}"
            );
        }
        catch (Exception ex)
        {
            SetStatus("Move footnote failed: " + ex.Message);
        }
    }
    private static bool LooksLikeNoteElement(string xmlSnippet)
    {
        if (string.IsNullOrWhiteSpace(xmlSnippet)) return false;

        var s = xmlSnippet.TrimStart();

        // Allow whitespace before <note ...>
        if (!s.StartsWith("<note", StringComparison.OrdinalIgnoreCase))
            return false;

        // Should contain a closing tag (renderer captures <note ...>...</note>)
        if (s.IndexOf("</note>", StringComparison.OrdinalIgnoreCase) < 0)
            return false;

        return true;
    }
    private void ForceTab(int idx)
    {
        if (_tabs == null) return;
        _suppressTabEvents = true;
        try { _tabs.SelectedIndex = idx; }
        finally { _suppressTabEvents = false; }
    }

    // =========================
    // Community notes handlers (DISK-TEI ONLY)
    // =========================

    private async Task OnCommunityNoteInsertRequestedAsync(int xmlIndex, string noteText, string? resp)
    {
        try
        {
            if (_currentRelPath == null || _translatedDir == null)
            {
                SetStatus("Community insert ignored: no file selected.");
                return;
            }

            await EnsureTranslatedXmlExistsForCurrentAsync();

            var tranAbs = Path.Combine(_translatedDir, _currentRelPath);
            var baseXml = ReadAllTextUtf8Strict(tranAbs);

            int insertAt = Math.Clamp(xmlIndex, 0, baseXml.Length);
            string noteXml = BuildCommunityNoteXml(noteText, resp);
            string updated = baseXml.Insert(insertAt, noteXml);

            await WriteTranslatedDiskAndRerenderAsync(_currentRelPath, updated, "community insert at " + insertAt);
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
                SetStatus("Community delete ignored: no file selected.");
                return;
            }

            await EnsureTranslatedXmlExistsForCurrentAsync();

            var tranAbs = Path.Combine(_translatedDir, _currentRelPath);
            var baseXml = ReadAllTextUtf8Strict(tranAbs);

            int s = Math.Clamp(xmlStart, 0, baseXml.Length);
            int e = Math.Clamp(xmlEndExclusive, 0, baseXml.Length);
            if (e < s) (s, e) = (e, s);

            if (e <= s)
            {
                SetStatus("Community delete ignored: invalid range " + xmlStart + ".." + xmlEndExclusive);
                return;
            }

            string updated = baseXml.Remove(s, e - s);
            await WriteTranslatedDiskAndRerenderAsync(_currentRelPath, updated, "community delete " + s + ".." + e);
        }
        catch (Exception ex)
        {
            SetStatus("Community delete failed: " + ex.Message);
        }
    }

    private static string BuildCommunityNoteXml(string noteText, string? resp)
    {
        string inner = EscapeXmlText((noteText ?? "").Trim());
        if (inner.Length == 0) inner = "…";

        string respAttr = "";
        if (!string.IsNullOrWhiteSpace(resp))
        {
            respAttr = " resp=\"" + EscapeXmlAttr(resp.Trim()) + "\"";
        }

        return "<note type=\"community\"" + respAttr + ">" + inner + "</note>";
    }

    private static string EscapeXmlText(string s)
        => (s ?? "").Replace("&", "&amp;").Replace("<", "&lt;").Replace(">", "&gt;");

    private static string EscapeXmlAttr(string s)
        => EscapeXmlText(s).Replace("\"", "&quot;").Replace("'", "&apos;");

    private async Task WriteTranslatedDiskAndRerenderAsync(string relPath, string updatedXml, string why)
    {
        if (_translatedDir == null) return;

        EnsureXmlIsWellFormed(updatedXml, "Updated translated XML is not well-formed.");

        var tranAbs = Path.Combine(_translatedDir, relPath);
        var saveInfo = await AtomicWriteXmlAsync(tranAbs, updatedXml);

        _rawTranXml = updatedXml;

        try
        {
            _renderCache.Invalidate(tranAbs);
        }
        catch { }

        if (string.Equals(_currentRelPath, relPath, StringComparison.OrdinalIgnoreCase))
        {
            _indexedDoc = _indexedTranslation.BuildIndex(_rawOrigXml, _rawTranXml);
            var projection = _indexedTranslation.RenderProjection(_indexedDoc, _translationMode);
            SetTranslationProjection(_translationMode, projection);
            SetBaselineFromCurrentTranslatedEditorText();
        }

        await RefreshReadableFromDiskOnlyAsync();
        await RefreshFileStatusAsync(relPath);

        SetStatus("OK: " + why + (saveInfo.BackupCreated ? " (backup updated)" : ""));
    }

    // =========================
    // Root + config
    // =========================

    private async Task LoadConfigApplyThemeAndMaybeAutoloadAsync()
    {
        try { _config = await _configService.TryLoadAsync() ?? new AppConfig { IsDarkTheme = true }; }
        catch { _config = new AppConfig { IsDarkTheme = true }; }

        ApplyTheme(_config.IsDarkTheme);
        ApplySettingsToChildViews();

        if (!string.IsNullOrWhiteSpace(_config.TextRootPath) && Directory.Exists(_config.TextRootPath))
        {
            _suppressConfigSaves = true;
            try
            {
                if (_chkZenOnly != null) _chkZenOnly.IsChecked = _config.ZenOnly;
            }
            finally
            {
                _suppressConfigSaves = false;
            }

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
        catch (Exception ex)
        {
            SetStatus("Open root failed: " + ex.Message);
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
            SetStatus("Original folder missing: " + _originalDir);
            return;
        }

        AppPaths.EnsureTranslatedDirExists(_root);

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
            _config.ZenOnly = _chkZenOnly != null && _chkZenOnly.IsChecked == true;
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
        _config.ZenOnly = _chkZenOnly != null && _chkZenOnly.IsChecked == true;
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
            ApplySettingsToChildViews();
            await SafeSaveConfigAsync();
        }
        catch (Exception ex)
        {
            SetStatus("Failed to open settings: " + ex.Message);
        }
    }

    // =========================
    // Index + filter
    // =========================

    private async Task LoadFileListFromCacheOrBuildAsync()
    {
        if (_root == null || _originalDir == null || _translatedDir == null || _filesList == null)
            return;

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

        try
        {
            var cache = await _indexCacheService.TryLoadAsync(_root);

            if (cache?.Entries is { Count: > 0 })
            {
                _allItems = cache.Entries;
                RebuildLookup();

                await ApplyFilterSafeAsync();
                WireSearchTab();

                SetStatus("Loaded index cache: " + _allItems.Count.ToString("n0") + " files.");
                return;
            }

            SetStatus("Building index cache…");

            var progress = new Progress<(int done, int total)>(p =>
            {
                SetStatus("Indexing files… " + p.done.ToString("n0") + "/" + p.total.ToString("n0"));
            });

            IndexCache built = await _indexCacheService.BuildAsync(_originalDir, _translatedDir, _root, progress);
            await _indexCacheService.SaveAsync(_root, built);

            _allItems = built.Entries ?? new List<FileNavItem>();
            RebuildLookup();

            await ApplyFilterSafeAsync();
            WireSearchTab();

            SetStatus("Index cache created: " + _allItems.Count.ToString("n0") + " files.");
        }
        catch (Exception ex)
        {
            SetStatus("Index load/build failed: " + ex.Message);
        }
    }

    private async Task RefreshAllCachedStatusesAsync()
    {
        if (_root == null || _originalDir == null || _translatedDir == null) return;

        bool changed = false;

        await Task.Run(() =>
        {
            foreach (var it in _allItems)
            {
                if (string.IsNullOrWhiteSpace(it.RelPath)) continue;

                var rel = NormalizeRel(it.RelPath);
                var origAbs = Path.Combine(_originalDir, it.RelPath);
                var tranAbs = Path.Combine(_translatedDir, it.RelPath);

                var newStatus = _indexCacheService.ComputeStatusForPairLive(origAbs, tranAbs, _root, rel, verboseLog: false);
                if (!Equals(it.Status, newStatus))
                {
                    it.Status = newStatus;
                    changed = true;
                }
            }
        });

        if (changed)
        {
            await _indexCacheService.SaveAsync(_root, new IndexCache { Entries = _allItems });
        }
    }

    private void RebuildLookup()
    {
        _allItemsByRel.Clear();
        foreach (var it in _allItems) _allItemsByRel[NormalizeRel(it.RelPath)] = it;
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

    private void ApplySettingsToChildViews()
    {
        try
        {
            _translationView?.SetHoverDictionaryEnabled(_config.EnableHoverDictionary);
        }
        catch { }

        try
        {
            var m = _readableView?.GetType().GetMethod("SetHoverDictionaryEnabled");
            m?.Invoke(_readableView, new object[] { _config.EnableHoverDictionary });
        }
        catch { }
    }

    // -------------------------
    // Nav filter scheduling / debounce
    // -------------------------

    private void ScheduleApplyFilter(bool debounce)
    {
        if (!debounce)
        {
            _ = ApplyFilterSafeAsync();
            return;
        }

        _navFilterDebounce ??= new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(220)
        };

        _navFilterDebounce.Tick -= NavFilterDebounce_Tick;
        _navFilterDebounce.Tick += NavFilterDebounce_Tick;

        _navFilterDebounce.Stop();
        _navFilterDebounce.Start();
    }

    private void NavFilterDebounce_Tick(object? sender, EventArgs e)
    {
        _navFilterDebounce?.Stop();
        _ = ApplyFilterSafeAsync();
    }

    private async Task ApplyFilterSafeAsync()
    {
        try
        {
            await ApplyFilterAsync();
        }
        catch (TaskCanceledException)
        {
            // expected during fast typing
        }
        catch (OperationCanceledException)
        {
            // expected during fast typing
        }
        catch (Exception ex)
        {
            SetStatus("Filter failed: " + ex.Message);
        }
    }

    private async Task ApplyFilterAsync()
    {
        if (_filesList == null)
            return;

        int myVersion = Interlocked.Increment(ref _navFilterVersion);

        try
        {
            try
            {
                _navSearchCts?.Cancel();
                _navSearchCts?.Dispose();
            }
            catch
            {
                // ignore
            }

            _navSearchCts = new CancellationTokenSource();
            var ct = _navSearchCts.Token;

            bool restoreFocus = _navSearch != null && _navSearch.IsFocused;

            string q = (_navSearch?.Text ?? "").Trim();
            string qLower = q.ToLowerInvariant();

            bool showFilenames = _chkShowFilenames != null && _chkShowFilenames.IsChecked == true;
            bool zenOnly = _chkZenOnly != null && _chkZenOnly.IsChecked == true;
            int statusIdx = _cmbStatusFilter?.SelectedIndex ?? 0;

            string? selectedRel = (_filesList.SelectedItem as FileNavItem)?.RelPath ?? _currentRelPath;

            var allSnapshot = _allItems.ToList();

            var built = await Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();

                IEnumerable<FileNavItem> seq = allSnapshot;

                if (zenOnly)
                    seq = seq.Where(it => !string.IsNullOrWhiteSpace(it.RelPath) && _zenTexts.IsZen(it.RelPath));

                if (statusIdx != 0)
                    seq = seq.Where(it => MatchesStatusFilter(it.Status, statusIdx));

                if (q.Length > 0)
                    seq = seq.Where(it => MatchesLocalText(it, qLower));

                return seq.Select(it =>
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
            }, ct);

            if (ct.IsCancellationRequested) return;
            if (myVersion != _navFilterVersion) return;

            _filteredItems = built;

            try
            {
                _suppressNavSelectionChanged = true;
                _filesList.ItemsSource = _filteredItems;

                if (!string.IsNullOrWhiteSpace(selectedRel))
                {
                    var match = _filteredItems.FirstOrDefault(x =>
                        string.Equals(x.RelPath, selectedRel, StringComparison.OrdinalIgnoreCase));
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
        catch
        {
            throw;
        }
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
        try { _navSearchCts?.Dispose(); } catch { }
        _navSearchCts = null;

        try { _renderCts?.Cancel(); } catch { }
        try { _renderCts?.Dispose(); } catch { }
        _renderCts = null;

        try { _navFilterDebounce?.Stop(); } catch { }

        _rawOrigXml = "";
        _rawTranXml = "";
        _currentRelPath = null;
        _indexedDoc = null;

        _baselineTranSha1 = "";
        _lastSeenTranSha1 = "";
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
            if (!await ConfirmNavigateIfDirtyAsync("switch files (" + _currentRelPath + " → " + item.RelPath + ")"))
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
    // Load/render (Readable = DISK XML ONLY)
    // =========================

    private async Task<string> ReadOriginalXmlAsync(string relPath)
    {
        if (_originalDir == null || string.IsNullOrWhiteSpace(relPath))
            return "";

        var path = Path.Combine(_originalDir, relPath);

        try
        {
            if (!File.Exists(path)) return "";
            return await File.ReadAllTextAsync(path, Encoding.UTF8);
        }
        catch
        {
            return "";
        }
    }

    private string? TryReadTranslatedXmlFromDisk(string relPath)
    {
        if (_translatedDir == null) return null;

        try
        {
            var tranAbs = Path.Combine(_translatedDir, relPath);
            if (!File.Exists(tranAbs))
                return null;

            var text = File.ReadAllText(tranAbs, Encoding.UTF8);

            if (TryParseXml(text, out _))
                return text;

            var bak = tranAbs + ".bak";
            if (File.Exists(bak))
            {
                var bakText = File.ReadAllText(bak, Encoding.UTF8);
                if (TryParseXml(bakText, out _))
                {
                    SetStatus("Translated XML was corrupted; loaded backup (.bak) instead.");
                    return bakText;
                }
            }

            SetStatus("Translated XML is malformed and no valid backup was found.");
            return null;
        }
        catch
        {
            return null;
        }
    }

    private async Task EnsureTranslatedXmlExistsForCurrentAsync()
    {
        if (_currentRelPath == null) return;
        await EnsureTranslatedXmlExistsForRelPathAsync(_currentRelPath);
    }

    private async Task EnsureTranslatedXmlExistsForRelPathAsync(string relPath)
    {
        if (_originalDir == null || _translatedDir == null) return;

        var tranAbs = Path.Combine(_translatedDir, relPath);
        if (File.Exists(tranAbs)) return;

        var origXml = await ReadOriginalXmlAsync(relPath);
        if (string.IsNullOrWhiteSpace(origXml)) return;

        EnsureXmlIsWellFormed(origXml, "Original XML is malformed; cannot create translated copy.");

        await AtomicWriteXmlAsync(tranAbs, origXml);
    }

    private async Task<(RenderedDocument ro, RenderedDocument rt)> RenderReadablePairDiskOnlyAsync(string relPath, CancellationToken ct)
    {
        if (_originalDir == null || _translatedDir == null)
            return (RenderedDocument.Empty, RenderedDocument.Empty);

        var origAbs = Path.Combine(_originalDir, relPath);
        var tranAbs = Path.Combine(_translatedDir, relPath);

        ct.ThrowIfCancellationRequested();

        var stampOrig = FileStamp.FromFile(origAbs);
        RenderedDocument ro;
        if (!_renderCache.TryGet(stampOrig, out ro))
        {
            ct.ThrowIfCancellationRequested();
            ro = CbetaTeiRenderer.Render(SafeReadAllTextUtf8(origAbs));
            _renderCache.Put(stampOrig, ro);
        }

        ct.ThrowIfCancellationRequested();

        if (!File.Exists(tranAbs))
        {
            var rtFallback = CbetaTeiRenderer.Render(SafeReadAllTextUtf8(origAbs));
            return (ro, rtFallback);
        }

        var stampTran = FileStamp.FromFile(tranAbs);
        RenderedDocument rt;
        if (!_renderCache.TryGet(stampTran, out rt))
        {
            ct.ThrowIfCancellationRequested();
            rt = CbetaTeiRenderer.Render(SafeReadAllTextUtf8(tranAbs));
            _renderCache.Put(stampTran, rt);
        }

        return (ro, rt);
    }

    private async Task LoadPairAsync(string relPath)
    {
        if (_originalDir == null || _translatedDir == null) return;

        _renderCts?.Cancel();
        _renderCts?.Dispose();
        _renderCts = new CancellationTokenSource();
        var ct = _renderCts.Token;

        _currentRelPath = relPath;

        if (_txtCurrentFile != null) _txtCurrentFile.Text = relPath;
        _gitView?.SetSelectedRelPath(_currentRelPath);

        SetStatus("Loading: " + relPath);

        _rawOrigXml = await ReadOriginalXmlAsync(relPath);

        await EnsureTranslatedXmlExistsForRelPathAsync(relPath);

        _rawTranXml = TryReadTranslatedXmlFromDisk(relPath) ?? _rawOrigXml;

        _indexedDoc = _indexedTranslation.BuildIndex(_rawOrigXml, _rawTranXml);

        var projection = _indexedTranslation.RenderProjection(_indexedDoc, _translationMode);
        SetTranslationProjection(_translationMode, projection);

        _baselineTranSha1 = Sha1Hex(projection);
        _lastSeenTranSha1 = _baselineTranSha1;
        _dirty = false;
        UpdateWindowTitle();
        UpdateSaveButtonState();

        SetStatus("Rendering readable view…");

        try
        {
            var swRender = Stopwatch.StartNew();

            var (ro, rt) = await Task.Run(async () =>
            {
                ct.ThrowIfCancellationRequested();
                return await RenderReadablePairDiskOnlyAsync(relPath, ct);
            }, ct);

            swRender.Stop();
            if (ct.IsCancellationRequested) return;

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                _readableView?.SetRendered(ro, rt);
            });

            try
            {
                bool isZen = _root != null && _zenTexts.IsZen(relPath);
                _readableView?.SetZenContext(relPath, isZen);
            }
            catch { }

            await SaveUiStateAsync();
            SetStatus("Loaded. Segments: O=" + ro.Segments.Count.ToString("n0") +
                      ", T=" + rt.Segments.Count.ToString("n0") +
                      ". Render=" + swRender.ElapsedMilliseconds.ToString("n0") + "ms");
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            SetStatus("Render failed: " + ex.Message);
        }
    }

    private async Task RefreshReadableFromDiskOnlyAsync()
    {
        if (_readableView == null || _currentRelPath == null || _originalDir == null || _translatedDir == null) return;

        _renderCts?.Cancel();
        _renderCts?.Dispose();
        _renderCts = new CancellationTokenSource();
        var ct = _renderCts.Token;

        try
        {
            await EnsureTranslatedXmlExistsForCurrentAsync();

            var (ro, rt) = await Task.Run(async () =>
            {
                ct.ThrowIfCancellationRequested();
                return await RenderReadablePairDiskOnlyAsync(_currentRelPath, ct);
            }, ct);

            if (ct.IsCancellationRequested) return;

            await Dispatcher.UIThread.InvokeAsync(() => _readableView.SetRendered(ro, rt));
            SetStatus("Readable refreshed (disk XML).");
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { SetStatus("Readable refresh failed: " + ex.Message); }
    }

    // =========================
    // Save/revert (translation projection -> TEI)
    // =========================

    private async Task SaveTranslatedFromTabAsync()
    {
        try
        {
            if (_currentRelPath == null) { SetStatus("Nothing to save."); return; }
            if (_translationView == null || _translatedDir == null) { SetStatus("Save unavailable."); return; }
            if (_indexedDoc == null) { SetStatus("Translation index not loaded."); return; }

            var editedProjection = GetTranslationProjectionText();

            _indexedTranslation.ApplyProjectionEdits(_indexedDoc, _translationMode, editedProjection);

            var builtXml = _indexedTranslation.BuildTranslatedXml(_indexedDoc, out var updatedCount);

            var saveInfo = await AtomicWriteTranslatedXmlForCurrentAsync(builtXml);

            _rawTranXml = builtXml;

            await RefreshFileStatusAsync(_currentRelPath);

            try
            {
                var tranAbs = Path.Combine(_translatedDir, _currentRelPath);
                _renderCache.Invalidate(tranAbs);
            }
            catch { }

            _indexedDoc = _indexedTranslation.BuildIndex(_rawOrigXml, _rawTranXml);
            var freshProjection = _indexedTranslation.RenderProjection(_indexedDoc, _translationMode);
            SetTranslationProjection(_translationMode, freshProjection);

            _baselineTranSha1 = Sha1Hex(freshProjection);
            _lastSeenTranSha1 = _baselineTranSha1;
            _dirty = false;
            UpdateWindowTitle();

            await RefreshReadableFromDiskOnlyAsync();

            var backupMsg = saveInfo.BackupCreated ? " backup=yes" : " backup=no";
            SetStatus("Saved translated XML (" + updatedCount.ToString("n0") + " units updated)." + backupMsg);
        }
        catch (Exception ex)
        {
            SetStatus("Save failed: " + ex.Message);
        }
    }

    private async Task RevertTranslatedXmlFromDiskAsync()
    {
        try
        {
            if (_currentRelPath == null) { SetStatus("Nothing to revert."); return; }

            _rawOrigXml = await ReadOriginalXmlAsync(_currentRelPath);
            if (string.IsNullOrWhiteSpace(_rawOrigXml))
            {
                SetStatus("Revert failed: original XML could not be read.");
                return;
            }

            _rawTranXml = TryReadTranslatedXmlFromDisk(_currentRelPath) ?? _rawOrigXml;

            EnsureXmlIsWellFormed(_rawOrigXml, "Original XML is malformed.");
            EnsureXmlIsWellFormed(_rawTranXml, "Translated XML is malformed.");

            _indexedDoc = _indexedTranslation.BuildIndex(_rawOrigXml, _rawTranXml);
            var projection = _indexedTranslation.RenderProjection(_indexedDoc, _translationMode);
            SetTranslationProjection(_translationMode, projection);

            SetBaselineFromCurrentTranslatedEditorText();
            await RefreshReadableFromDiskOnlyAsync();

            SetStatus("Reverted translation editor to disk state.");
        }
        catch (Exception ex)
        {
            SetStatus("Revert failed: " + ex.Message);
        }
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
                if (!Equals(existing.Status, newStatus))
                {
                    existing.Status = newStatus;
                    MarkIndexCacheDirty();
                }

                await ApplyFilterSafeAsync();
            }
        }
        catch { }
    }

    // =========================
    // Tab + dirty tracking
    // =========================

    private void UpdateSaveButtonState()
    {
        if (_btnSave == null) return;

        bool hasFile = _currentRelPath != null;
        bool translationTabSelected = _tabs != null && _tabs.SelectedIndex == 1;
        _btnSave.IsEnabled = hasFile && translationTabSelected;
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
        try { cur = GetTranslationProjectionText(); }
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
        try { cur = GetTranslationProjectionText(); }
        catch { cur = ""; }

        bool dirtyNow = Sha1Hex(cur) != (_baselineTranSha1 ?? "");
        if (dirtyNow == _dirty && !forceUi) return;

        _dirty = dirtyNow;
        UpdateWindowTitle();
    }

    private void SetBaselineFromCurrentTranslatedEditorText()
    {
        if (_translationView == null) return;

        string cur;
        try { cur = GetTranslationProjectionText(); }
        catch { cur = ""; }

        _baselineTranSha1 = Sha1Hex(cur);
        _lastSeenTranSha1 = _baselineTranSha1;
        _dirty = false;
        UpdateWindowTitle();
    }

    private void CaptureTranslationEditsToRaw()
    {
        if (_translationView == null || _indexedDoc == null) return;

        try
        {
            var projection = GetTranslationProjectionText();
            _indexedTranslation.ApplyProjectionEdits(_indexedDoc, _translationMode, projection);
            _rawTranXml = _indexedTranslation.BuildTranslatedXml(_indexedDoc, out _);
        }
        catch
        {
            // ignore during navigation prompts
        }
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
        Title = string.IsNullOrWhiteSpace(file) ? (AppTitleBase + star) : (AppTitleBase + star + " — " + file);

        if (_txtCurrentFile != null)
            _txtCurrentFile.Text = string.IsNullOrWhiteSpace(file) ? "" : (file + (_dirty ? "  *" : ""));
    }

    private async Task<bool> ConfirmNavigateIfDirtyAsync(string action)
    {
        CaptureTranslationEditsToRaw();
        UpdateDirtyStateFromEditor(forceUi: true);

        if (!_dirty) return true;

        return await ShowYesNoAsync("Unsaved changes", "You have unsaved changes.\n\nProceed to " + action + "?");
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

        var win = new Window
        {
            Title = title,
            Width = 620,
            Height = 360,
            Content = panel,
            WindowStartupLocation = WindowStartupLocation.CenterOwner
        };
        win.RequestedThemeVariant = this.ActualThemeVariant;

        var tcs = new TaskCompletionSource<bool>();
        btnYes.Click += (_, _) => { win.Close(); tcs.TrySetResult(true); };
        btnNo.Click += (_, _) => { win.Close(); tcs.TrySetResult(false); };

        await win.ShowDialog(this);
        return await tcs.Task;
    }

    // =========================
    // Theme
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
        if (Application.Current != null)
            Application.Current.RequestedThemeVariant = variant;

        var res = Application.Current?.Resources;
        if (res == null) return;

        string prefix = dark ? "Night_" : "Light_";

        foreach (var token in ThemeTokens)
        {
            var sourceKey = prefix + token;

            if (!res.TryGetValue(sourceKey, out var sourceObj) || sourceObj is null)
                continue;

            if (res.TryGetValue(token, out var activeObj) &&
                activeObj is SolidColorBrush activeBrush &&
                sourceObj is SolidColorBrush sourceBrush)
            {
                activeBrush.Color = sourceBrush.Color;
                activeBrush.Opacity = sourceBrush.Opacity;
                continue;
            }

            res[token] = sourceObj;
        }

        RefreshNavListVisuals();
    }

    private void RefreshNavListVisuals()
    {
        try
        {
            if (_filesList == null) return;

            var selected = _filesList.SelectedItem;
            var src = _filesList.ItemsSource;

            _filesList.ItemsSource = null;
            _filesList.ItemsSource = src;
            _filesList.SelectedItem = selected;
        }
        catch
        {
            // ignore
        }
    }

    // =========================
    // Git helper
    // =========================

    private async Task<bool> EnsureTranslatedXmlForRelPathAsync(string relPath, bool saveCurrentEditor)
    {
        if (_originalDir == null || _translatedDir == null) return false;

        var origPath = Path.Combine(_originalDir, relPath);
        if (!File.Exists(origPath)) return false;

        if (saveCurrentEditor &&
            _translationView != null &&
            _indexedDoc != null &&
            string.Equals(_currentRelPath, relPath, StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var projection = GetTranslationProjectionText();
                _indexedTranslation.ApplyProjectionEdits(_indexedDoc, _translationMode, projection);

                var xml = _indexedTranslation.BuildTranslatedXml(_indexedDoc, out _);
                var tranAbs = Path.Combine(_translatedDir, relPath);

                await AtomicWriteXmlAsync(tranAbs, xml);

                _rawTranXml = xml;
                return true;
            }
            catch
            {
                return false;
            }
        }

        await EnsureTranslatedXmlExistsForRelPathAsync(relPath);
        return File.Exists(Path.Combine(_translatedDir, relPath));
    }

    // =========================
    // TranslationTabView projection helpers
    // =========================

    private void SetTranslationProjection(TranslationEditMode mode, string projectionText)
    {
        if (_translationView == null) return;

        TrySetCurrentFilePaths();
        _translationView.SetModeProjection(mode, projectionText ?? "");
    }

    private string GetTranslationProjectionText()
    {
        if (_translationView == null) return "";
        return _translationView.GetCurrentProjectionText() ?? "";
    }

    private void TrySetCurrentFilePaths()
    {
        if (_translationView == null || _originalDir == null || _translatedDir == null || _currentRelPath == null) return;

        try
        {
            var origAbs = Path.Combine(_originalDir, _currentRelPath);
            var tranAbs = Path.Combine(_translatedDir, _currentRelPath);
            _translationView.SetCurrentFilePaths(origAbs, tranAbs);
        }
        catch { }
    }

    // =========================
    // Atomic XML save helpers
    // =========================

    private sealed class AtomicSaveInfo
    {
        public bool BackupCreated { get; init; }
        public string FinalPath { get; init; } = "";
        public string TempPath { get; init; } = "";
        public string BackupPath { get; init; } = "";
    }

    private async Task<AtomicSaveInfo> AtomicWriteTranslatedXmlForCurrentAsync(string xml)
    {
        if (_currentRelPath == null) throw new InvalidOperationException("No file selected.");
        if (_translatedDir == null) throw new InvalidOperationException("Translated directory not available.");

        var tranAbs = Path.Combine(_translatedDir, _currentRelPath);
        return await AtomicWriteXmlAsync(tranAbs, xml);
    }

    private async Task<AtomicSaveInfo> AtomicWriteXmlAsync(string finalPath, string xml)
    {
        if (string.IsNullOrWhiteSpace(finalPath))
            throw new ArgumentException("Target path is empty.", nameof(finalPath));

        xml ??= "";

        EnsureXmlIsWellFormed(xml, "XML validation failed before save.");

        var dir = Path.GetDirectoryName(finalPath);
        if (string.IsNullOrWhiteSpace(dir))
            throw new InvalidOperationException("Could not resolve target directory.");

        Directory.CreateDirectory(dir);

        string tmpPath = finalPath + ".tmp";
        string bakPath = finalPath + ".bak";

        try
        {
            if (File.Exists(tmpPath))
                File.Delete(tmpPath);
        }
        catch { }

        await File.WriteAllTextAsync(tmpPath, xml, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

        string tmpReadBack;
        try
        {
            tmpReadBack = await File.ReadAllTextAsync(tmpPath, Encoding.UTF8);
        }
        catch (Exception ex)
        {
            try { if (File.Exists(tmpPath)) File.Delete(tmpPath); } catch { }
            throw new InvalidOperationException("Save failed while verifying temporary file: " + ex.Message, ex);
        }

        EnsureXmlIsWellFormed(tmpReadBack, "Temporary save file is malformed.");
        if (!string.Equals(xml, tmpReadBack, StringComparison.Ordinal))
        {
            try { if (File.Exists(tmpPath)) File.Delete(tmpPath); } catch { }
            throw new InvalidOperationException("Temporary save file content mismatch after write verification.");
        }

        bool backupCreated = false;

        if (File.Exists(finalPath))
        {
            try
            {
                if (File.Exists(bakPath))
                    File.Delete(bakPath);

                File.Move(finalPath, bakPath);
                backupCreated = true;
            }
            catch (Exception ex)
            {
                try { if (File.Exists(tmpPath)) File.Delete(tmpPath); } catch { }
                throw new InvalidOperationException("Could not create backup before save: " + ex.Message, ex);
            }
        }

        try
        {
            File.Move(tmpPath, finalPath);
        }
        catch (Exception ex)
        {
            try
            {
                if (!File.Exists(finalPath) && File.Exists(bakPath))
                    File.Move(bakPath, finalPath);
            }
            catch { }

            try { if (File.Exists(tmpPath)) File.Delete(tmpPath); } catch { }

            throw new InvalidOperationException("Could not finalize save: " + ex.Message, ex);
        }

        try
        {
            var finalText = await File.ReadAllTextAsync(finalPath, Encoding.UTF8);
            EnsureXmlIsWellFormed(finalText, "Final saved file is malformed.");
        }
        catch (Exception ex)
        {
            try
            {
                if (File.Exists(bakPath))
                {
                    if (File.Exists(finalPath)) File.Delete(finalPath);
                    File.Move(bakPath, finalPath);
                }
            }
            catch { }

            throw new InvalidOperationException("Saved file failed verification and was rolled back if possible: " + ex.Message, ex);
        }

        return new AtomicSaveInfo
        {
            BackupCreated = backupCreated,
            FinalPath = finalPath,
            TempPath = tmpPath,
            BackupPath = bakPath
        };
    }

    private static void EnsureXmlIsWellFormed(string xml, string? prefix = null)
    {
        if (!TryParseXml(xml, out var error))
        {
            if (string.IsNullOrWhiteSpace(prefix))
                throw new InvalidOperationException(error ?? "XML is not well-formed.");

            throw new InvalidOperationException(prefix + " " + (error ?? "XML is not well-formed."));
        }
    }

    private static bool TryParseXml(string xml, out string? error)
    {
        try
        {
            _ = XDocument.Parse(xml ?? "", LoadOptions.PreserveWhitespace | LoadOptions.SetLineInfo);
            error = null;
            return true;
        }
        catch (XmlException xex)
        {
            error = "XML parse error at line " + xex.LineNumber + ", pos " + xex.LinePosition + ": " + xex.Message;
            return false;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
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
        catch
        {
            return "sha1_err";
        }
    }

    private static string SafeReadAllTextUtf8(string path)
    {
        try
        {
            if (File.Exists(path))
                return File.ReadAllText(path, Encoding.UTF8);
        }
        catch { }
        return "";
    }

    private static string ReadAllTextUtf8Strict(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path is empty.", nameof(path));

        if (!File.Exists(path))
            throw new FileNotFoundException("File not found.", path);

        return File.ReadAllText(path, Encoding.UTF8);
    }

    private void MarkIndexCacheDirty()
    {
        _indexCacheDirty = true;

        _indexCacheSaveDebounce ??= new DispatcherTimer
        {
            Interval = TimeSpan.FromMilliseconds(500)
        };

        _indexCacheSaveDebounce.Tick -= IndexCacheSaveDebounce_Tick;
        _indexCacheSaveDebounce.Tick += IndexCacheSaveDebounce_Tick;

        _indexCacheSaveDebounce.Stop();
        _indexCacheSaveDebounce.Start();
    }

    private async void IndexCacheSaveDebounce_Tick(object? sender, EventArgs e)
    {
        try
        {
            _indexCacheSaveDebounce?.Stop();
            if (!_indexCacheDirty) return;
            if (_root == null) return;

            _indexCacheDirty = false;

            await _indexCacheService.SaveAsync(_root, new IndexCache
            {
                Entries = _allItems
            });
        }
        catch
        {
            // ignore
        }
    }
}