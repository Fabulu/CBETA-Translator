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

    // Local app noise. We never want this to block update/commit UX.
    private static readonly string[] LocalNoise =
    {
        "index.cache.json",
        "search.index.manifest.json",
        "index.debug.log",
    };

    public void TryCancelRunningProcess()
    {
        try
        {
            if (_running != null && !_running.HasExited)
                _running.Kill(entireProcessTree: true);
        }
        catch { /* ignore */ }
        finally
        {
            _running = null;
        }
    }

    public async Task<bool> CheckGitAvailableAsync(CancellationToken ct)
    {
        var r = await RunGitAsync(
            repoDir: null,
            args: "--version",
            progress: null,
            ct: ct);

        return r.ExitCode == 0;
    }

    public async Task<GitOpResult> CloneAsync(string repoUrl, string targetDir, IProgress<string> progress, CancellationToken ct)
    {
        // clone into an empty folder path
        Directory.CreateDirectory(Path.GetDirectoryName(targetDir) ?? targetDir);

        var args = $"clone --progress \"{repoUrl}\" \"{targetDir}\"";
        var r = await RunGitAsync(repoDir: null, args: args, progress: progress, ct: ct);

        if (r.ExitCode != 0)
            return new GitOpResult(false, r.AllText);

        // After clone, apply local ignore/skip-worktree for app noise.
        await EnsureLocalNoiseSuppressedAsync(targetDir, progress, ct);

        return new GitOpResult(true);
    }

    public async Task<GitOpResult> FetchAsync(string repoDir, IProgress<string> progress, CancellationToken ct)
    {
        await EnsureLocalNoiseSuppressedAsync(repoDir, progress, ct);

        var r = await RunGitAsync(repoDir, "fetch --all --prune", progress, ct);
        return r.ExitCode == 0 ? new GitOpResult(true) : new GitOpResult(false, r.AllText);
    }

    public async Task<GitOpResult> PullFfOnlyAsync(string repoDir, IProgress<string> progress, CancellationToken ct)
    {
        await EnsureLocalNoiseSuppressedAsync(repoDir, progress, ct);

        // If dirty, auto-stash, pull, then restore.
        var status = await GetStatusPorcelainAsync(repoDir, ct);
        bool dirty = status.Length > 0;

        bool didStash = false;

        if (dirty)
        {
            progress.Report("[block] working tree is dirty");
            progress.Report("[info] auto-stashing to allow update");
            var stash = await RunGitAsync(repoDir, "stash push -u -m \"cbeta-update-stash\"", progress, ct);
            if (stash.ExitCode != 0)
                return new GitOpResult(false, stash.AllText);

            // If there were no changes, git prints "No local changes to save" and exit 0.
            didStash = !(stash.StdOut ?? "").Contains("No local changes to save", StringComparison.OrdinalIgnoreCase);
        }

        var pull = await RunGitAsync(repoDir, "pull --ff-only", progress, ct);
        if (pull.ExitCode != 0)
        {
            // Best effort restore if we created a stash
            if (didStash)
            {
                progress.Report("[restore] stash pop (after failed pull)");
                await RunGitAsync(repoDir, "stash pop", progress, CancellationToken.None);
            }

            return new GitOpResult(false, pull.AllText);
        }

        if (didStash)
        {
            progress.Report("[restore] stash pop");
            var pop = await RunGitAsync(repoDir, "stash pop", progress, ct);
            if (pop.ExitCode != 0)
            {
                // Conflicts: stash stays, user can resolve manually.
                return new GitOpResult(false, "Update succeeded, but stash restore had conflicts. Resolve conflicts and run: git stash pop");
            }
        }

        // Re-apply after pop (files may re-appear)
        await EnsureLocalNoiseSuppressedAsync(repoDir, progress, ct);

        return new GitOpResult(true);
    }

    public async Task<string[]> GetStatusPorcelainAsync(string repoDir, CancellationToken ct)
    {
        var r = await RunGitAsync(repoDir, "status --porcelain", progress: null, ct: ct);
        if (r.ExitCode != 0) return Array.Empty<string>();

        var lines = (r.StdOut ?? "")
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries)
            .Select(s => s.TrimEnd())
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .ToArray();

        return lines;
    }

    public async Task<string> GetCurrentBranchAsync(string repoDir, CancellationToken ct)
    {
        var r = await RunGitAsync(repoDir, "rev-parse --abbrev-ref HEAD", progress: null, ct: ct);
        var s = (r.StdOut ?? "").Trim();

        if (r.ExitCode != 0 || string.IsNullOrWhiteSpace(s))
            return "HEAD";

        return s;
    }

    public async Task EnsureUserIdentityAsync(string repoDir, IProgress<string> progress, CancellationToken ct)
    {
        await EnsureLocalNoiseSuppressedAsync(repoDir, progress, ct);

        string name = await GetConfigAsync(repoDir, "user.name", ct);
        string email = await GetConfigAsync(repoDir, "user.email", ct);

        if (!string.IsNullOrWhiteSpace(name) && !string.IsNullOrWhiteSpace(email))
        {
            progress.Report("[git] user identity ok");
            return;
        }

        // Repo-local defaults (safe + non-confusing)
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
        await EnsureLocalNoiseSuppressedAsync(repoDir, progress, ct);

        // Stage exactly that file path
        var r = await RunGitAsync(repoDir, $"add -- \"{relPath}\"", progress, ct);
        return r.ExitCode == 0 ? new GitOpResult(true) : new GitOpResult(false, r.AllText);
    }

    public async Task<GitOpResult> StashKeepIndexAsync(string repoDir, string message, IProgress<string> progress, CancellationToken ct)
    {
        await EnsureLocalNoiseSuppressedAsync(repoDir, progress, ct);

        // Note: If nothing to stash, git returns exit code 0 and prints "No local changes to save"
        var r = await RunGitAsync(repoDir, $"stash push -u -k -m \"{message}\"", progress, ct);
        return r.ExitCode == 0 ? new GitOpResult(true) : new GitOpResult(false, r.AllText);
    }

    public async Task<GitOpResult> SwitchCreateBranchAsync(string repoDir, string branchName, IProgress<string> progress, CancellationToken ct)
    {
        await EnsureLocalNoiseSuppressedAsync(repoDir, progress, ct);

        // Use switch if available; on older Git, checkout -b works too, but switch exists for years.
        var r = await RunGitAsync(repoDir, $"switch -c \"{branchName}\"", progress, ct);
        if (r.ExitCode == 0) return new GitOpResult(true);

        // fallback
        var r2 = await RunGitAsync(repoDir, $"checkout -b \"{branchName}\"", progress, ct);
        return r2.ExitCode == 0 ? new GitOpResult(true) : new GitOpResult(false, r2.AllText);
    }

    public async Task<GitOpResult> CommitAsync(string repoDir, string message, IProgress<string> progress, CancellationToken ct)
    {
        await EnsureLocalNoiseSuppressedAsync(repoDir, progress, ct);

        // Commit staged changes only.
        var r = await RunGitAsync(repoDir, $"commit -m \"{EscapeCommitMessage(message)}\"", progress, ct);
        return r.ExitCode == 0 ? new GitOpResult(true) : new GitOpResult(false, r.AllText);
    }

    public async Task<GitOpResult> SwitchBranchAsync(string repoDir, string branchName, IProgress<string> progress, CancellationToken ct)
    {
        await EnsureLocalNoiseSuppressedAsync(repoDir, progress, ct);

        var r = await RunGitAsync(repoDir, $"switch \"{branchName}\"", progress, ct);
        if (r.ExitCode == 0) return new GitOpResult(true);

        // fallback
        var r2 = await RunGitAsync(repoDir, $"checkout \"{branchName}\"", progress, ct);
        return r2.ExitCode == 0 ? new GitOpResult(true) : new GitOpResult(false, r2.AllText);
    }

    public async Task<GitOpResult> StashPopAsync(string repoDir, IProgress<string> progress, CancellationToken ct)
    {
        await EnsureLocalNoiseSuppressedAsync(repoDir, progress, ct);

        var r = await RunGitAsync(repoDir, "stash pop", progress, ct);
        return r.ExitCode == 0 ? new GitOpResult(true) : new GitOpResult(false, r.AllText);
    }

    // -------------------------
    // Local ignore helpers
    // -------------------------

    private async Task EnsureLocalNoiseSuppressedAsync(string repoDir, IProgress<string>? progress, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(repoDir))
            return;

        var gitDir = Path.Combine(repoDir, ".git");
        if (!Directory.Exists(gitDir))
            return;

        // 1) Add to .git/info/exclude so untracked noise doesn't show
        try
        {
            var infoDir = Path.Combine(gitDir, "info");
            Directory.CreateDirectory(infoDir);

            var excludePath = Path.Combine(infoDir, "exclude");
            var existing = File.Exists(excludePath) ? await File.ReadAllTextAsync(excludePath, ct) : "";

            var toAdd = new List<string>();
            foreach (var p in LocalNoise)
            {
                var pat = p.Replace('\\', '/');
                if (!existing.Contains(pat, StringComparison.Ordinal))
                    toAdd.Add(pat);
            }

            if (toAdd.Count > 0)
            {
                var sb = new StringBuilder();
                sb.AppendLine();
                sb.AppendLine("# CbetaTranslator local excludes");
                foreach (var a in toAdd) sb.AppendLine(a);

                await File.AppendAllTextAsync(excludePath, sb.ToString(), ct);
                progress?.Report("[git] updated .git/info/exclude (local)");
            }
        }
        catch
        {
            // ignore, this is best-effort
        }

        // 2) If any noise file is tracked, mark skip-worktree locally so it won't show dirty.
        foreach (var p in LocalNoise)
        {
            if (ct.IsCancellationRequested) return;

            var rel = p.Replace('\\', '/');
            var tracked = await IsTrackedAsync(repoDir, rel, ct);
            if (!tracked) continue;

            // local only
            await RunGitAsync(repoDir, $"update-index --skip-worktree -- \"{rel}\"", progress, ct);
        }
    }

    private async Task<bool> IsTrackedAsync(string repoDir, string relPath, CancellationToken ct)
    {
        var r = await RunGitAsync(repoDir, $"ls-files --error-unmatch -- \"{relPath}\"", progress: null, ct: ct);
        return r.ExitCode == 0;
    }

    // -------------------------
    // Helpers
    // -------------------------

    private static string EscapeCommitMessage(string s)
    {
        s ??= "";
        s = s.Replace("\r", " ").Replace("\n", " ");
        s = s.Replace("\"", "'"); // simplest safe quoting
        return s.Trim();
    }

    private async Task<string> GetConfigAsync(string repoDir, string key, CancellationToken ct)
    {
        var r = await RunGitAsync(repoDir, $"config --get {key}", progress: null, ct: ct);
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

        void OnOut(object sender, DataReceivedEventArgs e)
        {
            if (e.Data == null) return;
            lock (stdout) stdout.Add(e.Data);
            progress?.Report(e.Data);
        }

        void OnErr(object sender, DataReceivedEventArgs e)
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
