param(
    [switch]$InstallDotnet,
    [switch]$InstallGit,
    [switch]$InstallWkHtmlToPdf
)

$ErrorActionPreference = 'Stop'

function Test-Command($name) {
    return $null -ne (Get-Command $name -ErrorAction SilentlyContinue)
}

Write-Host "== CBETA Translator bootstrap check =="

$missing = @()

if (Test-Command dotnet) {
    $dotnetVersion = (& dotnet --version)
    Write-Host "[ok] dotnet found: $dotnetVersion"
} else {
    Write-Warning "dotnet not found"
    $missing += 'dotnet'
}

if (Test-Command git) {
    $gitVersion = (& git --version)
    Write-Host "[ok] $gitVersion"
} else {
    Write-Warning "git not found"
    $missing += 'git'
}

if (Test-Command wkhtmltopdf) {
    $wkVersion = (& wkhtmltopdf --version)
    Write-Host "[ok] $wkVersion"
} else {
    Write-Warning "wkhtmltopdf not found (PDF export may fail depending on configuration)"
    $missing += 'wkhtmltopdf'
}

if ($missing.Count -eq 0) {
    Write-Host "All required tools are available."
    exit 0
}

if (-not (Test-Command winget)) {
    Write-Warning "winget not available. Install missing tools manually: $($missing -join ', ')"
    exit 1
}

if ($InstallDotnet -and $missing -contains 'dotnet') {
    Write-Host "Installing .NET SDK 8 via winget..."
    winget install --id Microsoft.DotNet.SDK.8 --source winget --accept-package-agreements --accept-source-agreements
}

if ($InstallGit -and $missing -contains 'git') {
    Write-Host "Installing Git via winget..."
    winget install --id Git.Git --source winget --accept-package-agreements --accept-source-agreements
}

if ($InstallWkHtmlToPdf -and $missing -contains 'wkhtmltopdf') {
    Write-Host "Installing wkhtmltopdf via winget..."
    winget install --id wkhtmltopdf.wkhtmltox --source winget --accept-package-agreements --accept-source-agreements
}

Write-Host "Bootstrap finished. Re-open terminal and run eng/build.ps1"
