using System.Threading;
using System.Threading.Tasks;

namespace ReadZen.App.Services;

public interface ICommunityDataService
{
    Task<int> SortAndDedupApprovedTmAsync(string root, CancellationToken ct = default);
    Task<int> MergeApprovedTmFromAsync(string localRoot, string upstreamTmPath, CancellationToken ct = default);
    Task<int> SortAndDedupTermbaseAsync(string root, CancellationToken ct = default);
    Task<int> MergeTermbaseFromAsync(string localRoot, string upstreamTermbasePath, CancellationToken ct = default);
    Task<int> SortAndDedupScholarCollectionsAsync(string root, CancellationToken ct = default);
    Task<int> MergeScholarCollectionsFromAsync(string localRoot, string upstreamPath, CancellationToken ct = default);
}
