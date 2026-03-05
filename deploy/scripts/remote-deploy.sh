#!/usr/bin/env bash
set -euo pipefail

# Local machine deployment wrapper (Linux/WSL).
# Run this from your workstation to connect to the remote host over SSH.
# It validates access/prerequisites, updates remote git state, and then calls
# remote deploy/scripts/deploy.sh with IMAGE_TAG.

usage() {
  cat <<'EOF'
Usage:
  ./deploy/scripts/remote-deploy.sh [options]

Options:
  --host <host>             Remote SSH host (or use DEPLOY_SSH_HOST).
  --user <user>             Remote SSH user (or use DEPLOY_SSH_USER).
  --path <path>             Remote repo path (or use DEPLOY_PATH).
  --branch <branch>         Remote git branch to deploy (default: main).
  --image-tag <tag>         Image tag passed to deploy script (default: latest).
  --key-file <file>         SSH private key path (default: ~/.ssh/id_ed25519).
  --port <port>             SSH port (default: 22).
  --remote-env-file <file>  Remote env file path (default: deploy/.env.production).
  --verify-only             Only verify SSH/prerequisites, do not deploy.
  --dry-run                 Print remote deploy commands and exit.
  --config <file>           Optional local env file to source before parsing args.
  -h, --help                Show this help.

Examples:
  ./deploy/scripts/remote-deploy.sh --host 203.0.113.10 --user deploy --path /opt/sentinel --branch main --image-tag abcd123
  ./deploy/scripts/remote-deploy.sh --config deploy/.env.remote --image-tag abcd123
  ./deploy/scripts/remote-deploy.sh --config deploy/.env.remote --verify-only
  ./deploy/scripts/remote-deploy.sh --config deploy/.env.remote --image-tag abcd123 --dry-run
EOF
}

host="${DEPLOY_SSH_HOST:-}"
user="${DEPLOY_SSH_USER:-}"
deploy_path="${DEPLOY_PATH:-}"
branch="${DEPLOY_BRANCH:-main}"
image_tag="${IMAGE_TAG:-latest}"
key_file="${DEPLOY_SSH_KEY_FILE:-$HOME/.ssh/id_ed25519}"
port="${DEPLOY_SSH_PORT:-22}"
remote_env_file="${REMOTE_ENV_FILE:-deploy/.env.production}"
verify_only="false"
dry_run=""
config_file=""

while [[ $# -gt 0 ]]; do
  case "$1" in
    --host) host="$2"; shift 2 ;;
    --user) user="$2"; shift 2 ;;
    --path) deploy_path="$2"; shift 2 ;;
    --branch) branch="$2"; shift 2 ;;
    --image-tag) image_tag="$2"; shift 2 ;;
    --key-file) key_file="$2"; shift 2 ;;
    --port) port="$2"; shift 2 ;;
    --remote-env-file) remote_env_file="$2"; shift 2 ;;
    --verify-only) verify_only="true"; shift ;;
    --dry-run) dry_run="true"; shift ;;
    --config) config_file="$2"; shift 2 ;;
    -h|--help) usage; exit 0 ;;
    *)
      echo "Unknown option: $1"
      usage
      exit 1
      ;;
  esac
done

if [[ -n "$config_file" ]]; then
  if [[ ! -f "$config_file" ]]; then
    echo "Config file not found: $config_file"
    exit 1
  fi
  set -a
  # shellcheck disable=SC1090
  source "$config_file"
  set +a
  host="${host:-${DEPLOY_SSH_HOST:-}}"
  user="${user:-${DEPLOY_SSH_USER:-}}"
  deploy_path="${deploy_path:-${DEPLOY_PATH:-}}"
  branch="${branch:-${DEPLOY_BRANCH:-main}}"
  image_tag="${image_tag:-${IMAGE_TAG:-latest}}"
  key_file="${key_file:-${DEPLOY_SSH_KEY_FILE:-$HOME/.ssh/id_ed25519}}"
  port="${port:-${DEPLOY_SSH_PORT:-22}}"
  remote_env_file="${remote_env_file:-${REMOTE_ENV_FILE:-deploy/.env.production}}"
fi

dry_run="${dry_run:-${DRY_RUN:-false}}"

if [[ -z "$host" || -z "$user" || -z "$deploy_path" ]]; then
  echo "Missing required parameters: --host, --user, --path"
  usage
  exit 1
fi

if [[ ! -f "$key_file" ]]; then
  echo "SSH key file not found: $key_file"
  exit 1
fi

# This script only orchestrates over SSH; actual container rollout is remote.
echo "Connecting to ${user}@${host}:${port}"
echo "Remote path: ${deploy_path}"
echo "Branch: ${branch}"
echo "Image tag: ${image_tag}"
if [[ "$verify_only" == "true" ]]; then
  echo "Mode: verify-only"
fi
if [[ "$dry_run" == "true" ]]; then
  echo "Mode: dry-run"
fi

ssh -i "$key_file" -p "$port" \
  -o BatchMode=yes \
  -o StrictHostKeyChecking=accept-new \
  "${user}@${host}" \
  DEPLOY_PATH="$deploy_path" \
  DEPLOY_BRANCH="$branch" \
  IMAGE_TAG="$image_tag" \
  VERIFY_ONLY="$verify_only" \
  DRY_RUN="$dry_run" \
  REMOTE_ENV_FILE="$remote_env_file" \
  'bash -s' <<'EOF'
set -euo pipefail

# Remote side checks and deployment execution.
cd "$DEPLOY_PATH"

if [[ ! -d .git ]]; then
  echo "Not a git repository: $DEPLOY_PATH"
  exit 1
fi

command -v git >/dev/null || { echo "git is not installed"; exit 1; }
command -v docker >/dev/null || { echo "docker is not installed"; exit 1; }

if [[ ! -f "$REMOTE_ENV_FILE" ]]; then
  echo "Missing remote env file: $REMOTE_ENV_FILE"
  exit 1
fi

if [[ ! -f "./deploy/scripts/deploy.sh" ]]; then
  echo "Missing remote deploy script: ./deploy/scripts/deploy.sh"
  exit 1
fi

if [[ "$VERIFY_ONLY" == "true" ]]; then
  echo "Verification succeeded."
  echo "Remote current branch: $(git rev-parse --abbrev-ref HEAD)"
  echo "Remote commit: $(git rev-parse --short HEAD)"
  exit 0
fi

if [[ "$DRY_RUN" == "true" ]]; then
  echo "Dry run: remote deployment steps"
  echo "cd \"$DEPLOY_PATH\""
  echo "git fetch --all --prune"
  echo "git checkout \"$DEPLOY_BRANCH\""
  echo "git pull --ff-only"
  echo "IMAGE_TAG=\"$IMAGE_TAG\" ENV_FILE=\"$REMOTE_ENV_FILE\" bash ./deploy/scripts/deploy.sh"
  echo "Remote current branch: $(git rev-parse --abbrev-ref HEAD)"
  echo "Remote commit: $(git rev-parse --short HEAD)"
  exit 0
fi

git fetch --all --prune
git checkout "$DEPLOY_BRANCH"
git pull --ff-only

IMAGE_TAG="$IMAGE_TAG" ENV_FILE="$REMOTE_ENV_FILE" bash ./deploy/scripts/deploy.sh
echo "Remote deployment completed."
EOF
