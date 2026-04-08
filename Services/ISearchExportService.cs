using System.Threading;
using System.Threading.Tasks;
using ReadZen.App.Models;

namespace ReadZen.App.Services;

public interface ISearchExportService
{
    Task ExportAsync(string filePath, SearchExportSnapshot snapshot, SearchExportFormat format, CancellationToken ct = default);
}