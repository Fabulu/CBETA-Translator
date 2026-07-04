using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

/// <summary>
/// Regression tests for audit item P1.1 (RUN-20260702-2259 R3-H1/H2/M6): before the
/// v3 format, search.inverted.bin had no staleness contract with the bloom manifest
/// (a stale file from a failed rebuild loaded fine and silently dropped newer docs
/// from search results) and SaveAsync wrote .paths and .bin non-atomically on the
/// final paths (a torn save loaded cleanly with a WRONG docId-&gt;path mapping).
/// </summary>
public sealed class InvertedSearchIndexIntegrityTests : IDisposable
{
    private readonly string _dir;
    private string BinPath => Path.Combine(_dir, "search.inverted.bin");

    public InvertedSearchIndexIntegrityTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "readzen-inv-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch { }
    }

    private static InvertedSearchIndex BuildIndex(params (string relPath, string text)[] docs)
    {
        var idx = new InvertedSearchIndex();
        idx.Build(docs.Select(d => (d.relPath, d.text)).ToList());
        return idx;
    }

    [Fact]
    public async Task RoundTrip_WithMatchingStamp_LoadsAndMapsPaths()
    {
        var idx = BuildIndex(("a/one.xml", "無門關心"), ("b/two.xml", "門外之心"));
        await idx.SaveAsync(BinPath, "stamp-1");

        var loaded = new InvertedSearchIndex();
        Assert.True(await loaded.TryLoadAsync(BinPath, "stamp-1"));
        Assert.Equal(2, loaded.DocCount);

        var hits = loaded.Search("無門");
        Assert.NotNull(hits);
        var paths = hits!.Select(h => loaded.GetRelPath(h)).ToArray();
        Assert.Equal(new[] { "a/one.xml" }, paths);
    }

    [Fact]
    public async Task Load_WithMismatchedStamp_IsRefused()
    {
        // The core R3-H1 scenario: the manifest was rebuilt (new stamp) but the
        // inverted build failed, leaving a file stamped by the PREVIOUS build.
        var idx = BuildIndex(("a/one.xml", "無門關心"));
        await idx.SaveAsync(BinPath, "stamp-old");

        var loaded = new InvertedSearchIndex();
        Assert.False(await loaded.TryLoadAsync(BinPath, "stamp-new"));
        Assert.False(loaded.IsLoaded);
    }

    [Fact]
    public async Task Load_WithNullOrEmptyExpectedStamp_IsRefused()
    {
        var idx = BuildIndex(("a/one.xml", "無門關心"));
        await idx.SaveAsync(BinPath, "stamp-1");

        var loaded = new InvertedSearchIndex();
        Assert.False(await loaded.TryLoadAsync(BinPath, ""));
        Assert.False(loaded.IsLoaded);
    }

    [Fact]
    public async Task TornSave_NewPathsWithOldBin_IsRefused()
    {
        // The R3-H2 scenario: crash between the .paths write and the .bin write leaves
        // files from two different builds. Both are individually well-formed, so the
        // old loader accepted them and attributed hits to the wrong documents.
        var oldIdx = BuildIndex(("old/one.xml", "無門關心"), ("old/two.xml", "門外之心"));
        await oldIdx.SaveAsync(BinPath, "stamp-1");
        var oldBin = await File.ReadAllBytesAsync(BinPath);

        var newIdx = BuildIndex(("new/A.xml", "無門關心"), ("new/B.xml", "門外之心"), ("new/C.xml", "祖師西來"));
        await newIdx.SaveAsync(BinPath, "stamp-1"); // same build stamp on purpose
        await File.WriteAllBytesAsync(BinPath, oldBin); // simulate torn state: new .paths + old .bin

        var loaded = new InvertedSearchIndex();
        Assert.False(await loaded.TryLoadAsync(BinPath, "stamp-1"));
        Assert.False(loaded.IsLoaded);
    }

    [Fact]
    public async Task OldV2Format_IsRefused()
    {
        // Hand-craft a v2-style header (magic + version 2). The v3 loader must refuse
        // it; the BuildGuid bump forces the rebuild that replaces it.
        var idx = BuildIndex(("a/one.xml", "無門關心"));
        await idx.SaveAsync(BinPath, "stamp-1");

        var bytes = await File.ReadAllBytesAsync(BinPath);
        BitConverter.GetBytes(2).CopyTo(bytes, 4); // overwrite version int after 4-byte magic
        await File.WriteAllBytesAsync(BinPath, bytes);

        var loaded = new InvertedSearchIndex();
        Assert.False(await loaded.TryLoadAsync(BinPath, "stamp-1"));
    }

    [Fact]
    public async Task SaveAsync_LeavesNoTempFiles()
    {
        var idx = BuildIndex(("a/one.xml", "無門關心"));
        await idx.SaveAsync(BinPath, "stamp-1");

        Assert.True(File.Exists(BinPath));
        Assert.True(File.Exists(BinPath + ".paths"));
        Assert.Empty(Directory.GetFiles(_dir, "*.tmp"));
    }

    [Fact]
    public async Task SaveAsync_WithoutStamp_Throws()
    {
        var idx = BuildIndex(("a/one.xml", "無門關心"));
        await Assert.ThrowsAsync<ArgumentException>(() => idx.SaveAsync(BinPath, ""));
    }

    [Fact]
    public void Build_BeyondUShortDocLimit_Throws()
    {
        // R3-M6: doc IDs are ushort; past 65,535 the old code wrapped silently and
        // attributed hits to the wrong files.
        var docs = new List<(string relPath, string text)>(ushort.MaxValue + 1);
        for (int i = 0; i <= ushort.MaxValue; i++)
            docs.Add(($"d/{i}.xml", ""));

        var idx = new InvertedSearchIndex();
        Assert.Throws<InvalidOperationException>(() => idx.Build(docs));
    }

    [Fact]
    public async Task TamperedPathsFile_IsRefused()
    {
        // Editing/reordering .paths after the save must invalidate the pair — the
        // docId->path mapping is only trustworthy as a unit.
        var idx = BuildIndex(("a/one.xml", "無門關心"), ("b/two.xml", "門外之心"));
        await idx.SaveAsync(BinPath, "stamp-1");

        var lines = await File.ReadAllLinesAsync(BinPath + ".paths");
        Array.Reverse(lines);
        await File.WriteAllLinesAsync(BinPath + ".paths", lines);

        var loaded = new InvertedSearchIndex();
        Assert.False(await loaded.TryLoadAsync(BinPath, "stamp-1"));
    }
}
