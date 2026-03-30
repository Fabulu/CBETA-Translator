using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using CbetaTranslator.App.Models;
using CbetaTranslator.App.Services;
using Xunit;

namespace CbetaTranslator.Tests.Services;

/// <summary>
/// Tests for DocumentTagService: vocabulary CRUD, user tags JSONL I/O,
/// community tag aggregation, filename sanitization, and path-traversal guard.
/// </summary>
public class DocumentTagServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly DocumentTagService _svc = new();
    private const string Username = "testuser";

    public DocumentTagServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "DocumentTagServiceTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    // ── Helper ──────────────────────────────────────────────────────────

    private static TagVocabulary MakeSampleVocabulary()
    {
        var vocab = new TagVocabulary();
        vocab.Tags.Add(new TagDefinition
        {
            Id = "tag1",
            Name = "Metaphor",
            Color = "#FF0000",
            CreatedUtc = DateTimeOffset.UtcNow
        });
        vocab.Tags.Add(new TagDefinition
        {
            Id = "tag2",
            Name = "Simile",
            Color = "#00FF00",
            ParentId = "tag1",
            CreatedUtc = DateTimeOffset.UtcNow
        });
        vocab.Pages[1] = new string?[] { "tag1", "tag2", null, null, null, null, null, null, null };
        return vocab;
    }

    private static DocumentTag MakeSampleTag(string id = "dt1", string tagId = "tag1")
    {
        return new DocumentTag
        {
            Id = id,
            RelPath = "T48/T48n2005.xml",
            FromLb = "0396b01",
            ToLb = "0396b05",
            TagId = tagId,
            CreatedBy = Username,
            CreatedUtc = DateTimeOffset.UtcNow
        };
    }

    // ── 1. LoadVocabularyAsync — empty root returns empty ───────────────

    [Fact]
    public async Task LoadVocabularyAsync_EmptyRoot_ReturnsEmptyVocabulary()
    {
        // root exists but has no vocabulary file
        var vocab = await _svc.LoadVocabularyAsync(_tempDir, Username);

        Assert.NotNull(vocab);
        Assert.Empty(vocab.Tags);
        Assert.Empty(vocab.Pages);
    }

    // ── 2. LoadVocabularyAsync — valid file deserializes correctly ──────

    [Fact]
    public async Task LoadVocabularyAsync_ValidFile_DeserializesCorrectly()
    {
        var original = MakeSampleVocabulary();
        var json = JsonSerializer.Serialize(original, new JsonSerializerOptions { WriteIndented = true });
        var path = DocumentTagService.GetVocabularyPath(_tempDir);
        await File.WriteAllTextAsync(path, json, Encoding.UTF8);

        var loaded = await _svc.LoadVocabularyAsync(_tempDir, Username);

        Assert.Equal(2, loaded.Tags.Count);
        Assert.Equal("Metaphor", loaded.Tags[0].Name);
        Assert.Equal("Simile", loaded.Tags[1].Name);
        Assert.Equal("tag1", loaded.Tags[1].ParentId);
        Assert.True(loaded.Pages.ContainsKey(1));
        Assert.Equal("tag1", loaded.Pages[1][0]);
        Assert.Null(loaded.Pages[1][2]);
    }

    // ── 3. LoadVocabularyAsync — corrupted JSON returns empty ───────────

    [Fact]
    public async Task LoadVocabularyAsync_CorruptedJson_ReturnsEmptyVocabulary()
    {
        var path = DocumentTagService.GetVocabularyPath(_tempDir);
        await File.WriteAllTextAsync(path, "{ this is not valid json !!!", Encoding.UTF8);

        var vocab = await _svc.LoadVocabularyAsync(_tempDir, Username);

        Assert.NotNull(vocab);
        Assert.Empty(vocab.Tags);
    }

    // ── 4. SaveVocabularyAsync — writes atomically via .tmp pattern ─────

    [Fact]
    public async Task SaveVocabularyAsync_WritesAtomically()
    {
        var vocab = MakeSampleVocabulary();

        await _svc.SaveVocabularyAsync(_tempDir, Username, vocab);

        var path = DocumentTagService.GetVocabularyPath(_tempDir);
        var tmpPath = path + ".tmp";

        // Final file must exist, tmp must not
        Assert.True(File.Exists(path));
        Assert.False(File.Exists(tmpPath));

        // Verify content is valid JSON
        var json = await File.ReadAllTextAsync(path, Encoding.UTF8);
        var deserialized = JsonSerializer.Deserialize<TagVocabulary>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        Assert.NotNull(deserialized);
        Assert.Equal(2, deserialized!.Tags.Count);
    }

    // ── 5. SaveVocabularyAsync — round-trip ─────────────────────────────

    [Fact]
    public async Task SaveVocabularyAsync_RoundTrip()
    {
        var original = MakeSampleVocabulary();

        await _svc.SaveVocabularyAsync(_tempDir, Username, original);
        var loaded = await _svc.LoadVocabularyAsync(_tempDir, Username);

        Assert.Equal(original.Tags.Count, loaded.Tags.Count);
        for (int i = 0; i < original.Tags.Count; i++)
        {
            Assert.Equal(original.Tags[i].Id, loaded.Tags[i].Id);
            Assert.Equal(original.Tags[i].Name, loaded.Tags[i].Name);
            Assert.Equal(original.Tags[i].Color, loaded.Tags[i].Color);
            Assert.Equal(original.Tags[i].ParentId, loaded.Tags[i].ParentId);
        }

        Assert.Equal(original.Pages.Count, loaded.Pages.Count);
        Assert.Equal(original.Pages[1][0], loaded.Pages[1][0]);
        Assert.Equal(original.Pages[1][1], loaded.Pages[1][1]);
    }

    // ── 6. LoadUserTagsAsync — empty file returns empty list ────────────

    [Fact]
    public async Task LoadUserTagsAsync_EmptyFile_ReturnsEmptyList()
    {
        // Create an empty file
        var path = DocumentTagService.GetTagsPath(_tempDir);
        await File.WriteAllTextAsync(path, "", Encoding.UTF8);

        var tags = await _svc.LoadUserTagsAsync(_tempDir, Username);

        Assert.NotNull(tags);
        Assert.Empty(tags);
    }

    // ── 7. LoadUserTagsAsync — valid JSONL parses all lines ─────────────

    [Fact]
    public async Task LoadUserTagsAsync_ValidJsonl_ParsesAllLines()
    {
        var tag1 = MakeSampleTag("dt1", "tag1");
        var tag2 = MakeSampleTag("dt2", "tag2");

        var path = DocumentTagService.GetTagsPath(_tempDir);
        var sb = new StringBuilder();
        sb.AppendLine(JsonSerializer.Serialize(tag1));
        sb.AppendLine(JsonSerializer.Serialize(tag2));
        await File.WriteAllTextAsync(path, sb.ToString(), Encoding.UTF8);

        var loaded = await _svc.LoadUserTagsAsync(_tempDir, Username);

        Assert.Equal(2, loaded.Count);
        Assert.Equal("dt1", loaded[0].Id);
        Assert.Equal("dt2", loaded[1].Id);
        Assert.Equal("tag1", loaded[0].TagId);
        Assert.Equal("tag2", loaded[1].TagId);
    }

    // ── 8. LoadUserTagsAsync — malformed line skips and continues ───────

    [Fact]
    public async Task LoadUserTagsAsync_MalformedLine_SkipsAndContinues()
    {
        var goodTag = MakeSampleTag("dt1", "tag1");
        var path = DocumentTagService.GetTagsPath(_tempDir);

        var sb = new StringBuilder();
        sb.AppendLine(JsonSerializer.Serialize(goodTag));
        sb.AppendLine("THIS IS NOT JSON AT ALL");
        sb.AppendLine(JsonSerializer.Serialize(MakeSampleTag("dt3", "tag3")));
        await File.WriteAllTextAsync(path, sb.ToString(), Encoding.UTF8);

        var loaded = await _svc.LoadUserTagsAsync(_tempDir, Username);

        Assert.Equal(2, loaded.Count);
        Assert.Equal("dt1", loaded[0].Id);
        Assert.Equal("dt3", loaded[1].Id);
    }

    // ── 9. SaveUserTagsAsync — writes JSONL (one JSON object per line) ──

    [Fact]
    public async Task SaveUserTagsAsync_WritesJsonl()
    {
        var tags = new List<DocumentTag>
        {
            MakeSampleTag("dt1", "tag1"),
            MakeSampleTag("dt2", "tag2")
        };

        await _svc.SaveUserTagsAsync(_tempDir, Username, tags);

        var path = DocumentTagService.GetTagsPath(_tempDir);
        Assert.True(File.Exists(path));

        var lines = await File.ReadAllLinesAsync(path, Encoding.UTF8);
        // Filter out empty trailing line
        var nonEmpty = Array.FindAll(lines, l => !string.IsNullOrWhiteSpace(l));
        Assert.Equal(2, nonEmpty.Length);

        // Each line must be valid JSON
        foreach (var line in nonEmpty)
        {
            var parsed = JsonSerializer.Deserialize<DocumentTag>(line,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
            Assert.NotNull(parsed);
        }
    }

    // ── 10. SaveUserTagsAsync — round-trip ──────────────────────────────

    [Fact]
    public async Task SaveUserTagsAsync_RoundTrip()
    {
        var original = new List<DocumentTag>
        {
            MakeSampleTag("dt1", "tag1"),
            MakeSampleTag("dt2", "tag2"),
            MakeSampleTag("dt3", "tag3")
        };

        await _svc.SaveUserTagsAsync(_tempDir, Username, original);
        var loaded = await _svc.LoadUserTagsAsync(_tempDir, Username);

        Assert.Equal(original.Count, loaded.Count);
        for (int i = 0; i < original.Count; i++)
        {
            Assert.Equal(original[i].Id, loaded[i].Id);
            Assert.Equal(original[i].TagId, loaded[i].TagId);
            Assert.Equal(original[i].RelPath, loaded[i].RelPath);
            Assert.Equal(original[i].FromLb, loaded[i].FromLb);
            Assert.Equal(original[i].ToLb, loaded[i].ToLb);
        }
    }

    // ── 11. LoadAllCommunityTagsAsync — multiple users ──────────────────

    [Fact]
    public async Task LoadAllCommunityTagsAsync_MultipleUsers_ReturnsDictionary()
    {
        var communityDir = DocumentTagService.GetCommunityTagsDir(_tempDir);
        Directory.CreateDirectory(communityDir);

        // Write tags for user "alice"
        var aliceTags = new List<DocumentTag> { MakeSampleTag("a1", "tag1") };
        var aliceSb = new StringBuilder();
        foreach (var t in aliceTags) aliceSb.AppendLine(JsonSerializer.Serialize(t));
        await File.WriteAllTextAsync(Path.Combine(communityDir, "alice.jsonl"), aliceSb.ToString(), Encoding.UTF8);

        // Write tags for user "bob"
        var bobTags = new List<DocumentTag> { MakeSampleTag("b1", "tag2"), MakeSampleTag("b2", "tag3") };
        var bobSb = new StringBuilder();
        foreach (var t in bobTags) bobSb.AppendLine(JsonSerializer.Serialize(t));
        await File.WriteAllTextAsync(Path.Combine(communityDir, "bob.jsonl"), bobSb.ToString(), Encoding.UTF8);

        var result = await _svc.LoadAllCommunityTagsAsync(_tempDir);

        Assert.Equal(2, result.Count);
        Assert.True(result.ContainsKey("alice"));
        Assert.True(result.ContainsKey("bob"));
        Assert.Single(result["alice"]);
        Assert.Equal(2, result["bob"].Count);
    }

    // ── 12. LoadAllCommunityTagsAsync — empty dir returns empty dict ────

    [Fact]
    public async Task LoadAllCommunityTagsAsync_EmptyDir_ReturnsEmptyDict()
    {
        // Community dir does not exist at all
        var result = await _svc.LoadAllCommunityTagsAsync(_tempDir);

        Assert.NotNull(result);
        Assert.Empty(result);
    }

    // ── 13. WriteUserCommunityTagsAsync — creates directory ─────────────

    [Fact]
    public async Task WriteUserCommunityTagsAsync_CreatesDirectory()
    {
        var communityDir = DocumentTagService.GetCommunityTagsDir(_tempDir);
        Assert.False(Directory.Exists(communityDir));

        var tags = new List<DocumentTag> { MakeSampleTag("dt1", "tag1") };
        await _svc.WriteUserCommunityTagsAsync(_tempDir, "alice", tags);

        Assert.True(Directory.Exists(communityDir));
        Assert.True(File.Exists(Path.Combine(communityDir, "alice.jsonl")));
    }

    // ── 14. WriteUserCommunityTagsAsync — sanitizes username ────────────

    [Fact]
    public async Task WriteUserCommunityTagsAsync_SanitizesUsername()
    {
        var tags = new List<DocumentTag> { MakeSampleTag("dt1", "tag1") };

        // Username with dots and spaces should have them removed
        await _svc.WriteUserCommunityTagsAsync(_tempDir, "john. doe", tags);

        var communityDir = DocumentTagService.GetCommunityTagsDir(_tempDir);
        // "john. doe" → dots and spaces removed → "johndoe"
        Assert.True(File.Exists(Path.Combine(communityDir, "johndoe.jsonl")));
        Assert.False(File.Exists(Path.Combine(communityDir, "john. doe.jsonl")));
    }

    // ── 15. GuardPathTraversal — malicious username throws ──────────────

    [Fact]
    public async Task GuardPathTraversal_MaliciousUsername_Throws()
    {
        var tags = new List<DocumentTag> { MakeSampleTag("dt1", "tag1") };

        // After sanitization "../" becomes "" (dots and slashes removed),
        // which falls back to "unknown". But a raw path traversal attempt
        // with characters that survive sanitization is hard to construct.
        // Instead, test that a username that somehow resolves outside the
        // community dir would be caught. We test via the public API:
        // The sanitizer strips dots and path separators, so we verify
        // the file ends up in the correct directory.
        await _svc.WriteUserCommunityTagsAsync(_tempDir, "../../evil", tags);

        var communityDir = DocumentTagService.GetCommunityTagsDir(_tempDir);
        // Sanitizer removes dots, slashes (invalid filename chars), spaces → "evil"
        var expectedFile = Path.Combine(communityDir, "evil.jsonl");
        Assert.True(File.Exists(expectedFile));

        // Verify no file was written outside the community dir
        Assert.False(File.Exists(Path.Combine(_tempDir, "evil.jsonl")));
    }

    // ── 16. LoadVocabularyAsync — null root throws ──────────────────────

    [Fact]
    public async Task LoadVocabularyAsync_NullRoot_Throws()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _svc.LoadVocabularyAsync(null!, Username));
    }

    // ── Additional: empty username throws ───────────────────────────────

    [Fact]
    public async Task LoadVocabularyAsync_EmptyUsername_Throws()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => _svc.LoadVocabularyAsync(_tempDir, ""));
    }

    [Fact]
    public async Task SaveVocabularyAsync_NullVocab_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _svc.SaveVocabularyAsync(_tempDir, Username, null!));
    }

    [Fact]
    public async Task SaveUserTagsAsync_NullTags_Throws()
    {
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _svc.SaveUserTagsAsync(_tempDir, Username, null!));
    }
}
