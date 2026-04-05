using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CbetaTranslator.App.Models;
using CbetaTranslator.App.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CbetaTranslator.App.ViewModels;

public partial class SearchTabViewModel : ViewModelBase
{
    public sealed class SearchUiState
    {
        public string Query { get; init; } = "";
        public bool SearchOriginal { get; init; } = true;
        public bool SearchTranslated { get; init; }
        public bool ZenOnly { get; init; }
        public int SelectedStatusIndex { get; init; }
        public int SelectedContextIndex { get; init; } = 1;
        public string? SelectedTagFilterName { get; init; }
        public string? SelectedTagFilterId { get; init; }
    }
    private readonly ISearchIndexService _svc;

    public SearchTabViewModel(ISearchIndexService searchIndexService)
    {
        _svc = searchIndexService ?? throw new ArgumentNullException(nameof(searchIndexService));
    }

    private string? _root;
    private string? _originalDir;
    private string? _translatedDir;
    private bool _forceRebuildNextClick;

    private List<FileNavItem> _fileIndex = new();
    private Func<string, (string display, string tooltip, TranslationStatus? status)>? _meta;

    private CancellationTokenSource? _cts;
    private CancellationTokenSource? _autoRerunCts;
    private readonly List<SearchResultGroup> _groups = new();
    private int _batchedStateDepth;

    // remember last search so dropdown recompute works
    private string _lastQuery = "";
    private int _lastContextWidth = 40;

    // avoid stale async metric recomputes racing each other
    private int _metricComputeVersion;

    // avoid stale search UI updates racing each other
    private int _searchRunVersion;

    // Zen flag lookup (provided by MainWindow via SetZenResolver)
    private Func<string, bool>? _isZen;

    // Tag filter
    private List<string> _tagFilterItems = new() { "All Tags" };
    private Dictionary<string, HashSet<string>>? _tagsByName; // tagName -> set of RelPaths
    private Dictionary<string, string>? _tagNameById; // tagId -> displayName
    private Dictionary<string, string>? _tagIdByName; // displayName -> tagId
    private string? _pendingRestoredTagId;

    private static readonly Encoding Utf8NoBom = new UTF8Encoding(false);

    // ----- Observable properties -----

    [ObservableProperty]
    private string _query = "";

    [ObservableProperty]
    private string _progressText = "Index not loaded.";

    [ObservableProperty]
    private string _summaryText = "Ready.";

    [ObservableProperty]
    private bool _isSearching;

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
    private int _selectedStatusIndex;

    [ObservableProperty]
    private int _selectedTagFilterIndex;

    [ObservableProperty]
    private int _selectedContextIndex = 1;

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
    private string _metricGuideText = "";

    // ----- Collections -----

    public ObservableCollection<SearchResultGroup> ResultGroups { get; } = new();
    public ObservableCollection<CoocRow> CoocChars { get; } = new();
    public ObservableCollection<CoocRow> CoocNgrams { get; } = new();

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

    public string[] ContextItems { get; } = new[]
    {
        "20 chars",
        "40 chars",
        "80 chars"
    };

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

    // ----- Property change hooks (trigger auto-rerun) -----

    partial void OnZenOnlyChanged(bool value) => TriggerAutoRerunIfAllowed();
    partial void OnSelectedStatusIndexChanged(int value) => TriggerAutoRerunIfAllowed();
    partial void OnSelectedTagFilterIndexChanged(int value) => TriggerAutoRerunIfAllowed();
    partial void OnSelectedContextIndexChanged(int value) => TriggerAutoRerunIfAllowed();
    partial void OnSearchOriginalChanged(bool value) => TriggerAutoRerunIfAllowed();
    partial void OnSearchTranslatedChanged(bool value) => TriggerAutoRerunIfAllowed();
    partial void OnSelectedCoocMetricIndexChanged(int value) => _ = RefreshCoocUiFromCurrentStateAsync();

    // ----- Public wiring methods (called by MainWindow via code-behind) -----

    public void SetRootContext(string root, string originalDir, string translatedDir)
    {
        _root = root;
        _originalDir = originalDir;
        _translatedDir = translatedDir;
    }

    public void SetFileIndex(IReadOnlyList<FileNavItem> items)
    {
        _fileIndex = items?.ToList() ?? new List<FileNavItem>();
    }

    public void SetContext(
        string root,
        string originalDir,
        string translatedDir,
        Func<string, (string display, string tooltip, TranslationStatus? status)> fileMeta)
    {
        _root = root;
        _originalDir = originalDir;
        _translatedDir = translatedDir;
        _meta = fileMeta;

        ProgressText = "Ready. (Index will load automatically on first search if present.)";
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
        _translatedDir = null;
        _fileIndex.Clear();
        _meta = null;
        _isZen = null;

        _groups.Clear();
        ResultGroups.Clear();

        ProgressText = "No root loaded.";
        SummaryText = "Ready.";
        IsExportEnabled = false;

        _lastQuery = "";
        _lastContextWidth = 40;

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
        if (_root == null || _originalDir == null || _translatedDir == null)
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

            ProgressText = force ? "Rebuilding index..." : "Updating index...";
            SummaryText = force ? "Rebuilding index... (full rebuild)" : "Updating index... (incremental)";

            var prog = new Progress<(int done, int total, string phase)>(p =>
            {
                ProgressText = $"{p.phase} {p.done:n0}/{p.total:n0}";
            });

            await _svc.BuildOrUpdateAsync(_root, _originalDir, _translatedDir, forceRebuild: force, progress: prog, ct: ct);

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
    public Func<Task<string?>>? PickSaveFileAsync { get; set; }

    [RelayCommand]
    private async Task ExportTsvAsync()
    {
        try
        {
            if (_groups.Count == 0)
            {
                StatusChanged?.Invoke(this, "No results to export.");
                return;
            }

            if (PickSaveFileAsync == null)
            {
                StatusChanged?.Invoke(this, "Save file picker not available.");
                return;
            }

            var filePath = await PickSaveFileAsync();
            if (filePath == null) return;

            var sb = new StringBuilder(1024 * 16);
            sb.AppendLine("relPath\tside\tmatchIndex\tleft\tmatch\tright");

            foreach (var g in _groups)
            {
                foreach (var c in g.Children)
                {
                    string side = c.Side == SearchSide.Original ? "O" : "T";
                    sb.Append(g.RelPath).Append('\t')
                      .Append(side).Append('\t')
                      .Append(c.Hit.Index).Append('\t')
                      .Append(EscapeTsv(c.Hit.Left)).Append('\t')
                      .Append(EscapeTsv(c.Hit.Match)).Append('\t')
                      .Append(EscapeTsv(c.Hit.Right)).AppendLine();
                }
            }

            await System.IO.File.WriteAllBytesAsync(filePath, Utf8NoBom.GetBytes(sb.ToString()));

            StatusChanged?.Invoke(this, "Exported TSV.");
        }
        catch (Exception ex)
        {
            StatusChanged?.Invoke(this, "Export failed: " + ex.Message);
        }
    }

    // ----- Filters -----

    private int GetContextWidth()
    {
        return SelectedContextIndex switch
        {
            0 => 20,
            2 => 80,
            _ => 40
        };
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
        CoocSummaryText = "No data yet.";
        ZipfText = "";
        LeftTitle = "Top characters";
        RightTitle = "Top bigrams / trigrams";
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
            });
            return;
        }

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            IsMetricViewVisible = true;
            IsMetricGuideVisible = false;
            CoocSummaryText = "Computing\u2026";
        });

        int myVer = Interlocked.Increment(ref _metricComputeVersion);
        var metric = GetSelectedMetric();

        var snapshotGroups = _groups.ToList();
        string q = _lastQuery;
        int cw = _lastContextWidth;

        var result = await Task.Run(() =>
            SearchIndexService.ComputeCooccurrences(snapshotGroups, q, cw, metric, topK: 30));

        if (myVer != Volatile.Read(ref _metricComputeVersion))
            return;

        await Dispatcher.UIThread.InvokeAsync(() =>
        {
            CoocSummaryText = result.Summary;
            LeftTitle = result.LeftTitle;
            RightTitle = result.RightTitle;

            CoocChars.Clear();
            foreach (var r in result.Left) CoocChars.Add(r);

            CoocNgrams.Clear();
            foreach (var r in result.Right) CoocNgrams.Add(r);

            ZipfText = result.ExtraLine ?? "";
        });
    }

    // ----- Search -----

    private async Task StartSearchAsync()
    {
        if (_root == null || _originalDir == null || _translatedDir == null || _meta == null)
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

        // Reset results
        _groups.Clear();
        ResultGroups.Clear();
        IsExportEnabled = false;
        ClearCoocUi();

        // Snapshot UI-derived values before jumping to background work
        string root = _root;
        string originalDir = _originalDir;
        string translatedDir = _translatedDir;
        var metaFn = _meta;
        int contextWidth = GetContextWidth();

        _lastQuery = q;
        _lastContextWidth = contextWidth;

        try
        {
            IsCancelEnabled = true;
            SummaryText = $"Searching for: {q}";
            ProgressText = "Loading index...";

            // Force a UI turn so the user sees the immediate feedback
            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Render);

            await Task.Run(async () =>
            {
                var manifest = await _svc.TryLoadAsync(root);
                if (manifest == null)
                {
                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (mySearchVer != Volatile.Read(ref _searchRunVersion)) return;
                        ProgressText = "No index found. Build it first.";
                        StatusChanged?.Invoke(this, "No search index found. Click 'Build/Update Index' first.");
                    });
                    return;
                }

                int totalHits = 0;
                int totalGroups = 0;
                long uiAppendMs = 0;

                var localGroups = new List<SearchResultGroup>(256);
                var pendingUiBatch = new List<SearchResultGroup>(16);

                var prog = new Progress<SearchIndexService.SearchProgress>(p =>
                {
                    Dispatcher.UIThread.Post(() =>
                    {
                        if (mySearchVer != Volatile.Read(ref _searchRunVersion))
                            return;

                        string timing =
                            $"  t[cand={p.CandidateMs:n0}ms ver={p.VerifyMs:n0}ms ui={uiAppendMs:n0}ms total={p.TotalMs:n0}ms]";
                        ProgressText = $"{p.Phase}  verified {p.VerifiedDocs:n0}/{p.TotalDocsToVerify:n0}  groups={p.Groups:n0}  hits={p.TotalHits:n0}{timing}";
                    });
                });

                async Task FlushPendingUiBatchAsync(bool forceSummary)
                {
                    if (pendingUiBatch.Count == 0 && !forceSummary) return;

                    var batch = pendingUiBatch.Count > 0 ? pendingUiBatch.ToArray() : Array.Empty<SearchResultGroup>();
                    pendingUiBatch.Clear();

                    int snapshotGroups = totalGroups;
                    int snapshotHits = totalHits;
                    var swUi = System.Diagnostics.Stopwatch.StartNew();

                    await Dispatcher.UIThread.InvokeAsync(() =>
                    {
                        if (mySearchVer != Volatile.Read(ref _searchRunVersion))
                            return;

                        for (int i = 0; i < batch.Length; i++)
                            ResultGroups.Add(batch[i]);

                        if (forceSummary || batch.Length > 0)
                            SummaryText = $"Results: files={snapshotGroups:n0}, hits={snapshotHits:n0}";
                    }, DispatcherPriority.Background);
                    swUi.Stop();
                    uiAppendMs += swUi.ElapsedMilliseconds;
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

                    if (totalGroups <= 8 || pendingUiBatch.Count >= 12)
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

                    _groups.Clear();
                    _groups.AddRange(localGroups);

                    SummaryText = $"Done. files={localGroups.Count:n0}, hits={totalHits:n0}";
                    IsExportEnabled = localGroups.Count > 0;
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
            }
        }
        catch (Exception ex)
        {
            if (mySearchVer == Volatile.Read(ref _searchRunVersion))
            {
                ProgressText = "Search failed: " + ex.Message;
                SummaryText = "Search failed.";
                StatusChanged?.Invoke(this, "Search failed: " + ex.Message);
            }
        }
        finally
        {
            if (mySearchVer == Volatile.Read(ref _searchRunVersion))
            {
                IsCancelEnabled = false;
            }
        }
    }

    // ----- Helpers -----

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

    private static string EscapeTsv(string s)
    {
        s ??= "";
        s = s.Replace("\t", " ").Replace("\r", "").Replace("\n", " ");
        return s;
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









