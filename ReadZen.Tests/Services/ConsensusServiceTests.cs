using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using ReadZen.App.Models;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

/// <summary>
/// Tests for ConsensusService: disagreement detection, JSONL round-trip,
/// empty-file handling, and DocumentTag Memo backward compatibility.
/// </summary>
public class ConsensusServiceTests : IDisposable
{
    private readonly string _tempDir;
    private readonly ConsensusService _svc = new();
    private const string RelPath = "T48/T48n2005.xml";

    public ConsensusServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "ConsensusTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    // ── Helpers ─────────────────────────────────────────────────────────

    private static List<string> MakeLbs(int count)
    {
        var lbs = new List<string>(count);
        for (int i = 1; i <= count; i++)
            lbs.Add($"p0{i:D3}a01");
        return lbs;
    }

    private static DocumentTag MakeTag(string tagId, string fromLb, string toLb)
        => new()
        {
            Id = Guid.NewGuid().ToString("N"),
            RelPath = RelPath,
            FromLb = fromLb,
            ToLb = toLb,
            TagId = tagId,
            CreatedUtc = DateTimeOffset.UtcNow
        };

    private static TagVocabulary MakeVocab(params (string id, string name)[] defs)
    {
        var v = new TagVocabulary();
        foreach (var (id, name) in defs)
            v.Tags.Add(new TagDefinition { Id = id, Name = name, CreatedUtc = DateTimeOffset.UtcNow });
        return v;
    }

    // ── Disagreement detection ──────────────────────────────────────────

    [Fact]
    public void FindDisagreements_DetectsKnownOverlaps()
    {
        var lbs = MakeLbs(4);
        var vocab = MakeVocab(("t1", "Theme"));

        // Coder1 tags lb1-3, coder2 tags lb2-4 -> disagree on lb1 (only c1) and lb4 (only c2)
        var tags1 = new List<DocumentTag> { MakeTag("t1", lbs[0], lbs[2]) };
        var tags2 = new List<DocumentTag> { MakeTag("t1", lbs[1], lbs[3]) };

        var disagreements = ConsensusService.FindDisagreements(RelPath, lbs, tags1, tags2, vocab, vocab);

        Assert.Equal(2, disagreements.Count);

        // lb1: only coder1
        var d1 = disagreements.Find(d => d.FromLb == lbs[0]);
        Assert.NotNull(d1);
        Assert.True(d1!.Coder1HasIt);
        Assert.False(d1.Coder2HasIt);

        // lb4: only coder2
        var d2 = disagreements.Find(d => d.FromLb == lbs[3]);
        Assert.NotNull(d2);
        Assert.False(d2!.Coder1HasIt);
        Assert.True(d2.Coder2HasIt);
    }

    [Fact]
    public void FindDisagreements_NoDisagreements_ReturnsEmpty()
    {
        var lbs = MakeLbs(3);
        var vocab = MakeVocab(("t1", "Theme"));

        var tags1 = new List<DocumentTag> { MakeTag("t1", lbs[0], lbs[2]) };
        var tags2 = new List<DocumentTag> { MakeTag("t1", lbs[0], lbs[2]) };

        var disagreements = ConsensusService.FindDisagreements(RelPath, lbs, tags1, tags2, vocab, vocab);
        Assert.Empty(disagreements);
    }

    // ── JSONL round-trip ────────────────────────────────────────────────

    [Fact]
    public async Task SaveAndLoadResolutions_RoundTrips()
    {
        var resolutions = new List<ConsensusResolution>
        {
            new()
            {
                Id = "r1",
                RelPath = RelPath,
                FromLb = "p0001a01",
                ToLb = "p0001a01",
                TagId = "t1",
                AcceptedCoder = "alice",
                RejectedCoder = "bob",
                ResolvedBy = "admin",
                ResolvedUtc = DateTimeOffset.UtcNow,
                Reason = "alice's tagging is more precise"
            },
            new()
            {
                Id = "r2",
                RelPath = RelPath,
                FromLb = "p0002a01",
                ToLb = "p0002a01",
                TagId = "t2",
                AcceptedCoder = "bob",
                RejectedCoder = "alice",
                ResolvedBy = "admin",
                ResolvedUtc = DateTimeOffset.UtcNow,
                Reason = null
            }
        };

        await _svc.SaveResolutionsAsync(_tempDir, "admin", resolutions);
        var loaded = await _svc.LoadResolutionsAsync(_tempDir, "admin");

        Assert.Equal(2, loaded.Count);
        Assert.Equal("r1", loaded[0].Id);
        Assert.Equal("alice", loaded[0].AcceptedCoder);
        Assert.Equal("alice's tagging is more precise", loaded[0].Reason);
        Assert.Equal("r2", loaded[1].Id);
        Assert.Null(loaded[1].Reason);
    }

    [Fact]
    public async Task LoadResolutions_EmptyFile_ReturnsEmpty()
    {
        // Create an empty file
        var dir = Path.Combine(_tempDir, "community", "consensus");
        Directory.CreateDirectory(dir);
        await File.WriteAllTextAsync(Path.Combine(dir, "admin.jsonl"), "");

        var loaded = await _svc.LoadResolutionsAsync(_tempDir, "admin");
        Assert.Empty(loaded);
    }

    [Fact]
    public async Task LoadResolutions_MissingFile_ReturnsEmpty()
    {
        var loaded = await _svc.LoadResolutionsAsync(_tempDir, "nonexistent");
        Assert.Empty(loaded);
    }

    // ── DocumentTag Memo backward compatibility ─────────────────────────

    [Fact]
    public void DocumentTag_DeserializeWithoutMemo_MemoIsNull()
    {
        // Simulate a JSONL line from before Memo was added
        string json = """{"Id":"dt1","RelPath":"T48/T48n2005.xml","FromLb":"p0001a01","ToLb":"p0001a01","TagId":"tag1","CreatedUtc":"2026-01-01T00:00:00+00:00"}""";

        var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var tag = JsonSerializer.Deserialize<DocumentTag>(json, opts);

        Assert.NotNull(tag);
        Assert.Null(tag!.Memo);
        Assert.Equal("dt1", tag.Id);
        Assert.Equal("tag1", tag.TagId);
    }

    [Fact]
    public void DocumentTag_DeserializeWithMemo_MemoIsSet()
    {
        string json = """{"Id":"dt1","RelPath":"T48/T48n2005.xml","FromLb":"p0001a01","ToLb":"p0001a01","TagId":"tag1","CreatedUtc":"2026-01-01T00:00:00+00:00","Memo":"This is a key passage"}""";

        var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var tag = JsonSerializer.Deserialize<DocumentTag>(json, opts);

        Assert.NotNull(tag);
        Assert.Equal("This is a key passage", tag!.Memo);
    }

    // ── Additional edge-case tests ─────────────────────────────────────

    [Fact]
    public void FindDisagreements_IdenticalTags_ReturnsEmpty()
    {
        var lbs = MakeLbs(5);
        var vocab = MakeVocab(("t1", "Theme"), ("t2", "Metaphor"));

        var tags1 = new List<DocumentTag>
        {
            MakeTag("t1", lbs[0], lbs[2]),
            MakeTag("t2", lbs[3], lbs[4])
        };
        var tags2 = new List<DocumentTag>
        {
            MakeTag("t1", lbs[0], lbs[2]),
            MakeTag("t2", lbs[3], lbs[4])
        };

        var disagreements = ConsensusService.FindDisagreements(RelPath, lbs, tags1, tags2, vocab, vocab);
        Assert.Empty(disagreements);
    }

    [Fact]
    public void FindDisagreements_CompletelyDifferent_AllAreDisagreements()
    {
        var lbs = MakeLbs(3);
        var vocab = MakeVocab(("t1", "Theme"), ("t2", "Metaphor"));

        // Coder1 uses t1 on all lbs, coder2 uses t2 on all lbs
        var tags1 = new List<DocumentTag> { MakeTag("t1", lbs[0], lbs[2]) };
        var tags2 = new List<DocumentTag> { MakeTag("t2", lbs[0], lbs[2]) };

        var disagreements = ConsensusService.FindDisagreements(RelPath, lbs, tags1, tags2, vocab, vocab);

        // For t1: 3 lbs only coder1 has it; for t2: 3 lbs only coder2 has it -> 6 total
        Assert.Equal(6, disagreements.Count);

        var t1Only = disagreements.FindAll(d => d.TagId == "t1");
        Assert.Equal(3, t1Only.Count);
        Assert.All(t1Only, d => { Assert.True(d.Coder1HasIt); Assert.False(d.Coder2HasIt); });

        var t2Only = disagreements.FindAll(d => d.TagId == "t2");
        Assert.Equal(3, t2Only.Count);
        Assert.All(t2Only, d => { Assert.False(d.Coder1HasIt); Assert.True(d.Coder2HasIt); });
    }

    [Fact]
    public async Task SaveAndLoad_SpecialCharactersInReason_RoundTrips()
    {
        var resolutions = new List<ConsensusResolution>
        {
            new()
            {
                Id = "r_special",
                RelPath = RelPath,
                FromLb = "p0001a01",
                ToLb = "p0001a01",
                TagId = "t1",
                AcceptedCoder = "alice",
                RejectedCoder = "bob",
                ResolvedBy = "admin",
                ResolvedUtc = DateTimeOffset.UtcNow,
                Reason = "Unicode: \u4e2d\u6587\u200b\u00e9\u00f1 \u2014 quotes: \"hello\" \u2018world\u2019 \u2014 backslash: \\\\ \u2014 newline: line1\nline2"
            }
        };

        await _svc.SaveResolutionsAsync(_tempDir, "admin", resolutions);
        var loaded = await _svc.LoadResolutionsAsync(_tempDir, "admin");

        Assert.Single(loaded);
        Assert.Equal(resolutions[0].Reason, loaded[0].Reason);
    }

    [Fact]
    public async Task ConcurrentSave_FileNotCorrupted()
    {
        // Save twice concurrently; both writes should complete without file corruption
        var res1 = new List<ConsensusResolution>
        {
            new()
            {
                Id = "concurrent1",
                RelPath = RelPath,
                FromLb = "p0001a01",
                ToLb = "p0001a01",
                TagId = "t1",
                AcceptedCoder = "alice",
                RejectedCoder = "bob",
                ResolvedBy = "admin1",
                ResolvedUtc = DateTimeOffset.UtcNow,
                Reason = "first"
            }
        };
        var res2 = new List<ConsensusResolution>
        {
            new()
            {
                Id = "concurrent2",
                RelPath = RelPath,
                FromLb = "p0002a01",
                ToLb = "p0002a01",
                TagId = "t2",
                AcceptedCoder = "bob",
                RejectedCoder = "alice",
                ResolvedBy = "admin2",
                ResolvedUtc = DateTimeOffset.UtcNow,
                Reason = "second"
            }
        };

        // Run both saves concurrently with different usernames to avoid file contention
        var task1 = _svc.SaveResolutionsAsync(_tempDir, "user1", res1);
        var task2 = _svc.SaveResolutionsAsync(_tempDir, "user2", res2);
        await Task.WhenAll(task1, task2);

        // Both files should be loadable and not corrupted
        var loaded1 = await _svc.LoadResolutionsAsync(_tempDir, "user1");
        var loaded2 = await _svc.LoadResolutionsAsync(_tempDir, "user2");

        Assert.Single(loaded1);
        Assert.Equal("concurrent1", loaded1[0].Id);
        Assert.Single(loaded2);
        Assert.Equal("concurrent2", loaded2[0].Id);
    }

    [Fact]
    public void DocumentTag_MemoRoundTrip_SerializeDeserialize()
    {
        var tag = new DocumentTag
        {
            Id = "dt_memo",
            RelPath = RelPath,
            FromLb = "p0001a01",
            ToLb = "p0002a01",
            TagId = "tag1",
            CreatedUtc = DateTimeOffset.UtcNow,
            Memo = "Important passage about emptiness"
        };

        var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        string json = JsonSerializer.Serialize(tag, opts);
        var deserialized = JsonSerializer.Deserialize<DocumentTag>(json, opts);

        Assert.NotNull(deserialized);
        Assert.Equal("Important passage about emptiness", deserialized!.Memo);
        Assert.Equal(tag.Id, deserialized.Id);
        Assert.Equal(tag.FromLb, deserialized.FromLb);
        Assert.Equal(tag.ToLb, deserialized.ToLb);
    }

    [Fact]
    public void DocumentTag_NullMemo_SerializesCorrectly()
    {
        var tag = new DocumentTag
        {
            Id = "dt_nomemo",
            RelPath = RelPath,
            FromLb = "p0001a01",
            ToLb = "p0001a01",
            TagId = "tag1",
            CreatedUtc = DateTimeOffset.UtcNow,
            Memo = null
        };

        var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        string json = JsonSerializer.Serialize(tag, opts);

        // Deserialize should give null Memo
        var deserialized = JsonSerializer.Deserialize<DocumentTag>(json, opts);
        Assert.NotNull(deserialized);
        Assert.Null(deserialized!.Memo);
    }

    [Fact]
    public void DocumentTag_DeserializeWithoutMemoField_BackwardCompat()
    {
        // Manually construct JSON without Memo field at all (not even null)
        string json = """{"Id":"dt_old","RelPath":"T48/T48n2005.xml","FromLb":"p0001a01","ToLb":"p0001a01","TagId":"tag1","CreatedUtc":"2026-01-01T00:00:00+00:00"}""";

        var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var tag = JsonSerializer.Deserialize<DocumentTag>(json, opts);

        Assert.NotNull(tag);
        Assert.Null(tag!.Memo);
        Assert.Equal("dt_old", tag.Id);
    }

    [Fact]
    public void DocumentTag_DeserializeWithExplicitNullMemo_MemoIsNull()
    {
        string json = """{"Id":"dt_null","RelPath":"T48/T48n2005.xml","FromLb":"p0001a01","ToLb":"p0001a01","TagId":"tag1","CreatedUtc":"2026-01-01T00:00:00+00:00","Memo":null}""";

        var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
        var tag = JsonSerializer.Deserialize<DocumentTag>(json, opts);

        Assert.NotNull(tag);
        Assert.Null(tag!.Memo);
    }
}
