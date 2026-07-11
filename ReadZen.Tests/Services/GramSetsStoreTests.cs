using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using ReadZen.App.Models;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

/// <summary>
/// INC-3B: gramsets sidecar store (search.gramsets.bin + manifest). The sidecar is a
/// pure accelerator caching the inverted-alphabet gram set per entry: every
/// corruption/mismatch mode must make TryLoadAsync return null (never throw).
/// </summary>
[Trait("Domain", "SearchSprint")]
public sealed class GramSetsStoreTests : IDisposable
{
    private readonly string _dir;

    public GramSetsStoreTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "readzen-gramsets-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_dir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch { }
    }

    private static GramSetsEntry Meta(string relPath, SearchSide side, string? hash = null,
        long ticks = 0, long len = 0) => new()
    {
        RelPath = relPath,
        Side = side,
        ContentHash = hash,
        LastWriteUtcTicks = ticks,
        LengthBytes = len,
    };

    private static uint[] SortedUnique(params uint[] values)
    {
        var arr = values.Distinct().ToArray();
        Array.Sort(arr);
        return arr;
    }

    private static List<(GramSetsEntry meta, uint[] invGrams)> SampleEntries()
    {
        // Assorted shapes: empty arrays, small arrays, a large-ish array, boundary values.
        var large = new uint[5000];
        for (int i = 0; i < large.Length; i++)
            large[i] = (uint)(i * 977 + 13); // strictly ascending, unique

        return new List<(GramSetsEntry, uint[])>
        {
            (Meta("orig/T47/T47n2005.xml", SearchSide.Original, hash: new string('a', 64), ticks: 111, len: 222),
                SortedUnique(0u, 1u, 0x4E00_4E01u, 0xFFFF_FFFEu, 0xFFFF_FFFFu)),
            (Meta("orig/T47/T47n2005.xml", SearchSide.Translated, hash: null, ticks: 333, len: 444),
                Array.Empty<uint>()),
            (Meta("tran/X99/X99n0001.xml", SearchSide.Original, hash: new string('b', 64), ticks: 555, len: 666),
                large),
            (Meta("z/empty-both.xml", SearchSide.Translated),
                Array.Empty<uint>()),
        };
    }

    // ---------------------------------------------------------------- (a) round-trip

    [Fact]
    public async Task SaveThenTryLoad_RoundTripsMetasAndArrays()
    {
        var entries = SampleEntries();
        await GramSetsStore.SaveAsync(_dir, "stamp-abc", entries, CancellationToken.None);

        var loaded = await GramSetsStore.TryLoadAsync(_dir, CancellationToken.None);
        Assert.NotNull(loaded);

        Assert.Equal(1, loaded!.Manifest.Version);
        Assert.Equal("search-v2-gramsets-invonly", loaded.Manifest.BuildGuid);
        Assert.Equal("stamp-abc", loaded.Manifest.IndexStamp);
        Assert.Equal(entries.Count, loaded.Manifest.Entries.Count);

        foreach (var (meta, invGrams) in entries)
        {
            Assert.True(loaded.TryGet(meta.RelPath, meta.Side, out var got));
            Assert.Equal(meta.RelPath, got.RelPath);
            Assert.Equal(meta.Side, got.Side);
            Assert.Equal(meta.ContentHash, got.ContentHash);
            Assert.Equal(meta.LastWriteUtcTicks, got.LastWriteUtcTicks);
            Assert.Equal(meta.LengthBytes, got.LengthBytes);
            Assert.Equal(invGrams.Length, got.InvCount);

            Assert.Equal(invGrams, loaded.ReadInvGrams(got));
        }
    }

    [Fact]
    public async Task TryGet_IsRelPathCaseInsensitive_AndMissesUnknownKeys()
    {
        await GramSetsStore.SaveAsync(_dir, null, SampleEntries(), CancellationToken.None);
        var loaded = await GramSetsStore.TryLoadAsync(_dir, CancellationToken.None);
        Assert.NotNull(loaded);

        Assert.True(loaded!.TryGet("ORIG/T47/t47N2005.XML", SearchSide.Original, out var got));
        Assert.Equal("orig/T47/T47n2005.xml", got.RelPath);

        Assert.False(loaded.TryGet("orig/T47/T47n2005.xml", (SearchSide)99, out _));
        Assert.False(loaded.TryGet("nope/missing.xml", SearchSide.Original, out _));
    }

    [Fact]
    public async Task SaveAsync_FillsOffsetsSequentially_MagicFirst()
    {
        var entries = SampleEntries();
        await GramSetsStore.SaveAsync(_dir, null, entries, CancellationToken.None);

        long expected = 4 + 16; // after "GSB1" magic + the 16-byte per-save pairing token
        foreach (var (meta, invGrams) in entries)
        {
            Assert.Equal(expected, meta.InvOffset);
            expected += 4L * invGrams.Length;
        }

        var bin = await File.ReadAllBytesAsync(GramSetsStore.GetBinPath(_dir));
        Assert.Equal(expected, bin.Length);
        Assert.Equal(new[] { (byte)'G', (byte)'S', (byte)'B', (byte)'1' }, bin.Take(4).ToArray());
    }

    // ------------------------------------------------- (b) corruption/mismatch => null

    [Fact]
    public async Task TryLoad_MissingFiles_ReturnsNull()
    {
        Assert.Null(await GramSetsStore.TryLoadAsync(_dir, CancellationToken.None));

        // Manifest present but bin missing.
        await GramSetsStore.SaveAsync(_dir, null, SampleEntries(), CancellationToken.None);
        File.Delete(GramSetsStore.GetBinPath(_dir));
        Assert.Null(await GramSetsStore.TryLoadAsync(_dir, CancellationToken.None));
    }

    [Fact]
    public async Task TryLoad_RootPathMismatch_ReturnsNull()
    {
        var otherRoot = Path.Combine(_dir, "other");
        Directory.CreateDirectory(otherRoot);
        await GramSetsStore.SaveAsync(otherRoot, null, SampleEntries(), CancellationToken.None);

        // Copy the artifacts saved for otherRoot into _dir and load with root=_dir.
        File.Copy(GramSetsStore.GetManifestPath(otherRoot), GramSetsStore.GetManifestPath(_dir));
        File.Copy(GramSetsStore.GetBinPath(otherRoot), GramSetsStore.GetBinPath(_dir));

        Assert.Null(await GramSetsStore.TryLoadAsync(_dir, CancellationToken.None));
        Assert.NotNull(await GramSetsStore.TryLoadAsync(otherRoot, CancellationToken.None));
    }

    [Fact]
    public async Task TryLoad_WrongBuildGuid_ReturnsNull()
    {
        await GramSetsStore.SaveAsync(_dir, null, SampleEntries(), CancellationToken.None);
        await PatchManifestAsync(m => m.BuildGuid = "search-v0-wrong");
        Assert.Null(await GramSetsStore.TryLoadAsync(_dir, CancellationToken.None));
    }

    [Fact]
    public async Task TryLoad_WrongVersion_ReturnsNull()
    {
        await GramSetsStore.SaveAsync(_dir, null, SampleEntries(), CancellationToken.None);
        await PatchManifestAsync(m => m.Version = 2);
        Assert.Null(await GramSetsStore.TryLoadAsync(_dir, CancellationToken.None));
    }

    [Fact]
    public async Task TryLoad_TruncatedBin_ReturnsNull()
    {
        await GramSetsStore.SaveAsync(_dir, null, SampleEntries(), CancellationToken.None);

        var binPath = GramSetsStore.GetBinPath(_dir);
        var bin = await File.ReadAllBytesAsync(binPath);
        await File.WriteAllBytesAsync(binPath, bin.Take(bin.Length / 2).ToArray());
        Assert.Null(await GramSetsStore.TryLoadAsync(_dir, CancellationToken.None));

        // Truncated below the magic.
        await File.WriteAllBytesAsync(binPath, bin.Take(2).ToArray());
        Assert.Null(await GramSetsStore.TryLoadAsync(_dir, CancellationToken.None));
    }

    [Fact]
    public async Task TryLoad_BadMagic_ReturnsNull()
    {
        await GramSetsStore.SaveAsync(_dir, null, SampleEntries(), CancellationToken.None);

        var binPath = GramSetsStore.GetBinPath(_dir);
        var bin = await File.ReadAllBytesAsync(binPath);
        bin[0] = (byte)'X';
        await File.WriteAllBytesAsync(binPath, bin);

        Assert.Null(await GramSetsStore.TryLoadAsync(_dir, CancellationToken.None));
    }

    [Fact]
    public async Task TryLoad_MalformedManifestJson_ReturnsNull()
    {
        await GramSetsStore.SaveAsync(_dir, null, SampleEntries(), CancellationToken.None);
        await File.WriteAllTextAsync(GramSetsStore.GetManifestPath(_dir), "{ this is not json");
        Assert.Null(await GramSetsStore.TryLoadAsync(_dir, CancellationToken.None));
    }

    [Fact]
    public async Task TryLoad_CountPastEndOfBin_ReturnsNull()
    {
        // A huge count on an in-range offset must fail offset + 4*count <= binLen.
        await GramSetsStore.SaveAsync(_dir, null, SampleEntries(), CancellationToken.None);
        await PatchManifestAsync(m => m.Entries[0].InvCount = int.MaxValue);
        Assert.Null(await GramSetsStore.TryLoadAsync(_dir, CancellationToken.None));

        // An offset just past the end of the bin with a nonzero count must also fail.
        await GramSetsStore.SaveAsync(_dir, null, SampleEntries(), CancellationToken.None);
        var binLen = new FileInfo(GramSetsStore.GetBinPath(_dir)).Length;
        await PatchManifestAsync(m =>
        {
            m.Entries[0].InvOffset = binLen;
            m.Entries[0].InvCount = 1;
        });
        Assert.Null(await GramSetsStore.TryLoadAsync(_dir, CancellationToken.None));
    }

    [Fact]
    public async Task TryLoad_OffsetCountOverflow_ReturnsNull()
    {
        await GramSetsStore.SaveAsync(_dir, null, SampleEntries(), CancellationToken.None);

        // offset + 4*count would overflow naive int/long arithmetic into "valid" range.
        await PatchManifestAsync(m =>
        {
            m.Entries[0].InvOffset = long.MaxValue - 2;
            m.Entries[0].InvCount = 4;
        });
        Assert.Null(await GramSetsStore.TryLoadAsync(_dir, CancellationToken.None));

        await GramSetsStore.SaveAsync(_dir, null, SampleEntries(), CancellationToken.None);
        await PatchManifestAsync(m => m.Entries[0].InvOffset = -4);
        Assert.Null(await GramSetsStore.TryLoadAsync(_dir, CancellationToken.None));

        await GramSetsStore.SaveAsync(_dir, null, SampleEntries(), CancellationToken.None);
        await PatchManifestAsync(m => m.Entries[0].InvCount = -1);
        Assert.Null(await GramSetsStore.TryLoadAsync(_dir, CancellationToken.None));
    }

    [Fact]
    public async Task TryLoad_DuplicateRelPathSideKey_ReturnsNull()
    {
        await GramSetsStore.SaveAsync(_dir, null, SampleEntries(), CancellationToken.None);
        await PatchManifestAsync(m =>
        {
            // Same key as entry 0 modulo case => duplicate under OrdinalIgnoreCase.
            m.Entries[3].RelPath = "ORIG/T47/T47N2005.xml";
            m.Entries[3].Side = SearchSide.Original;
        });
        Assert.Null(await GramSetsStore.TryLoadAsync(_dir, CancellationToken.None));
    }

    // --------------------------------------------------------------- (c) tmp hygiene

    [Fact]
    public async Task SaveAsync_LeavesNoTmpFiles()
    {
        await GramSetsStore.SaveAsync(_dir, "stamp-1", SampleEntries(), CancellationToken.None);
        Assert.Empty(Directory.GetFiles(_dir, "*.tmp"));

        // Overwrite save (both finals already exist) must also leave no tmps.
        await GramSetsStore.SaveAsync(_dir, "stamp-2", SampleEntries(), CancellationToken.None);
        Assert.Empty(Directory.GetFiles(_dir, "*.tmp"));
        Assert.True(File.Exists(GramSetsStore.GetBinPath(_dir)));
        Assert.True(File.Exists(GramSetsStore.GetManifestPath(_dir)));
    }

    [Fact]
    public async Task SaveAsync_OnFailure_DeletesTmpsAndRethrows()
    {
        // Lock the FINAL bin path so File.Move fails after the tmp was written.
        using (var block = new FileStream(GramSetsStore.GetBinPath(_dir),
                   FileMode.Create, FileAccess.Write, FileShare.None))
        {
            // Windows reports the locked destination as UnauthorizedAccessException,
            // other platforms as IOException — the contract is only "rethrows".
            await Assert.ThrowsAnyAsync<Exception>(() =>
                GramSetsStore.SaveAsync(_dir, null, SampleEntries(), CancellationToken.None));
        }

        Assert.Empty(Directory.GetFiles(_dir, "*.tmp"));
    }

    // ------------------------------------------------------------------------ helpers

    private async Task PatchManifestAsync(Action<GramSetsManifest> mutate)
    {
        var path = GramSetsStore.GetManifestPath(_dir);
        var manifest = JsonSerializer.Deserialize<GramSetsManifest>(await File.ReadAllTextAsync(path))!;
        mutate(manifest);
        await File.WriteAllTextAsync(path, JsonSerializer.Serialize(manifest));
    }
}
