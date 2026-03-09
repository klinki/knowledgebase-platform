# 2026-03-02 Infra Startup Update

## Summary
This update changes local development startup to run only infrastructure in Docker by default, while running API and Worker with local `dotnet watch`.

## Why This Change Was Needed
The previous setup started API in Docker and API/Worker locally at the same time, which caused two recurring issues:

1. Port collisions:
API in Docker and API on host both tried to bind the same ports.

2. Local DB connectivity mismatch:
Local `dotnet watch` used `Host=postgres` (Docker service DNS), which is not reachable from host processes.

## Changes Implemented
1. Docker Compose profiles:
- `api` and `worker` were moved behind profile `app`.
- `docker compose up -d` now starts infra only (`postgres`).
- Full containerized backend is available with `docker compose --profile app up -d`.
- An optional shared proxy stack is available separately via `deploy/docker-compose.proxy.yml`.

2. Development configuration alignment:
- API `appsettings.Development.json` now uses:
  - `ConnectionStrings:DefaultConnection = Host=localhost;...`
  - `Serilog:WriteTo:Seq:serverUrl = http://localhost:5341`
- Worker `appsettings.Development.json` now uses the same localhost-based values.

3. Docs update:
- README now explicitly documents:
  - infra-only default compose startup
  - local API + Worker watch processes
  - full Docker backend command with `--profile app`

## Operational Outcome
The local dev flow is now deterministic:

1. Start infra with Docker.
2. Run API locally with `dotnet watch`.
3. Run Worker locally with `dotnet watch`.

This removes port conflicts and guarantees host processes can connect to PostgreSQL and Seq.
If needed, the shared Caddy proxy can be started separately for parity with production and for other containerized services on the machine.

## Verification Notes
- `docker compose config --services` returns only `postgres`.
- `docker compose --profile app config --services` returns `postgres`, `api`, `worker`.
- Updated Development JSON configs parse successfully.
