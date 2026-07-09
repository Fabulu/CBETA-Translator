using System;

namespace ReadZen.App.Models;

/// <summary>Manifest for the corpus-wide CJK character and bigram frequency index.</summary>
public sealed class CorpusFreqManifest
{
    public int Version { get; set; } = 1;
    public string BuildGuid { get; set; } = "";
    public DateTime BuiltUtc { get; set; }

    /// <summary>
    /// IndexStamp of the main <see cref="SearchIndexManifest"/> from the SAME build this
    /// frequency index was saved with. A crash between the main manifest commit and the
    /// corpusfreq save would otherwise leave the previous build's frequencies silently
    /// trusted for ranking. Loaders refuse the artifact when this is null (legacy file)
    /// or differs (Ordinal) from the loaded main manifest's IndexStamp. Nullable for
    /// backward compatibility.
    /// </summary>
    public string? IndexStamp { get; set; }
    public long TotalCharacters { get; set; }
    public int UniqueCharacters { get; set; }
    public int UniqueBigrams { get; set; }
}
