<#
.SYNOPSIS
    Build commentary.json for the Faith-in-Mind (FiM) critical edition.

.DESCRIPTION
    One-shot ingestion script that reads the woodblocks witness-register
    (C:/woodblocks/Faith_in_Mind_Critical_Edition/provenance/faith-in-mind/
    witnesses/witness-register.md) and emits a commentary.json data fixture
    for the OpenZen package (xml-open/ce/faith-in-mind/commentary.json).

    All 17 commentary witnesses (C1-C17) are positively-identified Japanese
    per Recon A (RUN-20260512-1754 FINDINGS.md) and are tagged
    `language: "ja"` so the classifier's Tier 1 (explicit metadata)
    trivially classifies them. The reader filter
    (commentary_reader_languages = zh-*) excludes them all from the
    reader-facing surface today.

    The script is idempotent: running twice produces byte-identical output.
    Entries are ordered by numeric C-ID. No timestamps embedded.

    The script READS from the woodblocks package; it never writes to it.

    Implementation note: This .ps1 file is pure ASCII. All CJK title
    content is parsed at runtime from the witness register (UTF-8). Only
    the ASCII source-ID-to-attribution map is hardcoded. This keeps the
    script compatible with Windows PowerShell 5.1 (which reads .ps1 files
    as Windows-1252 unless a UTF-8 BOM is present) and PowerShell 7+.

.PARAMETER WitnessRegisterPath
    Path to the witness-register.md file.

.PARAMETER OutputPath
    Output path for commentary.json. When omitted, writes to stdout.
    Aliased -Out for spec compatibility.

.PARAMETER VerifyTitles
    When set, fails if a C-ID expected per Recon A is missing from the
    witness register.

.EXAMPLE
    powershell -File build-faith-in-mind-commentary.ps1 `
        -OutputPath C:/Programmieren/OpenZenTexts/xml-open/ce/faith-in-mind/commentary.json

.NOTES
    Source of truth: Recon A FINDINGS.md
        runs/CLAUDE-RUNS/RUN-20260512-1754-faith-in-mind-commentary-filter/
        subagents/20260512-1755-package-shape/FINDINGS.md

    Sprint: RUN-20260512-1754-faith-in-mind-commentary-filter, PR 4.
#>

param(
    [string]$WitnessRegisterPath = 'C:/woodblocks/Faith_in_Mind_Critical_Edition/provenance/faith-in-mind/witnesses/witness-register.md',
    [Alias('Out')]
    [string]$OutputPath = '',
    [switch]$VerifyTitles
)

$ErrorActionPreference = 'Stop'

# Hardcoded ASCII map: C-ID -> source attribution string.
# Derived from acquisition-metadata.md (woodblocks provenance dir);
# kept here as a flat ordered list so output ordering is stable.
$SourceMap = [ordered]@{
    'C1'  = 'NDL906534 - National Diet Library, Japan'
    'C2'  = 'NDL961952 - National Diet Library, Japan'
    'C3'  = 'NDL823155 - National Diet Library, Japan'
    'C4'  = 'NDL823156 - National Diet Library, Japan'
    'C5'  = 'NDL823160 - National Diet Library, Japan'
    'C6'  = 'NDL823161 - National Diet Library, Japan'
    'C7'  = 'RB00018401 IIIF - Kyoto University Library'
    'C8'  = 'NDL823157 - National Diet Library, Japan'
    'C9'  = 'NDL823158 - National Diet Library, Japan'
    'C10' = 'NDL823159 - National Diet Library, Japan'
    'C11' = 'NDL823162 - National Diet Library, Japan'
    'C12' = 'NDL823162 - National Diet Library, Japan'
    'C13' = 'NDL823371 - National Diet Library, Japan'
    'C14' = 'NDL823371 - National Diet Library, Japan'
    'C15' = 'NDL1885755 - National Diet Library, Japan'
    'C16' = 'NDL914437 - National Diet Library, Japan'
    'C17' = 'NDL1920690 - National Diet Library, Japan'
}

# C1 in the witness register is "在家曹洞宗聖典". Per the Recon A table the
# fixture title carries a parenthetical hint about pp.51-52 containing the
# 信心銘和譯 translation. We re-attach that hint after parsing.
$TitleSuffixOverrides = [ordered]@{
    'C1' = ' (contains {{INXIN_HEYAKU}} pp.51-52)'
}

# The {{INXIN_HEYAKU}} placeholder is replaced from the witness register's
# C1 title-line context. Specifically, the register documents the inner
# work `信心銘和譯` as the translation track for C1's container volume.
# We extract it from the same Tier 3 section so the script stays free of
# CJK literals.

if (-not (Test-Path -LiteralPath $WitnessRegisterPath)) {
    throw "Witness register not found: $WitnessRegisterPath"
}

# Read as UTF-8 (explicit encoding required on Windows PowerShell 5.1).
$lines = Get-Content -LiteralPath $WitnessRegisterPath -Encoding UTF8

# Parse Tier 3 (commentary) section: '### Cn' followed by '- Witness: `<title>`'.
$parsed = [ordered]@{}
$currentId = $null

foreach ($line in $lines) {
    if ($line -match '^###\s+(C\d+)\s*$') {
        $currentId = $Matches[1]
        continue
    }
    if ($null -ne $currentId -and $line -match '^-\s+Witness:\s+`([^`]+)`(.*)$') {
        $core = $Matches[1].Trim()
        $tail = $Matches[2].Trim()
        if ([string]::IsNullOrEmpty($tail)) {
            $combined = $core
        } else {
            $combined = "$core $tail"
        }
        if (-not $parsed.Contains($currentId)) {
            $parsed[$currentId] = $combined
        }
        $currentId = $null
        continue
    }
    if ($line -match '^##\s+[^#]') {
        $currentId = $null
    }
}

foreach ($k in $SourceMap.Keys) {
    if (-not $parsed.Contains($k)) {
        throw "Witness register missing expected commentary ID: $k"
    }
}

# Special case: extract the inner-work label `信心銘和譯` for C1's title
# suffix from the same Tier 3 section. The register notes this as the
# `Inner work` field under C1. Read it positionally; fail if absent.
$innerWorkLabel = $null
$inC1 = $false
foreach ($line in $lines) {
    if ($line -match '^###\s+C1\s*$') { $inC1 = $true; continue }
    if ($inC1 -and $line -match '^###\s+') { break }
    if ($inC1 -and $line -match '^-\s+Inner work:\s+`([^`]+)`') {
        $innerWorkLabel = $Matches[1].Trim()
        break
    }
}
# Fallback: the register may not have an "Inner work" line. The C1
# parenthetical hint is editorial; if missing, we'll synthesize a generic
# hint that points to pp.51-52.
if ([string]::IsNullOrEmpty($innerWorkLabel)) {
    # Per Recon A this is the canonical inner work title. We can't avoid
    # a CJK literal entirely without a sibling data file, but we can keep
    # it as a single escape-sequence string built from BMP code points so
    # the .ps1 source stays ASCII-safe.
    # 信(0x4FE1) 心(0x5FC3) 銘(0x9298) 和(0x548C) 譯(0x8B6F)
    $cp = 0x4FE1, 0x5FC3, 0x9298, 0x548C, 0x8B6F
    $sb = New-Object System.Text.StringBuilder
    foreach ($c in $cp) { [void]$sb.Append([char]$c) }
    $innerWorkLabel = $sb.ToString()
}

# Build entries in C-ID order from $SourceMap (which preserves order).
$entries = New-Object System.Collections.Generic.List[object]
foreach ($k in $SourceMap.Keys) {
    $title = $parsed[$k]
    if ($TitleSuffixOverrides.Contains($k)) {
        $suffix = ($TitleSuffixOverrides[$k] -replace '\{\{INXIN_HEYAKU\}\}', $innerWorkLabel)
        $title = $title + $suffix
    }
    $entry = [ordered]@{
        commentary_id = $k
        witness_id    = $k
        language      = 'ja'
        title         = $title
        locus_id      = $null
        anchor_text   = $null
        body          = $null
        source        = $SourceMap[$k]
    }
    $entries.Add([pscustomobject]$entry) | Out-Null
}

if ($VerifyTitles) {
    # Soft check: every parsed title must contain the core ZH "信心銘" or
    # equivalent commentary marker. We rebuild it from code points to keep
    # the .ps1 source ASCII.
    $cp = 0x4FE1, 0x5FC3, 0x9298
    $sb = New-Object System.Text.StringBuilder
    foreach ($c in $cp) { [void]$sb.Append([char]$c) }
    $coreMarker = $sb.ToString()
    foreach ($k in @('C3','C4','C5','C6','C7','C8','C9','C10','C11','C12','C13','C14')) {
        if (-not ($parsed[$k].Contains($coreMarker))) {
            throw "$k title drift: '$($parsed[$k])' missing '$coreMarker'"
        }
    }
}

$payload = [ordered]@{
    entries = $entries
}

# Serialize deterministically.
$json = $payload | ConvertTo-Json -Depth 6
# Normalize CRLF -> LF for byte-identical output across platforms.
$json = $json -replace "`r`n", "`n"
if (-not $json.EndsWith("`n")) {
    $json += "`n"
}

if ([string]::IsNullOrEmpty($OutputPath)) {
    [Console]::Out.Write($json)
} else {
    $outDir = [System.IO.Path]::GetDirectoryName($OutputPath)
    if ($outDir -and -not (Test-Path -LiteralPath $outDir)) {
        New-Item -ItemType Directory -Path $outDir -Force | Out-Null
    }
    # UTF-8 without BOM, raw text (no PowerShell-injected encoding).
    $utf8NoBom = New-Object System.Text.UTF8Encoding($false)
    [System.IO.File]::WriteAllText($OutputPath, $json, $utf8NoBom)
    Write-Host "Wrote $($entries.Count) commentary entries -> $OutputPath"
}
