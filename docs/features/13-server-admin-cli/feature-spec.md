# Feature: Server Admin CLI (13-server-admin-cli)

## Goal

Add a Docker-first server administration CLI for direct Sentinel user management without relying on bootstrap-admin environment seeding or new HTTP admin endpoints.

## Acceptance Criteria

- [x] `users list`, `users add`, `users delete`, `users change-password`, `help`, and `version` commands exist.
- [x] The CLI uses direct EF Core and ASP.NET Core Identity access instead of calling HTTP APIs.
- [x] The CLI is packaged for Docker execution in production.
- [x] `users delete` refuses deletion when the target user owns data or is referenced by invitations or approved device authorizations.
- [x] `users change-password` revokes refresh tokens and updates the user security stamp.
- [x] The CLI is built and published in GitHub Actions as `sentinel-servercli`.

## Architecture Notes

- `SentinelKnowledgebase.ServerCLI` is a `net10.0` console application.
- Command parsing uses `System.CommandLine`.
- Startup uses `Host.CreateApplicationBuilder(args)` and reuses `AddInfrastructure(builder.Configuration)`.
- No new database schema, auth model, or HTTP API is introduced.
- Production execution path is `docker compose run --rm servercli ...` against the existing deployment stack.

## Important Interfaces and Behavior

### CLI commands

- `servercli users list [--role <admin|member>]`
- `servercli users add <email> [--display-name <name>] [--role <member|admin>] [--password <value>]`
- `servercli users delete <email>`
- `servercli users change-password <email> [--password <value>]`
- `servercli help [command]`
- `servercli version`

### Runtime artifacts

- Docker image: `sentinel-servercli`
- Dockerfile: `backend/Dockerfile.servercli`
- Production compose service: `servercli`

## Implementation Status

- [x] Add the `SentinelKnowledgebase.ServerCLI` project and solution references.
- [x] Add `System.CommandLine` command wiring and secure password prompting.
- [x] Add a CLI-local `UserAdminService` that reuses Identity and `ApplicationDbContext`.
- [x] Add Docker packaging for the CLI with an ASP.NET runtime image.
- [x] Add a production compose service for one-off CLI runs.
- [x] Add GitHub Actions image build and push support for `sentinel-servercli`.
- [x] Add unit coverage for command binding and prompt-driven flows.
- [x] Add integration coverage for add, list, delete, and change-password behaviors.
- [x] Document production configuration and example CLI usage.

## Verification Plan

- [x] `dotnet build` succeeds for `SentinelKnowledgebase.ServerCLI`.
- [x] Unit tests cover command binding defaults and prompt validation.
- [x] Integration tests cover add member, add admin, duplicate email rejection, list results, password change, blocked deletion, and successful deletion.
- [x] Docker image build succeeds for `backend/Dockerfile.servercli`.
- [x] Production docs describe `docker compose run --rm servercli ...` usage.

## Assumptions and Defaults

- Docker is the primary execution path in production.
- Email is the only operator-facing user identifier in v1.
- `users add` creates active accounts immediately.
- Default role is `member`.
- Passwords are prompted securely when omitted from mutating commands.
- Human-readable console output is sufficient for v1; JSON output is out of scope.
- Existing bootstrap-admin seeding remains available but is no longer the only viable first-user path.
