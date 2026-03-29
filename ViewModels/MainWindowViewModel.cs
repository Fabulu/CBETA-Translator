// ViewModels/MainWindowViewModel.cs
//
// Extracted from Views/MainWindow.axaml.cs (Wave 5 MVVM renovation).
// Contains all business logic, state, and orchestration that was previously
// in the MainWindow code-behind. UI-only concerns (dialogs, window chrome,
// keyboard shortcuts, FindControl, bridge wiring) remain in code-behind.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;
using Avalonia.Threading;
using CbetaTranslator.App.Infrastructure;
using CbetaTranslator.App.Models;
using CbetaTranslator.App.Services;
using CbetaTranslator.App.Text;
using Microsoft.Extensions.DependencyInjection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CbetaTranslator.App.ViewModels;

public enum StatusSeverity { Info, Success, Warning, Error }

public partial class MainWindowViewModel : ViewModelBase
{
    private const string AppTitleBase = "CBETA Translator";

    // ---- Services (injected) ----
    private readonly IFileService _fileService;
    private readonly IAppConfigService _configService;
    private readonly IIndexCacheService _indexCacheService;
    private readonly IRenderedDocumentCacheService _renderCache;
    private readonly IZenTextsService _zenTexts;
    private readonly IIndexedTranslationService _indexedTranslation;
    private readonly ITranslationAssistantService _translationAssistant;
    private readonly ITranslationAssistantBuildService _translationAssistantBuilder;
    private readonly ITranslationReviewService _translationReview;
    private readonly ISearchIndexService _searchIndex;

    // ---- Internal state ----
    private IndexedTranslationDocument? _indexedDoc;
    private TranslationEditMode _translationMode = TranslationEditMode.Body;

    private AppConfig _config = new() { IsDarkTheme = true };
    public AppConfig Config => _config;

    private string? _root, _originalDir, _translatedDir;
    public string? Root => _root;
    public string? OriginalDir => _originalDir;
    public string? TranslatedDir => _translatedDir;

    private string? _currentRelPath;
    public string? CurrentRelPath => _currentRelPath;

    private List<FileNavItem> _allItems = new();
    private List<FileNavItem> _filteredItems = new();
    private readonly Dictionary<string, FileNavItem> _allItemsByRel = new(StringComparer.OrdinalIgnoreCase);

    public List<FileNavItem> FilteredItems => _filteredItems;
    public Dictionary<string, FileNavItem> AllItemsByRel => _allItemsByRel;

    private CancellationTokenSource? _navSearchCts;
    private CancellationTokenSource? _renderCts;
    private CancellationTokenSource? _assistantCts;
    private CancellationTokenSource? _autoIndexCts;
    private bool _isAutoIndexing;

    // Nav filter performance / race control
    private int _navFilterVersion;

    private bool _indexCacheDirty;

    private string _rawOrigXml = "";
    private string _rawTranXml = "";

    // Dirty tracking (projection text hash)
    private string _baselineTranSha1 = "", _lastSeenTranSha1 = "";
    private bool _dirty;
    public bool IsDirty => _dirty;

    private int _lastTabIndex = -1;

    private CurrentSegmentContext? _currentSegmentContext;

    private bool _suppressConfigSaves;

    // ---- Observable properties ----

    [ObservableProperty]
    private string _statusText = "Ready.";

    [ObservableProperty]
    private StatusSeverity _statusSeverity = StatusSeverity.Info;

    [ObservableProperty]
    private string _rootDisplayText = "";

    [ObservableProperty]
    private string _currentFileText = "";

    [ObservableProperty]
    private string _windowTitle = AppTitleBase;

    // ===========================================================
    // Bridge delegates — wired by code-behind to tab view methods
    // ===========================================================

    // ReadableTabView bridges
    public Action<RenderedDocument, RenderedDocument>? SetReadableRendered { get; set; }
    public Action? ClearReadable { get; set; }
    public Action<bool>? SetReadableHoverDict { get; set; }
    public Action<string?, bool>? SetReadableZenContext { get; set; }
    public Action<IReadOnlyList<TermHit>?, string?, int?, string?>? UpdateReadableTermHighlights { get; set; }
    public Action<string>? SetReadableDefaultResp { get; set; }

    // TranslationTabView bridges
    public Action<TranslationEditMode, string>? SetTranslationModeProjection { get; set; }
    public Func<string>? GetTranslationProjectionText { get; set; }
    public Action? ClearTranslation { get; set; }
    public Action<bool>? SetTranslationHoverDict { get; set; }
    public Action<TranslationAssistantSnapshot?>? SetAssistantSnapshot { get; set; }
    public Action<string?, string?, DateTime?, SegmentReviewAggregation?>? SetCurrentReviewState { get; set; }
    public Action<int, int, int>? SetProgressStats { get; set; }
    public Action<string, int>? FillEnForCurrentBlock { get; set; }
    public Action? JumpToNextBlock { get; set; }
    public Action? JumpToPreviousBlock { get; set; }
    public Action<IReadOnlySet<int>>? JumpToNextUnapproved { get; set; }
    public Func<bool>? IsTranslationEditorFocused { get; set; }
    public Func<IReadOnlyList<int>>? GetAllBlockNumbers { get; set; }
    public Action<IReadOnlyList<TermHit>?, string?>? UpdateTranslationTermHighlights { get; set; }
    public Action<IReadOnlyList<TranslationTmMatch>?, IReadOnlyList<TranslationTmMatch>?, string?>? UpdateTranslationTmSharedHighlights { get; set; }
    public Action<string, string>? SetTranslationFilePaths { get; set; }
    public Action<Func<string, string>?>? SetAssistantTitleResolver { get; set; }

    // SearchTabView bridges
    public Action<string, string, string>? SetSearchRootContext { get; set; }
    public Action<Func<string, bool>>? SetSearchZenResolver { get; set; }
    public Action<string, string, string, Func<string, (string, string, TranslationStatus?)>>? SetSearchContext { get; set; }
    public Action? ClearSearch { get; set; }

    // GitTabView bridges
    public Action<string?>? SetGitRepoRoot { get; set; }
    public Action<string?>? SetGitSelectedRelPath { get; set; }
    public Action<string?>? SetGitUsername { get; set; }
    public Action<string?, string?>? LoadGitPersistedAuth { get; set; }

    // ScholarTabView bridges
    public Action<string>? SetScholarRoot { get; set; }
    public Action? ClearScholar { get; set; }
    public Action<string?>? SetScholarUsername { get; set; }
    public Action<string?, string?>? SetScholarTranslationDirs { get; set; }
    public Func<Task>? SaveScholarStateAsync { get; set; }

    // Dialog bridges (code-behind provides UI dialogs)
    public Func<Task<string?>>? ShowFolderPickerAsync { get; set; }
    public Func<string, Task<bool>>? ConfirmNavigateIfDirtyDialogAsync { get; set; }
    public Func<AppConfig, Task<AppConfig?>>? ShowSettingsDialogAsync { get; set; }
    public Func<Task<string?>>? ShowUsernamePromptAsync { get; set; }
    public Func<string?, Task>? ShowLicensesAsync { get; set; }
    public Func<string, string, Task<bool>>? ShowYesNoDialogAsync { get; set; }

    // Window bridges
    public Action<string>? SetWindowTitle { get; set; }
    public Action<bool>? ApplyTheme { get; set; }
    public Action<bool>? SetSaveButtonEnabled { get; set; }
    public Func<int>? GetSelectedTabIndex { get; set; }
    public Action<int>? ForceTabIndex { get; set; }
    public Action<NavigationRequest>? NavigateInReadable { get; set; }

    // Nav ListBox bridges
    public Action<List<FileNavItem>>? SetNavItemsSource { get; set; }
    public Action<FileNavItem?>? SetNavSelectedItem { get; set; }
    public Func<FileNavItem?>? GetNavSelectedItem { get; set; }
    public Action? RestoreNavSearchFocus { get; set; }

    // Index cache save debounce bridge
    public Action? ScheduleIndexCacheSave { get; set; }

    // ===========================================================
    // Constructor
    // ===========================================================

    public MainWindowViewModel(
        IFileService fileService,
        IAppConfigService configService,
        IIndexCacheService indexCacheService,
        IRenderedDocumentCacheService renderCache,
        IZenTextsService zenTexts,
        IIndexedTranslationService indexedTranslation,
        ITranslationAssistantService translationAssistant,
        ITranslationAssistantBuildService translationAssistantBuilder,
        ITranslationReviewService translationReview,
        ISearchIndexService searchIndex)
    {
        _fileService = fileService;
        _configService = configService;
        _indexCacheService = indexCacheService;
        _renderCache = renderCache;
        _zenTexts = zenTexts;
        _indexedTranslation = indexedTranslation;
        _translationAssistant = translationAssistant;
        _translationAssistantBuilder = translationAssistantBuilder;
        _translationReview = translationReview;
        _searchIndex = searchIndex;
    }

    // ===========================================================
    // Public accessors for code-behind
    // ===========================================================

    public TranslationEditMode TranslationMode => _translationMode;
    public IndexedTranslationDocument? IndexedDoc => _indexedDoc;
    public CurrentSegmentContext? CurrentSegmentCtx => _currentSegmentContext;

    // ===========================================================
    // Status
    // ===========================================================

    public void SetStatus(string msg, StatusSeverity severity = StatusSeverity.Info)
    {
        StatusText = msg;
        StatusSeverity = severity;
    }

    // ===========================================================
    // Root + config
    // ===========================================================

    public async Task LoadConfigApplyThemeAndMaybeAutoloadAsync(bool isSecondaryWindow)
    {
        try
        {
            try { _config = await _configService.TryLoadAsync() ?? new AppConfig { IsDarkTheme = true }; }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[MainWindowViewModel] Config load failed: {ex.Message}"); _config = new AppConfig { IsDarkTheme = true }; }

            ApplyTheme?.Invoke(_config.IsDarkTheme);
            ApplySettingsToChildViews();

            if (isSecondaryWindow) return;

            if (string.IsNullOrWhiteSpace(_config.Username))
            {
                var name = await (ShowUsernamePromptAsync?.Invoke() ?? Task.FromResult<string?>(null));
                _config.Username = name ?? "User";
                await SafeSaveConfigAsync();
                ApplySettingsToChildViews();
            }

            if (!string.IsNullOrWhiteSpace(_config.TextRootPath) && Directory.Exists(_config.TextRootPath))
            {
                _suppressConfigSaves = true;
                try
                {
                    // Code-behind should apply _config.ZenOnly to UI checkbox here
                    // via the OnConfigLoaded callback
                    OnConfigLoaded?.Invoke(_config);
                }
                finally
                {
                    _suppressConfigSaves = false;
                }

                SetStatus("Auto-loading last root...");
                await LoadRootAsync(_config.TextRootPath, saveToConfig: false);

                if (!string.IsNullOrWhiteSpace(_config.LastSelectedRelPath))
                {
                    var rel = NormalizeRel(_config.LastSelectedRelPath);
                    SelectInNav(rel);
                    await LoadPairAsync(rel);
                }
            }
        }
        finally
        {
            // Code-behind should signal _windowReady from here
        }
    }

    /// <summary>
    /// Called by code-behind after config is loaded to apply UI-only config (e.g., zen checkbox).
    /// </summary>
    public Action<AppConfig>? OnConfigLoaded { get; set; }

    /// <summary>
    /// Invoked after the fast phase of LoadPairAsync completes (projection editor ready).
    /// Code-behind uses this to signal the window as ready before the slow readable render.
    /// </summary>
    public Action? SignalCoreLoadComplete { get; set; }

    [RelayCommand]
    public async Task OpenRootAsync()
    {
        try
        {
            if (!await ConfirmNavigateIfDirtyAsync("open a different root")) return;

            var folder = await (ShowFolderPickerAsync?.Invoke() ?? Task.FromResult<string?>(null));
            if (folder == null) return;

            await LoadRootAsync(folder, saveToConfig: true);
        }
        catch (Exception ex)
        {
            SetStatus("Open root failed: " + ex.Message);
        }
    }

    public async Task LoadRootAsync(string rootPath, bool saveToConfig)
    {
        _root = rootPath;
        _originalDir = AppPaths.GetOriginalDir(_root);
        _translatedDir = AppPaths.GetTranslatedDir(_root);

        _renderCache.Clear();

        RootDisplayText = _root;

        if (!Directory.Exists(_originalDir))
        {
            SetStatus("Original folder missing: " + _originalDir);
            return;
        }

        AppPaths.EnsureTranslatedDirExists(_root);

        try
        {
            await _zenTexts.LoadAsync(_root);
            SetSearchZenResolver?.Invoke(rel => _zenTexts.IsZen(rel));
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[MainWindowViewModel] Zen texts load failed: {ex.Message}"); }

        SetGitRepoRoot?.Invoke(_root);
        SetSearchRootContext?.Invoke(_root, _originalDir, _translatedDir);
        SetScholarRoot?.Invoke(_root);
        SetScholarTranslationDirs?.Invoke(_originalDir, _translatedDir);

        try
        {
            var reviewsDir = ITranslationReviewService.GetCommunityReviewsDir(_root);
            await _translationReview.RefreshAggregationCacheAsync(_root, reviewsDir);
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[MainWindowViewModel] Review aggregation refresh failed: {ex.Message}"); }

        if (saveToConfig)
        {
            _config.TextRootPath = _root;
            _config.ZenOnly = GetZenOnly?.Invoke() ?? false;
            _config.Version = Math.Max(_config.Version, 3);
            await SafeSaveConfigAsync();
        }

        await LoadFileListFromCacheOrBuildAsync();

        QueueAutoIndexBuild();
    }

    private void QueueAutoIndexBuild()
    {
        if (_root == null || _originalDir == null || _translatedDir == null) return;

        _autoIndexCts?.Cancel();
        try { _autoIndexCts?.Dispose(); } catch { }
        _autoIndexCts = new CancellationTokenSource();
        var ct = _autoIndexCts.Token;

        var root = _root;
        var origDir = _originalDir;
        var tranDir = _translatedDir;

        _ = Task.Run(async () =>
        {
            _isAutoIndexing = true;
            try
            {
                // Preload CEDICT dictionary in background so first file load is fast
                try
                {
                    var cedict = App.Services.GetService<ICedictDictionary>();
                    if (cedict != null)
                        await cedict.EnsureLoadedAsync(ct);
                }
                catch { }

                // Let the initial file load finish before competing for disk I/O
                await Task.Delay(3000, ct);

                // Search index
                bool searchStale = await _searchIndex.IsStaleAsync(root, origDir, tranDir);
                if (searchStale && !ct.IsCancellationRequested)
                {
                    Dispatcher.UIThread.Post(() => SetStatus("Auto-updating search index..."));

                    var progress = new Progress<(int done, int total, string phase)>(t =>
                        Dispatcher.UIThread.Post(() => SetStatus($"Indexing: {t.phase} ({t.done}/{t.total})")));

                    await _searchIndex.BuildOrUpdateAsync(root, origDir, tranDir,
                        forceRebuild: false, progress, ct);

                    if (!ct.IsCancellationRequested)
                        Dispatcher.UIThread.Post(() => SetStatus("Search index ready."));
                }

                // TM Reference
                bool tmStale = await _translationAssistantBuilder.IsReferenceStaleAsync(root, tranDir);
                if (tmStale && !ct.IsCancellationRequested)
                {
                    Dispatcher.UIThread.Post(() => SetStatus("Auto-building reference TM..."));

                    var tmProgress = new Progress<(int done, int total, string status)>(t =>
                        Dispatcher.UIThread.Post(() => SetStatus($"Building TM: {t.status} ({t.done}/{t.total})")));

                    await _translationAssistantBuilder.BuildReferenceTranslationMemoryAsync(
                        root, origDir, tranDir,
                        rel => _zenTexts.IsZen(rel), tmProgress, ct);

                    if (!ct.IsCancellationRequested)
                        Dispatcher.UIThread.Post(() => SetStatus("Reference TM ready."));
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Dispatcher.UIThread.Post(() => SetStatus($"Auto-index: {ex.Message}"));
            }
            finally
            {
                _isAutoIndexing = false;
                Dispatcher.UIThread.Post(() => OnAutoIndexCompleted?.Invoke());
            }
        }, ct);
    }

    /// <summary>
    /// Bridge callback fired when auto-index build completes (search index + reference TM).
    /// Used by the onboarding tour to advance past the "Building Index" step.
    /// </summary>
    public Action? OnAutoIndexCompleted { get; set; }

    /// <summary>
    /// Bridge to get the ZenOnly checkbox state from code-behind.
    /// </summary>
    public Func<bool>? GetZenOnly { get; set; }

    /// <summary>
    /// Bridge to get the nav search text from code-behind.
    /// </summary>
    public Func<string>? GetNavSearchText { get; set; }

    /// <summary>
    /// Bridge to get the ShowFilenames checkbox state from code-behind.
    /// </summary>
    public Func<bool>? GetShowFilenames { get; set; }

    /// <summary>
    /// Bridge to get the status filter index from code-behind.
    /// </summary>
    public Func<int>? GetStatusFilterIndex { get; set; }

    /// <summary>
    /// Bridge to check if nav search is focused from code-behind.
    /// </summary>
    public Func<bool>? IsNavSearchFocused { get; set; }

    public async Task SafeSaveConfigAsync()
    {
        try { await _configService.SaveAsync(_config); }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[MainWindowViewModel] Config save failed: {ex.Message}"); }
    }

    public async Task SaveUiStateAsync()
    {
        if (_suppressConfigSaves) return;
        if (_root == null) return;

        _config.TextRootPath = _root;
        _config.LastSelectedRelPath = _currentRelPath;
        _config.ZenOnly = GetZenOnly?.Invoke() ?? false;
        _config.Version = Math.Max(_config.Version, 3);
        await SafeSaveConfigAsync();
    }

    [RelayCommand]
    public async Task OpenSettingsAsync()
    {
        try
        {
            var result = await (ShowSettingsDialogAsync?.Invoke(_config) ?? Task.FromResult<AppConfig?>(null));
            if (result == null) return;

            _config = result;
            ApplyTheme?.Invoke(_config.IsDarkTheme);
            ApplySettingsToChildViews();
            await SafeSaveConfigAsync();
        }
        catch (Exception ex)
        {
            SetStatus("Failed to open settings: " + ex.Message);
        }
    }

    [RelayCommand]
    public async Task OpenLicensesAsync()
    {
        try
        {
            await (ShowLicensesAsync?.Invoke(_root) ?? Task.CompletedTask);
        }
        catch (Exception ex)
        {
            SetStatus("Failed to open licenses: " + ex.Message);
        }
    }

    public void ApplySettingsToChildViews()
    {
        try { SetTranslationHoverDict?.Invoke(_config.EnableHoverDictionary); } catch { }
        try { SetReadableHoverDict?.Invoke(_config.EnableHoverDictionary); } catch { }
        try { SetReadableDefaultResp?.Invoke(_config.Username ?? ""); } catch { }
        try { SetGitUsername?.Invoke(_config.Username); } catch { }
        try { LoadGitPersistedAuth?.Invoke(_config.GitHubAccessToken, _config.GitHubUsername); } catch { }
        try { SetScholarUsername?.Invoke(_config.Username); } catch { }
    }

    public void UpdateConfig(AppConfig config)
    {
        _config = config;
    }

    public async Task HandleGitHubAuthCompletedAsync(string token, string login)
    {
        _config.GitHubAccessToken = token;
        _config.GitHubUsername = login;
        if (string.IsNullOrWhiteSpace(_config.Username))
            _config.Username = login;
        await SafeSaveConfigAsync();
    }

    // ===========================================================
    // Index + filter
    // ===========================================================

    public async Task LoadFileListFromCacheOrBuildAsync()
    {
        if (_root == null || _originalDir == null || _translatedDir == null)
            return;

        ClearViews();

        void WireSearchTab()
        {
            SetSearchContext?.Invoke(_root!, _originalDir!, _translatedDir!,
                relKey =>
                {
                    _allItemsByRel.TryGetValue(NormalizeRel(relKey), out var it);
                    return it != null ? (it.DisplayShort, it.Tooltip, it.Status) : (relKey, relKey, null);
                });

            SetSearchZenResolver?.Invoke(rel => _zenTexts.IsZen(rel));
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

            SetStatus("Building index cache...");

            var progress = new Progress<(int done, int total)>(p =>
            {
                SetStatus("Indexing files... " + p.done.ToString("n0") + "/" + p.total.ToString("n0"));
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

    public async Task RefreshAllCachedStatusesAsync()
    {
        if (_root == null || _originalDir == null || _translatedDir == null) return;

        bool changed = false;
        int total = _allItems.Count;

        var progress = new Progress<int>(done =>
            SetStatus($"Refreshing nav statuses... {done:n0}/{total:n0}"));

        await Task.Run(() =>
        {
            int done = 0;
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

                done++;
                if (done % 50 == 0)
                    ((IProgress<int>)progress).Report(done);
            }
            ((IProgress<int>)progress).Report(done);
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

    public async Task ApplyFilterSafeAsync()
    {
        try
        {
            await ApplyFilterAsync();
        }
        catch (TaskCanceledException) { }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            SetStatus("Filter failed: " + ex.Message);
        }
    }

    public async Task ApplyFilterAsync()
    {
        int myVersion = Interlocked.Increment(ref _navFilterVersion);

        try
        {
            try { _navSearchCts?.Cancel(); _navSearchCts?.Dispose(); } catch { }
            _navSearchCts = new CancellationTokenSource();
            var ct = _navSearchCts.Token;

            bool restoreFocus = IsNavSearchFocused?.Invoke() ?? false;

            string q = (GetNavSearchText?.Invoke() ?? "").Trim();
            string qLower = q.ToLowerInvariant();

            bool showFilenames = GetShowFilenames?.Invoke() ?? false;
            bool zenOnly = GetZenOnly?.Invoke() ?? false;
            int statusIdx = GetStatusFilterIndex?.Invoke() ?? 0;

            string? selectedRel = (GetNavSelectedItem?.Invoke())?.RelPath ?? _currentRelPath;

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

            SetNavItemsSource?.Invoke(_filteredItems);

            if (!string.IsNullOrWhiteSpace(selectedRel))
            {
                var match = _filteredItems.FirstOrDefault(x =>
                    string.Equals(x.RelPath, selectedRel, StringComparison.OrdinalIgnoreCase));
                if (match != null) SetNavSelectedItem?.Invoke(match);
            }

            if (restoreFocus)
                RestoreNavSearchFocus?.Invoke();
        }
        catch
        {
            throw;
        }
    }

    public void SelectInNav(string relPath)
    {
        if (string.IsNullOrWhiteSpace(relPath)) return;

        var match = _filteredItems.FirstOrDefault(x => string.Equals(x.RelPath, relPath, StringComparison.OrdinalIgnoreCase));
        if (match == null) return;

        SetNavSelectedItem?.Invoke(match);
    }

    public void ClearViews()
    {
        try { _navSearchCts?.Cancel(); } catch { }
        try { _navSearchCts?.Dispose(); } catch { }
        _navSearchCts = null;

        try { _renderCts?.Cancel(); } catch { }
        try { _renderCts?.Dispose(); } catch { }
        _renderCts = null;

        _rawOrigXml = "";
        _rawTranXml = "";
        _currentRelPath = null;
        _indexedDoc = null;

        _baselineTranSha1 = "";
        _lastSeenTranSha1 = "";
        _dirty = false;

        CurrentFileText = "";

        ClearReadable?.Invoke();
        ClearTranslation?.Invoke();
        ClearSearch?.Invoke();
        ClearScholar?.Invoke();

        SetNavItemsSource?.Invoke(new List<FileNavItem>());

        SetReadableZenContext?.Invoke(null, false);

        UpdateWindowTitle();
        UpdateSaveButtonState();
        SetGitSelectedRelPath?.Invoke(null);
    }

    // ===========================================================
    // File selection
    // ===========================================================

    public async Task OnFileSelectedAsync(FileNavItem item)
    {
        if (string.IsNullOrWhiteSpace(item.RelPath)) return;

        if (_currentRelPath != null && !string.Equals(_currentRelPath, item.RelPath, StringComparison.OrdinalIgnoreCase))
        {
            if (!await ConfirmNavigateIfDirtyAsync("switch files (" + _currentRelPath + " -> " + item.RelPath + ")"))
            {
                // Restore the previous selection
                var backRel = _currentRelPath;
                Dispatcher.UIThread.Post(() =>
                {
                    var back = _filteredItems.FirstOrDefault(x => string.Equals(x.RelPath, backRel, StringComparison.OrdinalIgnoreCase));
                    if (back != null) SetNavSelectedItem?.Invoke(back);
                }, DispatcherPriority.Background);
                return;
            }
        }

        await LoadPairAsync(item.RelPath);
    }

    // ===========================================================
    // Load/render
    // ===========================================================

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

    private async Task<string?> TryReadTranslatedXmlFromDiskAsync(string relPath)
    {
        if (_translatedDir == null) return null;

        try
        {
            var tranAbs = Path.Combine(_translatedDir, relPath);
            if (!File.Exists(tranAbs))
                return null;

            var text = await File.ReadAllTextAsync(tranAbs, Encoding.UTF8);

            if (TryParseXml(text, out _))
                return text;

            var bak = tranAbs + ".bak";
            if (File.Exists(bak))
            {
                var bakText = await File.ReadAllTextAsync(bak, Encoding.UTF8);
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

    public async Task LoadPairAsync(string relPath)
    {
        if (_originalDir == null || _translatedDir == null) return;

        _renderCts?.Cancel();
        _renderCts?.Dispose();
        _renderCts = new CancellationTokenSource();
        var ct = _renderCts.Token;

        _currentRelPath = relPath;
        _currentSegmentContext = null;

        UpdateTranslationTermHighlights?.Invoke(null, null);
        UpdateReadableTermHighlights?.Invoke(null, null, null, null);
        UpdateTranslationTmSharedHighlights?.Invoke(null, null, null);

        CurrentFileText = relPath;
        SetGitSelectedRelPath?.Invoke(_currentRelPath);

        SetStatus("Loading: " + relPath);

        _rawOrigXml = await ReadOriginalXmlAsync(relPath);

        await EnsureTranslatedXmlExistsForRelPathAsync(relPath);

        _rawTranXml = await TryReadTranslatedXmlFromDiskAsync(relPath) ?? _rawOrigXml;

        _indexedDoc = _indexedTranslation.BuildIndex(_rawOrigXml, _rawTranXml);

        var projection = _indexedTranslation.RenderProjection(_indexedDoc, _translationMode);
        SetTranslationProjection(_translationMode, projection);

        _baselineTranSha1 = Sha1Hex(projection);
        _lastSeenTranSha1 = _baselineTranSha1;
        _dirty = false;
        UpdateWindowTitle();
        UpdateSaveButtonState();

        // Signal that core data (projection editor) is ready — allows the window
        // to appear immediately while the slower readable render continues below.
        SignalCoreLoadComplete?.Invoke();

        SetStatus("Rendering readable view...");

        // Render the readable view FIRST (user sees this tab immediately)
        // Assistant build is deferred to AFTER render to avoid I/O contention
        try
        {
            var swRender = System.Diagnostics.Stopwatch.StartNew();

            var (ro, rt) = await Task.Run(async () =>
            {
                ct.ThrowIfCancellationRequested();
                return await RenderReadablePairDiskOnlyAsync(relPath, ct);
            }, ct);

            swRender.Stop();
            if (ct.IsCancellationRequested) return;

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                SetReadableRendered?.Invoke(ro, rt);
            });

            try
            {
                bool isZen = _root != null && _zenTexts.IsZen(relPath);
                SetReadableZenContext?.Invoke(relPath, isZen);
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[MainWindowViewModel] Zen context set failed: {ex.Message}"); }

            await SaveUiStateAsync();
            SetStatus("Loaded. Segments: O=" + ro.Segments.Count.ToString("n0") +
                      ", T=" + rt.Segments.Count.ToString("n0") +
                      ". Render=" + swRender.ElapsedMilliseconds.ToString("n0") + "ms");

            _ = RefreshProgressStatsAsync(); // Don't await — don't freeze UI
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            SetStatus("Render failed: " + ex.Message);
        }

        // Assistant + progress stats refresh in background (don't freeze UI)
        _ = Task.Run(async () =>
        {
            try { await RefreshAssistantFromEditorAsync(); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[MainWindowViewModel] Assistant refresh failed: {ex.Message}"); }
        });
    }

    private async Task RefreshAssistantFromEditorAsync()
    {
        try
        {
            if (_currentRelPath == null)
                return;

            var text = GetTranslationProjectionText?.Invoke() ?? "";
            if (string.IsNullOrWhiteSpace(text))
            {
                SetAssistantSnapshot?.Invoke(null);
                SetCurrentReviewState?.Invoke(null, null, null, null);
                return;
            }

            var snapshot = await _translationAssistant.BuildSnapshotAsync(
                new CurrentSegmentContext
                {
                    RelPath = _currentRelPath,
                    BlockNumber = 0,
                    ZhText = "",
                    EnText = "",
                    ProjectionOffsetStart = 0,
                    ProjectionOffsetEndExclusive = 0,
                    Mode = _translationMode
                },
                _root,
                _originalDir,
                _translatedDir);

            SetAssistantSnapshot?.Invoke(snapshot);
            await RefreshReviewBadgeAsync();
        }
        catch
        {
            // assistant must never break file loading
        }
    }

    // ===========================================================
    // Assistant + review
    // ===========================================================

    public async Task RefreshAssistantForCurrentSegmentAsync(CurrentProjectionSegmentChangedEventArgs ev)
    {
        try
        {
            if (_currentRelPath == null) return;

            try { _assistantCts?.Cancel(); } catch { }
            try { _assistantCts?.Dispose(); } catch { }
            _assistantCts = new CancellationTokenSource();
            var ct = _assistantCts.Token;

            // Clear stale assistant content immediately so the user doesn't see old data
            SetAssistantSnapshot?.Invoke(null);

            var ctx = new CurrentSegmentContext
            {
                RelPath = _currentRelPath,
                BlockNumber = ev.BlockNumber,
                ZhText = ev.Zh,
                ZhContextText = ev.ZhContext,
                EnText = ev.En,
                ProjectionOffsetStart = ev.BlockStartOffset,
                ProjectionOffsetEndExclusive = ev.BlockEndOffsetExclusive,
                Mode = ev.Mode
            };

            _currentSegmentContext = ctx;

            var snapshot = await _translationAssistant.BuildSnapshotAsync(
                ctx,
                _root,
                _originalDir,
                _translatedDir,
                ct);

            if (ct.IsCancellationRequested) return;

            SetAssistantSnapshot?.Invoke(snapshot);
            MaybeAutoFillFromExactMatch(snapshot);
            UpdateTranslationTermHighlights?.Invoke(snapshot?.Terms, _currentSegmentContext?.ZhText);
            UpdateTranslationTmSharedHighlights?.Invoke(snapshot?.ApprovedMatches, snapshot?.ReferenceMatches, _currentSegmentContext?.ZhText);

            int? readableOccurrenceHint =
                _currentSegmentContext != null && _currentSegmentContext.BlockNumber > 0
                    ? _currentSegmentContext.BlockNumber - 1
                    : null;
            string? readableAnchorSignal = string.IsNullOrWhiteSpace(_currentSegmentContext?.ZhContextText)
                ? null
                : _currentSegmentContext.ZhContextText;
            UpdateReadableTermHighlights?.Invoke(
                snapshot?.Terms,
                _currentSegmentContext?.ZhText,
                readableOccurrenceHint,
                readableAnchorSignal);

            await RefreshReviewBadgeAsync();
        }
        catch
        {
            // assistant errors must never break translation
        }
    }

    private void MaybeAutoFillFromExactMatch(TranslationAssistantSnapshot? snapshot)
    {
        if (snapshot == null) return;
        if (!string.IsNullOrWhiteSpace(snapshot.Segment.EnText)) return;
        var exact = snapshot.ApprovedMatches?.FirstOrDefault(m => m.Score >= 100);
        if (exact == null) return;
        FillEnForCurrentBlock?.Invoke(exact.TargetText, snapshot.Segment.BlockNumber);
    }

    public async Task RefreshProgressStatsAsync()
    {
        if (string.IsNullOrWhiteSpace(_root) || string.IsNullOrWhiteSpace(_currentRelPath))
        {
            SetProgressStats?.Invoke(0, 0, 0);
            return;
        }

        var map = await _translationReview.LoadLatestEntriesAsync(_root);
        var entries = map.Values
            .Where(e => NormalizeRel(e.RelPath) == NormalizeRel(_currentRelPath)
                     && e.Mode == _translationMode.ToString())
            .ToList();
        int approved = entries.Count(e => e.Status == TranslationReviewStatuses.Approved);
        int needsWork = entries.Count(e => e.Status == TranslationReviewStatuses.NeedsWork);
        int total = GetAllBlockNumbers?.Invoke()?.Count ?? 0;
        SetProgressStats?.Invoke(approved, needsWork, total);
    }

    private async Task RefreshReviewBadgeAsync()
    {
        try
        {
            if (_root == null || _currentSegmentContext == null)
            {
                SetCurrentReviewState?.Invoke(null, null, null, null);
                return;
            }

            var latest = await _translationReview.GetLatestEntryAsync(_root, _currentSegmentContext);
            var segKey = TranslationReviewService.BuildSegmentKey(
                _currentSegmentContext.RelPath, _currentSegmentContext.Mode, _currentSegmentContext.BlockNumber);
            var agg = _translationReview.GetAggregatedReview(segKey);
            SetCurrentReviewState?.Invoke(latest?.Status, latest?.Reviewer, latest?.ReviewedUtc, agg);
        }
        catch
        {
            SetCurrentReviewState?.Invoke(null, null, null, null);
        }
    }

    public async Task HandleReviewActionAsync(string status)
    {
        try
        {
            if (_root == null)
            {
                SetStatus("Review failed: no root is loaded.");
                return;
            }

            if (_currentSegmentContext == null)
            {
                SetStatus("Review failed: no current segment.");
                return;
            }

            _currentSegmentContext.EnText = _currentSegmentContext.EnText ?? "";

            await _translationReview.AppendReviewAsync(
                _root,
                _currentSegmentContext,
                status,
                reviewer: _config.Username ?? "User");

            int count = await _translationReview.RebuildApprovedTranslationMemoryAsync(_root);

            var reviewsDir = ITranslationReviewService.GetCommunityReviewsDir(_root);
            await _translationReview.RefreshAggregationCacheAsync(_root, reviewsDir);

            var latest = await _translationReview.GetLatestEntryAsync(_root, _currentSegmentContext);
            var segKey = TranslationReviewService.BuildSegmentKey(
                _currentSegmentContext.RelPath, _currentSegmentContext.Mode, _currentSegmentContext.BlockNumber);
            var agg = _translationReview.GetAggregatedReview(segKey);
            SetCurrentReviewState?.Invoke(latest?.Status, latest?.Reviewer, latest?.ReviewedUtc, agg);

            await RefreshAssistantForCurrentSegmentAsync(new CurrentProjectionSegmentChangedEventArgs
            {
                BlockNumber = _currentSegmentContext.BlockNumber,
                Zh = _currentSegmentContext.ZhText,
                En = _currentSegmentContext.EnText,
                BlockStartOffset = _currentSegmentContext.ProjectionOffsetStart,
                BlockEndOffsetExclusive = _currentSegmentContext.ProjectionOffsetEndExclusive,
                Mode = _currentSegmentContext.Mode
            });

            SetStatus($"Segment <{_currentSegmentContext.BlockNumber}> marked {TranslationReviewStatuses.Normalize(status)}. Approved TM rows: {count:n0}.");

            if (status == TranslationReviewStatuses.Approved)
            {
                var reviewMap = await _translationReview.LoadLatestEntriesAsync(_root);
                var currentRel = NormalizeRel(_currentRelPath ?? "");
                var approvedBlocks = reviewMap.Values
                    .Where(e => NormalizeRel(e.RelPath) == currentRel
                             && e.Mode == _currentSegmentContext.Mode.ToString()
                             && e.Status == TranslationReviewStatuses.Approved)
                    .Select(e => e.BlockNumber)
                    .ToHashSet();
                JumpToNextUnapproved?.Invoke(approvedBlocks);
            }

            await RefreshProgressStatsAsync();
        }
        catch (Exception ex)
        {
            SetStatus("Review failed: " + ex.Message);
        }
    }

    public async Task HandleNextUnapprovedAsync()
    {
        try
        {
            if (_root == null || _currentRelPath == null) return;
            var map = await _translationReview.LoadLatestEntriesAsync(_root);
            var approvedBlocks = map.Values
                .Where(e => NormalizeRel(e.RelPath) == NormalizeRel(_currentRelPath)
                         && e.Mode == _translationMode.ToString()
                         && e.Status == TranslationReviewStatuses.Approved)
                .Select(e => e.BlockNumber)
                .ToHashSet();
            JumpToNextUnapproved?.Invoke(approvedBlocks);
        }
        catch (Exception ex)
        {
            SetStatus("Next unapproved failed: " + ex.Message);
        }
    }

    // ===========================================================
    // Zen flag change
    // ===========================================================

    public async Task HandleZenFlagChangedAsync(string relPath, bool isZen)
    {
        try
        {
            if (_root == null) return;
            await _zenTexts.SetZenAsync(_root, relPath, isZen);
            SetStatus(isZen ? "Marked as Zen text." : "Unmarked as Zen text.");
            await ApplyFilterSafeAsync();
        }
        catch (Exception ex)
        {
            SetStatus("Zen toggle failed: " + ex.Message);
        }
    }

    // ===========================================================
    // Community notes handlers (DISK-TEI ONLY)
    // ===========================================================

    public async Task OnCommunityNoteInsertRequestedAsync(int xmlIndex, string noteText, string? resp)
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

    public async Task OnCommunityNoteDeleteRequestedAsync(int xmlStart, int xmlEndExclusive)
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

    public async Task OnFootnoteMoveRequestedAsync(ReadableTabViewModel.MoveFootnoteRequest req)
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

            if (newIndex >= oldS && newIndex <= oldE)
                newIndex = oldS;

            string noteXml = baseXml.Substring(oldS, oldE - oldS);

            if (!LooksLikeNoteElement(noteXml))
            {
                SetStatus("Move footnote refused: source span does not look like a <note> element.");
                return;
            }

            string withoutOld = baseXml.Remove(oldS, oldE - oldS);

            int removedLen = oldE - oldS;
            int adjustedNewIndex =
                (newIndex <= oldS)
                    ? newIndex
                    : newIndex - removedLen;

            adjustedNewIndex = Math.Clamp(adjustedNewIndex, 0, withoutOld.Length);

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
        if (!s.StartsWith("<note", StringComparison.OrdinalIgnoreCase)) return false;
        if (s.IndexOf("</note>", StringComparison.OrdinalIgnoreCase) < 0) return false;
        return true;
    }

    private static string BuildCommunityNoteXml(string noteText, string? resp)
    {
        string inner = EscapeXmlText((noteText ?? "").Trim());
        if (inner.Length == 0) inner = "...";

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

    // ===========================================================
    // Translator annotation
    // ===========================================================

    internal static string ApplyTranslatorAnnotation(
        string xml,
        IReadOnlyList<int> changedBlocks,
        string username)
    {
        RenderedDocument rendered;
        try { rendered = CbetaTeiRenderer.Render(xml); }
        catch { return xml; }

        DocAnnotation? existing = null;
        foreach (var ann in rendered.Annotations)
        {
            if (ann.Start > 500) break;
            if (!ann.IsCommunity) continue;
            if (!string.Equals(ann.Resp, username, StringComparison.OrdinalIgnoreCase)) continue;
            if (!ann.Text.StartsWith("translated blocks ", StringComparison.OrdinalIgnoreCase)) continue;
            if (!ann.HasXmlSpan) continue;
            existing = ann;
            break;
        }

        var allBlocks = new HashSet<int>(changedBlocks);
        if (existing != null)
            foreach (var n in ParseAnnotationBlockNumbers(existing.Text))
                allBlocks.Add(n);

        if (allBlocks.Count == 0) return xml;

        var annotationText = FormatBlockRanges(allBlocks);

        if (existing != null)
        {
            xml = CommunityNoteXmlEditor.DeleteSpan(xml, existing.XmlStart, existing.XmlEndExclusive);
            xml = CommunityNoteXmlEditor.InsertCommunityNote(xml, existing.XmlStart, annotationText, username);
        }
        else
        {
            int insertPos = 0;
            if (rendered.BaseToXmlIndex is { Length: > 0 })
            {
                int firstNl = rendered.Text.IndexOf('\n');
                int insertDisplay = (firstNl >= 0 && firstNl + 1 < rendered.Text.Length)
                    ? firstNl + 1
                    : Math.Max(1, rendered.Text.Length / 2);
                int xmlIdx = rendered.DisplayIndexToXmlIndex(insertDisplay);
                if (xmlIdx > 0) insertPos = xmlIdx;
            }
            xml = CommunityNoteXmlEditor.InsertCommunityNote(xml, insertPos, annotationText, username);
        }

        return xml;
    }

    internal static List<int> GetChangedBlockNumbers(IndexedTranslationDocument indexedDoc, TranslationEditMode mode)
    {
        var kindFilter = mode switch
        {
            TranslationEditMode.Head => CbetaTranslator.App.Services.TranslationUnitKind.Head,
            TranslationEditMode.Notes => CbetaTranslator.App.Services.TranslationUnitKind.Note,
            _ => CbetaTranslator.App.Services.TranslationUnitKind.Body
        };

        var units = indexedDoc.Units.Where(u => u.Kind == kindFilter).ToList();
        var changed = new List<int>();

        for (int i = 0; i < units.Count; i++)
        {
            var u = units[i];
            if (string.IsNullOrWhiteSpace(u.En)) continue;
            if (string.Equals(u.En.Trim(), u.EnBaseline.Trim(), StringComparison.Ordinal)) continue;
            changed.Add(i + 1);
        }

        return changed;
    }

    internal static IEnumerable<int> ParseAnnotationBlockNumbers(string text)
    {
        foreach (Match m in Regex.Matches(text, @"(\d+)(?:-(\d+))?"))
        {
            int start = int.Parse(m.Groups[1].Value);
            int end = m.Groups[2].Success ? int.Parse(m.Groups[2].Value) : start;
            for (int i = start; i <= end; i++)
                yield return i;
        }
    }

    internal static string FormatBlockRanges(IEnumerable<int> blocks)
    {
        var sorted = blocks.Distinct().OrderBy(x => x).ToList();
        if (sorted.Count == 0) return "";

        var ranges = new List<(int Start, int End)>();
        int rs = sorted[0], re = sorted[0];
        for (int i = 1; i < sorted.Count; i++)
        {
            if (sorted[i] == re + 1) { re = sorted[i]; }
            else { ranges.Add((rs, re)); rs = re = sorted[i]; }
        }
        ranges.Add((rs, re));

        var sb = new StringBuilder("translated blocks ");
        for (int i = 0; i < ranges.Count; i++)
        {
            if (i > 0) sb.Append(", ");
            var (s, e) = ranges[i];
            if (s == e) sb.Append(s);
            else sb.Append($"{s}-{e}");
        }
        return sb.ToString();
    }

    // ===========================================================
    // Write + rerender
    // ===========================================================

    private async Task WriteTranslatedDiskAndRerenderAsync(string relPath, string updatedXml, string why)
    {
        if (_translatedDir == null) return;

        EnsureXmlIsWellFormed(updatedXml, "Updated translated XML is not well-formed.");

        var tranAbs = Path.Combine(_translatedDir, relPath);
        var saveInfo = await AtomicWriteXmlAsync(tranAbs, updatedXml);

        _rawTranXml = updatedXml;

        try { _renderCache.Invalidate(tranAbs); } catch { }

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

    private async Task RefreshReadableFromDiskOnlyAsync()
    {
        if (_currentRelPath == null || _originalDir == null || _translatedDir == null) return;

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

            await Dispatcher.UIThread.InvokeAsync(() => SetReadableRendered?.Invoke(ro, rt));
            SetStatus("Readable refreshed (disk XML).");
        }
        catch (OperationCanceledException) { }
        catch (Exception ex) { SetStatus("Readable refresh failed: " + ex.Message); }
    }

    // ===========================================================
    // Save/revert
    // ===========================================================

    [RelayCommand]
    public async Task SaveTranslatedFromTabAsync()
    {
        try
        {
            if (_currentRelPath == null) { SetStatus("Nothing to save."); return; }
            if (_translatedDir == null) { SetStatus("Save unavailable."); return; }
            if (_indexedDoc == null) { SetStatus("Translation index not loaded."); return; }

            var editedProjection = GetTranslationProjectionText?.Invoke() ?? "";

            _indexedTranslation.ApplyProjectionEdits(_indexedDoc, _translationMode, editedProjection);

            var changedBlocks = string.IsNullOrWhiteSpace(_config.Username)
                ? null
                : GetChangedBlockNumbers(_indexedDoc, _translationMode);

            var builtXml = _indexedTranslation.BuildTranslatedXml(_indexedDoc, out var updatedCount);

            if (changedBlocks != null && changedBlocks.Count > 0)
                builtXml = ApplyTranslatorAnnotation(builtXml, changedBlocks, _config.Username);

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

            try { await RefreshReadableFromDiskOnlyAsync(); }
            catch (Exception refreshEx)
            {
                // Post-save refresh can fail on Mac (file access timing) — don't alarm the user
                System.Diagnostics.Debug.WriteLine($"[SaveXml] Post-save refresh failed (non-critical): {refreshEx.Message}");
            }

            var backupMsg = saveInfo.BackupCreated ? " backup=yes" : " backup=no";
            SetStatus("Saved translated XML (" + updatedCount.ToString("n0") + " units updated)." + backupMsg);
        }
        catch (Exception ex)
        {
            SetStatus("Save failed: " + ex.Message);
        }
    }

    public async Task RevertTranslatedXmlFromDiskAsync()
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

            _rawTranXml = await TryReadTranslatedXmlFromDiskAsync(_currentRelPath) ?? _rawOrigXml;

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

    // ===========================================================
    // Dirty tracking
    // ===========================================================

    public void UpdateSaveButtonState()
    {
        bool hasFile = _currentRelPath != null;
        bool translationTabSelected = (GetSelectedTabIndex?.Invoke() ?? -1) == 1;
        SetSaveButtonEnabled?.Invoke(hasFile && translationTabSelected);
    }

    /// <summary>
    /// Called by the dirty timer tick in code-behind.
    /// </summary>
    public void CheckDirtyTick()
    {
        if (_currentRelPath == null) return;

        string cur;
        try { cur = GetTranslationProjectionText?.Invoke() ?? ""; }
        catch { return; }

        var sha = Sha1Hex(cur);
        if (sha == _lastSeenTranSha1) return;

        _lastSeenTranSha1 = sha;
        UpdateDirtyStateFromEditor(forceUi: true);
    }

    public void UpdateDirtyStateFromEditor(bool forceUi)
    {
        if (_currentRelPath == null)
        {
            if (forceUi) UpdateWindowTitle();
            return;
        }

        string cur;
        try { cur = GetTranslationProjectionText?.Invoke() ?? ""; }
        catch { cur = ""; }

        bool dirtyNow = Sha1Hex(cur) != (_baselineTranSha1 ?? "");
        if (dirtyNow == _dirty && !forceUi) return;

        _dirty = dirtyNow;
        UpdateWindowTitle();
    }

    public void SetBaselineFromCurrentTranslatedEditorText()
    {
        string cur;
        try { cur = GetTranslationProjectionText?.Invoke() ?? ""; }
        catch { cur = ""; }

        _baselineTranSha1 = Sha1Hex(cur);
        _lastSeenTranSha1 = _baselineTranSha1;
        _dirty = false;
        UpdateWindowTitle();
    }

    public void CaptureTranslationEditsToRaw()
    {
        if (_indexedDoc == null) return;

        try
        {
            var projection = GetTranslationProjectionText?.Invoke() ?? "";
            _indexedTranslation.ApplyProjectionEdits(_indexedDoc, _translationMode, projection);
            _rawTranXml = _indexedTranslation.BuildTranslatedXml(_indexedDoc, out _);
        }
        catch
        {
            // ignore during navigation prompts
        }
    }

    public void UpdateWindowTitle()
    {
        var file = _currentRelPath ?? "";
        var star = _dirty ? "*" : "";
        var title = string.IsNullOrWhiteSpace(file) ? (AppTitleBase + star) : (AppTitleBase + star + " — " + file);
        WindowTitle = title;
        SetWindowTitle?.Invoke(title);

        CurrentFileText = string.IsNullOrWhiteSpace(file) ? "" : (file + (_dirty ? "  *" : ""));
    }

    public async Task<bool> ConfirmNavigateIfDirtyAsync(string action)
    {
        CaptureTranslationEditsToRaw();
        UpdateDirtyStateFromEditor(forceUi: true);

        if (!_dirty) return true;

        return await (ShowYesNoDialogAsync?.Invoke("Unsaved changes", "You have unsaved changes.\n\nProceed to " + action + "?")
            ?? Task.FromResult(true));
    }

    public async Task OnTabSelectionChangedAsync()
    {
        int newIdx = GetSelectedTabIndex?.Invoke() ?? 0;
        int oldIdx = _lastTabIndex;
        _lastTabIndex = newIdx;

        // Save scholar state silently when leaving the Scholar tab
        bool leavingScholar = oldIdx == 4 && newIdx != 4;
        if (leavingScholar)
        {
            try { if (SaveScholarStateAsync != null) await SaveScholarStateAsync(); } catch { }
        }

        bool leavingTranslation = oldIdx == 1 && newIdx != 1;
        if (leavingTranslation)
        {
            CaptureTranslationEditsToRaw();
            UpdateDirtyStateFromEditor(forceUi: true);

            if (_dirty)
            {
                bool proceed = await (ShowYesNoDialogAsync?.Invoke("Unsaved changes", "You have unsaved changes.\n\nLeave the Translation tab anyway?")
                    ?? Task.FromResult(true));
                if (!proceed)
                {
                    ForceTabIndex?.Invoke(1);
                    _lastTabIndex = 1;
                    return;
                }
            }
        }

        UpdateSaveButtonState();
        UpdateDirtyStateFromEditor(forceUi: true);

        if (!_isAutoIndexing)
            QueueAutoIndexBuild();
    }

    public void SetLastTabIndex(int idx)
    {
        _lastTabIndex = idx;
    }

    // ===========================================================
    // Mode change
    // ===========================================================

    public void HandleModeChanged(TranslationEditMode mode)
    {
        try
        {
            if (_indexedDoc == null) return;

            var currentProjection = GetTranslationProjectionText?.Invoke() ?? "";
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
    }

    // ===========================================================
    // Navigation
    // ===========================================================

    public void HandleNavigationRequested(NavigationRequest req)
    {
        if (_root == null) return;
        WindowNavigationService.OpenAndNavigate(_root, req);
    }

    // ===========================================================
    // Git helper
    // ===========================================================

    public async Task<bool> EnsureTranslatedXmlForRelPathAsync(string relPath, bool saveCurrentEditor)
    {
        if (_originalDir == null || _translatedDir == null) return false;

        var origPath = Path.Combine(_originalDir, relPath);
        if (!File.Exists(origPath)) return false;

        if (saveCurrentEditor &&
            _indexedDoc != null &&
            string.Equals(_currentRelPath, relPath, StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var projection = GetTranslationProjectionText?.Invoke() ?? "";
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

    public async Task HandleRootClonedAsync(string repoRoot, bool isSecondaryWindow)
    {
        try
        {
            if (!await ConfirmNavigateIfDirtyAsync("load a different root")) return;
            await LoadRootAsync(repoRoot, saveToConfig: true);
            SetStatus("Refreshing nav statuses after git update...");
            await RefreshAllCachedStatusesAsync();
            await ApplyFilterSafeAsync();
            ForceTabIndex?.Invoke(0);
        }
        catch (Exception ex)
        {
            SetStatus("Failed to load cloned repo: " + ex.Message);
        }
    }

    public async Task RefreshReviewAggregationAsync()
    {
        if (string.IsNullOrWhiteSpace(_root)) return;
        try
        {
            var reviewsDir = ITranslationReviewService.GetCommunityReviewsDir(_root);
            await _translationReview.RefreshAggregationCacheAsync(_root, reviewsDir);
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[MainWindowViewModel] Review aggregation failed: {ex.Message}"); }
    }

    // ===========================================================
    // Build reference TM
    // ===========================================================

    public async Task BuildReferenceTmAsync()
    {
        try
        {
            if (_root == null || _originalDir == null || _translatedDir == null)
            {
                SetStatus("Cannot build reference TM: no root is loaded.");
                return;
            }

            SetStatus("Building reference TM from Zen texts...");

            var progress = new Progress<(int done, int total, string status)>(p =>
            {
                if (p.total > 0)
                    SetStatus($"{p.status} ({p.done:n0}/{p.total:n0})");
                else
                    SetStatus(p.status);
            });

            int count = await _translationAssistantBuilder.BuildReferenceTranslationMemoryAsync(
                _root,
                _originalDir,
                _translatedDir,
                rel => _zenTexts.IsZen(rel),
                progress);

            SetStatus($"Built translation-memory.reference.jsonl with {count:n0} rows.");
        }
        catch (Exception ex)
        {
            SetStatus("Build reference TM failed: " + ex.Message);
        }
    }

    // ===========================================================
    // Termbase editor
    // ===========================================================

    public async Task OpenTermbaseEditorAsync()
    {
        // This remains partially in code-behind because it creates a Window.
        // VM signals intent; code-behind creates the TermbaseEditorWindow.
        // The bridge delegate handles the actual window creation.
        if (string.IsNullOrWhiteSpace(_root))
        {
            SetStatus("Cannot open termbase editor: no root is loaded.");
            return;
        }

        OpenTermbaseEditorRequested?.Invoke(_root, _config.Username);
    }

    /// <summary>
    /// Event for code-behind to handle termbase editor window creation.
    /// </summary>
    public Action<string, string?>? OpenTermbaseEditorRequested { get; set; }

    // ===========================================================
    // TranslationTabView projection helpers
    // ===========================================================

    private void SetTranslationProjection(TranslationEditMode mode, string projectionText)
    {
        TrySetCurrentFilePaths();
        SetTranslationModeProjection?.Invoke(mode, projectionText ?? "");
    }

    private void TrySetCurrentFilePaths()
    {
        if (_originalDir == null || _translatedDir == null || _currentRelPath == null) return;

        try
        {
            var origAbs = Path.Combine(_originalDir, _currentRelPath);
            var tranAbs = Path.Combine(_translatedDir, _currentRelPath);
            SetTranslationFilePaths?.Invoke(origAbs, tranAbs);
        }
        catch { }
    }

    /// <summary>
    /// Resolves a relative path to its display title from the nav index.
    /// </summary>
    public string ResolveAssistantTitle(string rel)
    {
        var key = NormalizeRel(rel);
        if (_allItemsByRel.TryGetValue(key, out var item))
        {
            if (!string.IsNullOrWhiteSpace(item.Tooltip)) return item.Tooltip;
            if (!string.IsNullOrWhiteSpace(item.DisplayShort)) return item.DisplayShort;
            if (!string.IsNullOrWhiteSpace(item.FileName)) return item.FileName;
        }
        return rel ?? "";
    }

    /// <summary>
    /// Called when the TranslationTabView fires TermsSaved.
    /// </summary>
    public void HandleTermsSaved()
    {
        SetStatus("Saved termbase.json");
        SetAssistantSnapshot?.Invoke(null);
    }

    // ===========================================================
    // OpenAtAsync support (for secondary windows)
    // ===========================================================

    public async Task OpenAtCoreAsync(string root, NavigationRequest request)
    {
        await LoadRootAsync(root, saveToConfig: false);
        SelectInNav(request.RelPath);
        await LoadPairAsync(request.RelPath);
        ForceTabIndex?.Invoke(0); // switch to Reader tab

        NavigateInReadable?.Invoke(request);
    }

    // ===========================================================
    // Atomic XML save helpers
    // ===========================================================

    internal sealed class AtomicSaveInfo
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

    internal static async Task<AtomicSaveInfo> AtomicWriteXmlAsync(string finalPath, string xml)
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

    // ===========================================================
    // Index cache dirty tracking
    // ===========================================================

    public void MarkIndexCacheDirty()
    {
        _indexCacheDirty = true;
        ScheduleIndexCacheSave?.Invoke();
    }

    public async Task SaveIndexCacheIfDirtyAsync()
    {
        if (!_indexCacheDirty) return;
        if (_root == null) return;

        _indexCacheDirty = false;

        try
        {
            await _indexCacheService.SaveAsync(_root, new IndexCache { Entries = _allItems });
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[MainWindowViewModel] Index cache save failed: {ex.Message}"); }
    }

    // ===========================================================
    // Utils
    // ===========================================================

    public static string NormalizeRel(string p) => (p ?? "").Replace('\\', '/').TrimStart('/');

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
}
