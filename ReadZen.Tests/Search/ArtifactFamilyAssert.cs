using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ReadZen.App.Models;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Search;

/// <summary>
/// Equivalence harness for the search index artifact family (INC-1C).
///
/// Implements the S6 comparison table verbatim: an incremental update must produce
/// artifacts EQUIVALENT to a from-scratch full rebuild of the same corpus state —
/// identical modulo build timestamps (<c>BuiltUtc</c>) and per-build stamps
/// (<c>IndexStamp</c>, and the stamp embedded in the inverted bin header).
///
/// Design constraints honored here:
///   - JSON manifest comparisons are explicit field ALLOWLISTS (never "everything
///     except X"), so new fields (e.g. the IndexStamp INC-1A adds to the
///     corpusfreq manifest) can never break the harness;
///   - sibling stamps are read via <see cref="JsonDocument"/>, not DTO properties, so
///     this file compiles regardless of INC-1A's merge state;
///   - the corpusfreq bin is parsed and compared AS MAPS, never bytes (dictionary
///     insertion order legitimately differs between delta and full build paths);
///   - artifact file names are hardcoded (precedent: InvertedSearchIndexIntegrityTests).
/// </summary>
public static class ArtifactFamilyAssert
{
    private const string MainManifestName = "search.index.manifest.json";
    private const string IndexBinName = "search.index.bin";
    private const string TextManifestName = "search.text.manifest.json";
    private const string TextBinName = "search.text.bin";
    private const string FreqManifestName = "search.corpusfreq.manifest.json";
    private const string FreqBinName = "search.corpusfreq.bin";
    private const string InvertedBinName = "search.inverted.bin";
    private const string InvertedPathsName = "search.inverted.bin.paths";

    /// <summary>In-memory snapshot of all 8 artifact-family files under a root.</summary>
    public sealed class FamilySnapshot
    {
        public required SearchIndexManifest MainManifest { get; init; }
        public required byte[] IndexBin { get; init; }
        public required SearchTextManifest TextManifest { get; init; }
        public required byte[] TextBin { get; init; }
        public required CorpusFreqManifest FreqManifest { get; init; }
        public required Dictionary<char, int> FreqChars { get; init; }
        public required Dictionary<(char c0, char c1), int> FreqBigrams { get; init; }
        public required long FreqTotalChars { get; init; }
        public required byte[] InvertedBin { get; init; }
        public required byte[] InvertedPaths { get; init; }
    }

    /// <summary>Reads all 8 family files into memory. Every file must exist.</summary>
    public static FamilySnapshot SnapshotFamily(string root)
    {
        var main = DeserializeRequired<SearchIndexManifest>(Path.Combine(root, MainManifestName));
        var text = DeserializeRequired<SearchTextManifest>(Path.Combine(root, TextManifestName));
        var freq = DeserializeRequired<CorpusFreqManifest>(Path.Combine(root, FreqManifestName));

        var freqBin = ReadRequired(Path.Combine(root, FreqBinName));
        var (freqChars, freqBigrams, freqTotal) = ParseCorpusFreqBin(freqBin);

        return new FamilySnapshot
        {
            MainManifest = main,
            IndexBin = ReadRequired(Path.Combine(root, IndexBinName)),
            TextManifest = text,
            TextBin = ReadRequired(Path.Combine(root, TextBinName)),
            FreqManifest = freq,
            FreqChars = freqChars,
            FreqBigrams = freqBigrams,
            FreqTotalChars = freqTotal,
            InvertedBin = ReadRequired(Path.Combine(root, InvertedBinName)),
            InvertedPaths = ReadRequired(Path.Combine(root, InvertedPathsName)),
        };
    }

    /// <summary>
    /// Asserts two snapshots are equivalent per the S6 table. Ignored everywhere:
    /// BuiltUtc and IndexStamp (per-build values by design).
    /// </summary>
    public static void AssertEquivalent(FamilySnapshot a, FamilySnapshot b)
    {
        AssertMainManifestEquivalent(a.MainManifest, b.MainManifest);
        AssertBytesEqual(a.IndexBin, b.IndexBin, "search.index.bin");
        AssertTextManifestEquivalent(a.TextManifest, b.TextManifest);
        AssertBytesEqual(a.TextBin, b.TextBin, "search.text.bin");
        AssertFreqManifestEquivalent(a.FreqManifest, b.FreqManifest);
        AssertFreqMapsEqual(a, b);
        AssertBytesEqual(a.InvertedPaths, b.InvertedPaths, "search.inverted.bin.paths");
        AssertInvertedStructurallyEqual(a.InvertedBin, b.InvertedBin);
    }

    /// <summary>
    /// Cross-family stamp consistency for a single committed build: the main manifest
    /// IndexStamp is non-null, the inverted index loads against it, and — IF the
    /// corpusfreq manifest carries an "IndexStamp" JSON property (it will once INC-1A
    /// lands; unconditional presence is INC-1A's own test's job) — it matches.
    /// </summary>
    public static async Task AssertFamilyStampsAsync(string root)
    {
        string? stamp;
        using (var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(root, MainManifestName))))
        {
            stamp = doc.RootElement.TryGetProperty("IndexStamp", out var st) && st.ValueKind == JsonValueKind.String
                ? st.GetString()
                : null;
        }
        Assert.False(string.IsNullOrEmpty(stamp), "main manifest IndexStamp must be non-null and non-empty");

        var inv = new InvertedSearchIndex();
        bool loaded = await inv.TryLoadAsync(Path.Combine(root, InvertedBinName), stamp!);
        Assert.True(loaded, "search.inverted.bin must load against the main manifest IndexStamp");

        AssertSiblingStampMatchesIfPresent(Path.Combine(root, FreqManifestName), stamp!, "corpusfreq manifest");
    }

    // ===== Per-artifact comparers (explicit allowlists only) =====

    private static void AssertMainManifestEquivalent(SearchIndexManifest a, SearchIndexManifest b)
    {
        // Allowlist: Version, RootPath, BloomBits, BloomHashCount, BuildGuid, InputHash,
        // Entries (Id, RelPath, Side, LastWriteUtcTicks, LengthBytes, ContentHash, BloomOffset).
        // Ignored: BuiltUtc, IndexStamp.
        Assert.Equal(a.Version, b.Version);
        Assert.Equal(a.RootPath, b.RootPath);
        Assert.Equal(a.BloomBits, b.BloomBits);
        Assert.Equal(a.BloomHashCount, b.BloomHashCount);
        Assert.Equal(a.BuildGuid, b.BuildGuid);
        // InputHash is content-derived, so it MUST be equal for the same corpus state.
        Assert.Equal(a.InputHash, b.InputHash);

        Assert.Equal(a.Entries.Count, b.Entries.Count);
        for (int i = 0; i < a.Entries.Count; i++)
        {
            var ea = a.Entries[i];
            var eb = b.Entries[i];
            string ctx = $"main manifest entry {i} ('{ea.RelPath}'/{ea.Side})";
            Assert.True(ea.Id == eb.Id, $"{ctx}: Id {ea.Id} != {eb.Id}");
            Assert.True(string.Equals(ea.RelPath, eb.RelPath, StringComparison.Ordinal), $"{ctx}: RelPath '{ea.RelPath}' != '{eb.RelPath}'");
            Assert.True(ea.Side == eb.Side, $"{ctx}: Side {ea.Side} != {eb.Side}");
            Assert.True(ea.LastWriteUtcTicks == eb.LastWriteUtcTicks, $"{ctx}: LastWriteUtcTicks differ");
            Assert.True(ea.LengthBytes == eb.LengthBytes, $"{ctx}: LengthBytes {ea.LengthBytes} != {eb.LengthBytes}");
            Assert.True(string.Equals(ea.ContentHash, eb.ContentHash, StringComparison.Ordinal), $"{ctx}: ContentHash differ");
            Assert.True(ea.BloomOffset == eb.BloomOffset, $"{ctx}: BloomOffset {ea.BloomOffset} != {eb.BloomOffset}");
        }
    }

    private static void AssertTextManifestEquivalent(SearchTextManifest a, SearchTextManifest b)
    {
        // Allowlist: Version, RootPath, BuildGuid, Entries (Id, RelPath, Side,
        // LastWriteUtcTicks, LengthBytes, TextOffset, TextLengthBytes). Ignored: BuiltUtc.
        Assert.Equal(a.Version, b.Version);
        Assert.Equal(a.RootPath, b.RootPath);
        Assert.Equal(a.BuildGuid, b.BuildGuid);

        Assert.Equal(a.Entries.Count, b.Entries.Count);
        for (int i = 0; i < a.Entries.Count; i++)
        {
            var ea = a.Entries[i];
            var eb = b.Entries[i];
            string ctx = $"text manifest entry {i} ('{ea.RelPath}'/{ea.Side})";
            Assert.True(ea.Id == eb.Id, $"{ctx}: Id {ea.Id} != {eb.Id}");
            Assert.True(string.Equals(ea.RelPath, eb.RelPath, StringComparison.Ordinal), $"{ctx}: RelPath '{ea.RelPath}' != '{eb.RelPath}'");
            Assert.True(ea.Side == eb.Side, $"{ctx}: Side {ea.Side} != {eb.Side}");
            Assert.True(ea.LastWriteUtcTicks == eb.LastWriteUtcTicks, $"{ctx}: LastWriteUtcTicks differ");
            Assert.True(ea.LengthBytes == eb.LengthBytes, $"{ctx}: LengthBytes {ea.LengthBytes} != {eb.LengthBytes}");
            Assert.True(ea.TextOffset == eb.TextOffset, $"{ctx}: TextOffset {ea.TextOffset} != {eb.TextOffset}");
            Assert.True(ea.TextLengthBytes == eb.TextLengthBytes, $"{ctx}: TextLengthBytes {ea.TextLengthBytes} != {eb.TextLengthBytes}");
        }
    }

    private static void AssertFreqManifestEquivalent(CorpusFreqManifest a, CorpusFreqManifest b)
    {
        // Allowlist: Version, BuildGuid, TotalCharacters, UniqueCharacters, UniqueBigrams.
        // Ignored: BuiltUtc, IndexStamp.
        Assert.Equal(a.Version, b.Version);
        Assert.Equal(a.BuildGuid, b.BuildGuid);
        Assert.Equal(a.TotalCharacters, b.TotalCharacters);
        Assert.Equal(a.UniqueCharacters, b.UniqueCharacters);
        Assert.Equal(a.UniqueBigrams, b.UniqueBigrams);
    }

    private static void AssertFreqMapsEqual(FamilySnapshot a, FamilySnapshot b)
    {
        // Compared AS MAPS, never bytes: dictionary insertion order legitimately
        // differs between the delta and full build paths.
        Assert.Equal(a.FreqTotalChars, b.FreqTotalChars);

        Assert.Equal(a.FreqChars.Count, b.FreqChars.Count);
        foreach (var kv in a.FreqChars)
        {
            Assert.True(b.FreqChars.TryGetValue(kv.Key, out var v) && v == kv.Value,
                $"corpusfreq char 'U+{(int)kv.Key:X4}' count differs: {kv.Value} vs {(b.FreqChars.TryGetValue(kv.Key, out var v2) ? v2.ToString() : "<absent>")}");
        }

        Assert.Equal(a.FreqBigrams.Count, b.FreqBigrams.Count);
        foreach (var kv in a.FreqBigrams)
        {
            Assert.True(b.FreqBigrams.TryGetValue(kv.Key, out var v) && v == kv.Value,
                $"corpusfreq bigram 'U+{(int)kv.Key.c0:X4} U+{(int)kv.Key.c1:X4}' count differs: {kv.Value} vs {(b.FreqBigrams.TryGetValue(kv.Key, out var v2) ? v2.ToString() : "<absent>")}");
        }
    }

    private static void AssertInvertedStructurallyEqual(byte[] a, byte[] b)
    {
        // bytes [0,8) equal (magic "IIDX" + int32 version); each file carries its own
        // ushort stampLen at offset 8 followed by the stamp; ALL bytes after each
        // file's stamp must be equal (this includes the 32-byte .paths checksum,
        // the dictionary, and the postings).
        Assert.True(a.Length >= 10, "inverted bin A too short for header");
        Assert.True(b.Length >= 10, "inverted bin B too short for header");
        AssertBytesEqual(a[..8], b[..8], "inverted bin header (magic+version)");

        int stampLenA = a[8] | (a[9] << 8); // little-endian ushort
        int stampLenB = b[8] | (b[9] << 8);
        Assert.True(a.Length >= 10 + stampLenA, "inverted bin A shorter than its own stamp");
        Assert.True(b.Length >= 10 + stampLenB, "inverted bin B shorter than its own stamp");

        AssertBytesEqual(a[(10 + stampLenA)..], b[(10 + stampLenB)..], "inverted bin body after stamp");
    }

    // ===== Plumbing =====

    private static void AssertSiblingStampMatchesIfPresent(string manifestPath, string mainStamp, string label)
    {
        Assert.True(File.Exists(manifestPath), $"{label} missing: {manifestPath}");
        using var doc = JsonDocument.Parse(File.ReadAllText(manifestPath));
        if (doc.RootElement.TryGetProperty("IndexStamp", out var el) && el.ValueKind == JsonValueKind.String)
        {
            Assert.True(string.Equals(el.GetString(), mainStamp, StringComparison.Ordinal),
                $"{label} IndexStamp '{el.GetString()}' != main manifest IndexStamp '{mainStamp}'");
        }
    }

    private static (Dictionary<char, int> chars, Dictionary<(char c0, char c1), int> bigrams, long totalChars)
        ParseCorpusFreqBin(byte[] bytes)
    {
        // Format: magic "CF01", int32 charCount, int32 bigramCount, int64 totalChars,
        // charCount x (char, int32), bigramCount x (char, char, int32). Chars are
        // BinaryWriter/BinaryReader UTF-8 encoded.
        using var ms = new MemoryStream(bytes);
        using var br = new BinaryReader(ms, new UTF8Encoding(false));

        var magic = br.ReadBytes(4);
        Assert.True(magic.Length == 4 && magic[0] == (byte)'C' && magic[1] == (byte)'F'
                    && magic[2] == (byte)'0' && magic[3] == (byte)'1',
            "corpusfreq bin: bad magic (expected CF01)");

        int charCount = br.ReadInt32();
        int bigramCount = br.ReadInt32();
        long totalChars = br.ReadInt64();

        var chars = new Dictionary<char, int>(charCount);
        for (int i = 0; i < charCount; i++)
        {
            char c = br.ReadChar();
            chars[c] = br.ReadInt32();
        }

        var bigrams = new Dictionary<(char c0, char c1), int>(bigramCount);
        for (int i = 0; i < bigramCount; i++)
        {
            char c0 = br.ReadChar();
            char c1 = br.ReadChar();
            bigrams[(c0, c1)] = br.ReadInt32();
        }

        return (chars, bigrams, totalChars);
    }

    private static void AssertBytesEqual(byte[] a, byte[] b, string label)
    {
        if (a.AsSpan().SequenceEqual(b))
            return;
        int n = Math.Min(a.Length, b.Length);
        int firstDiff = n; // differ only in length when the common prefix matches
        for (int i = 0; i < n; i++)
        {
            if (a[i] != b[i]) { firstDiff = i; break; }
        }
        Assert.Fail($"{label}: byte streams differ (lenA={a.Length}, lenB={b.Length}, first difference at offset {firstDiff}).");
    }

    private static byte[] ReadRequired(string path)
    {
        Assert.True(File.Exists(path), $"artifact missing: {path}");
        return File.ReadAllBytes(path);
    }

    private static T DeserializeRequired<T>(string path)
    {
        Assert.True(File.Exists(path), $"artifact missing: {path}");
        var value = JsonSerializer.Deserialize<T>(File.ReadAllText(path));
        Assert.True(value is not null, $"artifact failed to deserialize as {typeof(T).Name}: {path}");
        return value!;
    }
}
