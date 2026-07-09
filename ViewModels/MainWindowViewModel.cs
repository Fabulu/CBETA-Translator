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
using Avalonia;
using Avalonia.Threading;
using ReadZen.App.Infrastructure;
using ReadZen.App.Models;
using ReadZen.App.Services;
using ReadZen.App.Text;
using Microsoft.Extensions.DependencyInjection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;

namespace ReadZen.App.ViewModels;

public enum StatusSeverity { Info, Success, Warning, Error }

public partial class MainWindowViewModel : ViewModelBase
{
    private const string AppTitleBase = "Read Zen";

    // ---- Services (injected) ----
    private readonly IAppConfigService _configService;
    private readonly IIndexCacheService _indexCacheService;
    private readonly IRenderedDocumentCacheService _renderCache;
    private readonly ILicenseMetadataService _licenseMetadata;
    private readonly IManifestService _manifestService;
    private readonly IZenTextsService _zenTexts;
    private readonly IIndexedTranslationService _indexedTranslation;
    private readonly ITranslationAssistantService _translationAssistant;
    private readonly ITranslationAssistantBuildService _translationAssistantBuilder;
    private readonly ITranslationReviewService _translationReview;
    private readonly ISearchIndexService _searchIndex;
    private readonly IDocumentTagService _documentTagService;
    private readonly IGitRepoService _gitService;
    private ITranslationStarService? _starService;
    private static readonly TranslationStatusService LiveTranslationStatusService = new();

    // Coding mode state
    private TagVocabulary? _tagVocabulary;
    private List<DocumentTag> _appliedTags = new();
    private readonly SemaphoreSlim _tagSaveLock = new(1, 1);

    // ---- Internal state ----
    private IndexedTranslationDocument? _indexedDoc;
    private TranslationEditMode _translationMode = TranslationEditMode.Body;

    private AppConfig _config = new() { IsDarkTheme = true };
    public AppConfig Config => _config;

    private string? _root, _translationRoot, _originalDir, _translatedDir, _originalsRepoRoot;
    private string? _translatedCacheDir;
    // All corpus layouts found under _root (CBETA + Open siblings inside one
    // parent folder). Empty when the root is a legacy single-pair layout.
    private IReadOnlyList<CorpusLayout> _availableCorpora = System.Array.Empty<CorpusLayout>();
    public IReadOnlyList<CorpusLayout> AvailableCorpora
    {
        get => _availableCorpora;
        private set
        {
            _availableCorpora = value ?? System.Array.Empty<CorpusLayout>();
            OnPropertyChanged(nameof(AvailableCorpora));
        }
    }
    private string? _userTranslatedDir;   // community/translations/{username}/
    private string? _activeTranslatedDir; // currently selected dir (user, community, or other user)
    private readonly Dictionary<string, MeaningfulTranslationCacheEntry> _meaningfulTranslationCache = new(StringComparer.OrdinalIgnoreCase);
    public string? Root => _root;
    public string? TranslationRoot => _translationRoot;
    public string? Username => _config.Username;
    public string? OriginalDir => _originalDir;
    public string? TranslatedDir => _translatedDir;

    private string? _currentRelPath;
    public string? CurrentRelPath => _currentRelPath;

    private List<FileNavItem> _allItems = new();
    private List<FileNavItem> _filteredItems = new();
    private readonly Dictionary<string, FileNavItem> _allItemsByRel = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _navItemsLock = new();

    public List<FileNavItem> FilteredItems => _filteredItems;
    public Dictionary<string, FileNavItem> AllItemsByRel => _allItemsByRel;

    private CancellationTokenSource? _navSearchCts;
    private CancellationTokenSource? _renderCts;
    private bool _forceRebuildIndex;
    private CancellationTokenSource? _assistantCts;
    private CancellationTokenSource? _readerStudyCts;
    private CancellationTokenSource? _autoIndexCts;
    private bool _isAutoIndexing;

    // Nav filter performance / race control
    private int _navFilterVersion;

    private CancellationToken ResetRenderCts()
    {
        var old = _renderCts;
        _renderCts = new CancellationTokenSource();
        try { old?.Cancel(); } catch { }
        try { old?.Dispose(); } catch { }
        return _renderCts.Token;
    }

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
    public void SuppressConfigSavesForSecondaryWindow() => _suppressConfigSaves = true;
    private bool _suppressNavSelection;
    private bool _userHasManuallySelectedSource;

    private sealed record MeaningfulTranslationCacheEntry(
        DateTime OriginalWriteUtc,
        DateTime CandidateWriteUtc,
        long CandidateLength,
        bool IsMeaningful);

    private sealed record TranslationSourceEvaluation(
        int Index,
        string? Path,
        TranslationStatus Status,
        bool IsCommunity,
        long TranslatedMtimeTicks,
        DateTime LastWriteUtc,
        int StarCount = 0);

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

    private CorpusKind _activeCorpus = CorpusKind.Cbeta;
    public CorpusKind ActiveCorpus
    {
        get => _activeCorpus;
        private set
        {
            if (_activeCorpus == value) return;
            _activeCorpus = value;
            OnPropertyChanged(nameof(ActiveCorpus));
            OnPropertyChanged(nameof(CorpusBadgeLabel));
            OnPropertyChanged(nameof(CorpusBadgeBgKey));
            OnPropertyChanged(nameof(CorpusBadgeFgKey));
        }
    }

    public string CorpusBadgeLabel => _activeCorpus switch
    {
        CorpusKind.Open  => "OpenZen",
        CorpusKind.Cbeta => "CBETA",
        _                => "Unknown corpus"
    };

    /// <summary>DynamicResource key the view should use for the badge background.</summary>
    public string CorpusBadgeBgKey => _activeCorpus switch
    {
        CorpusKind.Open  => "SuccessBg",
        CorpusKind.Cbeta => "WarningBg",
        _                => "BarBg"
    };

    public string CorpusBadgeFgKey => _activeCorpus switch
    {
        CorpusKind.Open  => "SuccessFg",
        CorpusKind.Cbeta => "WarningFg",
        _                => "TextMutedFg"
    };

    /// <summary>
    /// Like <see cref="IZenTextsService.IsZen"/>, but also returns true for
    /// every file in the OpenZen corpus. OpenZen is curated as a
    /// pure Zen text collection — there's no need for users to manually
    /// flag each text. CBETA still uses the explicit per-file zen_texts.json
    /// list because its scope is much broader than just Zen.
    /// </summary>
    public bool IsZenOrOpenCorpusFile(string relPath)
    {
        if (_activeCorpus == CorpusKind.Open) return true;
        return _zenTexts.IsZen(relPath);
    }

    /// <summary>Called by ReadableTabView (via MainWindow) to render the per-file license chip.</summary>
    public TextLicenseInfo? GetLicenseForCurrentFile()
    {
        if (_originalDir == null || string.IsNullOrWhiteSpace(_currentRelPath))
            return null;
        var abs = System.IO.Path.Combine(_originalDir, _currentRelPath);
        return _licenseMetadata.TryGet(abs, out var info) ? info : null;
    }

    /// <summary>Loads manifest.json from the current file's directory (null for CBETA files).</summary>
    public ManifestInfo? GetManifestForCurrentFile()
    {
        if (_originalDir == null || string.IsNullOrWhiteSpace(_currentRelPath))
            return null;
        var abs = System.IO.Path.Combine(_originalDir, _currentRelPath);
        return _manifestService.TryLoad(abs);
    }

    /// <summary>
    /// Switch the active corpus to the requested kind. Only succeeds if the
    /// requested corpus was discovered under the current root by
    /// <see cref="AppPaths.DiscoverAllCorpora"/>. Re-points all directory
    /// pointers (originals, translations, cache, user) at the new corpus,
    /// clears render and translation caches, persists the new active corpus
    /// to config, and triggers a fresh nav rebuild so the file list reflects
    /// the new corpus's contents.
    /// </summary>
    public async Task SwitchCorpusAsync(CorpusKind target)
    {
        if (target == CorpusKind.Unknown) return;
        if (target == ActiveCorpus) return;
        if (string.IsNullOrEmpty(_root)) return;

        var layout = _availableCorpora.FirstOrDefault(c => c.Kind == target);
        if (layout == null)
        {
            SetStatus($"Corpus '{target}' is not available under the current root.", StatusSeverity.Error);
            return;
        }

        // Cancel any in-flight render so we don't end up applying it to
        // the wrong corpus's editor state after the switch.
        ResetRenderCts();

        _originalDir = layout.OriginalDir;
        _translatedDir = layout.TranslatedDir;
        _translatedCacheDir = layout.TranslatedCacheDir;
        _translationRoot = layout.TranslationsRepoRoot;
        _originalsRepoRoot = layout.OriginalsRepoRoot;

        // Use the active corpus's translations repo root directly. Passing
        // it through the legacy GetUserTranslatedDir would re-discover from
        // scratch and pick the wrong corpus.
        _userTranslatedDir = AppPaths.GetUserTranslatedDirForRepo(layout.TranslationsRepoRoot, GetTranslationFolderKey(_config));
        _activeTranslatedDir = _userTranslatedDir;

        _renderCache.Clear();
        _meaningfulTranslationCache.Clear();
        _licenseMetadata.Clear();

        _searchIndex.InvalidateIndexCaches();
        _searchIndex.ClearBloomCache();
        _searchIndex.ClearVerifyTextCache();

        ActiveCorpus = target;
        _config.ActiveCorpus = target;
        await SafeSaveConfigAsync();

        // Reload the per-corpus Zen-flag set + refresh the search resolver.
        // Without this, the in-memory _zen from the previous corpus's
        // zen_texts.json leaks into the new corpus's filter and breaks the
        // "Zen only" checkbox (ticking it shows nothing until the user
        // restarts the app). Fixes RUN-20260416-2302 post-run regression.
        try
        {
            await _zenTexts.LoadAsync(_translationRoot ?? _root);
            SetSearchZenResolver?.Invoke(rel => _zenTexts.IsZen(rel));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[MainWindowViewModel] Zen texts reload after corpus switch failed: {ex.Message}");
        }

        // Force a fresh nav rebuild from the new corpus's filesystem.
        SetStatus($"Switched to {target} corpus. Rebuilding nav…");
        try
        {
            try
            {
                if (_originalDir != null)
                {
                    var cached = await _indexCacheService.TryLoadAsync(_translationRoot, _originalsRepoRoot);
                    if (cached?.Entries is { Count: > 0 })
                    {
                        var diskCount = Directory.EnumerateFiles(_originalDir, "*.xml", SearchOption.AllDirectories).Count();
                        if (diskCount != cached.Entries.Count)
                            _forceRebuildIndex = true;
                    }
                }
            }
            catch { }

            await LoadFileListFromCacheOrBuildAsync();
            await ApplyFilterSafeAsync();
            SetStatus($"Active corpus: {target}");
        }
        catch (System.Exception ex)
        {
            SetStatus($"Corpus switch failed: {ex.Message}", StatusSeverity.Error);
        }
    }

    // ===========================================================
    // Bridge delegates wired by code-behind to tab view methods
    // ===========================================================

    // ReadableTabView bridges
    public Action<RenderedDocument, RenderedDocument>? SetReadableRendered { get; set; }
    public Action<Dictionary<string, LociEntry>>? SetReadableLociMap { get; set; }
    public Action? ClearReadable { get; set; }
    // SetReadableHoverDict folded into ReadableTabView's SettingsAppliedMessage handler (ratchet).
    public Action<string?, bool>? SetReadableZenContext { get; set; }
    public Action<IReadOnlyList<TermHit>?, string?, int?, string?>? UpdateReadableTermHighlights { get; set; }
    public Action<IReadOnlyList<TranslationTmMatch>?, IReadOnlyList<TranslationTmMatch>?, string?, int?, string?>? UpdateReadableTmSharedHighlights { get; set; }
    // SetReadableDefaultResp / SetReadableTagCompareIdentity / SetReadableTagUsername
    // folded into ReadableTabView's SettingsAppliedMessage handler (ratchet).
    public Action<TranslationAssistantSnapshot?>? SetReadableStudySnapshot { get; set; }
    // SetReadableStudyPanelVisible folded into ReadableTabView's SettingsAppliedMessage handler (ratchet).

    // Top-bar license chip. Fired whenever the active file's license metadata
    // becomes known — once from the readable-render bridge (cache path), and
    // again after the index build completes on cold load (the extractor runs
    // inside BuildIndex, so the first emission can arrive before metadata
    // exists).
    public Action<TextLicenseInfo?>? SetCurrentFileLicense { get; set; }

    // Provenance panel bridge
    public Action<ManifestInfo?, TextLicenseInfo?, CorpusKind, string?>? SetCurrentFileProvenance { get; set; }
    // SetReadableProvenancePanelVisible folded into ReadableTabView's SettingsAppliedMessage handler (ratchet).

    // ReadableTabView coding mode bridges
    public Action<TagVocabulary?>? SetReadableTagVocabulary { get; set; }
    public Action<List<DocumentTag>?>? SetReadableAppliedTags { get; set; }
    public Action<Dictionary<string, List<DocumentTag>>?>? SetReadableCommunityTags { get; set; }
    public Action<Dictionary<string, TagVocabulary>?>? SetReadableCommunityVocabularies { get; set; }
    public Action<List<DocumentTag>?, TagVocabulary?>? SetSearchTagFilterData { get; set; }
    public Action<List<string>>? SetReadableTranslationSourceOptions { get; set; }
    public Action<int>? SetReadableTranslationSourceIndex { get; set; }
    public Action<bool?>? UpdateReadableStarButton { get; set; }

    // TranslationTabView bridges
    public Action<List<string>>? SetTranslationSourceOptions { get; set; }
    public Action<int>? SetTranslationSourceIndex { get; set; }
    public Action<bool?>? UpdateTranslationStarButton { get; set; }
    public Action<bool>? SetTranslationEditorReadOnly { get; set; }
    public Action<TranslationEditMode, string>? SetTranslationModeProjection { get; set; }
    public Func<string>? GetTranslationProjectionText { get; set; }
    public Action? ClearTranslation { get; set; }
    // SetTranslationHoverDict folded into TranslationTabView's SettingsAppliedMessage handler (ratchet).
    public Action<bool>? SetAssistantLoading { get; set; }
    public Action<TranslationAssistantSnapshot?>? SetAssistantSnapshot { get; set; }

    /// <summary>
    /// Appends concordance hits (Chinese-only matches from untranslated texts)
    /// to the Translate assistant panel AFTER TM results are rendered. Wired by
    /// code-behind to call AssistantPanelRenderer.RenderConcordance.
    /// </summary>
    public Action<IReadOnlyList<ConcordanceHit>>? AppendTranslateConcordance { get; set; }

    /// <summary>Same as above but for the Reader study panel.</summary>
    public Action<IReadOnlyList<ConcordanceHit>>? AppendReaderConcordance { get; set; }
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
    public Action<string, string, IReadOnlyList<string>>? SetSearchRootContext { get; set; }
    public Action<Func<string, bool>>? SetSearchZenResolver { get; set; }
    public Action<ZenMasterCatalog>? SetSearchMasterCatalog { get; set; }

    /// <summary>Permanently cached master catalog for downstream consumers (search, etc.).</summary>
    public ZenMasterCatalog? MasterCatalog { get; private set; }
    public Action<string, string, IReadOnlyList<string>, Func<string, (string, string, TranslationStatus?)>, IReadOnlyList<string>?, IReadOnlyList<string>?>? SetSearchContext { get; set; }
    public Action<List<FileNavItem>>? SetSearchFileIndex { get; set; }
    public Action? ClearSearch { get; set; }

    // GitTabView bridges
    public Action<string?>? SetGitRepoRoot { get; set; }
    public Action<string?>? SetGitSelectedRelPath { get; set; }
    // SetGitUsername / LoadGitPersistedAuth folded into GitTabView's SettingsAppliedMessage handler (ratchet).

    // ScholarTabView bridges
    public Action<string>? SetScholarRoot { get; set; }
    public Action? ClearScholar { get; set; }
    public Action<string?>? SetScholarUsername { get; set; }
    public Action<string?>? SetScholarAssistantUsername { get; set; }
    public Action<string?, string?>? SetScholarTranslationDirs { get; set; }
    public Action<List<string>>? SetScholarDictionarySourceOptions { get; set; }
    public Action<int>? SetScholarDictionarySourceIndex { get; set; }
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
    public Func<NavigationRequest, Task>? NavigateInReadable { get; set; }

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
        IAppConfigService configService,
        IIndexCacheService indexCacheService,
        IRenderedDocumentCacheService renderCache,
        IZenTextsService zenTexts,
        IIndexedTranslationService indexedTranslation,
        ITranslationAssistantService translationAssistant,
        ITranslationAssistantBuildService translationAssistantBuilder,
        ITranslationReviewService translationReview,
        ISearchIndexService searchIndex,
        IDocumentTagService documentTagService,
        IGitRepoService gitService,
        ILicenseMetadataService licenseMetadata,
        IManifestService manifestService)
    {
        _configService = configService;
        _indexCacheService = indexCacheService;
        _renderCache = renderCache;
        _zenTexts = zenTexts;
        _indexedTranslation = indexedTranslation;
        _translationAssistant = translationAssistant;
        _translationAssistantBuilder = translationAssistantBuilder;
        _translationReview = translationReview;
        _searchIndex = searchIndex;
        _documentTagService = documentTagService;
        _gitService = gitService;
        _licenseMetadata = licenseMetadata;
        _manifestService = manifestService;

        // Corpus-changed trigger (git sync/clone/update/panic success): queue the
        // staleness-gated, debounced auto index build. Weak registration on the typed
        // messenger per the ratchet - no new MWVM bridge delegate. Intentionally NOT
        // guarded by _isAutoIndexing: a git success means the corpus changed, so any
        // in-flight auto build is stale by definition; QueueAutoIndexBuild's own CTS
        // cancel, 3s delay, and IsStaleAsync gate provide supersession, debounce, and
        // no-op cheapness. Known non-blocking gap: a SearchTab manual build's own CTS
        // is not cancelled by this trigger; the two serialize under the index IO gate
        // and the second pass is a cheap incremental no-op.
        WeakReferenceMessenger.Default.Register<MainWindowViewModel, Messages.CorpusFilesChangedMessage>(
            this, static (vm, _) => vm.QueueAutoIndexBuild());
    }

    /// <summary>Inject the star service after construction (optional dependency).</summary>
    public void SetStarService(ITranslationStarService starService) => _starService = starService;

    /// <summary>Reload star data from disk and refresh the translation source list so star counts update.</summary>
    public async Task ReloadStarsAsync()
    {
        if (_starService == null || _translationRoot == null) return;
        var starsDir = Path.Combine(_translationRoot, "community", "stars");
        try
        {
            await _starService.LoadAllStarsAsync(starsDir, CancellationToken.None);
            RefreshTranslationSources();
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[MainWindowViewModel] Star reload failed: {ex.Message}"); }
    }

    // ===========================================================
    // Public accessors for code-behind
    // ===========================================================

    public string? ViewConfigUsernameForAssistant() => _config.GitHubUsername ?? _config.Username;
    public string? GetActiveDictionaryUser() => GetActiveTranslationUser();
    public string? GetActiveTranslatedDir() => GetSearchTranslatedDir();
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

            if (!string.IsNullOrWhiteSpace(_config.TextRootPath) && Directory.Exists(_config.TextRootPath))
            {
                // Check for legacy single-repo layout needing migration
                var configPath = _config.TextRootPath!;
                if (LegacyRepoMigration.IsLegacySingleRepoLayout(configPath))
                {
                    SetStatus("Migrating to two-repo layout (one-time)...");
                    var migrationProgress = new Progress<string>(msg => SetStatus("Migration: " + msg));
                    var migrationUsername = _config.GitHubUsername ?? _config.Username;
                    var result = await LegacyRepoMigration.MigrateAsync(
                        configPath, _gitService, migrationUsername, migrationProgress, CancellationToken.None);

                    if (result.Success)
                    {
                        _config.TextRootPath = result.NewParentRoot;
                        await SafeSaveConfigAsync();
                        AppPaths.InvalidateDiscoveryCache();
                        SetStatus("Migration complete. Loading...");
                    }
                    else
                    {
                        SetStatus("Migration failed: " + (result.Error ?? "unknown") + " — Use Git tab to sync.");
                    }
                }
                else
                {
                    // Check for pending migration from interrupted attempt
                    var parentDir = Path.GetDirectoryName(configPath);
                    if (parentDir != null && LegacyRepoMigration.HasPendingMigration(parentDir))
                    {
                        SetStatus("Resuming interrupted migration...");
                        var migrationProgress = new Progress<string>(msg => SetStatus("Migration: " + msg));
                        var resumeUsername = _config.GitHubUsername ?? _config.Username;
                        var result = await LegacyRepoMigration.MigrateAsync(
                            configPath, _gitService, resumeUsername, migrationProgress, CancellationToken.None);

                        if (result.Success)
                        {
                            _config.TextRootPath = result.NewParentRoot;
                            await SafeSaveConfigAsync();
                            AppPaths.InvalidateDiscoveryCache();
                        }
                    }
                }

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
                await LoadRootAsync(_config.TextRootPath!, saveToConfig: false);

                if (!_config.HasCompletedOnboarding && _root != null)
                {
                    _config.HasCompletedOnboarding = true;
                    await SafeSaveConfigAsync();
                }

                // Skip auto-loading last file if a deep link will navigate us elsewhere
                var hasDeepLink = App.StartupArgs?.Any(a =>
                    a.StartsWith(ZenUriParser.Scheme + "://", StringComparison.OrdinalIgnoreCase)) == true;

                if (!hasDeepLink && !string.IsNullOrWhiteSpace(_config.LastSelectedRelPath))
                {
                    var rel = NormalizeRel(_config.LastSelectedRelPath);
                    _suppressNavSelection = true;
                    SelectInNav(rel);
                    _suppressNavSelection = false;
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
        // Skip redundant loads of the same root — this avoids the heavy
        // RefreshAllCachedStatusesAsync running multiple times when a deep
        // link comes in right after the initial config-driven auto-load.
        // EXCEPT: always re-run multi-corpus discovery here, because sync
        // can clone the OpenZen repo pair into the same root mid-
        // session, and the badge flyout needs to pick up the new corpus
        // without forcing a full reload.
        if (_root != null && string.Equals(_root, rootPath, StringComparison.OrdinalIgnoreCase) && _allItems.Count > 0)
        {
            // Bust the AppPaths discovery caches so we re-scan the disk
            // (sync just invalidated them but in case anything else
            // populated them since the last call, blow them away again).
            AppPaths.InvalidateDiscoveryCache(rootPath);
            var refreshed = AppPaths.DiscoverAllCorpora(rootPath);
            // Only update if the count actually changed — avoids spurious
            // PropertyChanged events when nothing differs.
            if (refreshed.Count != _availableCorpora.Count)
            {
                AvailableCorpora = refreshed;
            }

            if (saveToConfig && _config.TextRootPath != _root)
            {
                _config.TextRootPath = _root;
                await SafeSaveConfigAsync();
            }
            return;
        }

        _root = rootPath;
        _userHasManuallySelectedSource = false;

        // Multi-corpus discovery: find every (originals, translations) pair
        // under the parent root. CBETA and OpenZen can coexist as
        // sibling subfolders. The active corpus is chosen from the saved
        // preference if it's present in the list, otherwise the first one.
        AvailableCorpora = AppPaths.DiscoverAllCorpora(_root);

        CorpusLayout? activeLayout = null;
        if (_availableCorpora.Count > 0)
        {
            activeLayout = _availableCorpora.FirstOrDefault(c => c.Kind == _config.ActiveCorpus)
                        ?? _availableCorpora[0];
            _originalDir = activeLayout.OriginalDir;
            _translatedDir = activeLayout.TranslatedDir;
            _translatedCacheDir = activeLayout.TranslatedCacheDir;
            _translationRoot = activeLayout.TranslationsRepoRoot;
            _originalsRepoRoot = activeLayout.OriginalsRepoRoot;
        }
        else
        {
            // Legacy single-corpus path: user picked a folder that contains
            // just one repo pair (or even is itself the originals repo).
            // Fall back to AppPaths' single-pair discovery + the corpus
            // detector for the kind hint.
            _translationRoot = AppPaths.GetTranslationRepoRoot(_root);
            _originalDir = AppPaths.GetOriginalDir(_root);
            _translatedDir = AppPaths.GetTranslatedDir(_root);
            _translatedCacheDir = AppPaths.GetTranslatedCacheDir(_root);
            _originalsRepoRoot = AppPaths.DiscoverRepoPaths(_root).OriginalsRepoRoot;
        }

        // CRITICAL: use the ACTIVE corpus's translations repo root for the
        // per-user dir, not _root (the parent). The legacy GetUserTranslatedDir
        // overload internally calls GetTranslationRepoRoot which always returns
        // the FIRST discovered translation repo — in multi-corpus setups
        // (CBETA + OpenZen coexisting) that's CBETA, so OpenZen translations
        // would silently land in CBETA's working tree and get wiped by the
        // next CBETA sync's `git clean -fd`. Real data-loss bug. Always
        // route through GetUserTranslatedDirForRepo here.
        if (!string.IsNullOrEmpty(_translationRoot))
        {
            _userTranslatedDir = AppPaths.GetUserTranslatedDirForRepo(_translationRoot!, GetTranslationFolderKey(_config));
        }
        else
        {
            // Truly legacy: no discovered translations repo at all (the user
            // pointed at a degenerate folder). Fall back to the old helper
            // which constructs a default path under the parent.
            _userTranslatedDir = AppPaths.GetUserTranslatedDir(_root, GetTranslationFolderKey(_config));
        }
        _activeTranslatedDir = _userTranslatedDir; // default to user's own
        // Note: user dir is created on-demand by GetWritePath() when user first saves

        bool reposReady = (activeLayout != null) || AppPaths.ValidateBothReposExist(_root);
        if (!reposReady)
        {
            _root = null;
            _translationRoot = null;
            _originalDir = null;
            _translatedDir = null;
            _translatedCacheDir = null;
            _originalsRepoRoot = null;
            _userTranslatedDir = null;
            _activeTranslatedDir = null;
            AvailableCorpora = System.Array.Empty<CorpusLayout>();
            SetStatus("Both originals and translations repos are required. Please sync via Git tab.");
            return;
        }

        _renderCache.Clear();
        _meaningfulTranslationCache.Clear();
        // The license metadata cache is a DI singleton shared across windows.
        // Only the primary window (saveToConfig=true) is allowed to wipe it;
        // a secondary window doing so would nuke the primary's cached entries
        // mid-session. Secondary windows just update their own VM state.
        if (saveToConfig)
            _licenseMetadata.Clear();

        // Resolve the active corpus. Prefer the kind from the multi-corpus
        // discovery (most authoritative — it's filesystem evidence). Fall
        // back to CorpusDetector for legacy single-corpus roots, then to
        // the saved config preference. Only the primary window persists
        // the choice to _config; secondary windows must not mutate the
        // shared config object because their root selection is transient.
        CorpusKind resolvedKind;
        if (activeLayout != null)
        {
            resolvedKind = activeLayout.Kind;
        }
        else
        {
            var detected = CorpusDetector.Detect(_root);
            resolvedKind = detected != CorpusKind.Unknown ? detected : _config.ActiveCorpus;
        }
        ActiveCorpus = resolvedKind;
        if (saveToConfig)
            _config.ActiveCorpus = resolvedKind;

        RootDisplayText = _root;

        if (!Directory.Exists(_originalDir))
        {
            SetStatus("Original folder missing: " + _originalDir);
            return;
        }

        AppPaths.EnsureTranslatedDirExists(_root);

        try
        {
            await _zenTexts.LoadAsync(_translationRoot ?? _root);
            SetSearchZenResolver?.Invoke(rel => _zenTexts.IsZen(rel));
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[MainWindowViewModel] Zen texts load failed: {ex.Message}"); }

        SetGitRepoRoot?.Invoke(_root);
        PushSearchContext();
        SetScholarRoot?.Invoke(_translationRoot ?? _root);
        SetScholarTranslationDirs?.Invoke(_originalDir, GetActiveTranslatedDir());
        SetScholarUsername?.Invoke(_config.GitHubUsername ?? _config.Username);
        SetScholarAssistantUsername?.Invoke(GetActiveDictionaryUser());

        try
        {
            var reviewsDir = ITranslationReviewService.GetCommunityReviewsDir(_translationRoot!);
            await _translationReview.RefreshAggregationCacheAsync(_translationRoot!, reviewsDir);
        }
        catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[MainWindowViewModel] Review aggregation refresh failed: {ex.Message}"); }

        if (_starService != null && _translationRoot != null)
        {
            var starsDir = Path.Combine(_translationRoot, "community", "stars");
            try { await _starService.LoadAllStarsAsync(starsDir, CancellationToken.None); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[MainWindowViewModel] Star data load failed: {ex.Message}"); }
        }

        if (saveToConfig)
        {
            _config.TextRootPath = _root;
            _config.ZenOnly = GetZenOnly?.Invoke() ?? false;
            _config.Version = Math.Max(_config.Version, 3);
            await SafeSaveConfigAsync();
        }

        RefreshTranslationSources();
        await LoadFileListFromCacheOrBuildAsync();

        // Fire status refresh in background — the cache provides usable
        // statuses immediately, and the nav icons update progressively as
        // EvaluateBestTranslationSource catches up. This lets deep links
        // open the requested text without waiting for the full 0→4990 sweep.
        _ = Task.Run(async () =>
        {
            try { await RefreshAllCachedStatusesAsync(); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[MainWindowViewModel] Background nav status refresh failed: {ex.Message}"); }
        });

        QueueAutoIndexBuild();
    }

    /// <summary>
    /// Test seam (via InternalsVisibleTo): counts QueueAutoIndexBuild invocations.
    /// Incremented BEFORE the null-root early return so tests without corpus
    /// directories can still observe CorpusFilesChangedMessage receipt.
    /// </summary>
    internal int AutoIndexQueuedCount;

    private void QueueAutoIndexBuild()
    {
        AutoIndexQueuedCount++;

        if (_translationRoot == null || _originalDir == null || _translatedDir == null)
        {
            System.Diagnostics.Debug.WriteLine($"[QueueAutoIndexBuild] SKIPPED: translationRoot={_translationRoot}, originalDir={_originalDir}, translatedDir={_translatedDir}");
            return;
        }

        _autoIndexCts?.Cancel();
        try { _autoIndexCts?.Dispose(); } catch { }
        _autoIndexCts = new CancellationTokenSource();
        var ct = _autoIndexCts.Token;

        var root = _translationRoot;
        var origDir = _originalDir;
        var tranDir = _translatedDir; // Primary community xml-p5t/ — also used for TM reference build

        // Collect ALL translation dirs for multi-dir indexing
        var tranDirs = BuildAllTranslatedDirs();

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

                // Preload TM + termbase into cache so first assistant lookup is instant
                try
                {
                    await _translationAssistant.WarmupCacheAsync(root, ct);
                }
                catch { }

                // Let the initial file load finish before competing for disk I/O
                await Task.Delay(3000, ct);

                // Search index
                bool searchStale = await _searchIndex.IsStaleAsync(root, origDir, tranDirs);
                if (searchStale && !ct.IsCancellationRequested)
                {
                    Dispatcher.UIThread.Post(() => SetStatus("Auto-updating search index..."));

                    var progress = new Progress<(int done, int total, string phase)>(t =>
                        Dispatcher.UIThread.Post(() => SetStatus($"Indexing: {t.phase} ({t.done}/{t.total})")));

                    var addOrigDirsAuto = _availableCorpora
                        .Where(c => c.Kind != ActiveCorpus && Directory.Exists(c.OriginalDir))
                        .Select(c => c.OriginalDir).ToList();
                    var addTransDirsAuto = _availableCorpora
                        .Where(c => c.Kind != ActiveCorpus && Directory.Exists(c.TranslatedDir))
                        .Select(c => c.TranslatedDir).ToList();
                    await _searchIndex.BuildOrUpdateAsync(root, origDir, tranDirs,
                        forceRebuild: false,
                        additionalOriginalDirs: addOrigDirsAuto.Count > 0 ? addOrigDirsAuto : null,
                        additionalTranslatedDirs: addTransDirsAuto.Count > 0 ? addTransDirsAuto : null,
                        progress: progress, ct: ct);

                    if (!ct.IsCancellationRequested)
                        Dispatcher.UIThread.Post(() => SetStatus("Search index ready."));
                }

                // After search index is built/updated, warm it up
                try
                {
                    var manifest = await _searchIndex.TryLoadAsync(root);
                    if (manifest != null)
                        System.Diagnostics.Debug.WriteLine("[MainWindowViewModel] Search index warmed up on startup");
                }
                catch { }

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

                // Master corpus index (zen masters in texts)
                if (!ct.IsCancellationRequested && _root != null)
                {
                    try
                    {
                        var corpusSvc = new MasterCorpusSearchService();
                        var cacheDir = MasterCorpusSearchService.GetCacheDir(_root);
                        // Freshness check makes a corpus change (or a legacy unstamped
                        // cache) come back null → the auto-build below refreshes it.
                        var cached = await corpusSvc.TryLoadAsync(cacheDir, ct, parentRootForFreshness: _root);
                        MasterCorpusIndex? index = cached;

                        var masterMgr = App.Services.GetRequiredService<ZenMasterManagerService>();
                        var catalog = await masterMgr.LoadAsync(_root);
                        MasterCatalog = catalog;
                        if (catalog.Records.Count > 0)
                            Dispatcher.UIThread.Post(() => SetSearchMasterCatalog?.Invoke(catalog));

                        if (index == null && catalog.Records.Count > 0)
                        {
                            Dispatcher.UIThread.Post(() => SetStatus("Auto-building master corpus index..."));

                            var corpusProgress = new Progress<(int done, int total, string status)>(t =>
                                Dispatcher.UIThread.Post(() => SetStatus($"Master corpus: {t.status}")));

                            index = await corpusSvc.BuildFullIndexAsync(_root, catalog, corpusProgress, ct);
                            await corpusSvc.SaveAsync(cacheDir, index, ct);
                        }

                        // Always export (whether freshly built or loaded from cache)
                        if (index != null && catalog.Records.Count > 0)
                        {
                            var exportDir = cacheDir;
                            try
                            {
                                await MasterCorpusSearchService.ExportMastersJsonAsync(exportDir, catalog, ct);
                                await MasterCorpusSearchService.ExportMasterCorpusJsonAsync(exportDir, index, ct);
                            }
                            catch (Exception ex)
                            {
                                System.Diagnostics.Debug.WriteLine($"[MainWindowViewModel] Master export failed: {ex.Message}\n{ex.StackTrace}");
                                Dispatcher.UIThread.Post(() => SetStatus($"Master export failed: {ex.Message}"));
                            }

                            if (!ct.IsCancellationRequested)
                                Dispatcher.UIThread.Post(() => SetStatus($"Master corpus index ready ({index.MasterCount} of {catalog.Records.Count} masters found in {index.Appearances.Count} text appearances)."));
                        }
                    }
                    catch (OperationCanceledException) { throw; }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[MainWindowViewModel] Master corpus index failed: {ex.Message}");
                    }
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
        // Messenger broadcast (the ratchet-preferred channel). This is now the sole
        // fan-out channel for the pure config-driven view state: TranslationTabView,
        // ReadableTabView, GitTabView and ScholarTabView each register for it and
        // apply the same settings the old bridge delegates pushed (MVVM ratchet).
        try { WeakReferenceMessenger.Default.Send(new Messages.SettingsAppliedMessage(_config)); } catch { }

        // SetScholarAssistantUsername stays a delegate: its value is derived from
        // the ACTIVE TRANSLATION SOURCE (GetActiveDictionaryUser), not from
        // AppConfig, so the config-only message cannot carry it (see the
        // index>=2 assertion in the ScholarAssistant fan-out test).
        try { SetScholarAssistantUsername?.Invoke(GetActiveDictionaryUser()); } catch { }
        try { _translationAssistant.SetUsername(GetActiveDictionaryUser()); } catch { }
    }

    public void UpdateConfig(AppConfig config)
    {
        _config = config;
    }

    public async Task EnsureUserTranslationDirectoryCanonicalizedForSyncAsync()
    {
        if (string.IsNullOrWhiteSpace(_root) || string.IsNullOrWhiteSpace(_config.GitHubUsername))
            return;

        await RefreshUserTranslationDirectoryAsync(_config.Username);
        ApplySettingsToChildViews();
    }

    public async Task HandleGitHubAuthCompletedAsync(string token, string login)
    {
        var previousFolderKey = GetTranslationFolderKey(_config);
        _config.GitHubAccessToken = token;
        _config.GitHubUsername = login;
        if (string.IsNullOrWhiteSpace(_config.Username))
            _config.Username = login;
        await RefreshUserTranslationDirectoryAsync(previousFolderKey);
        ApplySettingsToChildViews();
        await SafeSaveConfigAsync();
    }

    // ===========================================================
    // Index + filter
    // ===========================================================

    public async Task LoadFileListFromCacheOrBuildAsync()
    {
        if (_translationRoot == null || _originalDir == null || _translatedDir == null)
            return;

        ClearViews();

        void WireSearchTab()
        {
            var addOrigDirs = _availableCorpora
                .Where(c => c.Kind != ActiveCorpus && Directory.Exists(c.OriginalDir))
                .Select(c => c.OriginalDir).ToList();
            var addTransDirs = _availableCorpora
                .Where(c => c.Kind != ActiveCorpus && Directory.Exists(c.TranslatedDir))
                .Select(c => c.TranslatedDir).ToList();

            SetSearchContext?.Invoke((_translationRoot ?? _root)!, _originalDir!, BuildAllTranslatedDirs(),
                relKey =>
                {
                    _allItemsByRel.TryGetValue(NormalizeRel(relKey), out var it);
                    return it != null ? (it.DisplayShort, it.Tooltip, it.Status) : (relKey, relKey, null);
                },
                addOrigDirs.Count > 0 ? addOrigDirs : null,
                addTransDirs.Count > 0 ? addTransDirs : null);

            SetSearchZenResolver?.Invoke(rel => _zenTexts.IsZen(rel));
        }

        try
        {
            var cache = await _indexCacheService.TryLoadAsync(_translationRoot, _originalsRepoRoot);

            if (cache?.Entries is { Count: > 0 } && !_forceRebuildIndex)
            {
                _allItems = cache.Entries;
                RebuildLookup();

                await ApplyFilterSafeAsync();
                WireSearchTab();
                SetSearchFileIndex?.Invoke(BuildSearchFileIndex());

                SetStatus("Loaded index cache: " + _allItems.Count.ToString("n0") + " files.");
                return;
            }

            SetStatus("Building index cache...");

            var progress = new Progress<(int done, int total)>(p =>
            {
                SetStatus("Indexing files... " + p.done.ToString("n0") + "/" + p.total.ToString("n0"));
            });

            IndexCache built = await _indexCacheService.BuildAsync(_originalDir, _translatedDir, _translationRoot, progress);
            await _indexCacheService.SaveAsync(_translationRoot, built, _originalsRepoRoot);

            _allItems = built.Entries ?? new List<FileNavItem>();
            RebuildLookup();

            await ApplyFilterSafeAsync();
            WireSearchTab();
            SetSearchFileIndex?.Invoke(BuildSearchFileIndex());

            SetStatus("Index cache created: " + _allItems.Count.ToString("n0") + " files.");
            _forceRebuildIndex = false;
        }
        catch (Exception ex)
        {
            _forceRebuildIndex = false;
            SetStatus("Index load/build failed: " + ex.Message);
        }
    }

    public async Task RefreshAllCachedStatusesAsync()
    {
        if (_root == null || _originalDir == null || _translatedDir == null) return;
        bool changed = false;
        int total = _allItems.Count;
        var progress = new Progress<int>(done =>
        {
            // Only show every 500 to avoid flooding status bar over indexing messages
            if (done % 500 == 0 && done < total)
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    SetStatus($"Refreshing nav statuses... {done:n0}/{total:n0}"));
        });
        var refilter = new Progress<int>(_ =>
        {
            // Re-apply the filter so the nav list reflects updated statuses progressively.
            // Marshal to UI thread because Progress<T> may fire on the threadpool when
            // RefreshAllCachedStatusesAsync was started from a background context.
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                var fireAndForget = ApplyFilterSafeAsync();
            });
        });
        var statusUpdates = new System.Collections.Concurrent.ConcurrentBag<(FileNavItem item, TranslationStatus newStatus, long newMtime)>();
        await Task.Run(() =>
        {
            int done = 0;
            int sinceRefilter = 0;
            foreach (var it in _allItems)
            {
                if (string.IsNullOrWhiteSpace(it.RelPath)) continue;
                var best = EvaluateBestTranslationSource(it.RelPath);
                var newStatus = best.Status;
                if (!Equals(it.Status, newStatus) || it.TranslatedMtimeTicks != best.TranslatedMtimeTicks)
                {
                    statusUpdates.Add((it, newStatus, best.TranslatedMtimeTicks));
                    changed = true;
                }
                done++;
                sinceRefilter++;
                if (done % 50 == 0)
                    ((IProgress<int>)progress).Report(done);
                // Periodically refresh the visible nav list so icons update live
                if (sinceRefilter >= 500)
                {
                    sinceRefilter = 0;
                    ((IProgress<int>)refilter).Report(done);
                }
            }
            ((IProgress<int>)progress).Report(done);
        });
        // Final refilter to catch any pending status changes — marshal to UI thread.
        // Use Post (fire-and-forget) instead of InvokeAsync to avoid deadlocking under
        // headless test hosts where no Avalonia dispatcher is pumping. The filter only
        // affects the visible nav list, not the cache save below which reads _allItems
        // directly, so awaiting it is not required for correctness.
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            var fireAndForget = ApplyFilterSafeAsync();
        });

        // Apply collected status updates on UI thread
        if (statusUpdates.Count > 0)
        {
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                foreach (var (item, newStatus, newMtime) in statusUpdates)
                {
                    item.Status = newStatus;
                    item.TranslatedMtimeTicks = newMtime;
                }
            });
        }

        if (changed)
        {
            await _indexCacheService.SaveAsync(_translationRoot!, new IndexCache { Entries = _allItems }, _originalsRepoRoot);
        }
    }
    private void RebuildLookup()
    {
        lock (_navItemsLock)
        {
            _allItemsByRel.Clear();
            foreach (var it in _allItems) _allItemsByRel[NormalizeRel(it.RelPath)] = it;
        }
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

            List<FileNavItem> allSnapshot;
            lock (_navItemsLock) { allSnapshot = _allItems.ToList(); }

            var built = await Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();

                IEnumerable<FileNavItem> seq = allSnapshot;

                if (zenOnly)
                {
                    // OpenZen files are all zen by definition; CBETA
                    // uses the explicit per-file zen_texts.json list.
                    bool openCorpus = _activeCorpus == CorpusKind.Open;
                    seq = seq.Where(it => !string.IsNullOrWhiteSpace(it.RelPath)
                        && (openCorpus || _zenTexts.IsZen(it.RelPath)));
                }

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
        var normalized = NormalizeRel(relPath);

        var match = _filteredItems.FirstOrDefault(x =>
            string.Equals(NormalizeRel(x.RelPath), normalized, StringComparison.OrdinalIgnoreCase));
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

        _allItems = new();
        _allItemsByRel.Clear();

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
        if (_suppressNavSelection) return; // Autoload in progress; do not double-load

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
        if (_activeTranslatedDir == null && _translatedDir == null && _originalDir == null) return null;

        try
        {
            // Prefer the active translation source (what the user selected/saved to)
            string? tranAbs = null;
            if (_activeTranslatedDir != null)
            {
                var activePath = Path.Combine(_activeTranslatedDir, relPath);
                if (File.Exists(activePath))
                    tranAbs = activePath;
            }
            // Fall back to best-source discovery
            tranAbs ??= FindTranslatedPath(relPath);

            // Try companion English TEI in same directory (for critical editions)
            if (tranAbs == null && _originalDir != null)
            {
                var origAbs = Path.Combine(_originalDir, relPath);
                var origDir = Path.GetDirectoryName(origAbs);
                var origName = Path.GetFileNameWithoutExtension(origAbs);
                var companionEn = Path.Combine(origDir!, origName + "-en.xml");
                if (File.Exists(companionEn))
                    tranAbs = companionEn;
            }

            if (tranAbs == null)
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
        if (_originalDir == null || (_translatedDir == null && _activeTranslatedDir == null)) return;

        // Check if a translated file already exists in active or community dir
        if (FindTranslatedPath(relPath) != null) return;

        // Auto-generated untranslated copies go to the cache dir (gitignored),
        // not the main translated dir.
        if (_translatedCacheDir != null)
        {
            var cachePath = Path.Combine(_translatedCacheDir, relPath);
            if (!File.Exists(cachePath))
            {
                var origXml = await ReadOriginalXmlAsync(relPath);
                if (string.IsNullOrWhiteSpace(origXml)) return;
                EnsureXmlIsWellFormed(origXml, "Original XML is malformed; cannot create translated copy.");
                var dir = Path.GetDirectoryName(cachePath);
                if (dir != null) Directory.CreateDirectory(dir);
                await AtomicWriteXmlAsync(cachePath, origXml);
            }
        }
    }

    private Task<(RenderedDocument ro, RenderedDocument rt)> RenderReadablePairDiskOnlyAsync(string relPath, CancellationToken ct)
    {
        if (_originalDir == null)
            return Task.FromResult((RenderedDocument.Empty, RenderedDocument.Empty));

        var origAbs = Path.Combine(_originalDir, relPath);
        // Prefer the active translation source so the reader shows what the user just saved
        string? tranAbs = null;
        if (_activeTranslatedDir != null)
        {
            var activePath = Path.Combine(_activeTranslatedDir, relPath);
            if (File.Exists(activePath))
                tranAbs = activePath;
        }
        tranAbs ??= FindTranslatedPath(relPath);

        // Try companion English TEI in same directory (for critical editions)
        if (tranAbs == null || !File.Exists(tranAbs))
        {
            var origDir = Path.GetDirectoryName(origAbs);
            var origName = Path.GetFileNameWithoutExtension(origAbs);
            var companionEn = Path.Combine(origDir!, origName + "-en.xml");
            if (File.Exists(companionEn))
                tranAbs = companionEn;
        }

        ct.ThrowIfCancellationRequested();

        var stampOrig = FileStamp.FromFile(origAbs);
        RenderedDocument ro;
        if (!_renderCache.TryGet(stampOrig, out ro))
        {
            ct.ThrowIfCancellationRequested();
            ro = TeiRenderer.Render(SafeReadAllTextUtf8(origAbs));
            _renderCache.Put(stampOrig, ro);
        }

        ct.ThrowIfCancellationRequested();

        if (tranAbs == null || !File.Exists(tranAbs))
        {
            var rtFallback = TeiRenderer.Render(SafeReadAllTextUtf8(origAbs));
            return Task.FromResult((ro, rtFallback));
        }

        var stampTran = FileStamp.FromFile(tranAbs);
        RenderedDocument rt;
        if (!_renderCache.TryGet(stampTran, out rt))
        {
            ct.ThrowIfCancellationRequested();
            rt = TeiRenderer.Render(SafeReadAllTextUtf8(tranAbs));
            _renderCache.Put(stampTran, rt);
        }

        return Task.FromResult((ro, rt));
    }

    /// <summary>Reloads the current file from disk (used when returning from historical view).</summary>
    public async Task ReloadCurrentReadableAsync()
    {
        if (_currentRelPath != null)
            await LoadPairAsync(_currentRelPath, autoChooseSource: false);
    }

    public async Task LoadPairAsync(string relPath, bool autoChooseSource = true)
    {
        if (_originalDir == null || (_translatedDir == null && _activeTranslatedDir == null)) return;

        var ct = ResetRenderCts();

        if (autoChooseSource && !_userHasManuallySelectedSource)
        {
            var bestIndex = await Task.Run(() => ResolveBestTranslationSourceIndex(relPath), ct);
            if (ct.IsCancellationRequested) return;
            ApplyTranslationSourceIndex(bestIndex);
        }

        _currentRelPath = relPath;
        _currentSegmentContext = null;

        UpdateTranslationTermHighlights?.Invoke(null, null);
        UpdateReadableTermHighlights?.Invoke(null, null, null, null);
        UpdateTranslationTmSharedHighlights?.Invoke(null, null, null);

        CurrentFileText = relPath;
        SetGitSelectedRelPath?.Invoke(_currentRelPath);

        SetStatus("Loading: " + relPath);

        // Kick off both XML pipelines in parallel:
        //   (1) index build for the translation editor (slower; XML parse + segment maps)
        //   (2) readable view render (uses _renderCache, often a cache hit on revisit)
        // Both read the same XML files but are independent. Running them in
        // parallel lets the readable view appear as soon as it's ready instead
        // of waiting for the index build to complete first.
        var indexTask = Task.Run(async () =>
        {
            var orig = await ReadOriginalXmlAsync(relPath);
            await EnsureTranslatedXmlExistsForRelPathAsync(relPath);
            var tran = await TryReadTranslatedXmlFromDiskAsync(relPath) ?? orig;
            // Pass the absolute path so the license extractor caches under the
            // same key the UI surfaces will look up later.
            var origAbsForLicense = _originalDir != null
                ? Path.Combine(_originalDir, relPath)
                : null;
            var doc = _indexedTranslation.BuildIndex(orig, tran, origAbsForLicense);
            return (orig, tran, doc);
        }, ct);

        var readableTask = Task.Run(async () =>
        {
            ct.ThrowIfCancellationRequested();
            return await RenderReadablePairDiskOnlyAsync(relPath, ct);
        }, ct);

        // Show the readable view as soon as it's ready — it's typically the
        // active tab on a deep link, so this is the "loaded" feeling for users.
        var swRender = System.Diagnostics.Stopwatch.StartNew();
        RenderedDocument readableOrig = RenderedDocument.Empty;
        RenderedDocument readableTran = RenderedDocument.Empty;
        try
        {
            var (ro, rt) = await readableTask;
            swRender.Stop();
            if (ct.IsCancellationRequested) return;

            readableOrig = ro;
            readableTran = rt;

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                SetReadableRendered?.Invoke(ro, rt);
            });

            try
            {
                bool isZen = _root != null && IsZenOrOpenCorpusFile(relPath);
                SetReadableZenContext?.Invoke(relPath, isZen);
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[MainWindowViewModel] Zen context set failed: {ex.Message}"); }
        }
        catch (OperationCanceledException) { return; }
        catch (Exception ex)
        {
            SetStatus("Render failed: " + ex.Message);
        }

        // Now wait for the index build to complete and wire up the translation editor.
        try
        {
            var (origXml, tranXml, indexedDoc) = await indexTask;
            if (ct.IsCancellationRequested) return;

            _rawOrigXml = origXml;
            _rawTranXml = tranXml;
            _indexedDoc = indexedDoc;

            // Build segment-key → locus mapping from the original XML
            try
            {
                var lociMap = LociMappingService.BuildFromXml(origXml);
                if (lociMap.Count > 0)
                    await Dispatcher.UIThread.InvokeAsync(() => SetReadableLociMap?.Invoke(lociMap));
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[MainWindowViewModel] Loci map build failed: {ex.Message}"); }

            // License metadata is populated as a side-effect of BuildIndex
            // (which ran inside indexTask). The readable render that fired
            // SetReadableRendered earlier may have raced ahead of indexTask
            // on cold load and pushed a null license to the chip; re-publish
            // now that the extractor has written the entry.
            try { SetCurrentFileLicense?.Invoke(GetLicenseForCurrentFile()); }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[MainWindowViewModel] License chip refresh failed: {ex.Message}"); }

            try
            {
                var absPath = _originalDir != null && _currentRelPath != null
                    ? Path.Combine(_originalDir, _currentRelPath)
                    : null;
                SetCurrentFileProvenance?.Invoke(GetManifestForCurrentFile(), GetLicenseForCurrentFile(), _activeCorpus, absPath);
            }
            catch (Exception ex) { System.Diagnostics.Debug.WriteLine($"[MainWindowViewModel] Provenance refresh failed: {ex.Message}"); }

            var projection = _indexedTranslation.RenderProjection(_indexedDoc, _translationMode);
            SetTranslationProjection(_translationMode, projection);

            _baselineTranSha1 = Sha1Hex(projection);
            _lastSeenTranSha1 = _baselineTranSha1;
            _dirty = false;
            UpdateWindowTitle();
            UpdateSaveButtonState();
            // Signal core data ready for any deep-link gates waiting on it.
            SignalCoreLoadComplete?.Invoke();

            await SaveUiStateAsync();
            var sourceName = _translationSourceIndex < _translationSourceOptions.Count
                ? _translationSourceOptions[_translationSourceIndex] : "unknown";
            SetStatus($"Loaded: {relPath} — Source: {sourceName} (O={readableOrig.Segments.Count:n0}, T={readableTran.Segments.Count:n0}, {swRender.ElapsedMilliseconds:n0}ms)");
            var capturedRelPath = _currentRelPath;
            _ = Task.Run(async () => { ct.ThrowIfCancellationRequested(); await RefreshProgressStatsAsync(); }, ct);
            _ = Task.Run(async () => { ct.ThrowIfCancellationRequested(); await LoadAndPushTagsForCurrentFileAsync(); }, ct);

            // Rebuild the translation source dropdown for this specific file
            // so only users who actually translated THIS file appear. Must run
            // AFTER the baseline is set and _dirty is cleared — running it
            // earlier triggers ComboBox SelectionChanged cascades that corrupt
            // the dirty state and cause the "unsaved changes" dialog to fire
            // on every file switch.
            RefreshTranslationSources();
        }
        catch (OperationCanceledException) { return; }
        catch (Exception ex)
        {
            SetStatus("Index build failed: " + ex.Message);
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
                _translationRoot,
                _originalDir,
                GetActiveTranslatedDir());

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

            // Show loading indicator; keep old content visible until new data arrives
            SetAssistantLoading?.Invoke(true);

            // Clear stale review state immediately so previous block's status doesn't linger
            SetCurrentReviewState?.Invoke(null, null, null, null);

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
                _translationRoot,
                _originalDir,
                GetActiveTranslatedDir(),
                ct,
                maxResults: _config.TmMaxResults).ConfigureAwait(false);

            if (ct.IsCancellationRequested)
            {
                SetAssistantLoading?.Invoke(false);
                return;
            }

            // Service calls used ConfigureAwait(false), so we may be on a
            // thread-pool thread here.  Marshal back to the UI thread for
            // all control-touching work.
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                SetAssistantLoading?.Invoke(false);
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
            });

            await RefreshReviewBadgeAsync();

            // Concordance (async-populate): TM results are already visible,
            // now search the corpus for Chinese-only matches and append them.
            _ = AppendConcordanceAsync(ctx.ZhText, _currentRelPath, AppendTranslateConcordance, ct);
        }
        catch
        {
            SetAssistantLoading?.Invoke(false);
            // assistant errors must never break translation
        }
    }

    /// <summary>
    /// Builds a study panel snapshot for the reader tab when the user moves
    /// to a different segment in the original pane.
    /// </summary>
    public async Task RefreshReaderStudyPanelAsync(CurrentSegmentContext ctx)
    {
        try
        {
            try { _readerStudyCts?.Cancel(); } catch { }
            try { _readerStudyCts?.Dispose(); } catch { }
            _readerStudyCts = new CancellationTokenSource();
            var ct = _readerStudyCts.Token;

            UpdateReadableTermHighlights?.Invoke(null, null, null, null);
            UpdateReadableTmSharedHighlights?.Invoke(null, null, null, null, null);

            var snapshot = await _translationAssistant.BuildSnapshotAsync(
                ctx, _translationRoot, _originalDir, GetActiveTranslatedDir(), ct)
                .ConfigureAwait(false);

            if (ct.IsCancellationRequested) return;

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                SetReadableStudySnapshot?.Invoke(snapshot);
                int? readableOccurrenceHint =
                    ctx.BlockNumber > 0
                        ? ctx.BlockNumber - 1
                        : null;
                string? readableAnchorSignal = string.IsNullOrWhiteSpace(ctx.ZhContextText)
                    ? null
                    : ctx.ZhContextText;
                UpdateReadableTermHighlights?.Invoke(
                    snapshot?.Terms,
                    ctx.ZhText,
                    readableOccurrenceHint,
                    readableAnchorSignal);
                UpdateReadableTmSharedHighlights?.Invoke(
                    snapshot?.ApprovedMatches,
                    snapshot?.ReferenceMatches,
                    ctx.ZhText,
                    readableOccurrenceHint,
                    readableAnchorSignal);
            });

            // Concordance (async-populate): study panel TM results are already
            // visible, now append Chinese-only corpus matches.
            _ = AppendConcordanceAsync(ctx.ZhText, ctx.RelPath, AppendReaderConcordance, ct);
        }
        catch
        {
            // study panel errors must never break reader
        }
    }

    /// <summary>
    /// Fires a concordance search (Chinese-only matches from untranslated texts)
    /// and appends results to the assistant panel via the provided delegate.
    /// Runs AFTER TM results are rendered (async-populate pattern).
    /// </summary>
    private async Task AppendConcordanceAsync(
        string zhText,
        string? currentRelPath,
        Action<IReadOnlyList<ConcordanceHit>>? appendDelegate,
        CancellationToken ct)
    {
        if (!_config.EnableConcordance) return;
        if (appendDelegate == null) return;
        if (string.IsNullOrWhiteSpace(zhText) || zhText.Length < 2) return;
        if (_translationRoot == null || _originalDir == null) return;

        try
        {
            var manifest = await _searchIndex.TryLoadAsync(_translationRoot);
            if (manifest == null || ct.IsCancellationRequested) return;

            var hits = new List<ConcordanceHit>();
            int maxHits = 5;

            await foreach (var group in _searchIndex.SearchAllAsync(
                _translationRoot, _originalDir!, GetActiveTranslatedDir() ?? _originalDir!,
                manifest, zhText,
                includeOriginal: true, includeTranslated: false,
                fileMeta: rel =>
                {
                    _allItemsByRel.TryGetValue(NormalizeRel(rel), out var it);
                    return it != null ? (it.DisplayShort, it.Tooltip, it.Status) : (rel, rel, null);
                },
                contextWidth: 40, ct: ct).WithCancellation(ct))
            {
                // Skip the file we're currently editing
                if (string.Equals(group.RelPath, currentRelPath, StringComparison.OrdinalIgnoreCase))
                    continue;

                var firstChild = group.Children.FirstOrDefault();
                if (firstChild == null) continue;

                var snippet = $"{firstChild.LeftText}{firstChild.MatchText}{firstChild.RightText}";
                if (snippet.Length > 120) snippet = snippet[..117] + "...";

                hits.Add(new ConcordanceHit
                {
                    RelPath = group.RelPath,
                    DisplayName = group.DisplayName,
                    SnippetZh = snippet
                });

                if (hits.Count >= maxHits) break;
            }

            if (ct.IsCancellationRequested || hits.Count == 0) return;

            await Dispatcher.UIThread.InvokeAsync(() => appendDelegate(hits));
        }
        catch
        {
            // Concordance is best-effort — never break the assistant flow
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
        if (string.IsNullOrWhiteSpace(_translationRoot) || string.IsNullOrWhiteSpace(_currentRelPath))
        {
            SetProgressStats?.Invoke(0, 0, 0);
            return;
        }

        var map = await _translationReview.LoadLatestEntriesAsync(_translationRoot);
        var entries = map.Values
            .Where(e => NormalizeRel(e.RelPath) == NormalizeRel(_currentRelPath)
                     && e.Mode == _translationMode.ToString())
            .ToList();
        int approved = entries.Count(e => e.Status == TranslationReviewStatuses.Approved);
        int needsWork = entries.Count(e => e.Status == TranslationReviewStatuses.NeedsWork);
        int total = GetAllBlockNumbers?.Invoke()?.Count ?? 0;
        SetProgressStats?.Invoke(approved, needsWork, total);
    }

    // ===========================================================
    // Coding mode: tag loading, saving, and event handling
    // ===========================================================

    private async Task LoadAndPushTagsForCurrentFileAsync()
    {
        try
        {
            var username = _config.Username;
            if (string.IsNullOrWhiteSpace(_translationRoot) || string.IsNullOrWhiteSpace(username))
            {
                await InvokeUiActionAsync(() => SetReadableAppliedTags?.Invoke(null));
                return;
            }

            // Load vocabulary (once, then cached)
            if (_tagVocabulary == null)
            {
                _tagVocabulary = await _documentTagService.LoadVocabularyAsync(_translationRoot, username);
                await InvokeUiActionAsync(() => SetReadableTagVocabulary?.Invoke(_tagVocabulary));
            }

            // Load all user tags and filter to current file.
            // Lock to prevent racing with OnTagAppliedAsync which also mutates _appliedTags.
            List<DocumentTag> forFile;
            await _tagSaveLock.WaitAsync();
            try
            {
                _appliedTags = await _documentTagService.LoadUserTagsAsync(_translationRoot, username);
                forFile = _appliedTags
                    .Where(t => string.Equals(t.RelPath, _currentRelPath, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
            finally
            {
                _tagSaveLock.Release();
            }

            await InvokeUiActionAsync(() =>
            {
                SetReadableAppliedTags?.Invoke(forFile);
                SetSearchTagFilterData?.Invoke(_appliedTags, _tagVocabulary);
            });

            await RefreshCommunityTagDataAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Tags] Load failed: {ex.Message}");
        }
    }

    public async Task RefreshCommunityDataForCurrentFileAsync()
    {
        await RefreshReviewAggregationAsync();

        if (!string.IsNullOrWhiteSpace(_currentRelPath))
            await LoadAndPushTagsForCurrentFileAsync();
    }

    public async Task RefreshCommunityTagDataAsync()
    {
        if (string.IsNullOrWhiteSpace(_translationRoot))
            return;

        try
        {
            var communityTags = await _documentTagService.LoadAllCommunityTagsAsync(_translationRoot);
            var communityVocabs = await _documentTagService.LoadAllCommunityVocabulariesAsync(_translationRoot);
            var identityKeys = GetCurrentTagIdentityKeys();

            foreach (var key in identityKeys)
            {
                communityTags.Remove(key);
                communityVocabs.Remove(key);
            }

            await InvokeUiActionAsync(() =>
            {
                SetReadableCommunityTags?.Invoke(communityTags);
                SetReadableCommunityVocabularies?.Invoke(communityVocabs);
            });
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[Tags] Community load failed: {ex.Message}");
        }
    }

    private string? GetCurrentTagUsername()
        => string.IsNullOrWhiteSpace(_config.Username) ? null : _config.Username.Trim();

    private HashSet<string> GetCurrentTagIdentityKeys()
    {
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var tagUsername = GetCurrentTagUsername();
        if (!string.IsNullOrWhiteSpace(tagUsername))
            keys.Add(tagUsername);
        if (!string.IsNullOrWhiteSpace(_config.GitHubUsername))
            keys.Add(_config.GitHubUsername.Trim());
        return keys;
    }

    public async Task OnTagAppliedAsync(DocumentTag tag)
    {
        try
        {
            var username = _config.Username;
            if (string.IsNullOrWhiteSpace(_translationRoot) || string.IsNullOrWhiteSpace(username)) return;

            await _tagSaveLock.WaitAsync();
            try
            {
                // Safety: if _appliedTags was never loaded from disk (race with LoadAndPushTagsForCurrentFileAsync),
                // load them first to avoid overwriting existing tags with only the new one.
                if (_appliedTags.Count == 0)
                {
                    _appliedTags = await _documentTagService.LoadUserTagsAsync(_translationRoot, username);
                }

                tag.CreatedBy = username;
                _appliedTags.Add(tag);
                await _documentTagService.SaveUserTagsAsync(_translationRoot, username, _appliedTags);
            }
            finally
            {
                _tagSaveLock.Release();
            }
        }
        catch (Exception ex)
        {
            SetStatus("Tag save failed: " + ex.Message);
        }

        // Update search tab tag filter with latest tags
        await Dispatcher.UIThread.InvokeAsync(() =>
            SetSearchTagFilterData?.Invoke(_appliedTags, _tagVocabulary));
    }

    public async Task SaveTagVocabularyAsync(TagVocabulary vocab)
    {
        try
        {
            var username = _config.Username;
            if (string.IsNullOrWhiteSpace(_translationRoot) || string.IsNullOrWhiteSpace(username)) return;

            _tagVocabulary = vocab;
            await _documentTagService.SaveVocabularyAsync(_translationRoot, username, vocab);
        }
        catch (Exception ex)
        {
            SetStatus("Vocabulary save failed: " + ex.Message);
        }
    }

    /// <summary>
    /// Reloads the tag vocabulary from disk and pushes it to the Reader view.
    /// Called after TagEditorWindow saves.
    /// </summary>
    public async Task ReloadTagVocabularyAsync()
    {
        try
        {
            var username = _config.Username;
            if (string.IsNullOrWhiteSpace(_translationRoot) || string.IsNullOrWhiteSpace(username)) return;

            _tagVocabulary = await _documentTagService.LoadVocabularyAsync(_translationRoot, username);
            await InvokeUiActionAsync(() => SetReadableTagVocabulary?.Invoke(_tagVocabulary));
        }
        catch (Exception ex)
        {
            SetStatus("Reload tag vocabulary failed: " + ex.Message);
        }
    }

    private async Task RefreshReviewBadgeAsync()
    {
        try
        {
            if (_translationRoot == null || _currentSegmentContext == null)
            {
                SetCurrentReviewState?.Invoke(null, null, null, null);
                return;
            }

            var latest = await _translationReview.GetLatestEntryAsync(_translationRoot, _currentSegmentContext);
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
            if (_translationRoot == null)
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
                _translationRoot,
                _currentSegmentContext,
                status,
                reviewer: _config.Username ?? "User");

            int count = await _translationReview.RebuildApprovedTranslationMemoryAsync(_translationRoot);

            var reviewsDir = ITranslationReviewService.GetCommunityReviewsDir(_translationRoot);
            await _translationReview.RefreshAggregationCacheAsync(_translationRoot, reviewsDir);

            var latest = await _translationReview.GetLatestEntryAsync(_translationRoot, _currentSegmentContext);
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
                var reviewMap = await _translationReview.LoadLatestEntriesAsync(_translationRoot);
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
            if (_translationRoot == null || _currentRelPath == null) return;
            var map = await _translationReview.LoadLatestEntriesAsync(_translationRoot);
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
            if (_translationRoot == null) return;
            await _zenTexts.SetZenAsync(_translationRoot, relPath, isZen);
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
            if (_currentRelPath == null || (_activeTranslatedDir == null && _translatedDir == null))
            {
                SetStatus("Community insert ignored: no file selected.");
                return;
            }

            await EnsureTranslatedXmlExistsForCurrentAsync();

            var tranAbs = FindTranslatedPath(_currentRelPath) ?? GetWritePath(_currentRelPath);
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
            if (_currentRelPath == null || (_activeTranslatedDir == null && _translatedDir == null))
            {
                SetStatus("Community delete ignored: no file selected.");
                return;
            }

            await EnsureTranslatedXmlExistsForCurrentAsync();

            var tranAbs = FindTranslatedPath(_currentRelPath) ?? GetWritePath(_currentRelPath);
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
            if (_currentRelPath == null || (_activeTranslatedDir == null && _translatedDir == null))
            {
                SetStatus("Move footnote ignored: no file selected.");
                return;
            }

            await EnsureTranslatedXmlExistsForCurrentAsync();

            var tranAbs = FindTranslatedPath(_currentRelPath) ?? GetWritePath(_currentRelPath);
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
        try { rendered = TeiRenderer.Render(xml); }
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
            TranslationEditMode.Head => ReadZen.App.Services.TranslationUnitKind.Head,
            TranslationEditMode.Notes => ReadZen.App.Services.TranslationUnitKind.Note,
            _ => ReadZen.App.Services.TranslationUnitKind.Body
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
        if (_activeTranslatedDir == null && _translatedDir == null) return;

        EnsureXmlIsWellFormed(updatedXml, "Updated translated XML is not well-formed.");

        var tranAbs = GetWritePath(relPath);
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
        if (SetReadableRendered == null) return;
        if (_currentRelPath == null || _originalDir == null || (_translatedDir == null && _activeTranslatedDir == null)) return;

        var ct = ResetRenderCts();

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

    // Re-entrancy guard for the (now asynchronous) save pipeline: only one save may be
    // in flight, and file navigation during a save is detected before the write.
    private bool _saveInFlight;

    [RelayCommand]
    public async Task SaveTranslatedFromTabAsync()
    {
        if (_saveInFlight) { SetStatus("Save already in progress."); return; }
        _saveInFlight = true;
        try
        {
            if (_currentRelPath == null) { SetStatus("Nothing to save."); return; }
            if (_activeTranslatedDir == null && _translatedDir == null) { SetStatus("Save unavailable."); return; }
            if (IsActiveTranslationReadOnly) { SetStatus("Cannot save: viewing another user's translation (read-only)."); return; }
            if (_indexedDoc == null) { SetStatus("Translation index not loaded."); return; }

            var editedProjection = GetTranslationProjectionText?.Invoke() ?? "";

            // Cheap text→unit mapping stays on the UI thread (same dirty-tracking
            // semantics as before); the expensive XML rebuild moves off it.
            _indexedTranslation.ApplyProjectionEdits(_indexedDoc, _translationMode, editedProjection);

            var doc = _indexedDoc;
            var relPathAtStart = _currentRelPath;
            var mode = _translationMode;
            var username = _config.Username;
            var annotationUser = _config.GitHubUsername ?? _config.Username ?? "User";

            // BuildTranslatedXml serializes/patches the whole document — the dominant
            // save cost — and used to run synchronously on the UI thread, freezing the
            // app on large texts (audit P2.1 / R2-H3). The captured doc snapshot is
            // safe: user typing edits the projection TEXT control, not the doc; a file
            // navigation replaces _indexedDoc with a NEW instance and is detected below.
            var (builtXml, updatedCount, skippedUnsafe) = await Task.Run(() =>
            {
                var changedBlocks = string.IsNullOrWhiteSpace(username)
                    ? null
                    : GetChangedBlockNumbers(doc, mode);

                var xml = _indexedTranslation.BuildTranslatedXml(doc, out var updated);

                // Captured inside the worker so a concurrent build can't overwrite it before
                // we read it. Non-zero => some paragraphs could not be saved automatically
                // (unsafe markup, or a target that drifted/detached — review finding 4).
                var skipped = _indexedTranslation.LastBuildSkippedDirtyGroupCount;

                if (changedBlocks != null && changedBlocks.Count > 0)
                    xml = ApplyTranslatorAnnotation(xml, changedBlocks, annotationUser);

                return (xml, updated, skipped);
            });

            if (!string.Equals(_currentRelPath, relPathAtStart, StringComparison.Ordinal))
            {
                // The write helpers target the CURRENT file — writing another file's
                // XML there would corrupt it. Same outcome as navigating away with
                // unsaved changes before this change existed.
                SetStatus("Save aborted: you navigated to a different file while saving. Re-open the file to save it.");
                return;
            }

            var saveInfo = await AtomicWriteTranslatedXmlForCurrentAsync(builtXml);

            _rawTranXml = builtXml;

            await RefreshFileStatusAsync(_currentRelPath);

            try
            {
                var tranAbs = GetWritePath(_currentRelPath);
                _renderCache.Invalidate(tranAbs);
            }
            catch { }

            if (skippedUnsafe > 0)
            {
                // Some paragraphs could NOT be written (unsafe markup / drifted target). Their
                // units kept IsDirty and their edited EN inside `doc` (== _indexedDoc); the
                // patched units were already cleared by BuildTranslatedXml. Rebuilding the index
                // from the freshly written file and replacing the editor text would ERASE the
                // user's typed-but-unwritten translation for those paragraphs (round-2 review
                // finding 1). Instead, keep the current editor text and the dirty flag so the
                // unsaved edits survive in the editor alongside the warning below.
                _dirty = true;
                // Re-baseline to the CURRENT editor text so external-change detection has a sane
                // reference; _dirty stays true explicitly because unwritten edits remain.
                _baselineTranSha1 = Sha1Hex(editedProjection);
                _lastSeenTranSha1 = _baselineTranSha1;
                UpdateWindowTitle();
            }
            else
            {
                _indexedDoc = _indexedTranslation.BuildIndex(_rawOrigXml, _rawTranXml);
                var freshProjection = _indexedTranslation.RenderProjection(_indexedDoc, _translationMode);
                SetTranslationProjection(_translationMode, freshProjection);

                _baselineTranSha1 = Sha1Hex(freshProjection);
                _lastSeenTranSha1 = _baselineTranSha1;
                _dirty = false;
                UpdateWindowTitle();
            }

            try { await RefreshReadableFromDiskOnlyAsync(); }
            catch (Exception refreshEx)
            {
                // Post-save refresh can fail on Mac due to file timing; keep this non-fatal.
                System.Diagnostics.Debug.WriteLine($"[SaveXml] Post-save refresh failed (non-critical): {refreshEx.Message}");
            }

            var backupMsg = saveInfo.BackupCreated ? " backup=yes" : " backup=no";
            var sourceName = _translationSourceIndex < _translationSourceOptions.Count
                ? _translationSourceOptions[_translationSourceIndex] : "active source";
            var skippedMsg = skippedUnsafe > 0
                ? $" WARNING: {skippedUnsafe:n0} paragraph(s) could NOT be saved automatically and were left unchanged to protect their XML structure (e.g. inline markup spanning a line break, or a translation target that changed on disk). Your edits to them were NOT written and remain in the editor as unsaved changes."
                : "";
            SetStatus($"Saved ({updatedCount:n0} units updated) to {sourceName}.{backupMsg}{skippedMsg}");
        }
        catch (Exception ex)
        {
            SetStatus("Save failed: " + ex.Message);
        }
        finally
        {
            _saveInFlight = false;
        }
    }

    public async Task ResetTranslatedToUntranslatedAsync()
    {
        try
        {
            if (_currentRelPath == null) { SetStatus("Nothing to reset."); return; }
            if (_activeTranslatedDir == null && _translatedDir == null) { SetStatus("Fresh start unavailable."); return; }
            if (IsActiveTranslationReadOnly) { SetStatus("Cannot fresh start: viewing another user's translation (read-only)."); return; }

            bool confirmed = await (ShowYesNoDialogAsync?.Invoke(
                "Fresh Start Translation",
                "This will replace the current writable translation for this file with the original untranslated XML.\n\nAll saved translation edits for this file in the active translation source will be lost.\n\nDo you want to continue?")
                ?? Task.FromResult(false));
            if (!confirmed)
            {
                SetStatus("Fresh start canceled.");
                return;
            }

            _rawOrigXml = await ReadOriginalXmlAsync(_currentRelPath);
            if (string.IsNullOrWhiteSpace(_rawOrigXml))
            {
                SetStatus("Fresh start failed: original XML could not be read.");
                return;
            }

            EnsureXmlIsWellFormed(_rawOrigXml, "Original XML is malformed.");

            await AtomicWriteTranslatedXmlForCurrentAsync(_rawOrigXml);

            _rawTranXml = _rawOrigXml;
            _indexedDoc = _indexedTranslation.BuildIndex(_rawOrigXml, _rawTranXml);
            var projection = _indexedTranslation.RenderProjection(_indexedDoc, _translationMode);
            SetTranslationProjection(_translationMode, projection);

            SetBaselineFromCurrentTranslatedEditorText();
            await RefreshFileStatusAsync(_currentRelPath);

            try
            {
                var tranAbs = GetWritePath(_currentRelPath);
                _renderCache.Invalidate(tranAbs);
            }
            catch { }

            await RefreshReadableFromDiskOnlyAsync();
            SetStatus("Reset translation to untranslated state.");
        }
        catch (Exception ex)
        {
            SetStatus("Fresh start failed: " + ex.Message);
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
            var relKey = NormalizeRel(relPath);
            var best = EvaluateBestTranslationSource(relPath);
            var newStatus = best.Status;
            long mtimeTicks = best.TranslatedMtimeTicks;

            if (_allItemsByRel.TryGetValue(relKey, out var existing))
            {
                if (!Equals(existing.Status, newStatus))
                {
                    existing.Status = newStatus;
                    MarkIndexCacheDirty();
                }
                existing.TranslatedMtimeTicks = mtimeTicks;

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
        var sourceLabel = !string.IsNullOrWhiteSpace(file) && _translationSourceIndex >= 0 && _translationSourceIndex < _translationSourceOptions.Count
            ? _translationSourceOptions[_translationSourceIndex] : null;
        var sourceSuffix = sourceLabel != null ? $" [{sourceLabel}]" : "";
        var title = string.IsNullOrWhiteSpace(file) ? (AppTitleBase + star) : (AppTitleBase + star + " - " + file + sourceSuffix);
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

    /// <summary>
    /// Ensures a translated XML file exists for the given relative path.
    /// Returns the absolute path of the saved/found file, or null on failure.
    /// </summary>
    public async Task<string?> EnsureTranslatedXmlForRelPathAsync(string relPath, bool saveCurrentEditor)
    {
        if (_originalDir == null || (_translatedDir == null && _activeTranslatedDir == null)) return null;

        var origPath = Path.Combine(_originalDir, relPath);
        if (!File.Exists(origPath)) return null;

        if (saveCurrentEditor &&
            _indexedDoc != null &&
            string.Equals(_currentRelPath, relPath, StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var projection = GetTranslationProjectionText?.Invoke() ?? "";
                _indexedTranslation.ApplyProjectionEdits(_indexedDoc, _translationMode, projection);

                var xml = _indexedTranslation.BuildTranslatedXml(_indexedDoc, out _);
                var tranAbs = GetWritePath(relPath);

                await AtomicWriteXmlAsync(tranAbs, xml);

                _rawTranXml = xml;
                return tranAbs;
            }
            catch
            {
                return null;
            }
        }

        await EnsureTranslatedXmlExistsForRelPathAsync(relPath);
        return FindTranslatedPath(relPath);
    }

    public async Task<bool> EnsurePersonalTranslatedXmlForRelPathAsync(string relPath, bool saveCurrentEditor)
    {
        if (_originalDir == null || string.IsNullOrWhiteSpace(_root)) return false;

        var origPath = Path.Combine(_originalDir, relPath);
        if (!File.Exists(origPath)) return false;

        var personalDir = _userTranslatedDir;
        if (string.IsNullOrWhiteSpace(personalDir))
        {
            // Recompute from the active corpus's translations repo, not _root.
            // See LoadRootAsync for the rationale (multi-corpus data-loss bug).
            personalDir = !string.IsNullOrEmpty(_translationRoot)
                ? AppPaths.GetUserTranslatedDirForRepo(_translationRoot!, GetTranslationFolderKey(_config))
                : AppPaths.GetUserTranslatedDir(_root, GetTranslationFolderKey(_config));
            _userTranslatedDir = personalDir;
        }

        var personalAbs = Path.Combine(personalDir!, relPath);
        var personalParentDir = Path.GetDirectoryName(personalAbs);
        if (!string.IsNullOrWhiteSpace(personalParentDir) && !Directory.Exists(personalParentDir))
            Directory.CreateDirectory(personalParentDir);

        if (saveCurrentEditor &&
            _indexedDoc != null &&
            string.Equals(_currentRelPath, relPath, StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var projection = GetTranslationProjectionText?.Invoke() ?? "";
                _indexedTranslation.ApplyProjectionEdits(_indexedDoc, _translationMode, projection);

                var xml = _indexedTranslation.BuildTranslatedXml(_indexedDoc, out _);
                await AtomicWriteXmlAsync(personalAbs, xml);

                if (string.Equals(_activeTranslatedDir, _userTranslatedDir, StringComparison.OrdinalIgnoreCase))
                    _rawTranXml = xml;
                return true;
            }
            catch
            {
                return false;
            }
        }

        var existing = FindTranslatedPath(relPath);
        if (!string.IsNullOrWhiteSpace(existing) && File.Exists(existing))
        {
            if (!string.Equals(existing, personalAbs, StringComparison.OrdinalIgnoreCase))
                File.Copy(existing, personalAbs, overwrite: true);
            return true;
        }

        var origXml = await ReadOriginalXmlAsync(relPath);
        if (string.IsNullOrWhiteSpace(origXml)) return false;
        await AtomicWriteXmlAsync(personalAbs, origXml);
        return true;
    }

    public async Task HandleRootClonedAsync(string repoRoot, bool isSecondaryWindow)
    {
        try
        {
            if (!await ConfirmNavigateIfDirtyAsync("load a different root")) return;

            // A clone OR sync just changed the corpus on disk. The cache's
            // git-HEAD gate (added 2026-04-12) will catch the common case
            // where HEAD moved, but we can't trust that alone — a script,
            // hand-edit, or stash apply might have introduced new files
            // without moving HEAD. Belt-and-braces: delete the per-corpus
            // cache files for every discovered corpus under this root so
            // the next LoadFileListFromCacheOrBuildAsync rebuilds from disk.
            //
            // The path-based cache lookup is corpus-aware (each translations
            // repo has its own index.cache.json under its own root), so we
            // need to walk the discovered corpora rather than guess.
            try
            {
                foreach (var layout in AppPaths.DiscoverAllCorpora(repoRoot))
                {
                    var cachePath = _indexCacheService.GetCachePath(layout.TranslationsRepoRoot);
                    if (System.IO.File.Exists(cachePath))
                    {
                        try { System.IO.File.Delete(cachePath); }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"[MainWindowViewModel] Could not delete stale cache {cachePath}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MainWindowViewModel] Cache wipe before clone-load failed: {ex.Message}");
            }

            await LoadRootAsync(repoRoot, saveToConfig: true);
            // LoadRootAsync now fires the status refresh in the background;
            // no need to await it here. Just update the filter view.
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
        if (string.IsNullOrWhiteSpace(_translationRoot)) return;
        try
        {
            var reviewsDir = ITranslationReviewService.GetCommunityReviewsDir(_translationRoot);
            await _translationReview.RefreshAggregationCacheAsync(_translationRoot, reviewsDir);
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
            if (_translationRoot == null || _originalDir == null || _translatedDir == null)
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
                _translationRoot,
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

    public Task OpenTermbaseEditorAsync()
    => OpenTermbaseEditorAsync(term: null, communityUser: null);

public Task OpenTermbaseEditorAsync(string? term, string? communityUser = null)
{
    // This remains partially in code-behind because it creates a Window.
    // VM signals intent; code-behind creates the TermbaseEditorWindow.
    // The bridge delegate handles the actual window creation.
    if (string.IsNullOrWhiteSpace(_translationRoot))
    {
        SetStatus("Cannot open termbase editor: no root is loaded.");
        return Task.CompletedTask;
    }

    var localEditorUsername = _config.GitHubUsername ?? _config.Username;
    var activeDictionaryUser = GetActiveDictionaryUser();
    if (string.IsNullOrWhiteSpace(communityUser)
        && !string.IsNullOrWhiteSpace(activeDictionaryUser)
        && !string.Equals(activeDictionaryUser, localEditorUsername, StringComparison.OrdinalIgnoreCase))
    {
        communityUser = activeDictionaryUser;
    }

    OpenTermbaseEditorRequested?.Invoke(_translationRoot, localEditorUsername, term, communityUser);
    return Task.CompletedTask;
}

/// <summary>
/// Event for code-behind to handle termbase editor window creation.
/// </summary>
public Action<string, string?, string?, string?>? OpenTermbaseEditorRequested { get; set; }

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
        if (_originalDir == null || (_translatedDir == null && _activeTranslatedDir == null) || _currentRelPath == null) return;

        try
        {
            var origAbs = Path.Combine(_originalDir, _currentRelPath);
            var tranAbs = FindTranslatedPath(_currentRelPath) ?? GetWritePath(_currentRelPath);
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
    // Per-user translation directories
    // ===========================================================

    private List<string> _translationSourceOptions = new();
    private int _translationSourceIndex;

    /// <summary>
    /// For reads: choose the most translated available source for this file.
    /// Community wins ties.
    /// </summary>
    private string? FindTranslatedPath(string relPath)
    {
        return EvaluateBestTranslationSource(relPath).Path;
    }

    private TranslationSourceEvaluation EvaluateBestTranslationSource(string relPath)
    {
        if (string.IsNullOrWhiteSpace(relPath) || _originalDir == null || string.IsNullOrWhiteSpace(_root))
            return new TranslationSourceEvaluation(Math.Clamp(_translationSourceIndex, 0, Math.Max(0, _translationSourceOptions.Count - 1)), null, TranslationStatus.Red, false, 0, DateTime.MinValue);

        var origAbs = Path.Combine(_originalDir, relPath);
        var relKey = NormalizeRel(relPath);
        TranslationSourceEvaluation? best = null;

        for (int index = 0; index < _translationSourceOptions.Count; index++)
        {
            var dir = ResolveTranslatedDirForSourceIndex(index);
            if (string.IsNullOrWhiteSpace(dir))
                continue;

            var candidatePath = Path.Combine(dir, relPath);
            TranslationStatus status = TranslationStatus.Red;
            long mtimeTicks = 0;
            DateTime writeUtc = DateTime.MinValue;

            if (File.Exists(candidatePath) && IsMeaningfullyTranslatedPath(relPath, candidatePath))
            {
                status = LiveTranslationStatusService.ComputeStatusForPairLive(origAbs, candidatePath, _root!, relKey, verboseLog: false);
                try
                {
                    writeUtc = File.GetLastWriteTimeUtc(candidatePath);
                    mtimeTicks = writeUtc.Ticks;
                }
                catch { }
            }

            int starCount = 0;
            if (_starService != null && index < _translationSourceOptions.Count)
            {
                var translator = index == 0 ? GetTranslationFolderKey(_config)
                    : index == 1 ? "community"
                    : index < _translationSourceOptions.Count ? _translationSourceOptions[index] : "community";
                starCount = _starService.GetStarCount(relKey, translator);
            }

            var evaluation = new TranslationSourceEvaluation(
                index,
                status == TranslationStatus.Red ? null : candidatePath,
                status,
                index == 1,
                mtimeTicks,
                writeUtc,
                starCount);

            if (IsBetterTranslationSource(evaluation, best))
                best = evaluation;
        }

        if ((best == null || best.Path == null) && !string.IsNullOrWhiteSpace(_translatedCacheDir))
        {
            var cachePath = Path.Combine(_translatedCacheDir, relPath);
            if (File.Exists(cachePath))
            {
                return new TranslationSourceEvaluation(
                    _translationSourceOptions.Count > 1 ? 1 : 0,
                    cachePath,
                    TranslationStatus.Red,
                    false,
                    0,
                    DateTime.MinValue);
            }
        }

        return best ?? new TranslationSourceEvaluation(
            _translationSourceOptions.Count > 1 ? 1 : 0,
            null,
            TranslationStatus.Red,
            _translationSourceOptions.Count > 1,
            0,
            DateTime.MinValue);
    }

    private static bool IsBetterTranslationSource(TranslationSourceEvaluation candidate, TranslationSourceEvaluation? currentBest)
    {
        if (currentBest == null)
            return true;

        int candidateRank = GetTranslationStatusRank(candidate.Status);
        int bestRank = GetTranslationStatusRank(currentBest.Status);
        if (candidateRank != bestRank)
            return candidateRank > bestRank;

        // Higher star count wins at equal quality
        if (candidate.StarCount != currentBest.StarCount)
            return candidate.StarCount > currentBest.StarCount;

        if (candidate.IsCommunity != currentBest.IsCommunity)
            return !candidate.IsCommunity; // Prefer personal over community at equal quality

        if (candidate.LastWriteUtc != currentBest.LastWriteUtc)
            return candidate.LastWriteUtc > currentBest.LastWriteUtc;

        return candidate.Index < currentBest.Index;
    }

    private static int GetTranslationStatusRank(TranslationStatus status) => status switch
    {
        TranslationStatus.Green => 2,
        TranslationStatus.Yellow => 1,
        _ => 0,
    };

    private int ResolveBestTranslationSourceIndex(string relPath) => EvaluateBestTranslationSource(relPath).Index;
    private bool IsMeaningfullyTranslatedPath(string relPath, string candidatePath)
    {
        try
        {
            if (_originalDir == null || string.IsNullOrWhiteSpace(relPath) || string.IsNullOrWhiteSpace(candidatePath))
                return false;

            var originalPath = Path.Combine(_originalDir, relPath);
            if (!File.Exists(originalPath) || !File.Exists(candidatePath))
                return false;

            var originalWriteUtc = File.GetLastWriteTimeUtc(originalPath);
            var candidateInfo = new FileInfo(candidatePath);
            var candidateWriteUtc = candidateInfo.LastWriteTimeUtc;
            var candidateLength = candidateInfo.Length;

            if (_meaningfulTranslationCache.TryGetValue(candidatePath, out var cached)
                && cached.OriginalWriteUtc == originalWriteUtc
                && cached.CandidateWriteUtc == candidateWriteUtc
                && cached.CandidateLength == candidateLength)
            {
                return cached.IsMeaningful;
            }

            var originalXml = File.ReadAllText(originalPath, Encoding.UTF8);
            var candidateXml = File.ReadAllText(candidatePath, Encoding.UTF8);

            bool isMeaningful;
            if (TryParseXml(originalXml, out _)
                && TryParseXml(candidateXml, out _)
                && XNode.DeepEquals(XDocument.Parse(originalXml, LoadOptions.PreserveWhitespace), XDocument.Parse(candidateXml, LoadOptions.PreserveWhitespace)))
            {
                isMeaningful = false;
            }
            else
            {
                try
                {
                    var doc = _indexedTranslation.BuildIndex(originalXml, candidateXml);
                    isMeaningful = doc.Units.Any(u => !string.IsNullOrWhiteSpace(u.En));
                }
                catch
                {
                    isMeaningful = true;
                }
            }

            if (_meaningfulTranslationCache.Count > 5000)
                _meaningfulTranslationCache.Clear();
            _meaningfulTranslationCache[candidatePath] = new MeaningfulTranslationCacheEntry(
                originalWriteUtc,
                candidateWriteUtc,
                candidateLength,
                isMeaningful);

            return isMeaningful;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// For writes: to active dir, creating subdirs as needed.
    /// </summary>
    private string GetWritePath(string relPath)
    {
        var dir = _activeTranslatedDir ?? _translatedDir;
        var path = Path.Combine(dir!, relPath);
        var parentDir = Path.GetDirectoryName(path);
        if (parentDir != null && !Directory.Exists(parentDir))
            Directory.CreateDirectory(parentDir);
        return path;
    }

    /// <summary>
    /// Populates the translation source ComboBox with "My Translation", "Community",
    /// and any other users' translation directories found on disk.
    /// </summary>
    public void RefreshTranslationSources()
    {
        var displayName = string.IsNullOrWhiteSpace(_config.Username) ? (_config.GitHubUsername ?? "User") : _config.Username;
        var options = new List<string> { $"My Translation ({displayName})", "Community" };

        if (_translationRoot != null)
        {
            var communityTransDir = Path.Combine(_translationRoot, "community", "translations");
            if (Directory.Exists(communityTransDir))
            {
                foreach (var dir in Directory.GetDirectories(communityTransDir))
                {
                    var username = Path.GetFileName(dir);
                    if (string.Equals(username, GetTranslationFolderKey(_config), StringComparison.OrdinalIgnoreCase))
                        continue;

                    // Only show this user if they have a file for the currently-
                    // open text. File.Exists is fast (no XML parsing); the
                    // meaningful-translation check (which reads + parses XML)
                    // runs later when the source is actually selected, not
                    // during dropdown population — avoids UI freeze.
                    if (!string.IsNullOrWhiteSpace(_currentRelPath))
                    {
                        var candidatePath = Path.Combine(dir, _currentRelPath);
                        if (!File.Exists(candidatePath))
                            continue;
                        // Skip users whose translation is identical to the original
                        if (!IsMeaningfullyTranslatedPath(_currentRelPath, candidatePath))
                            continue;
                    }

                    options.Add(username);
                }
            }
        }

        _translationSourceOptions = options;
        _translationSourceIndex = Math.Clamp(_translationSourceIndex, 0, Math.Max(0, options.Count - 1));

        // Build display list with star counts for the UI (raw usernames stay in _translationSourceOptions)
        var displayOptions = new List<string>(options);
        if (_starService != null && !string.IsNullOrWhiteSpace(_currentRelPath))
        {
            var relKey = NormalizeRel(_currentRelPath);
            for (int i = 2; i < displayOptions.Count; i++)
            {
                var stars = _starService.GetStarCount(relKey, displayOptions[i]);
                if (stars > 0)
                    displayOptions[i] = $"{displayOptions[i]} ({stars}\u2605)";
            }
        }

        SetTranslationSourceOptions?.Invoke(displayOptions);
        SetReadableTranslationSourceOptions?.Invoke(displayOptions);
        SetScholarDictionarySourceOptions?.Invoke(displayOptions);
        SetTranslationSourceIndex?.Invoke(_translationSourceIndex);
        SetReadableTranslationSourceIndex?.Invoke(_translationSourceIndex);
        SetScholarDictionarySourceIndex?.Invoke(_translationSourceIndex);
        RefreshStarButtons();
    }

    /// <summary>
    /// Switches the active translation source: 0 = My Translation, 1 = Community, 2+ = other user (read-only).
    /// </summary>
    public async Task SwitchTranslationSourceAsync(int index)
    {
        if (index < 0 || index >= _translationSourceOptions.Count) return;
        // Guard: if the index didn't actually change, bail. This breaks the
        // re-entrancy loop where RefreshTranslationSources (called at the end
        // of LoadPairAsync) sets the ComboBox to the current value → fires
        // SelectionChanged → calls SwitchTranslationSourceAsync → calls
        // LoadPairAsync → RefreshTranslationSources → infinite loop → hang.
        if (index == _translationSourceIndex) return;

        if (!await ConfirmNavigateIfDirtyAsync("switch translation source"))
        {
            SetTranslationSourceIndex?.Invoke(_translationSourceIndex);
            SetReadableTranslationSourceIndex?.Invoke(_translationSourceIndex);
            SetScholarDictionarySourceIndex?.Invoke(_translationSourceIndex);
            return;
        }

        _userHasManuallySelectedSource = true;

        ApplyTranslationSourceIndex(index);

        // Reload current file with new source
        if (_currentRelPath != null)
            await LoadPairAsync(_currentRelPath, autoChooseSource: false);
    }

    private void ApplyTranslationSourceIndex(int index)
    {
        if (index < 0 || index >= _translationSourceOptions.Count) return;

        _translationSourceIndex = index;
        _activeTranslatedDir = ResolveTranslatedDirForSourceIndex(index);

        SetTranslationEditorReadOnly?.Invoke(IsActiveTranslationReadOnly);
        SetTranslationSourceIndex?.Invoke(_translationSourceIndex);
        SetReadableTranslationSourceIndex?.Invoke(_translationSourceIndex);
        SetScholarDictionarySourceIndex?.Invoke(_translationSourceIndex);
        SetScholarTranslationDirs?.Invoke(_originalDir, GetActiveTranslatedDir());
        try { _translationAssistant.SetUsername(GetActiveDictionaryUser()); } catch { }
        try { SetScholarAssistantUsername?.Invoke(GetActiveDictionaryUser()); } catch { }
        PushSearchContext();
    }

    private string? ResolveTranslatedDirForSourceIndex(int index)
    {
        if (index == 0)
            return _userTranslatedDir;
        if (index == 1)
            return _translatedDir;
        if (index >= 2 && index < _translationSourceOptions.Count && !string.IsNullOrWhiteSpace(_root))
        {
            // Use the active corpus's translations repo for "other user" sources
            // so we don't accidentally read from CBETA when the user is in OpenZen.
            return !string.IsNullOrEmpty(_translationRoot)
                ? AppPaths.GetUserTranslatedDirForRepo(_translationRoot!, _translationSourceOptions[index])
                : AppPaths.GetUserTranslatedDir(_root!, _translationSourceOptions[index]);
        }
        return null;
    }

    /// <summary>
    /// True when viewing another user's translation (read-only).
    /// </summary>
    public bool IsActiveTranslationReadOnly => _translationSourceIndex >= 2;

    /// <summary>
    /// Returns the user whose translation is currently active.
    /// Index 0 = current user's own translation, index 1 = community (null), index 2+ = other user's name.
    /// </summary>
    public string? GetActiveTranslationUser()
    {
        if (_translationSourceIndex == 0) return _config.GitHubUsername ?? _config.Username;
        if (_translationSourceIndex == 1) return null; // community
        if (_translationSourceIndex >= 2 && _translationSourceIndex < _translationSourceOptions.Count)
            return _translationSourceOptions[_translationSourceIndex];
        return null;
    }

    public string GetActiveSearchSourceKey(bool forShareableLink = false)
    {
        if (_translationSourceIndex == 0)
        {
            if (forShareableLink)
            {
                var user = GetActiveTranslationUser();
                return string.IsNullOrWhiteSpace(user) ? "me" : user;
            }
            return "me";
        }
        if (_translationSourceIndex == 1) return "community";
        return GetActiveTranslationUser() ?? "community";
    }

    public async Task<bool> RestoreSearchTranslationSourceAsync(string? sourceKey)
    {
        if (string.IsNullOrWhiteSpace(sourceKey) || _translationSourceOptions.Count == 0)
            return false;

        int? targetIndex = sourceKey.Trim().ToLowerInvariant() switch
        {
            "me" => 0,
            "community" => _translationSourceOptions.Count > 1 ? 1 : null,
            _ => ResolveTranslationSourceIndexForNavigation(sourceKey)
        };

        if (!targetIndex.HasValue)
            return false;

        if (targetIndex.Value != _translationSourceIndex)
            await SwitchTranslationSourceAsync(targetIndex.Value);

        return true;
    }

    private async Task EnsureTranslationSourceForNavigationAsync(NavigationRequest request)
    {
        if (request.Side != SearchSide.Translated || _translationSourceOptions.Count == 0)
            return;

        int? targetIndex = ResolveTranslationSourceIndexForNavigation(request.User);
        if (!targetIndex.HasValue || targetIndex.Value == _translationSourceIndex)
            return;

        await SwitchTranslationSourceAsync(targetIndex.Value);
    }

    private int? ResolveTranslationSourceIndexForNavigation(string? requestedUser)
    {
        if (string.IsNullOrWhiteSpace(requestedUser))
            return _translationSourceOptions.Count > 1 ? 1 : null;

        var normalizedRequested = requestedUser.Trim();
        if (string.IsNullOrWhiteSpace(normalizedRequested))
            return _translationSourceOptions.Count > 1 ? 1 : null;

        var currentUserKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(_config.Username))
            currentUserKeys.Add(_config.Username.Trim());
        if (!string.IsNullOrWhiteSpace(_config.GitHubUsername))
            currentUserKeys.Add(_config.GitHubUsername.Trim());

        var currentFolderKey = GetTranslationFolderKey(_config);
        if (!string.IsNullOrWhiteSpace(currentFolderKey))
            currentUserKeys.Add(currentFolderKey);

        if (currentUserKeys.Contains(normalizedRequested))
            return 0;

        for (int i = 2; i < _translationSourceOptions.Count; i++)
        {
            if (string.Equals(_translationSourceOptions[i], normalizedRequested, StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return null;
    }

    /// <summary>
    /// Returns the current list of translation source labels (e.g. "My Translation (user)", "Community", other usernames).
    /// </summary>
    public IReadOnlyList<string> GetTranslationSourceLabels() => _translationSourceOptions;
    public int GetActiveTranslationSourceIndex() => _translationSourceIndex;

    /// <summary>
    /// Toggles the star state for the current file + translation source combination.
    /// </summary>
    public async Task ToggleStarAsync()
    {
        if (_starService == null || string.IsNullOrWhiteSpace(_currentRelPath)) return;

        var username = _config.GitHubUsername ?? _config.Username;
        if (string.IsNullOrWhiteSpace(username)) { SetStatus("Cannot star: no username configured."); return; }
        if (_translationSourceIndex == 0) { SetStatus("Cannot star your own translation."); return; }

        // Use the folder key (sanitized) for index 0 to match EvaluateBestTranslationSource
        var translator = _translationSourceIndex == 0
            ? GetTranslationFolderKey(_config)
            : (GetActiveTranslationUser() ?? "community");
        var fileId = NormalizeRel(_currentRelPath);

        var communityStarsDir = _translationRoot != null
            ? Path.Combine(_translationRoot, "community", "stars")
            : null;
        if (string.IsNullOrWhiteSpace(communityStarsDir)) { SetStatus("Cannot star: no community directory found."); return; }

        bool isCurrentlyStarred = _starService.IsStarredByUser(fileId, translator, username);
        try
        {
            await _starService.SetStarAsync(communityStarsDir, username, fileId, translator, !isCurrentlyStarred, CancellationToken.None);
            RefreshStarButtons();
            SetStatus(isCurrentlyStarred ? "Star removed." : "Star added.");
        }
        catch (Exception ex)
        {
            SetStatus($"Star toggle failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Refreshes the star button state on both views based on current file and source.
    /// </summary>
    public void RefreshStarButtons()
    {
        if (_starService == null || string.IsNullOrWhiteSpace(_currentRelPath))
        {
            UpdateReadableStarButton?.Invoke(null);
            UpdateTranslationStarButton?.Invoke(null);
            return;
        }

        var username = _config.GitHubUsername ?? _config.Username;
        if (string.IsNullOrWhiteSpace(username))
        {
            UpdateReadableStarButton?.Invoke(null);
            UpdateTranslationStarButton?.Invoke(null);
            return;
        }

        // Hide the star button if the active source has no meaningful translation
        // (identical to source, or Chinese-only with no English content).
        if (_translationSourceIndex >= 0 && !string.IsNullOrWhiteSpace(_currentRelPath))
        {
            var candidateDir = ResolveTranslatedDirForSourceIndex(_translationSourceIndex);
            if (candidateDir != null)
            {
                var candidatePath = Path.Combine(candidateDir, _currentRelPath);
                if (!File.Exists(candidatePath) || !IsMeaningfullyTranslatedPath(_currentRelPath, candidatePath))
                {
                    UpdateReadableStarButton?.Invoke(null);
                    UpdateTranslationStarButton?.Invoke(null);
                    return;
                }
            }
        }

        // Use the folder key (sanitized) for index 0 to match EvaluateBestTranslationSource
        var translator = _translationSourceIndex == 0
            ? GetTranslationFolderKey(_config)
            : (GetActiveTranslationUser() ?? "community");
        var fileId = NormalizeRel(_currentRelPath);
        bool isStarred = _starService.IsStarredByUser(fileId, translator, username);

        UpdateReadableStarButton?.Invoke(isStarred);
        UpdateTranslationStarButton?.Invoke(isStarred);
    }

    private string GetSearchTranslatedDir() => _activeTranslatedDir ?? _translatedDir!;

    /// <summary>
    /// Builds a search file index that includes both the active corpus's items
    /// AND secondary corpus titles. Does NOT modify _allItems (nav stays clean).
    /// </summary>
    private List<FileNavItem> BuildSearchFileIndex()
    {
        var result = new List<FileNavItem>(_allItems);
        if (_availableCorpora.Count < 2) return result;

        var existingPaths = new HashSet<string>(
            result.Select(i => i.RelPath), StringComparer.OrdinalIgnoreCase);

        foreach (var layout in _availableCorpora)
        {
            if (layout.Kind == ActiveCorpus) continue;
            var corpusLabel = layout.Kind == CorpusKind.Open ? "[OpenZen] " : "[CBETA] ";

            var titlesPath = Path.Combine(layout.TranslationsRepoRoot, "titles.jsonl");
            if (!File.Exists(titlesPath)) continue;

            try
            {
                foreach (var line in File.ReadLines(titlesPath, System.Text.Encoding.UTF8))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    try
                    {
                        using var doc = System.Text.Json.JsonDocument.Parse(line);
                        var r = doc.RootElement;
                        if (!r.TryGetProperty("path", out var pathEl)) continue;
                        var path = pathEl.GetString();
                        if (string.IsNullOrWhiteSpace(path) || existingPaths.Contains(path)) continue;

                        string? zh = r.TryGetProperty("zh", out var zhEl) ? zhEl.GetString() : null;
                        string? en = r.TryGetProperty("en", out var enEl) ? enEl.GetString() : null;
                        string? enShort = r.TryGetProperty("enShort", out var esEl) ? esEl.GetString() : null;

                        var tooltipParts = new List<string>();
                        if (!string.IsNullOrWhiteSpace(en)) tooltipParts.Add(en);
                        if (!string.IsNullOrWhiteSpace(zh)) tooltipParts.Add(zh);
                        if (tooltipParts.Count == 0) tooltipParts.Add(path);

                        result.Add(new FileNavItem
                        {
                            RelPath = path,
                            FileName = Path.GetFileName(path),
                            DisplayShort = corpusLabel + (!string.IsNullOrWhiteSpace(enShort) ? enShort : Path.GetFileName(path)),
                            Tooltip = string.Join("\n", tooltipParts),
                            Status = TranslationStatus.Green,
                        });
                        existingPaths.Add(path);
                    }
                    catch { /* skip bad lines */ }
                }
            }
            catch { /* skip if titles.jsonl unreadable */ }
        }
        return result;
    }

    /// <summary>
    /// Builds a list of all translation directories: community xml-p5t first,
    /// then each user directory under community/translations/.
    /// </summary>
    private IReadOnlyList<string> BuildAllTranslatedDirs()
    {
        var dirs = new List<string>();
        if (_translatedDir != null) dirs.Add(_translatedDir); // community xml-p5t first
        if (_translationRoot != null)
        {
            var communityTransDir = Path.Combine(_translationRoot, "community", "translations");
            if (Directory.Exists(communityTransDir))
            {
                foreach (var userDir in Directory.EnumerateDirectories(communityTransDir))
                    dirs.Add(userDir);
            }
        }
        return dirs;
    }

    private void PushSearchContext()
    {
        if (string.IsNullOrWhiteSpace(_root) || string.IsNullOrWhiteSpace(_originalDir) || string.IsNullOrWhiteSpace(_translatedDir))
            return;

        var allTranslatedDirs = BuildAllTranslatedDirs();
        var indexRoot = _translationRoot ?? _root;
        SetSearchRootContext?.Invoke(indexRoot, _originalDir, allTranslatedDirs);
        var addOrigDirs2 = _availableCorpora
            .Where(c => c.Kind != ActiveCorpus && Directory.Exists(c.OriginalDir))
            .Select(c => c.OriginalDir).ToList();
        var addTransDirs2 = _availableCorpora
            .Where(c => c.Kind != ActiveCorpus && Directory.Exists(c.TranslatedDir))
            .Select(c => c.TranslatedDir).ToList();
        SetSearchContext?.Invoke(indexRoot, _originalDir, allTranslatedDirs,
            relKey =>
            {
                _allItemsByRel.TryGetValue(NormalizeRel(relKey), out var it);
                return it != null ? (it.DisplayShort, it.Tooltip, it.Status) : (relKey, relKey, null);
            },
            addOrigDirs2.Count > 0 ? addOrigDirs2 : null,
            addTransDirs2.Count > 0 ? addTransDirs2 : null);
        SetSearchZenResolver?.Invoke(rel => _zenTexts.IsZen(rel));
    }

    /// <summary>
    /// Renders the translation for the current file from the specified source index.
    /// Returns null if the source or file is unavailable.
    /// </summary>
    public RenderedDocument? RenderTranslationSource(int sourceIndex)
    {
        if (_currentRelPath == null || _originalDir == null || _root == null)
            return null;

        string? translatedDir;
        if (sourceIndex == 0) // My Translation
            translatedDir = _userTranslatedDir;
        else if (sourceIndex == 1) // Community
            translatedDir = _translatedDir;
        else if (sourceIndex >= 2 && sourceIndex < _translationSourceOptions.Count) // Other user
        {
            var username = _translationSourceOptions[sourceIndex];
            // Use the active corpus's translations repo for the lookup.
            translatedDir = !string.IsNullOrEmpty(_translationRoot)
                ? AppPaths.GetUserTranslatedDirForRepo(_translationRoot!, username)
                : AppPaths.GetUserTranslatedDir(_root, username);
        }
        else
            return null;

        if (translatedDir == null) return null;

        var filePath = Path.Combine(translatedDir, _currentRelPath);
        if (!File.Exists(filePath))
        {
            // Fallback: try the community dir if not found in the selected dir
            if (_translatedDir != null && translatedDir != _translatedDir)
            {
                filePath = Path.Combine(_translatedDir, _currentRelPath);
                if (!File.Exists(filePath)) return null;
            }
            else
                return null;
        }

        return TeiRenderer.Render(SafeReadAllTextUtf8(filePath));
    }

    /// <summary>
    /// Returns the repo-relative path for the translated file at the given source index.
    /// Used by the Compare window to populate version pickers.
    /// </summary>
    public string? GetTranslationSourceRepoRelPath(int sourceIndex)
    {
        if (_currentRelPath == null || _translationRoot == null) return null;

        string? translatedDir;
        if (sourceIndex == 0)
            translatedDir = _userTranslatedDir;
        else if (sourceIndex == 1)
            translatedDir = _translatedDir;
        else if (sourceIndex >= 2 && sourceIndex < _translationSourceOptions.Count)
        {
            var username = _translationSourceOptions[sourceIndex];
            translatedDir = !string.IsNullOrEmpty(_translationRoot)
                ? AppPaths.GetUserTranslatedDirForRepo(_translationRoot, username)
                : null;
        }
        else
            return null;

        if (translatedDir == null) return null;
        var absPath = Path.Combine(translatedDir, _currentRelPath);
        return Path.GetRelativePath(_translationRoot, absPath);
    }

    /// <summary>
    /// Copies the current file's translation from the active source to the user's own directory.
    /// </summary>
    public async Task CopyCurrentTranslationToMyDirAsync()
    {
        if (_currentRelPath == null || _userTranslatedDir == null) return;
        var sourcePath = FindTranslatedPath(_currentRelPath);
        if (sourcePath == null) { SetStatus("No translation to copy."); return; }

        var destPath = Path.Combine(_userTranslatedDir, _currentRelPath);
        var destDir = Path.GetDirectoryName(destPath);
        if (destDir != null) Directory.CreateDirectory(destDir);

        File.Copy(sourcePath, destPath, overwrite: true);
        SetStatus("Copied translation to your workspace.");

        // Switch to user's translation and reload
        await SwitchTranslationSourceAsync(0);
    }

    // ===========================================================
    // OpenAtAsync support (for secondary windows)
    // ===========================================================

    public async Task OpenAtCoreAsync(string root, NavigationRequest request)
    {
        await LoadRootAsync(root, saveToConfig: false);

        // Deep-link corpus routing: if the request's relPath belongs to a
        // different corpus than the currently active one, switch corpora
        // before loading the file. Otherwise the load will fail (the file
        // doesn't exist in the active corpus's tree) or worse, the wrong
        // file will load if a same-named file happens to exist in both.
        var requiredCorpus = InferCorpusForRelPath(request.RelPath);
        if (requiredCorpus != CorpusKind.Unknown && requiredCorpus != ActiveCorpus
            && _availableCorpora.Any(c => c.Kind == requiredCorpus))
        {
            await SwitchCorpusAsync(requiredCorpus);
        }
        else if (requiredCorpus != CorpusKind.Unknown && requiredCorpus != ActiveCorpus)
        {
            var name = requiredCorpus == CorpusKind.Open ? "OpenZen" : "CBETA";
            SetStatus($"This text requires the {name} corpus. Use Sync in the Community tab to download it.");
            return;
        }

        await EnsureTranslationSourceForNavigationAsync(request);
        // Ensure the nav filter has run so _filteredItems is populated and
        // the ListBox has items before we try to select + scroll into view.
        await ApplyFilterSafeAsync();
        SelectInNav(request.RelPath);
        await LoadPairAsync(request.RelPath);
        ForceTabIndex?.Invoke(0); // switch to Reader tab

        if (NavigateInReadable != null)
            await NavigateInReadable(request);
    }

    /// <summary>
    /// Infers which corpus a relative path belongs to based on its top-level
    /// directory. OpenZen files live under publisher-prefixed dirs
    /// (ws/, pd/, ce/, mit/); CBETA files live under canon-prefixed dirs
    /// (T/, X/, S/, etc.). Returns Unknown if the path doesn't match either
    /// shape so the caller can fall back to whatever's currently active.
    /// </summary>
    public CorpusKind InferCorpusForPath(string relPath) => InferCorpusForRelPath(relPath);

    private static CorpusKind InferCorpusForRelPath(string? relPath)
    {
        if (string.IsNullOrWhiteSpace(relPath)) return CorpusKind.Unknown;
        var normalized = relPath.Replace('\\', '/');
        var firstSlash = normalized.IndexOf('/');
        if (firstSlash <= 0) return CorpusKind.Unknown;
        var top = normalized[..firstSlash];
        // OpenZen publisher prefixes
        if (top.Equals("ws", StringComparison.OrdinalIgnoreCase) ||
            top.Equals("pd", StringComparison.OrdinalIgnoreCase) ||
            top.Equals("ce", StringComparison.OrdinalIgnoreCase) ||
            top.Equals("mit", StringComparison.OrdinalIgnoreCase))
            return CorpusKind.Open;
        // CBETA canon abbreviations are 1-3 ASCII letters
        if (top.Length >= 1 && top.Length <= 3 && top.All(char.IsLetter))
            return CorpusKind.Cbeta;
        return CorpusKind.Unknown;
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
        if (_activeTranslatedDir == null && _translatedDir == null) throw new InvalidOperationException("Translated directory not available.");

        var tranAbs = GetWritePath(_currentRelPath);
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
        if (_translationRoot == null) return;

        _indexCacheDirty = false;

        try
        {
            await _indexCacheService.SaveAsync(_translationRoot, new IndexCache { Entries = _allItems });
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

    private static async Task InvokeUiActionAsync(Action action)
    {
        if (Application.Current == null)
        {
            action();
            return;
        }

        await Dispatcher.UIThread.InvokeAsync(action);
    }

    private static string ReadAllTextUtf8Strict(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path is empty.", nameof(path));

        if (!File.Exists(path))
            throw new FileNotFoundException("File not found.", path);

        return File.ReadAllText(path, Encoding.UTF8);
    }

    private static string GetTranslationFolderKey(AppConfig config)
    {
        if (!string.IsNullOrWhiteSpace(config.GitHubUsername))
            return AppPaths.SanitizeUsername(config.GitHubUsername);
        if (!string.IsNullOrWhiteSpace(config.Username))
            return AppPaths.SanitizeUsername(config.Username);
        return "User";
    }

    private async Task RefreshUserTranslationDirectoryAsync(string? previousFolderKey)
    {
        if (string.IsNullOrWhiteSpace(_root))
            return;

        var canonicalKey = GetTranslationFolderKey(_config);
        // Use the active corpus's translations repo so the username-rename
        // refresh stays inside the right corpus's tree.
        var canonicalDir = !string.IsNullOrEmpty(_translationRoot)
            ? AppPaths.GetUserTranslatedDirForRepo(_translationRoot!, canonicalKey)
            : AppPaths.GetUserTranslatedDir(_root, canonicalKey);
        var previousKey = string.IsNullOrWhiteSpace(previousFolderKey) ? null : AppPaths.SanitizeUsername(previousFolderKey);
        var previousDir = string.IsNullOrWhiteSpace(previousKey) || string.Equals(previousKey, canonicalKey, StringComparison.OrdinalIgnoreCase)
            ? null
            : (!string.IsNullOrEmpty(_translationRoot)
                ? AppPaths.GetUserTranslatedDirForRepo(_translationRoot!, previousKey!)
                : AppPaths.GetUserTranslatedDir(_root, previousKey!));

        if (!string.IsNullOrWhiteSpace(previousDir) && Directory.Exists(previousDir))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(canonicalDir)!);

            if (!Directory.Exists(canonicalDir))
            {
                try
                {
                    Directory.Move(previousDir, canonicalDir);
                }
                catch (IOException)
                {
                    MergeDirectoryContents(previousDir, canonicalDir);
                    TryDeleteDirectoryRecursive(previousDir);
                }
                catch (UnauthorizedAccessException)
                {
                    MergeDirectoryContents(previousDir, canonicalDir);
                    TryDeleteDirectoryRecursive(previousDir);
                }
            }
            else
            {
                MergeDirectoryContents(previousDir, canonicalDir);
                TryDeleteDirectoryRecursive(previousDir);
            }
        }
        _userTranslatedDir = canonicalDir;
        if (_translationSourceIndex == 0 || string.IsNullOrWhiteSpace(_activeTranslatedDir))
            _activeTranslatedDir = _userTranslatedDir;
        RefreshTranslationSources();

        if (!string.IsNullOrWhiteSpace(_currentRelPath) && _translationSourceIndex == 0)
            await LoadPairAsync(_currentRelPath, autoChooseSource: false);
    }

    private static void MergeDirectoryContents(string sourceDir, string destDir)
    {
        foreach (var sourceFile in Directory.GetFiles(sourceDir, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(sourceDir, sourceFile);
            var destFile = Path.Combine(destDir, relative);
            var destParent = Path.GetDirectoryName(destFile);
            if (destParent != null && !Directory.Exists(destParent))
                Directory.CreateDirectory(destParent);

            if (!File.Exists(destFile))
                File.Copy(sourceFile, destFile);
        }
    }

    private static void TryDeleteDirectoryRecursive(string path)
    {
        try
        {
            if (Directory.Exists(path))
                Directory.Delete(path, recursive: true);
        }
        catch (IOException) { }
        catch (UnauthorizedAccessException) { }
    }
}


















