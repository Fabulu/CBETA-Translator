// Models/TmRow.cs
using System;
using System.Text.Json.Serialization;

namespace ReadZen.App.Models;

/// <summary>
/// One translation-memory row in the community-shared jsonl files
/// (translation-memory.approved.jsonl / translation-memory.reference.jsonl).
/// Consolidates four identical-purpose private copies (audit P3.6) that had
/// drifted: two carried BlockNumber, two did not.
///
/// SERIALIZATION IS A SHARED CONTRACT — these files are community-synced.
/// Property declaration order IS the JSON key order. BlockNumber is nullable
/// and omitted when null, which reproduces both historical shapes: writers
/// that never set it (review ledger, assistant reference build) emit no key,
/// exactly as their 6-property row did; readers map a missing key to null.
/// Writers that must keep emitting "BlockNumber":0 for legacy rows normalize
/// null→0 before serializing (see CommunityDataService). Each service keeps
/// its own JsonSerializerOptions — the on-disk escaping already differs by
/// service and must stay as-is. Byte-compatibility is pinned by
/// TmRowSerializationCompatTests; run it before changing ANYTHING here.
/// </summary>
public sealed class TmRow
{
    public string SourceText { get; set; } = "";
    public string TargetText { get; set; } = "";
    public string RelPath { get; set; } = "";

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? BlockNumber { get; set; }

    public string ReviewStatus { get; set; } = "";
    public string Translator { get; set; } = "";
    public DateTimeOffset? WrittenUtc { get; set; }
}
