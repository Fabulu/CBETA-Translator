using System.Threading;
using System.Threading.Tasks;
using ReadZen.App.Models;

namespace ReadZen.App.Services;

public enum ScholarExportFormat { Json, Html, Markdown, PlainText, Csv, Tsv, ReaderTagBundle, ReaderTagTsv, BibTex, CslJson, PaperDraft, Ris }

public interface IScholarExportService
{
    Task ExportAsync(string filePath, ScholarCollection collection, ScholarExportFormat format, CancellationToken ct = default);
}

