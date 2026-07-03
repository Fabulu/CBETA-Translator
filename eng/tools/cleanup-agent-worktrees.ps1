<#
.SYNOPSIS
Garbage-collect stale Claude agent worktrees and their branches.

.DESCRIPTION
Agent sessions create worktrees under .claude/worktrees plus worktree-agent-*
branches and never clean them up (the 2026-07-02 audit found 37 worktrees / 11 GB
and 45 branches, all fully merged). This script removes every agent worktree whose
HEAD is reachable from main AND whose working tree has no real modifications
(line-ending-only diffs are ignored) and no untracked files, then deletes
worktree-agent-* branches that are merged into main.

Anything with unmerged commits, real modifications, or untracked files is left
alone and reported.

.PARAMETER Force
Actually delete. Without -Force the script only reports what it would do.

.EXAMPLE
.\eng\tools\cleanup-agent-worktrees.ps1           # dry run
.\eng\tools\cleanup-agent-worktrees.ps1 -Force    # delete
#>
param(
    [switch]$Force
)

$ErrorActionPreference = 'Stop'
$repoRoot = git rev-parse --show-toplevel
if (-not $repoRoot) { throw "Not inside a git repository." }
Set-Location $repoRoot

$mode = if ($Force) { "DELETE" } else { "DRY RUN (pass -Force to delete)" }
Write-Host "cleanup-agent-worktrees: $mode"

$wtRoot = Join-Path $repoRoot ".claude\worktrees"
$removed = 0; $kept = 0
foreach ($wt in Get-ChildItem $wtRoot -Directory -ErrorAction SilentlyContinue) {
    $p = $wt.FullName
    $unmerged = @(git -C $p log --oneline "main..HEAD" 2>$null | Where-Object { $_ }).Count
    # --numstat honors --ignore-cr-at-eol; --name-only does NOT (git quirk)
    $realMod = @(git -C $p diff --ignore-cr-at-eol --numstat 2>$null | Where-Object {
        $parts = $_ -split "`t"
        $parts.Count -ge 3 -and -not ($parts[0] -eq '0' -and $parts[1] -eq '0')
    }).Count
    $untracked = @(git -C $p status --porcelain 2>$null | Where-Object { $_ -match '^\?\?' }).Count

    if ($unmerged -eq 0 -and $realMod -eq 0 -and $untracked -eq 0) {
        Write-Host "  remove $($wt.Name) (clean, merged)"
        # Not `git worktree remove`: its back-pointer validation rejects these
        # worktrees on Windows when drive-path casing differs (seen 2026-07-04).
        # Deleting the directory and pruning is equivalent and robust.
        if ($Force) { Remove-Item -Recurse -Force $p }
        $removed++
    }
    else {
        Write-Host "  KEEP   $($wt.Name): unmerged=$unmerged realModified=$realMod untracked=$untracked"
        $kept++
    }
}
if ($Force) { git worktree prune }

$deletedBranches = 0
foreach ($b in git branch --merged main --list 'worktree-agent-*' --format '%(refname:short)') {
    # skip branches still checked out in a kept worktree
    $inUse = git worktree list --porcelain | Select-String -SimpleMatch "branch refs/heads/$b" -Quiet
    if ($inUse) { Write-Host "  KEEP branch $b (worktree in use)"; continue }
    Write-Host "  delete branch $b (merged)"
    if ($Force) { git branch -d $b | Out-Null }
    $deletedBranches++
}

Write-Host "worktrees: $removed removed, $kept kept; branches: $deletedBranches deleted ($mode)"
