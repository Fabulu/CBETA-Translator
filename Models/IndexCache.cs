using System;
using System.Collections.Generic;

namespace ReadZen.App.Models;

public sealed class IndexCache
{
    public int Version { get; set; } = 4;

    public string RootPath { get; set; } = "";
    public DateTime BuiltUtc { get; set; } = DateTime.UtcNow;

    // v2
    public List<FileNavItem> Entries { get; set; } = new();

    public string? BuildGuid { get; set; }

    /// <summary>
    /// SHA256 (hex) of the shipped <c>titles.jsonl</c> bytes at build time.
    /// Display fields (DisplayShort/Tooltip) are derived from this file, so a
    /// change to it forces a wholesale rebuild — the only content input, beyond
    /// the per-entry (orig, tran) file stats, that the nav cache depends on.
    /// </summary>
    public string? TitlesHash { get; set; }

    /// <summary>
    /// RETIRED (v4): the git-HEAD freshness gate was replaced by the content
    /// gate (TitlesHash + per-entry file stats). Kept only for JSON
    /// deserialization tolerance of pre-v4 caches; never written or compared.
    /// </summary>
    public string? GitHead { get; set; }

    /// <summary>
    /// RETIRED (v4): see <see cref="GitHead"/>. Kept only for JSON
    /// deserialization tolerance; never written or compared.
    /// </summary>
    public string? OriginalsGitHead { get; set; }
}
