// Infrastructure/DictionaryCardModel.cs
// Pure, UI-free presentation logic for the reader-facing Zen dictionary entry card.
// These functions encode the render-time normalizations described in
// runs/CLAUDE-RUNS/RUN-20260711-1248-full-cbeta-translation/dictionary-build/DICTIONARY_DISPLAY_DESIGN.md
// (§0 AttributionNote split, §3/§4 sense selection, §6 validation signal, §7 curated-first +
// collapse gate, §5.4 nested-master provenance). Kept free of Avalonia so it is unit-testable
// in ReadZen.Tests without an application host; the DictionaryEntryCard view consumes it.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ReadZen.App.Models;

namespace ReadZen.App.Infrastructure;

/// <summary>Quietness tier for a sense's validation signal (design §6).</summary>
public enum ValidationTier
{
    /// <summary>Quiet muted note (single-source / provisional).</summary>
    Quiet = 0,
    /// <summary>The one earned tint (disputed reading).</summary>
    Disputed = 1,
}

/// <summary>A resolved validation signal to place beside the gloss. Multi-source yields none.</summary>
public sealed record DictValidationSignal(string Label, ValidationTier Tier);

/// <summary>
/// The read shape of a stored <see cref="DictOccurrence.AttributionNote"/> after the §0 split.
/// <see cref="Matched"/> distinguishes the promoted citation header from the whole-note fallback.
/// </summary>
public sealed record DictAttributionParts(
    string? WorkTitleEnglish,
    string? WorkTitleChinese,
    string Gloss,
    bool Matched);

/// <summary>Human relationship category for a context master (design §5.4 role translation).</summary>
public enum ProvenanceKind
{
    Voice = 0,   // the speaking / raising / owning voice → ordered first
    Subject = 1, // the case-figure / person discussed → ordered after the voice
}

/// <summary>Pure presentation helpers for the dictionary entry card. No UI dependency.</summary>
public static class DictionaryCardModel
{
    // "Source record (T/T48/T48n2005.xml). " — the redundant prefix dropped at read time (§0.2).
    private static readonly Regex SourceRecordPrefix =
        new(@"^\s*Source record \([^)]*\)\.\s*", RegexOptions.Compiled);

    // "The Gateless Barrier (無門關)" — English title + trailing parenthetical Chinese title.
    private static readonly Regex TitleWithChinese =
        new(@"^(?<en>.*?)\s*[（(](?<zh>[^）)]*)[）)]\s*$", RegexOptions.Compiled);

    /// <summary>
    /// Split a stored AttributionNote into (English work title, Chinese work title, attribution
    /// gloss) per design §0. Drops the "Source record (path)." prefix and promotes the
    /// "Title (漢): prose" header. Falls back defensively (design §9): if the expected shape is
    /// absent the whole note becomes the gloss and titles are null (<see cref="DictAttributionParts.Matched"/> = false).
    /// </summary>
    public static DictAttributionParts SplitAttributionNote(string? note)
    {
        if (string.IsNullOrWhiteSpace(note))
            return new DictAttributionParts(null, null, "", false);

        var remainder = SourceRecordPrefix.Replace(note, "");

        // Separator between the work-title header and the prose gloss is the first ASCII ": ".
        int sep = remainder.IndexOf(": ", StringComparison.Ordinal);
        if (sep <= 0)
        {
            // Not the expected shape — print the whole (original) note as the gloss.
            return new DictAttributionParts(null, null, note.Trim(), false);
        }

        string titlePart = remainder[..sep].Trim();
        string gloss = remainder[(sep + 2)..].Trim();

        if (titlePart.Length == 0 || gloss.Length == 0)
            return new DictAttributionParts(null, null, note.Trim(), false);

        string? en = titlePart;
        string? zh = null;
        var m = TitleWithChinese.Match(titlePart);
        if (m.Success)
        {
            en = m.Groups["en"].Value.Trim();
            zh = m.Groups["zh"].Value.Trim();
            if (en.Length == 0) en = titlePart; // all-Chinese title: keep the whole thing visible
            if (zh!.Length == 0) zh = null;
        }

        return new DictAttributionParts(en, zh, gloss, true);
    }

    /// <summary>True when the entry is the 96% single-sense case (design §3): suppress all sense chrome.</summary>
    public static bool IsSingleSense(DictionaryEntry? entry)
        => entry?.Senses is { Count: 1 };

    /// <summary>
    /// Order a sense's occurrences curated-first (design §7.2), preserving stored order within each
    /// tier. Stable — defining witnesses (<see cref="DictOccurrence.Curated"/>) lead, the broader set follows.
    /// </summary>
    public static IReadOnlyList<DictOccurrence> OrderCuratedFirst(IEnumerable<DictOccurrence>? occurrences)
    {
        if (occurrences == null) return Array.Empty<DictOccurrence>();
        // OrderBy is a stable sort in LINQ-to-objects, so stored order is preserved within each tier.
        return occurrences.OrderBy(o => o.Curated ? 0 : 1).ToList();
    }

    /// <summary>
    /// Whether to offer the optional user-initiated collapse control for a sense (design §7.3):
    /// only when the evidence wall is genuinely long (&gt; ~6 witnesses, above the median). Default-open;
    /// short senses show no affordance at all.
    /// </summary>
    public static bool ShouldOfferCollapse(DictionarySense? sense)
        => (sense?.Occurrences?.Count ?? 0) > 6;

    /// <summary>Median-ish threshold above which the collapse control appears. Exposed for the view + tests.</summary>
    public const int CollapseGateWitnessCount = 6;

    /// <summary>
    /// Resolve the quiet validation signal for a sense (design §6). Multi-source (the 94% norm)
    /// returns null — silence means well-attested. single-source / single-source-explicit merge to
    /// one label; provisional is a neutral pill; disputed earns the one tint.
    /// </summary>
    public static DictValidationSignal? GetValidationSignal(string? validation)
    {
        switch ((validation ?? "").Trim().ToLowerInvariant())
        {
            case "multi-source":
            case "": // unknown / absent → treat as baseline, stay silent
                return null;
            case "single-source":
            case "single-source-explicit":
                return new DictValidationSignal("attested in a single source", ValidationTier.Quiet);
            case "provisional":
                return new DictValidationSignal("provisional", ValidationTier.Quiet);
            case "disputed":
                return new DictValidationSignal("disputed reading", ValidationTier.Disputed);
            default:
                return null; // unknown state stays silent rather than shouting
        }
    }

    /// <summary>
    /// One-line human provenance for an occurrence with nested attribution (design §5.4). Returns
    /// null for the trivial single-context-master case (the occurrence MasterName already covers it):
    /// rendering a roles list there would be noise. For &gt;1 master, translates role tokens into
    /// relationship words ("raised by X · on Y's case"); never emits raw role tokens.
    /// </summary>
    public static string? BuildProvenanceLine(DictOccurrence? occ)
    {
        var masters = occ?.ContextMasters;
        if (masters == null || masters.Count <= 1)
            return null;

        var voice = new List<string>();
        var subject = new List<string>();

        foreach (var cm in masters)
        {
            if (cm == null || string.IsNullOrWhiteSpace(cm.MasterName)) continue;
            var (kind, fragment) = ClassifyContextMaster(cm);
            (kind == ProvenanceKind.Voice ? voice : subject).Add(fragment);
        }

        var fragments = voice.Concat(subject).ToList();
        if (fragments.Count == 0)
            return null;

        // Edge case §9: keep to one line — beyond 3 named masters, show primary voice + "and N others".
        if (fragments.Count > 3)
        {
            int others = fragments.Count - 1;
            return $"{fragments[0]} · and {others} others";
        }

        return string.Join(" · ", fragments);
    }

    /// <summary>Classify a context master's roles into a provenance fragment (design §5.4).</summary>
    public static (ProvenanceKind Kind, string Fragment) ClassifyContextMaster(DictContextMaster cm)
    {
        var name = cm.MasterName;
        var roles = cm.Roles ?? new List<string>();
        bool Has(string r) => roles.Any(x => string.Equals(x, r, StringComparison.OrdinalIgnoreCase));

        // Voice tier — whoever speaks / raises / owns / answers the passage. utterer+case-figure
        // resolves to the primary voice (§5.4), so utterer is tested first.
        if (Has("utterer") || Has("later-raiser") || Has("later-quoter"))
            return (ProvenanceKind.Voice, $"raised by {name}");
        if (Has("respondent"))
            return (ProvenanceKind.Voice, $"answered by {name}");
        if (Has("commentator"))
            return (ProvenanceKind.Voice, $"commented by {name}");
        if (Has("record-owner"))
            return (ProvenanceKind.Voice, $"recorded by {name}");

        // Subject tier — whose case / who is discussed.
        if (Has("case-figure"))
            return (ProvenanceKind.Subject, $"on {name}'s case");
        if (Has("person-discussed") || Has("person-described"))
            return (ProvenanceKind.Subject, $"on {name}");

        return (ProvenanceKind.Subject, name);
    }

    /// <summary>
    /// Whether the entry's per-sense related regions (RelatedTerms + RelatedMasters) are identical
    /// across all senses (design §4.3). When true and multi-sense, the view de-duplicates them to a
    /// single entry-foot roll-up instead of repeating the same links under every sense.
    /// </summary>
    public static bool RelatedRegionsUniform(DictionaryEntry? entry)
    {
        var senses = entry?.Senses;
        if (senses == null || senses.Count <= 1)
            return true;

        static string Key(DictionarySense s)
        {
            var terms = (s.RelatedTerms ?? new List<string>()).OrderBy(x => x, StringComparer.Ordinal);
            var masters = (s.RelatedMasters ?? new List<string>()).OrderBy(x => x, StringComparer.Ordinal);
            return string.Join("␟", terms) + "␞" + string.Join("␟", masters);
        }

        var first = Key(senses[0]);
        return senses.All(s => string.Equals(Key(s), first, StringComparison.Ordinal));
    }

    /// <summary>
    /// The line reference label "FromLb–ToLb" (design §5.1). Collapses to a single ref when From==To
    /// or one side is missing; empty string when neither is present.
    /// </summary>
    public static string FormatLineRef(string? fromLb, string? toLb)
    {
        var from = (fromLb ?? "").Trim();
        var to = (toLb ?? "").Trim();
        if (from.Length == 0) return to;
        if (to.Length == 0 || string.Equals(from, to, StringComparison.Ordinal)) return from;
        return $"{from}–{to}"; // en dash
    }
}
