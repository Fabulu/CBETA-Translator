using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ReadZen.App.Models;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

public class ScholarCollectionsServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly ScholarCollectionsService _svc = new();

    public ScholarCollectionsServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "readzen-test-" + Guid.NewGuid().ToString("N")[..8]);
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

    // ---- JSONL: WriteUserJsonlAsync ----

    [Fact]
    public async Task WriteUserJsonlAsync_WritesCorrectFormat()
    {
        var communityDir = Path.Combine(_tempDir, "community", "collections");
        var collections = new List<ScholarCollection>
        {
            new() { Id = "c1", Name = "First Collection" },
            new() { Id = "c2", Name = "Second Collection" }
        };

        await _svc.WriteUserJsonlAsync(communityDir, "alice", collections);

        var path = Path.Combine(communityDir, "alice.jsonl");
        Assert.True(File.Exists(path));

        var lines = await File.ReadAllLinesAsync(path);
        // Filter out empty lines (trailing newline)
        var nonEmpty = lines.Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
        Assert.Equal(2, nonEmpty.Length);

        // Each line should be compact JSON (no indentation)
        foreach (var line in nonEmpty)
        {
            Assert.DoesNotContain("\n", line);
            Assert.DoesNotContain("  ", line); // no indentation in compact JSON
        }

        // Parse to verify structure
        var parsed1 = System.Text.Json.JsonSerializer.Deserialize<ScholarCollection>(nonEmpty[0]);
        Assert.NotNull(parsed1);
        Assert.Equal("c1", parsed1!.Id);
        Assert.Equal("First Collection", parsed1.Name);

        var parsed2 = System.Text.Json.JsonSerializer.Deserialize<ScholarCollection>(nonEmpty[1]);
        Assert.NotNull(parsed2);
        Assert.Equal("c2", parsed2!.Id);
    }

    [Fact]
    public async Task WriteUserJsonlAsync_CreatesDirectoryIfMissing()
    {
        var communityDir = Path.Combine(_tempDir, "deep", "nested", "path");
        Assert.False(Directory.Exists(communityDir));

        await _svc.WriteUserJsonlAsync(communityDir, "bob", new List<ScholarCollection>
        {
            new() { Id = "c1", Name = "Test" }
        });

        Assert.True(Directory.Exists(communityDir));
        Assert.True(File.Exists(Path.Combine(communityDir, "bob.jsonl")));
    }

    [Fact]
    public async Task WriteUserJsonlAsync_OverwritesExistingFile()
    {
        var communityDir = Path.Combine(_tempDir, "community");
        Directory.CreateDirectory(communityDir);

        // Write first version
        await _svc.WriteUserJsonlAsync(communityDir, "alice", new List<ScholarCollection>
        {
            new() { Id = "c1", Name = "Original" }
        });

        // Overwrite with new version
        await _svc.WriteUserJsonlAsync(communityDir, "alice", new List<ScholarCollection>
        {
            new() { Id = "c2", Name = "Replacement" },
            new() { Id = "c3", Name = "Extra" }
        });

        var lines = (await File.ReadAllLinesAsync(Path.Combine(communityDir, "alice.jsonl")))
            .Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
        Assert.Equal(2, lines.Length);

        var parsed = System.Text.Json.JsonSerializer.Deserialize<ScholarCollection>(lines[0]);
        Assert.Equal("c2", parsed!.Id);
        Assert.Equal("Replacement", parsed.Name);
    }

    // ---- JSONL: LoadAllCommunityJsonlAsync ----

    [Fact]
    public async Task LoadAllCommunityJsonlAsync_ReadsMultipleUserFiles()
    {
        var communityDir = Path.Combine(_tempDir, "community");
        Directory.CreateDirectory(communityDir);

        // Write two user files
        await _svc.WriteUserJsonlAsync(communityDir, "alice", new List<ScholarCollection>
        {
            new() { Id = "a1", Name = "Alice Collection" }
        });
        await _svc.WriteUserJsonlAsync(communityDir, "bob", new List<ScholarCollection>
        {
            new() { Id = "b1", Name = "Bob Collection 1" },
            new() { Id = "b2", Name = "Bob Collection 2" }
        });

        var result = await _svc.LoadAllCommunityJsonlAsync(communityDir);

        Assert.Equal(2, result.Count);
        Assert.True(result.ContainsKey("alice"));
        Assert.True(result.ContainsKey("bob"));
        Assert.Single(result["alice"]);
        Assert.Equal(2, result["bob"].Count);
        Assert.Equal("Alice Collection", result["alice"][0].Name);
    }

    [Fact]
    public async Task LoadAllCommunityJsonlAsync_ReturnsEmptyWhenDirectoryMissing()
    {
        var nonExistent = Path.Combine(_tempDir, "does-not-exist");

        var result = await _svc.LoadAllCommunityJsonlAsync(nonExistent);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    [Fact]
    public async Task LoadAllCommunityJsonlAsync_SkipsMalformedLines()
    {
        var communityDir = Path.Combine(_tempDir, "community");
        Directory.CreateDirectory(communityDir);

        // Write file with some malformed lines
        var content = "{\"Id\":\"c1\",\"Name\":\"Valid\"}\n" +
                      "THIS IS NOT JSON\n" +
                      "{broken json\n" +
                      "{\"Id\":\"c2\",\"Name\":\"Also Valid\"}\n";
        await File.WriteAllTextAsync(Path.Combine(communityDir, "alice.jsonl"), content);

        var result = await _svc.LoadAllCommunityJsonlAsync(communityDir);

        Assert.Single(result);
        Assert.Equal(2, result["alice"].Count);
        Assert.Equal("Valid", result["alice"][0].Name);
        Assert.Equal("Also Valid", result["alice"][1].Name);
    }

    [Fact]
    public async Task LoadAllCommunityJsonlAsync_HandlesEmptyFiles()
    {
        var communityDir = Path.Combine(_tempDir, "community");
        Directory.CreateDirectory(communityDir);

        // Write an empty file
        await File.WriteAllTextAsync(Path.Combine(communityDir, "empty.jsonl"), "");
        // Write a file with only whitespace/blank lines
        await File.WriteAllTextAsync(Path.Combine(communityDir, "blank.jsonl"), "\n\n  \n");

        var result = await _svc.LoadAllCommunityJsonlAsync(communityDir);

        // Neither file should produce entries (empty collections are not added)
        Assert.Empty(result);
    }

    // ---- GetCommunityCollectionsDir ----

    [Fact]
    public void GetCommunityCollectionsDir_ReturnsCorrectPath()
    {
        var dir = ScholarCollectionsService.GetCommunityCollectionsDir("/repo/root");

        Assert.Equal(Path.Combine("/repo/root", "community", "collections"), dir);
    }

    // ---- Round-trip: JSONL write then load ----

    [Fact]
    public async Task Jsonl_RoundTrip_ProducesSameData()
    {
        var communityDir = Path.Combine(_tempDir, "roundtrip");
        var original = new List<ScholarCollection>
        {
            new()
            {
                Id = "c1",
                Name = "Zen Koans",
                Description = "A curated collection of koans",
                Tags = new List<string> { "zen", "koan" },
                CreatedBy = "scholar1",
                CreatedUtc = new DateTimeOffset(2025, 6, 15, 12, 0, 0, TimeSpan.Zero),
                Passages = new List<ScholarPassage>
                {
                    new()
                    {
                        Id = "p1",
                        SourceRelPath = "xml-p5/T/T2076.xml",
                        ZhText = "some chinese",
                        EnText = "some english",
                        Notes = "interesting passage",
                        Tags = new List<string> { "dharma" },
                        MasterNames = new List<string> { "Huineng" },
                        AddedUtc = new DateTimeOffset(2025, 6, 16, 0, 0, 0, TimeSpan.Zero),
                        CreatedBy = "scholar1"
                    }
                }
            },
            new()
            {
                Id = "c2",
                Name = "Empty Collection",
                Description = "",
                Passages = new List<ScholarPassage>()
            }
        };

        await _svc.WriteUserJsonlAsync(communityDir, "scholar1", original);
        var loaded = await _svc.LoadAllCommunityJsonlAsync(communityDir);

        Assert.Single(loaded); // one user
        // Only c1 is expected since c2 has no passages... but actually LoadAll adds all deserialized.
        // Actually the service adds all parsed collections. c2 has 0 passages but still deserializes.
        var collections = loaded["scholar1"];
        Assert.Equal(2, collections.Count);

        var c1 = collections.First(c => c.Id == "c1");
        Assert.Equal("Zen Koans", c1.Name);
        Assert.Equal("A curated collection of koans", c1.Description);
        Assert.Equal(2, c1.Tags.Count);
        Assert.Contains("zen", c1.Tags);
        Assert.Equal("scholar1", c1.CreatedBy);
        Assert.Single(c1.Passages);

        var p1 = c1.Passages[0];
        Assert.Equal("p1", p1.Id);
        Assert.Equal("xml-p5/T/T2076.xml", p1.SourceRelPath);
        Assert.Equal("some chinese", p1.ZhText);
        Assert.Equal("some english", p1.EnText);
        Assert.Equal("interesting passage", p1.Notes);
        Assert.Contains("dharma", p1.Tags);
        Assert.Contains("Huineng", p1.MasterNames);
    }
}
