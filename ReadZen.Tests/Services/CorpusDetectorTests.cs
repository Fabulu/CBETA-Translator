// Tests for CorpusDetector — folder-name heuristic and sentinel-file detection.
// The TEI-sample fallback is exercised indirectly through the extractor tests.

using System;
using System.IO;
using ReadZen.App.Models;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

public class CorpusDetectorTests
{
    [Fact]
    public void Detect_ReturnsUnknown_ForNullOrMissingPath()
    {
        Assert.Equal(CorpusKind.Unknown, CorpusDetector.Detect(null));
        Assert.Equal(CorpusKind.Unknown, CorpusDetector.Detect(""));
        Assert.Equal(CorpusKind.Unknown, CorpusDetector.Detect(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString())));
    }

    [Fact]
    public void Detect_FolderNameHeuristic_OpenZenTexts()
    {
        var dir = MakeTempDir("OpenZenTexts-sample");
        try { Assert.Equal(CorpusKind.Open, CorpusDetector.Detect(dir)); }
        finally { CleanupTempDir(dir); }
    }

    [Fact]
    public void Detect_FolderNameHeuristic_CbetaZenTexts()
    {
        var dir = MakeTempDir("CbetaZenTexts-dev");
        try { Assert.Equal(CorpusKind.Cbeta, CorpusDetector.Detect(dir)); }
        finally { CleanupTempDir(dir); }
    }

    [Fact]
    public void Detect_FolderNameHeuristic_MatchesChildDirectories()
    {
        // Parent folder doesn't match the heuristic, but a child does.
        // CorpusDetector should still pick up the corpus from the child name.
        var parent = MakeTempDir("zen-roots");
        try
        {
            Directory.CreateDirectory(Path.Combine(parent, "OpenZenTexts"));
            Directory.CreateDirectory(Path.Combine(parent, "OpenZenTranslations"));
            Assert.Equal(CorpusKind.Open, CorpusDetector.Detect(parent));
        }
        finally { CleanupTempDir(parent); }
    }

    [Fact]
    public void Detect_SentinelFile_OverridesFolderName()
    {
        // Folder name says "Cbeta" but the sentinel file says "open".
        // Sentinel wins because it's the first detection step.
        var dir = MakeTempDir("CbetaZenTexts-but-not-really");
        try
        {
            File.WriteAllText(Path.Combine(dir, ".readzen-corpus"), "open\n");
            Assert.Equal(CorpusKind.Open, CorpusDetector.Detect(dir));
        }
        finally { CleanupTempDir(dir); }
    }

    [Fact]
    public void Detect_SentinelFile_AcceptsCbetaValue()
    {
        var dir = MakeTempDir("ambiguous-name");
        try
        {
            File.WriteAllText(Path.Combine(dir, ".readzen-corpus"), "cbeta");
            Assert.Equal(CorpusKind.Cbeta, CorpusDetector.Detect(dir));
        }
        finally { CleanupTempDir(dir); }
    }

    [Fact]
    public void Detect_UnknownFolderName_ReturnsUnknown()
    {
        var dir = MakeTempDir("just-a-folder");
        try { Assert.Equal(CorpusKind.Unknown, CorpusDetector.Detect(dir)); }
        finally { CleanupTempDir(dir); }
    }

    private static string MakeTempDir(string name)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{name}-{Guid.NewGuid():N}");
        Directory.CreateDirectory(path);
        return path;
    }

    private static void CleanupTempDir(string path)
    {
        try { if (Directory.Exists(path)) Directory.Delete(path, recursive: true); }
        catch { }
    }
}
