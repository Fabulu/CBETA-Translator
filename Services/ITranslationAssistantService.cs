using System.Threading;
using System.Threading.Tasks;
using CbetaTranslator.App.Models;

namespace CbetaTranslator.App.Services;

public interface ITranslationAssistantService
{
    Task<TranslationAssistantSnapshot> BuildSnapshotAsync(
        CurrentSegmentContext ctx,
        string? root,
        string? originalDir,
        string? translatedDir,
        CancellationToken ct = default);
}
