# Feature: CI/CD Deployment Infrastructure (09-ci-cd-deployment)

## Goal

Provide a production-ready deployment pipeline using Docker images, with support for both GitHub Actions and Bitbucket Pipelines, and Caddy as the web edge.

## Acceptance Criteria

- [x] Production Docker Compose stack includes API, Worker, PostgreSQL (pgvector), and web edge.
- [x] Frontend container serves Angular static assets and reverse proxies API traffic.
- [x] Server-side deployment script supports commit-tag rollouts.
- [x] GitHub Actions pipeline builds, pushes, and deploys containers.
- [x] Bitbucket Pipelines config builds, pushes, and deploys containers.
- [x] Deployment environment variable template is documented.
- [x] README includes deployment usage notes.
- [x] Local Linux/WSL remote deployment helper is available.
- [x] Formal release process is automated with Release Please.
- [x] Changelog generation is automated from Conventional Commits.

## Architecture Notes

- Caddy handles static SPA hosting, API reverse proxying, and HTTPS certificate automation.
- API and Worker remain separate processes, preserving queue-processing architecture.
- CI systems only publish immutable images and trigger remote rollout by image tag.

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
- [x] Added `bitbucket-pipelines.yml`.
- [x] Added `deploy/scripts/remote-deploy.sh`.
- [x] Added `deploy/.env.remote.example`.
- [x] Added `--dry-run` mode to `deploy/scripts/remote-deploy.sh`.
- [x] Added `.github/workflows/release-please.yml`.
- [x] Added `release-please-config.json`.
- [x] Added `release-please-manifest.json`.
- [x] Updated deploy workflow to trigger on `v*` release tags and keep manual dispatch.
- [x] Updated deploy workflow to upload deploy artifacts and remove remote git pull/checkout.
- [x] Updated production frontend API URL for proxy-based routing.
- [x] Updated `README.md` deployment section.
