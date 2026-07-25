using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using ReadZen.App.Messages;
using ReadZen.App.Models;
using ReadZen.App.Services;

namespace ReadZen.App.ViewModels;

/// <summary>
/// ViewModel for the searchable + browsable Zen dictionary view (the Dictionary tab and
/// its pop-out). Read-only over the rich dictionary (<see cref="IDictionaryStore"/>,
/// termbase.v2.json): the list is the browse surface, the selected entry renders as an
/// SPA-style card (senses, badges, explanation, evidence). Editing lives exclusively in
/// the rich editor window (<c>DictionaryEditorWindowViewModel</c>). No CC-CEDICT here —
/// this is the Zen-to-Zen dictionary only.
/// </summary>
public partial class ZenDictionaryBrowseViewModel : ViewModelBase
{
    private readonly IDictionaryStore _store;
    private readonly string _root;

    private List<DictionaryEntry> _all = new();
    private string? _pendingLandingTerm;

    [ObservableProperty]
    private string _searchQuery = "";

    [ObservableProperty]
    private DictionaryEntry? _selectedEntry;

    [ObservableProperty]
    private string _statusMessage = "";

    public ObservableCollection<DictionaryEntry> FilteredEntries { get; } = new();

    /// <summary>Raised when the user opens a source occurrence; the host navigates the reader.</summary>
    public event EventHandler<NavigationRequest>? OccurrenceNavigationRequested;

    /// <summary>Raised when the user asks to edit the dictionary (opens the rich editor).</summary>
    public event EventHandler? EditRequested;

    /// <summary>Wired by the host view to close the pop-out window (no-op for the tab).</summary>
    public Action? CloseRequested { get; set; }

    public ZenDictionaryBrowseViewModel(IDictionaryStore store, string root)
    {
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _root = root ?? throw new ArgumentNullException(nameof(root));
    }

    // ----- Commands -----

    [RelayCommand]
    private async Task LoadAsync()
    {
        try
        {
            var file = await _store.LoadAsync(_root);
            _all = (file.Entries ?? new List<DictionaryEntry>())
                .Where(e => !string.IsNullOrWhiteSpace(e?.SourceTerm))
                .OrderBy(e => e!.SourceTerm, StringComparer.Ordinal)
                .Cast<DictionaryEntry>()
                .ToList();

            ApplyFilter();
            StatusMessage = _all.Count == 0
                ? "The Zen dictionary is empty for this corpus."
                : $"{_all.Count:n0} term{(_all.Count == 1 ? "" : "s")} · Zen dictionary";

            if (!string.IsNullOrWhiteSpace(_pendingLandingTerm))
            {
                var term = _pendingLandingTerm!;
                _pendingLandingTerm = null;
                SelectTerm(term);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = "Load failed: " + ex.Message;
        }
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
    private void SelectRelatedTerm(string? term)
    {
        if (!string.IsNullOrWhiteSpace(term))
            SelectTerm(term!);
    }

    /// <summary>
    /// Open a Zen master in the Zen Master Manager (SPA "#/master/{name}" parity). Sent via
    /// the typed messenger so both hosts (Dictionary tab and termbase pop-out) work without
    /// per-host event wiring; MainWindow owns the single registration.
    /// </summary>
    [RelayCommand]
    private void OpenMaster(string? masterName)
    {
        if (!string.IsNullOrWhiteSpace(masterName))
            WeakReferenceMessenger.Default.Send(new OpenMasterRequestedMessage(masterName!.Trim()));
    }

    [RelayCommand]
    private void OpenEditor() => EditRequested?.Invoke(this, EventArgs.Empty);

    [RelayCommand]
    private void CloseWindow() => CloseRequested?.Invoke();

    // ----- Landing / selection -----

    /// <summary>
    /// Land on <paramref name="term"/>: select its entry when it exists, otherwise put the
    /// term in the search box so the user sees the (empty) result. Safe before load — the
    /// term is applied when loading finishes.
    /// </summary>
    public void SelectTerm(string term)
    {
        if (string.IsNullOrWhiteSpace(term)) return;

        if (_all.Count == 0)
        {
            _pendingLandingTerm = term;
            return;
        }

        var exact = _all.FirstOrDefault(e => string.Equals(e.SourceTerm, term, StringComparison.Ordinal));
        if (exact != null)
        {
            SearchQuery = "";
            if (!FilteredEntries.Contains(exact)) ApplyFilter();
            SelectedEntry = exact;
        }
        else
        {
            SearchQuery = term.Trim();
        }
    }

    // ----- Property-change hooks -----

    partial void OnSearchQueryChanged(string value) => ApplyFilter();

    // ----- Ranked search (ports the SPA's rankDictionaryEntry, views/dict-browse.js) -----

    private void ApplyFilter()
    {
        var q = Normalize(SearchQuery);

        List<DictionaryEntry> filtered;
        if (q.Length == 0)
        {
            filtered = _all;
        }
        else
        {
            filtered = _all
                .Select((e, i) => (Entry: e, Index: i, Score: Rank(e, q)))
                .Where(r => r.Score >= 0)
                .OrderByDescending(r => r.Score)
                .ThenBy(r => r.Index)
                .Select(r => r.Entry)
                .ToList();
        }

        var prev = SelectedEntry;

        FilteredEntries.Clear();
        foreach (var e in filtered)
            FilteredEntries.Add(e);

        if (prev != null && FilteredEntries.Contains(prev))
            SelectedEntry = prev;
        else
            SelectedEntry = FilteredEntries.FirstOrDefault();
    }

    /// <summary>Lowercased, punctuation-collapsed lookup key (mirror of the SPA normalizer).</summary>
    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return "";
        var sb = new System.Text.StringBuilder(value.Length);
        bool lastWasSpace = true;
        foreach (var ch in value.Normalize(System.Text.NormalizationForm.FormKC))
        {
            if (char.IsLetterOrDigit(ch))
            {
                sb.Append(char.ToLowerInvariant(ch));
                lastWasSpace = false;
            }
            else if (!lastWasSpace)
            {
                sb.Append(' ');
                lastWasSpace = true;
            }
        }
        return sb.ToString().Trim();
    }

    /// <summary>-1 for no match; larger values rank stronger lexical evidence first.</summary>
    private static int Rank(DictionaryEntry entry, string q)
    {
        var source = Normalize(entry.SourceTerm);
        if (source == q) return 1000;
        if (source.Contains(q, StringComparison.Ordinal)) return 950;

        var senses = entry.Senses ?? new List<DictionarySense>();

        var preferred = senses.Select(s => Normalize(s?.PreferredTarget)).Where(s => s.Length > 0).ToList();
        if (preferred.Any(v => v == q)) return 900;
        if (preferred.Any(v => v.Contains(q, StringComparison.Ordinal))) return 850;

        var alternates = senses.SelectMany(s => s?.AlternateTargets ?? new List<string>())
            .Select(Normalize).Where(s => s.Length > 0).ToList();
        if (alternates.Any(v => v == q)) return 800;
        if (alternates.Any(v => v.Contains(q, StringComparison.Ordinal))) return 750;

        var aliases = senses.SelectMany(s => s?.SearchAliases ?? new List<string>())
            .Select(Normalize).Where(s => s.Length > 0).ToList();
        if (aliases.Any(v => v == q)) return 700;
        if (aliases.Any(v => v.Contains(q, StringComparison.Ordinal))) return 650;

        var prose = Normalize(string.Join("  ", senses.SelectMany(ProseOf)));
        if (prose.Contains(q, StringComparison.Ordinal)) return 100;

        return -1;
    }

    private static IEnumerable<string> ProseOf(DictionarySense? s)
    {
        if (s == null) yield break;
        if (!string.IsNullOrWhiteSpace(s.Explanation)) yield return s.Explanation!;
        if (!string.IsNullOrWhiteSpace(s.Note)) yield return s.Note;
        if (!string.IsNullOrWhiteSpace(s.Status)) yield return s.Status;
        foreach (var occ in s.Occurrences ?? new List<DictOccurrence>())
        {
            if (!string.IsNullOrWhiteSpace(occ?.Kwic)) yield return occ!.Kwic;
        }
    }
}
