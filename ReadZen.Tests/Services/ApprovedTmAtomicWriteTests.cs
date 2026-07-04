using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ReadZen.App.Models;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

/// <summary>
/// Regression tests for audit item P1.6 (RUN-20260702-2259 R3-M5):
/// RebuildApprovedTranslationMemoryAsync wrote the community-shared
/// translation-memory.approved.jsonl with FileMode.Create directly on the final path,
/// so a crash/cancellation mid-write truncated it. The write is now tmp + atomic move,
/// matching the file's own sibling pattern (WriteUserReviewJsonlAsync).
/// </summary>
public sealed class ApprovedTmAtomicWriteTests : IDisposable
{
    private readonly string _root;

    public ApprovedTmAtomicWriteTests()
    {
        _root = Path.Combine(Path.GetTempPath(), "readzen-tm-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_root);
    }

    public void Dispose()
    {
        try { Directory.Delete(_root, recursive: true); } catch { }
    }

    private static CurrentSegmentContext Ctx(int block, string zh, string en) => new()
    {
        RelPath = "T/T48/T48n2005.xml",
        TextId = "T48n2005",
        BlockNumber = block,
        ZhText = zh,
        EnText = en,
    };

    [Fact]
    public async Task Rebuild_WritesCompleteFile_AndLeavesNoTmp()
    {
        var svc = new TranslationReviewService();
        await svc.AppendReviewAsync(_root, Ctx(1, "甲乙", "one two"), "Approved", "Rev", ct: CancellationToken.None);
        await svc.AppendReviewAsync(_root, Ctx(2, "丙丁", "three four"), "Approved", "Rev", ct: CancellationToken.None);

        var count = await svc.RebuildApprovedTranslationMemoryAsync(_root, CancellationToken.None);

        Assert.Equal(2, count);
        var path = TranslationReviewService.GetApprovedTmPath(_root);
        var content = await File.ReadAllTextAsync(path);
        Assert.Contains("one two", content);
        Assert.Contains("three four", content);
        Assert.Empty(Directory.GetFiles(_root, "*.tmp"));
    }

    [Fact]
    public async Task Rebuild_ReplacesExistingFileCompletely()
    {
        var svc = new TranslationReviewService();
        await svc.AppendReviewAsync(_root, Ctx(1, "甲乙", "one two"), "Approved", "Rev", ct: CancellationToken.None);

        // An existing approved TM (the community-shared artifact) is on disk; a rebuild
        // must atomically replace it with the complete new content. (A true mid-write
        // crash cannot be injected without an IO seam; the atomicity here is the
        // tmp + File.Move pattern, identical to the already-shipped sibling
        // WriteUserReviewJsonlAsync path in the same file.)
        var path = TranslationReviewService.GetApprovedTmPath(_root);
        await File.WriteAllTextAsync(path, "SENTINEL-PREVIOUS-CONTENT\n");

        var count = await svc.RebuildApprovedTranslationMemoryAsync(_root, CancellationToken.None);

        Assert.Equal(1, count);
        var content = await File.ReadAllTextAsync(path);
        Assert.DoesNotContain("SENTINEL-PREVIOUS-CONTENT", content);
        Assert.Contains("one two", content);
        Assert.Empty(Directory.GetFiles(_root, "*.tmp"));
    }
}
