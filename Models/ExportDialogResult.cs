// Models/ExportDialogResult.cs
using ReadZen.App.Services;

namespace ReadZen.App.Models;

/// <summary>
/// Result returned by <see cref="Views.ExportFormatDialog"/> containing both the
/// chosen export format and the desired citation style for inline citations.
/// </summary>
public sealed record ExportDialogResult(ScholarExportFormat Format, CitationStyle CitationStyle);
