using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CbetaTranslator.App.Models;
using CbetaTranslator.App.Services;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace CbetaTranslator.App.ViewModels;

public partial class ScholarTabViewModel : ViewModelBase
{
    private readonly IScholarCollectionsService _svc;
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

    public static string[] SearchFilterModes { get; } =
        { "All", "Tags", "Masters", "Chinese", "English", "Notes" };

    [ObservableProperty]
    private ScholarCollection? _selectedCollection;

    [ObservableProperty]
    private ScholarPassage? _selectedPassage;

    [ObservableProperty]
    private string _statusMessage = "";

    // Editor fields (bound to detail panel)
    [ObservableProperty]
    private string _passageNotes = "";

    [ObservableProperty]
    private string _passageTags = "";

    [ObservableProperty]
    private string _passageMasterNames = "";

    // Community collections
    [ObservableProperty]
    private ScholarCollection? _selectedCommunityCollection;

    [ObservableProperty]
    private ScholarPassage? _selectedCommunityPassage;

    [ObservableProperty]
    private string _communityFilter = "";

    [ObservableProperty]
    private bool _hasCommunityCollections;

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

    // ----- Bridge delegates (wired by code-behind for file pickers) -----

    public Func<Task<string?>>? PickExportFileAsync { get; set; }
    public Func<Task<string?>>? PickImportFileAsync { get; set; }

    // ----- Events -----

    public event EventHandler<NavigationRequest>? NavigationRequested;
    public event EventHandler<string>? StatusChanged;

    // ----- Constructor -----

    public ScholarTabViewModel(IScholarCollectionsService svc)
    {
        _svc = svc ?? throw new ArgumentNullException(nameof(svc));
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
            var loaded = await _svc.LoadAsync(_root);

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                _allCollections.Clear();
                _allCollections.AddRange(loaded);
                RefreshCollectionsList();

                IsEmptyState = _allCollections.Count == 0 && !HasCommunityCollections;
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

        // Sync editor fields back to selected passage before saving
        SyncEditorFieldsToPassage();

        try
        {
            var list = _allCollections.ToList();
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
    private void DeleteCollection()
    {
        if (SelectedCollection == null) return;
        _allCollections.Remove(SelectedCollection);
        Collections.Remove(SelectedCollection);
        SelectedCollection = Collections.FirstOrDefault();
        IsEmptyState = _allCollections.Count == 0 && !HasCommunityCollections;
        _ = SafeFireAndForget(SaveAsync());
    }

    [RelayCommand]
    private void DeletePassage()
    {
        if (SelectedPassage == null || SelectedCollection == null) return;
        SelectedCollection.Passages.Remove(SelectedPassage);
        Passages.Remove(SelectedPassage);
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
            var path = await PickExportFileAsync();
            if (string.IsNullOrWhiteSpace(path)) return;

            SyncEditorFieldsToPassage();
            var list = _allCollections.ToList();
            await _svc.ExportAsync(path, list);
            StatusMessage = $"Exported {list.Count} collection(s) to {Path.GetFileName(path)}.";
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

            await Dispatcher.UIThread.InvokeAsync(() =>
            {
                RefreshCollectionsList();
                IsEmptyState = _allCollections.Count == 0 && !HasCommunityCollections;
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

            await Dispatcher.UIThread.InvokeAsync(() =>
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
                IsEmptyState = _allCollections.Count == 0 && !HasCommunityCollections;
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

        foreach (var (author, c) in _allCommunityCollections)
        {
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

    // ----- Public API -----

    public async Task AddPassageToCollectionAsync(string collectionId, ScholarPassage passage)
    {
        var collection = Collections.FirstOrDefault(c => c.Id == collectionId);
        if (collection == null) return;

        passage.Id = Guid.NewGuid().ToString("N");
        passage.AddedUtc = DateTimeOffset.UtcNow;
        passage.CreatedBy = _username;
        collection.Passages.Add(passage);

        if (SelectedCollection?.Id == collectionId)
        {
            Passages.Add(passage);
        }

        IsEmptyState = false;
        await SaveAsync();
    }

    // ----- Selection sync -----

    partial void OnSelectedCollectionChanged(ScholarCollection? value)
    {
        RefreshPassagesList();
    }

    partial void OnSearchFilterChanged(string value)
    {
        RefreshPassagesList();
    }

    partial void OnSearchFilterModeChanged(string value)
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
                _ => p.Tags.Any(t => t.Contains(filter, StringComparison.OrdinalIgnoreCase)) ||
                     p.MasterNames.Any(m => m.Contains(filter, StringComparison.OrdinalIgnoreCase)) ||
                     (p.ZhText ?? "").Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                     (p.EnText ?? "").Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                     (p.Notes ?? "").Contains(filter, StringComparison.OrdinalIgnoreCase)
            });
        }

        foreach (var p in passages)
            Passages.Add(p);

        SelectedPassage = Passages.FirstOrDefault();
    }

    partial void OnSelectedPassageChanged(ScholarPassage? value)
    {
        if (value != null)
        {
            PassageNotes = value.Notes ?? "";
            PassageTags = string.Join(", ", value.Tags ?? new List<string>());
            PassageMasterNames = string.Join(", ", value.MasterNames ?? new List<string>());
        }
        else
        {
            PassageNotes = "";
            PassageTags = "";
            PassageMasterNames = "";
        }
    }

    // ----- Helpers -----

    private void SyncEditorFieldsToPassage()
    {
        if (SelectedPassage == null) return;

        SelectedPassage.Notes = PassageNotes ?? "";
        SelectedPassage.Tags = SplitCommaSeparated(PassageTags);
        SelectedPassage.MasterNames = SplitCommaSeparated(PassageMasterNames);
        SelectedPassage.ModifiedUtc = DateTimeOffset.UtcNow;
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
