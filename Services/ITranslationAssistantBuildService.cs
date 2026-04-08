using System;
using System.Threading;
using System.Threading.Tasks;
using ReadZen.App.Models;

namespace ReadZen.App.Services;

public interface ITranslationAssistantBuildService
{
    Task<bool> IsReferenceStaleAsync(string root, string translatedDir);

    Task<int> BuildReferenceTranslationMemoryAsync(
        string root,
        string originalDir,
        string translatedDir,
        Func<string, bool> isZen,
        IProgress<(int done, int total, string status)>? progress = null,
        CancellationToken ct = default);

    Task AppendApprovedEntryAsync(
        string root,
        CurrentSegmentContext ctx,
        string reviewStatus = "Approved",
        string translator = "User",
        CancellationToken ct = default);
}
