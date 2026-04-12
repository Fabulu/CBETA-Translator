using System;
using System.Collections.Generic;

namespace ReadZen.App.Models;

public sealed class IndexCache
{
    public int Version { get; set; } = 3;

    public string RootPath { get; set; } = "";
    public DateTime BuiltUtc { get; set; } = DateTime.UtcNow;

    // v2
    public List<FileNavItem> Entries { get; set; } = new();

    public string? BuildGuid { get; set; }

    /// <summary>
    /// SHA of the corpus translations repo's HEAD commit at the time the
    /// cache was built. Compared against the live HEAD on every load — when
    /// they differ, the cache is treated as stale and rebuilt from disk.
    /// This is the load-bearing invalidation signal for the "user synced
    /// the corpus and new files appeared" case. Null when the corpus root
    /// is not a git repo (manual file dump, dev sandbox, etc.) — in that
    /// situation we don't gate on it.
    /// </summary>
    public string? GitHead { get; set; }
}
