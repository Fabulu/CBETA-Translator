using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using ReadZen.App.Models;
using ReadZen.App.Services;

namespace ReadZen.App.ViewModels;

public partial class ZenMasterManagerWindowViewModel : ViewModelBase
{
    private readonly ZenMasterManagerService _service;
    private readonly string? _repoRoot;
    private readonly string? _parentRoot;
    private readonly string? _baseFilePath;
    private readonly ObservableCollection<ZenMasterRecord> _allMasters = new();

    private string? _pendingLandingName;
    private string? _pendingLandingUser;
    private ZenMasterCatalog _catalog = new();

    // Corpus search state
    private readonly MasterCorpusSearchService _corpusSearchService;
    private MasterCorpusIndex? _corpusIndex;
    private CancellationTokenSource? _corpusScanCts;

    public ZenMasterCatalog? GetCatalog() => _catalog.Records.Count > 0 ? _catalog : null;
    public MasterCorpusIndex? GetCorpusIndex() => _corpusIndex;

    public ZenMasterManagerWindowViewModel(ZenMasterManagerService service, string? repoRoot, string? parentRoot = null, string? baseFilePath = null)
    {
        _service = service;
        _repoRoot = repoRoot;
        _parentRoot = parentRoot ?? repoRoot;
        _baseFilePath = baseFilePath;
        _corpusSearchService = new MasterCorpusSearchService();
    }

    public ObservableCollection<ZenMasterRecord> Masters { get; } = new();

    [ObservableProperty]
    private string _filterText = "";

    [ObservableProperty]
    private ZenMasterRecord? _selectedMaster;

    [ObservableProperty]
    private string _statusText = "Loading Zen masters...";

    // Corpus search observable properties
    [ObservableProperty]
    private string _corpusStatusText = "Click 'Build Index' to scan the corpus.";

    [ObservableProperty]
    private bool _isCorpusScanning;

    [ObservableProperty]
    private double _corpusScanProgress;

    [ObservableProperty]
    private string _corpusScanProgressText = "";

    public ObservableCollection<MasterTextAppearance> CorpusPrimaryResults { get; } = new();
    public ObservableCollection<MasterTextAppearance> CorpusSecondaryResults { get; } = new();
    public ObservableCollection<CorpusCoOccurrence> CorpusCoOccurrences { get; } = new();

    public bool HasCorpusIndex => _corpusIndex != null;
    public bool HasCorpusPrimary => CorpusPrimaryResults.Count > 0;
    public bool HasCorpusSecondary => CorpusSecondaryResults.Count > 0;
    public bool HasCorpusCoOccurrences => CorpusCoOccurrences.Count > 0;
    public string CorpusSummaryText => _corpusIndex == null
        ? ""
        : $"{_corpusIndex.MasterCount} masters found across {_corpusIndex.FileCount} files ({_corpusIndex.Appearances.Count} appearances)";

    public string SelectedAliasesText => SelectedMaster == null ? "" : string.Join("  |  ", SelectedMaster.Aliases);
    public string SelectedDatesText => SelectedMaster?.DatesSummary ?? "";
    public string SelectedSourceText => SelectedMaster?.SourceSummary ?? "";
    public string SelectedStudentsText => SelectedMaster?.Students is { Count: > 0 } s ? string.Join(", ", s) : "";
    public bool HasSelection => SelectedMaster != null;
    public bool HasNoSelection => SelectedMaster == null;
    public bool HasSchool => !string.IsNullOrWhiteSpace(SelectedMaster?.School);
    public bool HasTeacher => !string.IsNullOrWhiteSpace(SelectedMaster?.Teacher);
    public bool HasStudents => SelectedMaster?.Students is { Count: > 0 };
    public bool HasNotes => !string.IsNullOrWhiteSpace(SelectedMaster?.Notes);
    public bool HasRegion => !string.IsNullOrWhiteSpace(SelectedMaster?.Region);
    public bool HasLinks => SelectedMaster?.HasLinks == true;

    partial void OnFilterTextChanged(string value) => RefreshFilteredMasters();

    partial void OnSelectedMasterChanged(ZenMasterRecord? value)
    {
        OnPropertyChanged(nameof(SelectedAliasesText));
        OnPropertyChanged(nameof(SelectedDatesText));
        OnPropertyChanged(nameof(SelectedSourceText));
        OnPropertyChanged(nameof(SelectedStudentsText));
        OnPropertyChanged(nameof(HasSelection));
        OnPropertyChanged(nameof(HasNoSelection));
        OnPropertyChanged(nameof(HasSchool));
        OnPropertyChanged(nameof(HasTeacher));
        OnPropertyChanged(nameof(HasStudents));
        OnPropertyChanged(nameof(HasNotes));
        OnPropertyChanged(nameof(HasRegion));
        OnPropertyChanged(nameof(HasLinks));

        // Update corpus results for selected master
        RefreshCorpusResultsForSelectedMaster();
    }

    public async Task LoadAsync()
    {
        _catalog = await _service.LoadAsync(_repoRoot, _baseFilePath);

        _allMasters.Clear();
        foreach (var master in _catalog.Records)
            _allMasters.Add(master);

        RefreshFilteredMasters();

        if (!ApplyLandingRequest() && SelectedMaster == null)
            SelectedMaster = Masters.FirstOrDefault();

        if (string.IsNullOrWhiteSpace(StatusText) || StatusText.StartsWith("Loading", StringComparison.OrdinalIgnoreCase))
            StatusText = _catalog.SummaryText;
        else if (!StatusText.StartsWith("Selected ", StringComparison.OrdinalIgnoreCase) &&
                 !StatusText.StartsWith("Zen master ", StringComparison.OrdinalIgnoreCase))
            StatusText = _catalog.SummaryText;

        // Try loading cached corpus index
        await TryLoadCachedCorpusIndexAsync();
    }

    private async Task TryLoadCachedCorpusIndexAsync()
    {
        if (string.IsNullOrEmpty(_parentRoot)) return;

        var cacheDir = MasterCorpusSearchService.GetCacheDir(_parentRoot);
        var cached = await _corpusSearchService.TryLoadAsync(cacheDir);
        if (cached != null)
        {
            _corpusIndex = cached;
            CorpusStatusText = $"Loaded cached index: {CorpusSummaryText}";
            OnPropertyChanged(nameof(HasCorpusIndex));
            OnPropertyChanged(nameof(CorpusSummaryText));
            RefreshCorpusResultsForSelectedMaster();
        }
    }

    public async Task BuildCorpusIndexAsync()
    {
        if (string.IsNullOrEmpty(_parentRoot) || _catalog.Records.Count == 0)
        {
            CorpusStatusText = "No corpus root or masters loaded.";
            return;
        }

        if (IsCorpusScanning)
        {
            // Cancel existing scan
            _corpusScanCts?.Cancel();
            return;
        }

        IsCorpusScanning = true;
        CorpusStatusText = "Scanning corpus...";
        _corpusScanCts = new CancellationTokenSource();
        var ct = _corpusScanCts.Token;

        var progress = new Progress<(int done, int total, string status)>(p =>
        {
            CorpusScanProgress = p.total > 0 ? (double)p.done / p.total * 100 : 0;
            CorpusScanProgressText = p.status;
        });

        try
        {
            _corpusIndex = await _corpusSearchService.BuildFullIndexAsync(_parentRoot!, _catalog, progress, ct);

            // Cache the result
            var cacheDir = MasterCorpusSearchService.GetCacheDir(_parentRoot!);
            await _corpusSearchService.SaveAsync(cacheDir, _corpusIndex, ct);

            CorpusStatusText = CorpusSummaryText;
            OnPropertyChanged(nameof(HasCorpusIndex));
            OnPropertyChanged(nameof(CorpusSummaryText));
            RefreshCorpusResultsForSelectedMaster();
        }
        catch (OperationCanceledException)
        {
            CorpusStatusText = "Corpus scan cancelled.";
        }
        catch (Exception ex)
        {
            CorpusStatusText = $"Scan failed: {ex.Message}";
        }
        finally
        {
            IsCorpusScanning = false;
            CorpusScanProgress = 0;
            CorpusScanProgressText = "";
            _corpusScanCts?.Dispose();
            _corpusScanCts = null;
        }
    }

    public void CancelCorpusScan()
    {
        _corpusScanCts?.Cancel();
    }

    private void RefreshCorpusResultsForSelectedMaster()
    {
        CorpusPrimaryResults.Clear();
        CorpusSecondaryResults.Clear();
        CorpusCoOccurrences.Clear();

        if (_corpusIndex == null || SelectedMaster == null)
        {
            NotifyCorpusResultsChanged();
            return;
        }

        var (primary, secondary) = MasterCorpusSearchService.GetAppearancesForMaster(
            _corpusIndex, SelectedMaster.CanonicalName ?? "");

        foreach (var p in primary) CorpusPrimaryResults.Add(p);
        foreach (var s in secondary) CorpusSecondaryResults.Add(s);

        // Co-occurrences
        var coOccurrences = MasterCorpusSearchService.GetTopCoOccurrences(
            _corpusIndex, SelectedMaster.CanonicalName ?? "", 15);
        foreach (var (name, count) in coOccurrences)
            CorpusCoOccurrences.Add(new CorpusCoOccurrence { MasterName = name, SharedTexts = count });

        NotifyCorpusResultsChanged();
    }

    private void NotifyCorpusResultsChanged()
    {
        OnPropertyChanged(nameof(HasCorpusPrimary));
        OnPropertyChanged(nameof(HasCorpusSecondary));
        OnPropertyChanged(nameof(HasCorpusCoOccurrences));
    }

    public void ApplyLanding(string? name, string? preferredUser = null)
    {
        _pendingLandingName = string.IsNullOrWhiteSpace(name) ? null : name.Trim();
        _pendingLandingUser = string.IsNullOrWhiteSpace(preferredUser) ? null : preferredUser.Trim();

        if (_allMasters.Count > 0)
            ApplyLandingRequest();
    }

    private bool ApplyLandingRequest()
    {
        var requestedName = _pendingLandingName;
        var requestedUser = _pendingLandingUser;

        _pendingLandingName = null;
        _pendingLandingUser = null;

        if (string.IsNullOrWhiteSpace(requestedName))
            return false;

        if (!string.IsNullOrEmpty(FilterText))
            FilterText = string.Empty;
        else
            RefreshFilteredMasters();

        var match = _service.FindLandingMatch(_allMasters, requestedName, requestedUser);
        if (match == null)
        {
            if (SelectedMaster == null)
                SelectedMaster = Masters.FirstOrDefault();

            StatusText = $"Zen master \"{requestedName}\" not found.";
            return false;
        }

        SelectedMaster = Masters.FirstOrDefault(m => ReferenceEquals(m, match.Record))
            ?? Masters.FirstOrDefault(m => string.Equals(m.CanonicalName, match.Record.CanonicalName, StringComparison.OrdinalIgnoreCase));

        if (SelectedMaster == null)
        {
            StatusText = $"Zen master \"{requestedName}\" not found.";
            return false;
        }

        StatusText = BuildLandingStatus(match, requestedUser);
        return true;
    }

    private static string BuildLandingStatus(ZenMasterLandingMatch match, string? requestedUser)
    {
        if (match.UsedPreferredUser)
            return $"Selected {match.Record.CanonicalName} ({match.Variant.SourceSummary}).";

        if (!string.IsNullOrWhiteSpace(requestedUser))
            return $"Selected {match.Record.CanonicalName}; preferred user \"{requestedUser}\" not found.";

        return $"Selected {match.Record.CanonicalName}.";
    }

    private void RefreshFilteredMasters()
    {
        var selectedName = SelectedMaster?.CanonicalName;
        var filtered = _allMasters.Where(m => m.MatchesFilter(FilterText)).ToList();

        Masters.Clear();
        foreach (var master in filtered)
            Masters.Add(master);

        if (!string.IsNullOrWhiteSpace(selectedName))
            SelectedMaster = Masters.FirstOrDefault(m => string.Equals(m.CanonicalName, selectedName, StringComparison.OrdinalIgnoreCase));
        else if (SelectedMaster != null && !Masters.Contains(SelectedMaster))
            SelectedMaster = Masters.FirstOrDefault();
    }
}

/// <summary>Simple model for co-occurrence display.</summary>
public sealed class CorpusCoOccurrence
{
    public string MasterName { get; set; } = "";
    public int SharedTexts { get; set; }
    public string DisplayText => $"{MasterName} ({SharedTexts} shared texts)";
}
