using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ReadZen.App.Models;

namespace ReadZen.App.Services;

public interface ITermbaseService
{
    Task<List<TermHit>> FindTermsAsync(
        CurrentSegmentContext ctx,
        string? root,
        CancellationToken ct = default);

    Task<List<TermHit>> FindCommunityTermsAsync(
        CurrentSegmentContext ctx,
        string? root,
        CancellationToken ct = default);

    /// <summary>Pre-loads termbase files into cache so the first assistant lookup is instant.</summary>
    Task WarmupCacheAsync(string root, CancellationToken ct = default);

    /// <summary>Returns all terms from the termbase as display items for picker dialogs.</summary>
    Task<List<TermHit>> GetAllTermsAsync(string? root, CancellationToken ct = default);
}
