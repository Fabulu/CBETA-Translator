using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ReadZen.App.Models;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

/// <summary>
/// Behavior tests for <see cref="TranslationStarService"/> — community "star"
/// tracking. Covers star/unstar in-memory state (counts + per-user membership),
/// jsonl round-trip persistence, aggregate-count export, argument validation,
/// the path-traversal guard, filename sanitization (routed to
/// FileNameSanitizer.Strict since v8.0.0), and the anti-clobber guards.
/// </summary>
public sealed class TranslationStarServiceTests : IDisposable
{
    private readonly string _root;
    private readonly string _starsDir;

    public TranslationStarServiceTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "readzen-star-tests-" + Guid.NewGuid().ToString("N"));
        _starsDir = Path.Combine(_root, "community-stars");
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch { }
    }

    private static CancellationToken Ct => CancellationToken.None;

    [Fact]
    public async Task SetStar_RecordsCountAndUserMembership()
    {
        var svc = new TranslationStarService();

        await svc.SetStarAsync(_starsDir, "alice", "T48n2005", "Cleary", starred: true, Ct);

        Assert.Equal(1, svc.GetStarCount("T48n2005", "Cleary"));
        Assert.True(svc.IsStarredByUser("T48n2005", "Cleary", "alice"));
        Assert.False(svc.IsStarredByUser("T48n2005", "Cleary", "bob"));
    }

    [Fact]
    public async Task SetStar_IsIdempotent_NoDoubleCount()
    {
        var svc = new TranslationStarService();

        await svc.SetStarAsync(_starsDir, "alice", "F", "T", starred: true, Ct);
        await svc.SetStarAsync(_starsDir, "alice", "F", "T", starred: true, Ct);

        Assert.Equal(1, svc.GetStarCount("F", "T"));
    }

    [Fact]
    public async Task Unstar_DecrementsAndRemovesMembership()
    {
        var svc = new TranslationStarService();
        await svc.SetStarAsync(_starsDir, "alice", "F", "T", starred: true, Ct);
        await svc.SetStarAsync(_starsDir, "bob", "F", "T", starred: true, Ct);
        Assert.Equal(2, svc.GetStarCount("F", "T"));

        await svc.SetStarAsync(_starsDir, "alice", "F", "T", starred: false, Ct);

        Assert.Equal(1, svc.GetStarCount("F", "T"));
        Assert.False(svc.IsStarredByUser("F", "T", "alice"));
        Assert.True(svc.IsStarredByUser("F", "T", "bob"));
    }

    [Fact]
    public async Task Unstar_WhenNotStarred_IsNoOp()
    {
        var svc = new TranslationStarService();
        await svc.SetStarAsync(_starsDir, "alice", "F", "T", starred: false, Ct);
        Assert.Equal(0, svc.GetStarCount("F", "T"));
    }

    [Fact]
    public async Task GetMostStarredTranslator_ReturnsHighestCount()
    {
        var svc = new TranslationStarService();
        await svc.SetStarAsync(_starsDir, "alice", "F", "Cleary", starred: true, Ct);
        await svc.SetStarAsync(_starsDir, "bob", "F", "Cleary", starred: true, Ct);
        await svc.SetStarAsync(_starsDir, "carol", "F", "Watson", starred: true, Ct);

        Assert.Equal("Cleary", svc.GetMostStarredTranslator("F"));
    }

    [Fact]
    public void GetMostStarredTranslator_NoStars_ReturnsNull()
    {
        var svc = new TranslationStarService();
        Assert.Null(svc.GetMostStarredTranslator("nobody"));
    }

    [Fact]
    public async Task SetStar_PersistsJsonl_RoundTripsViaLoadAll()
    {
        var svc = new TranslationStarService();
        await svc.SetStarAsync(_starsDir, "alice", "T48n2005", "Cleary", starred: true, Ct);

        var path = Path.Combine(_starsDir, "alice.jsonl");
        Assert.True(File.Exists(path));

        // Fresh service loads the persisted state back.
        var reloaded = new TranslationStarService();
        await reloaded.LoadAllStarsAsync(_starsDir, Ct);

        Assert.Equal(1, reloaded.GetStarCount("T48n2005", "Cleary"));
        Assert.True(reloaded.IsStarredByUser("T48n2005", "Cleary", "alice"));
    }

    [Fact]
    public async Task LoadAll_SkipsMalformedLines()
    {
        Directory.CreateDirectory(_starsDir);
        var path = Path.Combine(_starsDir, "alice.jsonl");
        await File.WriteAllLinesAsync(path, new[]
        {
            "{ this is not json",
            "{\"fileId\":\"F\",\"translator\":\"T\",\"starredUtc\":\"2026-01-01\"}",
            "",
            "{\"fileId\":\"\",\"translator\":\"missing-fileid\"}",  // dropped: empty fileId
        });

        var svc = new TranslationStarService();
        await svc.LoadAllStarsAsync(_starsDir, Ct);

        Assert.Equal(1, svc.GetStarCount("F", "T"));
    }

    [Fact]
    public async Task LoadAll_MissingDirectory_ClearsState()
    {
        var svc = new TranslationStarService();
        await svc.SetStarAsync(_starsDir, "alice", "F", "T", starred: true, Ct);

        await svc.LoadAllStarsAsync(Path.Combine(_root, "no-such-dir"), Ct);

        Assert.Equal(0, svc.GetStarCount("F", "T"));
    }

    [Theory]
    [InlineData("", "F", "T")]
    [InlineData("alice", "", "T")]
    [InlineData("alice", "F", "")]
    public async Task SetStar_MissingRequiredArgs_Throws(string user, string fileId, string translator)
    {
        var svc = new TranslationStarService();
        await Assert.ThrowsAsync<ArgumentException>(
            () => svc.SetStarAsync(_starsDir, user, fileId, translator, starred: true, Ct));
    }

    [Fact]
    public async Task WriteUserStars_SanitizesUsernameIntoFilename()
    {
        var svc = new TranslationStarService();
        // Dots and spaces are stripped by FileNameSanitizer.Strict.
        await svc.SetStarAsync(_starsDir, "a l i.ce", "F", "T", starred: true, Ct);

        var files = Directory.GetFiles(_starsDir, "*.jsonl").Select(Path.GetFileName).ToList();
        Assert.Contains("alice.jsonl", files);
    }

    [Theory]
    [InlineData("", "alice")]   // missing community dir
    [InlineData("dir", "")]     // missing username
    public async Task WriteUserStars_MissingRequiredArgs_Throws(string dir, string user)
    {
        var svc = new TranslationStarService();
        await Assert.ThrowsAsync<ArgumentException>(
            () => svc.WriteUserStarsJsonlAsync(dir, user, Ct));
    }

    [Fact]
    public async Task WriteUserStars_TraversalUsername_IsNeutralizedToSafeFile()
    {
        var svc = new TranslationStarService();
        // Strict sanitization strips separators/dots, so a traversal attempt cannot
        // escape the community dir — it collapses to a benign name that stays inside.
        await svc.SetStarAsync(_starsDir, "../../etc", "F", "T", starred: true, Ct);

        foreach (var f in Directory.GetFiles(_starsDir, "*.jsonl"))
            Assert.Equal(_starsDir, Path.GetDirectoryName(Path.GetFullPath(f)));
    }

    [Fact]
    public async Task ExportAggregatedCounts_WritesStarCountsJson()
    {
        var svc = new TranslationStarService();
        await svc.SetStarAsync(_starsDir, "alice", "F1", "Cleary", starred: true, Ct);
        await svc.SetStarAsync(_starsDir, "bob", "F1", "Cleary", starred: true, Ct);

        await svc.ExportAggregatedCountsAsync(_root, Ct);

        var path = Path.Combine(_root, "star-counts.json");
        Assert.True(File.Exists(path));
        var json = await File.ReadAllTextAsync(path);
        Assert.Contains("F1:Cleary", json);
        Assert.Contains("2", json);
        Assert.Empty(Directory.GetFiles(_root, "*.tmp"));
    }

    [Fact]
    public async Task ExportAggregatedCounts_EmptyData_DoesNotClobberNonEmptyFile()
    {
        var path = Path.Combine(_root, "star-counts.json");
        await File.WriteAllTextAsync(path, "{\"existing:data\":7}");

        var svc = new TranslationStarService(); // no stars
        await svc.ExportAggregatedCountsAsync(_root, Ct);

        // Non-empty existing file must be preserved when there's nothing to export.
        Assert.Contains("existing:data", await File.ReadAllTextAsync(path));
    }
}
