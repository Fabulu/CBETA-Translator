using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using CbetaTranslator.App.Models;
using CbetaTranslator.App.Services;
using Xunit;

namespace CbetaTranslator.Tests.Services;

public class ScholarCollectionsServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly ScholarCollectionsService _svc = new();

    public ScholarCollectionsServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "cbeta-test-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); } catch { }
    }

    [Fact]
    public async Task LoadAsync_NonExistentFile_ReturnsEmptyList()
    {
        var result = await _svc.LoadAsync(_tempDir);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task SaveAsync_ThenLoadAsync_RoundTrips()
    {
        var collections = new List<ScholarCollection>
        {
            new()
            {
                Id = "c1",
                Name = "Test Collection",
                Description = "A test",
                Tags = new List<string> { "tag1", "tag2" },
                CreatedUtc = new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero),
                Passages = new List<ScholarPassage>
                {
                    new()
                    {
                        Id = "p1",
                        SourceRelPath = "xml-p5/T/T0001.xml",
                        ZhText = "some chinese",
                        EnText = "some english",
                        Notes = "a note",
                        Tags = new List<string> { "dharma" },
                        MasterNames = new List<string> { "Master A" },
                        AddedUtc = new DateTimeOffset(2025, 1, 2, 0, 0, 0, TimeSpan.Zero)
                    }
                }
            }
        };

        await _svc.SaveAsync(_tempDir, collections);
        var loaded = await _svc.LoadAsync(_tempDir);

        Assert.Single(loaded);
        var c = loaded[0];
        Assert.Equal("c1", c.Id);
        Assert.Equal("Test Collection", c.Name);
        Assert.Equal("A test", c.Description);
        Assert.Equal(2, c.Tags.Count);
        Assert.Single(c.Passages);
        var p = c.Passages[0];
        Assert.Equal("p1", p.Id);
        Assert.Equal("xml-p5/T/T0001.xml", p.SourceRelPath);
        Assert.Equal("some chinese", p.ZhText);
        Assert.Equal("some english", p.EnText);
        Assert.Equal("a note", p.Notes);
        Assert.Single(p.Tags);
        Assert.Single(p.MasterNames);
    }

    [Fact]
    public async Task SaveAsync_EmptyList_ThenLoadAsync_ReturnsEmpty()
    {
        await _svc.SaveAsync(_tempDir, new List<ScholarCollection>());
        var loaded = await _svc.LoadAsync(_tempDir);

        Assert.Empty(loaded);
    }

    [Fact]
    public async Task SaveAsync_NullCollections_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(() => _svc.SaveAsync(_tempDir, null!));
    }

    [Fact]
    public async Task LoadAsync_NullRoot_Throws()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _svc.LoadAsync(null!));
    }

    [Fact]
    public async Task SaveAsync_NullRoot_Throws()
    {
        await Assert.ThrowsAsync<ArgumentException>(() => _svc.SaveAsync(null!, new List<ScholarCollection>()));
    }

    [Fact]
    public async Task SaveAsync_MultipleCollections_RoundTrips()
    {
        var collections = new List<ScholarCollection>
        {
            new() { Id = "c1", Name = "First" },
            new() { Id = "c2", Name = "Second" },
            new() { Id = "c3", Name = "Third" }
        };

        await _svc.SaveAsync(_tempDir, collections);
        var loaded = await _svc.LoadAsync(_tempDir);

        Assert.Equal(3, loaded.Count);
        Assert.Equal("First", loaded[0].Name);
        Assert.Equal("Second", loaded[1].Name);
        Assert.Equal("Third", loaded[2].Name);
    }
}
