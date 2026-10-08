#!/usr/bin/env bash
set -euo pipefail

SCRIPT="Documentation/Scripts/Docker/KUKULCAN.SharedKernel.i18n-docker-macos.sh"

test -f "$SCRIPT"
bash -n "$SCRIPT"

grep -Fq 'kukulcan-i18n' "$SCRIPT"
grep -Fq 'jpardokukulcan/kukulcan-i18n' "$SCRIPT"
grep -Fq 'Docker Hub' "$SCRIPT"
grep -Eq '(^|[[:space:]])(docker|docker_cmd)[[:space:]]+pull([[:space:]]|$)' "$SCRIPT"
grep -Eq '(^|[[:space:]])(docker|docker_cmd)[[:space:]]+(container[[:space:]]+)?inspect([[:space:]]|$)' "$SCRIPT"
grep -Eq '(^|[[:space:]])(docker|docker_cmd)[[:space:]]+run([[:space:]]|$)' "$SCRIPT"
grep -Fq 'PostgreSQL' "$SCRIPT"
grep -Fq 'DB_PASSWORD' "$SCRIPT"
grep -Fq 'DB_USER' "$SCRIPT"
grep -Fq 'openssl rand -base64 48' "$SCRIPT"
grep -Fq 'JWT_SECRET' "$SCRIPT"
grep -Fq 'unset' "$SCRIPT"
grep -Fq '/health/live' "$SCRIPT"
grep -Fq '/health/ready' "$SCRIPT"

if grep -Eq 'read[^\n]*JWT[ _-]?(secret|password)' "$SCRIPT"; then
  echo "The JWT secret must be generated automatically, not requested interactively."
  exit 1
fi

echo "macOS Docker update script contract tests passed."
