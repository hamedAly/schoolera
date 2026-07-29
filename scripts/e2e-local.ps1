# Run Schoolera Playwright E2E locally (single-origin, matches CI hosting).
# Prerequisites: SQL Server, seed password, portable Node 24.18.0 optional for SPA build.

param(
    [string]$SeedPassword = $env:E2E_SEED_PASSWORD,
    [string]$BaseUrl = "http://127.0.0.1:5085",
    [switch]$SkipBuild,
    [string]$Filter = ""
)

$ErrorActionPreference = "Stop"
$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")

if ([string]::IsNullOrWhiteSpace($SeedPassword)) {
    throw "Set E2E_SEED_PASSWORD or pass -SeedPassword."
}

$nodeDir = Join-Path $repoRoot ".tools\node-v24.18.0-win-x64"
if (Test-Path (Join-Path $nodeDir "node.exe")) {
    $env:Path = "$nodeDir;" + $env:Path
}

if (-not $SkipBuild) {
    Push-Location (Join-Path $repoRoot "src\Schoolera.Api\Schoolera-SPA")
    node --version
    npm ci
    npm run build
    Pop-Location

    dotnet build (Join-Path $repoRoot "Schoolera.slnx") -c Debug
    $playwright = Join-Path $repoRoot "tests\Schoolera.E2E\bin\Debug\net10.0\playwright.ps1"
    if (Test-Path $playwright) {
        powershell -ExecutionPolicy Bypass -File $playwright install chromium
    }
}

$env:E2E_BASE_URL = $BaseUrl
$env:E2E_SEED_PASSWORD = $SeedPassword

$testArgs = @(
    "test",
    (Join-Path $repoRoot "tests\Schoolera.E2E\Schoolera.E2E.csproj"),
    "-c", "Debug",
    "--settings", (Join-Path $repoRoot "tests\Schoolera.E2E\.runsettings")
)
if ($Filter) {
    $testArgs += @("--filter", $Filter)
}

Write-Host "E2E_BASE_URL=$BaseUrl"
Push-Location $repoRoot
dotnet @testArgs
Pop-Location
