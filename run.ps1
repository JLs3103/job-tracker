$ErrorActionPreference = 'Stop'
$root = $PSScriptRoot
$backendDirectory = Join-Path $root 'backend'
$frontendDirectory = Join-Path $root 'frontend'
$backendProject = Join-Path $backendDirectory 'backend.csproj'
$backendAssembly = Join-Path $backendDirectory 'bin\Debug\net10.0\backend.dll'
$viteCli = Join-Path $frontendDirectory 'node_modules\vite\bin\vite.js'

function Require-Command([string]$Name) {
    if (-not (Get-Command $Name -ErrorAction SilentlyContinue)) {
        throw "Required command '$Name' was not found. Install the .NET 10 SDK and Node.js, then try again."
    }
}

function Wait-ForEndpoint(
    [string]$Uri,
    [System.Diagnostics.Process]$BackendProcess,
    [System.Diagnostics.Process]$FrontendProcess
) {
    $deadline = (Get-Date).AddSeconds(60)
    while ((Get-Date) -lt $deadline) {
        if ($BackendProcess.HasExited) {
            $BackendProcess.WaitForExit()
            throw "Backend exited during startup with code $($BackendProcess.ExitCode)."
        }
        if ($FrontendProcess.HasExited) {
            $FrontendProcess.WaitForExit()
            throw "Frontend exited during startup with code $($FrontendProcess.ExitCode)."
        }

        try {
            $null = Invoke-WebRequest -Uri $Uri -UseBasicParsing -TimeoutSec 2
            return
        }
        catch {
            Start-Sleep -Seconds 1
        }
    }

    throw "Timed out waiting for $Uri."
}

Require-Command 'dotnet'
Require-Command 'node'
Require-Command 'npm'

$sdks = & dotnet --list-sdks
if ($LASTEXITCODE -ne 0 -or -not ($sdks | Where-Object { $_ -match '^10\.' })) {
    throw 'The .NET 10 SDK is required. Install it and run this script again.'
}

if (-not (Test-Path $viteCli)) {
    Push-Location $frontendDirectory
    try {
        & npm ci
        if ($LASTEXITCODE -ne 0) {
            throw "npm ci failed with exit code $LASTEXITCODE."
        }
    }
    finally {
        Pop-Location
    }
}

Push-Location $backendDirectory
try {
    & dotnet build $backendProject --configuration Debug
    if ($LASTEXITCODE -ne 0) {
        $buildExitCode = $LASTEXITCODE
        $assetsFile = Join-Path $backendDirectory 'obj\project.assets.json'
        if (-not (Test-Path $assetsFile)) {
            throw "Backend build failed with exit code $buildExitCode."
        }

        Write-Host 'Restore failed. Retrying with the existing NuGet restore assets...'
        & dotnet build $backendProject --configuration Debug --no-restore
        if ($LASTEXITCODE -ne 0) {
            throw "Backend build failed with exit code $LASTEXITCODE."
        }
    }
}
finally {
    Pop-Location
}

$environmentNames = @(
    'ASPNETCORE_ENVIRONMENT',
    'ASPNETCORE_URLS',
    'VITE_USE_MOCK_API',
    'VITE_API_BASE_URL'
)
$previousEnvironment = @{}
foreach ($name in $environmentNames) {
    $previousEnvironment[$name] = [Environment]::GetEnvironmentVariable($name, 'Process')
}

$backendProcess = $null
$frontendProcess = $null
try {
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    $env:ASPNETCORE_URLS = 'http://localhost:5227'
    $env:VITE_USE_MOCK_API = 'false'
    $env:VITE_API_BASE_URL = 'http://localhost:5227'

    $backendProcess = Start-Process `
        -FilePath 'dotnet' `
        -ArgumentList @('"' + $backendAssembly + '"') `
        -WorkingDirectory $backendDirectory `
        -NoNewWindow `
        -PassThru
    $frontendProcess = Start-Process `
        -FilePath 'node' `
        -ArgumentList @('"' + $viteCli + '"', '--host', 'localhost') `
        -WorkingDirectory $frontendDirectory `
        -NoNewWindow `
        -PassThru

    Wait-ForEndpoint 'http://localhost:5227/openapi/v1.json' $backendProcess $frontendProcess
    Wait-ForEndpoint 'http://localhost:5173/' $backendProcess $frontendProcess

    Write-Host ''
    Write-Host 'Job Tracker is running:'
    Write-Host '  App:     http://localhost:5173'
    Write-Host '  API:     http://localhost:5227'
    Write-Host '  OpenAPI: http://localhost:5227/openapi/v1.json'
    Write-Host 'Press Ctrl+C to stop both services.'

    while (-not $backendProcess.HasExited -and -not $frontendProcess.HasExited) {
        Start-Sleep -Seconds 1
    }

    if ($backendProcess.HasExited) {
        throw "Backend stopped with exit code $($backendProcess.ExitCode)."
    }
    throw "Frontend stopped with exit code $($frontendProcess.ExitCode)."
}
finally {
    foreach ($process in @($frontendProcess, $backendProcess)) {
        if ($null -ne $process -and -not $process.HasExited) {
            Stop-Process -Id $process.Id
            $process.WaitForExit()
        }
    }

    foreach ($name in $environmentNames) {
        if ($null -eq $previousEnvironment[$name]) {
            Remove-Item "Env:$name" -ErrorAction SilentlyContinue
        }
        else {
            [Environment]::SetEnvironmentVariable(
                $name,
                $previousEnvironment[$name],
                'Process')
        }
    }
}
