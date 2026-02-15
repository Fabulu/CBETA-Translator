param(
    [ValidateSet('Debug','Release')]
    [string]$Configuration = 'Debug'
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

Write-Host "== Restore =="
Invoke-Step { dotnet restore .\CbetaTranslator.App.sln } "restore"

Write-Host "== Build ($Configuration) =="
Invoke-Step { dotnet build .\CbetaTranslator.App.sln -c $Configuration --no-restore } "build"

Write-Host "== Done =="
Write-Host "Run app: dotnet run --project .\CbetaTranslator.App.csproj -c $Configuration"
