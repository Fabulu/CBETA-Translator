using System.Collections.Generic;

namespace CbetaTranslator.App.Models;

public sealed class FileNavItem
{
    public string RelPath { get; set; } = "";
    public string FileName { get; set; } = "";
    public string DisplayShort { get; set; } = "";
    public string Tooltip { get; set; } = "";
    public TranslationStatus Status { get; set; } = TranslationStatus.Red;

    /// <summary>
    /// Cached last-write-time (UTC ticks) of the translated file, used for
    /// incremental status refresh — skip re-parse when mtime unchanged.
    /// </summary>
    public long TranslatedMtimeTicks { get; set; }
}
