#Requires -Version 5.1
<#
.SYNOPSIS
  Stop local CareHub dev processes: PowerShell jobs, stray dotnet/node listeners, Docker Compose stack.

.DESCRIPTION
  Typical order:
  1. Stop all background jobs whose name starts with CareHub (from .\Run-CareHub.ps1 -Run / -RunWeb).
  2. Optionally stop processes listening on CareHub dev ports (dotnet / node only).
  3. docker compose down for this repo (Postgres, RabbitMQ, Redis).

.PARAMETER SkipJobs
  Do not stop or remove CareHub* PowerShell jobs.

.PARAMETER SkipDocker
  Do not run docker compose down.

.PARAMETER RemoveDockerVolumes
  Passes -v to docker compose down (deletes Postgres data volume and other compose volumes).

.PARAMETER SkipKillListeners
  Do not attempt to stop dotnet/node processes bound to CareHub HTTP ports (5001–5011, 53615, 5173).

.EXAMPLE
  .\Cleanup-CareHub.ps1
  .\Cleanup-CareHub.ps1 -RemoveDockerVolumes
  .\Cleanup-CareHub.ps1 -SkipDocker -SkipKillListeners
#>
param(
    [switch] $SkipJobs,
    [switch] $SkipDocker,
    [switch] $RemoveDockerVolumes,
    [switch] $SkipKillListeners
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$Root = $PSScriptRoot

# Ports used by gateway, microservices (appsettings), Telegram bot, and Vite — not Postgres/Rabbit/Redis host ports.
$CareHubListenerPorts = @(
    5001, 5002, 5003, 5004, 5005, 5006, 5007, 5008, 5009, 5010, 5011,
    53615,
    5173
)

Write-Host ""
Write-Host "CareHub cleanup" -ForegroundColor Cyan
Write-Host ""

if (-not $SkipJobs) {
    $careHubJobs = Get-Job -Name 'CareHub*' -ErrorAction SilentlyContinue
    if ($careHubJobs) {
        Write-Host "Stopping PowerShell jobs: $($careHubJobs.Name -join ', ')" -ForegroundColor Yellow
        $careHubJobs | Stop-Job -Force -ErrorAction SilentlyContinue
        $careHubJobs | Remove-Job -Force -ErrorAction SilentlyContinue
        Write-Host "Jobs removed." -ForegroundColor Green
    }
    else {
        Write-Host "No CareHub* background jobs in this session." -ForegroundColor Gray
    }
}
else {
    Write-Host "Skipping jobs (--SkipJobs)." -ForegroundColor Gray
}

if (-not $SkipKillListeners) {
    Write-Host "Checking listeners on dev ports (dotnet / node only)..." -ForegroundColor Yellow
    $killed = @()
    foreach ($port in $CareHubListenerPorts) {
        $conns = Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue
        foreach ($c in $conns) {
            $pid = $c.OwningProcess
            if (-not $pid) { continue }
            try {
                $proc = Get-Process -Id $pid -ErrorAction Stop
            }
            catch {
                continue
            }
            if ($proc.ProcessName -notin @('dotnet', 'node')) {
                continue
            }
            try {
                Stop-Process -Id $proc.Id -Force -ErrorAction Stop
                $killed += "$($proc.ProcessName) (PID $($proc.Id), port $port)"
            }
            catch {
                Write-Host "  Could not stop PID $($proc.Id) on port $port : $($_.Exception.Message)" -ForegroundColor DarkYellow
            }
        }
    }
    if ($killed.Count -gt 0) {
        $killed | ForEach-Object { Write-Host "  Stopped $_" -ForegroundColor Green }
    }
    else {
        Write-Host "  No matching dotnet/node listeners on CareHub ports." -ForegroundColor Gray
    }
}
else {
    Write-Host "Skipping port listener cleanup (--SkipKillListeners)." -ForegroundColor Gray
}

if (-not $SkipDocker) {
    $composeFile = Join-Path $Root 'docker-compose.yml'
    if (-not (Test-Path $composeFile)) {
        Write-Host "docker-compose.yml not found; skipping Docker." -ForegroundColor DarkYellow
    }
    elseif (-not (Get-Command docker -ErrorAction SilentlyContinue)) {
        Write-Host "Docker CLI not found; skipping docker compose down." -ForegroundColor DarkYellow
    }
    else {
        $prevEa = $ErrorActionPreference
        $ErrorActionPreference = 'SilentlyContinue'
        $null = & docker info 2>&1
        $dockerOk = ($LASTEXITCODE -eq 0)
        $ErrorActionPreference = $prevEa

        if (-not $dockerOk) {
            Write-Host "Docker daemon not reachable; skipping docker compose down." -ForegroundColor DarkYellow
        }
        else {
            $args = @('compose', '-f', $composeFile, 'down')
            if ($RemoveDockerVolumes) {
                $args += '-v'
                Write-Host "Docker: compose down -v (removes volumes)..." -ForegroundColor Yellow
            }
            else {
                Write-Host "Docker: compose down..." -ForegroundColor Yellow
            }
            $prevEa = $ErrorActionPreference
            $ErrorActionPreference = 'SilentlyContinue'
            & docker @args 2>&1 | ForEach-Object { Write-Host $_ }
            $exit = $LASTEXITCODE
            $ErrorActionPreference = $prevEa
            if ($exit -eq 0) {
                Write-Host "Docker stack stopped." -ForegroundColor Green
            }
            else {
                Write-Host "docker compose down exited with $exit (containers may already be stopped)." -ForegroundColor DarkYellow
            }
        }
    }
}
else {
    Write-Host "Skipping Docker (--SkipDocker)." -ForegroundColor Gray
}

Write-Host ""
Write-Host "Cleanup finished." -ForegroundColor Cyan
Write-Host ""
