// Services/SegmentMapGenerator.cs
//
// Batch runner around StructuralSegmentParser — the C# replacement for the retired
// eng/tools/build-structural-segments.js CLI (audit P3.1c 2/2, decision D5).
// Invoke via the app binary without starting the UI:
//
//   ReadZen.App --build-segments \
//       --source-dir C:/Programmieren/CbetaZenTexts/xml-p5 \
//       --trans-dir  C:/Programmieren/CbetaZenTexts/xml-p5 \
//       --out-dir    C:/Programmieren/CbetaZenTranslations/segments \
//       [--file T48n2005]
//
// Walks trans-dir for *.xml, pairs each with the same relative path under
// source-dir, and writes <out-dir>/<rel>.segments.jsonl (with the source_sha256
// staleness header). Prints the per-collection empty-lb_range coverage metric the
// JS runner introduced in P3.1a. Note the 4,990 committed corpus maps were produced
// with trans-dir pointed at the SOURCE corpus itself (every file gets a map).

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace ReadZen.App.Services;

public static class SegmentMapGenerator
{
    /// <summary>
    /// CLI entry (called from Program.Main for <c>--build-segments</c>).
    /// Returns a process exit code: 0 = success, 1 = bad arguments / nothing done.
    /// </summary>
    public static int Run(string[] args, TextWriter log)
    {
        string? Arg(string name)
        {
            var i = Array.IndexOf(args, "--" + name);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        }

        var sourceDir = Arg("source-dir");
        var transDir = Arg("trans-dir");
        var outDir = Arg("out-dir");
        var singleFile = Arg("file");

        if (sourceDir == null || transDir == null || outDir == null)
        {
            log.WriteLine("usage: --build-segments --source-dir <dir> --trans-dir <dir> --out-dir <dir> [--file <workId>]");
            return 1;
        }
        if (!Directory.Exists(sourceDir) || !Directory.Exists(transDir))
        {
            log.WriteLine($"error: source-dir or trans-dir does not exist");
            return 1;
        }

        var stats = GenerateAll(sourceDir, transDir, outDir, singleFile, log);
        return stats.Processed > 0 ? 0 : 1;
    }

    public sealed record GenerationStats(int Processed, int Segments, int EmptyLbSegments);

    /// <summary>
    /// Processes every source/translation pair and writes the segment maps.
    /// Same walk order, skip behavior, per-file log lines, and end-of-run
    /// empty-lb_range coverage metric as the retired JS runner.
    /// </summary>
    public static GenerationStats GenerateAll(
        string sourceDir, string transDir, string outDir, string? singleFile, TextWriter log)
    {
        log.WriteLine("Structural segmentation: grouping <lb> markers by <p>/<lg> parents");
        log.WriteLine($"  Source:  {sourceDir}");
        log.WriteLine($"  Trans:   {transDir}");
        log.WriteLine($"  Output:  {outDir}");
        log.WriteLine();

        int processed = 0;
        var byCollection = new Dictionary<string, (int Segments, int EmptyLb, int Files)>(StringComparer.Ordinal);

        foreach (var transPath in Directory.EnumerateFiles(transDir, "*.xml", SearchOption.AllDirectories))
        {
            var rel = Path.GetRelativePath(transDir, transPath);
            var workId = Path.GetFileNameWithoutExtension(transPath);

            if (singleFile != null && workId != singleFile) continue;

            var sourcePath = Path.Combine(sourceDir, rel);
            if (!File.Exists(sourcePath))
            {
                log.WriteLine($"  SKIP {workId}: no source at {sourcePath}");
                continue;
            }

            var outRel = Path.ChangeExtension(rel, null) + ".segments.jsonl";
            var outPath = Path.Combine(outDir, outRel);

            try
            {
                var sourceXml = File.ReadAllText(sourcePath);
                var transXml = File.ReadAllText(transPath);

                var jsonl = StructuralSegmentParser.BuildJsonl(workId, sourceXml, transXml);
                Directory.CreateDirectory(Path.GetDirectoryName(outPath)!);
                // LF endings, UTF-8 no BOM — byte-identical to the retired JS output.
                File.WriteAllText(outPath, jsonl, new System.Text.UTF8Encoding(false));
                processed++;

                // Per-file log + collection metric (counts derived from the emitted jsonl)
                int segCount = 0, emptyLb = 0, lbTotal = 0;
                foreach (var line in jsonl.Split('\n'))
                {
                    if (!line.StartsWith("{\"unit_id\"", StringComparison.Ordinal)) continue;
                    segCount++;
                    if (line.Contains("\"lb_range\":[]", StringComparison.Ordinal)) emptyLb++;
                    else lbTotal += CountLbs(line);
                }
                var emptyNote = emptyLb > 0 ? $", {emptyLb} empty-lb" : "";
                log.WriteLine($"  {workId}: {segCount} segments, {lbTotal} lbs{emptyNote} -> {outPath}");

                var collection = rel.Replace('\\', '/').Split('/')[0];
                byCollection.TryGetValue(collection, out var acc); // default = zeros
                byCollection[collection] = (acc.Segments + segCount, acc.EmptyLb + emptyLb, acc.Files + 1);
            }
            catch (Exception ex)
            {
                log.WriteLine($"  ERROR {workId}: {ex.Message}");
            }
        }

        log.WriteLine();
        log.WriteLine($"Done: {processed} files processed.");

        int totalSeg = byCollection.Values.Sum(v => v.Segments);
        int totalEmpty = byCollection.Values.Sum(v => v.EmptyLb);
        if (totalSeg > 0)
        {
            var pct = (totalEmpty * 100.0 / totalSeg).ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
            log.WriteLine();
            log.WriteLine($"Empty lb_range coverage: {totalEmpty}/{totalSeg} segments ({pct}%) across {processed} files");
            foreach (var col in byCollection.Keys.OrderBy(k => k, StringComparer.Ordinal))
            {
                var (seg, empty, files) = byCollection[col];
                var cpct = seg > 0 ? (empty * 100.0 / seg).ToString("F2", System.Globalization.CultureInfo.InvariantCulture) : "0.00";
                var flag = empty > 0 ? "  <-- has empty lb_range" : "";
                log.WriteLine($"  {col,-6} {empty}/{seg} ({cpct}%) in {files} files{flag}");
            }
        }

        return new GenerationStats(processed, totalSeg, totalEmpty);
    }

    private static int CountLbs(string jsonLine)
    {
        // lb_range":["a","b"] — count elements without a JSON parse.
        var start = jsonLine.IndexOf("\"lb_range\":[", StringComparison.Ordinal);
        if (start < 0) return 0;
        start += "\"lb_range\":[".Length;
        var end = jsonLine.IndexOf(']', start);
        if (end <= start) return 0;
        var inner = jsonLine.Substring(start, end - start);
        return inner.Length == 0 ? 0 : inner.Count(c => c == ',') + 1;
    }
}
