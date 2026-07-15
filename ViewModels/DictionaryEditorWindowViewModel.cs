using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ReadZen.App.Models;
using ReadZen.App.Services;

namespace ReadZen.App.ViewModels;

/// <summary>
/// ViewModel for the rich Zen-dictionary editor (schema v2). Edits a list of
/// <see cref="DictionaryEntry"/> articles, each carrying one or more senses (a corpus-wide
/// Zen sense plus optional master-specific senses). Each sense pulls Zen-scoped occurrence
/// evidence from <see cref="IDictionaryEvidenceService"/>, which the lexicographer curates
/// down to the defining occurrences persisted on the sense. The legacy flat editor
/// (<c>TermbaseEditorWindowViewModel</c>) is untouched; this is its rich sibling.
/// </summary>
public partial class DictionaryEditorWindowViewModel : ViewModelBase
{
    private readonly IDictionaryStore _store;
    private readonly string _root;
    private string? _username;

    // Evidence context (best-effort — set via SetContext).
    private IDictionaryEvidenceService? _evidence;
    private string? _originalDir;
    private string? _translatedDir;
    private string? _masterCacheDir;
    private readonly Dictionary<string, DictionaryEvidence> _evidenceCache = new(StringComparer.Ordinal);
    private CancellationTokenSource? _evidenceCts;

    private bool _suppressSourceTermSync;

    [ObservableProperty]
    private string _searchQuery = "";

    [ObservableProperty]
    private DictionaryEntry? _selectedEntry;

    [ObservableProperty]
    private SenseEditViewModel? _selectedSense;

    [ObservableProperty]
    private string _sourceTerm = "";

    [ObservableProperty]
    private string _statusMessage = "";

    [ObservableProperty]
    private DictionaryEvidence? _currentEvidence;

    [ObservableProperty]
    private bool _isLoadingEvidence;

    [ObservableProperty]
    private string _evidenceStatus = "";

    public ObservableCollection<DictionaryEntry> Entries { get; } = new();
    public ObservableCollection<DictionaryEntry> FilteredEntries { get; } = new();
    public ObservableCollection<SenseEditViewModel> Senses { get; } = new();

    public bool Saved { get; private set; }

    /// <summary>Fired after a successful save. The host refreshes any dependent panels.</summary>
    public event EventHandler? TermsSaved;

    /// <summary>Wired by code-behind to close the window.</summary>
    public Action? CloseRequested { get; set; }

    /// <summary>Wired by code-behind to focus the source-term field after New.</summary>
    public Action? FocusSourceTermRequested { get; set; }

    /// <summary>Raised when the user opens an occurrence; the host navigates the reader to it.</summary>
    public event EventHandler<NavigationRequest>? OccurrenceNavigationRequested;

    public DictionaryEditorWindowViewModel(IDictionaryStore store, string root)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _root = root ?? throw new ArgumentNullException(nameof(root));
    }

    /// <summary>Provide evidence context so the occurrence curator can query the Zen corpus.</summary>
    public void SetContext(IDictionaryEvidenceService evidence, string originalDir, string translatedDir, string? masterCacheDir)
    {
        _evidence = evidence;
        _originalDir = originalDir;
        _translatedDir = translatedDir;
        _masterCacheDir = string.IsNullOrWhiteSpace(masterCacheDir) ? null : masterCacheDir;
    }

    public void SetUsername(string? username)
    {
        _username = string.IsNullOrWhiteSpace(username) ? null : username.Trim();
    }

    // ----- Property-change hooks -----

    partial void OnSearchQueryChanged(string value) => ApplyFilter();

    partial void OnSelectedEntryChanged(DictionaryEntry? value) => LoadEntryIntoFields(value);

    partial void OnSelectedSenseChanged(SenseEditViewModel? value) => _ = LoadEvidenceAsync();

    partial void OnSourceTermChanged(string value)
    {
        if (_suppressSourceTermSync || SelectedEntry == null)
            return;

        SelectedEntry.SourceTerm = value?.Trim() ?? "";
        // Id is deterministic from the head term (the merge key): keep it canonical as the term is edited.
        SelectedEntry.Id = DictionaryStore.ComputeId(SelectedEntry.SourceTerm);
    }

    // ----- Commands -----

    [RelayCommand]
    private async Task LoadAsync()
    {
        try
        {
            var file = await _store.LoadAsync(_root);

            Entries.Clear();
            foreach (var e in file.Entries)
                Entries.Add(e);

            ApplyFilter();
            SelectedEntry = FilteredEntries.FirstOrDefault();

            StatusMessage = $"Loaded {Entries.Count:n0} entr{(Entries.Count == 1 ? "y" : "ies")}.";
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
            FlushCurrentEntry();

            var bad = Entries.FirstOrDefault(e => string.IsNullOrWhiteSpace(e.SourceTerm));
            if (bad != null)
            {
                StatusMessage = "Save blocked: every entry needs a Chinese head term.";
                return;
            }

            var file = new DictionaryFile
            {
                SchemaVersion = DictionaryStore.CurrentSchemaVersion,
                Entries = Entries.ToList(),
            };

            await _store.SaveAsync(_root, file);

            Saved = true;
            StatusMessage = $"Saved {Entries.Count:n0} entr{(Entries.Count == 1 ? "y" : "ies")}.";
            TermsSaved?.Invoke(this, EventArgs.Empty);
        }
        catch (Exception ex)
        {
            StatusMessage = "Save failed: " + ex.Message;
        }
    }

    [RelayCommand]
    private void NewEntry()
    {
        const string term = "New Term";
        var entry = new DictionaryEntry
        {
            Id = DictionaryStore.ComputeId(term),
            SourceTerm = term,
            CreatedBy = _username,
            WrittenUtc = DateTimeOffset.UtcNow,
            Senses = new List<DictionarySense>
            {
                new() { SenseKey = null, PreferredTarget = "", Status = "preferred", Validation = "provisional" }
            }
        };

        Entries.Add(entry);
        ApplyFilter();
        SelectedEntry = entry;
        FocusSourceTermRequested?.Invoke();
        StatusMessage = "New entry created.";
    }

    [RelayCommand]
    private void DeleteEntry()
    {
        if (SelectedEntry == null)
            return;

        var entry = SelectedEntry;
        SelectedEntry = null;
        Entries.Remove(entry);
        ApplyFilter();
        SelectedEntry = FilteredEntries.FirstOrDefault();
        StatusMessage = "Entry deleted.";
    }

    [RelayCommand]
    private void DuplicateEntry()
    {
        if (SelectedEntry == null)
            return;

        var src = SelectedEntry;
        var copyTerm = src.SourceTerm ?? "";
        var copy = new DictionaryEntry
        {
            Id = DictionaryStore.ComputeId(copyTerm),
            SourceTerm = copyTerm,
            CreatedBy = _username,
            WrittenUtc = DateTimeOffset.UtcNow,
            Senses = (src.Senses ?? new List<DictionarySense>()).Select(CloneSense).ToList()
        };
        if (copy.Senses.Count == 0)
            copy.Senses.Add(new DictionarySense { SenseKey = null, Status = "preferred", Validation = "provisional" });

        Entries.Add(copy);
        ApplyFilter();
        SelectedEntry = copy;
        StatusMessage = "Entry duplicated.";
    }

    [RelayCommand]
    private void AddSense()
    {
        if (SelectedEntry == null)
            return;

        var sense = new DictionarySense { SenseKey = null, PreferredTarget = "", Status = "preferred", Validation = "provisional" };
        SelectedEntry.Senses ??= new List<DictionarySense>();
        SelectedEntry.Senses.Add(sense);

        var svm = new SenseEditViewModel(sense);
        Senses.Add(svm);
        SelectedSense = svm;
        StatusMessage = "Sense added.";
    }

    [RelayCommand]
    private void RemoveSense()
    {
        if (SelectedEntry == null || SelectedSense == null)
            return;

        SelectedEntry.Senses?.Remove(SelectedSense.Model);
        Senses.Remove(SelectedSense);
        SelectedSense = Senses.FirstOrDefault();
        StatusMessage = "Sense removed.";
    }

    [RelayCommand]
    private void CurateOccurrence(DictOccurrence? occ)
    {
        if (SelectedSense == null || occ == null || string.IsNullOrWhiteSpace(occ.RelPath))
            return;

        bool exists = SelectedSense.CuratedOccurrences.Any(o =>
            string.Equals(o.RelPath, occ.RelPath, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(o.Kwic, occ.Kwic, StringComparison.Ordinal));
        if (exists)
            return;

        var curated = CloneOccurrence(occ);
        curated.Curated = true;
        SelectedSense.CuratedOccurrences.Add(curated);
        SelectedSense.SyncOccurrencesToModel();
        StatusMessage = "Occurrence curated.";
    }

    [RelayCommand]
    private void UncurateOccurrence(DictOccurrence? occ)
    {
        if (SelectedSense == null || occ == null)
            return;

        var match = SelectedSense.CuratedOccurrences.FirstOrDefault(o =>
            ReferenceEquals(o, occ) ||
            (string.Equals(o.RelPath, occ.RelPath, StringComparison.OrdinalIgnoreCase) &&
             string.Equals(o.Kwic, occ.Kwic, StringComparison.Ordinal)));
        if (match == null)
            return;

        SelectedSense.CuratedOccurrences.Remove(match);
        SelectedSense.SyncOccurrencesToModel();
        StatusMessage = "Occurrence removed from sense.";
    }

    [RelayCommand]
    private void OpenOccurrence(DictOccurrence? occ)
    {
        if (occ == null || string.IsNullOrWhiteSpace(occ.RelPath))
            return;

        OccurrenceNavigationRequested?.Invoke(this, new NavigationRequest
        {
            RelPath = occ.RelPath,
            FromLb = occ.FromLb,
            ToLb = occ.ToLb,
            Side = SearchSide.Original,
            MatchText = SelectedEntry?.SourceTerm ?? occ.Kwic,
        });
    }

    [RelayCommand]
    private async Task LoadEvidenceAsync()
    {
        // Cancel any in-flight query.
        _evidenceCts?.Cancel();
        _evidenceCts = null;

        var term = SelectedEntry?.SourceTerm?.Trim();
        if (string.IsNullOrWhiteSpace(term))
        {
            CurrentEvidence = null;
            EvidenceStatus = "";
            return;
        }

        if (_evidence == null || string.IsNullOrWhiteSpace(_originalDir) || string.IsNullOrWhiteSpace(_translatedDir))
        {
            CurrentEvidence = null;
            EvidenceStatus = "Evidence needs a built search index.";
            return;
        }

        var restrict = SelectedSense?.Model.SourceTexts;
        var hasScope = restrict != null && restrict.Count > 0;
        var scopeKey = hasScope ? string.Join(",", restrict!) : "";
        var cacheKey = term + "|" + scopeKey;

        if (_evidenceCache.TryGetValue(cacheKey, out var cached))
        {
            CurrentEvidence = cached;
            EvidenceStatus = Summarize(cached);
            return;
        }

        var cts = new CancellationTokenSource();
        _evidenceCts = cts;
        IsLoadingEvidence = true;
        EvidenceStatus = $"Loading evidence for \"{term}\"...";

        try
        {
            var ev = await _evidence.GetEvidenceAsync(
                term, _originalDir!, _translatedDir!, _masterCacheDir,
                restrictToRelPaths: hasScope ? restrict : null,
                ct: cts.Token);

            if (cts.Token.IsCancellationRequested)
                return;

            _evidenceCache[cacheKey] = ev;
            CurrentEvidence = ev;
            EvidenceStatus = Summarize(ev);
        }
        catch (OperationCanceledException)
        {
            // Superseded by a newer request.
        }
        catch (Exception ex)
        {
            EvidenceStatus = "Evidence error: " + ex.Message;
        }
        finally
        {
            IsLoadingEvidence = false;
        }
    }

    [RelayCommand]
    private void CloseWindow() => CloseRequested?.Invoke();

    // ----- Helpers -----

    private static string Summarize(DictionaryEvidence ev)
    {
        var s = $"{ev.ZenTextCount:n0} Zen text{(ev.ZenTextCount == 1 ? "" : "s")} · {ev.Masters.Count:n0} master{(ev.Masters.Count == 1 ? "" : "s")} · {ev.TotalHitCount:n0} hit{(ev.TotalHitCount == 1 ? "" : "s")}";
        return ev.Truncated ? s + " (truncated)" : s;
    }

    /// <summary>Push pending field edits into the currently-selected entry's models before saving.</summary>
    private void FlushCurrentEntry()
    {
        if (SelectedEntry != null)
        {
            SelectedEntry.SourceTerm = SourceTerm?.Trim() ?? "";
            SelectedEntry.Id = DictionaryStore.ComputeId(SelectedEntry.SourceTerm);
        }

        foreach (var svm in Senses)
        {
            svm.Push();
            svm.SyncOccurrencesToModel();
        }
    }

    private void LoadEntryIntoFields(DictionaryEntry? entry)
    {
        _suppressSourceTermSync = true;
        try
        {
            SourceTerm = entry?.SourceTerm ?? "";

            Senses.Clear();
            if (entry != null)
            {
                entry.Senses ??= new List<DictionarySense>();
                foreach (var s in entry.Senses)
                    Senses.Add(new SenseEditViewModel(s));
            }

            SelectedSense = Senses.FirstOrDefault();
        }
        finally
        {
            _suppressSourceTermSync = false;
        }

        // SelectedSense change triggers evidence load; force it when the entry has no senses.
        if (SelectedSense == null)
            _ = LoadEvidenceAsync();
    }

    private void ApplyFilter()
    {
        var q = (SearchQuery ?? "").Trim();

        IEnumerable<DictionaryEntry> seq = Entries;
        if (q.Length > 0)
        {
            seq = seq.Where(e =>
                (e.SourceTerm?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                (e.Senses?.Any(s =>
                    (s.PreferredTarget?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (s.Explanation?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (s.Note?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false) ||
                    (s.AlternateTargets?.Any(a => a.Contains(q, StringComparison.OrdinalIgnoreCase)) ?? false) ||
                    (s.SearchAliases?.Any(a => a.Contains(q, StringComparison.OrdinalIgnoreCase)) ?? false)) ?? false));
        }

        var filtered = seq
            .OrderBy(e => e.SourceTerm ?? "", StringComparer.OrdinalIgnoreCase)
            .ToList();

        var prev = SelectedEntry;

        FilteredEntries.Clear();
        foreach (var e in filtered)
            FilteredEntries.Add(e);

        if (prev != null && FilteredEntries.Contains(prev))
            SelectedEntry = prev;
        else if (FilteredEntries.Count > 0)
            SelectedEntry = FilteredEntries[0];
        else
            SelectedEntry = null;
    }

    private static DictionarySense CloneSense(DictionarySense s) => new()
    {
        SenseKey = s.SenseKey,
        MasterName = s.MasterName,
        PreferredTarget = s.PreferredTarget ?? "",
        AlternateTargets = (s.AlternateTargets ?? new List<string>()).ToList(),
        SearchAliases = (s.SearchAliases ?? new List<string>()).ToList(),
        Status = s.Status ?? "preferred",
        Explanation = s.Explanation,
        Validation = s.Validation ?? "provisional",
        Note = s.Note ?? "",
        Occurrences = (s.Occurrences ?? new List<DictOccurrence>()).Select(CloneOccurrence).ToList(),
        SourceTexts = (s.SourceTexts ?? new List<string>()).ToList(),
        RelatedMasters = (s.RelatedMasters ?? new List<string>()).ToList(),
        RelatedTerms = (s.RelatedTerms ?? new List<string>()).ToList(),
    };

    private static DictOccurrence CloneOccurrence(DictOccurrence o) => new()
    {
        RelPath = o.RelPath,
        FromLb = o.FromLb,
        ToLb = o.ToLb,
        CharOffset = o.CharOffset,
        Kwic = o.Kwic,
        MasterName = o.MasterName,
        ActorAttribution = o.ActorAttribution == null ? null : new DictActorAttribution
        {
            Status = o.ActorAttribution.Status,
            Kind = o.ActorAttribution.Kind,
            ActorLabel = o.ActorAttribution.ActorLabel,
            ActorRole = o.ActorAttribution.ActorRole,
            RungsChecked = (o.ActorAttribution.RungsChecked ?? new List<string>()).ToList(),
            GrammarEvidence = o.ActorAttribution.GrammarEvidence,
            ReviewedBy = o.ActorAttribution.ReviewedBy,
            ReviewedUtc = o.ActorAttribution.ReviewedUtc,
        },
        ContextMasters = (o.ContextMasters ?? new List<DictContextMaster>()).Select(c => new DictContextMaster
        {
            MasterName = c.MasterName,
            Roles = (c.Roles ?? new List<string>()).ToList(),
        }).ToList(),
        ApproxDate = o.ApproxDate,
        Curated = o.Curated,
        AttributionNote = o.AttributionNote,
        EvidenceRole = o.EvidenceRole,
    };
}

/// <summary>
/// Editable projection of one <see cref="DictionarySense"/>. Field edits push live into the
/// backing <see cref="Model"/> (the same instance held by the owning entry's Senses list), so the
/// entry model stays current without a flush-on-switch. Only curated occurrences are persisted.
/// </summary>
public partial class SenseEditViewModel : ObservableObject
{
    private bool _suppress;

    /// <summary>The backing sense model — shared with the owning entry's Senses list.</summary>
    public DictionarySense Model { get; }

    [ObservableProperty]
    private string _preferredTarget = "";

    [ObservableProperty]
    private string _explanation = "";

    [ObservableProperty]
    private int _selectedStatusIndex;

    [ObservableProperty]
    private int _selectedValidationIndex;

    [ObservableProperty]
    private string _masterName = "";

    [ObservableProperty]
    private string _alternatesText = "";

    [ObservableProperty]
    private string _searchAliasesText = "";

    [ObservableProperty]
    private string _noteText = "";

    [ObservableProperty]
    private string _relatedTermsText = "";

    [ObservableProperty]
    private string _relatedMastersText = "";

    /// <summary>Lexicographer-curated defining occurrences for this sense.</summary>
    public ObservableCollection<DictOccurrence> CuratedOccurrences { get; } = new();

    /// <summary>Sense-list label: the owning master, or "Corpus-wide" for the shared Zen sense.</summary>
    public string DisplayLabel => string.IsNullOrWhiteSpace(MasterName) ? "Corpus-wide" : MasterName!.Trim();

    public SenseEditViewModel(DictionarySense model)
    {
        Model = model ?? new DictionarySense();
        LoadFromModel();
    }

    private void LoadFromModel()
    {
        _suppress = true;
        try
        {
            PreferredTarget = Model.PreferredTarget ?? "";
            Explanation = Model.Explanation ?? "";
            MasterName = Model.MasterName ?? "";
            NoteText = Model.Note ?? "";
            AlternatesText = string.Join(Environment.NewLine, Model.AlternateTargets ?? new List<string>());
            SearchAliasesText = string.Join(Environment.NewLine, Model.SearchAliases ?? new List<string>());
            RelatedTermsText = string.Join(Environment.NewLine, Model.RelatedTerms ?? new List<string>());
            RelatedMastersText = string.Join(Environment.NewLine, Model.RelatedMasters ?? new List<string>());

            SelectedStatusIndex = (Model.Status ?? "preferred").Trim().ToLowerInvariant() switch
            {
                "preferred" => 0,
                "allowed" => 1,
                "deprecated" => 2,
                "forbidden" => 3,
                _ => 0
            };
            SelectedValidationIndex = (Model.Validation ?? "provisional").Trim().ToLowerInvariant() switch
            {
                "provisional" => 0,
                "multi-source" => 1,
                "disputed" => 2,
                _ => 0
            };

            CuratedOccurrences.Clear();
            _uncuratedOccurrences.Clear();
            foreach (var o in Model.Occurrences ?? new List<DictOccurrence>())
            {
                if (o.Curated) CuratedOccurrences.Add(o);
                else _uncuratedOccurrences.Add(o);
            }
        }
        finally
        {
            _suppress = false;
        }
    }

    partial void OnPreferredTargetChanged(string value) => Push();
    partial void OnExplanationChanged(string value) => Push();
    partial void OnSelectedStatusIndexChanged(int value) => Push();
    partial void OnSelectedValidationIndexChanged(int value) => Push();
    partial void OnAlternatesTextChanged(string value) => Push();
    partial void OnSearchAliasesTextChanged(string value) => Push();
    partial void OnNoteTextChanged(string value) => Push();
    partial void OnRelatedTermsTextChanged(string value) => Push();
    partial void OnRelatedMastersTextChanged(string value) => Push();

    partial void OnMasterNameChanged(string value)
    {
        Push();
        OnPropertyChanged(nameof(DisplayLabel));
    }

    /// <summary>Write the scalar/text fields back onto the backing sense model.</summary>
    public void Push()
    {
        if (_suppress)
            return;

        Model.PreferredTarget = PreferredTarget?.Trim() ?? "";
        Model.Explanation = string.IsNullOrWhiteSpace(Explanation) ? null : Explanation.Trim();
        Model.Note = NoteText?.Trim() ?? "";
        Model.MasterName = string.IsNullOrWhiteSpace(MasterName) ? null : MasterName.Trim();
        // Master-specific senses mirror MasterName into SenseKey; corpus-wide senses keep it null.
        Model.SenseKey = Model.MasterName;

        Model.Status = SelectedStatusIndex switch
        {
            0 => "preferred",
            1 => "allowed",
            2 => "deprecated",
            3 => "forbidden",
            _ => "preferred"
        };
        Model.Validation = SelectedValidationIndex switch
        {
            0 => "provisional",
            1 => "multi-source",
            2 => "disputed",
            _ => "provisional"
        };

        Model.AlternateTargets = SplitLines(AlternatesText);
        Model.SearchAliases = SplitLines(SearchAliasesText);
        Model.RelatedTerms = SplitLines(RelatedTermsText);
        Model.RelatedMasters = SplitLines(RelatedMastersText);
    }

    /// <summary>
    /// Persist the curated occurrences onto the backing model. Only curated occurrences are shown
    /// and editable, but uncurated ones are carried through untouched — dropping them here would
    /// silently delete evidence the editor never displayed.
    /// </summary>
    public void SyncOccurrencesToModel()
    {
        Model.Occurrences = CuratedOccurrences.Concat(_uncuratedOccurrences).ToList();
    }

    private readonly List<DictOccurrence> _uncuratedOccurrences = new();

    private static List<string> SplitLines(string? s) =>
        (s ?? "")
        .Replace("\r\n", "\n")
        .Split('\n')
        .Select(x => x.Trim())
        .Where(x => !string.IsNullOrWhiteSpace(x))
        .Distinct(StringComparer.Ordinal)
        .ToList();
}
