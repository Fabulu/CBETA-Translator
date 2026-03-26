using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

    // ----- Observable properties -----

    [ObservableProperty]
    private bool _isEmptyState = true;

    [ObservableProperty]
    private string _searchFilter = "";

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

    // ----- Collections -----

    public ObservableCollection<ScholarCollection> Collections { get; } = new();
    public ObservableCollection<ScholarPassage> Passages { get; } = new();

    // ----- Events -----

    public event EventHandler<NavigationRequest>? NavigationRequested;
    public event EventHandler<string>? StatusChanged;

    // ----- Constructor -----

    public ScholarTabViewModel(IScholarCollectionsService svc)
    {
        _svc = svc ?? throw new ArgumentNullException(nameof(svc));
    }

    // ----- Public wiring -----

    public void SetRoot(string root)
    {
        _root = root;
        _ = SafeFireAndForget(LoadAsync());
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
                Collections.Clear();
                foreach (var c in loaded)
                    Collections.Add(c);

                IsEmptyState = Collections.Count == 0;
                StatusMessage = $"Loaded {Collections.Count} collection(s).";
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
            var list = Collections.ToList();
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
            CreatedUtc = DateTimeOffset.UtcNow
        };
        Collections.Add(c);
        SelectedCollection = c;
        IsEmptyState = false;
        _ = SafeFireAndForget(SaveAsync());
    }

    [RelayCommand]
    private void DeleteCollection()
    {
        if (SelectedCollection == null) return;
        Collections.Remove(SelectedCollection);
        SelectedCollection = Collections.FirstOrDefault();
        IsEmptyState = Collections.Count == 0;
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
            MatchText = SelectedPassage.ZhText.Length > 20
                ? SelectedPassage.ZhText[..20]
                : SelectedPassage.ZhText
        });
    }

    // ----- Public API -----

    public async Task AddPassageToCollectionAsync(string collectionId, ScholarPassage passage)
    {
        var collection = Collections.FirstOrDefault(c => c.Id == collectionId);
        if (collection == null) return;

        passage.Id = Guid.NewGuid().ToString("N");
        passage.AddedUtc = DateTimeOffset.UtcNow;
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
        Passages.Clear();
        if (value != null)
        {
            foreach (var p in value.Passages)
                Passages.Add(p);
        }
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
        Collections.Clear();
        Passages.Clear();
        SelectedCollection = null;
        SelectedPassage = null;
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
