param(
    [switch]$UseCompose,
    [switch]$KeepCompose
)

$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path -Parent $PSScriptRoot
$IntegrationProject = Join-Path $ProjectRoot "tests\FlowBoard.IntegrationTests\FlowBoard.IntegrationTests.csproj"
$ComposeFile = Join-Path $ProjectRoot "docker-compose.integration.yml"

function Get-IntegrationSaPassword {
    $default = "FlowBoard_Integration_Test1!"
    $fromEnv = $env:MSSQL_SA_PASSWORD
    if ([string]::IsNullOrWhiteSpace($fromEnv)) { return $default }
    if ($fromEnv -match 'YOUR_STRONG|REPLACE_WITH|_HERE$') { return $default }
    return $fromEnv
}

function Test-DockerDaemon {
    docker info *> $null
    return $LASTEXITCODE -eq 0
}

function Import-FlowBoardDotEnv {
    param([string]$EnvPath)
    if (-not (Test-Path $EnvPath)) { return }
    Get-Content $EnvPath -Encoding UTF8 | ForEach-Object {
        $line = $_.Trim()
        if (-not $line -or $line.StartsWith("#")) { return }
        $eq = $line.IndexOf("=")
        if ($eq -lt 1) { return }
        $name = $line.Substring(0, $eq).Trim()
        $value = $line.Substring($eq + 1).Trim()
        if ($value.Length -ge 2) {
            $q = $value[0]
            if (($q -eq '"' -or $q -eq "'") -and $value[-1] -eq $q) {
                $value = $value.Substring(1, $value.Length - 2)
            }
        }
        if ([string]::IsNullOrWhiteSpace($value)) { return }
        if (-not (Get-Item -Path "env:$name" -ErrorAction SilentlyContinue)) {
            Set-Item -Path "env:$name" -Value $value
        }
    }
}

Import-FlowBoardDotEnv -EnvPath (Join-Path $ProjectRoot ".env")

if (-not (Test-DockerDaemon)) {
    Write-Error @"
Docker daemon is not running.

Start Docker Desktop, then rerun:
  pwsh scripts/run-integration-tests.ps1
"@
}

$startedCompose = $false
try {
    if ($UseCompose) {
        $password = Get-IntegrationSaPassword
        $env:MSSQL_SA_PASSWORD = $password
        $env:FLOWBOARD_INTEGRATION_CONNECTION = "Server=localhost,1434;Database=FlowBoardIntegration;User Id=sa;Password=$password;TrustServerCertificate=True;"

        Write-Host '[integration] Starting SQL Server via docker compose (port 1434)...'
        docker compose -f $ComposeFile up -d --wait
        if ($LASTEXITCODE -ne 0) {
            Write-Error "docker compose up failed"
        }
        $startedCompose = $true
        Write-Host '[integration] Using compose SQL - FLOWBOARD_INTEGRATION_CONNECTION set'
    } else {
        Remove-Item Env:FLOWBOARD_INTEGRATION_CONNECTION -ErrorAction SilentlyContinue
        Write-Host '[integration] Using TestContainers (ephemeral SQL Server container)'
    }

    Push-Location $ProjectRoot
    try {
        dotnet test $IntegrationProject --verbosity normal
        exit $LASTEXITCODE
    } finally {
        Pop-Location
    }
} finally {
    if ($startedCompose -and -not $KeepCompose) {
        Write-Host '[integration] Stopping compose SQL Server...'
        docker compose -f $ComposeFile down
    }
}
