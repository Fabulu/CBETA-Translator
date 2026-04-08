using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ReadZen.App.Models;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

/// <summary>
/// Tests for CommunityDataService scholar collection merge and dedup operations.
/// These tests use real file I/O against a temp directory.
/// </summary>
public class CommunityDataServiceScholarTests : IDisposable
{
    private readonly string _localRoot;
    private readonly string _upstreamDir;
    private readonly CommunityDataService _svc = new();

    private static readonly JsonSerializerOptions WriteOpts = new() { WriteIndented = true };

    public CommunityDataServiceScholarTests()
    {
        var baseDir = Path.Combine(Path.GetTempPath(), "readzen-cds-test-" + Guid.NewGuid().ToString("N")[..8]);
        _localRoot = Path.Combine(baseDir, "local");
        _upstreamDir = Path.Combine(baseDir, "upstream");
        Directory.CreateDirectory(_localRoot);
        Directory.CreateDirectory(_upstreamDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(Path.GetDirectoryName(_localRoot)!, true); } catch { }
    }

    private async Task WriteCollections(string dir, List<ScholarCollection> collections)
    {
        var path = Path.Combine(dir, "scholar-collections.json");
        var json = JsonSerializer.Serialize(collections, WriteOpts);
        await File.WriteAllTextAsync(path, json, new UTF8Encoding(false));
    }

    // ---- MergeScholarCollectionsFromAsync ----

    [Fact]
    public async Task Merge_WithEmptyUpstream_ReturnsLocalCount()
    {
        var local = new List<ScholarCollection>
        {
            new() { Id = "c1", Name = "Local Collection", CreatedUtc = DateTimeOffset.UtcNow }
        };
        await WriteCollections(_localRoot, local);

        // Upstream file does not exist
        var upstreamPath = Path.Combine(_upstreamDir, "scholar-collections.json");

        var result = await _svc.MergeScholarCollectionsFromAsync(_localRoot, upstreamPath);

        Assert.Equal(1, result);
    }

    [Fact]
    public async Task Merge_WithNewUpstreamCollection_AddsIt()
    {
        var local = new List<ScholarCollection>
        {
            new() { Id = "c1", Name = "Local", CreatedUtc = DateTimeOffset.UtcNow }
        };
        await WriteCollections(_localRoot, local);

        var upstream = new List<ScholarCollection>
        {
            new() { Id = "c2", Name = "Upstream New", CreatedUtc = DateTimeOffset.UtcNow }
        };
        var upstreamPath = Path.Combine(_upstreamDir, "scholar-collections.json");
        await WriteCollections(_upstreamDir, upstream);

        var result = await _svc.MergeScholarCollectionsFromAsync(_localRoot, upstreamPath);

        Assert.Equal(2, result);

        // Verify the merged file contains both
        var mergedJson = await File.ReadAllTextAsync(Path.Combine(_localRoot, "scholar-collections.json"));
        var merged = JsonSerializer.Deserialize<List<ScholarCollection>>(mergedJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(merged);
        Assert.Equal(2, merged!.Count);
    }

    [Fact]
    public async Task Merge_DuplicateCollectionId_KeepsNewestByTimestamp()
    {
        var olderTime = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var newerTime = new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero);

        var local = new List<ScholarCollection>
        {
            new()
            {
                Id = "c1",
                Name = "Local Old Version",
                CreatedUtc = olderTime,
                Passages = new List<ScholarPassage>
                {
                    new() { Id = "p1", ZhText = "local passage", SourceRelPath = "x.xml", AddedUtc = olderTime }
                }
            }
        };
        await WriteCollections(_localRoot, local);

        var upstream = new List<ScholarCollection>
        {
            new()
            {
                Id = "c1",
                Name = "Upstream Newer Version",
                CreatedUtc = newerTime,
                Passages = new List<ScholarPassage>
                {
                    new() { Id = "p2", ZhText = "upstream passage", SourceRelPath = "y.xml", AddedUtc = newerTime }
                }
            }
        };
        var upstreamPath = Path.Combine(_upstreamDir, "scholar-collections.json");
        await WriteCollections(_upstreamDir, upstream);

        var result = await _svc.MergeScholarCollectionsFromAsync(_localRoot, upstreamPath);

        Assert.Equal(1, result);

        // Read back merged result
        var mergedJson = await File.ReadAllTextAsync(Path.Combine(_localRoot, "scholar-collections.json"));
        var merged = JsonSerializer.Deserialize<List<ScholarCollection>>(mergedJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(merged);
        Assert.Single(merged!);

        // The winner should be the upstream (newer), but passages from both should be merged
        var c = merged[0];
        Assert.Equal("Upstream Newer Version", c.Name);
        Assert.Equal(2, c.Passages.Count); // both p1 and p2 should be present
    }

    [Fact]
    public async Task Merge_DuplicatePassageIdWithinCollection_KeepsNewest()
    {
        var olderTime = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var newerTime = new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero);

        var local = new List<ScholarCollection>
        {
            new()
            {
                Id = "c1",
                Name = "Collection",
                CreatedUtc = olderTime,
                Passages = new List<ScholarPassage>
                {
                    new() { Id = "p1", ZhText = "old text", SourceRelPath = "x.xml", AddedUtc = olderTime }
                }
            }
        };
        await WriteCollections(_localRoot, local);

        var upstream = new List<ScholarCollection>
        {
            new()
            {
                Id = "c1",
                Name = "Collection",
                CreatedUtc = olderTime,
                Passages = new List<ScholarPassage>
                {
                    new() { Id = "p1", ZhText = "newer text", SourceRelPath = "x.xml", AddedUtc = newerTime }
                }
            }
        };
        var upstreamPath = Path.Combine(_upstreamDir, "scholar-collections.json");
        await WriteCollections(_upstreamDir, upstream);

        var result = await _svc.MergeScholarCollectionsFromAsync(_localRoot, upstreamPath);

        Assert.Equal(1, result);

        var mergedJson = await File.ReadAllTextAsync(Path.Combine(_localRoot, "scholar-collections.json"));
        var merged = JsonSerializer.Deserialize<List<ScholarCollection>>(mergedJson, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(merged);
        Assert.Single(merged!);
        Assert.Single(merged[0].Passages);
        // The newer passage should win (deduped by Id, keep newest by AddedUtc)
        Assert.Equal("newer text", merged[0].Passages[0].ZhText);
    }

    // ---- SortAndDedupScholarCollectionsAsync ----

    [Fact]
    public async Task SortAndDedup_RemovesDuplicateCollections()
    {
        var ts1 = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero);
        var ts2 = new DateTimeOffset(2025, 6, 1, 0, 0, 0, TimeSpan.Zero);

        var collections = new List<ScholarCollection>
        {
            new() { Id = "c1", Name = "First Version", CreatedUtc = ts1 },
            new() { Id = "c1", Name = "Second Version", CreatedUtc = ts2 },
            new() { Id = "c2", Name = "Unique", CreatedUtc = ts1 }
        };
        await WriteCollections(_localRoot, collections);

        var result = await _svc.SortAndDedupScholarCollectionsAsync(_localRoot);

        Assert.Equal(2, result);

        var json = await File.ReadAllTextAsync(Path.Combine(_localRoot, "scholar-collections.json"));
        var deduped = JsonSerializer.Deserialize<List<ScholarCollection>>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(deduped);
        Assert.Equal(2, deduped!.Count);

        // The duplicate c1 should keep the newer version
        var c1 = deduped.Find(c => c.Id == "c1");
        Assert.NotNull(c1);
        Assert.Equal("Second Version", c1!.Name);
    }

    [Fact]
    public async Task SortAndDedup_NonExistentFile_ReturnsZero()
    {
        var emptyDir = Path.Combine(Path.GetTempPath(), "readzen-empty-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(emptyDir);

        try
        {
            var result = await _svc.SortAndDedupScholarCollectionsAsync(emptyDir);
            Assert.Equal(0, result);
        }
        finally
        {
            try { Directory.Delete(emptyDir, true); } catch { }
        }
    }

    [Fact]
    public async Task Merge_BothEmpty_ReturnsZero()
    {
        // Neither local nor upstream file exists
        var upstreamPath = Path.Combine(_upstreamDir, "scholar-collections.json");

        var result = await _svc.MergeScholarCollectionsFromAsync(_localRoot, upstreamPath);

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task Merge_EmptyLocalWithUpstream_AddsUpstream()
    {
        // No local file
        var upstream = new List<ScholarCollection>
        {
            new() { Id = "c1", Name = "From Upstream", CreatedUtc = DateTimeOffset.UtcNow }
        };
        var upstreamPath = Path.Combine(_upstreamDir, "scholar-collections.json");
        await WriteCollections(_upstreamDir, upstream);

        var result = await _svc.MergeScholarCollectionsFromAsync(_localRoot, upstreamPath);

        Assert.Equal(1, result);
    }
}
