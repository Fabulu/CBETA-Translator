using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CbetaTranslator.App.Models;

namespace CbetaTranslator.App.Services;

public interface ITranslationReviewService
{
    Task AppendReviewAsync(
        string root,
        CurrentSegmentContext ctx,
        string status,
        string reviewer = "User",
        string? comment = null,
        CancellationToken ct = default);

    Task<Dictionary<string, TranslationReviewEntry>> LoadLatestEntriesAsync(
        string root,
        CancellationToken ct = default);

    Task<TranslationReviewEntry?> GetLatestEntryAsync(
        string root,
        CurrentSegmentContext ctx,
        CancellationToken ct = default);

    Task<int> RebuildApprovedTranslationMemoryAsync(
        string root,
        CancellationToken ct = default);

    Task WriteUserReviewJsonlAsync(string communityReviewsDir, string username, CancellationToken ct = default);

    Task RefreshAggregationCacheAsync(string root, string? communityReviewsDir, CancellationToken ct = default);

    SegmentReviewAggregation? GetAggregatedReview(string segmentKey);

    static string GetCommunityReviewsDir(string repoRoot) => Path.Combine(repoRoot, "community", "reviews");
}
