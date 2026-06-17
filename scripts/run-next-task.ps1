param(
    [switch]$DryRun,
    [switch]$Run,
    [switch]$Loop,
    [switch]$Status,
    [switch]$Retry,
    [int]$Max = 3,
    [switch]$SkipInstall
)

$ErrorActionPreference = "Stop"
$ProjectRoot = Split-Path -Parent $PSScriptRoot
$RunnerDir = Join-Path $PSScriptRoot "agent-runner"

function Import-FlowBoardDotEnv {
    param([string]$EnvPath)
    if (-not (Test-Path $EnvPath)) { return $false }
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
    return $true
}

$dotEnvPath = Join-Path $ProjectRoot ".env"
$dotEnvLoaded = Import-FlowBoardDotEnv -EnvPath $dotEnvPath

if (-not $DryRun -and -not $Run -and -not $Loop -and -not $Status) {
    Write-Host @"
FlowBoard agent-runner

  powershell scripts/run-next-task.ps1 -DryRun     Preview prompt for manual new chat
  powershell scripts/run-next-task.ps1 -Run        Run next task via Cursor SDK
  powershell scripts/run-next-task.ps1 -Retry      Include failed tasks on retry
  powershell scripts/run-next-task.ps1 -Loop       Run up to -Max tasks sequentially (default 3)
  powershell scripts/run-next-task.ps1 -Status     Show queue status

API key (local only, git-ignored):
  Copy-Item .env.example .env
  Add CURSOR_API_KEY=cursor_... to .env
  Get key: Cursor Dashboard → API Keys (https://cursor.com/dashboard)

Examples:
  powershell scripts/run-next-task.ps1 -Run
  powershell scripts/run-next-task.ps1 -Loop -Max 2
"@
    exit 0
}

function Test-NodeVersion {
    $ver = node -p "process.versions.node" 2>$null
    if (-not $ver) { return $false }
    $parts = $ver -split '\.'
    $major = [int]$parts[0]; $minor = [int]$parts[1]
    return ($major -gt 22) -or ($major -eq 22 -and $minor -ge 13)
}

if (-not $DryRun -and -not $Status) {
    if (-not (Test-NodeVersion)) {
        $ver = node -p "process.versions.node" 2>$null
        Write-Error "@cursor/sdk requires Node >= 22.13 (current: $ver). Upgrade Node.js."
    }
}

if (-not (Test-Path (Join-Path $RunnerDir "node_modules\@connectrpc\connect-node"))) {
    if ($SkipInstall) {
        Write-Error "node_modules missing. Run: cd scripts/agent-runner && npm install"
    }
    Write-Host "[agent-runner] Installing dependencies…"
    Push-Location $RunnerDir
    try {
        npm install --silent 2>$null
        if ($LASTEXITCODE -ne 0) { npm install }
    } finally {
        Pop-Location
    }
}

Push-Location $RunnerDir
try {
    if ($Status) {
        npx tsx src/status.ts --root="$ProjectRoot"
        exit $LASTEXITCODE
    }
    if ($Loop) {
        $args = @("tsx", "src/run-loop.ts", "--root=$ProjectRoot", "--max=$Max")
        if ($DryRun) { $args += "--dry-run" }
        npx @args
        exit $LASTEXITCODE
    }
    if ($DryRun) {
        $tsxArgs = @("tsx", "src/run-next.ts", "--dry-run", "--root=$ProjectRoot")
        if ($Retry) { $tsxArgs += "--retry-failed" }
        npx @tsxArgs
        exit $LASTEXITCODE
    }
    if ($Run) {
        if (-not $env:CURSOR_API_KEY) {
            $hint = if (-not $dotEnvLoaded) {
                "Create .env from .env.example and set CURSOR_API_KEY=cursor_..."
            } else {
                "CURSOR_API_KEY is missing or empty in .env"
            }
            Write-Error "$hint`nMint a key: Cursor Dashboard → API Keys (https://cursor.com/dashboard)"
        }
        $tsxArgs = @("tsx", "src/run-next.ts", "--root=$ProjectRoot")
        if ($Retry) { $tsxArgs += "--retry-failed" }
        npx @tsxArgs
        exit $LASTEXITCODE
    }
} finally {
    Pop-Location
}
