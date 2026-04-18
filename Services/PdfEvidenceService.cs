// Services/PdfEvidenceService.cs
// Renders PDF page regions as Avalonia Bitmaps for witness evidence viewing.
// Uses Docnet.Core (Pdfium wrapper) for cross-platform PDF rendering.

using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Media.Imaging;
using Docnet.Core;
using Docnet.Core.Models;

namespace ReadZen.App.Services;

/// <summary>
/// Renders PDF pages (or cropped regions) into Avalonia Bitmaps for inline
/// evidence display. Caches rendered raw page data in memory so repeated
/// requests for the same page are instant.
/// </summary>
public sealed class PdfEvidenceService : IDisposable
{
    /// <summary>Cached raw BGRA pixel data for a rendered page.</summary>
    private sealed record RawPage(byte[] Bgra, int Width, int Height);

    private readonly Dictionary<(string Path, int Page), RawPage> _cache = new();
    private readonly IDocLib _docLib;

    /// <summary>
    /// Local directory for downloaded PDF files (outside git repo).
    /// Defaults to {LocalApplicationData}/ReadZen/pdf-cache/.
    /// </summary>
    public string PdfCacheDir { get; }

    public PdfEvidenceService()
    {
        _docLib = DocLib.Instance;
        PdfCacheDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "ReadZen", "pdf-cache");
        Directory.CreateDirectory(PdfCacheDir);
    }

    /// <summary>
    /// Renders a region of a PDF page as an Avalonia Bitmap.
    /// Region coordinates are percentages (0.0-1.0) of the full page.
    /// Applies generous padding (10% on each side) so the user sees context.
    /// </summary>
    /// <param name="pdfPath">Absolute path to the PDF file.</param>
    /// <param name="pageNumber">Zero-based page index.</param>
    /// <param name="regionY">Top edge of region as fraction of page height (0.0-1.0).</param>
    /// <param name="regionHeight">Height of region as fraction of page height (0.0-1.0).</param>
    /// <param name="regionX">Left edge of region as fraction of page width (0.0-1.0).</param>
    /// <param name="regionWidth">Width of region as fraction of page width (0.0-1.0).</param>
    /// <param name="dpi">Render resolution. Default 150 balances quality and speed.</param>
    /// <returns>Bitmap of the requested region, or null if the file/page cannot be read.</returns>
    public Bitmap? RenderPageRegion(
        string pdfPath,
        int pageNumber,
        double regionY = 0,
        double regionHeight = 1.0,
        double regionX = 0,
        double regionWidth = 1.0,
        int dpi = 150)
    {
        if (!File.Exists(pdfPath) || pageNumber < 0)
            return null;

        try
        {
            // Get or render the full page as raw pixel data
            var key = (pdfPath, pageNumber);
            if (!_cache.TryGetValue(key, out var rawPage))
            {
                rawPage = RenderFullPageRaw(pdfPath, pageNumber, dpi);
                if (rawPage == null)
                    return null;
                _cache[key] = rawPage;
            }

            // If requesting the full page, return as-is
            if (regionX <= 0 && regionY <= 0 && regionWidth >= 1.0 && regionHeight >= 1.0)
                return RawPageToBitmap(rawPage);

            // Crop with generous padding (10% on each side, clamped to page bounds)
            const double padding = 0.10;
            double x0 = Math.Max(0, regionX - padding);
            double y0 = Math.Max(0, regionY - padding);
            double x1 = Math.Min(1.0, regionX + regionWidth + padding);
            double y1 = Math.Min(1.0, regionY + regionHeight + padding);

            int pw = rawPage.Width;
            int ph = rawPage.Height;

            int cropX = Math.Clamp((int)(x0 * pw), 0, pw - 1);
            int cropY = Math.Clamp((int)(y0 * ph), 0, ph - 1);
            int cropW = Math.Max(1, Math.Min((int)((x1 - x0) * pw), pw - cropX));
            int cropH = Math.Max(1, Math.Min((int)((y1 - y0) * ph), ph - cropY));

            // Extract cropped BGRA region
            int srcStride = rawPage.Width * 4;
            int cropStride = cropW * 4;
            var cropPixels = new byte[cropStride * cropH];
            for (int row = 0; row < cropH; row++)
            {
                Array.Copy(rawPage.Bgra, (cropY + row) * srcStride + cropX * 4,
                           cropPixels, row * cropStride, cropStride);
            }

            return RawPageToBitmap(new RawPage(cropPixels, cropW, cropH));
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Returns the highlight rectangle coordinates (in pixels) for overlaying
    /// on the rendered page. Accounts for the same padding as RenderPageRegion.
    /// </summary>
    public (int X, int Y, int Width, int Height)? GetHighlightRect(
        int renderedWidth, int renderedHeight,
        double regionX, double regionY,
        double regionWidth, double regionHeight)
    {
        const double padding = 0.10;
        double x0 = Math.Max(0, regionX - padding);
        double y0 = Math.Max(0, regionY - padding);
        double totalW = Math.Min(1.0, regionX + regionWidth + padding) - x0;
        double totalH = Math.Min(1.0, regionY + regionHeight + padding) - y0;

        if (totalW <= 0 || totalH <= 0) return null;

        // The highlight rectangle relative to the cropped image
        double relX = (regionX - x0) / totalW;
        double relY = (regionY - y0) / totalH;
        double relW = regionWidth / totalW;
        double relH = regionHeight / totalH;

        return (
            (int)(relX * renderedWidth),
            (int)(relY * renderedHeight),
            (int)(relW * renderedWidth),
            (int)(relH * renderedHeight));
    }

    /// <summary>
    /// Downloads a PDF from a URL (typically Wikimedia Commons) to the local cache.
    /// Verifies the SHA-256 hash if provided.
    /// </summary>
    /// <param name="url">Download URL.</param>
    /// <param name="localFileName">Filename to save as in the cache dir.</param>
    /// <param name="expectedSha256">Optional SHA-256 hash to verify (hex string).</param>
    /// <param name="progress">Optional progress callback (0.0-1.0).</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Absolute path to the cached file, or null on failure.</returns>
    public async Task<string?> DownloadPdfAsync(
        string url,
        string localFileName,
        string? expectedSha256 = null,
        Action<double>? progress = null,
        CancellationToken ct = default)
    {
        var destPath = Path.Combine(PdfCacheDir, localFileName);
        if (File.Exists(destPath))
        {
            // Already cached -- optionally verify hash
            if (expectedSha256 != null && !VerifySha256(destPath, expectedSha256))
            {
                File.Delete(destPath);
            }
            else
            {
                return destPath;
            }
        }

        try
        {
            using var http = new HttpClient();
            http.Timeout = TimeSpan.FromMinutes(30);

            using var response = await http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
            response.EnsureSuccessStatusCode();

            var totalBytes = response.Content.Headers.ContentLength ?? -1;
            var tempPath = destPath + ".tmp";

            using (var contentStream = await response.Content.ReadAsStreamAsync(ct))
            using (var fs = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920))
            {
                var buffer = new byte[81920];
                long downloaded = 0;
                int bytesRead;
                while ((bytesRead = await contentStream.ReadAsync(buffer, ct)) > 0)
                {
                    await fs.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
                    downloaded += bytesRead;
                    if (totalBytes > 0)
                        progress?.Invoke((double)downloaded / totalBytes);
                }
            }

            // Verify hash if provided
            if (expectedSha256 != null && !VerifySha256(tempPath, expectedSha256))
            {
                File.Delete(tempPath);
                return null;
            }

            File.Move(tempPath, destPath, overwrite: true);
            return destPath;
        }
        catch (OperationCanceledException) { throw; }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Resolves a PDF path: if the file exists at the given path, returns it;
    /// otherwise checks the local cache directory.
    /// </summary>
    public string? ResolvePdfPath(string pdfPathOrName)
    {
        if (File.Exists(pdfPathOrName))
            return pdfPathOrName;

        var cached = Path.Combine(PdfCacheDir, Path.GetFileName(pdfPathOrName));
        return File.Exists(cached) ? cached : null;
    }

    /// <summary>Clears all cached rendered page data from memory.</summary>
    public void ClearCache()
    {
        _cache.Clear();
    }

    public void Dispose()
    {
        ClearCache();
    }

    // -- Private helpers --------------------------------------------------

    /// <summary>Renders a full PDF page to raw BGRA pixel data.</summary>
    private RawPage? RenderFullPageRaw(string pdfPath, int pageNumber, int dpi)
    {
        try
        {
            var dims = new PageDimensions(dpi * 10, dpi * 14); // approximate A4 at target DPI
            using var reader = _docLib.GetDocReader(pdfPath, dims);
            if (pageNumber >= reader.GetPageCount())
                return null;

            using var pageReader = reader.GetPageReader(pageNumber);
            var rawBytes = pageReader.GetImage(); // BGRA pixel data
            int width = pageReader.GetPageWidth();
            int height = pageReader.GetPageHeight();

            if (rawBytes == null || rawBytes.Length == 0 || width <= 0 || height <= 0)
                return null;

            return new RawPage(rawBytes, width, height);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Converts raw BGRA page data to an Avalonia Bitmap via BMP encoding.</summary>
    private static Bitmap RawPageToBitmap(RawPage page)
    {
        using var ms = new MemoryStream();
        WriteBmpFile(ms, page.Bgra, page.Width, page.Height);
        ms.Position = 0;
        return new Bitmap(ms);
    }

    /// <summary>
    /// Writes raw BGRA pixel data as a minimal BMP file for Avalonia to decode.
    /// </summary>
    private static void WriteBmpFile(Stream output, byte[] bgra, int width, int height)
    {
        int rowSize = width * 4;
        int imageSize = rowSize * height;
        int fileSize = 54 + imageSize;

        using var bw = new BinaryWriter(output, System.Text.Encoding.UTF8, leaveOpen: true);
        // BMP file header (14 bytes)
        bw.Write((byte)'B'); bw.Write((byte)'M');
        bw.Write(fileSize);
        bw.Write(0); // reserved
        bw.Write(54); // pixel data offset

        // DIB header (BITMAPINFOHEADER, 40 bytes)
        bw.Write(40); // header size
        bw.Write(width);
        bw.Write(-height); // top-down
        bw.Write((short)1); // planes
        bw.Write((short)32); // bpp
        bw.Write(0); // no compression
        bw.Write(imageSize);
        bw.Write(3780); // ~96 DPI horizontal
        bw.Write(3780);
        bw.Write(0); // colors
        bw.Write(0);

        // Pixel data (already BGRA, which matches BMP's BGRX with alpha)
        bw.Write(bgra, 0, imageSize);
    }

    private static bool VerifySha256(string filePath, string expectedHex)
    {
        try
        {
            using var fs = File.OpenRead(filePath);
            var hash = SHA256.HashData(fs);
            var hex = Convert.ToHexString(hash);
            return string.Equals(hex, expectedHex, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}
