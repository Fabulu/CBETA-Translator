using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

/// <summary>
/// Tests for the gram-set build API (incremental reindex, D3 item: uncut gram sets
/// in, DF cutoff at build time). The text overload of <see cref="InvertedSearchIndex.Build(System.Collections.Generic.IReadOnlyList{System.ValueTuple{string, string}})"/>
/// now delegates to <see cref="InvertedSearchIndex.ComputeGramSet"/> + the gram-set
/// overload; these tests prove the two paths are byte-identical on disk, that the
/// refactor did not change the pre-refactor save format (golden hash), and that the
/// dedup / DF-cutoff / ushort-cap semantics are reproduced exactly.
/// </summary>
[Trait("Domain", "SearchSprint")]
public sealed class InvertedGramSetBuildTests : IDisposable
{
    private readonly string _dir;

    public InvertedGramSetBuildTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "readzen-gramset-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_dir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch { }
    }

    private static uint Pack(char c0, char c1) => ((uint)c0 << 16) | c1;

    /// <summary>
    /// Fixture shared with the golden-hash test: case-differing duplicate relPaths,
    /// mixed CJK/Latin/punctuation, empty text, single-char text.
    /// </summary>
    private static List<(string relPath, string searchableText)> GoldenDocs() => new()
    {
        ("a/one.xml", "無門關心試"),
        ("A/One.xml", "門外之心"),                    // case-differing duplicate — must be dropped
        ("b/two.xml", "祖師西來意, hello 123 無門"),  // mixed CJK / Latin / punctuation
        ("c/empty.xml", ""),
        ("d/single.xml", "無"),
        ("e/three.xml", "門外之心無門"),
    };

    // ---- (a) Equivalence: text overload vs ComputeGramSet + gram-set overload ----

    [Fact]
    public async Task TextAndGramSetBuilds_ProduceByteIdenticalArtifacts()
    {
        var docs = GoldenDocs();

        var textIdx = new InvertedSearchIndex();
        textIdx.Build(docs);
        var textBin = Path.Combine(_dir, "text.inverted.bin");
        await textIdx.SaveAsync(textBin, "stamp-equiv");

        var gramIdx = new InvertedSearchIndex();
        gramIdx.Build(docs
            .Select(d => (d.relPath, InvertedSearchIndex.ComputeGramSet(d.searchableText)))
            .ToList());
        var gramBin = Path.Combine(_dir, "gram.inverted.bin");
        await gramIdx.SaveAsync(gramBin, "stamp-equiv");

        Assert.Equal(
            await File.ReadAllBytesAsync(textBin),
            await File.ReadAllBytesAsync(gramBin));
        Assert.Equal(
            await File.ReadAllBytesAsync(textBin + ".paths"),
            await File.ReadAllBytesAsync(gramBin + ".paths"));
    }

    [Fact]
    public async Task TextBuild_OutputMatchesPreRefactorGoldenBytes()
    {
        // SHA-256 of the v4 search.inverted.bin produced by Build over GoldenDocs() with
        // stamp "golden-stamp", with the 32-byte .paths checksum zeroed (it hashes
        // platform-dependent newlines; everything else in the file is deterministic).
        // Recaptured 2026-07-10 for the v4 (per-posting tf) format — every GoldenDocs
        // bigram occurs exactly once, so all tf values are 1 (one extra byte per posting)
        // and the version int is 4. A mismatch means the on-disk output changed for
        // identical inputs. (Pre-v4 golden was 642a93f0…bbd27302, format v3.)
        const string goldenSha256 = "b869050d62ecb7542a6bf00ac7895b26e488ae5badecce7cd1e6216f78d1869c";

        var idx = new InvertedSearchIndex();
        idx.Build(GoldenDocs());
        var bin = Path.Combine(_dir, "golden.inverted.bin");
        await idx.SaveAsync(bin, "golden-stamp");

        var bytes = await File.ReadAllBytesAsync(bin);
        int stampLen = BitConverter.ToUInt16(bytes, 8); // after 4-byte magic + int32 version
        for (int i = 0; i < 32; i++) bytes[10 + stampLen + i] = 0;

        Assert.Equal(goldenSha256, Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant());
    }

    [Fact]
    public void GramSetBuild_KeepsFirstOccurrence_CaseInsensitive()
    {
        // Third doc keeps maxDf = (int)(2 * 0.8) = 1 above zero so 1-doc terms survive.
        var idx = new InvertedSearchIndex();
        idx.Build(new List<(string relPath, uint[] gramSet)>
        {
            ("X/a.xml", InvertedSearchIndex.ComputeGramSet("無門")),
            ("x/A.xml", InvertedSearchIndex.ComputeGramSet("祖師")), // dropped duplicate
            ("y/b.xml", InvertedSearchIndex.ComputeGramSet("山水")),
        });

        Assert.Equal(2, idx.DocCount);
        Assert.Equal("X/a.xml", idx.GetRelPath(0));
        Assert.Equal("y/b.xml", idx.GetRelPath(1));
        Assert.Equal(new ushort[] { 0 }, idx.Search("無門"));
        Assert.Equal(new ushort[] { 1 }, idx.Search("山水"));
        Assert.Empty(idx.Search("祖師")!); // duplicate's grams were never indexed
    }

    // ---- (b) DF cutoff boundary: kept at exactly maxDf, dropped at maxDf + 1 ----

    [Fact]
    public async Task DfCutoffBoundary_BothOverloadsAgree()
    {
        // 10 docs → maxDf = (int)(10 * 0.8) = 8. "無門" in exactly 8 docs (kept:
        // count > maxDf is false), "祖師" in 9 docs (dropped). Comma separators keep
        // the fixture grams from forming cross-boundary bigrams.
        var docs = new List<(string relPath, string searchableText)>();
        for (int i = 0; i < 10; i++)
        {
            string text = (i < 8 ? "無門," : "") + (i < 9 ? "祖師," : "") + "山水";
            docs.Add(($"d/{i:D2}.xml", text));
        }

        var textIdx = new InvertedSearchIndex();
        textIdx.Build(docs);
        var gramIdx = new InvertedSearchIndex();
        gramIdx.Build(docs
            .Select(d => (d.relPath, InvertedSearchIndex.ComputeGramSet(d.searchableText)))
            .ToList());

        var expectedKept = Enumerable.Range(0, 8).Select(i => (ushort)i).ToArray();
        foreach (var idx in new[] { textIdx, gramIdx })
        {
            Assert.Equal(expectedKept, idx.Search("無門")); // exactly maxDf docs → kept
            Assert.Empty(idx.Search("祖師")!);              // maxDf + 1 docs → cut
        }

        // And the cutoff decision is identical on disk, not just via Search.
        var textBin = Path.Combine(_dir, "df-text.bin");
        var gramBin = Path.Combine(_dir, "df-gram.bin");
        await textIdx.SaveAsync(textBin, "stamp-df");
        await gramIdx.SaveAsync(gramBin, "stamp-df");
        Assert.Equal(
            await File.ReadAllBytesAsync(textBin),
            await File.ReadAllBytesAsync(gramBin));
    }

    // ---- (c) ushort docId cap ----

    [Fact]
    public void GramSetBuild_BeyondUShortDocLimit_Throws()
    {
        var docs = new List<(string relPath, uint[] gramSet)>(ushort.MaxValue + 1);
        for (int i = 0; i <= ushort.MaxValue; i++)
            docs.Add(($"d/{i}.xml", Array.Empty<uint>()));

        var idx = new InvertedSearchIndex();
        var ex = Assert.Throws<InvalidOperationException>(() => idx.Build(docs));
        Assert.Contains("65535", ex.Message);
    }

    // ---- (d) ComputeGramSet units ----

    [Fact]
    public void ComputeGramSet_ExcludesPairsWithNonCjkNeighbor()
    {
        // "a無" and "門b" have a non-indexable half; only "無門" survives.
        var grams = InvertedSearchIndex.ComputeGramSet("a無門b");
        Assert.Equal(new[] { Pack('無', '門') }, grams);
    }

    [Fact]
    public void ComputeGramSet_PacksUniquesAndSortsAscending()
    {
        // Pairs: 門無, 無門, 門無 → unique {無門, 門無}, ascending by packed value
        // ('無' U+7121 < '門' U+9580, and c0 occupies the high 16 bits).
        var grams = InvertedSearchIndex.ComputeGramSet("門無門無");
        Assert.Equal(new[] { Pack('無', '門'), Pack('門', '無') }, grams);
        Assert.True(grams[0] < grams[1]);
        Assert.Equal(0x71219580u, Pack('無', '門')); // packing formula, explicitly
    }

    [Fact]
    public void ComputeGramSet_ExcludesCjkExtBSurrogatePairs()
    {
        // U+20000 (CJK Ext-B) is a surrogate pair; both halves fail IsIndexable, so
        // neither the pair itself nor its junctions with BMP CJK chars are indexed.
        Assert.Empty(InvertedSearchIndex.ComputeGramSet("\U00020000\U00020000"));
        Assert.Empty(InvertedSearchIndex.ComputeGramSet("無\U00020000門"));
    }

    [Fact]
    public void ComputeGramSet_EmptyAndSingleCharInputs_YieldEmpty()
    {
        Assert.Empty(InvertedSearchIndex.ComputeGramSet(""));
        Assert.Empty(InvertedSearchIndex.ComputeGramSet("無"));
    }
}
