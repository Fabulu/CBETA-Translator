using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using ReadZen.App.Models;

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
    /// Returns <c>null</c> when status could NOT be determined (git failed) — callers
    /// MUST treat null as "unknown" and block destructive operations, never as "clean".
    /// </summary>
    Task<string[]?> GetStatusPorcelainAsync(string repoDir, CancellationToken ct);

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
    /// <summary>
    /// Repo-relative paths to preserve before a destructive update. Returns <c>null</c>
    /// when git status failed — the update flow MUST abort before reset --hard rather
    /// than proceed with an empty backup list.
    /// </summary>
    Task<string[]?> GetChangedPathsForBackupAsync(string repoDir, string[]? includePrefixes, CancellationToken ct);

    /// <summary>
    /// Behind/ahead counts vs an upstream ref. Returns <c>null</c> when the counts could
    /// NOT be determined — callers MUST treat null as "unknown" (ahead&gt;0 gates the
    /// rescue-branch safety net before destructive updates), never as "in sync".
    /// </summary>
    Task<(int behind, int ahead)?> GetAheadBehindAsync(string repoDir, string upstreamRef, CancellationToken ct);
    Task<GitOpResult> CreateBranchAtHeadAsync(string repoDir, string branchName, IProgress<string> progress, CancellationToken ct);

    // ── History browsing (read-only) ──────────────────────────────────

    /// <summary>
    /// Returns the commit log for a specific file, newest first.
    /// Uses --follow to track renames.
    /// </summary>
    Task<List<GitCommitEntry>> GetFileLogAsync(string repoDir, string relPath, int maxCount = 50, CancellationToken ct = default);

    /// <summary>
    /// Returns the contents of a file at a specific commit, or null if the file
    /// did not exist at that commit.
    /// </summary>
    Task<string?> GetFileAtCommitAsync(string repoDir, string commitHash, string relPath, CancellationToken ct = default);

    /// <summary>
    /// Returns a unified diff of a file between two commits.
    /// </summary>
    Task<string> GetFileDiffAsync(string repoDir, string commitHashA, string commitHashB, string relPath, CancellationToken ct = default);

    /// <summary>Returns the HEAD commit SHA for the given repo.</summary>
    Task<string?> GetHeadShaAsync(string repoDir, CancellationToken ct = default);

    /// <summary>
    /// Returns a diff --stat summary between two commits (or HEAD and a commit).
    /// Each line is "path | +N -M" format.
    /// </summary>
    Task<string> GetDiffStatAsync(string repoDir, string commitA, string commitB, CancellationToken ct = default);
}
