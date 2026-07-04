using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

/// <summary>
/// Regression tests for audit item P1.7 (RUN-20260702-2259 R3-M2): git command lines
/// were built by string interpolation inside escaped quotes, so branch names, paths,
/// and messages containing quotes or trailing backslashes could break Windows argument
/// parsing or smuggle extra git options. Arguments now go through
/// ProcessStartInfo.ArgumentList (per-argument, correctly quoted).
/// </summary>
public sealed class GitArgumentSafetyTests : IDisposable
{
    private readonly string _dir;

    public GitArgumentSafetyTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "readzen-gitargs-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
    }

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch { }
    }

    private static async Task<bool> GitAvailableAsync()
        => await new GitRepoService().CheckGitAvailableAsync(CancellationToken.None);

    private async Task<string?> InitRepoAsync()
    {
        var repoDir = Path.Combine(_dir, "repo");
        Directory.CreateDirectory(repoDir);
        var psi = new ProcessStartInfo("git", "init") { WorkingDirectory = repoDir, UseShellExecute = false, CreateNoWindow = true, RedirectStandardOutput = true, RedirectStandardError = true };
        using var p = Process.Start(psi);
        await p!.WaitForExitAsync();
        return p.ExitCode == 0 ? repoDir : null;
    }

    [Fact]
    public async Task Commit_MessageWithQuotesAndBackslash_IsPreservedVerbatim()
    {
        if (!await GitAvailableAsync()) return;
        var repoDir = await InitRepoAsync();
        if (repoDir == null) return;

        var svc = new GitRepoService();
        var prog = new Progress<string>(_ => { });
        var ct = CancellationToken.None;

        await svc.EnsureUserIdentityAsync(repoDir, "tester", prog, ct);
        await File.WriteAllTextAsync(Path.Combine(repoDir, "a.txt"), "x");
        var stage = await svc.StagePathAsync(repoDir, "a.txt", prog, ct);
        Assert.True(stage.Success);

        // Quotes + trailing backslash: the old interpolated "..."-wrapping mangled
        // quotes (EscapeCommitMessage turned them into apostrophes) and a trailing
        // backslash could eat the closing quote on Windows.
        var message = "say \"hello\" to c:\\path\\";
        var commit = await svc.CommitAsync(repoDir, message, prog, ct);
        Assert.True(commit.Success, commit.Error);

        var log = await svc.GetFileLogAsync(repoDir, "a.txt", 5, ct);
        var entry = Assert.Single(log);
        Assert.Equal(message, entry.Subject);
    }

    [Fact]
    public async Task Branch_NameThatLooksLikeAnOption_CannotSmuggleArguments()
    {
        if (!await GitAvailableAsync()) return;
        var repoDir = await InitRepoAsync();
        if (repoDir == null) return;

        var svc = new GitRepoService();
        var prog = new Progress<string>(_ => { });
        var ct = CancellationToken.None;

        await svc.EnsureUserIdentityAsync(repoDir, "tester", prog, ct);
        await File.WriteAllTextAsync(Path.Combine(repoDir, "a.txt"), "x");
        await svc.StagePathAsync(repoDir, "a.txt", prog, ct);
        await svc.CommitAsync(repoDir, "init", prog, ct);

        // A branch name that parses as loose words + an option under the old
        // interpolation. With ArgumentList it reaches git as ONE argument, and git
        // rejects it as an invalid ref name instead of doing something else.
        var evil = "x --force y";
        var result = await svc.SwitchCreateBranchAsync(repoDir, evil, prog, ct);
        Assert.False(result.Success);
    }
}
