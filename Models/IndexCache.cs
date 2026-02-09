using System;
using System.Collections.Generic;

namespace CbetaTranslator.App.Models;

public sealed class IndexCache
{
    public int Version { get; set; } = 2;

    public string RootPath { get; set; } = "";
    public DateTime BuiltUtc { get; set; } = DateTime.UtcNow;

    // v2
    public List<FileNavItem> Entries { get; set; } = new();

    public string? BuildGuid { get; set; }

}
