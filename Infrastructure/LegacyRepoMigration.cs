using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CbetaTranslator.App.Services;

namespace CbetaTranslator.App.Infrastructure;

public static class LegacyRepoMigration
{
    public sealed record MigrationResult(bool Success, string? NewParentRoot, string? Error);

    private const string TranslationRepoUrl = "https://github.com/Fabulu/CbetaZenTranslations.git";
    private const string MarkerFileName = ".migration-in-progress";

    private static readonly string[] DataFiles = {
        "termbase.json", "titles.jsonl",
        "translation-memory.approved.jsonl", "translation-review.jsonl",
        "zen_texts.json"
    };

    /// <summary>
    /// Detects old single-repo layout: a folder with .git/, xml-p5/, AND xml-p5t/ as direct children.
    /// </summary>
    public static bool IsLegacySingleRepoLayout(string path)
    {
        return Directory.Exists(path)
            && Directory.Exists(Path.Combine(path, ".git"))
            && Directory.Exists(Path.Combine(path, AppPaths.OriginalFolderName))
            && Directory.Exists(Path.Combine(path, AppPaths.TranslatedFolderName));
    }

    /// <summary>
    /// Checks if a migration was started but not completed.
    /// </summary>
    public static bool HasPendingMigration(string parentDir)
    {
        return File.Exists(Path.Combine(parentDir, MarkerFileName));
    }

    /// <summary>
    /// Main migration method. Moves user data from a legacy single-repo layout
    /// into a new two-repo layout (originals + translations as sibling repos).
    /// </summary>
    public static async Task<MigrationResult> MigrateAsync(
        string legacyRepoPath,
        IGitRepoService git,
        IProgress<string> progress,
        CancellationToken ct)
    {
        var parentDir = Path.GetDirectoryName(legacyRepoPath);
        if (parentDir == null) return new(false, null, "Cannot determine parent directory.");

        var translationDir = Path.Combine(parentDir, AppPaths.DefaultTranslationRepoFolderName);
        var markerPath = Path.Combine(parentDir, MarkerFileName);

        try
        {
            // Step 1: Write marker
            await File.WriteAllTextAsync(markerPath, "migration-started", ct);

            // Step 2: Clone CbetaZenTranslations if not already present
            if (!Directory.Exists(Path.Combine(translationDir, ".git")))
            {
                progress.Report("Cloning translations repository...");
                if (Directory.Exists(translationDir))
                    Directory.Delete(translationDir, true); // Clean partial clone
                var result = await git.CloneAsync(TranslationRepoUrl, translationDir, progress, ct);
                if (!result.Success)
                    return new(false, null, "Failed to clone translations repo: " + result.Error);
            }

            // Step 3: Copy user data files from legacy repo to translations repo
            progress.Report("Copying user data...");
            foreach (var file in DataFiles)
            {
                var src = Path.Combine(legacyRepoPath, file);
                var dst = Path.Combine(translationDir, file);
                if (File.Exists(src))
                    File.Copy(src, dst, overwrite: true);
            }

            // Step 4: Copy community directory
            var srcCommunity = Path.Combine(legacyRepoPath, "community");
            var dstCommunity = Path.Combine(translationDir, "community");
            if (Directory.Exists(srcCommunity))
                CopyDirectoryRecursive(srcCommunity, dstCommunity);

            // Step 5: Copy meaningful translations (files that differ from originals)
            progress.Report("Identifying local translations...");
            CopyMeaningfulTranslations(legacyRepoPath, translationDir);

            // Step 6: Rename legacy folders so DiscoverRepoPaths sees old repo as originals-only
            progress.Report("Cleaning up old layout...");
            RenameLegacyFolder(Path.Combine(legacyRepoPath, "xml-p5t"), "xml-p5t-legacy-backup");
            RenameLegacyFolder(Path.Combine(legacyRepoPath, "community"), "community-legacy-backup");
            // Also rename loose data files
            foreach (var file in DataFiles)
            {
                var src = Path.Combine(legacyRepoPath, file);
                if (File.Exists(src))
                {
                    try { File.Move(src, src + ".legacy-backup", overwrite: true); }
                    catch { /* non-critical */ }
                }
            }

            // Step 7: Delete marker
            try { File.Delete(markerPath); } catch { }

            progress.Report("Migration complete!");
            return new(true, parentDir, null);
        }
        catch (Exception ex)
        {
            return new(false, null, ex.Message);
        }
    }

    private static void CopyMeaningfulTranslations(string legacyRepoPath, string translationDir)
    {
        var origDir = Path.Combine(legacyRepoPath, AppPaths.OriginalFolderName);
        var tranDir = Path.Combine(legacyRepoPath, AppPaths.TranslatedFolderName);

        if (!Directory.Exists(tranDir)) return;

        foreach (var tranFile in Directory.EnumerateFiles(tranDir, "*.xml", SearchOption.AllDirectories))
        {
            var relPath = Path.GetRelativePath(tranDir, tranFile);
            var origFile = Path.Combine(origDir, relPath);

            if (!File.Exists(origFile) || !FilesAreIdentical(origFile, tranFile))
            {
                var dstFile = Path.Combine(translationDir, AppPaths.TranslatedFolderName, relPath);
                var dstDir = Path.GetDirectoryName(dstFile);
                if (dstDir != null) Directory.CreateDirectory(dstDir);
                File.Copy(tranFile, dstFile, overwrite: true);
            }
        }
    }

    private static bool FilesAreIdentical(string path1, string path2)
    {
        var info1 = new FileInfo(path1);
        var info2 = new FileInfo(path2);
        if (info1.Length != info2.Length) return false;

        // Quick byte comparison
        using var s1 = File.OpenRead(path1);
        using var s2 = File.OpenRead(path2);
        var buf1 = new byte[4096];
        var buf2 = new byte[4096];
        int read;
        while ((read = s1.Read(buf1, 0, buf1.Length)) > 0)
        {
            s2.ReadExactly(buf2, 0, read);
            if (!buf1.AsSpan(0, read).SequenceEqual(buf2.AsSpan(0, read)))
                return false;
        }
        return true;
    }

    private static void RenameLegacyFolder(string path, string backupSuffix)
    {
        if (!Directory.Exists(path)) return;
        var parent = Path.GetDirectoryName(path)!;
        var backupPath = Path.Combine(parent, backupSuffix);
        if (Directory.Exists(backupPath))
            Directory.Delete(backupPath, true); // Remove old backup if exists
        Directory.Move(path, backupPath);
    }

    private static void CopyDirectoryRecursive(string sourceDir, string targetDir)
    {
        Directory.CreateDirectory(targetDir);
        foreach (var file in Directory.GetFiles(sourceDir))
        {
            var dst = Path.Combine(targetDir, Path.GetFileName(file));
            File.Copy(file, dst, overwrite: true);
        }
        foreach (var dir in Directory.GetDirectories(sourceDir))
        {
            CopyDirectoryRecursive(dir, Path.Combine(targetDir, Path.GetFileName(dir)));
        }
    }
}
