using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CbetaTranslator.App.Services;

public sealed class GitRepoService : IGitRepoService
{
    private Process? _running;

    public void TryCancelRunningProcess()
    {
        try
        {
            if (_running != null && !_running.HasExited)
                _running.Kill(entireProcessTree: true);
        }
        catch { }
        finally
        {
            _running = null;
        }
    }

    public async Task<bool> CheckGitAvailableAsync(CancellationToken ct)
    {
        var r = await RunGitAsync(null, "--version", null, ct);
        return r.ExitCode == 0;
    }

    public async Task<GitOpResult> CloneAsync(string repoUrl, string targetDir, IProgress<string> progress, CancellationToken ct)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(targetDir) ?? targetDir);
        var r = await RunGitAsync(null, $"clone --progress \"{repoUrl}\" \"{targetDir}\"", progress, ct);
        return r.ExitCode == 0 ? new GitOpResult(true) : new GitOpResult(false, r.AllText);
    }

    public async Task<GitOpResult> FetchAsync(string repoDir, IProgress<string> progress, CancellationToken ct)
    {
        var r = await RunGitAsync(repoDir, "fetch --all --prune", progress, ct);
        return r.ExitCode == 0 ? new GitOpResult(true) : new GitOpResult(false, r.AllText);
    }

    public async Task<string[]> GetStatusPorcelainAsync(string repoDir, CancellationToken ct)
    {
        var r = await RunGitAsync(repoDir, "status --porcelain", null, ct);
        if (r.ExitCode != 0) return Array.Empty<string>();

        return (r.StdOut ?? "")
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.TrimEnd())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToArray();
    }

    public async Task<string> GetCurrentBranchAsync(string repoDir, CancellationToken ct)
    {
        var r = await RunGitAsync(repoDir, "rev-parse --abbrev-ref HEAD", null, ct);
        var s = (r.StdOut ?? "").Trim();
        return (r.ExitCode != 0 || string.IsNullOrWhiteSpace(s)) ? "HEAD" : s;
    }

    public async Task EnsureUserIdentityAsync(string repoDir, IProgress<string> progress, CancellationToken ct)
    {
        string name = await GetConfigAsync(repoDir, "user.name", ct);
        string email = await GetConfigAsync(repoDir, "user.email", ct);

        if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(email))
        {
            progress.Report("[git] user identity ok");
            return;
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            progress.Report("[git] setting local user.name = CbetaTranslator");
            await RunGitAsync(repoDir, "config user.name \"CbetaTranslator\"", progress, ct);
        }

        if (string.IsNullOrWhiteSpace(email))
        {
            progress.Report("[git] setting local user.email = cbeta-translator@local");
            await RunGitAsync(repoDir, "config user.email \"cbeta-translator@local\"", progress, ct);
        }
    }

    public async Task<GitOpResult> StagePathAsync(string repoDir, string relPath, IProgress<string> progress, CancellationToken ct)
    {
        var r = await RunGitAsync(repoDir, $"add -- \"{relPath}\"", progress, ct);
        return r.ExitCode == 0 ? new GitOpResult(true) : new GitOpResult(false, r.AllText);
    }

    public async Task<GitOpResult> StashKeepIndexAsync(string repoDir, string message, IProgress<string> progress, CancellationToken ct)
    {
        var r = await RunGitAsync(repoDir, $"stash push -u -k -m \"{message}\"", progress, ct);
        return r.ExitCode == 0 ? new GitOpResult(true) : new GitOpResult(false, r.AllText);
    }

    public async Task<GitOpResult> StashAllAsync(string repoDir, string message, IProgress<string> progress, CancellationToken ct)
    {
        // Stash EVERYTHING (including staged), include untracked.
        var r = await RunGitAsync(repoDir, $"stash push -u -m \"{message}\"", progress, ct);
        return r.ExitCode == 0 ? new GitOpResult(true) : new GitOpResult(false, r.AllText);
    }

    public async Task<GitOpResult> SwitchCreateBranchAsync(string repoDir, string branchName, IProgress<string> progress, CancellationToken ct)
    {
        var r = await RunGitAsync(repoDir, $"switch -c \"{branchName}\"", progress, ct);
        if (r.ExitCode == 0) return new GitOpResult(true);

        var r2 = await RunGitAsync(repoDir, $"checkout -b \"{branchName}\"", progress, ct);
        return r2.ExitCode == 0 ? new GitOpResult(true) : new GitOpResult(false, r2.AllText);
    }

    public async Task<GitOpResult> CommitAsync(string repoDir, string message, IProgress<string> progress, CancellationToken ct)
    {
        var r = await RunGitAsync(repoDir, $"commit -m \"{EscapeCommitMessage(message)}\"", progress, ct);
        return r.ExitCode == 0 ? new GitOpResult(true) : new GitOpResult(false, r.AllText);
    }

    public async Task<GitOpResult> SwitchBranchAsync(string repoDir, string branchName, IProgress<string> progress, CancellationToken ct)
    {
        var r = await RunGitAsync(repoDir, $"switch \"{branchName}\"", progress, ct);
        if (r.ExitCode == 0) return new GitOpResult(true);

        var r2 = await RunGitAsync(repoDir, $"checkout \"{branchName}\"", progress, ct);
        return r2.ExitCode == 0 ? new GitOpResult(true) : new GitOpResult(false, r2.AllText);
    }

    public async Task<GitOpResult> StashPopAsync(string repoDir, IProgress<string> progress, CancellationToken ct)
    {
        var r = await RunGitAsync(repoDir, "stash pop", progress, ct);
        return r.ExitCode == 0 ? new GitOpResult(true) : new GitOpResult(false, r.AllText);
    }

    public async Task<GitOpResult> HardResetToRemoteMainAsync(string repoDir, string remoteName, string branchName, IProgress<string> progress, CancellationToken ct)
    {
        // Example: reset --hard origin/main
        var r = await RunGitAsync(repoDir, $"reset --hard \"{remoteName}/{branchName}\"", progress, ct);
        return r.ExitCode == 0 ? new GitOpResult(true) : new GitOpResult(false, r.AllText);
    }

    public async Task<GitOpResult> CleanUntrackedAsync(string repoDir, IProgress<string> progress, CancellationToken ct)
    {
        // Remove untracked files/dirs to guarantee a clean tree after reset
        var r = await RunGitAsync(repoDir, "clean -fd", progress, ct);
        return r.ExitCode == 0 ? new GitOpResult(true) : new GitOpResult(false, r.AllText);
    }

    // -------------------------
    // Remotes / push / local exclude
    // -------------------------

    public async Task<string?> GetRemoteUrlAsync(string repoDir, string remoteName, CancellationToken ct)
    {
        var r = await RunGitAsync(repoDir, $"remote get-url \"{remoteName}\"", null, ct);
        if (r.ExitCode != 0) return null;
        var s = (r.StdOut ?? "").Trim();
        return string.IsNullOrWhiteSpace(s) ? null : s;
    }

    public async Task<GitOpResult> RemoveRemoteAsync(string repoDir, string remoteName, IProgress<string> progress, CancellationToken ct)
    {
        var r = await RunGitAsync(repoDir, $"remote remove \"{remoteName}\"", progress, ct);
        return r.ExitCode == 0 ? new GitOpResult(true) : new GitOpResult(false, r.AllText);
    }

    public async Task<GitOpResult> EnsureRemoteUrlAsync(string repoDir, string remoteName, string cleanRemoteUrl, IProgress<string> progress, CancellationToken ct)
    {
        var check = await RunGitAsync(repoDir, $"remote get-url \"{remoteName}\"", null, ct);
        if (check.ExitCode == 0)
        {
            var rSet = await RunGitAsync(repoDir, $"remote set-url \"{remoteName}\" \"{cleanRemoteUrl}\"", progress, ct);
            return rSet.ExitCode == 0 ? new GitOpResult(true) : new GitOpResult(false, rSet.AllText);
        }

        var rAdd = await RunGitAsync(repoDir, $"remote add \"{remoteName}\" \"{cleanRemoteUrl}\"", progress, ct);
        return rAdd.ExitCode == 0 ? new GitOpResult(true) : new GitOpResult(false, rAdd.AllText);
    }

    public async Task<GitOpResult> PushSetUpstreamAsync(string repoDir, string remoteName, string branchName, IProgress<string> progress, CancellationToken ct)
    {
        var r = await RunGitAsync(repoDir, $"push -u \"{remoteName}\" \"{branchName}\"", progress, ct);
        return r.ExitCode == 0 ? new GitOpResult(true) : new GitOpResult(false, r.AllText);
    }

    public async Task<GitOpResult> EnsureLocalExcludeAsync(string repoDir, string[] patterns, IProgress<string> progress, CancellationToken ct)
    {
        try
        {
            var infoDir = Path.Combine(repoDir, ".git", "info");
            Directory.CreateDirectory(infoDir);

            var excludePath = Path.Combine(infoDir, "exclude");
            var existing = File.Exists(excludePath)
                ? (await File.ReadAllLinesAsync(excludePath, ct)).ToList()
                : new List<string>();

            bool changed = false;

            foreach (var pat in patterns ?? Array.Empty<string>())
            {
                var p = (pat ?? "").Trim();
                if (string.IsNullOrWhiteSpace(p)) continue;

                if (!existing.Any(l => string.Equals(l.Trim(), p, StringComparison.OrdinalIgnoreCase)))
                {
                    existing.Add(p);
                    changed = true;
                }
            }

            if (changed)
            {
                await File.WriteAllLinesAsync(excludePath, existing, Encoding.UTF8, ct);
                progress.Report("[git] updated .git/info/exclude (local)");
            }
            else
            {
                progress.Report("[git] .git/info/exclude ok (local)");
            }

            return new GitOpResult(true);
        }
        catch (Exception ex)
        {
            return new GitOpResult(false, ex.ToString());
        }
    }

    // -------------------------
    // Helpers
    // -------------------------

    private static string EscapeCommitMessage(string s)
    {
        s ??= "";
        s = s.Replace("\r", " ").Replace("\n", " ");
        s = s.Replace("\"", "'");
        return s.Trim();
    }

    private async Task<string> GetConfigAsync(string repoDir, string key, CancellationToken ct)
    {
        var r = await RunGitAsync(repoDir, $"config --get {key}", null, ct);
        if (r.ExitCode != 0) return "";
        return (r.StdOut ?? "").Trim();
    }

    private sealed record RunResult(int ExitCode, string StdOut, string StdErr)
    {
        public string AllText => (StdOut + "\n" + StdErr).Trim();
    }

    private async Task<RunResult> RunGitAsync(string? repoDir, string args, IProgress<string>? progress, CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = "git",
            Arguments = args,
            WorkingDirectory = string.IsNullOrWhiteSpace(repoDir) ? Environment.CurrentDirectory : repoDir,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };

        var p = new Process { StartInfo = psi, EnableRaisingEvents = true };
        _running = p;

        var stdout = new List<string>();
        var stderr = new List<string>();

        void OnOut(object? sender, DataReceivedEventArgs e)
        {
            if (e.Data == null) return;
            lock (stdout) stdout.Add(e.Data);
            progress?.Report(e.Data);
        }

        void OnErr(object? sender, DataReceivedEventArgs e)
        {
            if (e.Data == null) return;
            lock (stderr) stderr.Add(e.Data);
            progress?.Report(e.Data);
        }

        p.OutputDataReceived += OnOut;
        p.ErrorDataReceived += OnErr;

        try
        {
            if (!p.Start())
                return new RunResult(-1, "", "Failed to start git process.");

            p.BeginOutputReadLine();
            p.BeginErrorReadLine();

            await p.WaitForExitAsync(ct);

            string so, se;
            lock (stdout) so = string.Join("\n", stdout);
            lock (stderr) se = string.Join("\n", stderr);

            return new RunResult(p.ExitCode, so, se);
        }
        catch (OperationCanceledException)
        {
            try { if (!p.HasExited) p.Kill(entireProcessTree: true); } catch { }
            throw;
        }
        catch (Exception ex)
        {
            try { if (!p.HasExited) p.Kill(entireProcessTree: true); } catch { }
            return new RunResult(-1, "", ex.ToString());
        }
        finally
        {
            try
            {
                p.OutputDataReceived -= OnOut;
                p.ErrorDataReceived -= OnErr;
            }
            catch { }

            _running = null;
        }
    }
}
