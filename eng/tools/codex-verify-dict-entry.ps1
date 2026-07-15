<#
.SYNOPSIS
    Third-gate adversarial verification of a Zen dictionary entry using the Codex CLI.

.DESCRIPTION
    Runs Codex (gpt-5.2-codex) as an INDEPENDENT adversarial verifier over one term's
    entry.v2.json. Codex re-checks every occurrence against the primary Chinese corpus,
    enforces the Zen allowlist, and tests the multi-source / over-read / imported-abstraction
    gates per CODEX_VERIFY_SPEC.md, then writes CODEX_VERDICT.md into the term directory.

    This is gate 3 of 3: (1) research self-check, (2) Claude adversarial pass, (3) Codex here.
    Codex runs with sandbox bypass so it reads the corpus (C:\temp\...\xml-p5), the allowlist,
    the guide, and the entry directly.

.EXAMPLE
    pwsh eng/tools/codex-verify-dict-entry.ps1 -TermId t_36aa29eb1287
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string]$TermId,
    # The real 5.6 model id on this account is "gpt-5.6-sol" (per ~/.codex/config.toml; the plain
    # "gpt-5.6"/"gpt-5.6-codex" ids 400 out). Strongest available — the user's requested gate model.
    [string]$Model = "gpt-5.6-sol"
)

$ErrorActionPreference = "Stop"

$repo   = (Resolve-Path (Join-Path $PSScriptRoot "..\..")).Path
$runDir = Join-Path $repo "runs\CLAUDE-RUNS\RUN-20260711-1248-full-cbeta-translation\dictionary-build"
$spec   = Join-Path $runDir "CODEX_VERIFY_SPEC.md"
$termDir = Join-Path (Join-Path $runDir "terms") $TermId
$entry  = Join-Path $termDir "entry.v2.json"

if (-not (Test-Path $spec))  { Write-Error "Spec not found: $spec"; exit 1 }
if (-not (Test-Path $entry)) { Write-Error "No entry.v2.json in $termDir (nothing to verify)"; exit 1 }

$prompt = @"
You are GATE 3, an independent adversarial verifier of a single Zen dictionary entry.
Read this spec and follow it EXACTLY:
$spec

Verify the entry in this term directory (its entry.v2.json + WORK.md):
$termDir

Re-derive everything from the primary Chinese. Grep each cited RelPath file under
C:\temp\NewTranslationrepos\CbetaZenTexts\xml-p5 to confirm every Kwic is VERBATIM; enforce the
Zen allowlist at $repo\Assets\Data\zen-corpus.json; test the multi-source, over-read, and
imported-abstraction checks. Be adversarial — do not pass fabrication, contamination, or an
unsupported multi-source/uniqueness claim.

WRITE your verdict (PASS | REVISE | FAIL, per-sense findings with file evidence) to:
$termDir\CODEX_VERDICT.md
"@

$logPath = Join-Path $termDir "codex-verify.log"
$verdictPath = Join-Path $termDir "CODEX_VERDICT.md"
# Delete any prior verdict so a stale one can never be mistaken for this run's result,
# and a run that writes nothing is detectable as a missing verdict.
if (Test-Path $verdictPath) { Remove-Item $verdictPath -Force }
Write-Host "[gate3] Codex verifying $TermId -> $verdictPath"

# Invoke Codex via its PowerShell shim (runs in-process) rather than letting `codex` resolve to
# codex.cmd, which spawns a visible cmd/console window per run.
$codexPs1 = Join-Path $env:APPDATA "npm\codex.ps1"
$codexInvoke = if (Test-Path $codexPs1) { $codexPs1 } else { "codex" }

$prompt | & $codexInvoke exec --model $Model --dangerously-bypass-approvals-and-sandbox -C $repo - |
    Out-File -FilePath $logPath -Encoding utf8

if (Test-Path (Join-Path $termDir "CODEX_VERDICT.md")) {
    Write-Host "[gate3] Verdict written: $termDir\CODEX_VERDICT.md"
} else {
    Write-Warning "[gate3] Codex finished but no CODEX_VERDICT.md was written - check $logPath"
}
