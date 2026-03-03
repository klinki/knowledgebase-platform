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

## Architecture Notes
- Caddy handles static SPA hosting, API reverse proxying, and HTTPS certificate automation.
- API and Worker remain separate processes, preserving queue-processing architecture.
- CI systems only publish immutable images and trigger remote rollout by image tag.

## Implementation Status
- [x] Added `deploy/docker-compose.prod.yml`.
- [x] Added `deploy/.env.production.example`.
- [x] Added `deploy/scripts/deploy.sh`.
- [x] Added `frontend/Dockerfile`.
- [x] Added `frontend/Caddyfile`.
- [x] Added `.github/workflows/deploy.yml`.
- [x] Added `bitbucket-pipelines.yml`.
- [x] Updated production frontend API URL for proxy-based routing.
- [x] Updated `README.md` deployment section.
