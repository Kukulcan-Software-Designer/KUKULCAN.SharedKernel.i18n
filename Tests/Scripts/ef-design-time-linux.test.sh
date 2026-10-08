#!/usr/bin/env bash
set -euo pipefail

REPO_ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../.." && pwd)"
MIGRATION_PROJECT="Source/KUKULCAN.SharedKernel.i18n.Migrations.PostgreSql/KUKULCAN.SharedKernel.i18n.Migrations.PostgreSql.csproj"
DATABASE_CONNECTION="Host=localhost;Port=5432;Database=DesignTimeTest;Username=postgre;Password=TestSecret123!"

cd "${REPO_ROOT}"

unset Jwt__SecretKey
export KUKULCAN__DATABASE__PROVIDER="PostgresSql"
export KUKULCAN__DATABASE__CONNECTIONSTRING="${DATABASE_CONNECTION}"

dotnet ef migrations script \
  --project "${MIGRATION_PROJECT}" \
  --startup-project "${MIGRATION_PROJECT}" \
  --configuration Release \
  --output "/tmp/kukulcan-i18n-migrations.sql"

grep -Fq -- '--startup-project "${migration_project}"' Documentation/Scripts/configure-database-linux.sh

printf '%s\n' 'Linux EF design-time migration test passed.'
