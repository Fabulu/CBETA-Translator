using System;
using System.IO;
using System.Threading.Tasks;
using CbetaTranslator.App.Services;
using Xunit;

namespace CbetaTranslator.Tests.Services;

/// <summary>
/// Tests for SearchIndexService.IsStaleAsync and TranslationAssistantBuildService.IsReferenceStaleAsync.
/// Uses real file system via temp directories.
/// </summary>
public class IndexStalenessTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly string _origDir;
    private readonly string _tranDir;

    public IndexStalenessTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), "cbeta-test-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_tempRoot);
        _origDir = Path.Combine(_tempRoot, "xml-p5");
        _tranDir = Path.Combine(_tempRoot, "xml-p5t");
        Directory.CreateDirectory(_origDir);
        Directory.CreateDirectory(_tranDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempRoot, true); } catch { }
    }

    // ===== SearchIndexService.IsStaleAsync =====

    [Fact]
    public async Task IsStaleAsync_ReturnsTrueWhenManifestMissing()
    {
        // No manifest file exists at all
        var svc = new SearchIndexService();

        bool stale = await svc.IsStaleAsync(_tempRoot, _origDir, new[] { _tranDir });

        Assert.True(stale);
    }

    [Fact]
    public async Task IsStaleAsync_ReturnsFalseWhenAllFilesOlderThanManifest()
    {
        var svc = new SearchIndexService();

        // Create an XML file with old timestamp
        var xmlFile = Path.Combine(_origDir, "test.xml");
        File.WriteAllText(xmlFile, "<x/>");
        File.SetLastWriteTimeUtc(xmlFile, DateTime.UtcNow.AddHours(-2));

        // Build the index (creates manifest)
        await svc.BuildAsync(_tempRoot, _origDir, new[] { _tranDir });

        // Manifest should now be newer than the XML file
        bool stale = await svc.IsStaleAsync(_tempRoot, _origDir, new[] { _tranDir });

        Assert.False(stale);
    }

    [Fact]
    public async Task IsStaleAsync_ReturnsTrueWhenOneTranslatedFileNewerThanManifest()
    {
        var svc = new SearchIndexService();

        // Create initial XML files
        var origFile = Path.Combine(_origDir, "test.xml");
        File.WriteAllText(origFile, "<x/>");
        File.SetLastWriteTimeUtc(origFile, DateTime.UtcNow.AddHours(-2));

        var tranFile = Path.Combine(_tranDir, "test.xml");
        File.WriteAllText(tranFile, "<x/>");
        File.SetLastWriteTimeUtc(tranFile, DateTime.UtcNow.AddHours(-2));

        // Build the index
        await svc.BuildAsync(_tempRoot, _origDir, new[] { _tranDir });

        // Now touch the translated XML file to be newer than the manifest
        var manifestPath = svc.GetManifestPath(_tempRoot);
        var manifestTime = File.GetLastWriteTimeUtc(manifestPath);
        File.SetLastWriteTimeUtc(tranFile, manifestTime.AddSeconds(5));

        bool stale = await svc.IsStaleAsync(_tempRoot, _origDir, new[] { _tranDir });

        Assert.True(stale);
    }

    // ===== TranslationAssistantBuildService.IsReferenceStaleAsync =====

    [Fact]
    public async Task IsReferenceStaleAsync_ReturnsTrueWhenReferenceFileMissing()
    {
        var svc = new TranslationAssistantBuildService();

        // No reference file exists
        bool stale = await svc.IsReferenceStaleAsync(_tempRoot, _tranDir);

        Assert.True(stale);
    }

    [Fact]
    public async Task IsReferenceStaleAsync_ReturnsFalseWhenReferenceNewerThanAllTranslatedFiles()
    {
        var svc = new TranslationAssistantBuildService();

        // Create a translated XML file with old timestamp
        var xmlFile = Path.Combine(_tranDir, "test.xml");
        File.WriteAllText(xmlFile, "<x/>");
        File.SetLastWriteTimeUtc(xmlFile, DateTime.UtcNow.AddHours(-2));

        // Create reference file newer than the XML
        var refPath = Path.Combine(_tempRoot, "translation-memory.reference.jsonl");
        File.WriteAllText(refPath, "{}");
        // Reference is created "now", which is newer than -2h

        bool stale = await svc.IsReferenceStaleAsync(_tempRoot, _tranDir);

        Assert.False(stale);
    }

    [Fact]
    public async Task IsReferenceStaleAsync_ReturnsTrueWhenOneTranslatedFileNewer()
    {
        var svc = new TranslationAssistantBuildService();

        // Create reference file
        var refPath = Path.Combine(_tempRoot, "translation-memory.reference.jsonl");
        File.WriteAllText(refPath, "{}");
        var refTime = File.GetLastWriteTimeUtc(refPath);

        // Create a translated XML file newer than the reference
        var xmlFile = Path.Combine(_tranDir, "test.xml");
        File.WriteAllText(xmlFile, "<x/>");
        File.SetLastWriteTimeUtc(xmlFile, refTime.AddSeconds(5));

        bool stale = await svc.IsReferenceStaleAsync(_tempRoot, _tranDir);

        Assert.True(stale);
    }
}
