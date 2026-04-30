using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using ReadZen.App.Models;
using ReadZen.App.Services;
using Microsoft.Extensions.DependencyInjection;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace ReadZen.App.ViewModels;

public partial class ScholarTabViewModel : ViewModelBase
{
    private readonly IScholarCollectionsService _svc;
    private readonly IAppConfigService? _configService;
    private readonly SemaphoreSlim _saveLock = new(1, 1);
    private string? _root;
    private string? _username;
    private string? _legacyUsername;
    private string? _preferredUsername;
    private bool _configLoadAttempted;
    private bool _loadedFromLegacyIdentity;
    private string? _loadedLegacyUsername;

    // ----- Observable properties -----

    [ObservableProperty]
    private bool _isEmptyState = true;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _searchFilter = "";

    [ObservableProperty]
    private string _searchFilterMode = "All";

    [ObservableProperty]
    private string _collectionFilter = "";

    [ObservableProperty]
    private string _sortMode = "Default";

    [ObservableProperty]
    private int _navigatorTabIndex = 1;

    public static string[] SearchFilterModes { get; } =
        { "All", "Tags", "Masters", "Chinese", "English", "Notes", "Topic", "Form", "Lineage", "Function" };

    public static string[] SortModes { get; } =
        { "Default", "A-Z (Chinese)", "Chronological" };

    /// <summary>Index-based accessor for SortMode, suitable for ComboBox.SelectedIndex binding.</summary>
    public int SortModeIndex
    {
        get => Array.IndexOf(SortModes, SortMode) is var i && i >= 0 ? i : 0;
        set
        {
            if (value >= 0 && value < SortModes.Length)
                SortMode = SortModes[value];
        }
    }


    [ObservableProperty]
    private ScholarCollection? _selectedCollection;

    public bool HasSelectedCollection => SelectedCollection != null;
    public bool ShowWorkspaceHelper => SelectedCollection == null || Passages.Count == 0;

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
    private string _passageSummary = "";

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
    public ObservableCollection<CollectionTreeNode> CollectionTreeNodes { get; } = new();

    // Backing list for collection filtering
    private readonly List<ScholarCollection> _allCollections = new();
    private readonly List<(string Author, ScholarCollection Collection)> _allCommunityCollections = new();

    // Master dates for chronological sort
    private Dictionary<string, int>? _masterDatesLookup;
    private bool _masterDatesLoadAttempted;

    // Raw master entries for auto-detection of master names in passages
    private List<MasterNameEntry>? _masterEntries;

    // ----- Bridge delegates (wired by code-behind for file pickers) -----

    public Func<ScholarExportFormat, string?, Task<string?>>? PickExportFileAsync { get; set; }
    public Func<Task<string?>>? PickImportFileAsync { get; set; }
    public Func<Task<ExportDialogResult?>>? PickExportFormatAsync { get; set; }

    // ----- Events -----

    public event EventHandler<NavigationRequest>? NavigationRequested;
    public event EventHandler<string>? StatusChanged;

    // ----- Constructor -----

    public ScholarTabViewModel(IScholarCollectionsService svc, IAppConfigService? configService = null)
    {
        _svc = svc ?? throw new ArgumentNullException(nameof(svc));
        _configService = configService;
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
        _preferredUsername = string.IsNullOrWhiteSpace(username) ? null : username.Trim();
        var changed = ApplyIdentity(_preferredUsername, null);
        if (changed && !string.IsNullOrWhiteSpace(_root))
        {
            _ = SafeFireAndForget(LoadAsync());
            _ = SafeFireAndForget(LoadCommunityAsync());
        }
    }

    public string? GetRoot() => _root;

    public async Task<ScholarCollection> EnsureDefaultCollectionAsync()
    {
        if (!await EnsureStorageContextAsync())
            throw new InvalidOperationException("Scholar storage is not initialized.");

        if (_allCollections.Count > 0)
            return _allCollections[0];

        var collection = CreateCollection();

        _allCollections.Add(collection);
        Collections.Add(collection);
        SelectedCollection = collection;
        IsEmptyState = false;
        await SaveAsync();
        return collection;
    }


    public async Task<ScholarCollection?> EnsureWritableCollectionAsync()
    {
        if (SelectedCollection != null)
            return SelectedCollection;
        if (Collections.Count > 0)
        {
            SelectedCollection = Collections[0];
            return SelectedCollection;
        }
        if (_allCollections.Count > 0)
        {
            SelectedCollection = _allCollections[0];
            if (!Collections.Contains(SelectedCollection))
                Collections.Add(SelectedCollection);
            IsEmptyState = false;
            return SelectedCollection;
        }

        if (!await EnsureStorageContextAsync())
            return null;

        return await EnsureDefaultCollectionAsync();
    }

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
        if (!await EnsureStorageContextAsync()) return;

        IsBusy = true;
        try
        {
            var loaded = await LoadOwnedCollectionsAsync();
            NormalizeOwnedCollections(loaded);

            await RunOnUiAsync(() =>
            {
                _allCollections.Clear();
                _allCollections.AddRange(loaded);
                RefreshCollectionsList();
                RebuildTree();

                RefreshIsEmptyState();
                StatusMessage = _loadedFromLegacyIdentity && !string.IsNullOrWhiteSpace(_loadedLegacyUsername)
                    ? $"Loaded {_allCollections.Count} collection(s) from legacy identity '{_loadedLegacyUsername}'. Next save will write canonical GitHub identity '{_username}'."
                    : $"Loaded {_allCollections.Count} collection(s).";
                StatusChanged?.Invoke(this, StatusMessage);
            });
        }
        catch (Exception ex)
        {
            StatusMessage = "Load failed: " + ex.Message;
            StatusChanged?.Invoke(this, StatusMessage);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        if (!await EnsureStorageContextAsync()) return;

        await _saveLock.WaitAsync();
        try
        {
            // Sync editor fields back to selected passage before saving
            SyncEditorFieldsToPassage();

            try
            {
                var list = _allCollections.ToList();
                NormalizeOwnedCollections(list);
                if (!string.IsNullOrWhiteSpace(_username))
                {
                    if (string.IsNullOrWhiteSpace(_root)) throw new InvalidOperationException("Scholar root is not set.");
                    await _svc.SaveUserAsync(_root, _username, list);
                    if (_loadedFromLegacyIdentity && !string.IsNullOrWhiteSpace(_loadedLegacyUsername))
                    {
                        StatusMessage = $"Saved under GitHub identity '{_username}' using legacy Scholar data from '{_loadedLegacyUsername}'.";
                        _loadedFromLegacyIdentity = false;
                        _loadedLegacyUsername = null;
                    }
                    else
                    {
                        StatusMessage = "Saved.";
                    }
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(_root)) throw new InvalidOperationException("Scholar root is not set.");
                    await _svc.SaveAsync(_root, list);
                    StatusMessage = "Saved.";
                }
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

    private ScholarCollection CreateCollection()
    {
        // Ensure unique name to prevent URL collisions
        var baseName = "New Collection";
        var name = baseName;
        int i = 2;
        while (_allCollections.Any(c => string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase)))
            name = $"{baseName} {i++}";

        return new ScholarCollection
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = name,
            CreatedUtc = DateTimeOffset.UtcNow,
            CreatedBy = _username
        };
    }

    /// <summary>Returns true if the name is already used by another collection.</summary>
    public bool IsCollectionNameTaken(string name, string? excludeId = null)
    {
        return _allCollections.Any(c =>
            string.Equals(c.Name, name, StringComparison.OrdinalIgnoreCase) &&
            c.Id != excludeId);
    }

    [RelayCommand]
    private async Task AddCollectionAsync()
    {
        var c = CreateCollection();
        _allCollections.Add(c);
        Collections.Add(c);
        SelectedCollection = c;
        IsEmptyState = false;
        RebuildTree();
        await SaveAsync();
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

    /// <summary>Removes a collection by ID without confirmation. Used for tour sample cleanup.</summary>
    public async Task RemoveCollectionAsync(string collectionId)
    {
        var c = _allCollections.FirstOrDefault(x => x.Id == collectionId);
        if (c == null) return;
        _allCollections.Remove(c);
        Collections.Remove(c);
        if (SelectedCollection?.Id == collectionId)
            SelectedCollection = Collections.FirstOrDefault();
        RefreshIsEmptyState();
        await SaveAsync();
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
    private async Task MovePassageUp()
    {
        var passage = SelectedPassage;
        var col = SelectedCollection;
        if (passage == null || col == null) return;
        var idx = col.Passages.IndexOf(passage);
        if (idx <= 0) return;
        col.Passages.RemoveAt(idx);
        col.Passages.Insert(idx - 1, passage);
        RefreshPassagesList();
        SelectedPassage = passage;
        RebuildTree();
        await SaveAsync();
    }

    [RelayCommand]
    private async Task MovePassageDown()
    {
        var passage = SelectedPassage;
        var col = SelectedCollection;
        if (passage == null || col == null) return;
        var idx = col.Passages.IndexOf(passage);
        if (idx < 0 || idx >= col.Passages.Count - 1) return;
        col.Passages.RemoveAt(idx);
        col.Passages.Insert(idx + 1, passage);
        RefreshPassagesList();
        SelectedPassage = passage;
        RebuildTree();
        await SaveAsync();
    }

    /// <summary>
    /// Moves a passage to a specific index within the selected collection.
    /// Used by drag-and-drop reorder in the tree view.
    /// </summary>
    public async Task MovePassageToIndexAsync(ScholarPassage passage, int targetIndex)
    {
        var col = SelectedCollection;
        if (col == null) return;
        var currentIndex = col.Passages.IndexOf(passage);
        if (currentIndex < 0 || currentIndex == targetIndex) return;
        col.Passages.RemoveAt(currentIndex);
        if (targetIndex > currentIndex) targetIndex--;
        col.Passages.Insert(Math.Clamp(targetIndex, 0, col.Passages.Count), passage);
        RefreshPassagesList();
        SelectedPassage = passage;
        RebuildTree();
        await SaveAsync();
    }

    [RelayCommand]
    private void NavigateToPassage()
    {
        if (SelectedPassage == null) return;
        var side = SelectedPassage.PreferredSide;
        var matchText = side == SearchSide.Translated ? SelectedPassage.EnText : SelectedPassage.ZhText;
        if (matchText.Length > 80)
            matchText = matchText[..80];

        NavigationRequested?.Invoke(this, new NavigationRequest
        {
            RelPath = SelectedPassage.SourceRelPath,
            Side = side,
            User = side == SearchSide.Translated ? SelectedPassage.TranslationUser : null,
            MatchText = matchText,
            FromLb = SelectedPassage.FromLb,
            ToLb = SelectedPassage.ToLb,
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
            var chosenFormat = ScholarExportFormat.Json;
            var chosenStyle = CitationStyle.Chicago;
            if (PickExportFormatAsync != null)
            {
                var dialogResult = await PickExportFormatAsync();
                if (dialogResult == null)
                {
                    StatusMessage = "Export cancelled.";
                    StatusChanged?.Invoke(this, StatusMessage);
                    return;
                }

                chosenFormat = dialogResult.Format;
                chosenStyle = dialogResult.CitationStyle;
            }

            ScholarCollection? target = null;
            if (chosenFormat != ScholarExportFormat.Json)
            {
                target = SelectedCollection;
                if (target == null)
                {
                    StatusMessage = "Select a collection to export.";
                    StatusChanged?.Invoke(this, StatusMessage);
                    return;
                }
            }

            var suggestedName = chosenFormat == ScholarExportFormat.Json ? null : target?.Name;
            var path = await PickExportFileAsync(chosenFormat, suggestedName);
            if (string.IsNullOrWhiteSpace(path))
            {
                StatusMessage = "Export cancelled.";
                StatusChanged?.Invoke(this, StatusMessage);
                return;
            }

            SyncEditorFieldsToPassage();

            if (chosenFormat == ScholarExportFormat.Json)
            {
                var list = _allCollections.ToList();
                await _svc.ExportAsync(path, list);
                StatusMessage = $"Exported {list.Count} collection(s) to {Path.GetFileName(path)}.";
            }
            else
            {
                var exportSvc = App.Services.GetRequiredService<IScholarExportService>();
                await exportSvc.ExportAsync(path, target!, chosenFormat, chosenStyle);
                StatusMessage = $"Exported '{target!.Name}' as {chosenFormat} to {Path.GetFileName(path)}.";
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

        IsBusy = true;
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
        finally
        {
            IsBusy = false;
        }
    }

    // ----- Community collections -----

    [RelayCommand]
    private async Task LoadCommunityAsync()
    {
        if (!await EnsureStorageContextAsync()) return;

        IsBusy = true;
        try
        {
            if (string.IsNullOrWhiteSpace(_root)) return;
            var communityDir = ScholarCollectionsService.GetCommunityCollectionsDir(_root);
            var allUsers = await _svc.LoadAllCommunityJsonlAsync(communityDir);
            var identityKeys = GetCurrentIdentityKeys();

            await RunOnUiAsync(() =>
            {
                _allCommunityCollections.Clear();

                foreach (var (username, collections) in allUsers)
                {
                    // Skip current user's own collections
                    if (identityKeys.Contains(username))
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
        finally
        {
            IsBusy = false;
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

        string? selectedUser = SelectedCommunityUserIndex > 0 && SelectedCommunityUserIndex < _communityUsernames.Count
            ? _communityUsernames[SelectedCommunityUserIndex]
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
            await EnsureWritableCollectionAsync();
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

    public async Task CreateLinkAsync(string fromId, string toId, string relationType, string? note = null)
    {
        if (SelectedCollection == null) return;

        var link = new PassageLink
        {
            Id = Guid.NewGuid().ToString("N"),
            FromPassageId = fromId,
            ToPassageId = toId,
            RelationType = relationType,
            Note = note,
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

    public void SelectPassageById(string passageId)
    {
        if (SelectedCollection == null) return;
        var passage = SelectedCollection.Passages.FirstOrDefault(p => p.Id == passageId);
        if (passage != null)
            SelectedPassage = passage;
    }

    /// <summary>
    /// Navigates to a specific collection (and optionally passage) by ID.
    /// Used by deep link routing. Returns false if the collection was not found.
    /// </summary>
    public async Task<bool> TryNavigateToPassageAsync(string collectionId, string? passageId)
    {
        return await TryNavigateToPassageAsync(collectionId, passageId, ownerUser: null);
    }

    /// <summary>
    /// Navigates to a specific local or shared collection (and optionally passage) by ID and owner.
    /// When ownerUser matches the current identity, local collections are searched; otherwise shared collections are searched.
    /// Returns false if the requested collection was not found.
    /// </summary>
    public async Task<bool> TryNavigateToPassageAsync(string collectionId, string? passageId, string? ownerUser)
    {
        // Ensure collections are loaded
        if (_allCollections.Count == 0 && !string.IsNullOrWhiteSpace(_root))
            await LoadAsync();

        if (ShouldResolveOwnedCollection(ownerUser))
            return TrySelectOwnedCollection(collectionId, passageId);

        if (!string.IsNullOrWhiteSpace(ownerUser))
        {
            if (_allCommunityCollections.Count == 0 && !string.IsNullOrWhiteSpace(_root))
                await LoadCommunityAsync();

            return TrySelectCommunityCollection(ownerUser, collectionId, passageId);
        }

        return TrySelectOwnedCollection(collectionId, passageId);
    }

    public string? GetCurrentUsername() => _username;

    // ----- Public API -----

    public async Task AddPassageToCollectionAsync(string collectionId, ScholarPassage passage)
    {
        if (!await EnsureStorageContextAsync())
            return;

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
        RefreshPassagesList();
        RebuildTree();
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
        OnPropertyChanged(nameof(HasSelectedCollection));
        OnPropertyChanged(nameof(ShowWorkspaceHelper));
        RefreshPassagesList();
        RebuildTree();
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
        OnPropertyChanged(nameof(SortModeIndex));
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

        // Restore previous selection if still visible, otherwise prefer user-owned collection
        if (prev != null && Collections.Contains(prev))
        {
            SelectedCollection = prev;
        }
        else
        {
            var owned = Collections.FirstOrDefault(c =>
                string.IsNullOrWhiteSpace(c.CreatedBy) ||
                GetCurrentIdentityKeys().Contains(c.CreatedBy?.Trim() ?? ""));
            SelectedCollection = owned ?? Collections.FirstOrDefault();
        }

        OnPropertyChanged(nameof(HasSelectedCollection));
        OnPropertyChanged(nameof(ShowWorkspaceHelper));
    }

    public void RebuildTree()
    {
        CollectionTreeNodes.Clear();
        foreach (var collection in _allCollections)
        {
            var cNode = new CollectionTreeNode
            {
                Id = collection.Id,
                Title = collection.Name ?? "Untitled",
                Kind = TreeNodeKind.Collection,
                ItemCount = collection.Passages.Count,
                IsExpanded = collection == SelectedCollection,
                Tag = collection
            };
            foreach (var passage in collection.Passages)
            {
                cNode.Children.Add(new CollectionTreeNode
                {
                    Id = passage.Id,
                    Title = passage.DisplayTitle,
                    Kind = TreeNodeKind.Passage,
                    Tag = passage,
                    Importance = passage.Importance ?? 0,
                    ReadingStatus = passage.ReadingStatus
                });
            }
            CollectionTreeNodes.Add(cNode);
        }
    }

    /// <summary>Search all collections for passages matching a query string.</summary>
    public List<(ScholarCollection Collection, ScholarPassage Passage)> SearchAllCollections(string query)
    {
        var results = new List<(ScholarCollection, ScholarPassage)>();
        if (string.IsNullOrWhiteSpace(query)) return results;
        var q = query.Trim();
        foreach (var c in _allCollections)
        {
            foreach (var p in c.Passages)
            {
                if ((p.ZhText ?? "").Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    (p.EnText ?? "").Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    (p.Summary ?? "").Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    (p.Notes ?? "").Contains(q, StringComparison.OrdinalIgnoreCase) ||
                    p.Tags.Any(t => t.Contains(q, StringComparison.OrdinalIgnoreCase)) ||
                    p.MasterNames.Any(m => m.Contains(q, StringComparison.OrdinalIgnoreCase)))
                {
                    results.Add((c, p));
                }
            }
        }
        return results;
    }

    /// <summary>Navigate to a specific passage in a specific collection.</summary>
    public void NavigateToPassageInCollection(ScholarCollection collection, ScholarPassage passage)
    {
        SelectedCollection = collection;
        SelectedPassage = passage;
    }

    public void RefreshPassagesList()
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
        OnPropertyChanged(nameof(ShowWorkspaceHelper));
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
            PassageSummary = value.Summary ?? "";
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
            PassageSummary = "";
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

        SelectedPassage.Summary = string.IsNullOrWhiteSpace(PassageSummary) ? null : PassageSummary.Trim();
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

    /// <summary>Syncs editor fields to the passage and saves. Called by code-behind after field edits.</summary>
    public void SyncAndSave()
    {
        SyncEditorFieldsToPassage();
        _ = SafeFireAndForget(SaveAsync());
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
        string[] defaultDoctrinalTopics = { "Buddha-nature", "Emptiness", "Precepts", "Wisdom", "Compassion", "One mind", "Sudden awakening" };
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
        NavigatorTabIndex = 0;
        _root = null;
        _username = null;
        _legacyUsername = null;
        _preferredUsername = null;
        _configLoadAttempted = false;
        _loadedFromLegacyIdentity = false;
        _loadedLegacyUsername = null;
    }

    /// <summary>
    /// Recomputes IsEmptyState from the current data.
    /// Empty = no local collections (or all empty) AND no community collections.
    /// </summary>
    private void RefreshIsEmptyState()
    {
        bool hasAnyLocal = _allCollections.Count > 0;
        IsEmptyState = !hasAnyLocal && !HasCommunityCollections;
        NavigatorTabIndex = !hasAnyLocal && HasCommunityCollections ? 1 : 0;
    }


    public async Task<bool> EnsureStorageContextAsync()
    {
        await EnsureConfigInitializedAsync();

        if (!string.IsNullOrWhiteSpace(_root))
            return true;

        StatusMessage = "Scholar save unavailable: no text root is configured.";
        StatusChanged?.Invoke(this, StatusMessage);
        return false;
    }

    private async Task EnsureConfigInitializedAsync()
    {
        if (_configLoadAttempted || _configService == null)
            return;

        _configLoadAttempted = true;

        try
        {
            var cfg = await _configService.TryLoadAsync();
            if (cfg == null)
                return;

            if (string.IsNullOrWhiteSpace(_root) && !string.IsNullOrWhiteSpace(cfg.TextRootPath))
            {
                // Scholar needs the translation repo root (community/ lives there), not the parent folder.
                var transRoot = Infrastructure.AppPaths.GetTranslationRepoRoot(cfg.TextRootPath.Trim());
                _root = transRoot ?? cfg.TextRootPath.Trim();
            }

            ApplyIdentity(
                string.IsNullOrWhiteSpace(_preferredUsername) ? cfg.Username : _preferredUsername,
                cfg.GitHubUsername);
        }
        catch
        {
            // Config fallback is optional; explicit SetRoot/SetUsername can still initialize the VM.
        }
    }

    private bool ApplyIdentity(string? username, string? githubUsername)
    {
        string? preferred = string.IsNullOrWhiteSpace(username) ? null : username.Trim();
        string? github = string.IsNullOrWhiteSpace(githubUsername) ? null : githubUsername.Trim();

        var nextUsername = !string.IsNullOrWhiteSpace(github) ? github : preferred;
        var nextLegacy = !string.IsNullOrWhiteSpace(github) &&
                         !string.IsNullOrWhiteSpace(preferred) &&
                         !string.Equals(github, preferred, StringComparison.OrdinalIgnoreCase)
            ? preferred
            : null;

        bool changed =
            !string.Equals(_username, nextUsername, StringComparison.Ordinal) ||
            !string.Equals(_legacyUsername, nextLegacy, StringComparison.Ordinal);

        _username = nextUsername;
        _legacyUsername = nextLegacy;
        return changed;
    }

    private async Task<List<ScholarCollection>> LoadOwnedCollectionsAsync()
    {
        _loadedFromLegacyIdentity = false;
        _loadedLegacyUsername = null;

        if (string.IsNullOrWhiteSpace(_root))
            return new List<ScholarCollection>();

        if (string.IsNullOrWhiteSpace(_username))
            return await _svc.LoadAsync(_root);

        if (!string.IsNullOrWhiteSpace(_legacyUsername))
        {
            var canonicalPath = ScholarCollectionsService.GetUserPath(_root, _username);
            var legacyPath = ScholarCollectionsService.GetUserPath(_root, _legacyUsername);

            if (!File.Exists(canonicalPath) && File.Exists(legacyPath))
            {
                _loadedFromLegacyIdentity = true;
                _loadedLegacyUsername = _legacyUsername;
                return await _svc.LoadUserAsync(_root, _legacyUsername);
            }
        }

        return await _svc.LoadUserAsync(_root, _username);
    }

    private HashSet<string> GetCurrentIdentityKeys()
    {
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!string.IsNullOrWhiteSpace(_username))
            keys.Add(_username);
        if (!string.IsNullOrWhiteSpace(_legacyUsername))
            keys.Add(_legacyUsername);
        return keys;
    }

    private bool ShouldResolveOwnedCollection(string? ownerUser)
    {
        if (string.IsNullOrWhiteSpace(ownerUser))
            return true;

        return GetCurrentIdentityKeys().Contains(ownerUser.Trim());
    }

    private bool TrySelectOwnedCollection(string collectionId, string? passageId)
    {
        var collection = _allCollections.FirstOrDefault(c => c.Id == collectionId);
        if (collection == null)
            return false;

        NavigatorTabIndex = 0;
        SelectedCollection = collection;

        if (string.IsNullOrWhiteSpace(passageId))
            return true;

        SelectPassageById(passageId);
        return SelectedPassage?.Id == passageId;
    }

    private bool TrySelectCommunityCollection(string ownerUser, string collectionId, string? passageId)
    {
        var communityEntry = _allCommunityCollections.FirstOrDefault(x =>
            string.Equals(x.Author, ownerUser, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(x.Collection.Id, collectionId, StringComparison.Ordinal));
        if (communityEntry.Collection == null)
            return false;

        NavigatorTabIndex = 1;

        int userIndex = _communityUsernames.FindIndex(u => string.Equals(u, communityEntry.Author, StringComparison.OrdinalIgnoreCase));
        if (userIndex >= 0)
            SelectedCommunityUserIndex = userIndex;

        SelectedCommunityCollection = communityEntry.Collection;

        if (string.IsNullOrWhiteSpace(passageId))
            return true;

        SelectedCommunityPassage = SelectedCommunityCollection?.Passages.FirstOrDefault(p => p.Id == passageId);
        return SelectedCommunityPassage?.Id == passageId;
    }

    private void NormalizeOwnedCollections(IEnumerable<ScholarCollection> collections)
    {
        if (collections == null || string.IsNullOrWhiteSpace(_username))
            return;

        var identityKeys = GetCurrentIdentityKeys();
        foreach (var collection in collections)
        {
            if (string.IsNullOrWhiteSpace(collection.CreatedBy) || identityKeys.Contains(collection.CreatedBy))
                collection.CreatedBy = _username;

            foreach (var passage in collection.Passages)
            {
                if (string.IsNullOrWhiteSpace(passage.CreatedBy) || identityKeys.Contains(passage.CreatedBy))
                    passage.CreatedBy = _username;
            }
        }
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

        // Chinese name matching - longest match first to avoid partial matches
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

        // Pinyin name matching in English text - case-insensitive, min 4 chars
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
            // No UI thread (test context) - run directly
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




