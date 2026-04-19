using System.Threading;
using System.Threading.Tasks;

namespace ReadZen.App.Services;

public interface ITranslationStarService
{
    Task LoadAllStarsAsync(string communityStarsDir, CancellationToken ct);
    int GetStarCount(string fileId, string translator);
    string? GetMostStarredTranslator(string fileId);
    bool IsStarredByUser(string fileId, string translator, string username);
    Task SetStarAsync(string communityStarsDir, string username, string fileId, string translator, bool starred, CancellationToken ct);
    Task WriteUserStarsJsonlAsync(string communityStarsDir, string username, CancellationToken ct);
    Task ExportAggregatedCountsAsync(string repoDir, CancellationToken ct);
}
