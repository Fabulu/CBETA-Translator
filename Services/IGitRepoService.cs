using System;
using System.Threading;
using System.Threading.Tasks;

namespace CbetaTranslator.App.Services;

public sealed record GitOpResult(bool Success, string? Error = null);

public interface IGitRepoService
{
    void TryCancelRunningProcess();

    Task<bool> CheckGitAvailableAsync(CancellationToken ct);

    Task<GitOpResult> CloneAsync(string repoUrl, string targetDir, IProgress<string> progress, CancellationToken ct);
    Task<GitOpResult> FetchAsync(string repoDir, IProgress<string> progress, CancellationToken ct);
    Task<GitOpResult> PullFfOnlyAsync(string repoDir, IProgress<string> progress, CancellationToken ct);

    /// <summary>
    /// Returns git status porcelain output (raw lines). Includes untracked.
    /// </summary>
    Task<string[]> GetStatusPorcelainAsync(string repoDir, CancellationToken ct);

    /// <summary>
    /// Get current branch name (or HEAD if detached).
    /// </summary>
    Task<string> GetCurrentBranchAsync(string repoDir, CancellationToken ct);

    /// <summary>
    /// Ensure local repo has user.name and user.email set (repo-local config).
    /// </summary>
    Task EnsureUserIdentityAsync(string repoDir, IProgress<string> progress, CancellationToken ct);

    Task<GitOpResult> StagePathAsync(string repoDir, string relPath, IProgress<string> progress, CancellationToken ct);

    /// <summary>
    /// Stash all changes EXCEPT staged/index (keep-index), include untracked (-u).
    /// If nothing to stash, returns Success=true and logs that fact.
    /// </summary>
    Task<GitOpResult> StashKeepIndexAsync(string repoDir, string message, IProgress<string> progress, CancellationToken ct);

    Task<GitOpResult> SwitchCreateBranchAsync(string repoDir, string branchName, IProgress<string> progress, CancellationToken ct);

    Task<GitOpResult> CommitAsync(string repoDir, string message, IProgress<string> progress, CancellationToken ct);

    Task<GitOpResult> SwitchBranchAsync(string repoDir, string branchName, IProgress<string> progress, CancellationToken ct);

    /// <summary>
    /// Pop latest stash. If conflicts happen, git will keep the stash; we return Success=false with error text.
    /// </summary>
    Task<GitOpResult> StashPopAsync(string repoDir, IProgress<string> progress, CancellationToken ct);
}
