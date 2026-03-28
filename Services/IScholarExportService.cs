using System.Threading;
using System.Threading.Tasks;
using CbetaTranslator.App.Models;

namespace CbetaTranslator.App.Services;

public enum ScholarExportFormat { Html, Markdown, PlainText }

public interface IScholarExportService
{
    Task ExportAsync(string filePath, ScholarCollection collection, ScholarExportFormat format, CancellationToken ct = default);
}
