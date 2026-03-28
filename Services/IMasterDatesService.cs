using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CbetaTranslator.App.Models;

namespace CbetaTranslator.App.Services;

public interface IMasterDatesService
{
    Task WriteMasterDatesJsonlAsync(string communityDir, string username, List<MasterDateEntry> entries, CancellationToken ct = default);
    Task<Dictionary<string, List<MasterDateEntry>>> LoadAllCommunityMasterDatesAsync(string communityDir, CancellationToken ct = default);

    static string GetCommunityMasterDatesDir(string repoRoot) => System.IO.Path.Combine(repoRoot, "community", "master-dates");
}
