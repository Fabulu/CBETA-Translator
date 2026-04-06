using System;
using System.Collections.Generic;

namespace CbetaTranslator.App.Models;

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

public sealed class SearchResultGroup
{
    public string RelPath { get; set; } = "";
    public string DisplayName { get; set; } = "";   // titles/enShort if available
    public string Tooltip { get; set; } = "";       // full titles or relpath

    public TranslationStatus? Status { get; set; }  // from your index cache (optional)
    public int HitsOriginal { get; set; }
    public int HitsTranslated { get; set; }

    // Tree children
    public List<SearchResultChild> Children { get; set; } = new();

    public string HeaderText
    {
        get
        {
            string st = Status.HasValue ? $" | {Status.Value}" : "";
            return $"{DisplayName}  ({RelPath}){st}  |  O: {HitsOriginal:n0}  T: {HitsTranslated:n0}";
        }
    }
}

public sealed class SearchResultChild
{
    public string RelPath { get; set; } = "";
    public SearchSide Side { get; set; }
    public SearchHit Hit { get; set; } = new();
    public bool PrimaryIsContextOnly { get; set; }
    public SearchHit? SecondaryHit { get; set; }
    public bool SecondaryIsContextOnly { get; set; } = true;

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

    public ScholarPassage ToScholarPassage()
    {
        string zh = Side == SearchSide.Original ? PrimarySnippetText : SecondarySnippetText;
        string en = Side == SearchSide.Translated ? PrimarySnippetText : SecondarySnippetText;

        return new ScholarPassage
        {
            Id = Guid.NewGuid().ToString("N"),
            SourceRelPath = RelPath,
            ZhText = zh,
            EnText = en,
            AddedUtc = DateTimeOffset.UtcNow
        };
    }
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





