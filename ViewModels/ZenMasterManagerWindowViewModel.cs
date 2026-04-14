using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using ReadZen.App.Models;
using ReadZen.App.Services;

namespace ReadZen.App.ViewModels;

public partial class ZenMasterManagerWindowViewModel : ViewModelBase
{
    private readonly ZenMasterManagerService _service;
    private readonly string? _repoRoot;
    private readonly string? _baseFilePath;
    private readonly ObservableCollection<ZenMasterRecord> _allMasters = new();

    private string? _pendingLandingName;
    private string? _pendingLandingUser;
    private ZenMasterCatalog _catalog = new();

    public ZenMasterCatalog? GetCatalog() => _catalog.Records.Count > 0 ? _catalog : null;

    public ZenMasterManagerWindowViewModel(ZenMasterManagerService service, string? repoRoot, string? baseFilePath = null)
    {
        _service = service;
        _repoRoot = repoRoot;
        _baseFilePath = baseFilePath;
    }

    public ObservableCollection<ZenMasterRecord> Masters { get; } = new();

    [ObservableProperty]
    private string _filterText = "";

    [ObservableProperty]
    private ZenMasterRecord? _selectedMaster;

    [ObservableProperty]
    private string _statusText = "Loading Zen masters...";

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
