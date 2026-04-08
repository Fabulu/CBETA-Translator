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
}

public sealed class MasterDateConflict
{
    public string MasterName { get; set; } = "";
    public List<(string Username, int Floruit, int Death)> Entries { get; set; } = new();
}
