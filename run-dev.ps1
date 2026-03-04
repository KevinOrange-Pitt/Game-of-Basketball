$ErrorActionPreference = 'Stop'

$rootDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$apiProject = Join-Path $rootDir 'GobTrackerApi\GobTrackerApi.csproj'
$mauiProject = Join-Path $rootDir 'MauiApp1\MauiApp1.csproj'
$apiSettings = Join-Path $rootDir 'GobTrackerApi\appsettings.Development.json'
$apiHealthUrl = 'http://127.0.0.1:5117/api/health'

Set-Location $rootDir

$configContent = Get-Content $apiSettings -Raw
if ($configContent -match '<sql-user>|<sql-password>|\{your_username\}|\{your_password\}') {
    Write-Host 'Update GobTrackerApi\appsettings.Development.json with real auth settings before running.'
    exit 1
}

Write-Host 'Starting local API...'
$env:ASPNETCORE_ENVIRONMENT = 'Development'
$apiProcess = Start-Process -FilePath dotnet -ArgumentList @('run', '--project', $apiProject, '--urls', 'http://127.0.0.1:5117') -PassThru -WindowStyle Hidden

function Stop-Api {
    if ($null -ne $apiProcess -and -not $apiProcess.HasExited) {
        Write-Host 'Stopping local API...'
        Stop-Process -Id $apiProcess.Id -Force -ErrorAction SilentlyContinue
    }
}

try {
    Write-Host 'Waiting for API to become healthy...'
    $apiReady = $false
    for ($i = 0; $i -lt 30; $i++) {
        try {
            $response = Invoke-WebRequest -Uri $apiHealthUrl -UseBasicParsing -TimeoutSec 2
            if ($response.StatusCode -eq 200) {
                $apiReady = $true
                break
            }
        }
        catch {
            Start-Sleep -Seconds 1
        }
    }

    if (-not $apiReady) {
        Write-Host 'API failed to start on http://127.0.0.1:5117.'
        exit 1
    }

    Write-Host 'API is ready.'
    Write-Host 'Starting MAUI app...'
    dotnet run --project $mauiProject -f net10.0-windows10.0.19041.0
}
finally {
    Stop-Api
}
