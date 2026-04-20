using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ReadZen.App.Models;

namespace ReadZen.App.Services;

public interface ITranslationMemoryService
{
    Task<List<TranslationTmMatch>> FindApprovedMatchesAsync(
        CurrentSegmentContext ctx,
        string? root,
        string? translatedDir,
        CancellationToken ct = default,
        int maxResults = 8);

    Task<List<TranslationTmMatch>> FindReferenceMatchesAsync(
        CurrentSegmentContext ctx,
        string? root,
        string? translatedDir,
        CancellationToken ct = default,
        int maxResults = 8);

    /// <summary>Pre-loads TM files into cache so the first assistant lookup is instant.</summary>
    Task WarmupCacheAsync(string root, CancellationToken ct = default);
}
