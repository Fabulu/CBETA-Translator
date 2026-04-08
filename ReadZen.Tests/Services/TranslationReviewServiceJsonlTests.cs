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

namespace ReadZen.Tests.Services;

public class TranslationReviewServiceJsonlTests : IDisposable
{
    private readonly string _tempDir;
    private readonly TranslationReviewService _svc = new();

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };

    public TranslationReviewServiceJsonlTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), "readzen-review-test-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, true); } catch { }
    }

    /// <summary>
    /// Build a fake repo root with a local ledger containing the given entries.
    /// The community reviews dir is at {root}/community/reviews.
    /// </summary>
    private async Task<(string root, string communityDir)> SetupRepoWithLedgerAsync(
        params TranslationReviewEntry[] entries)
    {
        var root = Path.Combine(_tempDir, "repo-" + Guid.NewGuid().ToString("N")[..6]);
        Directory.CreateDirectory(root);

        var ledgerPath = TranslationReviewService.GetLedgerPath(root);
        var sb = new StringBuilder();
        foreach (var e in entries)
            sb.AppendLine(JsonSerializer.Serialize(e, JsonOpts));
        await File.WriteAllTextAsync(ledgerPath, sb.ToString(), new UTF8Encoding(false));

        var communityDir = Path.Combine(root, "community", "reviews");
        return (root, communityDir);
    }

    // ---- 1. WriteUserReviewJsonlAsync — writes compact JSONL format ----

    [Fact]
    public async Task WriteUserReviewJsonlAsync_WritesCompactJsonlFormat()
    {
        var entries = new[]
        {
            new TranslationReviewEntry
            {
                SegmentKey = "T2076/T2076_.xml|Body|1",
                RelPath = "T2076/T2076_.xml",
                Mode = "Body",
                BlockNumber = 1,
                ZhText = "\u4f60\u597d",
                EnText = "Hello",
                Status = "approved",
                Reviewer = "alice"
            },
            new TranslationReviewEntry
            {
                SegmentKey = "T2076/T2076_.xml|Body|2",
                RelPath = "T2076/T2076_.xml",
                Mode = "Body",
                BlockNumber = 2,
                ZhText = "\u4e16\u754c",
                EnText = "World",
                Status = "approved",
                Reviewer = "alice"
            }
        };

        var (root, communityDir) = await SetupRepoWithLedgerAsync(entries);

        await _svc.WriteUserReviewJsonlAsync(communityDir, "alice");

        var path = Path.Combine(communityDir, "alice.jsonl");
        Assert.True(File.Exists(path));

        var lines = (await File.ReadAllLinesAsync(path))
            .Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();
        Assert.Equal(2, lines.Length);

        // Each line should be compact JSON (no indentation)
        foreach (var line in lines)
        {
            Assert.DoesNotContain("\n", line);
            Assert.DoesNotContain("  ", line); // no indentation
        }

        // Parse to verify structure
        var parsed1 = JsonSerializer.Deserialize<TranslationReviewEntry>(lines[0], JsonOpts);
        Assert.NotNull(parsed1);
        Assert.Equal("T2076/T2076_.xml|Body|1", parsed1!.SegmentKey);
        Assert.Equal("approved", parsed1.Status);
    }

    // ---- 2. WriteUserReviewJsonlAsync — creates directory if missing ----

    [Fact]
    public async Task WriteUserReviewJsonlAsync_CreatesDirectoryIfMissing()
    {
        var entry = new TranslationReviewEntry
        {
            SegmentKey = "T2076/T2076_.xml|Body|1",
            RelPath = "T2076/T2076_.xml",
            Mode = "Body",
            BlockNumber = 1,
            ZhText = "\u4f60\u597d",
            EnText = "Hello",
            Status = "approved",
            Reviewer = "bob"
        };
        var (root, communityDir) = await SetupRepoWithLedgerAsync(entry);

        // The community dir should not exist yet (SetupRepoWithLedgerAsync does not create it)
        Assert.False(Directory.Exists(communityDir));

        await _svc.WriteUserReviewJsonlAsync(communityDir, "bob");

        Assert.True(Directory.Exists(communityDir));
        Assert.True(File.Exists(Path.Combine(communityDir, "bob.jsonl")));
    }

    // ---- 3. WriteUserReviewJsonlAsync — sanitizes filename ----

    [Fact]
    public async Task WriteUserReviewJsonlAsync_SanitizesFilename()
    {
        var entry = new TranslationReviewEntry
        {
            SegmentKey = "T2076/T2076_.xml|Body|1",
            RelPath = "T2076/T2076_.xml",
            Mode = "Body",
            BlockNumber = 1,
            ZhText = "\u4f60\u597d",
            EnText = "Hello",
            Status = "approved",
            Reviewer = "user.name"
        };
        var (root, communityDir) = await SetupRepoWithLedgerAsync(entry);

        await _svc.WriteUserReviewJsonlAsync(communityDir, "user.name");

        // Dots and spaces are stripped by SanitizeFilename
        var expectedPath = Path.Combine(communityDir, "username.jsonl");
        Assert.True(File.Exists(expectedPath));
    }

    // ---- 4. WriteUserReviewJsonlAsync — path traversal throws ----

    [Fact]
    public async Task WriteUserReviewJsonlAsync_EmptyUsername_ThrowsArgumentException()
    {
        var (root, communityDir) = await SetupRepoWithLedgerAsync();
        Directory.CreateDirectory(communityDir);

        await Assert.ThrowsAsync<ArgumentException>(
            () => _svc.WriteUserReviewJsonlAsync(communityDir, ""));
    }

    [Fact]
    public async Task WriteUserReviewJsonlAsync_NullUsername_ThrowsArgumentException()
    {
        var (root, communityDir) = await SetupRepoWithLedgerAsync();
        Directory.CreateDirectory(communityDir);

        await Assert.ThrowsAsync<ArgumentException>(
            () => _svc.WriteUserReviewJsonlAsync(communityDir, null!));
    }

    // ---- 5. WriteUserReviewJsonlAsync — filters entries by username ----

    [Fact]
    public async Task WriteUserReviewJsonlAsync_FiltersEntriesByUsername()
    {
        var entries = new[]
        {
            new TranslationReviewEntry
            {
                SegmentKey = "T2076/T2076_.xml|Body|1",
                RelPath = "T2076/T2076_.xml",
                Mode = "Body",
                BlockNumber = 1,
                ZhText = "\u4f60\u597d",
                EnText = "Hello",
                Status = "approved",
                Reviewer = "alice"
            },
            new TranslationReviewEntry
            {
                SegmentKey = "T2076/T2076_.xml|Body|2",
                RelPath = "T2076/T2076_.xml",
                Mode = "Body",
                BlockNumber = 2,
                ZhText = "\u4e16\u754c",
                EnText = "World",
                Status = "needs-work",
                Reviewer = "bob"
            },
            new TranslationReviewEntry
            {
                SegmentKey = "T2076/T2076_.xml|Body|3",
                RelPath = "T2076/T2076_.xml",
                Mode = "Body",
                BlockNumber = 3,
                ZhText = "\u6cd5",
                EnText = "Dharma",
                Status = "approved",
                Reviewer = "alice"
            }
        };

        var (root, communityDir) = await SetupRepoWithLedgerAsync(entries);

        await _svc.WriteUserReviewJsonlAsync(communityDir, "alice");

        var path = Path.Combine(communityDir, "alice.jsonl");
        var lines = (await File.ReadAllLinesAsync(path))
            .Where(l => !string.IsNullOrWhiteSpace(l)).ToArray();

        // Only alice's 2 entries should be written
        Assert.Equal(2, lines.Length);

        foreach (var line in lines)
        {
            var parsed = JsonSerializer.Deserialize<TranslationReviewEntry>(line, JsonOpts);
            Assert.Equal("alice", parsed!.Reviewer);
        }
    }

    // ---- 6. RefreshAggregationCacheAsync — loads multiple user files ----

    [Fact]
    public async Task RefreshAggregationCacheAsync_LoadsMultipleUserFiles()
    {
        var root = Path.Combine(_tempDir, "repo-agg");
        Directory.CreateDirectory(root);

        var communityDir = Path.Combine(root, "community", "reviews");
        Directory.CreateDirectory(communityDir);

        // Write alice's community file
        var aliceEntry = new TranslationReviewEntry
        {
            SegmentKey = "T2076/T2076_.xml|Body|1",
            RelPath = "T2076/T2076_.xml",
            Mode = "Body",
            BlockNumber = 1,
            ZhText = "\u4f60\u597d",
            EnText = "Hello",
            Status = "approved",
            Reviewer = "alice",
            ReviewedUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };
        await File.WriteAllTextAsync(
            Path.Combine(communityDir, "alice.jsonl"),
            JsonSerializer.Serialize(aliceEntry, JsonOpts) + Environment.NewLine);

        // Write bob's community file
        var bobEntry = new TranslationReviewEntry
        {
            SegmentKey = "T2076/T2076_.xml|Body|1",
            RelPath = "T2076/T2076_.xml",
            Mode = "Body",
            BlockNumber = 1,
            ZhText = "\u4f60\u597d",
            EnText = "Hello",
            Status = "needs-work",
            Reviewer = "bob",
            ReviewedUtc = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc)
        };
        await File.WriteAllTextAsync(
            Path.Combine(communityDir, "bob.jsonl"),
            JsonSerializer.Serialize(bobEntry, JsonOpts) + Environment.NewLine);

        await _svc.RefreshAggregationCacheAsync(root, communityDir);

        var agg = _svc.GetAggregatedReview("T2076/T2076_.xml|Body|1");
        Assert.NotNull(agg);
        Assert.Equal(2, agg!.ByReviewer.Count);
        Assert.True(agg.ByReviewer.ContainsKey("alice"));
        Assert.True(agg.ByReviewer.ContainsKey("bob"));
        Assert.Equal("approved", agg.ByReviewer["alice"].Status);
        Assert.Equal("needs-work", agg.ByReviewer["bob"].Status);
    }

    // ---- 7. RefreshAggregationCacheAsync — empty dir returns empty cache ----

    [Fact]
    public async Task RefreshAggregationCacheAsync_EmptyDir_ReturnsEmptyCache()
    {
        var root = Path.Combine(_tempDir, "repo-empty");
        Directory.CreateDirectory(root);

        var communityDir = Path.Combine(root, "community", "reviews");
        Directory.CreateDirectory(communityDir);

        await _svc.RefreshAggregationCacheAsync(root, communityDir);

        var agg = _svc.GetAggregatedReview("anything");
        Assert.Null(agg);
    }

    // ---- 8. RefreshAggregationCacheAsync — local ledger takes precedence for same reviewer ----

    [Fact]
    public async Task RefreshAggregationCacheAsync_LocalLedgerTakesPrecedenceForSameReviewer()
    {
        var root = Path.Combine(_tempDir, "repo-precedence");
        Directory.CreateDirectory(root);

        var communityDir = Path.Combine(root, "community", "reviews");
        Directory.CreateDirectory(communityDir);

        // Community file has alice's older review as "needs-work"
        var communityEntry = new TranslationReviewEntry
        {
            SegmentKey = "T2076/T2076_.xml|Body|1",
            RelPath = "T2076/T2076_.xml",
            Mode = "Body",
            BlockNumber = 1,
            ZhText = "\u4f60\u597d",
            EnText = "Hello",
            Status = "needs-work",
            Reviewer = "alice",
            ReviewedUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        };
        await File.WriteAllTextAsync(
            Path.Combine(communityDir, "alice.jsonl"),
            JsonSerializer.Serialize(communityEntry, JsonOpts) + Environment.NewLine);

        // Local ledger has alice's newer review as "approved"
        var localEntry = new TranslationReviewEntry
        {
            SegmentKey = "T2076/T2076_.xml|Body|1",
            RelPath = "T2076/T2076_.xml",
            Mode = "Body",
            BlockNumber = 1,
            ZhText = "\u4f60\u597d",
            EnText = "Hello",
            Status = "approved",
            Reviewer = "alice",
            ReviewedUtc = new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc)
        };
        var ledgerPath = TranslationReviewService.GetLedgerPath(root);
        await File.WriteAllTextAsync(ledgerPath,
            JsonSerializer.Serialize(localEntry, JsonOpts) + Environment.NewLine,
            new UTF8Encoding(false));

        await _svc.RefreshAggregationCacheAsync(root, communityDir);

        var agg = _svc.GetAggregatedReview("T2076/T2076_.xml|Body|1");
        Assert.NotNull(agg);
        // Local ledger entry should win (newer date)
        Assert.Equal("approved", agg!.ByReviewer["alice"].Status);
    }

    // ---- 9. GetAggregatedReview — returns correct aggregation ----

    [Fact]
    public async Task GetAggregatedReview_ReturnsCorrectAggregation()
    {
        var root = Path.Combine(_tempDir, "repo-correct-agg");
        Directory.CreateDirectory(root);

        var communityDir = Path.Combine(root, "community", "reviews");
        Directory.CreateDirectory(communityDir);

        // Two reviewers with different statuses for same segment
        var entries = new StringBuilder();
        entries.AppendLine(JsonSerializer.Serialize(new TranslationReviewEntry
        {
            SegmentKey = "file.xml|Body|1",
            RelPath = "file.xml",
            Mode = "Body",
            BlockNumber = 1,
            ZhText = "\u4f60\u597d",
            EnText = "Hello",
            Status = "approved",
            Reviewer = "alice",
            ReviewedUtc = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
        }, JsonOpts));
        entries.AppendLine(JsonSerializer.Serialize(new TranslationReviewEntry
        {
            SegmentKey = "file.xml|Body|1",
            RelPath = "file.xml",
            Mode = "Body",
            BlockNumber = 1,
            ZhText = "\u4f60\u597d",
            EnText = "Hello",
            Status = "rejected",
            Reviewer = "bob",
            ReviewedUtc = new DateTime(2026, 1, 2, 0, 0, 0, DateTimeKind.Utc)
        }, JsonOpts));

        await File.WriteAllTextAsync(
            Path.Combine(communityDir, "combined.jsonl"),
            entries.ToString());

        await _svc.RefreshAggregationCacheAsync(root, communityDir);

        var agg = _svc.GetAggregatedReview("file.xml|Body|1");
        Assert.NotNull(agg);
        Assert.Equal(1, agg!.ApprovalCount);
        Assert.Contains("alice", agg.ApprovedBy);
        Assert.Contains("bob", agg.RejectedBy);
    }

    // ---- 10. GetAggregatedReview — returns null for unknown segment ----

    [Fact]
    public async Task GetAggregatedReview_UnknownSegment_ReturnsNull()
    {
        var root = Path.Combine(_tempDir, "repo-unknown");
        Directory.CreateDirectory(root);

        await _svc.RefreshAggregationCacheAsync(root, null);

        var result = _svc.GetAggregatedReview("nonexistent|Body|999");
        Assert.Null(result);
    }

    // ---- 11. SegmentReviewAggregation — ApprovedBy/NeedsWorkBy/RejectedBy computed correctly ----

    [Fact]
    public void SegmentReviewAggregation_ComputedProperties_ReturnCorrectReviewers()
    {
        var agg = new SegmentReviewAggregation
        {
            SegmentKey = "test|Body|1",
            ByReviewer = new Dictionary<string, TranslationReviewEntry>(StringComparer.OrdinalIgnoreCase)
            {
                ["alice"] = new TranslationReviewEntry { Status = "approved", Reviewer = "alice" },
                ["bob"] = new TranslationReviewEntry { Status = "needs-work", Reviewer = "bob" },
                ["carol"] = new TranslationReviewEntry { Status = "approved", Reviewer = "carol" },
                ["dave"] = new TranslationReviewEntry { Status = "rejected", Reviewer = "dave" }
            }
        };

        var approvedBy = agg.ApprovedBy.ToList();
        var needsWorkBy = agg.NeedsWorkBy.ToList();
        var rejectedBy = agg.RejectedBy.ToList();

        Assert.Equal(2, approvedBy.Count);
        Assert.Contains("alice", approvedBy);
        Assert.Contains("carol", approvedBy);

        Assert.Single(needsWorkBy);
        Assert.Contains("bob", needsWorkBy);

        Assert.Single(rejectedBy);
        Assert.Contains("dave", rejectedBy);

        Assert.Equal(2, agg.ApprovalCount);
    }

    [Fact]
    public void SegmentReviewAggregation_EmptyByReviewer_AllComputedPropertiesEmpty()
    {
        var agg = new SegmentReviewAggregation { SegmentKey = "test|Body|1" };

        Assert.Empty(agg.ApprovedBy);
        Assert.Empty(agg.NeedsWorkBy);
        Assert.Empty(agg.RejectedBy);
        Assert.Equal(0, agg.ApprovalCount);
    }
}
