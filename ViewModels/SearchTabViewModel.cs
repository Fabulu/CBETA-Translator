using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using ReadZen.App.Models;
using ReadZen.App.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace ReadZen.App.ViewModels;

public partial class SearchTabViewModel : ViewModelBase
{
    public sealed class SearchUiState
    {
        public string Query { get; init; } = "";
        public bool SearchOriginal { get; init; } = true;
        public bool SearchTranslated { get; init; }
        public bool ZenOnly { get; init; }
        public int SelectedStatusIndex { get; init; }
        public int SelectedContextIndex { get; init; } = 2;
        public string? SelectedTagFilterName { get; init; }
        public string? SelectedTagFilterId { get; init; }
    }
    private readonly ISearchIndexService _svc;
    private readonly ISearchExportService _exportSvc;

    public SearchTabViewModel(
        ISearchIndexService searchIndexService,
        ISearchExportService? searchExportService = null,
        IMessenger? messenger = null)
    {
        _svc = searchIndexService ?? throw new ArgumentNullException(nameof(searchIndexService));
        _exportSvc = searchExportService ?? new SearchExportService();
        // Push the current (default-on) instant-search preference onto the service so the
        // query path honours it from the first search. Config load / settings apply then
        // override it via the SettingsAppliedMessage subscription below.
        ApplyInstantSearchToService();

        // MVVM ratchet: apply config-driven search prefs from the typed messenger, matching
        // the pattern GitTabView/ScholarTabView use for their config-driven state. The shell
        // broadcasts SettingsAppliedMessage both on startup config load
        // (MainWindowViewModel.LoadConfigApplyThemeAndMaybeAutoloadAsync) and on Settings ▸
        // Apply, so the persisted AppConfig.InstantSearch reaches Options.InstantSearch
        // instead of the ctor default overriding it. Weak registration — no unsubscribe
        // needed for the VM's lifetime. The messenger is injectable so tests can isolate
        // from the process-wide default; production passes null → WeakReferenceMessenger.Default.
        (messenger ?? WeakReferenceMessenger.Default)
            .Register<SearchTabViewModel, Messages.SettingsAppliedMessage>(
                this, static (vm, msg) => vm.InstantSearch = msg.Config.InstantSearch);
    }

    private bool _instantSearch = true;

    /// <summary>
    /// Instant search preference (default ON). When set it is pushed onto the search
    /// service options so <c>SearchAllAsync</c> ranks by index tf and lazily loads
    /// snippets for single-bigram queries. Wired from <c>AppConfig.InstantSearch</c> via
    /// the <c>SettingsAppliedMessage</c> subscription registered in the constructor, which
    /// fires on both startup config load and Settings ▸ Apply.
    /// </summary>
    public bool InstantSearch
    {
        get => _instantSearch;
        set
        {
            if (_instantSearch == value) return;
            _instantSearch = value;
            ApplyInstantSearchToService();
            OnPropertyChanged();
        }
    }

    private void ApplyInstantSearchToService()
    {
        // Options lives on the concrete service; the interface exposes it too, but test
        // fakes return a throwaway instance per get, so the set is harmlessly discarded
        // there and only takes effect on the real SearchIndexService.
        try { _svc.Options.InstantSearch = _instantSearch; } catch { }
    }

    private string? _root;
    private string? _originalDir;
    private IReadOnlyList<string>? _translatedDirs;
    private IReadOnlyList<string>? _additionalOriginalDirs;
    private IReadOnlyList<string>? _additionalTranslatedDirs;
    private bool _forceRebuildNextClick;

    private List<FileNavItem> _fileIndex = new();
    private Func<string, (string display, string tooltip, TranslationStatus? status)>? _meta;

    private CancellationTokenSource? _cts;
    private CancellationTokenSource? _autoRerunCts;
    private readonly List<SearchResultGroup> _groups = new();
    private int _batchedStateDepth;

    // 5B: cap visible children per group
    private const int MaxVisibleChildren = 5;
    private readonly Dictionary<string, List<SearchResultChild>> _fullChildrenMap = new(StringComparer.OrdinalIgnoreCase);

    // remember last search so dropdown recompute works
    private string _lastQuery = "";
    private int _lastContextWidth = 80;

    // avoid stale async metric recomputes racing each other
    private int _metricComputeVersion;

    // avoid stale search UI updates racing each other
    private int _searchRunVersion;
    private readonly SemaphoreSlim _resultEnrichmentGate = new(2, 2);

    // Zen flag lookup (provided by MainWindow via SetZenResolver)
    private Func<string, bool>? _isZen;

    // Master catalog for master card results
    private ZenMasterCatalog? _masterCatalog;
    private ZenMasterRecord? _matchedMaster; // preserved across result rebuilds

    // Tag filter
    private List<string> _tagFilterItems = new() { "All Tags" };
    private Dictionary<string, HashSet<string>>? _tagsByName; // tagName -> set of RelPaths
    private Dictionary<string, string>? _tagNameById; // tagId -> displayName
    private Dictionary<string, string>? _tagIdByName; // displayName -> tagId
    private string? _pendingRestoredTagId;
    private bool _userChangedContextWidth;
    private static readonly int[] ContextWidths = new[] { 5, 10, 15, 20, 40, 80, 160, 320 };
    private static bool _firstSearchSupportShown;

    // Search history (static so it persists across tab rebuilds within a session).
    // Thread safety: all mutations happen on the UI thread (LoadHistory during config load,
    // AddToHistory at end of StartSearchAsync which is Dispatcher-marshalled).
    private static readonly List<string> _searchHistory = new(20);

    public IReadOnlyList<string> SearchHistory => _searchHistory;

    /// <summary>Populate in-memory history from a previously loaded config.</summary>
    public void LoadHistory(IEnumerable<string> saved)
    {
        _searchHistory.Clear();
        foreach (var s in saved.Take(20))
            if (!string.IsNullOrWhiteSpace(s))
                _searchHistory.Add(s);
        OnPropertyChanged(nameof(SearchHistory));
    }

    /// <summary>Return a snapshot suitable for writing to AppConfig.</summary>
    public List<string> SnapshotHistory() => _searchHistory.ToList();

    // 4A: returns relPaths that the inverted index associates with the given query
    public HashSet<string>? GetIndexedRelPaths(string query)
    {
        if (_svc is not SearchIndexService concrete) return null;
        if (concrete.InvertedIndex?.IsLoaded != true) return null;
        var hits = concrete.InvertedIndex.Search(query);
        if (hits == null || hits.Length == 0) return null;
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var docId in hits)
        {
            var rp = concrete.InvertedIndex.GetRelPath(docId);
            if (rp != null) result.Add(rp);
        }
        return result;
    }

    // ----- Observable properties -----

    [ObservableProperty]
    private int _selectedSearchSubTabIndex;

    [ObservableProperty]
    private string _query = "";

    [ObservableProperty]
    private string _progressText = "Index not loaded.";

    [ObservableProperty]
    private string _summaryText = "Ready.";

    [ObservableProperty]
    private string _resultCountText = "";

    [ObservableProperty]
    private bool _hasResults;

    [ObservableProperty]
    private bool _isSearching;

    [ObservableProperty]
    private bool _isSearchProgressVisible;

    [ObservableProperty]
    private bool _isSearchProgressIndeterminate;

    [ObservableProperty]
    private double _searchProgressPercent;

    [ObservableProperty]
    private bool _isResultsLoadingVisible;

    [ObservableProperty]
    private string _resultsLoadingText = "Searching...";

    [ObservableProperty]
    private bool _isBuildingIndex;

    [ObservableProperty]
    private bool _searchOriginal = true;

    [ObservableProperty]
    private bool _searchTranslated = true;

    [ObservableProperty]
    private bool _zenOnly;

    [ObservableProperty]
    private string _coocSummaryText = "No data yet.";

    [ObservableProperty]
    private string _zipfText = "";

    [ObservableProperty]
    private int _selectedCoocMetricIndex;

    [ObservableProperty]
    private int _selectedAnalyticsScopeIndex;

    [ObservableProperty]
    private bool _isAnalyticsBusy;

    [ObservableProperty]
    private bool _isAnalyticsProgressVisible;

    [ObservableProperty]
    private double _analyticsProgressPercent;
    [ObservableProperty]
    private int _selectedStatusIndex;

    [ObservableProperty]
    private int _selectedTagFilterIndex;

    [ObservableProperty]
    private int _selectedContextIndex = 2;

    [ObservableProperty]
    private bool _isCancelEnabled;

    [ObservableProperty]
    private bool _isExportEnabled;

    [ObservableProperty]
    private bool _isEmptyStateVisible = true;


    [ObservableProperty]
    private string _validationMessage = "";

    [ObservableProperty]
    private bool _hasValidationError;

    [ObservableProperty]
    private string _leftTitle = "Character pairs";

    [ObservableProperty]
    private string _rightTitle = "Recurring phrases";

    [ObservableProperty]
    private bool _isMetricGuideVisible;

    [ObservableProperty]
    private bool _isMetricViewVisible = true;

    [ObservableProperty]
    private string? _coocFilterTerm;

    [ObservableProperty]
    private bool _isCoocFilterActive;

    [ObservableProperty]
    private bool _isMasterFilterActive;

    // Multi-master intersection filter: each entry narrows the result set.
    private readonly ObservableCollection<ActiveMasterFilter> _activeMasterFilters = new();
    public ObservableCollection<ActiveMasterFilter> ActiveMasterFilters => _activeMasterFilters;

    /// <summary>
    /// Display name for the current filter — joins all active master names with the intersection symbol.
    /// </summary>
    public string? MasterFilterName =>
        _activeMasterFilters.Count == 0
            ? null
            : string.Join(" \u2229 ", _activeMasterFilters.Select(m => m.MasterName));

    private HashSet<string>? _masterFilterRelPaths;

    [ObservableProperty]
    private string _resultFilter = "";

    private DispatcherTimer? _resultFilterDebounce;
    private DispatcherTimer? _streamFlushCoalescer;

    // PR A (load-all-snippets): true while LoadAllSnippetsAsync is in flight — keeps the
    // CanExecute false against re-entry and shows the toolbar progress affordance.
    [ObservableProperty]
    private bool _isLoadingAllSnippets;

    /// <summary>
    /// PR A (load-all-snippets): true iff at least one group in <see cref="ResultGroups"/>
    /// currently has at least one <see cref="SearchResultChild.IsSkippedVerify"/>=true child.
    /// Drives both the visibility of the "Load all snippets" toolbar button and its
    /// <c>CanExecute</c>. Recomputed via <see cref="RefreshHasSkippedVerifyRows"/> after
    /// the final rebuild and after a successful load — never on a hot path.
    /// </summary>
    [ObservableProperty]
    private bool _hasSkippedVerifyRows;

    partial void OnResultFilterChanged(string value)
    {
        if (_resultFilterDebounce == null)
        {
            _resultFilterDebounce = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(150) };
            _resultFilterDebounce.Tick += (_, _) => { _resultFilterDebounce.Stop(); ApplyResultFilter(); };
        }
        _resultFilterDebounce.Stop();
        _resultFilterDebounce.Start();
    }

    [ObservableProperty]
    private string _metricGuideText = "";

    [ObservableProperty]
    private string _metricTooltip = "Select a statistical metric for co-occurrence analysis";

    // ----- Collections -----

    public ObservableCollection<SearchResultGroup> ResultGroups { get; } = new();
    public bool IsSearchLoadingPlaceholderVisible => IsResultsLoadingVisible && ResultGroups.Count == 0 && !HasValidationError;
    public ObservableCollection<CoocRow> CoocChars { get; } = new();
    public ObservableCollection<CoocRow> CoocNgrams { get; } = new();
    public ObservableCollection<AnalyticsBubbleItem> CoocCharVisuals { get; } = new();
    public ObservableCollection<AnalyticsBubbleItem> CoocNgramVisuals { get; } = new();

    [ObservableProperty]
    private ISeries[] _charChartSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private ISeries[] _ngramChartSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private Axis[] _charChartYAxes = new[] { new Axis() };

    [ObservableProperty]
    private Axis[] _ngramChartYAxes = new[] { new Axis() };

    [ObservableProperty]
    private double _charChartHeight = 300;

    [ObservableProperty]
    private double _ngramChartHeight = 300;

    [ObservableProperty]
    private ISeries[] _scatterSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private Axis[] _scatterXAxes = new[] { new Axis() };

    [ObservableProperty]
    private Axis[] _scatterYAxes = new[] { new Axis() };

    // ----- Events -----

    public event EventHandler<string>? StatusChanged;
    public event EventHandler<NavigationRequest>? NavigationRequested;
    public event EventHandler<string>? OpenMasterRequested;

    // ----- Status/Context combo items (exposed for XAML) -----

    public string[] StatusItems { get; } = new[]
    {
        "All",
        "Red (untranslated)",
        "Yellow (WIP)",
        "Green (done)"
    };

    public string[] ContextItems { get; } = ContextWidths.Select(w => $"{w} chars").ToArray();

    public List<string> TagFilterItems
    {
        get => _tagFilterItems;
        set { _tagFilterItems = value; OnPropertyChanged(); }
    }

    public string[] CoocMetricItems { get; } = new[]
    {
        "Typicality (default)",
        "Distinctive",
        "Balanced MI",
        "Common patterns",
        "Significance",
        "Frequency (raw)",
        "Concentration (artifact)",
        "Metric guide"
    };

    public string[] AnalyticsScopeItems { get; } = new[]
    {
        "Current Results",
        "Zen Corpus",
        "Full Corpus (slow)"
    };
    // ----- Property change hooks (trigger auto-rerun) -----

    partial void OnZenOnlyChanged(bool value) => TriggerAutoRerunIfAllowed();
    partial void OnSelectedStatusIndexChanged(int value) => TriggerAutoRerunIfAllowed();
    partial void OnSelectedTagFilterIndexChanged(int value) => TriggerAutoRerunIfAllowed();
    partial void OnSelectedContextIndexChanged(int value) { _userChangedContextWidth = true; TriggerAutoRerunIfAllowed(); }
    partial void OnSearchOriginalChanged(bool value) => TriggerAutoRerunIfAllowed();
    partial void OnSearchTranslatedChanged(bool value) => TriggerAutoRerunIfAllowed();
    partial void OnSelectedCoocMetricIndexChanged(int value)
    {
        MetricTooltip = value switch
        {
            0 => "Typicality (logDice): stable, corpus-size independent. Best default for most queries. Range 0\u201314.",
            1 => "Distinctive (MI): finds rare but exclusive pairs. Needs \u22655 observations. Can be noisy.",
            2 => "Balanced MI (MI\u00B3): middle ground between distinctiveness and frequency.",
            3 => "Common patterns (t-score): frequency-biased. Surfaces high-volume collocations.",
            4 => "Significance (G2): statistical significance test. Used as a noise floor internally.",
            5 => "Frequency: raw co-occurrence count in KWIC windows. No corpus data needed.",
            6 => "Concentration: percentage from one text. \u226595% = single-source artifact.",
            7 => "Guide: descriptions of all metrics.",
            _ => "Select a metric"
        };
        _ = RefreshCoocUiFromCurrentStateAsync();
    }
    partial void OnSelectedAnalyticsScopeIndexChanged(int value) => _ = RefreshCoocUiFromCurrentStateAsync();

    // ----- Public wiring methods (called by MainWindow via code-behind) -----

    public void SetRootContext(string root, string originalDir, IReadOnlyList<string> translatedDirs)
    {
        _root = root;
        _originalDir = originalDir;
        _translatedDirs = translatedDirs;
    }

    public void SetFileIndex(IReadOnlyList<FileNavItem> items)
    {
        _fileIndex = items?.ToList() ?? new List<FileNavItem>();
    }

    public void SetContext(
        string root,
        string originalDir,
        IReadOnlyList<string> translatedDirs,
        Func<string, (string display, string tooltip, TranslationStatus? status)> fileMeta,
        IReadOnlyList<string>? additionalOriginalDirs = null,
        IReadOnlyList<string>? additionalTranslatedDirs = null)
    {
        _root = root;
        _additionalOriginalDirs = additionalOriginalDirs;
        _additionalTranslatedDirs = additionalTranslatedDirs;
        _originalDir = originalDir;
        _translatedDirs = translatedDirs;
        _meta = fileMeta;

        // Clear stale caches from previous corpus
        _svc.ClearBloomCache();
        _svc.ClearVerifyTextCache();
        _groups.Clear();
        ResultGroups.Clear();

        ProgressText = "Ready.";
        SummaryText = "Ready.";
        ClearCoocUi();

        _ = Task.Run(async () =>
        {
            try { await _svc.TryLoadAsync(root); }
            catch { }
        });
    }

    public void SetZenResolver(Func<string, bool>? resolver)
    {
        _isZen = resolver;
    }

    public ZenMasterCatalog? MasterCatalog => _masterCatalog;
    public void SetMasterCatalog(ZenMasterCatalog? catalog) => _masterCatalog = catalog;

    private static SearchResultGroup BuildMasterCardGroup(ZenMasterRecord master)
    {
        var dates = master.DatesSummary;
        var tooltip = string.Join(" | ",
            new[] { master.School, dates, master.Notes }
                .Where(s => !string.IsNullOrWhiteSpace(s)));
        return new SearchResultGroup
        {
            RelPath = "__master__",
            DisplayName = $"\u2638 Zen Master: {master.CanonicalName}  ({dates})",
            Tooltip = tooltip,
            HitsOriginal = 0,
            HitsTranslated = 0,
            // Wave 5: default-expand at construction so the master card shows expanded
            // on first paint. ApplyDefaultExpansionForNewGroupsOnly preserves user
            // mid-stream toggles (it skips groups already in previouslyKnown), so this
            // initialization is the sole place that needs to set the default.
            IsExpanded = true
        };
    }

    /// <summary>
    /// Populates the tag filter ComboBox with the current user's tag vocabulary and applied tags.
    /// Called by MainWindow after loading tags.
    /// </summary>
    public void SetTagFilterData(List<DocumentTag>? tags, TagVocabulary? vocab)
    {
        var items = new List<string> { "All Tags" };
        _tagsByName = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
        _tagNameById = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        _tagIdByName = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        if (tags != null && vocab != null)
        {
            foreach (var t in vocab.Tags)
            {
                if (string.IsNullOrWhiteSpace(t.Id) || string.IsNullOrWhiteSpace(t.DisplayName))
                    continue;

                _tagNameById[t.Id] = t.DisplayName;
                if (!_tagIdByName.ContainsKey(t.DisplayName))
                    _tagIdByName[t.DisplayName] = t.Id;
            }

            foreach (var tag in tags)
            {
                if (!_tagNameById.TryGetValue(tag.TagId, out var name)) continue;
                if (!_tagsByName.TryGetValue(name, out var paths))
                {
                    paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    _tagsByName[name] = paths;
                }
                paths.Add(tag.RelPath);
            }

            foreach (var name in _tagsByName.Keys.OrderBy(k => k))
                items.Add(name);
        }

        TagFilterItems = items;

        if (!TryRestorePendingTagFilter())
        {
            if (SelectedTagFilterIndex < 0 || SelectedTagFilterIndex >= items.Count)
                SelectedTagFilterIndex = 0;
        }
    }

    public SearchUiState ExportUiState()
    {
        var selectedTagName = SelectedTagFilterIndex > 0 && SelectedTagFilterIndex < TagFilterItems.Count
            ? TagFilterItems[SelectedTagFilterIndex]
            : null;

        string? selectedTagId = null;
        if (!string.IsNullOrWhiteSpace(selectedTagName) && _tagIdByName != null)
            _tagIdByName.TryGetValue(selectedTagName, out selectedTagId);

        return new SearchUiState
        {
            Query = Query,
            SearchOriginal = SearchOriginal,
            SearchTranslated = SearchTranslated,
            ZenOnly = ZenOnly,
            SelectedStatusIndex = SelectedStatusIndex,
            SelectedContextIndex = SelectedContextIndex,
            SelectedTagFilterName = selectedTagName,
            SelectedTagFilterId = selectedTagId
        };
    }

    private SearchExportSnapshot BuildExportSnapshot()
    {
        return new SearchExportSnapshot
        {
            Query = Query.Trim(),
            SearchOriginal = SearchOriginal,
            SearchTranslated = SearchTranslated,
            ZenOnly = ZenOnly,
            StatusFilter = GetStatusFilterLabel(),
            TagFilter = GetSelectedTagFilterLabel(),
            ContextLabel = GetContextLabel(),
            ExportedUtc = DateTimeOffset.UtcNow,
            Groups = ResultGroups.ToList()
        };
    }

    private string GetStatusFilterLabel() => SelectedStatusIndex switch
    {
        1 => StatusItems[1],
        2 => StatusItems[2],
        3 => StatusItems[3],
        _ => StatusItems[0]
    };

    private string GetSelectedTagFilterLabel()
    {
        if (SelectedTagFilterIndex > 0 && SelectedTagFilterIndex < TagFilterItems.Count)
            return TagFilterItems[SelectedTagFilterIndex];
        return "All Tags";
    }

    private string GetContextLabel()
    {
        var index = CoerceIndex(SelectedContextIndex, ContextItems.Length);
        return ContextItems[index];
    }

    private string BuildSuggestedExportBaseName()
    {
        if (string.IsNullOrWhiteSpace(Query))
            return "search-results";

        var cleaned = new string(Query.Trim().Where(ch => char.IsLetterOrDigit(ch) || ch == '-' || ch == '_').ToArray());
        return string.IsNullOrWhiteSpace(cleaned) ? "search-results" : "search-" + cleaned;
    }

    public Task ApplyUiStateAsync(SearchUiState? state, bool executeSearch = false)
    {
        state ??= new SearchUiState();

        BeginBatchedStateApply();
        try
        {
            Query = state.Query ?? "";
            SearchOriginal = state.SearchOriginal;
            SearchTranslated = state.SearchTranslated;
            ZenOnly = state.ZenOnly;
            SelectedStatusIndex = CoerceIndex(state.SelectedStatusIndex, StatusItems.Length);
            SelectedContextIndex = CoerceIndex(state.SelectedContextIndex, ContextItems.Length);

            if (!string.IsNullOrWhiteSpace(state.SelectedTagFilterId))
            {
                _pendingRestoredTagId = state.SelectedTagFilterId;
                TryRestorePendingTagFilter();
            }
            else
            {
                _pendingRestoredTagId = null;
                SelectedTagFilterIndex = ResolveTagFilterIndex(state.SelectedTagFilterName, null);
            }
        }
        finally
        {
            EndBatchedStateApply();
        }

        return executeSearch ? StartSearchAsync() : Task.CompletedTask;
    }

    public void Clear()
    {
        Cancel();

        _root = null;
        _originalDir = null;
        _translatedDirs = null;
        _fileIndex.Clear();
        _meta = null;
        _isZen = null;

        _groups.Clear();
        ResultGroups.Clear();
        _fullChildrenMap.Clear();
        RefreshSearchPlaceholderVisibility();

        ProgressText = "No root loaded.";
        SummaryText = "Ready.";
        ResultCountText = "";
        HasResults = false;
        IsExportEnabled = false;
        IsResultsLoadingVisible = false;
        ResultsLoadingText = "Searching...";

        _lastQuery = "";
        _lastContextWidth = 80;

        _activeMasterFilters.Clear();
        OnPropertyChanged(nameof(MasterFilterName));
        _masterFilterRelPaths = null;
        IsMasterFilterActive = false;

        ResultFilter = "";

        ZenOnly = false;
        _tagsByName = null;
        _tagNameById = null;
        _tagIdByName = null;
        _pendingRestoredTagId = null;
        TagFilterItems = new List<string> { "All Tags" };
        SelectedTagFilterIndex = 0;
        ClearCoocUi();
    }

    /// <summary>
    /// Called by code-behind when Shift+Click is detected on Build Index button.
    /// </summary>
    public void SetForceRebuild() => _forceRebuildNextClick = true;

    /// <summary>
    /// Called by code-behind for TreeView double-click to fire NavigationRequested.
    /// </summary>
    public void HandleResultDoubleTap(object? selectedItem)
    {
        if (selectedItem is SearchResultGroup g && !string.IsNullOrWhiteSpace(g.RelPath))
        {
            if (g.RelPath == "__master__" && _matchedMaster != null)
            {
                OpenMasterRequested?.Invoke(this, _matchedMaster.CanonicalName);
                return;
            }
            NavigationRequested?.Invoke(this, new NavigationRequest { RelPath = g.RelPath });
        }
        else if (selectedItem is SearchResultChild c && !string.IsNullOrWhiteSpace(c.RelPath))
        {
            string anchorSignal = string.Concat(c.Hit.Left ?? "", c.Hit.Match ?? "", c.Hit.Right ?? "");
            NavigationRequested?.Invoke(this, new NavigationRequest
            {
                RelPath = c.RelPath,
                Side = c.Side,
                MatchText = c.Hit.Match,
                LeftContext = c.Hit.Left,
                RightContext = c.Hit.Right,
                AnchorStartHint = c.Hit.Index,
                AnchorTextSignal = string.IsNullOrWhiteSpace(anchorSignal) ? null : anchorSignal,
            });
        }
    }

    // ----- Commands -----

    /// <summary>
    /// 5B: Expands a capped group to show all children when the user clicks "Show N more…".
    /// </summary>
    [RelayCommand]
    private void ShowMore(SearchResultShowMoreItem item)
    {
        if (item == null) return;
        var key = item.GroupRelPath;
        if (!_fullChildrenMap.TryGetValue(key, out var full)) return;

        // Find the group in ResultGroups and replace its children with the full list.
        foreach (var g in ResultGroups)
        {
            if (string.Equals(g.RelPath, key, StringComparison.OrdinalIgnoreCase))
            {
                g.Children = new List<SearchResultChild>(full);
                break;
            }
        }
    }

    [RelayCommand]
    private async Task SearchAsync()
    {
        await StartSearchAsync();
    }

    [RelayCommand]
    private void CancelSearch()
    {
        Cancel();
    }

    [RelayCommand]
    private async Task BuildIndexAsync()
    {
        if (_root == null || _originalDir == null || _translatedDirs == null)
        {
            StatusChanged?.Invoke(this, "Search tab has no root context yet.");
            return;
        }

        bool force = _forceRebuildNextClick;
        _forceRebuildNextClick = false;

        Cancel();
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        try
        {
            IsBuildingIndex = true;
            IsCancelEnabled = true;

            ProgressText = force ? "Index rebuild..." : "Index update...";
            SummaryText = force ? "Rebuilding search index" : "Updating search index";

            var prog = new Progress<(int done, int total, string phase)>(p =>
            {
                int percent = p.total <= 0 ? 0 : (int)Math.Round((double)p.done * 100 / p.total);
                var msg = $"Index {Math.Clamp(percent, 0, 100)}% \u2014 {p.phase}";
                ProgressText = msg;
                StatusChanged?.Invoke(this, msg);
            });

            await _svc.BuildOrUpdateAsync(_root, _originalDir, _translatedDirs, forceRebuild: force,
                additionalOriginalDirs: _additionalOriginalDirs,
                additionalTranslatedDirs: _additionalTranslatedDirs,
                progress: prog, ct: ct);

            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                ProgressText = force ? "Index rebuilt." : "Index updated.";
                SummaryText = "Index ready. Search will be fast.";
                StatusChanged?.Invoke(this, force ? "Search index rebuilt." : "Search index updated.");
            });
        }
        catch (OperationCanceledException)
        {
            ProgressText = "Canceled.";
            SummaryText = "Canceled.";
        }
        catch (Exception ex)
        {
            ProgressText = "Index build failed: " + ex.Message;
            SummaryText = "Index build failed.";
            StatusChanged?.Invoke(this, "Index build failed: " + ex.Message);
        }
        finally
        {
            await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() =>
            {
                IsBuildingIndex = false;
                IsCancelEnabled = false;
                BuildIndexCommand.NotifyCanExecuteChanged();
            });
        }
    }

    /// <summary>
    /// PR A (load-all-snippets): recomputes <see cref="HasSkippedVerifyRows"/> from
    /// the current <see cref="ResultGroups"/>. Cheap — short-circuits on first match.
    /// Call sites: after the streaming end-of-stream rebuild, after a successful
    /// LoadAllSnippetsAsync, and after any operation that mutates Children in-place
    /// (e.g. <see cref="QueueDeferredEnrichmentAsync"/>).
    /// </summary>
    private void RefreshHasSkippedVerifyRows()
    {
        bool any = false;
        foreach (var g in ResultGroups)
        {
            if (g.Children == null) continue;
            for (int i = 0; i < g.Children.Count; i++)
            {
                if (g.Children[i].IsSkippedVerify)
                {
                    any = true;
                    break;
                }
            }
            if (any) break;
        }
        HasSkippedVerifyRows = any;
    }

    private bool CanLoadAllSnippets()
        => HasSkippedVerifyRows && !IsLoadingAllSnippets && !IsSearching;

    partial void OnHasSkippedVerifyRowsChanged(bool value)
        => LoadAllSnippetsCommand.NotifyCanExecuteChanged();

    partial void OnIsLoadingAllSnippetsChanged(bool value)
        => LoadAllSnippetsCommand.NotifyCanExecuteChanged();

    partial void OnIsSearchingChanged(bool value)
        => LoadAllSnippetsCommand.NotifyCanExecuteChanged();

    /// <summary>
    /// PR A (load-all-snippets): promotes every <see cref="SearchResultChild.IsSkippedVerify"/>=true
    /// row to a real verified-with-snippet row. Snapshot the affected groups, delegate the
    /// disk-bound work to <see cref="ISearchIndexService.LoadSnippetsForAsync"/>, then apply
    /// the returned children dictionary on the UI thread — preserving group identity (so the
    /// user's IsExpanded toggle survives, per the PR4 invariant from the prior sprint).
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanLoadAllSnippets))]
    private async Task LoadAllSnippetsAsync()
    {
        if (_root == null || _originalDir == null || _translatedDirs == null)
            return;

        string root = _root;
        string originalDir = _originalDir;
        string translatedDir = _translatedDirs.Count > 0 ? _translatedDirs[0] : "";

        // Snapshot a CancellationToken — share the existing _cts if a search is somehow
        // still in flight (CanExecute prevents this, but be defensive). Otherwise create
        // a fresh source so the operation can be canceled by Cancel().
        var cts = new CancellationTokenSource();
        var ct = cts.Token;

        // Snapshot the affected groups now; the parallel verify runs off the UI thread.
        // We snapshot identities (the SearchResultGroup instances themselves) so the
        // final apply step can update them in place without identity loss.
        var skipGroups = ResultGroups
            .Where(g => g.Children != null && g.Children.Any(c => c.IsSkippedVerify))
            .ToList();
        if (skipGroups.Count == 0)
            return;

        IsLoadingAllSnippets = true;
        IsSearchProgressVisible = true;
        IsSearchProgressIndeterminate = false;
        SearchProgressPercent = 0;
        ProgressText = $"Loading snippets for {skipGroups.Count:n0} files...";

        try
        {
            var manifest = await _svc.TryLoadAsync(root);
            if (manifest == null)
            {
                ProgressText = "No index.";
                return;
            }

            string query = string.IsNullOrWhiteSpace(_lastQuery) ? Query : _lastQuery;
            int contextWidth = _lastContextWidth > 0 ? _lastContextWidth : GetContextWidth();

            var prog = new Progress<SearchIndexService.SearchProgress>(p =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    int percent = p.TotalDocsToVerify <= 0 ? 0 : (int)Math.Round((double)p.VerifiedDocs * 100 / p.TotalDocsToVerify);
                    SearchProgressPercent = Math.Clamp(percent, 0, 100);
                    IsSearchProgressIndeterminate = p.TotalDocsToVerify <= 0;
                    ProgressText = p.TotalDocsToVerify > 0
                        ? $"{p.Phase} {p.VerifiedDocs:n0}/{p.TotalDocsToVerify:n0} docs - {p.Groups:n0} files - {p.TotalHits:n0} hits"
                        : $"{p.Phase}";
                });
            });

            var promoted = await _svc.LoadSnippetsForAsync(
                root,
                originalDir,
                translatedDir,
                manifest,
                skipGroups,
                query,
                contextWidth,
                progress: prog,
                additionalOriginalDirs: _additionalOriginalDirs,
                additionalTranslatedDirs: _additionalTranslatedDirs,
                ct: ct);

            // Apply on the UI thread, preserving SearchResultGroup identity so IsExpanded
            // survives. Each promoted relPath maps to a fresh children list — assign in
            // place; ApplyChildrenCap re-applies the 5B "Show N more" cap.
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (promoted == null || promoted.Count == 0)
                    return;

                foreach (var g in ResultGroups)
                {
                    if (g.RelPath == null) continue;
                    if (!promoted.TryGetValue(g.RelPath, out var fresh) || fresh == null)
                        continue;

                    // Replace via the existing setter (raises PropertyChanged so the
                    // TreeView re-binds the children template).
                    g.Children = new List<SearchResultChild>(fresh);
                    g.HitsOriginal = fresh.Count(c => c.Side == SearchSide.Original);
                    g.HitsTranslated = fresh.Count(c => c.Side == SearchSide.Translated);

                    // Also update the backing _groups list (same instance is usually
                    // shared, but if a master/title-only group was promoted somehow it
                    // may not be — be defensive). Pull through ApplyChildrenCap to keep
                    // the 5B cap-and-show-more affordance correct.
                    ApplyChildrenCap(g);
                }

                RefreshHasSkippedVerifyRows();
                ProgressText = $"Loaded snippets for {promoted.Count:n0} files.";
                StatusChanged?.Invoke(this, $"Snippets loaded for {promoted.Count:n0} files.");
            });
        }
        catch (OperationCanceledException)
        {
            ProgressText = "Load snippets canceled.";
        }
        catch (Exception ex)
        {
            ProgressText = "Load snippets failed: " + ex.Message;
            StatusChanged?.Invoke(this, "Load snippets failed: " + ex.Message);
        }
        finally
        {
            IsLoadingAllSnippets = false;
            IsSearchProgressVisible = false;
            IsSearchProgressIndeterminate = false;
            try { cts.Dispose(); } catch { }
        }
    }

    /// <summary>
    /// Delegate that opens a save-file picker and returns the chosen path, or null if cancelled.
    /// Wired by code-behind to avoid ViewModel depending on Window/StorageProvider.
    /// </summary>
    public Func<SearchExportFormat, string?, Task<string?>>? PickExportFileAsync { get; set; }
    public Func<Task<SearchExportFormat?>>? PickExportFormatAsync { get; set; }

    [RelayCommand]
    private async Task ExportAnalyticsTsvAsync()
    {
        if (CoocChars.Count == 0 && CoocNgrams.Count == 0)
        {
            StatusChanged?.Invoke(this, "No analytics data to export.");
            return;
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Type\tKey\tFrequency\tRange\tAssociation\tDominance");

        foreach (var row in CoocChars)
            sb.AppendLine($"char\t{row.Key}\t{row.Freq}\t{row.Range}\t{row.Assoc:0.###}\t{row.Dominance:0.##%}");

        foreach (var row in CoocNgrams)
            sb.AppendLine($"ngram\t{row.Key}\t{row.Freq}\t{row.Range}\t{row.Assoc:0.###}\t{row.Dominance:0.##%}");

        var tsv = sb.ToString();

        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            var clipboard = desktop.MainWindow?.Clipboard;
            if (clipboard != null)
                await clipboard.SetTextAsync(tsv);
        }

        StatusChanged?.Invoke(this, "Analytics TSV copied to clipboard.");
    }

    [RelayCommand]
    private async Task SaveAnalyticsTsvAsync()
    {
        if (CoocChars.Count == 0 && CoocNgrams.Count == 0)
        {
            StatusChanged?.Invoke(this, "No analytics data to export.");
            return;
        }

        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Type\tKey\tFrequency\tRange\tAssociation\tDominance\tBar");

        foreach (var row in CoocChars)
            sb.AppendLine($"char\t{row.Key}\t{row.Freq}\t{row.Range}\t{row.Assoc:0.###}\t{row.Dominance:0.##%}\t{row.Bar}");

        foreach (var row in CoocNgrams)
            sb.AppendLine($"ngram\t{row.Key}\t{row.Freq}\t{row.Range}\t{row.Assoc:0.###}\t{row.Dominance:0.##%}\t{row.Bar}");

        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            var window = desktop.MainWindow;
            if (window == null) return;

            var storageProvider = window.StorageProvider;
            var file = await storageProvider.SaveFilePickerAsync(new Avalonia.Platform.Storage.FilePickerSaveOptions
            {
                Title = "Save Analytics as TSV",
                DefaultExtension = "tsv",
                FileTypeChoices = new[]
                {
                    new Avalonia.Platform.Storage.FilePickerFileType("Tab-Separated Values") { Patterns = new[] { "*.tsv" } },
                    new Avalonia.Platform.Storage.FilePickerFileType("All Files") { Patterns = new[] { "*" } }
                },
                SuggestedFileName = "analytics-cooccurrence.tsv"
            });

            if (file != null)
            {
                await using var stream = await file.OpenWriteAsync();
                await using var writer = new System.IO.StreamWriter(stream, System.Text.Encoding.UTF8);
                await writer.WriteAsync(sb.ToString());
                StatusChanged?.Invoke(this, "Analytics TSV saved.");
            }
        }
    }

    [RelayCommand]
    private async Task ExportAsync()
        => await ExportCoreAsync();

    [RelayCommand]
    private async Task ExportTsvAsync()
        => await ExportCoreAsync(SearchExportFormat.Tsv, promptForFormat: false);

    private async Task ExportCoreAsync(SearchExportFormat? forcedFormat = null, bool promptForFormat = true)
    {
        try
        {
            var snapshot = BuildExportSnapshot();
            if (snapshot.Groups.Count == 0)
            {
                StatusChanged?.Invoke(this, "No results to export.");
                return;
            }

            var format = forcedFormat ?? SearchExportFormat.Tsv;
            if (promptForFormat && PickExportFormatAsync != null)
            {
                var pickedFormat = await PickExportFormatAsync();
                if (pickedFormat == null)
                {
                    StatusChanged?.Invoke(this, "Export cancelled.");
                    return;
                }

                format = pickedFormat.Value;
            }

            if (PickExportFileAsync == null)
            {
                StatusChanged?.Invoke(this, "Save file picker not available.");
                return;
            }

            var filePath = await PickExportFileAsync(format, BuildSuggestedExportBaseName());
            if (string.IsNullOrWhiteSpace(filePath))
                return;

            await _exportSvc.ExportAsync(filePath, snapshot, format);
            StatusChanged?.Invoke(this, $"Exported search results as {format}.");
        }
        catch (Exception ex)
        {
            StatusChanged?.Invoke(this, "Export failed: " + ex.Message);
        }
    }

    // ----- Filters -----

    private int GetContextWidth()
    {
        var index = CoerceIndex(SelectedContextIndex, ContextWidths.Length);
        return ContextWidths[index];
    }

    private TranslationStatus? GetStatusFilter()
    {
        return SelectedStatusIndex switch
        {
            1 => TranslationStatus.Red,
            2 => TranslationStatus.Yellow,
            3 => TranslationStatus.Green,
            _ => null
        };
    }

    private Func<string, bool>? BuildRelPathFilter(bool zenOnly)
    {
        Func<string, bool>? zenFilter = null;
        if (zenOnly && _isZen != null)
        {
            var isZen = _isZen;
            zenFilter = rel =>
            {
                if (string.IsNullOrWhiteSpace(rel)) return false;
                rel = rel.Replace('\\', '/').TrimStart('/');
                return isZen(rel);
            };
        }

        HashSet<string>? taggedPaths = null;
        if (SelectedTagFilterIndex > 0 && _tagsByName != null)
        {
            var tagName = _tagFilterItems[SelectedTagFilterIndex];
            _tagsByName.TryGetValue(tagName, out taggedPaths);
        }

        if (zenFilter == null && taggedPaths == null) return null;

        return rel =>
        {
            if (zenFilter != null && !zenFilter(rel)) return false;
            if (taggedPaths != null && !taggedPaths.Contains(rel)) return false;
            return true;
        };
    }

    // ----- Co-occurrence helpers -----

    private bool IsGuideSelected() => SelectedCoocMetricIndex == 7;

    private CoocMetric GetSelectedMetric()
    {
        return SelectedCoocMetricIndex switch
        {
            0 => CoocMetric.LogDice,
            1 => CoocMetric.MI,
            2 => CoocMetric.MI3,
            3 => CoocMetric.TScore,
            4 => CoocMetric.LogLikelihood,
            5 => CoocMetric.Frequency,
            6 => CoocMetric.Dominance,
            _ => CoocMetric.LogDice
        };
    }

    private string GetMetricLabel()
    {
        return SelectedCoocMetricIndex switch
        {
            0 => "Typicality",
            1 => "Distinctive",
            2 => "Balanced MI",
            3 => "Common patterns",
            4 => "Significance",
            5 => "Frequency",
            6 => "Concentration",
            _ => "Score"
        };
    }

    private async Task TriggerAutoRerunAsync()
    {
        if (string.IsNullOrWhiteSpace(_lastQuery) || _root == null || _meta == null)
            return;

        try { _autoRerunCts?.Cancel(); } catch { }
        try { _autoRerunCts?.Dispose(); } catch { }
        _autoRerunCts = new CancellationTokenSource();
        var token = _autoRerunCts.Token;

        try
        {
            await Task.Delay(120, token);
            await StartSearchAsync();
        }
        catch (OperationCanceledException)
        {
            // newer UI change won
        }
    }

    private void Cancel()
    {
        try { _autoRerunCts?.Cancel(); } catch { }
        try { _autoRerunCts?.Dispose(); } catch { }
        _autoRerunCts = null;

        try { _cts?.Cancel(); } catch { }
        try { _cts?.Dispose(); } catch { }
        _cts = null;

        try { _streamFlushCoalescer?.Stop(); } catch { }
        _streamFlushCoalescer = null;

        IsCancelEnabled = false;
    }

    private void ClearCoocUi()
    {
        CoocChars.Clear();
        CoocNgrams.Clear();
        CoocCharVisuals.Clear();
        CoocNgramVisuals.Clear();
        CharChartSeries = Array.Empty<ISeries>();
        NgramChartSeries = Array.Empty<ISeries>();
        CharChartYAxes = new[] { new Axis() };
        NgramChartYAxes = new[] { new Axis() };
        ScatterSeries = Array.Empty<ISeries>();
        ScatterXAxes = new[] { new Axis() };
        ScatterYAxes = new[] { new Axis() };
        CoocSummaryText = "No data yet.";
        ZipfText = "";
        LeftTitle = "Top characters";
        RightTitle = "Top bigrams / trigrams";
        IsAnalyticsProgressVisible = false;
        AnalyticsProgressPercent = 0;
    }

    private async Task RefreshCoocUiFromCurrentStateAsync()
    {
        if (_groups.Count == 0)
        {
            ClearCoocUi();
            IsMetricViewVisible = true;
            IsMetricGuideVisible = false;
            return;
        }

        if (IsGuideSelected())
        {
            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                IsMetricViewVisible = false;
                IsMetricGuideVisible = true;
                MetricGuideText = GetMetricGuideText();
                CoocSummaryText = $"Guide (query='{_lastQuery}', context={_lastContextWidth} chars)";
                ZipfText = "";
                IsAnalyticsBusy = false;
                IsAnalyticsProgressVisible = false;
            });
            return;
        }

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            IsMetricViewVisible = true;
            IsMetricGuideVisible = false;
            IsAnalyticsBusy = true;
            IsAnalyticsProgressVisible = SelectedAnalyticsScopeIndex >= 1;
            AnalyticsProgressPercent = 0;
            var scopeLabel = SelectedAnalyticsScopeIndex == 1 ? "Zen corpus scan" : "Corpus scan";
            CoocSummaryText = SelectedAnalyticsScopeIndex >= 1 ? $"{scopeLabel} 0%" : "Computing insights...";
        });

        int myVer = Interlocked.Increment(ref _metricComputeVersion);
        var metric = GetSelectedMetric();
        var snapshotGroups = _groups.ToList();
        string q = _lastQuery;
        int cw = _lastContextWidth;

        // Capture corpus frequency data for association metrics (may be null if index not built)
        IReadOnlyDictionary<string, int>? corpusCharFreqs = null;
        IReadOnlyDictionary<string, int>? corpusBigramFreqs = null;
        long corpusTotalChars = 0;
        if (_svc is SearchIndexService concrete && concrete.HasCorpusFrequencies)
        {
            corpusCharFreqs = concrete.CorpusCharFreqs;
            corpusBigramFreqs = concrete.CorpusBigramFreqs;
            corpusTotalChars = concrete.CorpusTotalChars;
        }
        var statusFilter = GetStatusFilter();
        // Scope 0 = current results, 1 = zen corpus, 2 = full corpus
        var isCorpusScan = SelectedAnalyticsScopeIndex >= 1;
        var forceZen = SelectedAnalyticsScopeIndex == 1;
        var relFilter = forceZen ? BuildRelPathFilter(zenOnly: true) : BuildRelPathFilter(ZenOnly);

        SearchIndexService.CooccurrencePanelResult result;
        if (isCorpusScan &&
            !string.IsNullOrWhiteSpace(_originalDir) &&
            _translatedDirs is { Count: > 0 } &&
            _fileIndex.Count > 0)
        {
            var corpusProgress = new Progress<(int done, int total)>(p =>
            {
                if (myVer != Volatile.Read(ref _metricComputeVersion))
                    return;

                Dispatcher.UIThread.Post(() =>
                {
                    if (myVer != Volatile.Read(ref _metricComputeVersion))
                        return;

                    var percent = p.total <= 0 ? 0d : Math.Clamp((double)p.done * 100d / p.total, 0d, 100d);
                    AnalyticsProgressPercent = percent;
                    var label = forceZen ? "Zen corpus scan" : "Corpus scan";
                    CoocSummaryText = $"{label} {p.done:n0}/{p.total:n0} files ({percent:0}%)";
                });
            });

            result = await Task.Run(() =>
                SearchIndexService.ComputeCorpusCooccurrences(
                    _originalDir!,
                    _translatedDirs[0],
                    _fileIndex,
                    q,
                    SearchOriginal,
                    SearchTranslated,
                    cw,
                    metric,
                    topK: 20,
                    relPathFilter: relFilter,
                    statusFilter: statusFilter,
                    progress: corpusProgress,
                    corpusCharFreqs: corpusCharFreqs,
                    corpusBigramFreqs: corpusBigramFreqs,
                    corpusTotalChars: corpusTotalChars,
                    ct: CancellationToken.None));
        }
        else
        {
            result = await Task.Run(() =>
                SearchIndexService.ComputeCooccurrences(snapshotGroups, q, cw, metric, corpusCharFreqs, corpusBigramFreqs, corpusTotalChars, topK: 20));
        }

        if (myVer != Volatile.Read(ref _metricComputeVersion))
            return;

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            ApplyCooccurrenceResult(result);
            IsAnalyticsBusy = false;
            IsAnalyticsProgressVisible = false;
            AnalyticsProgressPercent = 0;
        });
    }

    private void ApplyCooccurrenceResult(SearchIndexService.CooccurrencePanelResult result)
    {
        CoocSummaryText = string.IsNullOrWhiteSpace(result.ExtraLine)
            ? result.Summary
            : result.Summary + " | " + result.ExtraLine.Replace("\n", " | ");
        LeftTitle = result.LeftTitle;
        RightTitle = result.RightTitle;

        CoocChars.Clear();
        foreach (var row in result.Left)
            CoocChars.Add(row);

        CoocNgrams.Clear();
        foreach (var row in result.Right)
            CoocNgrams.Add(row);

        // Bubble visuals removed — charts are the primary view now

        // ZipfText removed from UI — no longer displayed

        var currentMetric = GetSelectedMetric();
        var (cs, cy, ch) = BuildBarChartFromCoocRows(result.Left, currentMetric);
        CharChartSeries = cs;
        CharChartYAxes = cy;
        CharChartHeight = ch;

        var (ns, ny, nh) = BuildBarChartFromCoocRows(result.Right, currentMetric);
        NgramChartSeries = ns;
        NgramChartYAxes = ny;
        NgramChartHeight = nh;

        // Change 4b: warn if all Concentration results come from a single text
        if (currentMetric == CoocMetric.Dominance)
        {
            var allRows = result.Left.Concat(result.Right).ToList();
            if (allRows.Count > 0 && allRows.All(r => r.Dominance >= 0.95))
            {
                CoocSummaryText += " | All results from one text \u2014 try Zen Corpus or Full Corpus scope.";
            }
        }

        BuildScatterPlot(result.Left, result.Right);
    }

    private static readonly SKColor DarkBarFill = new(69, 123, 157);
    private static readonly SKColor LightBarFill = new(33, 76, 120);
    private static readonly SKColor DarkLabelColor = new(200, 200, 200);
    private static readonly SKColor LightLabelColor = new(50, 50, 50);

    // CJK-capable font for chart labels (SkiaSharp default doesn't render Chinese)
    private static readonly SKTypeface CjkTypeface =
        SKFontManager.Default.MatchCharacter('\u6c49') ?? SKTypeface.Default;

    private (ISeries[] series, Axis[] yAxes, double height) BuildBarChartFromCoocRows(IReadOnlyList<CoocRow> rows, CoocMetric metric, int maxItems = 20)
    {
        if (rows == null || rows.Count == 0)
            return (Array.Empty<ISeries>(), new[] { new Axis() }, 200);

        var isDark = Avalonia.Application.Current?.ActualThemeVariant ==
                     Avalonia.Styling.ThemeVariant.Dark;
        var barColor = isDark ? DarkBarFill : LightBarFill;
        var labelColor = isDark ? DarkLabelColor : LightLabelColor;

        var top = rows.Take(maxItems).Reverse().ToArray();
        var values = top.Select(r => r.Assoc).ToArray();
        var labels = top.Select(r => r.Key).ToArray();
        var metricName = GetMetricLabel();
        var isConcentration = metric == CoocMetric.Dominance;

        var height = Math.Max(200, labels.Length * 36 + 40);

        var rowSeries = new RowSeries<double>
        {
            Values = values,
            Name = metricName,
            Fill = new SolidColorPaint(barColor),
            MaxBarWidth = 20,
            Padding = 2,
            DataLabelsPaint = new SolidColorPaint(labelColor) { SKTypeface = CjkTypeface },
            DataLabelsSize = 11,
            DataLabelsPosition = LiveChartsCore.Measure.DataLabelsPosition.End,
            DataLabelsFormatter = pt => isConcentration
                ? $"{pt.Coordinate.PrimaryValue * 100:0}%"
                : $"{pt.Coordinate.PrimaryValue:0.##}",
        };

        rowSeries.ChartPointPointerDown += (sender, point) =>
        {
            if (point == null) return;
            var idx = (int)point.Index;
            if (idx >= 0 && idx < labels.Length)
                Dispatcher.UIThread.Post(() => FilterByCooccurrent(labels[idx]));
        };

        var series = new ISeries[] { rowSeries };

        var yAxes = new Axis[]
        {
            new Axis
            {
                Labels = labels,
                TextSize = 14,
                MinStep = 1,
                ForceStepToMin = true,
                LabelsPaint = new SolidColorPaint(labelColor) { SKTypeface = CjkTypeface },
            }
        };

        return (series, yAxes, height);
    }

    /// <summary>
    /// Rebuilds ResultGroups from _groups applying all active filters in order:
    /// master filter → cooc filter → text filter (ResultFilter).
    /// This is the single source of truth for ResultGroups reconstruction.
    /// </summary>
    private void ApplyResultFilter()
    {
        ResultGroups.Clear();

        // Always re-insert the master card if present
        if (_matchedMaster != null)
            ResultGroups.Add(BuildMasterCardGroup(_matchedMaster));

        // Start from all groups
        IEnumerable<SearchResultGroup> source = _groups;

        // Layer 1: master filter
        if (IsMasterFilterActive && _masterFilterRelPaths != null)
            source = source.Where(g => _masterFilterRelPaths.Contains(g.RelPath));

        // Layer 2: cooc filter
        if (IsCoocFilterActive && !string.IsNullOrEmpty(CoocFilterTerm))
        {
            var term = CoocFilterTerm!;
            source = source.Where(g => g.Children.Any(c =>
                c.Hit != null && (
                (!string.IsNullOrEmpty(c.Hit.Left) && c.Hit.Left.Contains(term, StringComparison.Ordinal)) ||
                (!string.IsNullOrEmpty(c.Hit.Match) && c.Hit.Match.Contains(term, StringComparison.Ordinal)) ||
                (!string.IsNullOrEmpty(c.Hit.Right) && c.Hit.Right.Contains(term, StringComparison.Ordinal)))));
        }

        // Layer 3: text filter
        if (!string.IsNullOrEmpty(ResultFilter))
        {
            var tf = ResultFilter;
            source = source.Where(g =>
                g.DisplayName.Contains(tf, StringComparison.OrdinalIgnoreCase) ||
                (g.Tooltip != null && g.Tooltip.Contains(tf, StringComparison.OrdinalIgnoreCase)) ||
                g.Children.Any(c =>
                    c.PrimarySnippetText.Contains(tf, StringComparison.OrdinalIgnoreCase) ||
                    c.SecondarySnippetText.Contains(tf, StringComparison.OrdinalIgnoreCase)));
        }

        foreach (var g in source)
            ResultGroups.Add(g);

        HasResults = ResultGroups.Count > 0;
    }

    [RelayCommand]
    private void FilterByCooccurrent(string? term)
    {
        if (string.IsNullOrEmpty(term) || _groups.Count == 0)
        {
            ClearCoocFilter();
            return;
        }

        CoocFilterTerm = term;
        IsCoocFilterActive = true;
        ApplyResultFilter();
        SelectedSearchSubTabIndex = 0;
    }

    [RelayCommand]
    private void ClearCoocFilter()
    {
        CoocFilterTerm = null;
        IsCoocFilterActive = false;
        ApplyResultFilter();
    }

    public void HandleMasterCardClick()
    {
        if (_matchedMaster == null || _groups.Count == 0) return;

        var masterName = _matchedMaster.CanonicalName;

        // Toggle off: if this master is already in the filter, remove it
        var existing = _activeMasterFilters.FirstOrDefault(m =>
            string.Equals(m.MasterName, masterName, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
            _activeMasterFilters.Remove(existing);
            OnPropertyChanged(nameof(MasterFilterName));
            RebuildMasterFilterRelPaths();
            IsMasterFilterActive = _activeMasterFilters.Count > 0;
            RebuildResultGroupsFromMasterFilter();
            return;
        }

        // Build rel-path set for this master using aliases against KWIC text and file index titles
        var aliases = _matchedMaster.Aliases
            .Where(a => !string.IsNullOrWhiteSpace(a) && a.Length >= 2)
            .ToList();
        if (aliases.Count == 0) return;

        var matchingPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var g in _groups)
        {
            if (g.Children.Any(c =>
                aliases.Any(a =>
                    (c.Hit.Left?.Contains(a, StringComparison.Ordinal) == true) ||
                    (c.Hit.Match?.Contains(a, StringComparison.Ordinal) == true) ||
                    (c.Hit.Right?.Contains(a, StringComparison.Ordinal) == true))))
            {
                matchingPaths.Add(g.RelPath);
            }
        }

        // Also include title/tooltip matches from the file index
        foreach (var f in _fileIndex)
        {
            if (aliases.Any(a => f.Tooltip?.Contains(a, StringComparison.Ordinal) == true))
                matchingPaths.Add(f.RelPath);
        }

        _activeMasterFilters.Add(new ActiveMasterFilter
        {
            MasterName = masterName,
            RelPaths = matchingPaths
        });

        OnPropertyChanged(nameof(MasterFilterName));
        RebuildMasterFilterRelPaths();
        IsMasterFilterActive = _activeMasterFilters.Count > 0;
        RebuildResultGroupsFromMasterFilter();
    }

    /// <summary>
    /// External entry point (e.g. from the master nav panel) to activate a named master filter.
    /// Adds the master to the intersection set; does NOT toggle-off.
    /// </summary>
    public void ApplyMasterFilter(string masterName, IReadOnlyList<string>? relPaths)
    {
        // Remove any existing entry for this master first (idempotent add)
        var existing = _activeMasterFilters.FirstOrDefault(m =>
            string.Equals(m.MasterName, masterName, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
            _activeMasterFilters.Remove(existing);

        _activeMasterFilters.Add(new ActiveMasterFilter
        {
            MasterName = masterName,
            RelPaths = relPaths != null
                ? new HashSet<string>(relPaths, StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        });

        OnPropertyChanged(nameof(MasterFilterName));
        RebuildMasterFilterRelPaths();
        IsMasterFilterActive = _activeMasterFilters.Count > 0;
        RebuildResultGroupsFromMasterFilter();
    }

    /// <summary>
    /// Recomputes _masterFilterRelPaths as the intersection of all active master filters.
    /// </summary>
    private void RebuildMasterFilterRelPaths()
    {
        if (_activeMasterFilters.Count == 0) { _masterFilterRelPaths = null; return; }
        var intersection = new HashSet<string>(_activeMasterFilters[0].RelPaths, StringComparer.OrdinalIgnoreCase);
        for (int i = 1; i < _activeMasterFilters.Count; i++)
            intersection.IntersectWith(_activeMasterFilters[i].RelPaths);
        _masterFilterRelPaths = intersection;
    }

    /// <summary>
    /// Rebuilds ResultGroups using the current _masterFilterRelPaths intersection.
    /// Delegates to ApplyResultFilter so all filter layers are honoured.
    /// </summary>
    private void RebuildResultGroupsFromMasterFilter() => ApplyResultFilter();

    [RelayCommand]
    private void RemoveMasterFilter(string masterName)
    {
        var toRemove = _activeMasterFilters.FirstOrDefault(m =>
            string.Equals(m.MasterName, masterName, StringComparison.OrdinalIgnoreCase));
        if (toRemove == null) return;
        _activeMasterFilters.Remove(toRemove);
        OnPropertyChanged(nameof(MasterFilterName));
        RebuildMasterFilterRelPaths();
        IsMasterFilterActive = _activeMasterFilters.Count > 0;
        RebuildResultGroupsFromMasterFilter();
    }

    [RelayCommand]
    private void ClearMasterFilter()
    {
        _activeMasterFilters.Clear();
        OnPropertyChanged(nameof(MasterFilterName));
        _masterFilterRelPaths = null;
        IsMasterFilterActive = false;
        ApplyResultFilter();
    }

    private sealed class LabeledPoint
    {
        public double X { get; set; }
        public double Y { get; set; }
        public string Label { get; set; } = "";
        public int Freq { get; set; }
    }

    private void BuildScatterPlot(IReadOnlyList<CoocRow> chars, IReadOnlyList<CoocRow> ngrams)
    {
        if ((chars == null || chars.Count == 0) && (ngrams == null || ngrams.Count == 0))
        {
            ScatterSeries = Array.Empty<ISeries>();
            return;
        }

        var isDark = Avalonia.Application.Current?.ActualThemeVariant ==
                     Avalonia.Styling.ThemeVariant.Dark;

        var charPoints = (chars ?? Array.Empty<CoocRow>())
            .Where(r => r.Freq > 0)
            .Select(r => new LabeledPoint { X = Math.Log2(1 + r.Freq), Y = r.Assoc, Label = r.Key, Freq = r.Freq })
            .ToArray();

        var ngramPoints = (ngrams ?? Array.Empty<CoocRow>())
            .Where(r => r.Freq > 0)
            .Select(r => new LabeledPoint { X = Math.Log2(1 + r.Freq), Y = r.Assoc, Label = r.Key, Freq = r.Freq })
            .ToArray();

        var metricLabel = GetMetricLabel();

        ScatterSeries = new ISeries[]
        {
            new ScatterSeries<LabeledPoint>
            {
                Values = charPoints,
                Name = "Characters",
                GeometrySize = 8,
                Fill = new SolidColorPaint(isDark ? new SKColor(69, 123, 157, 180) : new SKColor(33, 76, 120, 180)),
                Mapping = (pt, _) => new(pt.X, pt.Y),
                YToolTipLabelFormatter = pt => $"{((LabeledPoint)pt.Model!).Label}  freq={((LabeledPoint)pt.Model!).Freq}  {metricLabel}={pt.Coordinate.SecondaryValue:0.##}",
            },
            new ScatterSeries<LabeledPoint>
            {
                Values = ngramPoints,
                Name = "N-grams",
                GeometrySize = 8,
                Fill = new SolidColorPaint(isDark ? new SKColor(233, 196, 106, 180) : new SKColor(180, 140, 50, 180)),
                Mapping = (pt, _) => new(pt.X, pt.Y),
                YToolTipLabelFormatter = pt => $"{((LabeledPoint)pt.Model!).Label}  freq={((LabeledPoint)pt.Model!).Freq}  {metricLabel}={pt.Coordinate.SecondaryValue:0.##}",
            }
        };

        ScatterXAxes = new Axis[]
        {
            new Axis
            {
                Name = "log2(Frequency)",
                TextSize = 11,
                NamePaint = new SolidColorPaint(isDark ? new SKColor(200, 200, 200) : new SKColor(50, 50, 50)) { SKTypeface = CjkTypeface },
                LabelsPaint = new SolidColorPaint(isDark ? new SKColor(180, 180, 180) : new SKColor(70, 70, 70)) { SKTypeface = CjkTypeface },
            }
        };

        ScatterYAxes = new Axis[]
        {
            new Axis
            {
                Name = GetMetricLabel() + " Score",
                TextSize = 11,
                NamePaint = new SolidColorPaint(isDark ? new SKColor(200, 200, 200) : new SKColor(50, 50, 50)) { SKTypeface = CjkTypeface },
                LabelsPaint = new SolidColorPaint(isDark ? new SKColor(180, 180, 180) : new SKColor(70, 70, 70)) { SKTypeface = CjkTypeface },
            }
        };
    }

    private static List<AnalyticsBubbleItem> BuildAnalyticsVisuals(IReadOnlyList<CoocRow> rows)
    {
        var visuals = new List<AnalyticsBubbleItem>();
        if (rows == null || rows.Count == 0)
            return visuals;

        var maxFreq = Math.Max(1, rows.Max(r => r.Freq));
        foreach (var row in rows.Take(18))
        {
            var ratio = row.Freq <= 0 ? 0.2 : Math.Clamp((double)row.Freq / maxFreq, 0.18, 1.0);
            visuals.Add(new AnalyticsBubbleItem
            {
                Label = row.Key,
                Width = 52 + (70 * ratio),
                Height = 24 + (18 * ratio),
                FontSize = 11 + (7 * ratio),
                Tooltip = $"{row.Key} | freq {row.Freq:n0} | range {row.Range:n0} | score {row.Assoc:0.###}"
            });
        }

        return visuals;
    }

    // ----- Search -----

    private static void ApplyDefaultExpansion(IEnumerable<SearchResultGroup> groups)
    {
        bool firstFullTextSeen = false;
        foreach (var g in groups)
        {
            if (g.RelPath == "__master__" || g.RelPath == "__title_section__")
            {
                g.IsExpanded = true;
                continue;
            }
            g.IsExpanded = !firstFullTextSeen;
            firstFullTextSeen = true;
        }
    }

    /// <summary>
    /// Variant of <see cref="ApplyDefaultExpansion"/> that preserves the user's
    /// <c>IsExpanded</c> toggle on groups already present pre-rebuild. Only brand-new
    /// groups (not in <paramref name="previouslyKnown"/>) receive the default policy:
    /// master/title-section always expanded, first full-text group expanded, others collapsed.
    /// </summary>
    private static void ApplyDefaultExpansionForNewGroupsOnly(
        IEnumerable<SearchResultGroup> groups,
        IReadOnlyDictionary<string, SearchResultGroup> previouslyKnown)
    {
        // If any previously-known full-text group is already expanded, treat that as
        // "first full-text already seen" so we don't auto-expand a newly-arrived one.
        bool firstFullTextSeen = false;
        foreach (var kv in previouslyKnown)
        {
            var g = kv.Value;
            if (g.RelPath == "__master__" || g.RelPath == "__title_section__") continue;
            if (g.IsExpanded) { firstFullTextSeen = true; break; }
        }

        foreach (var g in groups)
        {
            // Master + title-section pseudo-paths: default-expand ONLY on first
            // appearance. They're typically inserted pre-stream with IsExpanded=true
            // by their factory (BuildMasterCardGroup / pre-stream insert site). If they
            // appear in previouslyKnown, fall through to the preservation branch below
            // so a user's mid-stream toggle survives the rebuild.
            if ((g.RelPath == "__master__" || g.RelPath == "__title_section__")
                && !previouslyKnown.ContainsKey(g.RelPath))
            {
                g.IsExpanded = true;
                continue;
            }

            if (previouslyKnown.ContainsKey(g.RelPath))
                continue; // preserve user toggle on existing groups (incl. master/title)

            g.IsExpanded = !firstFullTextSeen;
            firstFullTextSeen = true;
        }
    }

    private async Task StartSearchAsync()
    {
        if (_root == null || _originalDir == null || _translatedDirs == null || _meta == null)
        {
            StatusChanged?.Invoke(this, "Search tab has no root context yet.");
            return;
        }

        HasValidationError = false;
        ValidationMessage = "";

        string q = Query.Trim();
        if (q.Length == 0)
        {
            ValidationMessage = "Please enter a search query.";
            HasValidationError = true;
            StatusChanged?.Invoke(this, "Enter a search query.");
            return;
        }

        bool includeO = SearchOriginal;
        bool includeT = SearchTranslated;
        if (!includeO && !includeT)
        {
            ValidationMessage = "Select at least one of Original or Translated.";
            HasValidationError = true;
            StatusChanged?.Invoke(this, "Select Original and/or Translated.");
            return;
        }

        IsEmptyStateVisible = false;

        bool zenOnly = ZenOnly;
        if (zenOnly && _isZen == null)
        {
            StatusChanged?.Invoke(this, "Zen filter is enabled but no Zen resolver was provided.");
            zenOnly = false;
        }

        var statusFilter = GetStatusFilter();
        var relFilter = BuildRelPathFilter(zenOnly);

        Cancel();
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;

        int mySearchVer = Interlocked.Increment(ref _searchRunVersion);

        _groups.Clear();
        ResultGroups.Clear();
        _fullChildrenMap.Clear();
        IsExportEnabled = false;
        ClearCoocUi();
        IsSearching = true;
        IsSearchProgressVisible = true;
        IsSearchProgressIndeterminate = true;
        SearchProgressPercent = 0;
        IsResultsLoadingVisible = true;
        ResultsLoadingText = "Searching... no results yet";
        RefreshSearchPlaceholderVisibility();

        string root = _root;
        string originalDir = _originalDir;
        string translatedDir = _translatedDirs is { Count: > 0 } ? _translatedDirs[0] : "";
        var metaFn = _meta;

        // Auto-select context width based on query language:
        // CJK queries → 10 chars (index 1), English → 80 chars (index 5)
        bool hasCjk = q.Any(c => c >= '\u3400' && c <= '\u9FFF' || c >= '\uF900' && c <= '\uFAFF');
        int autoIndex = hasCjk ? 1 : 5; // 10 chars for CJK, 80 for English
        if (!_userChangedContextWidth)
            SelectedContextIndex = autoIndex;

        int contextWidth = GetContextWidth();

        _lastQuery = q;
        _lastContextWidth = contextWidth;

        try
        {
            IsCancelEnabled = true;
            SummaryText = $"Search: {q}";
            ProgressText = "Preparing search...";
            ResultsLoadingText = $"Searching for \"{q}\"...";

            // Insert master card at top if query matches a zen master name
            _matchedMaster = null;
            if (_masterCatalog != null && !string.IsNullOrWhiteSpace(q))
            {
                _matchedMaster = _masterCatalog.Records.FirstOrDefault(r =>
                    r.CanonicalName.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    r.Aliases.Any(a => a.Contains(q, StringComparison.OrdinalIgnoreCase)));

                if (_matchedMaster != null)
                    ResultGroups.Insert(0, BuildMasterCardGroup(_matchedMaster));
            }

            // Title matching: find texts whose title contains the query
            // Also include master's primary texts if a master was matched
            if (_fileIndex.Count > 0 && !string.IsNullOrWhiteSpace(q))
            {
                var titleMatches = _fileIndex
                    .Where(f => (!string.IsNullOrWhiteSpace(f.DisplayShort)
                                  && f.DisplayShort.Contains(q, StringComparison.OrdinalIgnoreCase))
                              || (!string.IsNullOrWhiteSpace(f.Tooltip)
                                  && f.Tooltip.Contains(q, StringComparison.OrdinalIgnoreCase)))
                    .Take(10)
                    .ToList();

                // If a master was matched, also find texts with their Chinese name in the title
                if (_matchedMaster != null)
                {
                    var matchedRelPaths = new HashSet<string>(
                        titleMatches.Select(t => t.RelPath), StringComparer.OrdinalIgnoreCase);
                    foreach (var alias in _matchedMaster.Aliases)
                    {
                        if (string.IsNullOrWhiteSpace(alias) || alias.Length < 2) continue;
                        var masterTexts = _fileIndex
                            .Where(f => !matchedRelPaths.Contains(f.RelPath)
                                && !string.IsNullOrWhiteSpace(f.Tooltip)
                                && f.Tooltip.Contains(alias, StringComparison.Ordinal))
                            .Take(5);
                        foreach (var mt in masterTexts)
                        {
                            matchedRelPaths.Add(mt.RelPath);
                            titleMatches.Add(mt);
                        }
                    }
                }

                foreach (var item in titleMatches)
                {
                    var tooltip = item.Tooltip ?? "";
                    var zhTitle = "";
                    var nlIdx = tooltip.IndexOf('\n');
                    if (nlIdx >= 0 && nlIdx < tooltip.Length - 1)
                        zhTitle = tooltip[(nlIdx + 1)..];

                    ResultGroups.Add(new SearchResultGroup
                    {
                        RelPath = item.RelPath,
                        DisplayName = $"\uD83D\uDCD6 {item.DisplayShort ?? item.FileName}",
                        Tooltip = item.Tooltip ?? item.RelPath,
                        ChineseTitle = zhTitle,
                        Status = item.Status,
                        HitsOriginal = 0,
                        HitsTranslated = 0
                    });
                }
            }

            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Render);

            await Task.Run(async () =>
            {
                var manifest = await _svc.TryLoadAsync(root);
                if (manifest == null)
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (mySearchVer != Volatile.Read(ref _searchRunVersion)) return;
                        ProgressText = "No index.";
                        IsSearchProgressVisible = false;
                        IsSearchProgressIndeterminate = false;
                        IsResultsLoadingVisible = false;
                        RefreshSearchPlaceholderVisibility();
                        StatusChanged?.Invoke(this, "No search index found. Click 'Index' first.");
                    });
                    return;
                }

                int totalHits = 0;
                int totalGroups = 0;
                var localGroups = new List<SearchResultGroup>(256);
                var pendingUiBatch = new List<SearchResultGroup>(12);
                // PR4: track default-expansion state across streaming flushes. The first
                // full-text group to arrive gets IsExpanded = true; subsequent ones stay collapsed.
                // The end-of-stream rebuild preserves whatever state each group is in (so
                // user toggles during streaming survive).
                bool firstFullTextExpandedDuringStream = false;

                var prog = new Progress<SearchIndexService.SearchProgress>(p =>
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        if (mySearchVer != Volatile.Read(ref _searchRunVersion))
                            return;

                        int percent = p.TotalDocsToVerify <= 0 ? 0 : (int)Math.Round((double)p.VerifiedDocs * 100 / p.TotalDocsToVerify);
                        IsSearchProgressVisible = true;
                        IsSearchProgressIndeterminate = p.TotalDocsToVerify <= 0;
                        SearchProgressPercent = Math.Clamp(percent, 0, 100);
                        ProgressText = p.TotalDocsToVerify > 0
                            ? $"{p.Phase} {p.VerifiedDocs:n0}/{p.TotalDocsToVerify:n0} docs - {p.Groups:n0} files - {p.TotalHits:n0} hits"
                            : $"{p.Phase} - {p.Groups:n0} files - {p.TotalHits:n0} hits";
                        ResultsLoadingText = p.TotalHits > 0
                            ? $"Searching... {p.TotalHits:n0} hits found so far"
                            : $"{p.Phase}...";
                    });
                });

                async Task FlushPendingUiBatchAsync(bool forceSummary)
                {
                    SearchResultGroup[] batch;
                    lock (pendingUiBatch)
                    {
                        if (pendingUiBatch.Count == 0 && !forceSummary) return;
                        batch = pendingUiBatch.Count > 0 ? pendingUiBatch.ToArray() : Array.Empty<SearchResultGroup>();
                        pendingUiBatch.Clear();
                    }

                    int snapshotGroups = totalGroups;
                    int snapshotHits = totalHits;

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (mySearchVer != Volatile.Read(ref _searchRunVersion))
                            return;

                        if (batch.Length > 0)
                        {
                            IsResultsLoadingVisible = false;
                            RefreshSearchPlaceholderVisibility();
                        }

                        for (int i = 0; i < batch.Length; i++)
                        {
                            ApplyChildrenCap(batch[i]); // 5B: cap before first render
                            // PR4: apply default-expansion policy at insert time so the user
                            // sees a sensible initial state and any later user toggle is
                            // preserved by the end-of-stream rebuild.
                            if (batch[i].RelPath != "__master__"
                                && batch[i].RelPath != "__title_section__")
                            {
                                batch[i].IsExpanded = !firstFullTextExpandedDuringStream;
                                firstFullTextExpandedDuringStream = true;
                            }
                            ResultGroups.Add(batch[i]);
                        }

                        RefreshSearchPlaceholderVisibility();

                        if (forceSummary || batch.Length > 0)
                            SummaryText = $"Results: {snapshotGroups:n0} files - {snapshotHits:n0} hits";

                        // PR A: any batched-in group might carry IsSkippedVerify children
                        // (2-char CJK hybrid path). Refresh the flag so the toolbar button
                        // appears as soon as the first skip-verified row streams in.
                        if (batch.Length > 0)
                            RefreshHasSkippedVerifyRows();
                    }, DispatcherPriority.Background);

                    foreach (var group in batch)
                        _ = QueueDeferredEnrichmentAsync(group, originalDir, translatedDir, q, includeO, includeT, contextWidth, mySearchVer);
                }

                // Streaming-flush coalescer: defends against UI thread thrash when groups
                // arrive faster than the dispatcher can drain (~60 ms = one frame at 16 fps).
                // The Tick fires on the UI thread (DispatcherTimer default). The loop body
                // below also flushes eagerly per-group; the timer is a backstop for cases
                // where the loop runs in tight succession without yielding control.
                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (mySearchVer != Volatile.Read(ref _searchRunVersion)) return;
                    if (ct.IsCancellationRequested) return; // Cancel() raced ahead of us
                    var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(60) };
                    timer.Tick += async (_, _) =>
                    {
                        try
                        {
                            if (mySearchVer != Volatile.Read(ref _searchRunVersion)
                                || ct.IsCancellationRequested)
                            {
                                timer.Stop();
                                return;
                            }
                            await FlushPendingUiBatchAsync(forceSummary: false);
                        }
                        catch { /* swallow — Cancel() may dispose state mid-tick */ }
                    };
                    _streamFlushCoalescer = timer;
                    timer.Start();
                });

                await foreach (var g in _svc.SearchAllAsync(
                                   root,
                                   originalDir,
                                   translatedDir,
                                   manifest,
                                   q,
                                   includeO,
                                   includeT,
                                   fileMeta: rel =>
                                   {
                                       var m = metaFn(rel);

                                       if (statusFilter.HasValue)
                                       {
                                           if (m.status.HasValue)
                                           {
                                               if (m.status.Value != statusFilter.Value)
                                                   return ("", "", m.status);
                                           }
                                           else
                                           {
                                               return ("", "", null);
                                           }
                                       }

                                       return m;
                                   },
                                   contextWidth: contextWidth,
                                   progress: prog,
                                   relPathFilter: relFilter,
                                   additionalOriginalDirs: _additionalOriginalDirs,
                                   additionalTranslatedDirs: _additionalTranslatedDirs,
                                   ct: ct))
                {
                    ct.ThrowIfCancellationRequested();

                    if (mySearchVer != Volatile.Read(ref _searchRunVersion))
                        return;

                    if (string.IsNullOrWhiteSpace(g.DisplayName))
                        continue;

                    localGroups.Add(g);
                    lock (pendingUiBatch) pendingUiBatch.Add(g);
                    totalGroups++;
                    totalHits += g.Children.Count;

                    // Per-iteration flush is needed for test infrastructure (the existing
                    // ControlledSearchIndexService tests use synchronous gates that never let
                    // the 60ms DispatcherTimer tick). In production, the per-group flush
                    // gives instant first-paint; the 60ms timer is a backstop for bursts
                    // that arrive faster than the dispatcher can drain. Wave 5 considered
                    // dropping one path but kept both after the streaming tests required it.
                    if (pendingUiBatch.Count >= 1)
                    {
                        await FlushPendingUiBatchAsync(forceSummary: false);
                        await Task.Yield();
                    }
                }

                // Stop the streaming coalescer; the end-of-stream rebuild below is the final flush.
                try
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        try { _streamFlushCoalescer?.Stop(); } catch { }
                        _streamFlushCoalescer = null;
                    });
                }
                catch { /* dispatcher may be torn down on cancellation */ }

                await FlushPendingUiBatchAsync(forceSummary: true);

                await Dispatcher.UIThread.InvokeAsync(() =>
                {
                    if (mySearchVer != Volatile.Read(ref _searchRunVersion))
                        return;

                    var currentByRelPath = ResultGroups
                        .GroupBy(g => g.RelPath, StringComparer.OrdinalIgnoreCase)
                        .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

                    var sortedGroups = localGroups
                        .Select(g => currentByRelPath.TryGetValue(g.RelPath, out var existing) ? existing : g)
                        .OrderByDescending(g => g.HitsOriginal + g.HitsTranslated)
                        .ThenBy(g => g.RelPath, StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    _groups.Clear();
                    _groups.AddRange(sortedGroups);

                    // Preserve title matches (📖 prefixed) from initial insert
                    var titleMatches = ResultGroups
                        .Where(g => g.DisplayName.StartsWith("\uD83D\uDCD6"))
                        .ToList();

                    // ---- In-place mutation: preserve identity + IsExpanded across rebuild ----
                    var desired = new List<SearchResultGroup>(sortedGroups.Count + 16);
                    if (_matchedMaster != null)
                    {
                        var existingMaster = ResultGroups.FirstOrDefault(g => g.RelPath == "__master__");
                        desired.Add(existingMaster ?? BuildMasterCardGroup(_matchedMaster));
                    }
                    foreach (var tm in titleMatches)
                        desired.Add(tm);
                    foreach (var fg in sortedGroups)
                        desired.Add(fg);

                    var desiredKeys = new HashSet<string>(
                        desired.Select(g => g.RelPath), StringComparer.OrdinalIgnoreCase);
                    for (int i = ResultGroups.Count - 1; i >= 0; i--)
                    {
                        if (!desiredKeys.Contains(ResultGroups[i].RelPath))
                            ResultGroups.RemoveAt(i);
                    }

                    for (int i = 0; i < desired.Count; i++)
                    {
                        var want = desired[i];
                        if (i >= ResultGroups.Count)
                        {
                            ResultGroups.Add(want);
                            continue;
                        }
                        if (!ReferenceEquals(ResultGroups[i], want))
                        {
                            int existingIdx = -1;
                            for (int j = i + 1; j < ResultGroups.Count; j++)
                            {
                                if (ReferenceEquals(ResultGroups[j], want))
                                {
                                    existingIdx = j;
                                    break;
                                }
                            }
                            if (existingIdx >= 0)
                                ResultGroups.Move(existingIdx, i);
                            else
                                ResultGroups.Insert(i, want);
                        }
                    }

                    ApplyDefaultExpansionForNewGroupsOnly(ResultGroups, currentByRelPath);

                    SummaryText = $"Done: {sortedGroups.Count:n0} files - {totalHits:n0} hits";
                    ResultCountText = $"{ResultGroups.Count} texts \u00b7 {ResultGroups.Sum(g => g.HitsOriginal + g.HitsTranslated)} hits";
                    HasResults = ResultGroups.Count > 0 || _matchedMaster != null;
                    ProgressText = $"Verified {sortedGroups.Count:n0} matching files";

                    if (!_firstSearchSupportShown && sortedGroups.Count > 0)
                    {
                        _firstSearchSupportShown = true;
                        _ = Task.Delay(3000).ContinueWith(_ =>
                            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                                StatusChanged?.Invoke(this, "ReadZen is free and open source. Support on ko-fi.com/readzen \u2661")),
                            System.Threading.Tasks.TaskScheduler.Default);
                    }

                    SearchProgressPercent = 100;
                    IsSearchProgressIndeterminate = false;
                    IsResultsLoadingVisible = false;
                    RefreshSearchPlaceholderVisibility();
                    IsExportEnabled = sortedGroups.Count > 0;

                    // PR A: the final rebuild is the canonical snapshot — recompute
                    // HasSkippedVerifyRows once here so the toolbar button is in sync
                    // with the actual ResultGroups state (some groups may have been
                    // dropped, others added since the streaming flushes refreshed it).
                    RefreshHasSkippedVerifyRows();

                    foreach (var group in sortedGroups)
                        _ = QueueDeferredEnrichmentAsync(group, originalDir, translatedDir, q, includeO, includeT, contextWidth, mySearchVer);
                });
            }, ct);

            if (mySearchVer == Volatile.Read(ref _searchRunVersion))
                await RefreshCoocUiFromCurrentStateAsync();

            // Track search history
            if (!string.IsNullOrWhiteSpace(q) && !_searchHistory.Contains(q))
            {
                _searchHistory.Insert(0, q);
                if (_searchHistory.Count > 20)
                    _searchHistory.RemoveAt(20);
                OnPropertyChanged(nameof(SearchHistory));
            }
        }
        catch (OperationCanceledException)
        {
            if (mySearchVer == Volatile.Read(ref _searchRunVersion))
            {
                ProgressText = "Canceled.";
                SummaryText = "Canceled.";
                IsSearchProgressVisible = false;
                IsSearchProgressIndeterminate = false;
                IsResultsLoadingVisible = false;
                RefreshSearchPlaceholderVisibility();
            }
        }
        catch (Exception ex)
        {
            if (mySearchVer == Volatile.Read(ref _searchRunVersion))
            {
                ProgressText = "Search failed: " + ex.Message;
                SummaryText = "Search failed.";
                IsSearchProgressVisible = false;
                IsSearchProgressIndeterminate = false;
                IsResultsLoadingVisible = false;
                RefreshSearchPlaceholderVisibility();
                StatusChanged?.Invoke(this, "Search failed: " + ex.Message);
            }
        }
        finally
        {
            if (mySearchVer == Volatile.Read(ref _searchRunVersion))
            {
                IsCancelEnabled = false;
                IsSearching = false;
                IsSearchProgressVisible = false;
                IsSearchProgressIndeterminate = false;
                RefreshSearchPlaceholderVisibility();
            }
        }
    }

    // ----- Helpers -----

    private static bool NeedsDeferredEnrichment(SearchResultGroup group)
    {
        if (group.Children == null || group.Children.Count == 0)
            return false;

        // Wave 5 fix: skip-verify placeholder rows (PR2 hybrid path) have IsSkippedVerify=true
        // and intentionally empty Left/Match/Right. They have no source bytes to re-read, and
        // re-running enrichment would force the file open we deliberately skipped — partially
        // negating the perf win. Treat them as terminal; the UI shows a "snippet on demand"
        // placeholder via the IsSkippedVerify XAML binding.
        bool allSkippedVerify = group.Children.All(c =>
            c is SearchResultShowMoreItem || c.IsSkippedVerify);
        if (allSkippedVerify)
            return false;

        // Ignore the ShowMore sentinel when deciding whether enrichment is needed.
        return group.Children.Any(c => c is not SearchResultShowMoreItem
            && !c.IsSkippedVerify
            && (!c.HasSecondaryDisplayText || c.PrimaryIsContextOnly || c.SecondaryIsContextOnly));
    }

    /// <summary>
    /// 5B: If <paramref name="group"/> has more than <see cref="MaxVisibleChildren"/> real children,
    /// stores the full list in <see cref="_fullChildrenMap"/> and replaces the group's Children with a
    /// capped list followed by a <see cref="SearchResultShowMoreItem"/> sentinel.
    /// Safe to call multiple times (idempotent — re-reads the full map if already stored).
    /// </summary>
    private void ApplyChildrenCap(SearchResultGroup group)
    {
        if (group == null || string.IsNullOrEmpty(group.RelPath)) return;

        // Strip any existing sentinel to get the real children.
        var real = group.Children
            .Where(c => c is not SearchResultShowMoreItem)
            .ToList();

        if (real.Count <= MaxVisibleChildren)
        {
            // Ensure no stale sentinel remains.
            if (group.Children.Any(c => c is SearchResultShowMoreItem))
                group.Children = real;
            return;
        }

        // Store the full list for later expansion.
        _fullChildrenMap[group.RelPath] = real;

        var capped = new List<SearchResultChild>(MaxVisibleChildren + 1);
        capped.AddRange(real.Take(MaxVisibleChildren));
        capped.Add(new SearchResultShowMoreItem
        {
            RelPath = group.RelPath,
            GroupRelPath = group.RelPath,
            RemainingCount = real.Count - MaxVisibleChildren
        });
        group.Children = capped;
    }

    private async Task QueueDeferredEnrichmentAsync(SearchResultGroup group, string originalDir, string translatedDir, string query, bool includeOriginal, bool includeTranslated, int contextWidth, int searchVersion)
    {
        if (!NeedsDeferredEnrichment(group) || string.IsNullOrWhiteSpace(originalDir) || string.IsNullOrWhiteSpace(translatedDir))
            return;

        await _resultEnrichmentGate.WaitAsync();
        try
        {
            var displayChildren = await Task.Run(() =>
                SearchIndexService.BuildAlignedDisplayChildrenFromIndexedUnits(
                    originalDir,
                    translatedDir,
                    group.RelPath,
                    query,
                    includeOriginal,
                    includeTranslated,
                    contextWidth));

            if (displayChildren.Count == 0 || searchVersion != Volatile.Read(ref _searchRunVersion))
                return;

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                if (searchVersion != Volatile.Read(ref _searchRunVersion))
                    return;

                SearchResultGroup target = group;
                for (int i = 0; i < ResultGroups.Count; i++)
                {
                    if (string.Equals(ResultGroups[i].RelPath, group.RelPath, StringComparison.OrdinalIgnoreCase))
                    {
                        target = ResultGroups[i];
                        break;
                    }
                }

                target.ApplyEnrichment(displayChildren);
                ApplyChildrenCap(target); // 5B: re-apply cap after enrichment replaces children

                if (!ReferenceEquals(target, group))
                {
                    group.ApplyEnrichment(displayChildren);
                    ApplyChildrenCap(group); // 5B: re-apply cap on backing group too
                }
            }, DispatcherPriority.Background);
        }
        catch
        {
            // Keep the basic fast result if enrichment fails.
        }
        finally
        {
            _resultEnrichmentGate.Release();
        }
    }

    private void RefreshSearchPlaceholderVisibility()
    {
        OnPropertyChanged(nameof(IsSearchLoadingPlaceholderVisible));
        IsEmptyStateVisible = !IsSearchLoadingPlaceholderVisible && !IsSearching && ResultGroups.Count == 0 && !HasValidationError;
    }

    private void TriggerAutoRerunIfAllowed()
    {
        if (_batchedStateDepth > 0)
            return;

        _ = TriggerAutoRerunAsync();
    }

    private void BeginBatchedStateApply()
    {
        _batchedStateDepth++;
    }

    private void EndBatchedStateApply()
    {
        if (_batchedStateDepth > 0)
            _batchedStateDepth--;
    }

    private static int CoerceIndex(int index, int itemCount)
    {
        if (itemCount <= 0)
            return 0;

        if (index < 0)
            return 0;

        return index >= itemCount ? 0 : index;
    }

    private int ResolveTagFilterIndex(string? tagFilterName, string? tagFilterId)
    {
        if (!string.IsNullOrWhiteSpace(tagFilterId) && _tagNameById != null && _tagNameById.TryGetValue(tagFilterId, out var resolvedName))
            tagFilterName = resolvedName;

        if (string.IsNullOrWhiteSpace(tagFilterName))
            return 0;

        for (int i = 1; i < TagFilterItems.Count; i++)
        {
            if (string.Equals(TagFilterItems[i], tagFilterName, StringComparison.OrdinalIgnoreCase))
                return i;
        }

        return 0;
    }

    private bool TryRestorePendingTagFilter()
    {
        if (string.IsNullOrWhiteSpace(_pendingRestoredTagId))
            return false;

        int index = ResolveTagFilterIndex(null, _pendingRestoredTagId);
        if (index <= 0)
            return false;

        SelectedTagFilterIndex = index;
        _pendingRestoredTagId = null;
        return true;
    }

    private static string GetMetricGuideText()
    {
        return
@"Metric guide

Two panels show what appears near your search term:
  Left  — single characters (names, particles, punctuation patterns)
  Right — bigrams and trigrams (phrase fragments, compounds)

Each row shows: key | freq (how often) | range (how many files) | score (selected metric)

Metrics:

Typicality (logDice)
  Stable, corpus-size independent ranking. Best default.
  Score: 14 + log2(2 * freq / (f_collocate + f_query))
  Ignores raw frequency; rewards consistent co-occurrence across the corpus.
  Range roughly 0-14; higher = more typical pairing.

Distinctive (MI)
  Finds rare but exclusive pairs — things that appear near your query
  but rarely elsewhere. Can surface unique terminology or nicknames.
  Requires at least 5 observations. Can be noisy for low-frequency items.
  Score: log2(freq * N / (f_collocate * f_query))

Balanced MI (MI3)
  Middle ground: rewards distinctiveness but dampens noise from very
  rare collocates. Score: log2(freq^3 * N / (f_collocate * f_query))
  Requires at least 5 observations.

Common patterns (t-score)
  Frequency-biased. Surfaces collocations with lots of evidence —
  formulaic phrases, stock expressions, high-volume names.
  Score: (freq - expected) / sqrt(freq)

Significance (G2)
  Statistical significance test (log-likelihood ratio). Used internally
  as a noise floor (p < 0.01 threshold). Selecting it shows the raw G2
  values so you can inspect borderline items.

Frequency (raw)
  Just the count: how many times the item appeared inside context windows.
  No corpus data needed. Useful for spotting formulaic refrains.

Concentration (artifact detector)
  Shows what fraction of occurrences come from a single text.
  100% = entirely one-text artifact. Use to spot search results
  dominated by one document. Displayed as a percentage on bars.

Rule of thumb:
  Start with Typicality (default) for reliable collocates.
  Switch to Distinctive to hunt for unique terminology.
  Use Concentration to verify results aren't one-book artifacts.";
    }
}




























