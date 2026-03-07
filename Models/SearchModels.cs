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

    public string SideLabel
        => Side == SearchSide.Original ? "O: " : "T: ";

    public string LeftText => Hit.Left ?? "";
    public string MatchText => Hit.Match ?? "";
    public string RightText => Hit.Right ?? "";

    public string RowText
        => $"{SideLabel}{LeftText}[{MatchText}]{RightText}";
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
