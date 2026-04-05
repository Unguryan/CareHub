#Requires -Version 5.1
<#
.SYNOPSIS
  One entry point for local CareHub: Docker dependencies, EF migrations, build, and optional run.

.DESCRIPTION
  - Mode Seed: sets CareHub__SeedDemoData=true — demo users, patients, schedule doctors/shifts, etc.
  - Mode Clean: sets CareHub__SeedDemoData=false — only Identity infrastructure (roles + OIDC clients);
    no demo users and no Patient/Schedule demo inserts.

.PARAMETER Mode
  Seed (default) or Clean.

.PARAMETER RecreateVolumes
  Runs docker compose down -v before up, wiping Postgres data. Use for a truly empty database.

.PARAMETER Run
  Starts all .NET microservices and the gateway as PowerShell background jobs (see Get-Job / Receive-Job).

.PARAMETER RunWeb
  Starts the Vite web portal (npm run dev). Runs as a background job.

.PARAMETER IncludeTelegramBot
  Also starts CareHub.TelegramBot (optional; Identity calls it when configured).

.PARAMETER SkipDocker
  Skip docker compose (assume Postgres/RabbitMQ/Redis already running).

.PARAMETER SkipMigrations
  Skip dotnet ef database update for all contexts.

.PARAMETER SkipBuild
  Skip dotnet build.

.EXAMPLE
  .\Run-CareHub.ps1 -Mode Seed
  .\Run-CareHub.ps1 -Mode Clean -RecreateVolumes
  .\Run-CareHub.ps1 -Mode Seed -Run -RunWeb
#>
param(
    [ValidateSet('Seed', 'Clean')]
    [string] $Mode = 'Seed',

    [switch] $RecreateVolumes,
    [switch] $Run,
    [switch] $RunWeb,
    [switch] $IncludeTelegramBot,

    [switch] $SkipDocker,
    [switch] $SkipMigrations,
    [switch] $SkipBuild
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$Root = $PSScriptRoot
Set-Location $Root

$seedValue = if ($Mode -eq 'Seed') { 'true' } else { 'false' }
$env:CareHub__SeedDemoData = $seedValue

Write-Host ""
Write-Host "CareHub local runner" -ForegroundColor Cyan
Write-Host "  Mode: $Mode  (environment variable CareHub__SeedDemoData=$seedValue)" -ForegroundColor Gray
Write-Host ""

if (-not $SkipDocker) {
    if ($RecreateVolumes) {
        Write-Host "Docker: stopping stack and removing volumes..." -ForegroundColor Yellow
        docker compose -f (Join-Path $Root 'docker-compose.yml') down -v
    }

    Write-Host "Docker: starting Postgres, RabbitMQ, Redis..." -ForegroundColor Yellow
    docker compose -f (Join-Path $Root 'docker-compose.yml') up -d

    Write-Host "Waiting for Postgres (carehub-postgres)..." -ForegroundColor Yellow
    $deadline = (Get-Date).AddMinutes(3)
    $ready = $false
    while ((Get-Date) -lt $deadline) {
        docker exec carehub-postgres pg_isready -U carehub 2>$null | Out-Null
        if ($LASTEXITCODE -eq 0) {
            $ready = $true
            break
        }
        Start-Sleep -Seconds 2
    }
    if (-not $ready) {
        throw "Postgres did not become ready in time. Check: docker compose logs postgres"
    }
    Write-Host "Postgres is ready." -ForegroundColor Green
}
else {
    Write-Host "Skipping Docker (--SkipDocker)." -ForegroundColor Gray
}

if (-not $SkipMigrations) {
    $null = dotnet ef --version 2>$null
    if ($LASTEXITCODE -ne 0) {
        throw "dotnet-ef is not available. Install: dotnet tool install --global dotnet-ef"
    }

    $efProjects = @(
        'Services\Identity\CareHub.Identity\CareHub.Identity.csproj',
        'Services\Patient\CareHub.Patient\CareHub.Patient.csproj',
        'Services\Schedule\CareHub.Schedule\CareHub.Schedule.csproj',
        'Services\Appointment\CareHub.Appointment\CareHub.Appointment.csproj',
        'Services\Billing\CareHub.Billing\CareHub.Billing.csproj',
        'Services\Laboratory\CareHub.Laboratory\CareHub.Laboratory.csproj',
        'Services\Notification\CareHub.Notification\CareHub.Notification.csproj',
        'Services\Audit\CareHub.Audit\CareHub.Audit.csproj',
        'Services\Document\CareHub.Document\CareHub.Document.csproj',
        'Services\Reporting\CareHub.Reporting\CareHub.Reporting.csproj'
    )

    foreach ($rel in $efProjects) {
        $proj = Join-Path $Root $rel
        Write-Host "EF migrate: $rel" -ForegroundColor Yellow
        dotnet ef database update --project $proj --startup-project $proj
        if ($LASTEXITCODE -ne 0) {
            throw "dotnet ef database update failed for $rel"
        }
    }
    Write-Host "Database migrations applied." -ForegroundColor Green
}

if (-not $SkipBuild) {
    Write-Host "Building solution..." -ForegroundColor Yellow
    dotnet build (Join-Path $Root 'CareHub.sln') -c Debug --nologo -v minimal
    if ($LASTEXITCODE -ne 0) {
        throw "Build failed."
    }
    Write-Host "Build succeeded." -ForegroundColor Green
}

if (-not $Run -and -not $RunWeb) {
    Write-Host ""
    Write-Host "Done (prepare only). Start apps with:" -ForegroundColor Cyan
    Write-Host "  .\Run-CareHub.ps1 -Mode $Mode -Run -RunWeb" -ForegroundColor Gray
    Write-Host "Or run projects from Visual Studio / dotnet run (same CareHub__SeedDemoData applies to this shell)." -ForegroundColor Gray
    exit 0
}

function Wait-HttpOk {
    param(
        [string] $Url,
        [int] $TimeoutSec = 120
    )
    $sw = [System.Diagnostics.Stopwatch]::StartNew()
    while ($sw.Elapsed.TotalSeconds -lt $TimeoutSec) {
        try {
            $r = Invoke-WebRequest -Uri $Url -UseBasicParsing -TimeoutSec 5
            if ($r.StatusCode -eq 200) {
                return
            }
        }
        catch {
            Start-Sleep -Seconds 1
        }
    }
    throw "Timed out waiting for $Url"
}

function Start-CareHubDotnetJob {
    param(
        [string] $JobName,
        [string] $ProjectRelativePath,
        [string] $SeedFlag,
        [string] $RepoRoot
    )

    $projPath = Join-Path $RepoRoot $ProjectRelativePath
    Start-Job -Name $JobName -ScriptBlock {
        param($P, $Seed, $R)
        Set-Location $R
        $env:CareHub__SeedDemoData = $Seed
        dotnet run --project $P --no-launch-profile --verbosity minimal
    } -ArgumentList $projPath, $SeedFlag, $RepoRoot
}

if ($Run) {
    Write-Host ""
    Write-Host "Starting backend jobs (output: Receive-Job -Name '<name>' -Keep)..." -ForegroundColor Yellow

    Start-CareHubDotnetJob -JobName 'CareHub.Identity' -ProjectRelativePath 'Services\Identity\CareHub.Identity\CareHub.Identity.csproj' -SeedFlag $seedValue -RepoRoot $Root

    Write-Host "Waiting for Identity health..." -ForegroundColor Yellow
    Wait-HttpOk -Url 'http://localhost:5001/health'

    $parallel = @(
        @{ JobName = 'CareHub.Patient'; Path = 'Services\Patient\CareHub.Patient\CareHub.Patient.csproj' },
        @{ JobName = 'CareHub.Schedule'; Path = 'Services\Schedule\CareHub.Schedule\CareHub.Schedule.csproj' },
        @{ JobName = 'CareHub.Appointment'; Path = 'Services\Appointment\CareHub.Appointment\CareHub.Appointment.csproj' },
        @{ JobName = 'CareHub.Billing'; Path = 'Services\Billing\CareHub.Billing\CareHub.Billing.csproj' },
        @{ JobName = 'CareHub.Laboratory'; Path = 'Services\Laboratory\CareHub.Laboratory\CareHub.Laboratory.csproj' },
        @{ JobName = 'CareHub.Notification'; Path = 'Services\Notification\CareHub.Notification\CareHub.Notification.csproj' },
        @{ JobName = 'CareHub.Audit'; Path = 'Services\Audit\CareHub.Audit\CareHub.Audit.csproj' },
        @{ JobName = 'CareHub.Document'; Path = 'Services\Document\CareHub.Document\CareHub.Document.csproj' },
        @{ JobName = 'CareHub.Reporting'; Path = 'Services\Reporting\CareHub.Reporting\CareHub.Reporting.csproj' }
    )

    foreach ($p in $parallel) {
        Start-CareHubDotnetJob -JobName $p.JobName -ProjectRelativePath $p.Path -SeedFlag $seedValue -RepoRoot $Root
    }

    if ($IncludeTelegramBot) {
        Start-CareHubDotnetJob -JobName 'CareHub.TelegramBot' -ProjectRelativePath 'TelegramBot\CareHub.TelegramBot\CareHub.TelegramBot.csproj' -SeedFlag $seedValue -RepoRoot $Root
    }

    Start-CareHubDotnetJob -JobName 'CareHub.Gateway' -ProjectRelativePath 'Gateway\CareHub.Gateway\CareHub.Gateway.csproj' -SeedFlag $seedValue -RepoRoot $Root

    Write-Host "Waiting for Gateway health..." -ForegroundColor Yellow
    Wait-HttpOk -Url 'http://localhost:53615/health'

    Write-Host "Backend is up (Gateway http://localhost:53615)." -ForegroundColor Green
}

if ($RunWeb) {
    $webRoot = Join-Path $Root 'Clients\WebPortal'
    if (-not (Test-Path (Join-Path $webRoot 'node_modules'))) {
        Write-Host "npm install (WebPortal)..." -ForegroundColor Yellow
        Push-Location $webRoot
        npm install
        Pop-Location
    }

    Start-Job -Name 'CareHub.WebPortal' -ScriptBlock {
        param($W)
        Set-Location $W
        npm run dev -- --host
    } -ArgumentList $webRoot

    Write-Host "Web portal job started (usually http://localhost:5173)." -ForegroundColor Green
}

if ($RunWeb -and -not $Run) {
    Write-Host ""
    Write-Host "Note: -RunWeb without -Run only starts Vite. Start the gateway and services separately (e.g. .\Run-CareHub.ps1 -Mode $Mode -Run)." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Background jobs:" -ForegroundColor Cyan
Get-Job | Format-Table -AutoSize Id, Name, State
Write-Host "View logs: Receive-Job -Name 'CareHub.Identity' -Keep" -ForegroundColor Gray
Write-Host "Stop all:  Get-Job | Stop-Job; Get-Job | Remove-Job" -ForegroundColor Gray
