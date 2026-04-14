using System.Collections.Generic;

namespace ReadZen.App.Models;

/// <summary>
/// A single row in a code frequency report: one tag with its segment and file counts.
/// </summary>
public sealed record CodeFrequencyRow(
    string TagId,
    string TagName,
    string Color,
    int SegmentCount,
    int FileCount);

/// <summary>
/// A complete code frequency report containing rows for each tag.
/// </summary>
public sealed class CodeFrequencyReport
{
    public List<CodeFrequencyRow> Rows { get; set; } = new();
}
