using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CbetaTranslator.App.Models;

namespace CbetaTranslator.App.Services;

public interface ITermbaseStorageService
{
    Task<List<TermbaseEntry>> LoadAsync(string root, CancellationToken ct = default);
    Task SaveAsync(string root, IEnumerable<TermbaseEntry> entries, CancellationToken ct = default);
}
