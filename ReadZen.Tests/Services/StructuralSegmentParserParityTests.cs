using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

/// <summary>
/// Parity tests for the C# port of the segment-map generator (audit P3.1c / D5).
/// The goldens under TestData/segparity were produced by running the JS generator
/// (eng/tools/build-structural-segments.js) end-to-end on the committed fixtures;
/// the C# <see cref="StructuralSegmentParser.BuildJsonl"/> must reproduce them
/// byte-for-byte (same segmentation, same JSON key order and escaping, same
/// source_sha256 header, LF endings).
///
/// Regenerating goldens after a deliberate JS change:
///   stage fixtures as src/FX/*.xml + tran/FX/*.xml, run the JS with
///   --source-dir/--trans-dir/--out-dir, copy out/FX/*.segments.jsonl over the
///   *.golden.jsonl files (see the P3.1c commit message for the exact commands).
/// </summary>
[Trait("Domain", "Segmentation")]
public class StructuralSegmentParserParityTests
{
    private static string DataDir =>
        Path.Combine(AppContext.BaseDirectory, "TestData", "segparity");

    private static (string workId, string src, string tran, byte[] golden) LoadFixture(string stem)
    {
        var src = File.ReadAllText(Path.Combine(DataDir, stem + ".src.xml"));
        var tran = File.ReadAllText(Path.Combine(DataDir, stem + ".tran.xml"));
        var golden = File.ReadAllBytes(Path.Combine(DataDir, stem + ".golden.jsonl"));
        return (stem, src, tran, golden);
    }

    [Theory]
    [InlineData("fx1-t-kitchen-sink")] // nested containers, notes/apps, self-closing note, head skip, verse+caesura, secondary-ed lb, carried lbs, type detection
    [InlineData("fx2-zw-edition")]     // per-file edition detection (ZW), multi-lb segment
    [InlineData("fx3-noed-crlf")]      // no-ed lbs, CRLF source (hash normalization), quote/backslash escaping
    public void BuildJsonl_MatchesJsGeneratorGolden_ByteForByte(string stem)
    {
        var (workId, src, tran, golden) = LoadFixture(stem);

        var actual = Encoding.UTF8.GetBytes(StructuralSegmentParser.BuildJsonl(workId, src, tran));

        if (!golden.AsSpan().SequenceEqual(actual))
        {
            // Produce a readable first-difference diagnostic instead of a raw byte dump.
            var goldenText = Encoding.UTF8.GetString(golden);
            var actualText = Encoding.UTF8.GetString(actual);
            var gl = goldenText.Split('\n');
            var al = actualText.Split('\n');
            for (int i = 0; i < Math.Max(gl.Length, al.Length); i++)
            {
                var g = i < gl.Length ? gl[i] : "<missing>";
                var a = i < al.Length ? al[i] : "<missing>";
                Assert.True(g == a, $"{stem} line {i + 1} differs.\n golden: {g}\n actual: {a}");
            }
            Assert.Fail($"{stem}: same lines but different bytes (BOM/EOL?)");
        }
    }

    [Fact]
    public void SourceContentHash_MatchesTheSharedAnchors()
    {
        // Same anchors as eng/tools/test-structural-segments.js and SegmentMapServiceTests.
        Assert.Equal(
            "730c6fa790830ef1efbd219963d42c66be2aa49f8ba93a8f45430784b754068e",
            StructuralSegmentParser.SourceContentHash("<TEI><body><p>hi</p></body></TEI>"));
        Assert.Equal(
            StructuralSegmentParser.SourceContentHash("<TEI>\n<body><p>hi</p></body>\n</TEI>"),
            StructuralSegmentParser.SourceContentHash("<TEI>\r\n<body><p>hi</p></body>\r\n</TEI>"));
    }

    [Fact]
    public void JsEscape_MirrorsJsonStringify()
    {
        Assert.Equal("plain 漢字 unescaped", StructuralSegmentParser.JsEscape("plain 漢字 unescaped"));
        Assert.Equal("q\\\"q b\\\\b", StructuralSegmentParser.JsEscape("q\"q b\\b"));
        Assert.Equal("\\n\\r\\t\\b\\f\\u0001", StructuralSegmentParser.JsEscape("\n\r\t\b\f"));
    }

    /// <summary>
    /// Full-corpus parity sweep: runs the JS generator ONCE over the real corpus
    /// (source-dir = trans-dir = xml-p5, matching how the 4,990 committed maps were
    /// produced) and compares every produced jsonl against the C# port. Opt-in
    /// (slow, needs node + the local corpus): set RZ_SEGPARITY_SWEEP=1.
    /// Inert in normal gates.
    /// </summary>
    [Fact]
    public void FullCorpusSweep_WhenExplicitlyEnabled()
    {
        if (Environment.GetEnvironmentVariable("RZ_SEGPARITY_SWEEP") != "1")
            return; // opt-in only

        var sourceDir = Environment.GetEnvironmentVariable("RZ_SEGPARITY_SRC") ?? @"C:/Programmieren/CbetaZenTexts/xml-p5";
        var transDir = Environment.GetEnvironmentVariable("RZ_SEGPARITY_TRAN") ?? sourceDir;
        if (!Directory.Exists(sourceDir) || !Directory.Exists(transDir))
            return; // corpus not on this machine

        var outDir = Path.Combine(Path.GetTempPath(), "rz-segparity-" + Guid.NewGuid().ToString("N")[..8]);
        try
        {
            // One bulk JS run over the whole corpus.
            var repoRoot = FindRepoRoot();
            var psi = new System.Diagnostics.ProcessStartInfo("node")
            {
                WorkingDirectory = repoRoot,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            psi.ArgumentList.Add(Path.Combine(repoRoot, "eng", "tools", "build-structural-segments.js"));
            psi.ArgumentList.Add("--source-dir"); psi.ArgumentList.Add(sourceDir);
            psi.ArgumentList.Add("--trans-dir"); psi.ArgumentList.Add(transDir);
            psi.ArgumentList.Add("--out-dir"); psi.ArgumentList.Add(outDir);

            using (var p = System.Diagnostics.Process.Start(psi)!)
            {
                p.StandardOutput.ReadToEnd(); // drain to avoid pipe-full deadlock
                p.StandardError.ReadToEnd();
                Assert.True(p.WaitForExit(30 * 60_000), "JS generator did not finish in 30 min");
                Assert.Equal(0, p.ExitCode);
            }

            var failures = new List<string>();
            int compared = 0;

            foreach (var jsOut in Directory.EnumerateFiles(outDir, "*.segments.jsonl", SearchOption.AllDirectories))
            {
                var rel = Path.GetRelativePath(outDir, jsOut);
                var xmlRel = rel.Substring(0, rel.Length - ".segments.jsonl".Length) + ".xml";
                var sourcePath = Path.Combine(sourceDir, xmlRel);
                var transPath = Path.Combine(transDir, xmlRel);
                if (!File.Exists(sourcePath) || !File.Exists(transPath)) continue;

                var workId = Path.GetFileNameWithoutExtension(xmlRel);
                var expected = File.ReadAllText(jsOut);
                var actual = StructuralSegmentParser.BuildJsonl(
                    workId, File.ReadAllText(sourcePath), File.ReadAllText(transPath));
                compared++;

                if (expected != actual)
                    failures.Add(xmlRel);
            }

            Assert.True(compared > 0, "sweep enabled but nothing compared — corpus paths wrong?");
            Assert.True(failures.Count == 0,
                $"C#/JS divergence in {failures.Count}/{compared} files: {string.Join(", ", failures.GetRange(0, Math.Min(10, failures.Count)))}");
        }
        finally
        {
            try { Directory.Delete(outDir, recursive: true); } catch { }
        }
    }

    private static string FindRepoRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (dir != null && !File.Exists(Path.Combine(dir, "ReadZen.App.csproj")))
            dir = Path.GetDirectoryName(dir);
        return dir ?? throw new InvalidOperationException("repo root not found");
    }
}
