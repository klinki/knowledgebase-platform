# CI/CD Deployment Recommendations

## Recommended Deployment Model

1. Build and push immutable Docker images tagged by commit SHA in CI.
2. Deploy by SSH to the server and run one script (`deploy/scripts/deploy.sh`) with `IMAGE_TAG=<sha>`.
3. Keep runtime secrets on the server in `deploy/.env.production` (avoid logging secrets in CI).
4. Roll back by rerunning the deploy script with a previously known-good `IMAGE_TAG`.

## What Is Implemented

- Production stack definition:
  - `deploy/docker-compose.prod.yml`
- Server-side deployment script:
  - `deploy/scripts/deploy.sh`
- Production env template:
  - `deploy/.env.production.example`
- Frontend container with Caddy:
  - `frontend/Dockerfile`
  - `frontend/Caddyfile`
  - `frontend/.dockerignore`
- CI pipelines:
  - `.github/workflows/deploy.yml`
  - `bitbucket-pipelines.yml`
- Backend Docker build context optimization:
  - `backend/.dockerignore`
- Frontend production API routing adjustment:
  - `frontend/src/environments/environment.ts` (`apiUrl` uses `/api/v1/search`)

## Caddy vs Nginx Recommendation

- Caddy is a good fit for this project because it is simple to maintain, supports automatic TLS, and cleanly handles SPA static hosting plus reverse proxying `/api/*` to the backend.
- Keep Nginx only if you already depend on advanced Nginx-specific behavior or established operations tooling around it.

## Validation Performed

- `npm run build` in `frontend` completed successfully.
- `docker compose --env-file deploy/.env.production.test -f deploy/docker-compose.prod.yml config` rendered successfully during validation.

## Follow-up Setup Steps

1. On the server, copy `deploy/.env.production.example` to `deploy/.env.production` and fill real values.
2. Ensure deploy script is executable on Linux host:
   - `chmod +x deploy/scripts/deploy.sh`
3. Configure GitHub secrets:
   - `DEPLOY_SSH_HOST`, `DEPLOY_SSH_USER`, `DEPLOY_SSH_KEY`, `DEPLOY_PATH`
4. Configure Bitbucket variables:
   - `REGISTRY_USERNAME`, `REGISTRY_PASSWORD`, `DEPLOY_SSH_HOST`, `DEPLOY_SSH_USER`, `DEPLOY_PATH`
5. If only one default branch is used, simplify pipeline branch filters to either `main` or `master`.
