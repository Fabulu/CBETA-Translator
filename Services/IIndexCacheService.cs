using System;
using System.Threading;
using System.Threading.Tasks;
using ReadZen.App.Models;

namespace ReadZen.App.Services;

public interface IIndexCacheService
{
    string GetCachePath(string root);

    Task<IndexCache?> TryLoadAsync(string root, string? originalsRepoRoot = null);
    Task SaveAsync(string root, IndexCache cache, string? originalsRepoRoot = null);

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
