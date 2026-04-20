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

    public SearchTabViewModel(ISearchIndexService searchIndexService, ISearchExportService? searchExportService = null)
    {
        _svc = searchIndexService ?? throw new ArgumentNullException(nameof(searchIndexService));
        _exportSvc = searchExportService ?? new SearchExportService();
    }

    private string? _root;
    private string? _originalDir;
    private IReadOnlyList<string>? _translatedDirs;
    private bool _forceRebuildNextClick;

    private List<FileNavItem> _fileIndex = new();
    private Func<string, (string display, string tooltip, TranslationStatus? status)>? _meta;

    private CancellationTokenSource? _cts;
    private CancellationTokenSource? _autoRerunCts;
    private readonly List<SearchResultGroup> _groups = new();
    private int _batchedStateDepth;

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

    // Tag filter
    private List<string> _tagFilterItems = new() { "All Tags" };
    private Dictionary<string, HashSet<string>>? _tagsByName; // tagName -> set of RelPaths
    private Dictionary<string, string>? _tagNameById; // tagId -> displayName
    private Dictionary<string, string>? _tagIdByName; // displayName -> tagId
    private string? _pendingRestoredTagId;
    private static readonly int[] ContextWidths = new[] { 20, 40, 80, 160, 240, 320, 480, 640 };



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
    private bool _searchTranslated;

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
    private string _leftTitle = "Top characters";

    [ObservableProperty]
    private string _rightTitle = "Top bigrams / trigrams";

    [ObservableProperty]
    private bool _isMetricGuideVisible;

    [ObservableProperty]
    private bool _isMetricViewVisible = true;

    [ObservableProperty]
    private string? _coocFilterTerm;

    [ObservableProperty]
    private bool _isCoocFilterActive;

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
    private Axis[] _charChartYAxes = Array.Empty<Axis>();

    [ObservableProperty]
    private Axis[] _ngramChartYAxes = Array.Empty<Axis>();

    [ObservableProperty]
    private ISeries[] _scatterSeries = Array.Empty<ISeries>();

    [ObservableProperty]
    private Axis[] _scatterXAxes = Array.Empty<Axis>();

    [ObservableProperty]
    private Axis[] _scatterYAxes = Array.Empty<Axis>();

    // ----- Events -----

    public event EventHandler<string>? StatusChanged;
    public event EventHandler<NavigationRequest>? NavigationRequested;

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
        "Top co-occurrences (overview)",
        "Dispersion score (stable)",
        "Frequency (raw)",
        "Range (dispersion proxy)",
        "Dominance (top-file share)",
        "PMI (window-based)",
        "logDice (lexicography)",
        "t-score (frequency-biased)",
        "Metric guide (how to read these)"
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
    partial void OnSelectedContextIndexChanged(int value) => TriggerAutoRerunIfAllowed();
    partial void OnSearchOriginalChanged(bool value) => TriggerAutoRerunIfAllowed();
    partial void OnSearchTranslatedChanged(bool value) => TriggerAutoRerunIfAllowed();
    partial void OnSelectedCoocMetricIndexChanged(int value)
    {
        MetricTooltip = value switch
        {
            0 => "Overview: balanced ranking by frequency and dispersion across texts",
            1 => "Dispersion: stable evidence distributed across many documents",
            2 => "Raw frequency: total count in context windows",
            3 => "Range: number of distinct texts containing the term",
            4 => "Dominance: concentration in a single text (>80% = artifact)",
            5 => "PMI: association strength \u2014 rewards rare but tight collocations",
            6 => "logDice: lexicography metric \u2014 dictionary-like collocation candidates",
            7 => "t-score: statistically significant, frequency-biased collocations",
            8 => "Guide: explanations of all metrics with examples",
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
        Func<string, (string display, string tooltip, TranslationStatus? status)> fileMeta)
    {
        _root = root;
        _originalDir = originalDir;
        _translatedDirs = translatedDirs;
        _meta = fileMeta;

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
        RefreshSearchPlaceholderVisibility();

        ProgressText = "No root loaded.";
        SummaryText = "Ready.";
        IsExportEnabled = false;
        IsResultsLoadingVisible = false;
        ResultsLoadingText = "Searching...";

        _lastQuery = "";
        _lastContextWidth = 80;

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
            IsCancelEnabled = true;

            ProgressText = force ? "Index rebuild..." : "Index update...";
            SummaryText = force ? "Rebuilding search index" : "Updating search index";

            var prog = new Progress<(int done, int total, string phase)>(p =>
            {
                int percent = p.total <= 0 ? 0 : (int)Math.Round((double)p.done * 100 / p.total);
                ProgressText = $"Index {Math.Clamp(percent, 0, 100)}% ? {p.phase}";
            });

            await _svc.BuildOrUpdateAsync(_root, _originalDir, _translatedDirs, forceRebuild: force, progress: prog, ct: ct);

            ProgressText = force ? "Index rebuilt." : "Index updated.";
            SummaryText = "Index ready. Search will be fast.";
            StatusChanged?.Invoke(this, force ? "Search index rebuilt." : "Search index updated.");
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
            IsCancelEnabled = false;
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

    private bool IsGuideSelected() => SelectedCoocMetricIndex == 8;

    private CoocMetric GetSelectedMetric()
    {
        return SelectedCoocMetricIndex switch
        {
            0 => CoocMetric.TopCooccurrences,
            1 => CoocMetric.DispersionScore,
            2 => CoocMetric.Frequency,
            3 => CoocMetric.Range,
            4 => CoocMetric.Dominance,
            5 => CoocMetric.PMI,
            6 => CoocMetric.LogDice,
            7 => CoocMetric.TScore,
            _ => CoocMetric.TopCooccurrences
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
        CharChartYAxes = Array.Empty<Axis>();
        NgramChartYAxes = Array.Empty<Axis>();
        ScatterSeries = Array.Empty<ISeries>();
        ScatterXAxes = Array.Empty<Axis>();
        ScatterYAxes = Array.Empty<Axis>();
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
                    topK: 30,
                    relPathFilter: relFilter,
                    statusFilter: statusFilter,
                    progress: corpusProgress,
                    ct: CancellationToken.None));
        }
        else
        {
            result = await Task.Run(() =>
                SearchIndexService.ComputeCooccurrences(snapshotGroups, q, cw, metric, topK: 30));
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
        CoocSummaryText = result.Summary;
        LeftTitle = result.LeftTitle;
        RightTitle = result.RightTitle;

        CoocChars.Clear();
        foreach (var row in result.Left)
            CoocChars.Add(row);

        CoocNgrams.Clear();
        foreach (var row in result.Right)
            CoocNgrams.Add(row);

        CoocCharVisuals.Clear();
        foreach (var item in BuildAnalyticsVisuals(result.Left))
            CoocCharVisuals.Add(item);

        CoocNgramVisuals.Clear();
        foreach (var item in BuildAnalyticsVisuals(result.Right))
            CoocNgramVisuals.Add(item);

        ZipfText = result.ExtraLine ?? "";

        var (cs, cy) = BuildBarChartFromCoocRows(result.Left);
        CharChartSeries = cs;
        CharChartYAxes = cy;

        var (ns, ny) = BuildBarChartFromCoocRows(result.Right);
        NgramChartSeries = ns;
        NgramChartYAxes = ny;

        BuildScatterPlot(result.Left, result.Right);
    }

    private static readonly SKColor DarkBarFill = new(69, 123, 157);
    private static readonly SKColor LightBarFill = new(33, 76, 120);
    private static readonly SKColor DarkLabelColor = new(200, 200, 200);
    private static readonly SKColor LightLabelColor = new(50, 50, 50);

    private (ISeries[] series, Axis[] yAxes) BuildBarChartFromCoocRows(IReadOnlyList<CoocRow> rows, int maxItems = 20)
    {
        if (rows == null || rows.Count == 0)
            return (Array.Empty<ISeries>(), Array.Empty<Axis>());

        var isDark = Avalonia.Application.Current?.ActualThemeVariant ==
                     Avalonia.Styling.ThemeVariant.Dark;
        var barColor = isDark ? DarkBarFill : LightBarFill;
        var labelColor = isDark ? DarkLabelColor : LightLabelColor;

        var top = rows.Take(maxItems).Reverse().ToArray();
        var values = top.Select(r => (double)r.Freq).ToArray();
        var labels = top.Select(r => r.Key).ToArray();

        var rowSeries = new RowSeries<double>
        {
            Values = values,
            Name = "Frequency",
            Fill = new SolidColorPaint(barColor),
            MaxBarWidth = 20,
            Padding = 2,
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
                TextSize = 12,
                LabelsPaint = new SolidColorPaint(labelColor),
            }
        };

        return (series, yAxes);
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

        ResultGroups.Clear();
        foreach (var group in _groups)
        {
            bool hasMatch = group.Children.Any(c =>
                c.Hit != null && (
                (!string.IsNullOrEmpty(c.Hit.Left) && c.Hit.Left.Contains(term, StringComparison.Ordinal)) ||
                (!string.IsNullOrEmpty(c.Hit.Match) && c.Hit.Match.Contains(term, StringComparison.Ordinal)) ||
                (!string.IsNullOrEmpty(c.Hit.Right) && c.Hit.Right.Contains(term, StringComparison.Ordinal))));
            if (hasMatch)
                ResultGroups.Add(group);
        }

        SelectedSearchSubTabIndex = 0;
    }

    [RelayCommand]
    private void ClearCoocFilter()
    {
        CoocFilterTerm = null;
        IsCoocFilterActive = false;
        ResultGroups.Clear();
        foreach (var g in _groups)
            ResultGroups.Add(g);
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
            .Select(r => new ObservablePoint(Math.Log2(1 + r.Freq), r.Assoc))
            .ToArray();

        var ngramPoints = (ngrams ?? Array.Empty<CoocRow>())
            .Where(r => r.Freq > 0)
            .Select(r => new ObservablePoint(Math.Log2(1 + r.Freq), r.Assoc))
            .ToArray();

        ScatterSeries = new ISeries[]
        {
            new ScatterSeries<ObservablePoint>
            {
                Values = charPoints,
                Name = "Characters",
                GeometrySize = 8,
                Fill = new SolidColorPaint(isDark ? new SKColor(69, 123, 157, 180) : new SKColor(33, 76, 120, 180)),
            },
            new ScatterSeries<ObservablePoint>
            {
                Values = ngramPoints,
                Name = "N-grams",
                GeometrySize = 8,
                Fill = new SolidColorPaint(isDark ? new SKColor(233, 196, 106, 180) : new SKColor(180, 140, 50, 180)),
            }
        };

        ScatterXAxes = new Axis[]
        {
            new Axis
            {
                Name = "log\u2082(Frequency)",
                TextSize = 11,
                NamePaint = new SolidColorPaint(isDark ? new SKColor(200, 200, 200) : new SKColor(50, 50, 50)),
                LabelsPaint = new SolidColorPaint(isDark ? new SKColor(180, 180, 180) : new SKColor(70, 70, 70)),
            }
        };

        ScatterYAxes = new Axis[]
        {
            new Axis
            {
                Name = "Association Score",
                TextSize = 11,
                NamePaint = new SolidColorPaint(isDark ? new SKColor(200, 200, 200) : new SKColor(50, 50, 50)),
                LabelsPaint = new SolidColorPaint(isDark ? new SKColor(180, 180, 180) : new SKColor(70, 70, 70)),
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
        int contextWidth = GetContextWidth();

        _lastQuery = q;
        _lastContextWidth = contextWidth;

        try
        {
            IsCancelEnabled = true;
            SummaryText = $"Search: {q}";
            ProgressText = "Preparing search...";
            ResultsLoadingText = $"Searching for \"{q}\"...";

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
                    if (pendingUiBatch.Count == 0 && !forceSummary) return;

                    var batch = pendingUiBatch.Count > 0 ? pendingUiBatch.ToArray() : Array.Empty<SearchResultGroup>();
                    pendingUiBatch.Clear();

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
                            ResultGroups.Add(batch[i]);

                        RefreshSearchPlaceholderVisibility();

                        if (forceSummary || batch.Length > 0)
                            SummaryText = $"Results: {snapshotGroups:n0} files - {snapshotHits:n0} hits";
                    }, DispatcherPriority.Background);

                    foreach (var group in batch)
                        _ = QueueDeferredEnrichmentAsync(group, originalDir, translatedDir, q, includeO, includeT, contextWidth, mySearchVer);
                }

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
                                   ct: ct))
                {
                    ct.ThrowIfCancellationRequested();

                    if (mySearchVer != Volatile.Read(ref _searchRunVersion))
                        return;

                    if (string.IsNullOrWhiteSpace(g.DisplayName))
                        continue;

                    localGroups.Add(g);
                    pendingUiBatch.Add(g);
                    totalGroups++;
                    totalHits += g.Children.Count;

                    if (totalGroups <= 3 || pendingUiBatch.Count >= 4)
                    {
                        await FlushPendingUiBatchAsync(forceSummary: false);
                        await Task.Yield();
                    }
                }

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
                        .OrderBy(g => g.RelPath, StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    _groups.Clear();
                    _groups.AddRange(sortedGroups);

                    ResultGroups.Clear();
                    foreach (var group in sortedGroups)
                        ResultGroups.Add(group);

                    SummaryText = $"Done: {sortedGroups.Count:n0} files - {totalHits:n0} hits";
                    ProgressText = $"Verified {sortedGroups.Count:n0} matching files";
                    SearchProgressPercent = 100;
                    IsSearchProgressIndeterminate = false;
                    IsResultsLoadingVisible = false;
                    RefreshSearchPlaceholderVisibility();
                    IsExportEnabled = sortedGroups.Count > 0;

                    foreach (var group in sortedGroups)
                        _ = QueueDeferredEnrichmentAsync(group, originalDir, translatedDir, q, includeO, includeT, contextWidth, mySearchVer);
                });
            }, ct);

            if (mySearchVer == Volatile.Read(ref _searchRunVersion))
                await RefreshCoocUiFromCurrentStateAsync();
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

        return group.Children.Any(c => !c.HasSecondaryDisplayText || c.PrimaryIsContextOnly || c.SecondaryIsContextOnly);
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

                if (!ReferenceEquals(target, group))
                    group.ApplyEnrichment(displayChildren);
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
@"Metric guide (using KWIC windows)

All metrics are computed from the SAME evidence:
each hit contributes a window string = Left + Match + Right using your Context width.

Two lists are shown:
- Left panel: single characters (fast signal; useful for names, particles, punctuation patterns)
- Right panel: bigrams + trigrams (phrase fragments; stronger collocation signal)

Fields in each row:
- freq  = total occurrences inside all windows (how often it appears near your query)
- range = number of distinct files where it appears at least once (dispersion proxy)
- score = depends on selected metric
- bar   = tiny visual scale of freq

Metrics:
1) Top co-occurrences (overview)
   Ranking = your stable dispersion-aware score:
   score = (freq / sqrt(1 + totalWindows)) * log(1 + range)
   What it tells you: 'what repeatedly shows up near this query, across many files'
   Best default.

2) Dispersion score (stable)
   Same as overview, but explicitly framed as 'reliable evidence over many documents'.
   Use when you want to avoid one-file artifacts.

3) Frequency (raw)
   score = freq
   Use when you only care about 'what shows up most', even if it's dominated by one text.
   Good for: spotting formulaic refrains in a single long discourse.

4) Range (dispersion proxy)
   score = range
   Use when you want 'breadth' rather than 'intensity'.
   Good for: whether a phrase is widespread across the corpus.

5) Dominance (top-file share)
   score = topFileShare = maxCountInSingleFile / freq
   Interpretation:
   - 80-100% = probably a single-document artifact
   - 20-40%  = fairly dispersed
   Use when results look suspiciously 'too specific'.

6) PMI (window-based)
   PMI is association strength. It rewards exclusivity.
   High PMI often surfaces rare but very 'tight' collocations.
   Warning: PMI loves low-frequency one-offs. Always sanity-check freq + range.

7) logDice
   A lexicography-friendly association measure (more stable than PMI).
   Good for: 'dictionary-like' collocation candidates.

8) t-score
   Frequency-biased association: prefers collocations with lots of evidence.
   Good for: robust collocations that occur often.

How a researcher uses this:
Example query: \u6d1e\u5c71 (Dongshan)
- Start with Top co-occurrences (overview): find names/titles that reliably surround Dongshan.
- Switch to Range: see which collocates appear across many files (tradition-wide usage).
- Switch to Dominance: verify you're not just seeing one famous text dominating.
- Switch to PMI/logDice: hunt for tighter phrase fragments (nicknames, technical terms).
- Use t-score to prioritize collocations with real volume.

Rule of thumb:
PMI/logDice = 'interesting and specific'
t-score/dispersion = 'reliable and common'
dominance = 'artifact detector'";
    }
}




























