using System.Threading;
using System.Threading.Tasks;
using CbetaTranslator.App.Models;

namespace CbetaTranslator.App.Services;

public interface ISearchExportService
{
    Task ExportAsync(string filePath, SearchExportSnapshot snapshot, SearchExportFormat format, CancellationToken ct = default);
}