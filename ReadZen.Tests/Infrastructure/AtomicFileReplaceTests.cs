using System;
using System.IO;
using Xunit;

namespace ReadZen.Tests.Infrastructure;

/// <summary>
/// Tests the atomic file backup/restore pattern used throughout the codebase
/// (e.g., SearchIndexService.ReplaceFileAtomicWithRetry, community file saves).
/// The pattern is: write to tmp, File.Replace(tmp, final, bak), delete bak.
/// This ensures crash safety: if the process dies mid-write, either the old
/// file or the new file survives, never a half-written file.
/// </summary>
public class AtomicFileReplaceTests : IDisposable
{
    private readonly string _testDir;

    public AtomicFileReplaceTests()
    {
        _testDir = Path.Combine(Path.GetTempPath(), "readzen-atomic-test-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(_testDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_testDir, true); } catch { }
    }

    /// <summary>
    /// Simulates the backup/restore pattern: write tmp, File.Replace with backup, cleanup.
    /// Verifies the final file has new content and the backup is cleaned up.
    /// </summary>
    [Fact]
    public void AtomicReplace_ExistingFile_ReplacesContentAndCleansBackup()
    {
        var finalPath = Path.Combine(_testDir, "data.json");
        var tmpPath = finalPath + ".tmp";
        var bakPath = finalPath + ".bak";

        // Setup: existing file with old content
        File.WriteAllText(finalPath, "old-content");

        // Write new content to tmp
        File.WriteAllText(tmpPath, "new-content");

        // Atomic replace pattern
        try { if (File.Exists(bakPath)) File.Delete(bakPath); } catch { }
        File.Replace(tmpPath, finalPath, bakPath, ignoreMetadataErrors: true);
        try { if (File.Exists(bakPath)) File.Delete(bakPath); } catch { }

        // Verify
        Assert.True(File.Exists(finalPath));
        Assert.Equal("new-content", File.ReadAllText(finalPath));
        Assert.False(File.Exists(tmpPath), "Tmp file should be consumed by File.Replace");
        Assert.False(File.Exists(bakPath), "Backup file should be cleaned up");
    }

    /// <summary>
    /// When the final file does not yet exist, the pattern falls back to File.Move.
    /// </summary>
    [Fact]
    public void AtomicReplace_NoExistingFile_MovesInPlace()
    {
        var finalPath = Path.Combine(_testDir, "new-data.json");
        var tmpPath = finalPath + ".tmp";

        File.WriteAllText(tmpPath, "fresh-content");

        // Pattern: no existing file, use Move instead of Replace
        if (File.Exists(finalPath))
        {
            var bak = finalPath + ".bak";
            try { if (File.Exists(bak)) File.Delete(bak); } catch { }
            File.Replace(tmpPath, finalPath, bak, ignoreMetadataErrors: true);
            try { if (File.Exists(bak)) File.Delete(bak); } catch { }
        }
        else
        {
            File.Move(tmpPath, finalPath);
        }

        Assert.True(File.Exists(finalPath));
        Assert.Equal("fresh-content", File.ReadAllText(finalPath));
        Assert.False(File.Exists(tmpPath));
    }

    /// <summary>
    /// After File.Replace crashes (simulated), the .bak file preserves the old content
    /// so it can be recovered.
    /// </summary>
    [Fact]
    public void AtomicReplace_BackupPreservesOldContent()
    {
        var finalPath = Path.Combine(_testDir, "crash.json");
        var tmpPath = finalPath + ".tmp";
        var bakPath = finalPath + ".bak";

        File.WriteAllText(finalPath, "precious-data");
        File.WriteAllText(tmpPath, "replacement");

        // Perform Replace but skip the backup cleanup (simulating crash after replace)
        try { if (File.Exists(bakPath)) File.Delete(bakPath); } catch { }
        File.Replace(tmpPath, finalPath, bakPath, ignoreMetadataErrors: true);
        // Intentionally do NOT delete bakPath — simulating crash

        Assert.True(File.Exists(bakPath), "Backup should exist after replace");
        Assert.Equal("precious-data", File.ReadAllText(bakPath));
        Assert.Equal("replacement", File.ReadAllText(finalPath));

        // Recovery: restore from backup
        File.Copy(bakPath, finalPath, overwrite: true);
        Assert.Equal("precious-data", File.ReadAllText(finalPath));
    }

    /// <summary>
    /// Verifies that an existing stale .bak file is cleaned up before the replace.
    /// </summary>
    [Fact]
    public void AtomicReplace_StaleBackup_IsCleanedBeforeReplace()
    {
        var finalPath = Path.Combine(_testDir, "stale.json");
        var tmpPath = finalPath + ".tmp";
        var bakPath = finalPath + ".bak";

        File.WriteAllText(finalPath, "current");
        File.WriteAllText(tmpPath, "updated");
        File.WriteAllText(bakPath, "stale-backup-from-previous-crash");

        // The pattern cleans stale backup first
        try { if (File.Exists(bakPath)) File.Delete(bakPath); } catch { }
        File.Replace(tmpPath, finalPath, bakPath, ignoreMetadataErrors: true);
        try { if (File.Exists(bakPath)) File.Delete(bakPath); } catch { }

        Assert.Equal("updated", File.ReadAllText(finalPath));
        Assert.False(File.Exists(bakPath));
    }
}
