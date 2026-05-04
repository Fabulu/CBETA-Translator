using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ReadZen.App.Models;

public enum SearchSide
{
    Original = 0,
    Translated = 1
}

public sealed class SearchHit
{
    public int Index { get; set; }          // match start in searchable text
    public string Left { get; set; } = "";  // KWIC left context
    public string Match { get; set; } = ""; // the query itself (as found)
    public string Right { get; set; } = ""; // KWIC right context

    public string SnippetText => $"{Left}{Match}{Right}";
}

public sealed class SearchResultGroup : INotifyPropertyChanged
{
    private List<SearchResultChild> _children = new();

    public string RelPath { get; set; } = "";
    public string DisplayName { get; set; } = "";   // titles/enShort if available
    public string Tooltip { get; set; } = "";       // full titles or relpath
    public string ChineseTitle { get; set; } = "";   // zh title for side-by-side display
    public bool HasChineseTitle => !string.IsNullOrWhiteSpace(ChineseTitle);

    public TranslationStatus? Status { get; set; }  // from your index cache (optional)
    public int HitsOriginal { get; set; }
    public int HitsTranslated { get; set; }

    // Tree children
    public List<SearchResultChild> Children
    {
        get => _children;
        set
        {
            if (ReferenceEquals(_children, value))
                return;

            _children = value ?? new List<SearchResultChild>();
            OnPropertyChanged();
        }
    }

    public string HeaderText
    {
        get
        {
            string st = Status.HasValue ? $" | {Status.Value}" : "";
            return $"{DisplayName}  ({RelPath}){st}  |  O: {HitsOriginal:n0}  T: {HitsTranslated:n0}";
        }
    }

    public string HitCountBadge => $"O:{HitsOriginal} T:{HitsTranslated}";

    private bool _isExpanded;
    public bool IsExpanded
    {
        get => _isExpanded;
        set
        {
            if (_isExpanded == value) return;
            _isExpanded = value;
            OnPropertyChanged();
        }
    }

    public void ApplyEnrichment(IReadOnlyList<SearchResultChild> enrichedChildren)
    {
        if (enrichedChildren == null || enrichedChildren.Count == 0)
            return;

        if (Children.Count == enrichedChildren.Count && HaveMatchingChildShape(Children, enrichedChildren))
        {
            for (int i = 0; i < Children.Count; i++)
                Children[i].ApplyEnrichment(enrichedChildren[i]);

            return;
        }

        var replacement = new List<SearchResultChild>(enrichedChildren.Count);
        for (int i = 0; i < enrichedChildren.Count; i++)
            replacement.Add(enrichedChildren[i]);

        Children = replacement;
    }

    private static bool HaveMatchingChildShape(IReadOnlyList<SearchResultChild> current, IReadOnlyList<SearchResultChild> enriched)
    {
        if (current.Count != enriched.Count)
            return false;

        for (int i = 0; i < current.Count; i++)
        {
            if (current[i].Side != enriched[i].Side)
                return false;
        }

        return true;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

public class SearchResultChild : INotifyPropertyChanged
{
    private SearchHit _hit = new();
    private bool _primaryIsContextOnly;
    private SearchHit? _secondaryHit;
    private bool _secondaryIsContextOnly = true;

    public string RelPath { get; set; } = "";
    public SearchSide Side { get; set; }
    public SearchHit Hit
    {
        get => _hit;
        set
        {
            if (ReferenceEquals(_hit, value))
                return;

            _hit = value ?? new SearchHit();
            NotifyPrimaryChanged();
        }
    }
    public bool PrimaryIsContextOnly
    {
        get => _primaryIsContextOnly;
        set
        {
            if (_primaryIsContextOnly == value)
                return;

            _primaryIsContextOnly = value;
            NotifyPrimaryChanged();
        }
    }
    public SearchHit? SecondaryHit
    {
        get => _secondaryHit;
        set
        {
            if (ReferenceEquals(_secondaryHit, value))
                return;

            _secondaryHit = value;
            NotifySecondaryChanged();
        }
    }
    public bool SecondaryIsContextOnly
    {
        get => _secondaryIsContextOnly;
        set
        {
            if (_secondaryIsContextOnly == value)
                return;

            _secondaryIsContextOnly = value;
            NotifySecondaryChanged();
        }
    }

    public string SideLabel
        => Side == SearchSide.Original ? "O: " : "T: ";

    public string LeftText => Hit.Left ?? "";
    public string MatchText => Hit.Match ?? "";
    public string RightText => Hit.Right ?? "";
    public string PrimarySnippetText => Hit.SnippetText;
    public string PrimaryDisplayText => $"{SideLabel}{PrimarySnippetText}";
    public bool HasPrimaryStructuredDisplay => !PrimaryIsContextOnly;
    public bool HasPrimaryContextOnlyDisplay => PrimaryIsContextOnly;
    public string SecondarySideLabel => SecondaryHit == null ? "" : (Side == SearchSide.Original ? "T: " : "O: ");
    public string SecondaryLeftText => SecondaryHit?.Left ?? "";
    public string SecondaryMatchText => SecondaryHit?.Match ?? "";
    public string SecondaryRightText => SecondaryHit?.Right ?? "";
    public string SecondarySnippetText => SecondaryHit?.SnippetText ?? "";
    public string SecondaryDisplayText => SecondaryHit == null ? "" : $"{SecondarySideLabel}{SecondarySnippetText}";
    public bool HasSecondaryDisplayText => SecondaryHit != null;
    public bool HasSecondaryStructuredDisplay => SecondaryHit != null && !SecondaryIsContextOnly;
    public bool HasSecondaryContextOnlyDisplay => SecondaryHit != null && SecondaryIsContextOnly;

    public string RowText
        => $"{SideLabel}{LeftText}[{MatchText}]{RightText}";

    public string BilingualRowText
        => HasSecondaryDisplayText ? $"{PrimaryDisplayText}{Environment.NewLine}{SecondaryDisplayText}" : PrimaryDisplayText;

    public void ApplyEnrichment(SearchResultChild enriched)
    {
        if (enriched == null)
            return;

        if (ShouldReplacePrimaryWith(enriched))
        {
            Hit = enriched.Hit;
            PrimaryIsContextOnly = enriched.PrimaryIsContextOnly;
        }
        SecondaryHit = enriched.SecondaryHit;
        SecondaryIsContextOnly = enriched.SecondaryIsContextOnly;
    }

    private bool ShouldReplacePrimaryWith(SearchResultChild enriched)
    {
        if (PrimaryIsContextOnly)
            return true;

        if (string.IsNullOrEmpty(Hit.Match))
            return true;

        return enriched.PrimaryIsContextOnly;
    }
    public ScholarPassage ToScholarPassage()
    {
        string zh = Side == SearchSide.Original ? PrimarySnippetText : SecondarySnippetText;
        string en = Side == SearchSide.Translated ? PrimarySnippetText : SecondarySnippetText;

        var passage = new ScholarPassage
        {
            Id = Guid.NewGuid().ToString("N"),
            SourceRelPath = RelPath,
            ZhText = zh,
            EnText = en,
            AddedUtc = DateTimeOffset.UtcNow
        };
        if (string.IsNullOrWhiteSpace(passage.Summary))
            passage.Summary = passage.GenerateAutoSummary();
        return passage;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void NotifyPrimaryChanged()
    {
        OnPropertyChanged(nameof(Hit));
        OnPropertyChanged(nameof(PrimaryIsContextOnly));
        OnPropertyChanged(nameof(LeftText));
        OnPropertyChanged(nameof(MatchText));
        OnPropertyChanged(nameof(RightText));
        OnPropertyChanged(nameof(PrimarySnippetText));
        OnPropertyChanged(nameof(PrimaryDisplayText));
        OnPropertyChanged(nameof(HasPrimaryStructuredDisplay));
        OnPropertyChanged(nameof(HasPrimaryContextOnlyDisplay));
        OnPropertyChanged(nameof(RowText));
        OnPropertyChanged(nameof(BilingualRowText));
    }

    private void NotifySecondaryChanged()
    {
        OnPropertyChanged(nameof(SecondaryHit));
        OnPropertyChanged(nameof(SecondaryIsContextOnly));
        OnPropertyChanged(nameof(SecondarySideLabel));
        OnPropertyChanged(nameof(SecondaryLeftText));
        OnPropertyChanged(nameof(SecondaryMatchText));
        OnPropertyChanged(nameof(SecondaryRightText));
        OnPropertyChanged(nameof(SecondarySnippetText));
        OnPropertyChanged(nameof(SecondaryDisplayText));
        OnPropertyChanged(nameof(HasSecondaryDisplayText));
        OnPropertyChanged(nameof(HasSecondaryStructuredDisplay));
        OnPropertyChanged(nameof(HasSecondaryContextOnlyDisplay));
        OnPropertyChanged(nameof(BilingualRowText));
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

/// <summary>
/// Sentinel child appended to a <see cref="SearchResultGroup"/> when its full child list has been
/// capped at <c>MaxVisibleChildren</c>. Rendered as a "Show N more…" button in the TreeView.
/// </summary>
public sealed class SearchResultShowMoreItem : SearchResultChild
{
    /// <summary>Number of children hidden behind this sentinel.</summary>
    public int RemainingCount { get; set; }

    /// <summary>RelPath of the parent group, used as the command parameter key.</summary>
    public string GroupRelPath { get; set; } = "";
}

public sealed class AnalyticsBubbleItem
{
    public string Label { get; set; } = "";
    public double Width { get; set; }
    public double Height { get; set; }
    public double FontSize { get; set; }
    public string Tooltip { get; set; } = "";
}
// A small manifest for the bloom index on disk
public sealed class SearchIndexManifest
{
    public int Version { get; set; } = 1;
    public string RootPath { get; set; } = "";
    public DateTime BuiltUtc { get; set; } = DateTime.UtcNow;

    public int BloomBits { get; set; } = 4096;
    public int BloomHashCount { get; set; } = 4;
    public string BuildGuid { get; set; } = "search-v1-bloom-4096";

    public List<SearchIndexEntry> Entries { get; set; } = new();
}

public sealed class SearchIndexEntry
{
    public int Id { get; set; }                 // sequential
    public string RelPath { get; set; } = "";
    public SearchSide Side { get; set; }

    public long LastWriteUtcTicks { get; set; }
    public long LengthBytes { get; set; }

    public long BloomOffset { get; set; }       // offset in index.bin
}

// Sidecar manifest for searchable text blocks aligned by (relPath, side).
// This intentionally duplicates file identity fields so the search verify path
// can validate it is reading text for the exact file version.
public sealed class SearchTextManifest
{
    public int Version { get; set; } = 1;
    public string RootPath { get; set; } = "";
    public DateTime BuiltUtc { get; set; } = DateTime.UtcNow;
    public string BuildGuid { get; set; } = "search-v1-text-sidecar";
    public List<SearchTextEntry> Entries { get; set; } = new();
}

public sealed class SearchTextEntry
{
    public int Id { get; set; }
    public string RelPath { get; set; } = "";
    public SearchSide Side { get; set; }
    public long LastWriteUtcTicks { get; set; }
    public long LengthBytes { get; set; }
    public long TextOffset { get; set; }
    public int TextLengthBytes { get; set; }
}

// Optional Phase C artifact: compact-CJK 2-gram postings for short-query prefiltering.
// Search must remain correct when this is missing or invalid (fallback to bloom+verify path).
public sealed class SearchCjkBigramManifest
{
    public int Version { get; set; } = 1;
    public string RootPath { get; set; } = "";
    public DateTime BuiltUtc { get; set; } = DateTime.UtcNow;
    public string BuildGuid { get; set; } = "search-v1-cjk2-postings";
    public int GramSize { get; set; } = 2;
    public int EntryCount { get; set; }
    public List<SearchCjkBigramPosting> Postings { get; set; } = new();
}

public sealed class SearchCjkBigramPosting
{
    public string Gram { get; set; } = "";
    public List<int> EntryIds { get; set; } = new();
}

/// <summary>
/// Represents one master currently active in the multi-master intersection filter.
/// </summary>
public sealed class ActiveMasterFilter
{
    public string MasterName { get; set; } = "";
    public HashSet<string> RelPaths { get; set; } = new(StringComparer.OrdinalIgnoreCase);
}

