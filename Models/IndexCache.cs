using System;
using System.Collections.Generic;

namespace ReadZen.App.Models;

public sealed class IndexCache
{
    // v5 (NAV_CACHE_REDESIGN §2): bundleable, machine-independent nav cache.
    public int Version { get; set; } = 5;

    /// <summary>
    /// DEMOTED (v5, NAV_CACHE_REDESIGN §2.1): informational/diagnostic only. NEVER
    /// compared on load — a foreign-root cache is adopted and re-homed by simply saving
    /// with the local root. Kept so diagnostics can show where a cache was built.
    /// </summary>
    public string RootPath { get; set; } = "";
    public DateTime BuiltUtc { get; set; } = DateTime.UtcNow;

    // v2
    public List<FileNavItem> Entries { get; set; } = new();

    public string? BuildGuid { get; set; }

    /// <summary>
    /// SHA256 (hex) of the shipped <c>titles.jsonl</c> bytes at build time. Display fields
    /// (DisplayShort/Tooltip) derive from this file. In v5 a mismatch re-derives display
    /// fields ONLY and keeps statuses (NAV_CACHE_REDESIGN §3.4) — no longer a full rebuild.
    /// </summary>
    public string? TitlesHash { get; set; }

    /// <summary>
    /// NEW (v5, NAV_CACHE_REDESIGN §2.1): which corpus this cache describes
    /// ("Cbeta"/"Open"). Adoption refuses a kind mismatch (PR-NV5).
    /// </summary>
    public string? CorpusKind { get; set; }

    /// <summary>
    /// NEW (v5, NAV_CACHE_REDESIGN §2.1): <c>"files={N};bytes={SUM};pathsig={P16}"</c> over
    /// the originals dir — the master corpus-stamp recipe. Stat-only, mtime-immune.
    /// Fast-path accelerator: equal ⇒ no original appeared/changed/vanished.
    /// </summary>
    public string? OriginalsSig { get; set; }

    /// <summary>
    /// NEW (v5, NAV_CACHE_REDESIGN §2.1): hash over the sorted
    /// <c>"{token}|{relKey}|{contentSig}"</c> lines of the translation-source manifest
    /// (§3.1). Equal ⇒ no translation appeared/changed/vanished anywhere (canonical +
    /// every community user). Content-based, so mtime-immune.
    /// </summary>
    public string? SourceSig { get; set; }

    /// <summary>
    /// RETIRED (v4): the git-HEAD freshness gate was replaced by the content gate. Kept
    /// only for JSON deserialization tolerance; never written or compared.
    /// </summary>
    public string? GitHead { get; set; }

    /// <summary>
    /// RETIRED (v4): see <see cref="GitHead"/>. Kept only for JSON deserialization
    /// tolerance; never written or compared.
    /// </summary>
    public string? OriginalsGitHead { get; set; }
}
