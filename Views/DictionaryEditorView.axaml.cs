using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Markup.Xaml;
using ReadZen.App.Infrastructure;
using ReadZen.App.Models;
using ReadZen.App.Services;
using ReadZen.App.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace ReadZen.App.Views;

/// <summary>
/// The searchable + browsable Zen dictionary surface (Zen-to-Zen dictionary ONLY — no
/// CC-CEDICT anywhere here). Hosted both by <see cref="TermbaseEditorWindow"/> (pop-out
/// window, opened from context-menu term lookups and deep links) and by the top-level
/// Dictionary tab in MainWindow. Read-only browse over the rich dictionary
/// (termbase.v2.json via <see cref="IDictionaryStore"/>); editing happens in the rich
/// editor window (<see cref="DictionaryEditorWindow"/>), reachable via
/// <see cref="EditRequested"/>. The view owns a <see cref="ZenDictionaryBrowseViewModel"/>
/// that is (re)built per corpus via <see cref="Load"/>.
/// </summary>
public partial class DictionaryEditorView : UserControl
{
    private ZenDictionaryBrowseViewModel? _vm;

    /// <summary>Fired when the user wants to navigate to a source occurrence in the reader.</summary>
    public event EventHandler<NavigationRequest>? CorpusNavigationRequested;

    /// <summary>Fired when the user asks to edit the dictionary (host opens the rich editor).</summary>
    public event EventHandler? EditRequested;

    /// <summary>Wired by the host: the pop-out window closes; the tab host may no-op.</summary>
    public Action? CloseRequested { get; set; }

    public DictionaryEditorView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// (Re)builds the view model for the given corpus and loads its Zen dictionary. Safe to
    /// call repeatedly; the caller is responsible for only reloading when the corpus changes.
    /// The extra parameters are accepted for host-call compatibility; the browse view needs
    /// only the corpus root (the dictionary file lives at its top level).
    /// </summary>
    public void Load(string root, string origDir, string transDir, string? username = null,
        string? landingTerm = null, string? landingCommunityUser = null)
    {
        if (_vm != null)
        {
            _vm.OccurrenceNavigationRequested -= OnVmOccurrenceNavigation;
            _vm.EditRequested -= OnVmEditRequested;
        }

        var store = App.Services.GetRequiredService<IDictionaryStore>();
        var vm = new ZenDictionaryBrowseViewModel(store, root);

        vm.CloseRequested = () => CloseRequested?.Invoke();
        vm.OccurrenceNavigationRequested += OnVmOccurrenceNavigation;
        vm.EditRequested += OnVmEditRequested;

        if (!string.IsNullOrWhiteSpace(landingTerm))
            vm.SelectTerm(landingTerm!);

        _vm = vm;
        DataContext = vm;

        AsyncGuard.Run(async () =>
        {
            // Warm the exact-lookup index first so RelatedTermLabel can resolve the
            // English gloss of related entries on the first render. Never forces a
            // rebuild for a different root; failures degrade to Chinese-only labels.
            try
            {
                var lookup = App.Services.GetService<IZenDictionaryLookup>();
                if (lookup != null && !lookup.IsLoaded)
                    await lookup.EnsureLoadedAsync(root);
            }
            catch { /* glosses degrade to the bare Chinese term */ }

            await vm.LoadCommand.ExecuteAsync(null);
        }, "DictionaryEditorView.Load");
    }

    /// <summary>Reloads the dictionary from disk (e.g. after the rich editor saved).</summary>
    public void Reload()
    {
        var vm = _vm;
        if (vm == null) return;
        AsyncGuard.Run(async () => await vm.LoadCommand.ExecuteAsync(null), "DictionaryEditorView.Reload");
    }

    private void OnVmOccurrenceNavigation(object? sender, NavigationRequest req) => CorpusNavigationRequested?.Invoke(this, req);
    private void OnVmEditRequested(object? sender, EventArgs e) => EditRequested?.Invoke(this, e);

    /// <summary>
    /// The source term of the currently-selected dictionary entry, or null when nothing is
    /// selected. Used by the tab pop-out affordance to land the pop-out window on the same term.
    /// </summary>
    public string? GetCurrentTerm() => _vm?.SelectedEntry?.SourceTerm;

    /// <summary>Lands the browse view on a term (selects it, or searches for it).</summary>
    public void ApplyLanding(string? term, string? communityUser = null)
    {
        if (!string.IsNullOrWhiteSpace(term))
            _vm?.SelectTerm(term!);
    }

    /// <summary>
    /// Legacy entry point ("create termbase entry" context flows): the browse view lands on
    /// the term so the user sees whether an entry exists; authoring happens in the rich editor.
    /// </summary>
    public void PreFillNewEntry(string sourceTerm) => ApplyLanding(sourceTerm);

    /// <summary>Hides the bottom "Close" button (the tab host has no window to close).</summary>
    public void SetCloseButtonVisible(bool visible)
    {
        var btn = this.FindControl<Button>("BtnCloseEditor");
        if (btn != null) btn.IsVisible = visible;
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
}

/// <summary>
/// Small value converters for the Zen dictionary entry card (SPA lookup-card parity).
/// Static fields so compiled bindings can reference them via x:Static.
/// </summary>
public static class ZenDictConverters
{
    /// <summary>MasterName → the sense heading ("Sense · Zhaozhou" / "Corpus-wide sense").</summary>
    public static readonly IValueConverter SenseLabel =
        new FuncValueConverter<string?, string>(m =>
            string.IsNullOrWhiteSpace(m) ? "Corpus-wide sense" : "Sense · " + m!.Trim());

    /// <summary>List&lt;string&gt; → "a, b, c".</summary>
    public static readonly IValueConverter JoinComma =
        new FuncValueConverter<List<string>?, string>(v =>
            v == null ? "" : string.Join(", ", v.Where(s => !string.IsNullOrWhiteSpace(s))));

    /// <summary>List&lt;string&gt; → true when it has at least one non-blank item.</summary>
    public static readonly IValueConverter HasAnyString =
        new FuncValueConverter<List<string>?, bool>(v =>
            v != null && v.Any(s => !string.IsNullOrWhiteSpace(s)));

    /// <summary>Occurrence list → true when non-empty.</summary>
    public static readonly IValueConverter HasAnyOccurrence =
        new FuncValueConverter<List<DictOccurrence>?, bool>(v => v is { Count: > 0 });

    /// <summary>Senses list → "N senses" label (shown only for multi-sense entries).</summary>
    public static readonly IValueConverter SenseCountLabel =
        new FuncValueConverter<List<DictionarySense>?, string>(v =>
            v == null ? "" : $"{v.Count} senses");

    /// <summary>Senses list → true when the entry has more than one sense.</summary>
    public static readonly IValueConverter HasMultipleSenses =
        new FuncValueConverter<List<DictionarySense>?, bool>(v => v is { Count: > 1 });

    /// <summary>
    /// Related head term → "english gloss · 術語" (SPA related-entries parity: English is
    /// the DEFAULT label, Chinese secondary). Resolves the gloss from the loaded Zen
    /// dictionary (structured data, first sense's PreferredTarget); degrades to the bare
    /// Chinese term when no English exists or the lookup index is unavailable (host-less
    /// tests, not-yet-loaded corpus). Display only — the lookup key stays the Chinese term.
    /// </summary>
    public static readonly IValueConverter RelatedTermLabel =
        new FuncValueConverter<string?, string>(t =>
        {
            if (string.IsNullOrWhiteSpace(t)) return "";
            var term = t!.Trim();
            try
            {
                var lookup = App.Services.GetService<IZenDictionaryLookup>();
                if (lookup is { IsLoaded: true }
                    && lookup.TryLookupExact(term, out var entry)
                    && !string.IsNullOrWhiteSpace(entry.FirstSenseTarget))
                    return entry.FirstSenseTarget + " · " + term;
            }
            catch { /* no app host — Chinese-only label */ }
            return term;
        });

    /// <summary>DictOccurrence → "T48n2005 · 0526c25–0526c27" open-source label.</summary>
    public static readonly IValueConverter OccurrenceSourceLabel =
        new FuncValueConverter<DictOccurrence?, string>(occ =>
        {
            if (occ == null) return "";
            var baseName = (occ.RelPath ?? "").Replace('\\', '/');
            int slash = baseName.LastIndexOf('/');
            if (slash >= 0) baseName = baseName[(slash + 1)..];
            if (baseName.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
                baseName = baseName[..^4];

            string range = occ.FromLb ?? "";
            if (!string.IsNullOrEmpty(occ.ToLb) && occ.ToLb != occ.FromLb)
                range += "–" + occ.ToLb;

            return string.IsNullOrEmpty(range) ? "Open " + baseName : $"Open {baseName} · {range}";
        });
}
