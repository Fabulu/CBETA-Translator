using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

/// <summary>
/// Regression tests for audit item P1.4 (RUN-20260702-2259 R3-M4): git status /
/// ahead-behind failures used to be reported as "clean" / "(0,0) in sync" — right
/// upstream of HardResetToRemoteMainAsync and CleanUntrackedAsync, where a broken repo
/// indistinguishable from a clean tree meant resetting away unbacked-up local work.
/// Failures must surface as null so callers block destructive flows.
/// </summary>
public sealed class GitRepoServiceFailureSurfacingTests : IDisposable
{
    private readonly string _nonRepoDir;

    public GitRepoServiceFailureSurfacingTests()
    {
        _nonRepoDir = Path.Combine(Path.GetTempPath(), "readzen-nonrepo-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_nonRepoDir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_nonRepoDir, recursive: true); } catch { }
    }

    private static async Task<GitRepoService?> GetServiceIfGitAvailableAsync()
    {
        var svc = new GitRepoService();
        // Environments without any git binary can't exercise these paths; the rest of
        // the suite (and the app's own flows) already require git, so soft-skip.
        return await svc.CheckGitAvailableAsync(CancellationToken.None) ? svc : null;
    }

    [Fact]
    public async Task GetStatusPorcelain_OutsideARepo_ReturnsNull_NotClean()
    {
        var svc = await GetServiceIfGitAvailableAsync();
        if (svc == null) return;

        var status = await svc.GetStatusPorcelainAsync(_nonRepoDir, CancellationToken.None);

        // Before the fix this returned Array.Empty — indistinguishable from a clean
        // tree, which the update flow then hard-reset without backing anything up.
        Assert.Null(status);
    }

    [Fact]
    public async Task GetAheadBehind_OutsideARepo_ReturnsNull_NotInSync()
    {
        var svc = await GetServiceIfGitAvailableAsync();
        if (svc == null) return;

        var ab = await svc.GetAheadBehindAsync(_nonRepoDir, "origin/main", CancellationToken.None);

        // Before the fix this returned (0,0) — "in sync" — which skipped the
        // rescue-branch safety net before the destructive update.
        Assert.Null(ab);
    }

    [Fact]
    public async Task GetChangedPathsForBackup_OutsideARepo_ReturnsNull_NotEmpty()
    {
        var svc = await GetServiceIfGitAvailableAsync();
        if (svc == null) return;

        var paths = await svc.GetChangedPathsForBackupAsync(_nonRepoDir, includePrefixes: null, CancellationToken.None);

        // null must propagate so DoUpdateKeepLocalAsync aborts BEFORE reset --hard
        // instead of proceeding with an empty backup list.
        Assert.Null(paths);
    }

    [Fact]
    public async Task GetStatusPorcelain_InAValidRepo_StillReturnsLines()
    {
        var svc = await GetServiceIfGitAvailableAsync();
        if (svc == null) return;

        var repoDir = Path.Combine(_nonRepoDir, "repo");
        Directory.CreateDirectory(repoDir);
        var progress = new Progress<string>(_ => { });
        // GitRepoService has no InitAsync; shell out via its own runner is private,
        // so init through a plain process-less route: use clone of nothing? Simplest
        // is to create the repo with git directly.
        var psi = new System.Diagnostics.ProcessStartInfo("git", "init")
        {
            WorkingDirectory = repoDir,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
        };
        using (var p = System.Diagnostics.Process.Start(psi))
        {
            await p!.WaitForExitAsync();
            if (p.ExitCode != 0) return; // environment can't init repos; soft-skip
        }
        await File.WriteAllTextAsync(Path.Combine(repoDir, "a.txt"), "hello");

        var status = await svc.GetStatusPorcelainAsync(repoDir, CancellationToken.None);

        Assert.NotNull(status);
        Assert.Contains(status!, l => l.Contains("a.txt", StringComparison.OrdinalIgnoreCase));
    }
}
