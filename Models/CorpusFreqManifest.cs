using System;

namespace ReadZen.App.Models;

/// <summary>Manifest for the corpus-wide CJK character and bigram frequency index.</summary>
public sealed class CorpusFreqManifest
{
    public int Version { get; set; } = 1;
    public string BuildGuid { get; set; } = "";
    public DateTime BuiltUtc { get; set; }
    public long TotalCharacters { get; set; }
    public int UniqueCharacters { get; set; }
    public int UniqueBigrams { get; set; }
}
