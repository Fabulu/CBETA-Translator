// Infrastructure/CbetaReferenceHelper.cs
// Shared CBETA reference parsing and formatting utilities.
// Consolidates duplicated logic from CitationService and ScholarExportService.
// Both services delegate to these static methods; no duplication remains.

namespace ReadZen.App.Infrastructure;

/// <summary>
/// Shared CBETA reference parsing and formatting utilities used by both
/// CitationService and ScholarExportService. Eliminates prior duplication.
/// </summary>
public static class CbetaReferenceHelper
{
    /// <summary>
    /// Parse CBETA canon/volume/number from a FileId like "T48n2005".
    /// Canon = letter prefix(es), Volume = digits before 'n', Number = string after 'n'.
    /// Returns false if the FileId does not match the expected pattern.
    /// </summary>
    public static bool TryParseCbetaFromFileId(
        string? fileId,
        out string? canon,
        out int? volume,
        out string? number)
    {
        canon = null;
        volume = null;
        number = null;

        if (string.IsNullOrEmpty(fileId)) return false;

        // Match pattern like "T48n2005", "X70n1363", "J26nB180"
        // Canon = letter(s), Volume = digits before 'n', Number = string after 'n'
        int nIdx = fileId.IndexOf('n');
        if (nIdx < 2) return false; // need at least 1 canon char + 1 vol digit

        // Find where digits start (after canon letter(s))
        int volStart = 0;
        for (int i = 0; i < nIdx; i++)
        {
            if (char.IsDigit(fileId[i]))
            {
                volStart = i;
                break;
            }
        }
        if (volStart == 0) return false; // no canon prefix found

        var canonStr = fileId.Substring(0, volStart);
        var volStr = fileId.Substring(volStart, nIdx - volStart);
        var numStr = fileId.Substring(nIdx + 1);

        if (int.TryParse(volStr, out var vol) && !string.IsNullOrEmpty(numStr))
        {
            canon = canonStr;
            volume = vol;
            number = numStr;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Convert lb n-value "0292c18" to page reference "292c18".
    /// Strips leading zeros from the 4-digit page number.
    /// Returns null for null/empty or too-short inputs.
    /// </summary>
    public static string? FormatLbAsPageRef(string? lbValue)
    {
        if (string.IsNullOrEmpty(lbValue) || lbValue.Length < 5) return null;

        // Format: PPPP + column_letter + LL (e.g., 0292c18)
        // Page is first 4 chars, rest is column+line
        var pageStr = lbValue.Substring(0, 4);
        var rest = lbValue.Substring(4); // "c18"

        // Strip leading zeros from page
        var page = pageStr.TrimStart('0');
        if (page.Length == 0) page = "0";

        return page + rest;
    }

    /// <summary>
    /// Format a full CBETA canonical reference string.
    /// Example: "T no. 2005, 48: 292c18"
    /// Returns null when required fields (canon, volume, number) are absent.
    /// </summary>
    public static string? FormatCbetaReference(
        string? lbValue,
        string? canon,
        int? volume,
        string? number)
    {
        if (string.IsNullOrEmpty(canon) || !volume.HasValue || string.IsNullOrEmpty(number))
            return null;

        // Base reference without page: "T no. 2005, 48"
        var refBase = $"{canon} no. {number}, {volume.Value}";

        if (string.IsNullOrEmpty(lbValue)) return refBase;

        var pageRef = FormatLbAsPageRef(lbValue);
        if (pageRef == null) return refBase;

        return $"{refBase}: {pageRef}";
    }

    /// <summary>
    /// Escape special characters for BibTeX field values.
    /// Handles backslash, braces, and all line-ending variants.
    /// This is the canonical 5-replacement version (more complete than the
    /// 2-replacement version previously in CitationService).
    /// </summary>
    public static string EscapeBibTeX(string value)
    {
        return value
            .Replace("\\", "\\\\")
            .Replace("{", "\\{")
            .Replace("}", "\\}")
            .Replace("\r\n", " ")
            .Replace("\n", " ")
            .Replace("\r", " ");
    }

    /// <summary>
    /// Extract a FileId (e.g. "T48n2005") from a relative TEI XML path
    /// like "T/T48/T48n2005.xml". Returns the filename without extension.
    /// </summary>
    public static string ExtractFileIdFromRelPath(string? relPath)
    {
        if (string.IsNullOrEmpty(relPath))
            return string.Empty;

        var fileName = relPath;
        int lastSlash = relPath.LastIndexOfAny(new[] { '/', '\\' });
        if (lastSlash >= 0 && lastSlash < relPath.Length - 1)
            fileName = relPath[(lastSlash + 1)..];

        int dotIdx = fileName.LastIndexOf('.');
        if (dotIdx > 0)
            fileName = fileName[..dotIdx];

        return fileName;
    }
}
