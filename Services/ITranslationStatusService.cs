using ReadZen.App.Models;

namespace ReadZen.App.Services;

/// <summary>
/// Responsible for computing per-file translation status (Red/Yellow/Green)
/// by comparing original and translated XML files.
/// </summary>
public interface ITranslationStatusService
{
    TranslationStatus ComputeStatusForPairLive(
        string origAbs,
        string tranAbs,
        string rootForLogs,
        string relKeyForLogs,
        bool verboseLog = true);
}
