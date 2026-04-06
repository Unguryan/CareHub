# CareHub

CareHub is a multi-service healthcare platform: an API **gateway**, **ASP.NET Core** microservices (Identity, Patient, Schedule, Appointment, Billing, Laboratory, Notification, Audit, Document, Reporting), optional **Telegram** integration, and a **React (Vite)** web portal.

This document is the main place to learn how to run everything on your machine, what the two startup modes mean, and how the pieces fit together.

---

## Table of contents

1. [What you need installed](#what-you-need-installed)
2. [Repository layout (mental map)](#repository-layout-mental-map)
3. [The one script: `Run-CareHub.ps1`](#the-one-script-run-carehubps1)
4. [Two modes: Seed vs Clean](#two-modes-seed-vs-clean)
5. [Configuration: `CareHub:SeedDemoData`](#configuration-carehubseeddemodata)
6. [Docker dependencies](#docker-dependencies)
7. [Database migrations](#database-migrations)
8. [Ports and URLs](#ports-and-urls)
9. [Demo accounts (Seed mode only)](#demo-accounts-seed-mode-only)
10. [Web portal](#web-portal)
11. [Running without the script (manual)](#running-without-the-script-manual)
12. [Background jobs, cleanup, and stopping](#background-jobs-logs-and-stopping)
13. [Clean mode: first real users](#clean-mode-first-real-users)
14. [Troubleshooting](#troubleshooting)
15. [Running automated tests](#running-automated-tests)

---

## What you need installed

| Requirement | Why |
|-------------|-----|
| **.NET SDK** (version aligned with the repo / `global.json` if present) | Build and run all C# services |
| **Docker Desktop** (or compatible engine) | Postgres, RabbitMQ, Redis via `docker-compose.yml` |
| **Node.js 20+** and **npm** | Web portal (`Clients/WebPortal`) |
| **PowerShell 5.1+** (Windows built-in, or PowerShell 7) | `Run-CareHub.ps1` |
| **EF Core CLI** | `dotnet tool install --global dotnet-ef` — used by the script for `database update` |

Optional:

- **Git** — clone and update the repo
- **Visual Studio 2022** or **Rider** — F5 debugging (launch profiles are aligned with gateway ports for Identity, Patient, and Schedule)

---

## Repository layout (mental map)

| Path | Role |
|------|------|
| `CareHub.sln` | Main solution (services, gateway, shared libraries, tests) |
| `docker-compose.yml` | Local **Postgres** (all DBs), **RabbitMQ**, **Redis** |
| `Scripts/init-databases.sql` | Creates per-service databases on first Postgres init |
| `Run-CareHub.ps1` | **Single entry script**: Docker, migrations, build, optional run |
| `Cleanup-CareHub.ps1` | Stop CareHub jobs, optional dotnet/node on dev ports, `docker compose down` |
| `Gateway/CareHub.Gateway` | YARP reverse proxy; front door for the web app |
| `Services/*` | Domain microservices |
| `Clients/WebPortal` | React SPA (Vite); talks to the **gateway**, not directly to each service |
| `TelegramBot/CareHub.TelegramBot` | Optional bot; Identity can call it when configured |

---

## The one script: `Run-CareHub.ps1`

From the **repository root** (the folder that contains `CareHub.sln` and `docker-compose.yml`):

```powershell
# Prepare only: Docker up, apply migrations, build (no running apps)
.\Run-CareHub.ps1 -Mode Seed

# Same, but wipe Postgres data first (fresh databases)
.\Run-CareHub.ps1 -Mode Seed -RecreateVolumes

# Clean install: infrastructure only, no demo users / demo domain data
.\Run-CareHub.ps1 -Mode Clean -RecreateVolumes

# Full dev stack: backend jobs + Vite
.\Run-CareHub.ps1 -Mode Seed -Run -RunWeb
```

### Script parameters (quick reference)

| Parameter | Effect |
|-----------|--------|
| `-Mode Seed` | Demo data enabled (`CareHub__SeedDemoData=true`). Default mode. |
| `-Mode Clean` | Demo data disabled (`CareHub__SeedDemoData=false`). |
| `-RecreateVolumes` | `docker compose down -v` before `up` — **erases** Postgres volume. |
| `-Run` | Start all .NET services + gateway as **PowerShell background jobs**. |
| `-RunWeb` | `npm run dev` for the web portal (job). |
| `-IncludeTelegramBot` | Also start `CareHub.TelegramBot`. |
| `-SkipDocker` | Assume containers already running. |
| `-SkipMigrations` | Skip `dotnet ef database update`. |
| `-SkipBuild` | Skip `dotnet build`. |
| `-VerboseMigrations` | Full EF/SQL logs during `dotnet ef` (default is quiet). |

The script sets **`$env:CareHub__SeedDemoData`** in **your current PowerShell session** so any `dotnet run` you start manually in that same window inherits the same mode.

---

## Two modes: Seed vs Clean

### Seed mode (default for local exploration)

Use this when you want **ready-made users and domain data** to click through the product.

- **Identity** creates **roles**, **OpenIddict clients** (`carehub-web`, `carehub-desktop`, `carehub-services`), and **demo users** (phone + password; see [Demo accounts](#demo-accounts-seed-mode-only)).
- **Patient** inserts sample patients (only if the patients table is empty).
- **Schedule** inserts sample doctors and shifts (only if the doctors table is empty).
- Other services may run empty seed hooks today; the flag keeps behavior consistent as seeds grow.

### Clean mode

Use this when you want an application that is **wired correctly** (same Docker, same migrations, same OIDC clients) but **without** demo people or demo clinical/schedule rows.

- **Identity** still creates **roles** and **OIDC clients** — the web portal and gateway **need** those clients to exist.
- **Identity** does **not** create demo users.
- **Patient**, **Schedule**, and other gated seeds **do not** insert demo rows.

**Important:** In Clean mode there is **no** seeded password login. You will need a separate process to create the first `ApplicationUser` (for example a future admin UI, operational script, or direct database procedure). Until then, password login via the portal will not succeed.

---

## Configuration: `CareHub:SeedDemoData`

Services read a boolean setting:

- **JSON (appsettings):** `"CareHub": { "SeedDemoData": true }`
- **Environment variable (recommended for scripts and containers):** `CareHub__SeedDemoData`  
  (`__` is the nested-section delimiter in .NET configuration.)

Precedence is standard ASP.NET Core: environment variables override `appsettings.json`.

The shared helper lives in `CareHub.Shared.AspNetCore` (`CareHubConfiguration.SeedDemoDataKey`). If the value is **missing**, the default is **`true`** (safe for dev and for existing tests).

---

## Docker dependencies

```powershell
docker compose up -d
```

Services:

| Service | Host ports | Notes |
|---------|------------|--------|
| Postgres | `5432` | User `carehub`, password `carehub_dev`, DB `carehub_identity` + others from init script |
| RabbitMQ | `5672`, management UI `15672` | User/password `carehub` / `carehub_dev` |
| Redis | `6379` | Used by Identity (e.g. OTP) when configured |

Health: the script waits until `pg_isready` succeeds inside `carehub-postgres`.

---

## Full Docker mode (all microservices)

Default `docker-compose.yml` runs only infrastructure.  
To run **all APIs + gateway + web** in containers, use the compose overlay:

```powershell
# Build and run infra + full CareHub app stack
docker compose -f docker-compose.yml -f docker-compose.full.yml --profile full up -d --build

# Stop full stack
docker compose -f docker-compose.yml -f docker-compose.full.yml --profile full down
```

Notes:

- Uses `build/docker/Dockerfile.dotnet-service` for all .NET services.
- Uses `build/docker/Dockerfile.webportal` for Vite web container (`http://localhost:5173`).
- Gateway runs with `Gateway/CareHub.Gateway/appsettings.Docker.json` so reverse-proxy destinations point to container service names.
- Seed mode in containers is controlled by env var `CAREHUB_SEED_DEMO_DATA` (default `true`).

Example (clean mode in full Docker):

```powershell
$env:CAREHUB_SEED_DEMO_DATA = "false"
docker compose -f docker-compose.yml -f docker-compose.full.yml --profile full up -d --build
```

---

## Database migrations

The script runs, in order:

```text
dotnet ef database update --project <project> --startup-project <project>
```

for each service that owns an EF Core context (Identity, Patient, Schedule, Appointment, Billing, Laboratory, Notification, Audit, Document, Reporting).

By default the script uses **quiet** logging for `dotnet ef` (fewer lines, no EF `dbug:` spam). Pass **`-VerboseMigrations`** if you want the full SQL and EF Core trace.

**If you run `dotnet ef` manually** and see `Failed executing DbCommand` for `SELECT … FROM "__EFMigrationsHistory"` on a **brand‑new** database, that is **normal**: the history table does not exist yet on the first check; EF then creates it and applies migrations. Your run succeeded if you see `Done.` and the script continues.

If you prefer to run migrations yourself, use the same commands (from repo root) or your IDE’s EF tools.

---

## Ports and URLs

These match `Gateway/CareHub.Gateway/appsettings.json` and each service’s `Urls` (or launch profile).

| Component | Base URL |
|-----------|----------|
| **Gateway** | `http://localhost:53615` |
| Identity | `http://localhost:5001` |
| Patient | `http://localhost:5002` |
| Appointment | `http://localhost:5003` |
| Schedule | `http://localhost:5004` |
| Billing | `http://localhost:5005` |
| Laboratory | `http://localhost:5006` |
| Notification | `http://localhost:5007` |
| Audit | `http://localhost:5008` |
| Document | `http://localhost:5009` |
| Reporting | `http://localhost:5010` |
| Telegram bot (optional) | `http://localhost:5011` |
| **Web portal (Vite)** | `http://localhost:5173` (default Vite) |

Health checks (anonymous where configured):

- Gateway: `GET http://localhost:53615/health`
- Identity: `GET http://localhost:5001/health`

The web app expects the gateway at **`VITE_GATEWAY_URL`** (see `Clients/WebPortal/.env.development` — default `http://localhost:53615`).

---

## Demo accounts (Seed mode only)

Sign-in uses **phone number** as the username (resource owner password flow via OpenIddict).

| Label | Phone | Password | Role |
|-------|-------|----------|------|
| **admin1** | `+380000000000` | `Admin1234!` | Admin |
| **doctor1** | `+380000000001` | `Doctor1234!` | Doctor |
| **user1** (receptionist) | `+380000000002` | `User1234!` | Receptionist |
| **manager1** | `+380000000003` | `Manager1234!` | Manager |
| **lab1** | `+380000000004` | `Lab1234!` | LabTechnician |

Web OIDC client (from seed): **`carehub-web`** / secret **`web-secret`** (see `Clients/WebPortal/.env.development`).

Desktop / service clients (for tools such as Postman) are also seeded: **`carehub-desktop`** / `desktop-secret`, **`carehub-services`** / `services-secret`.

**None of these credentials are suitable for production.**

---

## Web portal

```powershell
cd Clients\WebPortal
npm install
npm run dev
```

Environment files:

- `.env.example` — template
- `.env.development` — local defaults (`VITE_GATEWAY_URL`, OIDC client id/secret)

The login screen lists a subset of demo accounts; this README is the full list.

---

## Running without the script (manual)

1. Start Docker: `docker compose up -d`
2. Apply EF migrations (per project, as in the script) or rely on your own process
3. Set mode for your shell:

   ```powershell
   $env:CareHub__SeedDemoData = 'true'   # or 'false'
   ```

4. Start services (each in its own terminal or IDE multi-start), typically **Identity first**, then others, then **Gateway**
5. Start the web portal with `npm run dev` in `Clients/WebPortal`

---

## Background jobs: logs and stopping

With `-Run` / `-RunWeb`, processes are **PowerShell jobs** in your session.

```powershell
# List jobs
Get-Job

# Stream logs from one job (example)
Receive-Job -Name 'CareHub.Identity' -Keep

# Stop everything
Get-Job | Stop-Job
Get-Job | Remove-Job
```

### Full local teardown: `Cleanup-CareHub.ps1`

From the repo root, one script can stop **CareHub\*** background jobs in the current PowerShell session, stop **dotnet** and **node** processes that are **listening** on CareHub dev ports (5001–5011, 53615, 5173), and run **`docker compose down`** for this repository’s stack.

```powershell
# Default: jobs + port listeners (dotnet/node only) + docker compose down
.\Cleanup-CareHub.ps1

# Also remove Docker volumes (wipes Postgres data from the compose volume)
.\Cleanup-CareHub.ps1 -RemoveDockerVolumes

# Only stop jobs; leave Docker and other terminals alone
.\Cleanup-CareHub.ps1 -SkipDocker -SkipKillListeners
```

| Switch | Effect |
|--------|--------|
| `-RemoveDockerVolumes` | `docker compose down -v` |
| `-SkipDocker` | Do not run Docker commands |
| `-SkipJobs` | Do not stop CareHub* jobs |
| `-SkipKillListeners` | Do not kill processes by port (use if another app shares 5173, etc.) |

`Get-Help .\Cleanup-CareHub.ps1 -Full` has more detail. Port cleanup is **scoped** to process names `dotnet` and `node` so unrelated services on those ports are not stopped.

If you prefer visible consoles for each service, skip `-Run` and start each `dotnet run` in its own terminal (after setting `CareHub__SeedDemoData` the same way in each, or using matching `appsettings`). For those processes, `Cleanup-CareHub.ps1` still helps via **listener** cleanup if they use the standard ports.

---

## Clean mode: first real users

After `.\Run-CareHub.ps1 -Mode Clean` (and migrations), you have:

- Database schemas
- Roles and OIDC clients
- **No** demo users and **no** Patient/Schedule demo inserts

Creating the first administrative user is **not** exposed as a public self-service flow in this README; plan on one of:

- A controlled internal provisioning path your team adds (recommended long-term)
- Database-level bootstrap performed by someone with DBA access (understand Identity / ASP.NET Identity schema before doing this)

For **local experimentation**, switching back to **Seed mode** is the fastest way to obtain a known admin login.

---

## Troubleshooting

| Symptom | Things to check |
|---------|------------------|
| `open //./pipe/dockerDesktopLinuxEngine: The system cannot find the file specified` (or `Cannot connect to the Docker engine`) | **Start Docker Desktop** and wait until it is fully running; run `docker info` — it must succeed before `Run-CareHub.ps1`. Or use `-SkipDocker` if Postgres/RabbitMQ/Redis are already up. |
| `dotnet ef` errors | Install: `dotnet tool install --global dotnet-ef` |
| Postgres connection errors | `docker compose ps`, `docker compose logs postgres`, confirm port `5432` not used by another Postgres |
| Gateway 502 / timeouts | Ensure target service is running on the port in `Gateway/.../appsettings.json` |
| Cannot log in (Seed) | Confirm `CareHub__SeedDemoData` was `true` when **Identity** first started after a volume wipe; wrong mode on first boot can leave you without users |
| Cannot log in (Clean) | Expected until you provision users |
| CORS errors in browser | Gateway `Cors:AllowedOrigins` must include your Vite origin (`http://localhost:5173`) |
| RabbitMQ / Redis errors | Containers up; credentials in appsettings match `docker-compose.yml` |

---

## Running automated tests

From the repo root:

```powershell
dotnet test CareHub.sln -c Debug --no-build
# or with build:
dotnet test CareHub.sln -c Debug
```

Tests use isolated configuration (e.g. Identity **Testing** environment with SQLite); they do not require Docker for the Identity unit/integration tests.

---

## Further reading

- `docker-compose.yml` — container definitions and health checks
- `Clients/README.md` — client-focused notes
- `.spec/` — design and phase documents

If something in this README drifts from the code (ports, client ids, new services), prefer the **source** (`appsettings.json`, `Run-CareHub.ps1`, `Program.cs` seed blocks) and update the docs in the same change.
