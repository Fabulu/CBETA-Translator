// Services/CorpusDetector.cs
// Maps a text root path to a CorpusKind. Strategy (first successful step wins):
//   1. Sentinel file ".readzen-corpus" in the root — single line: "cbeta" or "open"
//   2. Folder-name heuristic: names containing "OpenZenTexts" => Open,
//      names containing "CbetaZenTexts" or "CBETA" => Cbeta
//   3. TEI sample: find the first *.xml under the originals dir, run the
//      license extractor, inspect Corpus hint
//   4. Unknown
using System;
using System.IO;
using System.Linq;
using ReadZen.App.Infrastructure;
using ReadZen.App.Models;

namespace ReadZen.App.Services;

public static class CorpusDetector
{
    public const string SentinelFileName = ".readzen-corpus";

    public static CorpusKind Detect(string? rootPath)
    {
        if (string.IsNullOrWhiteSpace(rootPath) || !Directory.Exists(rootPath))
            return CorpusKind.Unknown;

        // 1) sentinel — strip BOM and trim before comparing. Notepad on
        // Windows defaults to writing a UTF-8 BOM, which would otherwise
        // cause a silent classification miss for hand-edited sentinels.
        try
        {
            var sentinel = Path.Combine(rootPath, SentinelFileName);
            if (File.Exists(sentinel))
            {
                var raw = File.ReadAllText(sentinel);
                var line = raw.TrimStart('\uFEFF').Trim().ToLowerInvariant();
                if (line == "open" || line == "openzentexts") return CorpusKind.Open;
                if (line == "cbeta") return CorpusKind.Cbeta;
            }
        }
        catch { }

        // 2) folder-name heuristic — check root AND immediate subdirs.
        // The DirectoryInfo ctor can throw on paths with characters that
        // slipped past Directory.Exists (network paths, reserved names),
        // so the whole block lives inside a single try/catch.
        try
        {
            string rootName = new DirectoryInfo(rootPath).Name;
            if (MatchesOpen(rootName)) return CorpusKind.Open;
            if (MatchesCbeta(rootName)) return CorpusKind.Cbeta;

            foreach (var sub in Directory.EnumerateDirectories(rootPath))
            {
                string n = Path.GetFileName(sub);
                if (MatchesOpen(n)) return CorpusKind.Open;
                if (MatchesCbeta(n)) return CorpusKind.Cbeta;
            }
        }
        catch { }

        // 3) TEI sample. Sort the enumerated files so the choice is
        // deterministic across runs and platforms — Directory.EnumerateFiles
        // does not guarantee order, and a single oddly-labeled file in a
        // mixed root could otherwise decide the whole corpus arbitrarily.
        try
        {
            string originalDir = AppPaths.GetOriginalDir(rootPath);
            if (!string.IsNullOrEmpty(originalDir) && Directory.Exists(originalDir))
            {
                var sample = Directory.EnumerateFiles(originalDir, "*.xml", SearchOption.AllDirectories)
                    .OrderBy(p => p, StringComparer.OrdinalIgnoreCase)
                    .FirstOrDefault();
                if (sample != null)
                {
                    string xml = File.ReadAllText(sample);
                    var info = TextLicenseExtractor.Extract(xml);
                    if (info != null && info.Corpus != CorpusKind.Unknown)
                        return info.Corpus;
                }
            }
        }
        catch { }

        return CorpusKind.Unknown;
    }

    private static bool MatchesOpen(string name) =>
        name.Contains("OpenZenTexts", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("OpenZenTranslations", StringComparison.OrdinalIgnoreCase) ||
        name.StartsWith("xml-open", StringComparison.OrdinalIgnoreCase);

    private static bool MatchesCbeta(string name) =>
        name.Contains("CbetaZenTexts", StringComparison.OrdinalIgnoreCase) ||
        name.Contains("CbetaZenTranslations", StringComparison.OrdinalIgnoreCase) ||
        name.Equals("CBETA", StringComparison.OrdinalIgnoreCase);
}
