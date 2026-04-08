using System.Threading;
using System.Threading.Tasks;
using ReadZen.App.Models;

namespace ReadZen.App.Services;

public interface ITranslationAssistantService
{
    /// <summary>
    /// Sets the current username so the termbase service resolves the per-user file.
    /// </summary>
    void SetUsername(string? username);

    Task<TranslationAssistantSnapshot> BuildSnapshotAsync(
        CurrentSegmentContext ctx,
        string? root,
        string? originalDir,
        string? translatedDir,
        CancellationToken ct = default);
}
