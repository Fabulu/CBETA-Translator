using System.Collections.Generic;

namespace ReadZen.App.Models;

public sealed class FileNavItem
{
    public string RelPath { get; set; } = "";
    public string FileName { get; set; } = "";
    public string DisplayShort { get; set; } = "";
    public string Tooltip { get; set; } = "";
    public TranslationStatus Status { get; set; } = TranslationStatus.Red;

    /// <summary>
    /// v5 (NAV_CACHE_REDESIGN §2.2): the per-candidate translation-source records for
    /// this original — one per translated file that exists for this rel (usually 0,
    /// occasionally 1-2). <see cref="Status"/> is defined as the max over these records.
    /// This is the ONLY translation-side gate input in v5: an entry is reused iff
    /// <see cref="OrigSizeBytes"/> is unchanged AND the live candidate
    /// {(Token, ContentSig)} set equals this stored set. Each record's
    /// <see cref="NavSourceRecord.Status"/> is the per-candidate verdict (meaningfulness
    /// folded in), PERSISTED keyed by content so a given (orig, tran-content) pair is
    /// evaluated at most once ever per machine.
    /// </summary>
    public List<NavSourceRecord> Sources { get; set; } = new();

    /// <summary>
    /// v5 (NAV_CACHE_REDESIGN §2.2): optional per-candidate local (size, mtime)
    /// accelerator hints, keyed by Token. NOT part of the gate's truth — when a hint
    /// matches the live file the source scan skips re-hashing that candidate; on a miss
    /// (e.g. after a clone rewrites mtimes) it re-hashes (cheap) and heals the hint. A
    /// bundle ships these from the CI machine; the first local launch re-hashes and heals.
    /// </summary>
    public List<NavSourceHint>? TranLocalHints { get; set; }

    /// <summary>
    /// LEGACY (v4): translated-file last-write-time (UTC ticks). NOT a v5 gate input. The
    /// full-corpus sweep that read/wrote it was removed in PR-NV4; v5 BuildEntry still
    /// populates it (from the chosen candidate) for JSON tolerance and diagnostics, and
    /// RefreshFileStatusAsync keeps it coherent on a save. The v5 content gate ignores it.
    /// </summary>
    public long TranslatedMtimeTicks { get; set; }

    /// <summary>
    /// Size in bytes of the original XML file at build time. The ONLY original-side gate
    /// input in v5 (size alone is a valid content proxy — CBETA originals are read-only,
    /// NAV_CACHE_REDESIGN §2.2 / §9.1). A changed original size forces a recompute for
    /// this entry only.
    /// </summary>
    public long OrigSizeBytes { get; set; }

    /// <summary>
    /// LEGACY (v4): original-file last-write-time (UTC ticks). NOT a v5 gate input
    /// (mtime-immune by design). Kept for JSON tolerance; v5 BuildEntry still populates it.
    /// </summary>
    public long OrigMtimeTicks { get; set; }

    /// <summary>
    /// LEGACY (v4): size in bytes of the resolved translated file. NOT a v5 gate input
    /// (subsumed by <see cref="Sources"/>[].ContentSig). Kept for JSON tolerance; v5
    /// BuildEntry still populates it from the chosen candidate.
    /// </summary>
    public long TranSizeBytes { get; set; }

    /// <summary>
    /// LEGACY (v4): the absolute path the community-fallback resolution chose. This is
    /// machine-bound, so v5 NO LONGER populates it (left null) to keep the serialized
    /// cache machine-independent — the resolution is expressed instead by
    /// <see cref="Sources"/>[].Token. The property is retained only for JSON-deserialization
    /// tolerance of pre-v5 caches and for the runtime sweep, which never reads it.
    /// </summary>
    public string? TranResolvedPath { get; set; }
}

/// <summary>
/// v5 per-candidate translation-source record (NAV_CACHE_REDESIGN §2.2). <see cref="Token"/>
/// is a RELATIVE source id — <c>"canonical"</c> (xml-p5t / xml-open-t) or
/// <c>"user:{username}"</c> (community/translations/{username}) — resolved to an absolute
/// path at use time. <see cref="ContentSig"/> is SHA256-16 of the candidate file bytes.
/// <see cref="Status"/> is the per-candidate verdict with meaningfulness folded in.
/// </summary>
public sealed class NavSourceRecord
{
    public string Token { get; set; } = "";
    public string ContentSig { get; set; } = "";
    public TranslationStatus Status { get; set; } = TranslationStatus.Red;
}

/// <summary>
/// v5 per-candidate local accelerator hint (NAV_CACHE_REDESIGN §2.2). (SizeBytes,
/// MtimeTicks) of the candidate file on THIS machine, keyed by <see cref="Token"/>. A
/// hint hit lets the source scan skip re-hashing; a miss triggers a cheap re-hash + heal.
/// </summary>
public sealed class NavSourceHint
{
    public string Token { get; set; } = "";
    public long SizeBytes { get; set; }
    public long MtimeTicks { get; set; }
}
