// Services/WitnessDownloadService.cs
// Downloads witness source files (PDFs, IIIF JPEGs) on demand with local caching.

using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using ReadZen.App.Models;

namespace ReadZen.App.Services;

/// <summary>
/// Downloads witness source files (PDFs from Wikimedia Commons, JPEGs from Kyoto IIIF)
/// on demand with local caching.
/// </summary>
public sealed class WitnessDownloadService
{
    private static readonly string CacheDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "ReadZen", "witness-cache");

    private static readonly HttpClient Http = new()
    {
        Timeout = TimeSpan.FromMinutes(30)
    };

    static WitnessDownloadService()
    {
        Directory.CreateDirectory(CacheDir);
    }

    /// <summary>
    /// Returns the local cached path for a witness file, or null if not cached.
    /// </summary>
    public static string? GetCachedPath(string witnessId, string? pageId = null)
    {
        // For PDFs: witness-cache/{witnessId}.pdf
        // For IIIF JPEGs: witness-cache/{witnessId}/{pageId}.jpg
        if (pageId != null)
        {
            var path = Path.Combine(CacheDir, witnessId, pageId + ".jpg");
            return File.Exists(path) ? path : null;
        }
        var pdfPath = Path.Combine(CacheDir, witnessId + ".pdf");
        return File.Exists(pdfPath) ? pdfPath : null;
    }

    /// <summary>
    /// Downloads a PDF from Wikimedia Commons.
    /// Returns the local cached path.
    /// </summary>
    public static async Task<string> DownloadCommonsPdfAsync(
        string witnessId, string url,
        Action<double>? progress = null,
        CancellationToken ct = default)
    {
        var localPath = Path.Combine(CacheDir, witnessId + ".pdf");
        if (File.Exists(localPath)) return localPath;

        var tmpPath = localPath + ".tmp";
        using var response = await Http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? -1;
        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var fileStream = File.Create(tmpPath);

        var buffer = new byte[81920];
        long downloaded = 0;
        int read;
        while ((read = await stream.ReadAsync(buffer, ct)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, read), ct);
            downloaded += read;
            if (totalBytes > 0)
                progress?.Invoke((double)downloaded / totalBytes);
        }

        fileStream.Close();
        File.Move(tmpPath, localPath, overwrite: true);
        return localPath;
    }

    /// <summary>
    /// Downloads a single IIIF page image from Kyoto University.
    /// URL pattern: https://rmda.kulib.kyoto-u.ac.jp/iiif/3/{recordId}%2F{imageFilename}_0.ptif/full/max/0/default.jpg
    /// Returns the local cached path.
    /// </summary>
    public static async Task<string> DownloadIiifPageAsync(
        string witnessId, string recordId, string imageFilename,
        Action<double>? progress = null,
        CancellationToken ct = default)
    {
        var dir = Path.Combine(CacheDir, witnessId);
        Directory.CreateDirectory(dir);
        var localPath = Path.Combine(dir, imageFilename + ".jpg");
        if (File.Exists(localPath)) return localPath;

        var url = $"https://rmda.kulib.kyoto-u.ac.jp/iiif/3/{recordId}%2F{imageFilename}_0.ptif/full/max/0/default.jpg";

        var tmpPath = localPath + ".tmp";
        using var response = await Http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);
        response.EnsureSuccessStatusCode();

        var totalBytes = response.Content.Headers.ContentLength ?? -1;
        using var stream = await response.Content.ReadAsStreamAsync(ct);
        using var fileStream = File.Create(tmpPath);

        var buffer = new byte[81920];
        long downloaded = 0;
        int read;
        while ((read = await stream.ReadAsync(buffer, ct)) > 0)
        {
            await fileStream.WriteAsync(buffer.AsMemory(0, read), ct);
            downloaded += read;
            if (totalBytes > 0)
                progress?.Invoke((double)downloaded / totalBytes);
        }

        fileStream.Close();
        File.Move(tmpPath, localPath, overwrite: true);
        return localPath;
    }

    /// <summary>
    /// Resolves the download source for a witness from manifest data.
    /// Returns (isIiif, url, recordId).
    /// </summary>
    public static (bool isIiif, string url, string? recordId) ResolveWitnessSource(ManifestInfo manifest, string witnessId)
    {
        if (manifest.Witnesses == null) return (false, "", null);

        foreach (var w in manifest.Witnesses)
        {
            if (w.Id == null || w.UpstreamUrl == null) continue;

            bool isMatch = MatchesWitness(w, witnessId);
            if (!isMatch) continue;

            bool isIiif = w.UpstreamUrl.Contains("rmda.kulib.kyoto-u.ac.jp", StringComparison.OrdinalIgnoreCase);
            string? recordId = null;
            if (isIiif)
            {
                // Extract record ID from manifest URL
                // e.g., https://rmda.kulib.kyoto-u.ac.jp/item/rb00009461 -> RB00009461
                var parts = w.UpstreamUrl.Split('/');
                recordId = parts[^1].ToUpperInvariant();
            }

            return (isIiif, w.UpstreamUrl, recordId);
        }

        return (false, "", null);
    }

    private static bool MatchesWitness(WitnessInfo w, string siglum)
    {
        // Direct ID matching for known patterns
        return siglum switch
        {
            "T1" => w.Id == "korea-commons",
            "T2" => w.Id == "ndl-2537640",
            "T3" => w.Id == "kyoto-rb00016909",
            "T4" => w.Id == "kyoto-rb00009461",
            "T5" => w.Id == "kyoto-rb00012929",
            "A1" => w.Id == "waseda-e1087",
            "A2" => w.Id == "waseda-e1089",
            "A3" => w.Id == "ndl-2537799",
            _ => false
        };
    }

    /// <summary>
    /// Downloads all witnesses for an edition. Reports progress per witness.
    /// </summary>
    public static async Task DownloadAllWitnessesAsync(
        ManifestInfo manifest,
        Action<string, double>? progress = null,
        CancellationToken ct = default)
    {
        var sigla = new[] { "T1", "T2", "T3", "T4", "T5", "A1", "A2", "A3" };

        foreach (var siglum in sigla)
        {
            ct.ThrowIfCancellationRequested();
            var (isIiif, url, recordId) = ResolveWitnessSource(manifest, siglum);
            if (string.IsNullOrEmpty(url)) continue;

            progress?.Invoke(siglum, 0);

            if (isIiif && recordId != null)
            {
                // For IIIF, download individual pages as needed
                // For batch, download first 5 pages as a preview
                for (int i = 1; i <= 5; i++)
                {
                    var filename = $"{recordId}_{i:D5}";
                    await DownloadIiifPageAsync(siglum, recordId, filename,
                        p => progress?.Invoke(siglum, (i - 1 + p) / 5.0), ct);
                }
            }
            else
            {
                // Commons PDF -- convert file page URL to direct download
                var downloadUrl = url;
                if (url.Contains("commons.wikimedia.org/wiki/File:"))
                {
                    var fileName = url.Split("File:")[^1];
                    downloadUrl = $"https://commons.wikimedia.org/wiki/Special:Redirect/file/{fileName}";
                }
                await DownloadCommonsPdfAsync(siglum, downloadUrl,
                    p => progress?.Invoke(siglum, p), ct);
            }

            progress?.Invoke(siglum, 1.0);
        }
    }
}
