#!/usr/bin/env bash
set -euo pipefail

# Server-side deployment script.
# Run this on the deployment host where Docker and the repo are already present.
# It reads production env vars, logs into the registry, pulls IMAGE_TAG images,
# and updates the running compose stack.

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DEPLOY_DIR="$(cd "${SCRIPT_DIR}/.." && pwd)"
COMPOSE_FILE="${COMPOSE_FILE:-${DEPLOY_DIR}/docker-compose.prod.yml}"
LITELLM_COMPOSE_FILE="${LITELLM_COMPOSE_FILE:-${DEPLOY_DIR}/docker-compose.litellm.yml}"
ENV_FILE="${ENV_FILE:-${DEPLOY_DIR}/.env.production}"
LITELLM_ENV_FILE="${LITELLM_ENV_FILE:-${DEPLOY_DIR}/.env.litellm}"
IMAGE_TAG="${IMAGE_TAG:-latest}"

if [[ ! -f "${ENV_FILE}" ]]; then
  echo "Missing env file: ${ENV_FILE}"
  exit 1
fi

if [[ ! -f "${LITELLM_ENV_FILE}" ]]; then
  echo "Missing LiteLLM env file: ${LITELLM_ENV_FILE}"
  exit 1
fi

set -a
source "${ENV_FILE}"
source "${LITELLM_ENV_FILE}"
set +a

# Required runtime settings.
production_required_vars=(
  REGISTRY
  REGISTRY_USERNAME
  REGISTRY_PASSWORD
  IMAGE_NAMESPACE
  SENTINEL_DOMAIN
  POSTGRES_PASSWORD
  AUTHENTICATION_JWT_SIGNING_KEY
  OPENAI_API_KEY
)

litellm_required_vars=(
  LITELLM_VERTEX_PROJECT
  LITELLM_GOOGLE_ACCOUNT_FILE
)

for var_name in "${production_required_vars[@]}"; do
  if [[ -z "${!var_name:-}" ]]; then
    echo "Missing required variable in env: ${var_name}"
    exit 1
  fi
done

for var_name in "${litellm_required_vars[@]}"; do
  if [[ -z "${!var_name:-}" ]]; then
    echo "Missing required LiteLLM variable in env: ${var_name}"
    exit 1
  fi
done

if [[ ! -f "${LITELLM_GOOGLE_ACCOUNT_FILE}" ]]; then
  echo "Missing LiteLLM Google account file: ${LITELLM_GOOGLE_ACCOUNT_FILE}"
  exit 1
fi

# Auth + rollout using immutable image tag.
echo "${REGISTRY_PASSWORD}" | docker login "${REGISTRY}" -u "${REGISTRY_USERNAME}" --password-stdin

# Shared reverse-proxy network for multi-app hosts.
if ! docker network inspect shared-proxy >/dev/null 2>&1; then
  docker network create shared-proxy >/dev/null
fi

litellm_network="${LITELLM_DOCKER_NETWORK:-sentinel-ai}"
if ! docker network inspect "${litellm_network}" >/dev/null 2>&1; then
  docker network create "${litellm_network}" >/dev/null
fi

echo "Pulling images for tag ${IMAGE_TAG}..."
IMAGE_TAG="${IMAGE_TAG}" docker compose --env-file "${ENV_FILE}" -f "${COMPOSE_FILE}" pull
docker compose --env-file "${LITELLM_ENV_FILE}" -f "${LITELLM_COMPOSE_FILE}" pull

echo "Starting PostgreSQL..."
IMAGE_TAG="${IMAGE_TAG}" docker compose --env-file "${ENV_FILE}" -f "${COMPOSE_FILE}" up -d postgres

echo "Running database migrations..."
IMAGE_TAG="${IMAGE_TAG}" docker compose --env-file "${ENV_FILE}" -f "${COMPOSE_FILE}" up --no-deps migrator

echo "Starting updated stack..."
# Do not use --remove-orphans here: auxiliary compose stacks such as LiteLLM
# may share the host and must not be deleted during a Sentinel application rollout.
IMAGE_TAG="${IMAGE_TAG}" docker compose --env-file "${ENV_FILE}" -f "${COMPOSE_FILE}" up -d api worker web
docker compose --env-file "${LITELLM_ENV_FILE}" -f "${LITELLM_COMPOSE_FILE}" up -d litellm

echo "Deployment finished for tag ${IMAGE_TAG}."
