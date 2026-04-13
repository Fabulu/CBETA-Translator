using System;

namespace ReadZen.App.Models;

/// <summary>
/// A single tag applied to a range of text in a document, identified by lb-tag ranges.
/// Stored per-user in JSONL files.
/// </summary>
public sealed class DocumentTag
{
    public string Id { get; set; } = "";
    public string RelPath { get; set; } = "";       // e.g. "T48/T48n2005.xml"
    public string FromLb { get; set; } = "";         // lb n-value start
    public string ToLb { get; set; } = "";           // lb n-value end
    public string TagId { get; set; } = "";          // references TagDefinition.Id
    public string? CreatedBy { get; set; }
    public DateTimeOffset CreatedUtc { get; set; }
    public DateTimeOffset? ModifiedUtc { get; set; }
    public string? Memo { get; set; }
}
