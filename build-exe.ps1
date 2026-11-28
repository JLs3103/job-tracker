$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$frontendDir = Join-Path $root 'frontend'
$backendDir = Join-Path $root 'backend'
$backendProject = Join-Path $backendDir 'backend.csproj'
$wwwrootDir = Join-Path $backendDir 'wwwroot'
$publishDir = Join-Path $root 'publish'

Write-Host '1. Building frontend directly to backend/wwwroot...'
Push-Location $frontendDir
$prevMock = $env:VITE_USE_MOCK_API
$prevApi = $env:VITE_API_BASE_URL
try {
    $env:VITE_USE_MOCK_API = 'false'
    $env:VITE_API_BASE_URL = ''
    & npx tsc -b
    if ($LASTEXITCODE -ne 0) {
        throw "TypeScript check failed with exit code $LASTEXITCODE."
    }
    & npx vite build --outDir $wwwrootDir --emptyOutDir false
    if ($LASTEXITCODE -ne 0) {
        throw "Frontend build failed with exit code $LASTEXITCODE."
    }
}
finally {
    $env:VITE_USE_MOCK_API = $prevMock
    $env:VITE_API_BASE_URL = $prevApi
    Pop-Location
}

Write-Host '2. Publishing JobTracker.exe...'
$assetsFile = Join-Path $backendDir 'obj\project.assets.json'
$publishArgs = @(
    'publish',
    $backendProject,
    '--configuration', 'Release',
    '-p:AssemblyName=JobTracker',
    '-p:OutputType=WinExe',
    '--output', $publishDir
)
if (Test-Path $assetsFile) {
    $publishArgs += '--no-restore'
}
& dotnet @publishArgs
if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish failed with exit code $LASTEXITCODE."
}

$sourceDb = Join-Path $backendDir 'jobs.db'
$sourceProfile = Join-Path $backendDir 'Data\profile.json'
$sourceDevSettings = Join-Path $backendDir 'appsettings.Development.json'

if (Test-Path $sourceDevSettings) {
    Copy-Item $sourceDevSettings (Join-Path $publishDir 'appsettings.Production.json') -Force
}

$prodSettingsPath = Join-Path $publishDir 'appsettings.Production.json'
$prodSettings = if (Test-Path $prodSettingsPath) {
    Get-Content $prodSettingsPath -Raw | ConvertFrom-Json
} else {
    [PSCustomObject]@{}
}
$prodSettings | Add-Member -NotePropertyName 'ConnectionStrings' -NotePropertyValue ([PSCustomObject]@{
    DefaultConnection = "Data Source=$sourceDb"
}) -Force
$prodSettings | ConvertTo-Json -Depth 10 | Set-Content $prodSettingsPath -Encoding UTF8

$publishDataDir = Join-Path $publishDir 'Data'
if (-not (Test-Path $publishDataDir)) {
    New-Item -ItemType Directory -Path $publishDataDir | Out-Null
}
if (Test-Path $sourceProfile) {
    $targetProfile = Join-Path $publishDataDir 'profile.json'
    if (-not (Test-Path $targetProfile)) {
        Copy-Item $sourceProfile $targetProfile -Force
    }
}

Write-Host ''
Write-Host 'Build selesai!'
Write-Host "  Executable: $(Join-Path $publishDir 'JobTracker.exe')"
Write-Host "  Database:   $sourceDb (terhubung langsung)"
Write-Host "  Cara pakai: Double-click JobTracker.exe (masuk ke System Tray & buka http://localhost:5173)"
