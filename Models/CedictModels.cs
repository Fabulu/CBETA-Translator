using System.Collections.Generic;

namespace ReadZen.App.Models;

public sealed record CedictEntry(
    string Traditional,
    string Simplified,
    string Pinyin,
    string[] Senses);

public sealed record CedictMatch(
    string Headword,
    int StartIndex,
    int Length,
    IReadOnlyList<CedictEntry> Entries);
