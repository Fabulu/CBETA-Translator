using System;
using System.Collections.Generic;

namespace ReadZen.App.Models;

public sealed class MasterDateEntry
{
    public List<string> Names { get; set; } = new();
    public int Floruit { get; set; }
    public int Death { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? WrittenUtc { get; set; }

    // New fields for zen master expansion
    public string? Notes { get; set; }
    public string? School { get; set; }
    public string? Teacher { get; set; }
    public List<string>? Students { get; set; }
    public string? Attestation { get; set; }
    public string? Region { get; set; }
    public string? ReferenceUrl { get; set; }
    public List<MasterLink>? Links { get; set; }
}

public sealed class MasterLink
{
    public string Label { get; set; } = "";
    public string Url { get; set; } = "";
}

public sealed class MasterDateConflict
{
    public string MasterName { get; set; } = "";
    public List<(string Username, int Floruit, int Death)> Entries { get; set; } = new();
}
