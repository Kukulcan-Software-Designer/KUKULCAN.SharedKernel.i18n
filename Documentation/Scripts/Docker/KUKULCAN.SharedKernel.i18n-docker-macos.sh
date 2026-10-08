#!/usr/bin/env bash
set -Eeuo pipefail

IMAGE_REPOSITORY="jpardokukulcan/kukulcan-i18n"
CONTAINER_NAME="kukulcan-i18n"

cleanup() {
  DB_PASSWORD=""
  JWT_SECRET=""
  CONNECTION_STRING=""
  DOCKER_SERVER=""
  unset DB_PASSWORD JWT_SECRET CONNECTION_STRING DOCKER_SERVER
}
trap cleanup EXIT

fail() {
  local code=1
  if [ "$#" -ge 2 ]; then code="$2"; fi
  printf '\n[ERROR] %s\n' "$1" >&2
  exit "$code"
}

version_is_greater() {
  local left_major left_minor left_patch right_major right_minor right_patch
  IFS='.' read -r left_major left_minor left_patch <<< "$1"
  IFS='.' read -r right_major right_minor right_patch <<< "$2"
  if (( left_major > right_major )); then return 0; fi
  if (( left_major < right_major )); then return 1; fi
  if (( left_minor > right_minor )); then return 0; fi
  if (( left_minor < right_minor )); then return 1; fi
  (( left_patch > right_patch ))
}

latest_version_from_docker_hub() {
  local response version latest=""
  response="$(curl --fail --silent --show-error \
    "https://registry.hub.docker.com/v2/repositories/$IMAGE_REPOSITORY/tags?ordering=last_updated&page_size=100")" \
    || fail "Unable to query Docker Hub for $IMAGE_REPOSITORY."

  while IFS= read -r version; do
    [ -n "$version" ] || continue
    if [ -z "$latest" ] || version_is_greater "$version" "$latest"; then
      latest="$version"
    fi
  done < <(
    printf '%s' "$response" |
      grep -oE '"name"[[:space:]]*:[[:space:]]*"[0-9]+\.[0-9]+\.[0-9]+"' |
      sed -E 's/.*"([0-9]+\.[0-9]+\.[0-9]+)"/\1/'
  )

  [ -n "$latest" ] || fail "No semantic version tag (x.y.z) was found on Docker Hub."
  printf '%s' "$latest"
}

prompt_default() {
  local prompt="$1" default="$2" value
  read -r -p "  $prompt [$default]: " value
  if [ -z "$value" ]; then value="$default"; fi
  printf '%s' "$value"
}

prompt_required() {
  local prompt="$1" value
  read -r -p "  $prompt: " value
  [ -n "$value" ] || fail "$prompt cannot be empty."
  printf '%s' "$value"
}

prompt_password() {
  local prompt="$1" value
  read -r -s -p "  $prompt: " value
  printf '\n' >&2
  [ -n "$value" ] || fail "Password cannot be empty."
  printf '%s' "$value"
}

prompt_yes_no() {
  local prompt="$1" default="$2" value
  read -r -p "  $prompt [$default]: " value
  if [ -z "$value" ]; then value="$default"; fi
  case "$value" in
    Y|y|Yes|yes) printf 'true' ;;
    N|n|No|no) printf 'false' ;;
    *) fail "Please answer Y or N." ;;
  esac
}

quote_connection_value() {
  local value
  value="$(printf '%s' "$1" | sed 's/"/""/g')"
  printf '"%s"' "$value"
}

printf '%s\n' '========================================================'
printf '%s\n' ' KUKULCAN.SharedKernel.I18N Docker Deployment'
printf '%s\n' '========================================================'

printf '\nDocker server\n'
DOCKER_SERVER="$(prompt_default 'Docker server (local or tcp://host:port)' 'local')"

DOCKER_ARGS=()
HEALTH_HOST="127.0.0.1"

case "$DOCKER_SERVER" in
  ""|local|localhost)
    DOCKER_SERVER="local"
    ;;
  tcp://*)
    [[ "$DOCKER_SERVER" =~ ^tcp://[^/:]+:[0-9]+$ ]] ||
      fail "Remote Docker server must use tcp://host:port."
    DOCKER_ARGS=(--host "$DOCKER_SERVER")
    HEALTH_HOST="$(printf '%s' "$DOCKER_SERVER" | sed 's#^tcp://##; s/:.*$//')"
    ;;
  *)
    fail "Use 'local' or a Docker endpoint in the form tcp://host:port."
    ;;
esac

docker_cmd() {
  docker "\${DOCKER_ARGS[@]}" "$@"
}

printf '\nDocker\n'
command -v docker >/dev/null 2>&1 || fail "Docker CLI is not available in PATH."
command -v curl >/dev/null 2>&1 || fail "curl is required and was not found in PATH."
command -v openssl >/dev/null 2>&1 || fail "OpenSSL is required to generate the JWT secret."
docker_cmd info >/dev/null 2>&1 || fail "Docker server is not accessible."
printf '  Docker connection: OK\n'

printf '\nContainer\n'
CONTAINER_EXISTS=false
CURRENT_IMAGE=""
CURRENT_VERSION=""

if docker_cmd container inspect "$CONTAINER_NAME" >/dev/null 2>&1; then
  CONTAINER_EXISTS=true
  CURRENT_IMAGE="$(docker_cmd inspect -f '{{.Config.Image}}' "$CONTAINER_NAME")"
  CURRENT_VERSION="$(printf '%s' "$CURRENT_IMAGE" | sed -nE 's/.*:([0-9]+\.[0-9]+\.[0-9]+)$/\1/p')"
  printf '  Container: %s\n' "$CONTAINER_NAME"
  printf '  Current image: %s\n' "$CURRENT_IMAGE"
  if [ -n "$CURRENT_VERSION" ]; then
    printf '  Current version: %s\n' "$CURRENT_VERSION"
  else
    printf '  Current version: unknown\n'
  fi
else
  printf '  Container %s does not exist.\n' "$CONTAINER_NAME"
fi

printf '\nDocker Hub\n'
LATEST_VERSION="$(latest_version_from_docker_hub)"
printf '  Latest version: %s\n' "$LATEST_VERSION"

if [ "$CONTAINER_EXISTS" = false ]; then
  ACTION="create"
elif [ -z "$CURRENT_VERSION" ]; then
  ACTION="update"
elif version_is_greater "$LATEST_VERSION" "$CURRENT_VERSION"; then
  ACTION="update"
else
  ACTION="none"
fi

if [ "$ACTION" = "none" ]; then
  printf '\nUpdate\n'
  printf '  Current version: %s\n' "$CURRENT_VERSION"
  printf '  Latest version:  %s\n' "$LATEST_VERSION"
  printf '  No update required.\n'
  RUNNING="$(docker_cmd inspect -f '{{.State.Running}}' "$CONTAINER_NAME")"
  if [ "$RUNNING" = "true" ]; then
    printf '  Container is already running.\n'
  else
    printf '  Container is stopped; it has not been started because no update is required.\n'
  fi
  exit 0
fi

printf '\nDatabase (PostgreSQL)\n'
DB_HOST="$(prompt_default 'PostgreSQL host' 'mypostgres')"
DB_PORT="$(prompt_default 'PostgreSQL port' '5432')"
DB_NAME="$(prompt_default 'PostgreSQL database' 'Atlas')"
DB_USER="$(prompt_required 'PostgreSQL user')"
DB_PASSWORD="$(prompt_password 'PostgreSQL password')"

printf '\nDocker configuration\n'
NETWORK_NAME="$(prompt_default 'Docker network' 'kukulcan-local')"
HTTP_PORT="$(prompt_default 'HTTP port' '8080')"
AUTO_MIGRATE="$(prompt_yes_no 'Enable automatic migrations' 'Y')"
SEED_DATA="$(prompt_yes_no 'Enable seed data' 'Y')"
REDIS_CONNECTION="$(prompt_default 'Redis connection (empty to disable)' '')"

printf '\nApplication security\n'
JWT_ISSUER="$(prompt_default 'JWT issuer' 'ATLAS')"
JWT_AUDIENCE="$(prompt_default 'JWT audience' 'ATLAS.i18n')"
printf '  Generating JWT secret...\n'
JWT_SECRET="$(openssl rand -base64 48 | tr -d '\n')"
JWT_LENGTH="$(printf '%s' "$JWT_SECRET" | wc -c | tr -d '[:space:]')"
[ "$JWT_LENGTH" -ge 32 ] || fail "Generated JWT secret is too short."

CONNECTION_STRING="Host=$(quote_connection_value "$DB_HOST");Port=$(quote_connection_value "$DB_PORT");Database=$(quote_connection_value "$DB_NAME");Username=$(quote_connection_value "$DB_USER");Password=$(quote_connection_value "$DB_PASSWORD")"

printf '\nDocker network\n'
if docker_cmd network inspect "$NETWORK_NAME" >/dev/null 2>&1; then
  printf '  Network %s already exists.\n' "$NETWORK_NAME"
else
  docker_cmd network create "$NETWORK_NAME" >/dev/null ||
    fail "Unable to create Docker network $NETWORK_NAME."
  printf '  Network %s created.\n' "$NETWORK_NAME"
fi

printf '\nImage\n'
TARGET_IMAGE="$IMAGE_REPOSITORY:$LATEST_VERSION"
docker_cmd pull "$TARGET_IMAGE" ||
  fail "Unable to pull $TARGET_IMAGE."
printf '  Pulled %s\n' "$TARGET_IMAGE"

printf '\nContainer\n'
if [ "$ACTION" = "update" ]; then
  OLD_VERSION="$CURRENT_VERSION"
  if [ -z "$OLD_VERSION" ]; then OLD_VERSION="unknown"; fi
  printf '  Updating %s: %s -> %s\n' "$CONTAINER_NAME" "$OLD_VERSION" "$LATEST_VERSION"
  docker_cmd rm --force "$CONTAINER_NAME" >/dev/null ||
    fail "Unable to remove existing container $CONTAINER_NAME."
else
  printf '  Creating %s at version %s\n' "$CONTAINER_NAME" "$LATEST_VERSION"
fi

docker_cmd run --detach \
  --name "$CONTAINER_NAME" \
  --network "$NETWORK_NAME" \
  --publish "$HTTP_PORT:8080" \
  --env "ASPNETCORE_HTTP_PORTS=8080" \
  --env "Kukulcan__Database__Provider=PostgresSql" \
  --env "Kukulcan__Database__ConnectionString=$CONNECTION_STRING" \
  --env "Kukulcan__Database__Migration__AutoMigrateOnStartup=$AUTO_MIGRATE" \
  --env "Kukulcan__Database__Migration__SeedDataOnStartup=$SEED_DATA" \
  --env "Jwt__SecretKey=$JWT_SECRET" \
  --env "Jwt__Issuer=$JWT_ISSUER" \
  --env "Jwt__Audience=$JWT_AUDIENCE" \
  --env "ConnectionStrings__Redis=$REDIS_CONNECTION" \
  "$TARGET_IMAGE" >/dev/null ||
  fail "Unable to start container $CONTAINER_NAME."

printf '  Container started.\n'

printf '\nHealth\n'
HEALTH_BASE_URL="http://$HEALTH_HOST:$HTTP_PORT"
LIVE_URL="$HEALTH_BASE_URL/health/live"
READY_URL="$HEALTH_BASE_URL/health/ready"

LIVE_OK=false
for attempt in $(seq 1 30); do
  if curl --silent --show-error --fail "$LIVE_URL" >/dev/null 2>&1; then
    LIVE_OK=true
    break
  fi
  sleep 2
done
[ "$LIVE_OK" = true ] ||
  fail "/health/live did not become available. Check: docker logs $CONTAINER_NAME."

READY_OK=false
for attempt in $(seq 1 30); do
  if curl --silent --show-error --fail "$READY_URL" >/dev/null 2>&1; then
    READY_OK=true
    break
  fi
  sleep 2
done
[ "$READY_OK" = true ] ||
  fail "/health/ready did not become available. Check: docker logs $CONTAINER_NAME."

printf '  /health/live  -> Healthy\n'
printf '  /health/ready -> Healthy\n'
printf '\n========================================================\n'
printf ' KUKULCAN.SharedKernel.I18N %s\n' "$LATEST_VERSION"
if [ "$ACTION" = "update" ]; then
  printf ' Status: Updated successfully\n'
else
  printf ' Status: Created successfully\n'
fi
printf '========================================================\n'
