using System;
using System.Threading;
using System.Threading.Tasks;

namespace ReadZen.App.Services;

public sealed record GitOpResult(bool Success, string? Error = null);

public interface IGitRepoService
{
    void TryCancelRunningProcess();

    Task<bool> CheckGitAvailableAsync(CancellationToken ct);

    Task<GitOpResult> CloneAsync(string repoUrl, string targetDir, IProgress<string> progress, CancellationToken ct);
    Task<GitOpResult> FetchAsync(string repoDir, IProgress<string> progress, CancellationToken ct);

    /// <summary>
    /// Returns git status porcelain output (raw lines). Includes untracked.
    /// </summary>
    Task<string[]> GetStatusPorcelainAsync(string repoDir, CancellationToken ct);

    Task<string> GetCurrentBranchAsync(string repoDir, CancellationToken ct);

    /// <summary>
    /// Ensures git user.name and user.email are configured locally.
    /// If <paramref name="username"/> is provided, it is used for user.name and email;
    /// otherwise falls back to "ReadZen" / "readzen@readzen.local".
    /// </summary>
    /// <remarks>
    /// Security: username is self-declared (from AppConfig), not verified.
    /// Git commits can be attributed to any name — this is inherent to git.
    /// GitHub PRs are verified via OAuth token (GitHubAccessToken).
    /// Community data files include CreatedBy field — also self-declared.
    /// Mitigation: GitHub PR author is verified; community data merges should be reviewed.
    /// </remarks>
    Task EnsureUserIdentityAsync(string repoDir, string? username, IProgress<string> progress, CancellationToken ct);

    Task<GitOpResult> StagePathAsync(string repoDir, string relPath, IProgress<string> progress, CancellationToken ct);

    /// <summary>
    /// Stash all changes EXCEPT staged/index (keep-index), include untracked (-u).
    /// </summary>
    Task<GitOpResult> StashKeepIndexAsync(string repoDir, string message, IProgress<string> progress, CancellationToken ct);

    /// <summary>
    /// Stash EVERYTHING (including staged), include untracked (-u).
    /// Used for "Update no matter what".
    /// </summary>
    Task<GitOpResult> StashAllAsync(string repoDir, string message, IProgress<string> progress, CancellationToken ct);

    Task<GitOpResult> SwitchCreateBranchAsync(string repoDir, string branchName, IProgress<string> progress, CancellationToken ct);
    Task<GitOpResult> CommitAsync(string repoDir, string message, IProgress<string> progress, CancellationToken ct);
    Task<GitOpResult> SwitchBranchAsync(string repoDir, string branchName, IProgress<string> progress, CancellationToken ct);

    Task<GitOpResult> StashPopAsync(string repoDir, IProgress<string> progress, CancellationToken ct);

    // Force update helpers
    Task<GitOpResult> HardResetToRemoteMainAsync(string repoDir, string remoteName, string branchName, IProgress<string> progress, CancellationToken ct);
    Task<GitOpResult> CleanUntrackedAsync(string repoDir, IProgress<string> progress, CancellationToken ct);

    // remotes / push / local exclude
    Task<string?> GetRemoteUrlAsync(string repoDir, string remoteName, CancellationToken ct);
    Task<GitOpResult> RemoveRemoteAsync(string repoDir, string remoteName, IProgress<string> progress, CancellationToken ct);
    Task<GitOpResult> EnsureRemoteUrlAsync(string repoDir, string remoteName, string cleanRemoteUrl, IProgress<string> progress, CancellationToken ct);

    Task<GitOpResult> PushSetUpstreamAsync(string repoDir, string remoteName, string branchName, IProgress<string> progress, CancellationToken ct);

    Task<GitOpResult> EnsureLocalExcludeAsync(string repoDir, string[] patterns, IProgress<string> progress, CancellationToken ct);
    Task<GitOpResult> EnsureCredentialHelperAsync(string repoDir, IProgress<string> progress, CancellationToken ct);
    Task<GitOpResult> EnsureLineEndingConfigAsync(string repoDir, IProgress<string> progress, CancellationToken ct);
    Task<string[]> GetChangedPathsForBackupAsync(string repoDir, string[]? includePrefixes, CancellationToken ct);
    Task<(int behind, int ahead)> GetAheadBehindAsync(string repoDir, string upstreamRef, CancellationToken ct);
    Task<GitOpResult> CreateBranchAtHeadAsync(string repoDir, string branchName, IProgress<string> progress, CancellationToken ct);
}
