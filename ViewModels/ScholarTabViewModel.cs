using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using CbetaTranslator.App.Models;
using CbetaTranslator.App.Services;
using Microsoft.Extensions.DependencyInjection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CbetaTranslator.App.ViewModels;

public partial class ScholarTabViewModel : ViewModelBase
{
    private readonly IScholarCollectionsService _svc;
    private readonly SemaphoreSlim _saveLock = new(1, 1);
    private string? _root;
    private string? _username;

    // ----- Observable properties -----

    [ObservableProperty]
    private bool _isEmptyState = true;

    [ObservableProperty]
    private string _searchFilter = "";

    [ObservableProperty]
    private string _searchFilterMode = "All";

    [ObservableProperty]
    private string _collectionFilter = "";

    [ObservableProperty]
    private string _sortMode = "Default";

    public static string[] SearchFilterModes { get; } =
        { "All", "Tags", "Masters", "Chinese", "English", "Notes", "Topic", "Form", "Lineage", "Function" };

    public static string[] SortModes { get; } =
        { "Default", "A-Z (Chinese)", "Chronological" };

    [ObservableProperty]
    private ScholarCollection? _selectedCollection;

    [ObservableProperty]
    private ScholarPassage? _selectedPassage;

    [ObservableProperty]
    private string _statusMessage = "";

    /// <summary>
    /// Injected by the view to show a yes/no confirmation dialog.
    /// Parameters: (title, message) => true if confirmed.
    /// </summary>
    public Func<string, string, Task<bool>>? ConfirmAsync { get; set; }

    // Editor fields (bound to detail panel)
    [ObservableProperty]
    private string _passageNotes = "";

    [ObservableProperty]
    private string _passageTags = "";

    [ObservableProperty]
    private string _passageMasterNames = "";

    // Bubble collections for tags and masters
    public ObservableCollection<string> TagBubbles { get; } = new();
    public ObservableCollection<string> MasterBubbles { get; } = new();

    /// <summary>All known master display names (pinyin) for autocomplete.</summary>
    private List<string>? _cachedMasterDisplayNames;
    public List<string> AllMasterDisplayNames
    {
        get
        {
            if (_cachedMasterDisplayNames != null) return _cachedMasterDisplayNames;
            EnsureMasterDatesLoaded();
            _cachedMasterDisplayNames = _masterEntries?.Select(e => e.Names[0]).OrderBy(n => n).ToList()
                                        ?? new List<string>();
            return _cachedMasterDisplayNames;
        }
    }

    // Facet categorization fields
    [ObservableProperty]
    private string _doctrinalTopic = "";

    [ObservableProperty]
    private string _literaryForm = "";

    [ObservableProperty]
    private string _lineage = "";

    [ObservableProperty]
    private string _rhetoricalFunction = "";

    // Whether the detail editor fields are enabled (false for community passages)
    [ObservableProperty]
    private bool _isEditorEnabled = true;

    // Study notes (per-collection)
    [ObservableProperty]
    private string _studyNotes = "";

    // Facet dropdown options
    public ObservableCollection<string> DoctrinalTopicOptions { get; } = new();
    public ObservableCollection<string> LiteraryFormOptions { get; } = new();
    public ObservableCollection<string> LineageOptions { get; } = new();
    public ObservableCollection<string> RhetoricalFunctionOptions { get; } = new();

    // Community collections
    [ObservableProperty]
    private ScholarCollection? _selectedCommunityCollection;

    [ObservableProperty]
    private ScholarPassage? _selectedCommunityPassage;

    [ObservableProperty]
    private string _communityFilter = "";

    [ObservableProperty]
    private bool _hasCommunityCollections;

    [ObservableProperty]
    private int _selectedCommunityUserIndex;

    private List<string> _communityUsernames = new() { "All Users" };
    public List<string> CommunityUsernames
    {
        get => _communityUsernames;
        set { _communityUsernames = value; OnPropertyChanged(); }
    }

    partial void OnSelectedCommunityUserIndexChanged(int value)
    {
        RefreshCommunityCollectionsList();
    }

    // Target collection for adopting community passages
    [ObservableProperty]
    private ScholarCollection? _adoptTargetCollection;

    // ----- Collections -----

    public ObservableCollection<ScholarCollection> Collections { get; } = new();
    public ObservableCollection<ScholarPassage> Passages { get; } = new();
    public ObservableCollection<ScholarCollection> CommunityCollections { get; } = new();
    public ObservableCollection<ScholarPassage> CommunityPassages { get; } = new();

    // Backing list for collection filtering
    private readonly List<ScholarCollection> _allCollections = new();
    private readonly List<(string Author, ScholarCollection Collection)> _allCommunityCollections = new();

    // Master dates for chronological sort
    private Dictionary<string, int>? _masterDatesLookup;
    private bool _masterDatesLoadAttempted;

    // Raw master entries for auto-detection of master names in passages
    private List<MasterNameEntry>? _masterEntries;

    // ----- Bridge delegates (wired by code-behind for file pickers) -----

    public Func<Task<string?>>? PickExportFileAsync { get; set; }
    public Func<Task<string?>>? PickImportFileAsync { get; set; }
    public Func<Task<ScholarExportFormat?>>? PickExportFormatAsync { get; set; }

    // ----- Events -----

    public event EventHandler<NavigationRequest>? NavigationRequested;
    public event EventHandler<string>? StatusChanged;

    // ----- Constructor -----

    public ScholarTabViewModel(IScholarCollectionsService svc)
    {
        _svc = svc ?? throw new ArgumentNullException(nameof(svc));
        LoadFacetOptions();
    }

    // ----- Public API: save all current state (for tab switch / app close) -----

    public async Task SaveCurrentStateAsync()
    {
        SyncEditorFieldsToPassage();
        if (SelectedCollection != null)
            SelectedCollection.StudyNotes = StudyNotes;
        await SaveAsync();
    }

    // ----- Public wiring -----

    public void SetUsername(string? username)
    {
        _username = string.IsNullOrWhiteSpace(username) ? null : username.Trim();
    }

    public string? GetRoot() => _root;

    public void SetRoot(string root)
    {
        _root = root;
        _ = SafeFireAndForget(LoadAsync());
        _ = SafeFireAndForget(LoadCommunityAsync());
    }

    // ----- Commands -----

    [RelayCommand]
    private async Task LoadAsync()
    {
        if (string.IsNullOrWhiteSpace(_root)) return;

        try
        {
            var loaded = !string.IsNullOrWhiteSpace(_username)
                ? await _svc.LoadUserAsync(_root, _username)
                : await _svc.LoadAsync(_root);

            await RunOnUiAsync(() =>
            {
                _allCollections.Clear();
                _allCollections.AddRange(loaded);
                RefreshCollectionsList();

                RefreshIsEmptyState();
                StatusMessage = $"Loaded {_allCollections.Count} collection(s).";
                StatusChanged?.Invoke(this, StatusMessage);
            });
        }
        catch (Exception ex)
        {
            StatusMessage = "Load failed: " + ex.Message;
            StatusChanged?.Invoke(this, StatusMessage);
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (string.IsNullOrWhiteSpace(_root)) return;

        await _saveLock.WaitAsync();
        try
        {
            // Sync editor fields back to selected passage before saving
            SyncEditorFieldsToPassage();

            try
            {
                var list = _allCollections.ToList();
                if (!string.IsNullOrWhiteSpace(_username))
                    await _svc.SaveUserAsync(_root, _username, list);
                else
                    await _svc.SaveAsync(_root, list);
                StatusMessage = "Saved.";
                StatusChanged?.Invoke(this, StatusMessage);
            }
            catch (Exception ex)
            {
                StatusMessage = "Save failed: " + ex.Message;
                StatusChanged?.Invoke(this, StatusMessage);
            }
        }
        finally
        {
            _saveLock.Release();
        }
    }

    [RelayCommand]
    private void AddCollection()
    {
        var c = new ScholarCollection
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = "New Collection",
            CreatedUtc = DateTimeOffset.UtcNow,
            CreatedBy = _username
        };
        _allCollections.Add(c);
        Collections.Add(c);
        SelectedCollection = c;
        IsEmptyState = false;
        _ = SafeFireAndForget(SaveAsync());
    }

    [RelayCommand]
    private async Task DeleteCollectionAsync()
    {
        if (SelectedCollection == null) return;
        if (ConfirmAsync != null && !await ConfirmAsync("Delete Collection", $"Delete '{SelectedCollection.Name}'? This cannot be undone.")) return;
        _allCollections.Remove(SelectedCollection);
        Collections.Remove(SelectedCollection);
        SelectedCollection = Collections.FirstOrDefault();
        RefreshIsEmptyState();
        _ = SafeFireAndForget(SaveAsync());
    }

    [RelayCommand]
    private async Task DeletePassageAsync()
    {
        if (SelectedPassage == null || SelectedCollection == null) return;
        if (ConfirmAsync != null && !await ConfirmAsync("Delete Passage", "Delete this passage? This cannot be undone.")) return;
        var deletedId = SelectedPassage.Id;
        var deletedRelPath = SelectedPassage.SourceRelPath;
        SelectedCollection.Passages.Remove(SelectedPassage);
        Passages.Remove(SelectedPassage);

        // Clean up orphan links referencing the deleted passage
        SelectedCollection.Links.RemoveAll(l =>
            l.FromPassageId == deletedId || l.ToPassageId == deletedId);

        // Clean up LinkedTexts referencing the deleted passage's source
        if (!string.IsNullOrEmpty(deletedRelPath))
        {
            foreach (var p in SelectedCollection.Passages)
                p.LinkedTexts.Remove(deletedRelPath);
        }

        SelectedPassage = Passages.FirstOrDefault();
        _ = SafeFireAndForget(SaveAsync());
    }

    [RelayCommand]
    private void NavigateToPassage()
    {
        if (SelectedPassage == null) return;
        NavigationRequested?.Invoke(this, new NavigationRequest
        {
            RelPath = SelectedPassage.SourceRelPath,
            MatchText = SelectedPassage.ZhText.Length > 80
                ? SelectedPassage.ZhText[..80]
                : SelectedPassage.ZhText,
            AnchorStartHint = SelectedPassage.StartBlockNumber
        });
    }

    [RelayCommand]
    private async Task ExportCollectionsAsync()
    {
        if (PickExportFileAsync == null)
        {
            StatusMessage = "Export not available (no file picker).";
            return;
        }

        try
        {
            // Ask user for export format first
            ScholarExportFormat? chosenFormat = null;
            if (PickExportFormatAsync != null)
                chosenFormat = await PickExportFormatAsync();

            // null means user cancelled the format dialog — fall back to JSON export
            // (also used when PickExportFormatAsync is not wired)

            var path = await PickExportFileAsync();
            if (string.IsNullOrWhiteSpace(path)) return;

            SyncEditorFieldsToPassage();

            if (chosenFormat != null)
            {
                // Rich export via ScholarExportService
                var exportSvc = App.Services.GetRequiredService<IScholarExportService>();
                var target = SelectedCollection;
                if (target == null)
                {
                    StatusMessage = "Select a collection to export.";
                    StatusChanged?.Invoke(this, StatusMessage);
                    return;
                }
                await exportSvc.ExportAsync(path, target, chosenFormat.Value);
                StatusMessage = $"Exported '{target.Name}' as {chosenFormat.Value} to {Path.GetFileName(path)}.";
            }
            else
            {
                // Default JSON export of all collections
                var list = _allCollections.ToList();
                await _svc.ExportAsync(path, list);
                StatusMessage = $"Exported {list.Count} collection(s) to {Path.GetFileName(path)}.";
            }

            StatusChanged?.Invoke(this, StatusMessage);
        }
        catch (Exception ex)
        {
            StatusMessage = "Export failed: " + ex.Message;
            StatusChanged?.Invoke(this, StatusMessage);
        }
    }

    [RelayCommand]
    private async Task ImportCollectionsAsync()
    {
        if (PickImportFileAsync == null)
        {
            StatusMessage = "Import not available (no file picker).";
            return;
        }

        try
        {
            var path = await PickImportFileAsync();
            if (string.IsNullOrWhiteSpace(path)) return;

            var imported = await _svc.ImportAsync(path);
            if (imported.Count == 0)
            {
                StatusMessage = "No collections found in file.";
                StatusChanged?.Invoke(this, StatusMessage);
                return;
            }

            // Merge: add new collections, merge passages into existing ones by Id
            int newCollections = 0;
            int mergedPassages = 0;

            foreach (var ic in imported)
            {
                var existing = _allCollections.FirstOrDefault(c =>
                    string.Equals(c.Id, ic.Id, StringComparison.Ordinal));

                if (existing != null)
                {
                    // Merge passages by Id
                    var existingIds = new HashSet<string>(
                        existing.Passages.Select(p => p.Id),
                        StringComparer.Ordinal);

                    foreach (var p in ic.Passages)
                    {
                        if (!existingIds.Contains(p.Id))
                        {
                            existing.Passages.Add(p);
                            mergedPassages++;
                        }
                    }
                }
                else
                {
                    _allCollections.Add(ic);
                    newCollections++;
                }
            }

            await RunOnUiAsync(() =>
            {
                RefreshCollectionsList();
                RefreshIsEmptyState();
            });

            await SaveAsync();

            StatusMessage = $"Imported: {newCollections} new collection(s), {mergedPassages} new passage(s) merged.";
            StatusChanged?.Invoke(this, StatusMessage);
        }
        catch (Exception ex)
        {
            StatusMessage = "Import failed: " + ex.Message;
            StatusChanged?.Invoke(this, StatusMessage);
        }
    }

    // ----- Community collections -----

    [RelayCommand]
    private async Task LoadCommunityAsync()
    {
        if (string.IsNullOrWhiteSpace(_root)) return;

        try
        {
            var communityDir = ScholarCollectionsService.GetCommunityCollectionsDir(_root);
            var allUsers = await _svc.LoadAllCommunityJsonlAsync(communityDir);

            await RunOnUiAsync(() =>
            {
                _allCommunityCollections.Clear();

                foreach (var (username, collections) in allUsers)
                {
                    // Skip current user's own collections
                    if (_username != null &&
                        string.Equals(username, _username, StringComparison.OrdinalIgnoreCase))
                        continue;

                    foreach (var c in collections)
                    {
                        if (string.IsNullOrWhiteSpace(c.CreatedBy))
                            c.CreatedBy = username;

                        _allCommunityCollections.Add((username, c));
                    }
                }

                HasCommunityCollections = _allCommunityCollections.Count > 0;
                RefreshIsEmptyState();

                // Populate user picker
                var usernames = _allCommunityCollections
                    .Select(x => x.Author)
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(u => u, StringComparer.OrdinalIgnoreCase)
                    .ToList();
                var items = new List<string> { "All Users" };
                items.AddRange(usernames);
                CommunityUsernames = items;
                SelectedCommunityUserIndex = 0;

                RefreshCommunityCollectionsList();
            });
        }
        catch (Exception ex)
        {
            StatusMessage = "Community load failed: " + ex.Message;
            StatusChanged?.Invoke(this, StatusMessage);
        }
    }

    partial void OnCommunityFilterChanged(string value)
    {
        RefreshCommunityCollectionsList();
    }

    partial void OnSelectedCommunityCollectionChanged(ScholarCollection? value)
    {
        RefreshCommunityPassagesList();
    }

    partial void OnSelectedCommunityPassageChanged(ScholarPassage? value)
    {
        // Handled by code-behind to update detail fields
    }

    private void RefreshCommunityCollectionsList()
    {
        var prev = SelectedCommunityCollection;
        CommunityCollections.Clear();

        var filter = CommunityFilter?.Trim() ?? "";

        string? selectedUser = _selectedCommunityUserIndex > 0 && _selectedCommunityUserIndex < _communityUsernames.Count
            ? _communityUsernames[_selectedCommunityUserIndex]
            : null;

        foreach (var (author, c) in _allCommunityCollections)
        {
            // Filter by selected user
            if (selectedUser != null && !string.Equals(author, selectedUser, StringComparison.OrdinalIgnoreCase))
                continue;

            if (!string.IsNullOrEmpty(filter))
            {
                bool matches =
                    (c.Name ?? "").Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                    (c.Description ?? "").Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                    (author ?? "").Contains(filter, StringComparison.OrdinalIgnoreCase);

                if (!matches) continue;
            }

            CommunityCollections.Add(c);
        }

        SelectedCommunityCollection = (prev != null && CommunityCollections.Contains(prev))
            ? prev
            : CommunityCollections.FirstOrDefault();
    }

    private void RefreshCommunityPassagesList()
    {
        CommunityPassages.Clear();
        if (SelectedCommunityCollection == null)
        {
            SelectedCommunityPassage = null;
            return;
        }

        foreach (var p in SelectedCommunityCollection.Passages)
            CommunityPassages.Add(p);

        SelectedCommunityPassage = CommunityPassages.FirstOrDefault();
    }

    [RelayCommand]
    private async Task AdoptSelectedPassageAsync()
    {
        if (SelectedCommunityPassage == null)
        {
            StatusMessage = "No community passage selected.";
            StatusChanged?.Invoke(this, StatusMessage);
            return;
        }

        // If no collections exist, create a default one
        if (_allCollections.Count == 0)
        {
            AddCollection();
        }

        var target = AdoptTargetCollection ?? SelectedCollection ?? Collections.FirstOrDefault();
        if (target == null)
        {
            StatusMessage = "Select a target collection first.";
            StatusChanged?.Invoke(this, StatusMessage);
            return;
        }

        await AdoptPassageToCollectionAsync(SelectedCommunityPassage, target);
    }

    public async Task AdoptPassageToCollectionAsync(ScholarPassage sourcePassage, ScholarCollection targetCollection)
    {
        // Deep copy
        var adopted = new ScholarPassage
        {
            Id = Guid.NewGuid().ToString("N"),
            SourceRelPath = sourcePassage.SourceRelPath,
            ZhText = sourcePassage.ZhText,
            EnText = sourcePassage.EnText,
            Notes = sourcePassage.Notes,
            Tags = new List<string>(sourcePassage.Tags),
            MasterNames = new List<string>(sourcePassage.MasterNames),
            CreatedBy = _username,
            AddedUtc = DateTimeOffset.UtcNow,
            ModifiedUtc = null
        };

        // Auto-detect and merge master names from passage text
        AutoTagMasterNames(adopted);

        targetCollection.Passages.Add(adopted);

        // If the target collection is currently displayed, update the observable list
        if (SelectedCollection?.Id == targetCollection.Id)
        {
            Passages.Add(adopted);
        }

        IsEmptyState = false;
        await SaveAsync();

        StatusMessage = $"Adopted passage to '{targetCollection.Name}'.";
        StatusChanged?.Invoke(this, StatusMessage);
    }

    // ----- Link management -----

    public async Task CreateLinkAsync(string fromId, string toId, string relationType)
    {
        if (SelectedCollection == null) return;

        var link = new PassageLink
        {
            Id = Guid.NewGuid().ToString("N"),
            FromPassageId = fromId,
            ToPassageId = toId,
            RelationType = relationType,
            CreatedUtc = DateTimeOffset.UtcNow
        };

        SelectedCollection.Links.Add(link);
        await SaveAsync();
    }

    public async Task RemoveLinkAsync(string linkId)
    {
        if (SelectedCollection == null) return;
        SelectedCollection.Links.RemoveAll(l => l.Id == linkId);
        await SaveAsync();
    }

    public List<PassageLink> GetLinksForPassage(string passageId)
    {
        if (SelectedCollection == null) return new List<PassageLink>();
        return SelectedCollection.Links
            .Where(l => l.FromPassageId == passageId || l.ToPassageId == passageId)
            .ToList();
    }

    public ScholarPassage? FindPassageById(string passageId)
    {
        return SelectedCollection?.Passages.FirstOrDefault(p => p.Id == passageId);
    }

    // ----- Public API -----

    public async Task AddPassageToCollectionAsync(string collectionId, ScholarPassage passage)
    {
        var collection = Collections.FirstOrDefault(c => c.Id == collectionId);
        if (collection == null) return;

        passage.Id = Guid.NewGuid().ToString("N");
        passage.AddedUtc = DateTimeOffset.UtcNow;
        passage.CreatedBy = _username;

        // Auto-detect master names from passage text
        AutoTagMasterNames(passage);

        collection.Passages.Add(passage);

        if (SelectedCollection?.Id == collectionId)
        {
            Passages.Add(passage);
        }

        IsEmptyState = false;
        await SaveAsync();
    }

    // ----- Selection sync -----

    partial void OnSelectedCollectionChanging(ScholarCollection? value)
    {
        // Save passage edits back to the outgoing passage
        if (SelectedPassage != null && SelectedCollection != null)
        {
            SyncEditorFieldsToPassage();
        }

        // Save study notes back to the outgoing collection
        if (SelectedCollection != null && SelectedCollection.StudyNotes != StudyNotes)
        {
            SelectedCollection.StudyNotes = StudyNotes;
        }

        // Persist everything
        if (SelectedCollection != null)
        {
            _ = SafeFireAndForget(SaveAsync());
        }
    }

    partial void OnSelectedCollectionChanged(ScholarCollection? value)
    {
        StudyNotes = value?.StudyNotes ?? "";
        RefreshPassagesList();
    }

    partial void OnStudyNotesChanged(string value)
    {
        // Keep in-memory collection in sync so study notes are never lost
        if (SelectedCollection != null)
            SelectedCollection.StudyNotes = value;
    }

    partial void OnSearchFilterChanged(string value)
    {
        RefreshPassagesList();
    }

    partial void OnSearchFilterModeChanged(string value)
    {
        RefreshPassagesList();
    }

    partial void OnSortModeChanged(string value)
    {
        RefreshPassagesList();
    }

    partial void OnCollectionFilterChanged(string value)
    {
        RefreshCollectionsList();
    }

    private void RefreshCollectionsList()
    {
        var prev = SelectedCollection;
        Collections.Clear();

        var filter = CollectionFilter?.Trim() ?? "";
        IEnumerable<ScholarCollection> source = _allCollections;

        if (!string.IsNullOrEmpty(filter))
        {
            source = source.Where(c =>
                c.Name.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                c.Description.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                c.Tags.Any(t => t.Contains(filter, StringComparison.OrdinalIgnoreCase)));
        }

        foreach (var c in source)
            Collections.Add(c);

        // Restore previous selection if still visible, otherwise pick first
        SelectedCollection = (prev != null && Collections.Contains(prev))
            ? prev
            : Collections.FirstOrDefault();
    }

    private void RefreshPassagesList()
    {
        Passages.Clear();
        if (SelectedCollection == null)
        {
            SelectedPassage = null;
            return;
        }

        var filter = SearchFilter?.Trim() ?? "";
        IEnumerable<ScholarPassage> passages = SelectedCollection.Passages;

        if (!string.IsNullOrEmpty(filter))
        {
            var mode = SearchFilterMode ?? "All";
            passages = passages.Where(p => mode switch
            {
                "Tags" => p.Tags.Any(t => t.Contains(filter, StringComparison.OrdinalIgnoreCase)),
                "Masters" => p.MasterNames.Any(m => m.Contains(filter, StringComparison.OrdinalIgnoreCase)),
                "Chinese" => (p.ZhText ?? "").Contains(filter, StringComparison.OrdinalIgnoreCase),
                "English" => (p.EnText ?? "").Contains(filter, StringComparison.OrdinalIgnoreCase),
                "Notes" => (p.Notes ?? "").Contains(filter, StringComparison.OrdinalIgnoreCase),
                "Topic" => (p.DoctrinalTopic ?? "").Contains(filter, StringComparison.OrdinalIgnoreCase),
                "Form" => (p.LiteraryForm ?? "").Contains(filter, StringComparison.OrdinalIgnoreCase),
                "Lineage" => (p.Lineage ?? "").Contains(filter, StringComparison.OrdinalIgnoreCase),
                "Function" => (p.RhetoricalFunction ?? "").Contains(filter, StringComparison.OrdinalIgnoreCase),
                _ => p.Tags.Any(t => t.Contains(filter, StringComparison.OrdinalIgnoreCase)) ||
                     p.MasterNames.Any(m => m.Contains(filter, StringComparison.OrdinalIgnoreCase)) ||
                     (p.ZhText ?? "").Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                     (p.EnText ?? "").Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                     (p.Notes ?? "").Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                     (p.DoctrinalTopic ?? "").Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                     (p.LiteraryForm ?? "").Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                     (p.Lineage ?? "").Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                     (p.RhetoricalFunction ?? "").Contains(filter, StringComparison.OrdinalIgnoreCase)
            });
        }

        // Apply sort mode
        var sortMode = SortMode ?? "Default";
        var sorted = sortMode switch
        {
            "A-Z (Chinese)" => passages.OrderBy(p => p.ZhText ?? "", StringComparer.Ordinal),
            "Chronological" => passages.OrderBy(p => GetChronologicalKey(p)),
            _ => passages
        };

        foreach (var p in sorted)
            Passages.Add(p);

        SelectedPassage = Passages.FirstOrDefault();
    }

    private int GetChronologicalKey(ScholarPassage passage)
    {
        EnsureMasterDatesLoaded();
        if (_masterDatesLookup == null || _masterDatesLookup.Count == 0)
            return int.MaxValue;

        // Check each master name for a match
        foreach (var name in passage.MasterNames)
        {
            if (_masterDatesLookup.TryGetValue(name, out var year))
                return year;
        }

        // Also check ZhText for master name mentions
        foreach (var (masterName, year) in _masterDatesLookup)
        {
            if ((passage.ZhText ?? "").Contains(masterName, StringComparison.Ordinal))
                return year;
        }

        return int.MaxValue;
    }

    private void EnsureMasterDatesLoaded()
    {
        if (_masterDatesLoadAttempted) return;
        _masterDatesLoadAttempted = true;

        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "Assets", "Data", "master-dates.json");
            if (!File.Exists(path))
                return;

            var json = File.ReadAllText(path);
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (!root.TryGetProperty("masters", out var mastersEl))
                return;

            _masterDatesLookup = new Dictionary<string, int>(StringComparer.Ordinal);
            _masterEntries = new List<MasterNameEntry>();

            var baseNames = new HashSet<string>(StringComparer.Ordinal);

            foreach (var master in mastersEl.EnumerateArray())
            {
                int floruit = master.TryGetProperty("floruit", out var f) ? f.GetInt32() : 0;

                var names = new List<string>();
                if (master.TryGetProperty("names", out var namesEl))
                {
                    foreach (var nameEl in namesEl.EnumerateArray())
                    {
                        var name = nameEl.GetString();
                        if (!string.IsNullOrEmpty(name))
                        {
                            names.Add(name);
                            baseNames.Add(name);
                        }
                    }
                }

                if (names.Count > 0)
                    _masterEntries.Add(new MasterNameEntry(names));

                if (floruit == 0) continue;

                foreach (var name in names)
                {
                    if (!_masterDatesLookup.ContainsKey(name))
                        _masterDatesLookup[name] = floruit;
                }
            }

            // Merge community master dates (new masters only; base entries win)
            MergeCommunityMasterDates(baseNames);
        }
        catch
        {
            _masterDatesLookup = null;
            _masterEntries = null;
            _cachedMasterDisplayNames = null;
        }
    }

    private void MergeCommunityMasterDates(HashSet<string> baseNames)
    {
        if (string.IsNullOrWhiteSpace(_root) || _masterDatesLookup == null || _masterEntries == null)
            return;

        try
        {
            var communityDir = IMasterDatesService.GetCommunityMasterDatesDir(_root);
            if (!Directory.Exists(communityDir))
                return;

            // Load synchronously (same pattern as base load above)
            var allUsers = new Dictionary<string, List<Models.MasterDateEntry>>(StringComparer.OrdinalIgnoreCase);
            var readOpts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

            foreach (var file in Directory.GetFiles(communityDir, "*.jsonl"))
            {
                var username = Path.GetFileNameWithoutExtension(file);
                var entries = new List<Models.MasterDateEntry>();
                var lines = File.ReadAllLines(file, System.Text.Encoding.UTF8);
                foreach (var line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    try
                    {
                        var e = JsonSerializer.Deserialize<Models.MasterDateEntry>(line, readOpts);
                        if (e != null) entries.Add(e);
                    }
                    catch { }
                }
                if (entries.Count > 0)
                    allUsers[username] = entries;
            }

            // Track which community masters have been added (first user alphabetically wins)
            var addedCommunityNames = new HashSet<string>(StringComparer.Ordinal);

            foreach (var (_, entries) in allUsers.OrderBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase))
            {
                foreach (var entry in entries)
                {
                    // Skip if overlaps with base
                    if (MasterDatesService.OverlapsWithBase(entry, baseNames))
                        continue;

                    // Skip if already added by an earlier user (alphabetical wins)
                    bool alreadyAdded = false;
                    foreach (var name in entry.Names)
                    {
                        var trimmed = name.Trim();
                        if (!string.IsNullOrEmpty(trimmed) && addedCommunityNames.Contains(trimmed))
                        {
                            alreadyAdded = true;
                            break;
                        }
                    }
                    if (alreadyAdded) continue;

                    // Add new community master to the lookup + master entries
                    var cleanNames = entry.Names
                        .Select(n => n.Trim())
                        .Where(n => n.Length > 0)
                        .ToList();

                    if (cleanNames.Count > 0)
                        _masterEntries.Add(new MasterNameEntry(cleanNames));

                    foreach (var name in cleanNames)
                    {
                        addedCommunityNames.Add(name);
                        if (entry.Floruit > 0 && !_masterDatesLookup.ContainsKey(name))
                            _masterDatesLookup[name] = entry.Floruit;
                    }
                }
            }
        }
        catch
        {
            // Community merge failure is non-fatal
        }
    }

    public void InvalidateMasterDatesCache()
    {
        _masterDatesLookup = null;
        _masterEntries = null;
        _masterDatesLoadAttempted = false;
    }

    partial void OnSelectedPassageChanging(ScholarPassage? value)
    {
        // Save current passage edits before switching to the new one
        if (SelectedPassage != null && SelectedCollection != null)
        {
            SyncEditorFieldsToPassage();
            _ = SafeFireAndForget(SaveAsync());
        }
    }

    partial void OnSelectedPassageChanged(ScholarPassage? value)
    {
        IsEditorEnabled = true;

        if (value != null)
        {
            PassageNotes = value.Notes ?? "";
            PassageTags = string.Join(", ", value.Tags ?? new List<string>());
            PassageMasterNames = string.Join(", ", value.MasterNames ?? new List<string>());
            DoctrinalTopic = value.DoctrinalTopic ?? "";
            LiteraryForm = value.LiteraryForm ?? "";
            Lineage = value.Lineage ?? "";
            RhetoricalFunction = value.RhetoricalFunction ?? "";

            // Populate bubble collections
            TagBubbles.Clear();
            foreach (var t in value.Tags ?? new List<string>())
                if (!string.IsNullOrWhiteSpace(t)) TagBubbles.Add(t.Trim());

            MasterBubbles.Clear();
            foreach (var m in value.MasterNames ?? new List<string>())
                if (!string.IsNullOrWhiteSpace(m)) MasterBubbles.Add(m.Trim());
        }
        else
        {
            PassageNotes = "";
            PassageTags = "";
            PassageMasterNames = "";
            DoctrinalTopic = "";
            LiteraryForm = "";
            Lineage = "";
            RhetoricalFunction = "";
            TagBubbles.Clear();
            MasterBubbles.Clear();
        }
    }


    // ----- Helpers -----

    private void SyncEditorFieldsToPassage()
    {
        if (SelectedPassage == null) return;

        SelectedPassage.Notes = PassageNotes ?? "";
        SelectedPassage.Tags = SplitCommaSeparated(PassageTags);
        SelectedPassage.MasterNames = SplitCommaSeparated(PassageMasterNames);
        SelectedPassage.DoctrinalTopic = string.IsNullOrWhiteSpace(DoctrinalTopic) ? null : DoctrinalTopic.Trim();
        SelectedPassage.LiteraryForm = string.IsNullOrWhiteSpace(LiteraryForm) ? null : LiteraryForm.Trim();
        SelectedPassage.Lineage = string.IsNullOrWhiteSpace(Lineage) ? null : Lineage.Trim();
        SelectedPassage.RhetoricalFunction = string.IsNullOrWhiteSpace(RhetoricalFunction) ? null : RhetoricalFunction.Trim();
        SelectedPassage.ModifiedUtc = DateTimeOffset.UtcNow;

        // Sync study notes back to collection
        if (SelectedCollection != null)
            SelectedCollection.StudyNotes = StudyNotes ?? "";
    }

    public void AddTag(string tag)
    {
        tag = tag.Trim();
        if (string.IsNullOrWhiteSpace(tag)) return;
        if (TagBubbles.Contains(tag, StringComparer.OrdinalIgnoreCase)) return;
        TagBubbles.Add(tag);
        PassageTags = string.Join(", ", TagBubbles);
        SyncEditorFieldsToPassage();
        _ = SafeFireAndForget(SaveAsync());
    }

    public void RemoveTag(string tag)
    {
        var existing = TagBubbles.FirstOrDefault(t => string.Equals(t, tag, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
            TagBubbles.Remove(existing);
            PassageTags = string.Join(", ", TagBubbles);
            SyncEditorFieldsToPassage();
            _ = SafeFireAndForget(SaveAsync());
        }
    }

    public void AddMaster(string name)
    {
        name = name.Trim();
        if (string.IsNullOrWhiteSpace(name)) return;
        if (MasterBubbles.Contains(name, StringComparer.OrdinalIgnoreCase)) return;
        MasterBubbles.Add(name);
        PassageMasterNames = string.Join(", ", MasterBubbles);
        SyncEditorFieldsToPassage();
        _ = SafeFireAndForget(SaveAsync());
    }

    public void RemoveMaster(string name)
    {
        var existing = MasterBubbles.FirstOrDefault(m => string.Equals(m, name, StringComparison.OrdinalIgnoreCase));
        if (existing != null)
        {
            MasterBubbles.Remove(existing);
            PassageMasterNames = string.Join(", ", MasterBubbles);
            SyncEditorFieldsToPassage();
            _ = SafeFireAndForget(SaveAsync());
        }
    }

    private void LoadFacetOptions()
    {
        // Hardcoded defaults
        string[] defaultDoctrinalTopics = { "Buddha-nature", "Emptiness", "Dependent origination", "Karma", "Nirvana", "Precepts", "Meditation", "Wisdom", "Compassion", "Mind-only", "Sudden awakening", "Gradual cultivation" };
        string[] defaultLiteraryForms = { "Koan case", "Verse commentary", "Prose commentary", "Encounter dialogue", "Dharma talk", "Transmission record", "Sutra", "Letter", "Preface", "Biography" };
        string[] defaultLineages = { "Linji/Rinzai", "Caodong/Soto", "Yunmen", "Fayan", "Guiyang", "Hongzhou", "Niutou", "Early Chan", "Pre-Chan" };
        string[] defaultRhetoricalFunctions = { "Assertion", "Negation", "Paradox", "Question", "Narrative", "Exhortation", "Pedagogy", "Polemic" };

        try
        {
            var baseDir = AppContext.BaseDirectory;
            var facetsPath = Path.Combine(baseDir, "Assets", "Data", "scholar-facets.json");
            if (File.Exists(facetsPath))
            {
                var json = File.ReadAllText(facetsPath);
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;

                if (root.TryGetProperty("doctrinalTopics", out var dt))
                    defaultDoctrinalTopics = dt.EnumerateArray().Select(e => e.GetString() ?? "").Where(s => s.Length > 0).ToArray();
                if (root.TryGetProperty("literaryForms", out var lf))
                    defaultLiteraryForms = lf.EnumerateArray().Select(e => e.GetString() ?? "").Where(s => s.Length > 0).ToArray();
                if (root.TryGetProperty("lineages", out var ln))
                    defaultLineages = ln.EnumerateArray().Select(e => e.GetString() ?? "").Where(s => s.Length > 0).ToArray();
                if (root.TryGetProperty("rhetoricalFunctions", out var rf))
                    defaultRhetoricalFunctions = rf.EnumerateArray().Select(e => e.GetString() ?? "").Where(s => s.Length > 0).ToArray();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"LoadFacetOptions: failed to load scholar-facets.json, using defaults. {ex.Message}");
            // Fall through to defaults
        }

        foreach (var s in defaultDoctrinalTopics) DoctrinalTopicOptions.Add(s);
        foreach (var s in defaultLiteraryForms) LiteraryFormOptions.Add(s);
        foreach (var s in defaultLineages) LineageOptions.Add(s);
        foreach (var s in defaultRhetoricalFunctions) RhetoricalFunctionOptions.Add(s);
    }

    private static List<string> SplitCommaSeparated(string? input)
    {
        if (string.IsNullOrWhiteSpace(input)) return new List<string>();
        return input.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                    .Where(s => s.Length > 0)
                    .ToList();
    }

    public void Clear()
    {
        _allCollections.Clear();
        Collections.Clear();
        Passages.Clear();
        SelectedCollection = null;
        SelectedPassage = null;
        _allCommunityCollections.Clear();
        CommunityCollections.Clear();
        CommunityPassages.Clear();
        SelectedCommunityCollection = null;
        SelectedCommunityPassage = null;
        HasCommunityCollections = false;
        IsEmptyState = true;
        _root = null;
    }

    /// <summary>
    /// Recomputes IsEmptyState from the current data.
    /// Empty = no local collections (or all empty) AND no community collections.
    /// </summary>
    private void RefreshIsEmptyState()
    {
        bool hasAnyLocal = _allCollections.Count > 0;
        IsEmptyState = !hasAnyLocal && !HasCommunityCollections;
    }

    // ----- Master name auto-detection -----

    /// <summary>
    /// Scans passage ZhText and EnText for known master names and adds any
    /// newly detected names to the passage's MasterNames list (preserving
    /// any names already present, e.g. manually set or adopted from community).
    /// </summary>
    private void AutoTagMasterNames(ScholarPassage passage)
    {
        EnsureMasterDatesLoaded();
        if (_masterEntries == null || _masterEntries.Count == 0) return;

        var detected = DetectMasterNames(passage.ZhText, passage.EnText, _masterEntries);
        foreach (var name in detected)
        {
            if (!passage.MasterNames.Contains(name, StringComparer.OrdinalIgnoreCase))
                passage.MasterNames.Add(name);
        }
    }

    /// <summary>
    /// Detects Zen master names in Chinese and English text by matching against
    /// the master-dates.json entries. Returns canonical display names (pinyin).
    /// </summary>
    internal static List<string> DetectMasterNames(string? zhText, string? enText, List<MasterNameEntry> masterEntries)
    {
        var found = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Chinese name matching — longest match first to avoid partial matches
        if (!string.IsNullOrWhiteSpace(zhText))
        {
            var chineseCandidates = masterEntries
                .SelectMany(e => e.Names
                    .Where(n => n.Length >= 2 && ContainsChinese(n))
                    .Select(n => (Name: n, DisplayName: e.Names[0])))
                .OrderByDescending(c => c.Name.Length)
                .ToList();

            foreach (var (name, display) in chineseCandidates)
            {
                if (zhText.Contains(name, StringComparison.Ordinal))
                    found.Add(display);
            }
        }

        // Pinyin name matching in English text — case-insensitive, min 4 chars
        if (!string.IsNullOrWhiteSpace(enText))
        {
            var pinyinCandidates = masterEntries
                .SelectMany(e => e.Names
                    .Where(n => !ContainsChinese(n) && n.Length >= 4)
                    .Select(n => (Name: n, DisplayName: e.Names[0])))
                .ToList();

            foreach (var (name, display) in pinyinCandidates)
            {
                if (enText.Contains(name, StringComparison.OrdinalIgnoreCase))
                    found.Add(display);
            }
        }

        return found.ToList();
    }

    private static bool ContainsChinese(string s)
    {
        foreach (var c in s)
            if (c >= 0x4E00 && c <= 0x9FFF) return true;
        return false;
    }

    private static async Task RunOnUiAsync(Action action)
    {
        try
        {
            if (Dispatcher.UIThread.CheckAccess())
                action();
            else
                await Dispatcher.UIThread.InvokeAsync(action);
        }
        catch (InvalidOperationException)
        {
            // No UI thread (test context) — run directly
            action();
        }
    }

    private async Task SafeFireAndForget(Task task)
    {
        try
        {
            await task;
        }
        catch (Exception ex)
        {
            StatusMessage = "Error: " + ex.Message;
            StatusChanged?.Invoke(this, StatusMessage);
        }
    }
}

/// <summary>
/// Holds all name variants for a single master (pinyin + Chinese).
/// Names[0] is the canonical pinyin display name.
/// </summary>
internal sealed class MasterNameEntry
{
    public List<string> Names { get; }

    public MasterNameEntry(List<string> names)
    {
        Names = names;
    }
}
