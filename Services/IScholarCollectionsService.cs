using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CbetaTranslator.App.Models;

namespace CbetaTranslator.App.Services;

public interface IScholarCollectionsService
{
    Task<List<ScholarCollection>> LoadAsync(string root, CancellationToken ct = default);
    Task SaveAsync(string root, List<ScholarCollection> collections, CancellationToken ct = default);
}
