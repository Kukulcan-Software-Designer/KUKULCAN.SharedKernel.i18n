#!/usr/bin/env bash
set -Ee

# KUKULCAN.SharedKernel.i18n local Docker deployment helper.
# Run from the repository root.

IMAGE_NAME="kukulcan-i18n:local"
CONTAINER_NAME="kukulcan-i18n"
NETWORK_NAME="kukulcan-local"
DB_CONTAINER="mypostgres"
DB_PROVIDER="PostgresSql"
DB_HOST="mypostgres"
DB_PORT="5432"
DB_NAME="Atlas"
DB_USER="postgres"
HTTP_PORT="8080"
AUTO_MIGRATE="true"
SEED_DATA="true"
REDIS_CONNECTION=""
REPO_ROOT="$(pwd)"

if [ -n "$KUKULCAN_I18N_IMAGE_NAME" ]; then IMAGE_NAME="$KUKULCAN_I18N_IMAGE_NAME"; fi
if [ -n "$KUKULCAN_I18N_CONTAINER_NAME" ]; then CONTAINER_NAME="$KUKULCAN_I18N_CONTAINER_NAME"; fi
if [ -n "$KUKULCAN_I18N_NETWORK_NAME" ]; then NETWORK_NAME="$KUKULCAN_I18N_NETWORK_NAME"; fi
if [ -n "$KUKULCAN_I18N_DB_CONTAINER" ]; then DB_CONTAINER="$KUKULCAN_I18N_DB_CONTAINER"; fi
if [ -n "$KUKULCAN_I18N_DB_PROVIDER" ]; then DB_PROVIDER="$KUKULCAN_I18N_DB_PROVIDER"; fi
if [ -n "$KUKULCAN_I18N_DB_HOST" ]; then DB_HOST="$KUKULCAN_I18N_DB_HOST"; fi
if [ -n "$KUKULCAN_I18N_DB_PORT" ]; then DB_PORT="$KUKULCAN_I18N_DB_PORT"; fi
if [ -n "$KUKULCAN_I18N_DB_NAME" ]; then DB_NAME="$KUKULCAN_I18N_DB_NAME"; fi
if [ -n "$KUKULCAN_I18N_DB_USER" ]; then DB_USER="$KUKULCAN_I18N_DB_USER"; fi
if [ -n "$KUKULCAN_I18N_HTTP_PORT" ]; then HTTP_PORT="$KUKULCAN_I18N_HTTP_PORT"; fi
if [ -n "$KUKULCAN_I18N_AUTO_MIGRATE" ]; then AUTO_MIGRATE="$KUKULCAN_I18N_AUTO_MIGRATE"; fi
if [ -n "$KUKULCAN_I18N_SEED_DATA" ]; then SEED_DATA="$KUKULCAN_I18N_SEED_DATA"; fi
if [ -n "$KUKULCAN_I18N_REDIS_CONNECTION" ]; then REDIS_CONNECTION="$KUKULCAN_I18N_REDIS_CONNECTION"; fi
if [ -n "$KUKULCAN_I18N_REPO_ROOT" ]; then REPO_ROOT="$KUKULCAN_I18N_REPO_ROOT"; fi

DB_PASSWORD="$KUKULCAN_I18N_DB_PASSWORD"
JWT_SECRET="$KUKULCAN_I18N_JWT_SECRET"
JWT_ISSUER="ATLAS"
JWT_AUDIENCE="ATLAS.i18n"

TOTAL_STEPS=11
STEP=0

step() {
  STEP=$((STEP + 1))
  PERCENT=$((STEP * 100 / TOTAL_STEPS))
  printf '\n[%3s%%] %s\n' "$PERCENT" "$1"
}

fail() {
  CODE="$2"
  printf '\n[ERROR] %s\n' "$1" >&2
  exit "$CODE"
}

run_step() {
  DESCRIPTION="$1"
  shift
  step "$DESCRIPTION"
  printf '      Command: '
  printf '%q ' "$@"
  printf '\n'
  "$@"
  CODE=$?
  if [ "$CODE" -ne 0 ]; then
    printf '\n[ERROR] Command failed (exit code %s).\n' "$CODE" >&2
    printf '[ERROR] The command output above is the reported cause.\n' >&2
    exit "$CODE"
  fi
}

step "Checking Docker and required tools."
command -v docker >/dev/null 2>&1 || fail "Docker CLI is not available in PATH." 1
command -v curl >/dev/null 2>&1 || fail "curl is required and was not found in PATH." 1
docker info >/dev/null 2>&1 || fail "Docker Engine is not running or is not accessible." 1
printf '      Docker is available.\n'

step "Checking the repository and Dockerfile."
[ -f "$REPO_ROOT/Dockerfile" ] || fail "Dockerfile not found under $REPO_ROOT." 1
printf '      Repository: %s\n' "$REPO_ROOT"

step "Preparing PostgreSQL password and JWT secret."
if [ -z "$DB_PASSWORD" ]; then
  read -r -s -p "      PostgreSQL password for $DB_USER@$DB_CONTAINER: " DB_PASSWORD
  printf '\n'
fi
[ -n "$DB_PASSWORD" ] || fail "PostgreSQL password cannot be empty." 1

if [ -z "$JWT_SECRET" ]; then
  read -r -s -p "      JWT secret (leave empty to generate a local development secret): " JWT_SECRET
  printf '\n'
fi
if [ -z "$JWT_SECRET" ]; then
  if command -v openssl >/dev/null 2>&1; then
    JWT_SECRET="$(openssl rand -base64 48 | tr -d '\n')"
  else
    JWT_SECRET="$(head -c 48 /dev/urandom | base64 | tr -d '\n')"
  fi
  printf '      A temporary local JWT secret was generated.\n'
fi
[ "$(printf '%s' "$JWT_SECRET" | wc -c)" -ge 32 ] || fail "JWT secret must contain at least 32 characters." 1

step "Checking PostgreSQL container $DB_CONTAINER."
docker inspect "$DB_CONTAINER" >/dev/null 2>&1 || fail "PostgreSQL container $DB_CONTAINER does not exist." 1
DB_RUNNING="$(docker inspect -f '{{.State.Running}}' "$DB_CONTAINER")"
[ "$DB_RUNNING" = "true" ] || fail "PostgreSQL container $DB_CONTAINER is not running." 1
printf '      PostgreSQL container is running.\n'

step "Ensuring Docker network $NETWORK_NAME."
if docker network inspect "$NETWORK_NAME" >/dev/null 2>&1; then
  printf '      Network already exists.\n'
else
  run_step "Creating Docker network $NETWORK_NAME." docker network create "$NETWORK_NAME"
fi
if docker network inspect "$NETWORK_NAME" -f '{{range .Containers}}{{.Name}}{{"\n"}}{{end}}' | grep -Fxq "$DB_CONTAINER"; then
  printf '      PostgreSQL is already attached to the network.\n'
else
  run_step "Connecting PostgreSQL to $NETWORK_NAME." docker network connect "$NETWORK_NAME" "$DB_CONTAINER"
fi

step "Checking PostgreSQL readiness and database $DB_NAME."
docker exec "$DB_CONTAINER" pg_isready -U "$DB_USER"
CODE=$?
[ "$CODE" -eq 0 ] || fail "PostgreSQL readiness command failed (exit code $CODE)." "$CODE"
DB_EXISTS="$(docker exec "$DB_CONTAINER" psql -U "$DB_USER" -d postgres -tAc "SELECT 1 FROM pg_database WHERE datname='$DB_NAME';")"
CODE=$?
[ "$CODE" -eq 0 ] || fail "Database existence command failed (exit code $CODE)." "$CODE"
[ "$(printf '%s' "$DB_EXISTS" | tr -d '[:space:]')" = "1" ] || fail "Database $DB_NAME was not found. Create it before starting the API." 1
printf '      PostgreSQL is ready and database exists.\n'

run_step "Building Docker image $IMAGE_NAME." docker build --tag "$IMAGE_NAME" "$REPO_ROOT"

step "Removing existing container $CONTAINER_NAME, if present."
if docker container inspect "$CONTAINER_NAME" >/dev/null 2>&1; then
  run_step "Removing container $CONTAINER_NAME." docker rm --force "$CONTAINER_NAME"
else
  printf '      No existing container found.\n'
fi

step "Starting container $CONTAINER_NAME."
printf '      Image: %s\n' "$IMAGE_NAME"
printf '      Network: %s\n' "$NETWORK_NAME"
printf '      API port: %s:8080\n' "$HTTP_PORT"
printf '      Database: %s on %s:%s\n' "$DB_NAME" "$DB_HOST" "$DB_PORT"
CONNECTION_STRING="Host=$DB_HOST;Port=$DB_PORT;Database=$DB_NAME;Username=$DB_USER;Password=$DB_PASSWORD"

docker run --detach \
  --name "$CONTAINER_NAME" \
  --network "$NETWORK_NAME" \
  --publish "$HTTP_PORT:8080" \
  --env "ASPNETCORE_HTTP_PORTS=8080" \
  --env "Kukulcan__Database__Provider=$DB_PROVIDER" \
  --env "Kukulcan__Database__ConnectionString=$CONNECTION_STRING" \
  --env "Kukulcan__Database__Migration__AutoMigrateOnStartup=$AUTO_MIGRATE" \
  --env "Kukulcan__Database__Migration__SeedDataOnStartup=$SEED_DATA" \
  --env "Jwt__SecretKey=$JWT_SECRET" \
  --env "Jwt__Issuer=$JWT_ISSUER" \
  --env "Jwt__Audience=$JWT_AUDIENCE" \
  --env "ConnectionStrings__Redis=$REDIS_CONNECTION" \
  "$IMAGE_NAME"
CODE=$?
if [ "$CODE" -ne 0 ]; then
  printf '\n[ERROR] docker run failed (exit code %s).\n' "$CODE" >&2
  docker logs "$CONTAINER_NAME" >&2 2>/dev/null || true
  exit "$CODE"
fi

step "Waiting for liveness endpoint."
LIVE_URL="http://127.0.0.1:$HTTP_PORT/health/live"
LIVE_OK=false
for ATTEMPT in $(seq 1 30); do
  if curl --silent --show-error --fail "$LIVE_URL" >/dev/null 2>&1; then
    LIVE_OK=true
    printf '      Liveness is UP (attempt %s/30).\n' "$ATTEMPT"
    break
  fi
  printf '      Waiting... (%s/30)\n' "$ATTEMPT"
  sleep 2
done
[ "$LIVE_OK" = true ] || {
  printf '\n[ERROR] /health/live did not become available.\n' >&2
  docker logs "$CONTAINER_NAME" >&2 || true
  exit 1
}

step "Waiting for readiness endpoint (PostgreSQL)."
READY_URL="http://127.0.0.1:$HTTP_PORT/health/ready"
READY_OK=false
for ATTEMPT in $(seq 1 30); do
  if curl --silent --show-error --fail "$READY_URL" >/dev/null 2>&1; then
    READY_OK=true
    printf '      Readiness is UP (attempt %s/30).\n' "$ATTEMPT"
    break
  fi
  printf '      Waiting... (%s/30)\n' "$ATTEMPT"
  sleep 2
done
[ "$READY_OK" = true ] || {
  printf '\n[ERROR] /health/ready did not become available.\n' >&2
  docker logs "$CONTAINER_NAME" >&2 || true
  exit 1
}

printf '\n[100%%] Deployment completed successfully.\n'
printf '      API:       http://127.0.0.1:%s\n' "$HTTP_PORT"
printf '      Liveness:  %s\n' "$LIVE_URL"
printf '      Readiness: %s\n' "$READY_URL"
printf '      Logs:      docker logs -f %s\n' "$CONTAINER_NAME"
