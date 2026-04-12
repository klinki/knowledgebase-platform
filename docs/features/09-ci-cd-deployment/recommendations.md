# CI/CD Deployment Recommendations

## Recommended Deployment Model

1. Use Release Please to manage versioning and release PRs from Conventional Commits.
2. Build and push immutable Docker images tagged by release tag (`vX.Y.Z`) in CI.
3. Deploy by SSH to the server and run one script (`deploy/scripts/deploy.sh`) with `IMAGE_TAG=<release-tag>`.
4. Keep runtime secrets on the server in `deploy/.env.production` (avoid logging secrets in CI).
5. Roll back by rerunning the deploy script with a previously known-good release tag.

## What Is Implemented

- Production stack definition:
  - `deploy/docker-compose.prod.yml`
- Shared autodiscovery Caddy bootstrap for multi-app hosts:
  - `deploy/docker-compose.proxy.yml`
  - `deploy/.env.proxy.example`
- Server-side deployment script:
  - `deploy/scripts/deploy.sh`
- EF migration bundle image:
  - `backend/Dockerfile.migrator`
- Production env template:
  - `deploy/.env.production.example`
- Frontend container with Caddy:
  - `frontend/Dockerfile`
  - `frontend/Caddyfile`
  - `frontend/.dockerignore`
- CI pipelines:
  - `.github/workflows/deploy.yml`
  - `.github/workflows/release-please.yml`
  - `bitbucket-pipelines.yml`
- Release Please config:
  - `release-please-config.json`
  - `release-please-manifest.json`
- Backend Docker build context optimization:
  - `backend/.dockerignore`
- Frontend production API routing adjustment:
  - `frontend/src/environments/environment.ts` (`apiUrl` uses `/api/v1/search`)

## Caddy vs Nginx Recommendation

- Caddy is a good fit for this project because it is simple to maintain, supports automatic TLS, and cleanly routes multiple apps from one shared host using Docker label autodiscovery.
- Keep Nginx only if you already depend on advanced Nginx-specific behavior or established operations tooling around it.

## Validation Performed

- `npm run build` in `frontend` completed successfully.
- `docker compose --env-file deploy/.env.production.example -f deploy/docker-compose.prod.yml config` renders successfully.
- `docker compose --env-file deploy/.env.proxy.example -f deploy/docker-compose.proxy.yml config` renders successfully.

## Current Rollout Sequence

Production rollout now happens in this order:

1. Pull the requested image tag.
2. Start PostgreSQL.
3. Run the one-shot `migrator` service.
4. Start `api`, `worker`, and `web`.

The `migrator` image is an EF Core migration bundle built from [backend/src/SentinelKnowledgebase.Migrations](/backend/src/SentinelKnowledgebase.Migrations/). It is not a separate migrations source tree.

## Follow-up Setup Steps

1. On the server, copy `deploy/.env.production.example` to `deploy/.env.production` and fill real values.
2. Bootstrap the shared Caddy stack once with `deploy/docker-compose.proxy.yml` and `deploy/.env.proxy`.
3. Ensure deploy script is executable on Linux host:
   - `chmod +x deploy/scripts/deploy.sh`
4. Configure GitHub secrets:
   - `DEPLOY_SSH_HOST`, `DEPLOY_SSH_USER`, `DEPLOY_SSH_KEY`, `DEPLOY_PATH`
   - optional: `DEPLOY_SSH_PASSPHRASE` (if SSH key is encrypted)
5. Configure Bitbucket variables:
   - `REGISTRY_USERNAME`, `REGISTRY_PASSWORD`, `DEPLOY_SSH_HOST`, `DEPLOY_SSH_USER`, `DEPLOY_PATH`
6. If only one default branch is used, simplify pipeline branch filters to either `main` or `master`.

## Local Manual Deploy (Linux/WSL)

Use `deploy/scripts/remote-deploy.sh` to run the same remote deployment flow locally:

1. Copy `deploy/.env.remote.example` to `deploy/.env.remote`.
2. Fill SSH connection details (`DEPLOY_SSH_HOST`, `DEPLOY_SSH_USER`, `DEPLOY_PATH`, key file, branch).
3. Verify remote connectivity and prerequisites:
   - `./deploy/scripts/remote-deploy.sh --config deploy/.env.remote --verify-only`
4. Deploy chosen image tag:
   - `./deploy/scripts/remote-deploy.sh --config deploy/.env.remote --image-tag <commit-sha>`
5. Optional dry run (no deploy):
   - `./deploy/scripts/remote-deploy.sh --config deploy/.env.remote --image-tag <commit-sha> --dry-run`

This uses the same server-side deploy script as CI, but not the same transport:
- `git fetch --all --prune`
- `git checkout <branch>`
- `git pull --ff-only`
- `IMAGE_TAG=<tag> ./deploy/scripts/deploy.sh`

## Release Flow

1. Merge Conventional Commit PRs into the default branch.
2. Release Please updates or opens a release PR with version bump and changelog entries.
3. Merge release PR to create `v*` tag and GitHub release.
4. Deploy workflow runs on `v*` tag pushes, uploads deploy artifacts (`deploy.sh` and compose file) to the server, and deploys that immutable release image tag.
5. `workflow_dispatch` remains available for manual deployments.
6. During deployment, CI builds and publishes `sentinel-api`, `sentinel-worker`, `sentinel-migrator`, `sentinel-servercli`, and `sentinel-web`.

