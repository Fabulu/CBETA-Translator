using System;
using System.Collections.Generic;

namespace ReadZen.App.Models;

// Sixth index artifact (gramsets sidecar): a PURE ACCELERATOR cache of per-entry
// bigram ("gram") sets used by the incremental inverted/cjk2 rebuild paths.
// Losing or corrupting this sidecar only costs speed, never correctness, and never
// triggers a full rebuild by itself — any anomaly makes the loader treat the whole
// sidecar as absent and the build recomputes gram sets from text.
//
// Ids are NEVER stored here: entries are keyed by (RelPath, Side) and validated by
// content identity (ContentHash, or LastWriteUtcTicks+LengthBytes for legacy/null
// hashes). Consumers look up NEW positional Ids from the freshly built entry list
// on every build.
public sealed class GramSetsManifest
{
    public int Version { get; set; } = 1;

    /// <summary>Expected value: <c>"search-v1-gramsets"</c> (see GramSetsStore.BuildGuid).</summary>
    public string BuildGuid { get; set; } = "";

    public string? RootPath { get; set; }

    public DateTime BuiltUtc { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// The family IndexStamp this sidecar was saved with. INFORMATIONAL ONLY — it is
    /// NOT used for per-entry validity (entries validate individually by content
    /// identity, so the sidecar stays useful across builds it did not participate in).
    /// </summary>
    public string? IndexStamp { get; set; }

    /// <summary>
    /// Uppercase-hex copy of the 16-byte pairing token embedded in the bin header
    /// (right after the "GSB1" magic). Fresh per save; the loader refuses the sidecar
    /// when bin and manifest tokens differ — that is what catches a torn pair (crash
    /// between the bin move and the manifest move), which bounds/content-identity
    /// checks cannot, since those validate the XML files rather than the bin bytes.
    /// </summary>
    public string? BinPairingToken { get; set; }

    public List<GramSetsEntry> Entries { get; set; } = new();
}

public sealed class GramSetsEntry
{
    public string RelPath { get; set; } = "";
    public SearchSide Side { get; set; }

    /// <summary>SHA256 lowercase hex of the source XML bytes, when known; null for legacy rows.</summary>
    public string? ContentHash { get; set; }

    public long LastWriteUtcTicks { get; set; }
    public long LengthBytes { get; set; }

    /// <summary>Absolute byte offset of this entry's inverted-alphabet gram array in search.gramsets.bin.</summary>
    public long InvOffset { get; set; }

    /// <summary>Element count (uint32s) of the inverted-alphabet gram array.</summary>
    public int InvCount { get; set; }

    /// <summary>Absolute byte offset of this entry's cjk2-alphabet gram array in search.gramsets.bin.</summary>
    public long Cjk2Offset { get; set; }

    /// <summary>Element count (uint32s) of the cjk2-alphabet gram array.</summary>
    public int Cjk2Count { get; set; }
}
