using System;
using System.Threading;
using System.Threading.Tasks;
using CbetaTranslator.App.Models;

namespace CbetaTranslator.App.Services;

public interface IIndexCacheService
{
    string GetCachePath(string root);

    Task<IndexCache?> TryLoadAsync(string root);
    Task SaveAsync(string root, IndexCache cache);

    TranslationStatus ComputeStatusForPairLive(
        string origAbs,
        string tranAbs,
        string rootForLogs,
        string relKeyForLogs,
        bool verboseLog = true);

    Task<IndexCache> BuildAsync(
        string originalDir,
        string translatedDir,
        string root,
        IProgress<(int done, int total)>? progress = null,
        CancellationToken ct = default);
}
