using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CbetaTranslator.App.Models;
using CbetaTranslator.App.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CbetaTranslator.App.ViewModels;

public partial class TermbaseEditorWindowViewModel : ViewModelBase
{
    private readonly ITermbaseStorageService _storage;
    private readonly string _root;
    private string? _username;

    private bool _suppressFieldSync;

    [ObservableProperty]
    private string _searchQuery = "";

    [ObservableProperty]
    private TermbaseEntry? _selectedEntry;

    [ObservableProperty]
    private string _sourceTerm = "";

    [ObservableProperty]
    private string _preferredTarget = "";

    [ObservableProperty]
    private int _selectedStatusIndex;

    [ObservableProperty]
    private string _alternatesText = "";

    [ObservableProperty]
    private string _noteText = "";

    [ObservableProperty]
    private string _statusMessage = "";

    // Community termbases
    [ObservableProperty]
    private TermbaseEntry? _selectedCommunityEntry;

    [ObservableProperty]
    private string _communityFilter = "";

    [ObservableProperty]
    private bool _hasCommunityEntries;

    public ObservableCollection<TermbaseEntry> AllEntries { get; } = new();
    public ObservableCollection<TermbaseEntry> FilteredEntries { get; } = new();
    public ObservableCollection<TermbaseEntry> CommunityEntries { get; } = new();

    private readonly List<(string Author, TermbaseEntry Entry)> _allCommunityEntries = new();

    public bool Saved { get; private set; }

    /// <summary>
    /// Fired after a successful save. MainWindow subscribes to refresh the assistant panel.
    /// </summary>
    public event EventHandler? TermsSaved;

    /// <summary>
    /// Wired by code-behind to close the window.
    /// </summary>
    public Action? CloseRequested { get; set; }

    /// <summary>
    /// Wired by code-behind to focus the source term field after NewTerm.
    /// </summary>
    public Action? FocusSourceTermRequested { get; set; }

    public TermbaseEditorWindowViewModel(ITermbaseStorageService storage, string root)
    {
        _storage = storage ?? throw new ArgumentNullException(nameof(storage));
        _root = root ?? throw new ArgumentNullException(nameof(root));
    }

    public void SetUsername(string? username)
    {
        _username = string.IsNullOrWhiteSpace(username) ? null : username.Trim();
    }

    // ----- Generated partial methods for property change hooks -----

    partial void OnSearchQueryChanged(string value) => ApplyFilter();

    partial void OnSelectedEntryChanged(TermbaseEntry? value)
    {
        LoadEntryIntoFields(value);
    }

    partial void OnSourceTermChanged(string value) => PushFieldsIntoCurrentEntry();
    partial void OnPreferredTargetChanged(string value) => PushFieldsIntoCurrentEntry();
    partial void OnSelectedStatusIndexChanged(int value) => PushFieldsIntoCurrentEntry();
    partial void OnAlternatesTextChanged(string value) => PushFieldsIntoCurrentEntry();
    partial void OnNoteTextChanged(string value) => PushFieldsIntoCurrentEntry();
    partial void OnCommunityFilterChanged(string value) => RefreshCommunityList();

    // ----- Commands -----

    [RelayCommand]
    private async Task LoadAsync()
    {
        try
        {
            var entries = await _storage.LoadAsync(_root);

            AllEntries.Clear();
            foreach (var entry in entries ?? new List<TermbaseEntry>())
                AllEntries.Add(NormalizeEntry(entry));

            ApplyFilter();

            if (FilteredEntries.Count > 0)
                SelectedEntry = FilteredEntries[0];
            else
                SelectedEntry = null;

            StatusMessage = $"Loaded {AllEntries.Count:n0} term(s).";

            // Load community termbases after main load
            await LoadCommunityAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = "Load failed: " + ex.Message;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        try
        {
            PushFieldsIntoCurrentEntry();

            var cleaned = AllEntries
                .Select(NormalizeEntry)
                .Where(x =>
                    !string.IsNullOrWhiteSpace(x.SourceTerm) ||
                    !string.IsNullOrWhiteSpace(x.PreferredTarget) ||
                    !string.IsNullOrWhiteSpace(x.Note) ||
                    (x.AlternateTargets?.Count ?? 0) > 0)
                .ToList();

            var bad = cleaned.FirstOrDefault(x => string.IsNullOrWhiteSpace(x.SourceTerm));
            if (bad != null)
            {
                StatusMessage = "Save blocked: every non-empty term needs a source term.";
                return;
            }

            await _storage.SaveAsync(_root, cleaned);

            AllEntries.Clear();
            foreach (var entry in cleaned)
                AllEntries.Add(entry);

            Saved = true;
            StatusMessage = $"Saved {cleaned.Count:n0} terms.";
            TermsSaved?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            StatusMessage = "Save failed: " + ex.Message;
        }
    }

    [RelayCommand]
    private void NewTerm()
    {
        var entry = new TermbaseEntry
        {
            SourceTerm = "\u65b0\u8a9e",
            PreferredTarget = "",
            Status = "preferred",
            Note = "",
            AlternateTargets = new List<string>()
        };

        AllEntries.Add(entry);
        ApplyFilter();
        SelectedEntry = entry;
        FocusSourceTermRequested?.Invoke();
        StatusMessage = "New term created.";
    }

    [RelayCommand]
    private void DeleteTerm()
    {
        if (SelectedEntry == null)
            return;

        var entry = SelectedEntry;
        SelectedEntry = null;
        AllEntries.Remove(entry);
        ApplyFilter();
        StatusMessage = "Term deleted.";
    }

    [RelayCommand]
    private void DuplicateTerm()
    {
        if (SelectedEntry == null)
            return;

        var copy = new TermbaseEntry
        {
            SourceTerm = SelectedEntry.SourceTerm,
            PreferredTarget = SelectedEntry.PreferredTarget,
            Status = SelectedEntry.Status,
            Note = SelectedEntry.Note,
            AlternateTargets = (SelectedEntry.AlternateTargets ?? new List<string>()).ToList()
        };

        AllEntries.Add(copy);
        ApplyFilter();
        SelectedEntry = copy;
        StatusMessage = "Term duplicated.";
    }

    [RelayCommand]
    private void CloseWindow()
    {
        CloseRequested?.Invoke();
    }

    // ----- Community termbases -----

    [RelayCommand]
    private async Task LoadCommunityAsync()
    {
        if (string.IsNullOrWhiteSpace(_root)) return;

        try
        {
            var communityDir = TermbaseStorageService.GetCommunityTermbasesDir(_root);
            var allUsers = await _storage.LoadAllCommunityJsonlAsync(communityDir);

            _allCommunityEntries.Clear();

            foreach (var (username, entries) in allUsers)
            {
                // Skip current user's own entries
                if (_username != null &&
                    string.Equals(username, _username, StringComparison.OrdinalIgnoreCase))
                    continue;

                foreach (var e in entries)
                {
                    if (string.IsNullOrWhiteSpace(e.CreatedBy))
                        e.CreatedBy = username;

                    _allCommunityEntries.Add((username, e));
                }
            }

            HasCommunityEntries = _allCommunityEntries.Count > 0;
            RefreshCommunityList();
        }
        catch (Exception ex)
        {
            StatusMessage = "Community load failed: " + ex.Message;
        }
    }

    [RelayCommand]
    private void AdoptSelectedTerm()
    {
        if (SelectedCommunityEntry == null)
            return;

        var source = SelectedCommunityEntry;

        // Check for duplicate
        bool hasDuplicate = AllEntries.Any(e =>
            string.Equals(e.SourceTerm, source.SourceTerm, StringComparison.Ordinal));

        var adopted = new TermbaseEntry
        {
            SourceTerm = source.SourceTerm,
            PreferredTarget = source.PreferredTarget,
            AlternateTargets = (source.AlternateTargets ?? new List<string>()).ToList(),
            Status = source.Status ?? "preferred",
            Note = source.Note ?? "",
            CreatedBy = _username,
            WrittenUtc = DateTimeOffset.UtcNow
        };

        AllEntries.Add(adopted);
        ApplyFilter();
        SelectedEntry = adopted;

        if (hasDuplicate)
            StatusMessage = $"Adopted term \"{source.SourceTerm}\" (note: a term with this source already exists).";
        else
            StatusMessage = $"Adopted term \"{source.SourceTerm}\" from {source.CreatedBy ?? "community"}.";
    }

    private void RefreshCommunityList()
    {
        var prev = SelectedCommunityEntry;
        CommunityEntries.Clear();

        var filter = CommunityFilter?.Trim() ?? "";

        foreach (var (author, e) in _allCommunityEntries)
        {
            if (!string.IsNullOrEmpty(filter))
            {
                bool matches =
                    (e.SourceTerm ?? "").Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                    (e.PreferredTarget ?? "").Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                    (author ?? "").Contains(filter, StringComparison.OrdinalIgnoreCase);

                if (!matches) continue;
            }

            CommunityEntries.Add(e);
        }

        SelectedCommunityEntry = (prev != null && CommunityEntries.Contains(prev))
            ? prev
            : CommunityEntries.FirstOrDefault();
    }

    // ----- Helpers -----

    private void ApplyFilter()
    {
        string q = (SearchQuery ?? "").Trim();

        IEnumerable<TermbaseEntry> seq = AllEntries;

        if (q.Length > 0)
        {
            seq = seq.Where(x =>
                (x.SourceTerm?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (x.PreferredTarget?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (x.Note?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (x.AlternateTargets?.Any(a => a.Contains(q, StringComparison.OrdinalIgnoreCase)) ?? false));
        }

        var filtered = seq
            .OrderBy(x => x.SourceTerm ?? "", StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.PreferredTarget ?? "", StringComparer.OrdinalIgnoreCase)
            .ToList();

        var prev = SelectedEntry;

        FilteredEntries.Clear();
        foreach (var entry in filtered)
            FilteredEntries.Add(entry);

        // Restore or pick first
        if (prev != null && FilteredEntries.Contains(prev))
            SelectedEntry = prev;
        else if (FilteredEntries.Count > 0)
            SelectedEntry = FilteredEntries[0];
        else
            SelectedEntry = null;
    }

    private void LoadEntryIntoFields(TermbaseEntry? entry)
    {
        try
        {
            _suppressFieldSync = true;

            if (entry == null)
            {
                SourceTerm = "";
                PreferredTarget = "";
                AlternatesText = "";
                NoteText = "";
                SelectedStatusIndex = 0;
                return;
            }

            SourceTerm = entry.SourceTerm ?? "";
            PreferredTarget = entry.PreferredTarget ?? "";
            AlternatesText = string.Join(Environment.NewLine, entry.AlternateTargets ?? new List<string>());
            NoteText = entry.Note ?? "";

            string status = (entry.Status ?? "preferred").Trim().ToLowerInvariant();
            SelectedStatusIndex = status switch
            {
                "preferred" => 0,
                "allowed" => 1,
                "deprecated" => 2,
                "forbidden" => 3,
                _ => 0
            };
        }
        finally
        {
            _suppressFieldSync = false;
        }
    }

    private void PushFieldsIntoCurrentEntry()
    {
        if (_suppressFieldSync || SelectedEntry == null)
            return;

        SelectedEntry.SourceTerm = SourceTerm?.Trim() ?? "";
        SelectedEntry.PreferredTarget = PreferredTarget?.Trim() ?? "";
        SelectedEntry.Note = NoteText?.Trim() ?? "";

        SelectedEntry.Status = SelectedStatusIndex switch
        {
            0 => "preferred",
            1 => "allowed",
            2 => "deprecated",
            3 => "forbidden",
            _ => "preferred"
        };

        SelectedEntry.AlternateTargets = (AlternatesText ?? "")
            .Replace("\r\n", "\n")
            .Split('\n')
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    private static TermbaseEntry NormalizeEntry(TermbaseEntry? entry)
    {
        entry ??= new TermbaseEntry();

        entry.SourceTerm = (entry.SourceTerm ?? "").Trim();
        entry.PreferredTarget = (entry.PreferredTarget ?? "").Trim();
        entry.Note = (entry.Note ?? "").Trim();
        entry.Status = string.IsNullOrWhiteSpace(entry.Status) ? "preferred" : entry.Status.Trim();

        entry.AlternateTargets = (entry.AlternateTargets ?? new List<string>())
            .Select(x => (x ?? "").Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        return entry;
    }
}
