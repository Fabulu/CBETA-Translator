using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

/// <summary>
/// Format-contract tests for the segment-map generator (audit P3.1c / D5).
/// The goldens under TestData/segparity were produced by the now-RETIRED JS
/// generator (eng/tools/build-structural-segments.js, deleted in P3.1c 2/2) running
/// end-to-end on the committed fixtures; before retirement the C# port was verified
/// byte-identical against a fresh JS run over all 4,990 corpus files (zero
/// divergence — see RUN-20260704-1141 TASK_LOG, 2026-07-08). The goldens are
/// therefore FROZEN: they pin the on-disk jsonl format (segmentation, JSON key
/// order, JSON.stringify-style escaping, source_sha256 header, LF endings) that the
/// 4,990 committed corpus maps use. A deliberate format change requires
/// regenerating both the goldens AND the corpus maps together.
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

    // NOTE: an env-gated full-corpus sweep (fresh JS run vs BuildJsonl over all 4,990
    // files) lived here during the port; it PASSED with zero divergence on 2026-07-08
    // and was removed together with the JS generator it invoked (P3.1c 2/2).

    /// <summary>
    /// End-to-end run of the C# batch generator over the committed fixtures: the
    /// files it writes must equal the frozen goldens byte-for-byte (exercises the
    /// directory walk + file I/O around BuildJsonl, i.e. the retired JS main()).
    /// </summary>
    [Fact]
    public void GenerateAll_OverFixtures_WritesGoldenBytes()
    {
        var stage = Path.Combine(Path.GetTempPath(), "rz-seggen-" + Guid.NewGuid().ToString("N")[..8]);
        try
        {
            foreach (var sub in new[] { "src/FX", "tran/FX" })
                Directory.CreateDirectory(Path.Combine(stage, sub));
            foreach (var stem in new[] { "fx1-t-kitchen-sink", "fx2-zw-edition", "fx3-noed-crlf" })
            {
                File.Copy(Path.Combine(DataDir, stem + ".src.xml"), Path.Combine(stage, "src", "FX", stem + ".xml"));
                File.Copy(Path.Combine(DataDir, stem + ".tran.xml"), Path.Combine(stage, "tran", "FX", stem + ".xml"));
            }

            using var log = new StringWriter();
            var stats = SegmentMapGenerator.GenerateAll(
                Path.Combine(stage, "src"), Path.Combine(stage, "tran"), Path.Combine(stage, "out"),
                singleFile: null, log);

            Assert.Equal(3, stats.Processed);
            foreach (var stem in new[] { "fx1-t-kitchen-sink", "fx2-zw-edition", "fx3-noed-crlf" })
            {
                var written = File.ReadAllBytes(Path.Combine(stage, "out", "FX", stem + ".segments.jsonl"));
                var golden = File.ReadAllBytes(Path.Combine(DataDir, stem + ".golden.jsonl"));
                Assert.True(golden.AsSpan().SequenceEqual(written), $"{stem}: generator output differs from golden");
            }
            // The coverage metric is printed (fx1 has empty-lb segments).
            Assert.Contains("Empty lb_range coverage:", log.ToString());
        }
        finally
        {
            try { Directory.Delete(stage, recursive: true); } catch { }
        }
    }
}
