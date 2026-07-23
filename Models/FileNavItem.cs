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
    /// Cached last-write-time (UTC ticks) of the translated file (the resolved
    /// <see cref="TranResolvedPath"/>), used as the translated-side reuse hint
    /// for incremental status refresh — skip re-parse when size+mtime unchanged.
    /// </summary>
    public long TranslatedMtimeTicks { get; set; }

    /// <summary>
    /// Size in bytes of the original XML file at build time. Paired with
    /// <see cref="OrigMtimeTicks"/> as the original-side reuse hint: status is a
    /// pure function of the (orig, tran) byte pair, so a changed orig size/mtime
    /// forces a status recompute for this entry only.
    /// </summary>
    public long OrigSizeBytes { get; set; }

    /// <summary>
    /// Last-write-time (UTC ticks) of the original XML file at build time.
    /// Reuse hint paired with <see cref="OrigSizeBytes"/> (see that field).
    /// </summary>
    public long OrigMtimeTicks { get; set; }

    /// <summary>
    /// Size in bytes of the resolved translated file at build time (0 when no
    /// translation exists). Paired with <see cref="TranslatedMtimeTicks"/> as the
    /// translated-side reuse hint.
    /// </summary>
    public long TranSizeBytes { get; set; }

    /// <summary>
    /// The absolute path the community-fallback resolution actually chose for the
    /// translated file (canonical xml-p5t path when present, otherwise the first
    /// matching community/translations/{user}/ file, otherwise the canonical
    /// non-existent path). Pins WHICH file the status was computed from, so a
    /// community translation appearing or disappearing flips this and forces a
    /// recompute even when the canonical path's stats are unchanged.
    /// </summary>
    public string? TranResolvedPath { get; set; }
}
