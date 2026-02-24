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
    private readonly string _gitExe;

    public GitRepoService()
    {
        _gitExe = GitBinaryLocator.ResolveGitExecutablePath();
    }

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

        return SplitLines(r.StdOut)
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

    /// <summary>
    /// Ensures a local credential helper is configured so HTTPS push can open the
    /// browser/device login flow via bundled Portable Git on Windows.
    /// </summary>
    public async Task<GitOpResult> EnsureCredentialHelperAsync(string repoDir, IProgress<string> progress, CancellationToken ct)
    {
        if (!OperatingSystem.IsWindows())
        {
            progress.Report("[git] credential helper bootstrap skipped (non-Windows)");
            return new GitOpResult(true);
        }

        try
        {
            // If already set locally, keep it.
            var existing = await RunGitAsync(repoDir, "config --local --get credential.helper", null, ct);
            var helper = (existing.StdOut ?? "").Trim();

            if (!string.IsNullOrWhiteSpace(helper))
            {
                progress.Report("[git] credential.helper (local) = " + helper);
                return new GitOpResult(true);
            }

            // Prefer manager-core, fallback to manager for older variants.
            progress.Report("[git] setting local credential.helper = manager-core");
            var r1 = await RunGitAsync(repoDir, "config --local credential.helper manager-core", progress, ct);
            if (r1.ExitCode == 0)
            {
                progress.Report("[git] credential.helper set to manager-core");
                return new GitOpResult(true);
            }

            progress.Report("[git] manager-core failed, trying manager");
            var r2 = await RunGitAsync(repoDir, "config --local credential.helper manager", progress, ct);
            if (r2.ExitCode == 0)
            {
                progress.Report("[git] credential.helper set to manager");
                return new GitOpResult(true);
            }

            return new GitOpResult(false, (r1.AllText + "\n" + r2.AllText).Trim());
        }
        catch (Exception ex)
        {
            return new GitOpResult(false, ex.ToString());
        }
    }

    /// <summary>
    /// Repo-local line ending config to avoid scary CRLF warnings and churn.
    /// </summary>
    public async Task<GitOpResult> EnsureLineEndingConfigAsync(string repoDir, IProgress<string> progress, CancellationToken ct)
    {
        try
        {
            var r1 = await RunGitAsync(repoDir, "config --local core.autocrlf false", progress, ct);
            if (r1.ExitCode != 0) return new GitOpResult(false, r1.AllText);

            var r2 = await RunGitAsync(repoDir, "config --local core.eol lf", progress, ct);
            if (r2.ExitCode != 0) return new GitOpResult(false, r2.AllText);

            progress.Report("[git] line endings configured (local): autocrlf=false, eol=lf");
            return new GitOpResult(true);
        }
        catch (Exception ex)
        {
            return new GitOpResult(false, ex.ToString());
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
    // Update safety helpers (new)
    // -------------------------

    /// <summary>
    /// Returns repo-relative paths to preserve during "update but keep my local edits".
    /// Includes modified, added, staged, untracked, renamed/copied targets.
    /// Excludes deletes and anything under .git/.
    /// Optionally filter paths via prefixes (e.g. xml-p5t/).
    /// </summary>
    public async Task<string[]> GetChangedPathsForBackupAsync(string repoDir, string[]? includePrefixes, CancellationToken ct)
    {
        var porcelain = await GetStatusPorcelainAsync(repoDir, ct);
        if (porcelain.Length == 0) return Array.Empty<string>();

        var prefixes = (includePrefixes ?? Array.Empty<string>())
            .Select(NormalizeRelPath)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .ToArray();

        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var line in porcelain)
        {
            foreach (var path in ParsePorcelainPaths(line))
            {
                var rel = NormalizeRelPath(path);
                if (string.IsNullOrWhiteSpace(rel)) continue;
                if (rel.StartsWith(".git/", StringComparison.OrdinalIgnoreCase)) continue;

                if (prefixes.Length > 0 && !prefixes.Any(p => rel.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
                    continue;

                if (IsDeletedStatus(line))
                    continue;

                result.Add(rel);
            }
        }

        return result.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    /// <summary>
    /// Returns ahead/behind counts for e.g. origin/main...HEAD using:
    /// git rev-list --left-right --count origin/main...HEAD
    /// left=behind, right=ahead
    /// </summary>
    public async Task<(int behind, int ahead)> GetAheadBehindAsync(string repoDir, string upstreamRef, CancellationToken ct)
    {
        var r = await RunGitAsync(repoDir, $"rev-list --left-right --count \"{upstreamRef}...HEAD\"", null, ct);
        if (r.ExitCode != 0)
            return (0, 0);

        var txt = (r.StdOut ?? "").Trim();
        if (string.IsNullOrWhiteSpace(txt))
            return (0, 0);

        // Usually: "12\t3" or "12 3"
        var parts = txt.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
            return (0, 0);

        if (!int.TryParse(parts[0], out int behind)) behind = 0;
        if (!int.TryParse(parts[1], out int ahead)) ahead = 0;
        return (behind, ahead);
    }

    /// <summary>
    /// Creates a lightweight rescue branch at current HEAD (used before destructive reset).
    /// If branch already exists, returns success.
    /// </summary>
    public async Task<GitOpResult> CreateBranchAtHeadAsync(string repoDir, string branchName, IProgress<string> progress, CancellationToken ct)
    {
        var check = await RunGitAsync(repoDir, $"show-ref --verify --quiet \"refs/heads/{branchName}\"", null, ct);
        if (check.ExitCode == 0)
        {
            progress.Report("[git] rescue branch already exists: " + branchName);
            return new GitOpResult(true);
        }

        var r = await RunGitAsync(repoDir, $"branch \"{branchName}\" HEAD", progress, ct);
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

    private static IEnumerable<string> SplitLines(string? s)
    {
        return (s ?? "")
            .Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
    }

    private static string NormalizeRelPath(string p)
    {
        return (p ?? "")
            .Replace('\\', '/')
            .Trim()
            .TrimStart('/');
    }

    private static bool IsDeletedStatus(string porcelainLine)
    {
        if (string.IsNullOrWhiteSpace(porcelainLine) || porcelainLine.Length < 2)
            return false;

        char x = porcelainLine[0];
        char y = porcelainLine[1];

        // Delete if either side marks deletion. We do not "restore deletions" in the backup flow.
        return x == 'D' || y == 'D';
    }

    private static IEnumerable<string> ParsePorcelainPaths(string line)
    {
        // Porcelain (v1) examples:
        // " M path/file.xml"
        // "A  path/file.xml"
        // "?? path/file.xml"
        // "R  old/path.xml -> new/path.xml"
        // "RM old -> new"
        // We preserve the destination path for renames/copies.
        if (string.IsNullOrWhiteSpace(line))
            yield break;

        string payload = line.Length > 3 ? line.Substring(3) : "";

        if (string.IsNullOrWhiteSpace(payload))
            yield break;

        // Rename/copy style "old -> new"
        int arrow = payload.IndexOf(" -> ", StringComparison.Ordinal);
        if (arrow >= 0)
        {
            var oldPath = payload.Substring(0, arrow).Trim();
            var newPath = payload.Substring(arrow + 4).Trim();

            // Destination matters most for restore.
            if (!string.IsNullOrWhiteSpace(newPath))
                yield return newPath;

            // Optionally also keep old path if it still exists physically (rare useful case)
            // Caller will copy only existing files anyway.
            if (!string.IsNullOrWhiteSpace(oldPath))
                yield return oldPath;

            yield break;
        }

        yield return payload.Trim();
    }

    private sealed record RunResult(int ExitCode, string StdOut, string StdErr)
    {
        public string AllText => (StdOut + "\n" + StdErr).Trim();
    }

    private async Task<RunResult> RunGitAsync(string? repoDir, string args, IProgress<string>? progress, CancellationToken ct)
    {
        var psi = new ProcessStartInfo
        {
            FileName = _gitExe,
            Arguments = args,
            WorkingDirectory = string.IsNullOrWhiteSpace(repoDir) ? Environment.CurrentDirectory : repoDir,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };

        // Make bundled Portable Git fully usable (helpers + DLLs on PATH).
        GitBinaryLocator.EnrichProcessStartInfoForBundledGit(psi);

        // Fail fast instead of hanging on an invisible prompt in a headless process.
        // - GIT_TERMINAL_PROMPT=0 -> git errors instead of waiting for username/password
        // - GCM_INTERACTIVE=Always -> nudge Git Credential Manager to show UI when available
        try
        {
            psi.Environment["GIT_TERMINAL_PROMPT"] = "0";
            psi.Environment["GCM_INTERACTIVE"] = "Always";
        }
        catch
        {
            // ignore env failures; still try to run git
        }

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