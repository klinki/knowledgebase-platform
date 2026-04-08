# Feature: CI/CD Deployment Infrastructure (09-ci-cd-deployment)

## Goal

Provide a production-ready deployment pipeline using Docker images, with support for both GitHub Actions and Bitbucket Pipelines, and Caddy as the web edge.

## Acceptance Criteria

- [x] Production Docker Compose stack includes API, Worker, PostgreSQL (pgvector), and the web app, with the shared Caddy edge bootstrapped separately.
- [x] Production deployment runs database migrations as a one-shot container before starting API and Worker.
- [x] Frontend container serves Angular static assets, and shared Caddy routes API traffic to the backend.
- [x] Server-side deployment script supports commit-tag rollouts.
- [x] GitHub Actions pipeline builds, pushes, and deploys containers.
- [x] Bitbucket Pipelines config builds, pushes, and deploys containers.
- [x] Deployment environment variable template is documented.
- [x] README includes deployment usage notes.
- [x] Local Linux/WSL remote deployment helper is available.
- [x] Formal release process is automated with Release Please.
- [x] Changelog generation is automated from Conventional Commits.
- [x] Production stack supports multi-app deployment host via shared reverse proxy.

## Architecture Notes

- Shared autodiscovery Caddy handles domain routing and HTTPS certificate automation.
- API and Worker remain separate processes, preserving queue-processing architecture.
- CI systems only publish immutable images and trigger remote rollout by image tag.
- Database schema rollout is handled by an EF Core migration bundle container built from the existing migrations project.

## GitHub Secrets Setup

Required repository secrets for `.github/workflows/deploy.yml`:

- `DEPLOY_SSH_HOST`: deployment server host or IP.
- `DEPLOY_SSH_USER`: SSH user used for deployment.
- `DEPLOY_SSH_KEY`: private key content for SSH auth.
- `DEPLOY_PATH`: absolute path on remote host where deploy assets/scripts are executed.

Optional repository secret:

- `DEPLOY_SSH_PASSPHRASE`: passphrase for encrypted `DEPLOY_SSH_KEY`.

Notes:

- `GITHUB_TOKEN` is provided automatically by GitHub Actions and is used for GHCR push and Release Please.
- If your SSH key is not encrypted, `DEPLOY_SSH_PASSPHRASE` can be omitted.

## Implementation Status

- [x] Added `deploy/docker-compose.prod.yml`.
- [x] Added `deploy/.env.production.example`.
- [x] Added `deploy/scripts/deploy.sh`.
- [x] Added `frontend/Dockerfile`.
- [x] Added `frontend/Caddyfile`.
- [x] Added `.github/workflows/deploy.yml`.
- [x] Added `backend/Dockerfile.migrator`.
- [x] Added `bitbucket-pipelines.yml`.
- [x] Added `deploy/scripts/remote-deploy.sh`.
- [x] Added `deploy/.env.remote.example`.
- [x] Added `--dry-run` mode to `deploy/scripts/remote-deploy.sh`.
- [x] Added `.github/workflows/release-please.yml`.
- [x] Added `release-please-config.json`.
- [x] Added `release-please-manifest.json`.
- [x] Updated deploy workflow to trigger on `v*` release tags and keep manual dispatch.
- [x] Updated deploy workflow to upload deploy artifacts and remove remote git pull/checkout.
- [x] Updated deploy workflow and compose stack to run a one-shot migration bundle before starting application services.
- [x] Added `deploy/docker-compose.proxy.yml` for a shared autodiscovery Caddy host.
- [x] Added `deploy/.env.proxy.example` for shared Caddy bootstrap.
- [x] Added `deploy/docker-compose.vertex-proxy.yml` and `deploy/.env.vertex-proxy.example` for optional Vertex AI proxy bootstrap.
- [x] Added `deploy/docker-compose.litellm.yml`, `deploy/.env.litellm.example`, and `deploy/litellm.vertex.yaml` for optional LiteLLM Vertex AI proxy bootstrap.
- [x] Updated app compose to attach `api` and `web` services to external `shared-proxy` network.
- [x] Updated production app routing to use Docker labels for shared Caddy autodiscovery.
- [x] Added production env overrides for custom OpenAI-compatible URLs and model names.
- [x] Added ADR documenting the shared Caddy deployment edge and routing model.
- [x] Added detailed Vertex proxy setup guide with Google Cloud and Sentinel configuration steps.
- [x] Added LiteLLM Vertex proxy setup guide with Sentinel routing and rollout notes.
- [x] Updated production frontend API URL for proxy-based routing.
- [x] Added `backend/Dockerfile.servercli` and a one-off `servercli` production compose service for server-side admin operations.
- [x] Updated the deploy workflow to build and publish the `sentinel-servercli` image.
- [x] Updated `README.md` deployment section.

