#!/usr/bin/env bash
set -euo pipefail

# Server-side deployment script.
# Run this on the deployment host where Docker and the repo are already present.
# It reads production env vars, logs into the registry, pulls IMAGE_TAG images,
# and updates the running compose stack.

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DEPLOY_DIR="$(cd "${SCRIPT_DIR}/.." && pwd)"
COMPOSE_FILE="${COMPOSE_FILE:-${DEPLOY_DIR}/docker-compose.prod.yml}"
ENV_FILE="${ENV_FILE:-${DEPLOY_DIR}/.env.production}"
IMAGE_TAG="${IMAGE_TAG:-latest}"

if [[ ! -f "${ENV_FILE}" ]]; then
  echo "Missing env file: ${ENV_FILE}"
  exit 1
fi

set -a
source "${ENV_FILE}"
set +a

# Required runtime settings (must exist in .env.production).
required_vars=(
  REGISTRY
  REGISTRY_USERNAME
  REGISTRY_PASSWORD
  IMAGE_NAMESPACE
  SENTINEL_DOMAIN
  POSTGRES_PASSWORD
  AUTHENTICATION_JWT_SIGNING_KEY
  OPENAI_API_KEY
)

for var_name in "${required_vars[@]}"; do
  if [[ -z "${!var_name:-}" ]]; then
    echo "Missing required variable in env: ${var_name}"
    exit 1
  fi
done

# Auth + rollout using immutable image tag.
echo "${REGISTRY_PASSWORD}" | docker login "${REGISTRY}" -u "${REGISTRY_USERNAME}" --password-stdin

# Shared reverse-proxy network for multi-app hosts.
if ! docker network inspect shared-proxy >/dev/null 2>&1; then
  docker network create shared-proxy >/dev/null
fi

echo "Pulling images for tag ${IMAGE_TAG}..."
IMAGE_TAG="${IMAGE_TAG}" docker compose --env-file "${ENV_FILE}" -f "${COMPOSE_FILE}" pull

echo "Starting PostgreSQL..."
IMAGE_TAG="${IMAGE_TAG}" docker compose --env-file "${ENV_FILE}" -f "${COMPOSE_FILE}" up -d postgres

echo "Running database migrations..."
IMAGE_TAG="${IMAGE_TAG}" docker compose --env-file "${ENV_FILE}" -f "${COMPOSE_FILE}" up --no-deps migrator

echo "Starting updated stack..."
# Do not use --remove-orphans here: auxiliary compose stacks such as LiteLLM
# may share the host and must not be deleted during a Sentinel application rollout.
IMAGE_TAG="${IMAGE_TAG}" docker compose --env-file "${ENV_FILE}" -f "${COMPOSE_FILE}" up -d api worker web

echo "Deployment finished for tag ${IMAGE_TAG}."
