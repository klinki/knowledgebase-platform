#!/usr/bin/env bash
set -euo pipefail

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

required_vars=(
  REGISTRY
  REGISTRY_USERNAME
  REGISTRY_PASSWORD
  IMAGE_NAMESPACE
  POSTGRES_PASSWORD
  OPENAI_API_KEY
  CADDY_SITE
)

for var_name in "${required_vars[@]}"; do
  if [[ -z "${!var_name:-}" ]]; then
    echo "Missing required variable in env: ${var_name}"
    exit 1
  fi
done

echo "${REGISTRY_PASSWORD}" | docker login "${REGISTRY}" -u "${REGISTRY_USERNAME}" --password-stdin

echo "Pulling images for tag ${IMAGE_TAG}..."
IMAGE_TAG="${IMAGE_TAG}" docker compose --env-file "${ENV_FILE}" -f "${COMPOSE_FILE}" pull

echo "Starting updated stack..."
IMAGE_TAG="${IMAGE_TAG}" docker compose --env-file "${ENV_FILE}" -f "${COMPOSE_FILE}" up -d --remove-orphans

echo "Deployment finished for tag ${IMAGE_TAG}."
