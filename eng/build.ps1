param(
    [ValidateSet('Debug','Release')]
    [string]$Configuration = 'Debug',
    [switch]$Clean
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
Set-Location $root

if (-not (Get-Command dotnet -ErrorAction SilentlyContinue)) {
    Write-Error "dotnet not found. Run eng/bootstrap.ps1 first."
}

function Invoke-Step([scriptblock]$Step, [string]$Name) {
    & $Step
    if ($LASTEXITCODE -ne 0) {
        throw "$Name failed with exit code $LASTEXITCODE"
    }
}

if ($Clean) {
    Write-Host "== Clean outputs =="
    Get-ChildItem -Path $root -Recurse -Directory -Force |
        Where-Object { $_.Name -in @('bin','obj') } |
        ForEach-Object {
            try { Remove-Item -Recurse -Force $_.FullName -ErrorAction Stop } catch {}
        }
}

Write-Host "== Restore =="
Invoke-Step { dotnet restore .\CbetaTranslator.App.sln } "restore"

Write-Host "== Build ($Configuration) =="
Invoke-Step { dotnet build .\CbetaTranslator.App.sln -c $Configuration --no-restore } "build"

# Stage only the active native PDF DLL for the app.
$outDir = Join-Path $root "bin\$Configuration\net8.0"
$nativeCandidates = @(
    $env:CBETA_GUI_DLL_PATH,
    "D:\Rust-projects\MT15-model\cbeta-gui-dll\target\$Configuration\cbeta_gui_dll.dll",
    "D:\Rust-projects\MT15-model\cbeta-gui-dll\target\release\cbeta_gui_dll.dll",
    "/mnt/d/Rust-projects/MT15-model/cbeta-gui-dll/target/release/cbeta_gui_dll.dll"
) | Where-Object { -not [string]::IsNullOrWhiteSpace($_) }

$nativeDll = $nativeCandidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if ($nativeDll) {
    New-Item -ItemType Directory -Path $outDir -Force | Out-Null
    Copy-Item -Force $nativeDll (Join-Path $outDir "cbeta_gui_dll.dll")
    Write-Host "Native DLL staged: $nativeDll -> $outDir\\cbeta_gui_dll.dll"
}
else {
    Write-Warning "cbeta_gui_dll.dll not found in expected locations."
}

# Remove stale/legacy native DLL to avoid confusion.
$legacy = Join-Path $outDir "cbeta_pdf_creator.dll"
if (Test-Path $legacy) {
    Remove-Item -Force $legacy
    Write-Host "Removed legacy DLL: $legacy"
}

Write-Host "== Done =="
Write-Host "Run app: dotnet run --project .\CbetaTranslator.App.csproj -c $Configuration"
