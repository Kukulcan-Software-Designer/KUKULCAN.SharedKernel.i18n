#!/usr/bin/env bash
set -euo pipefail

# Regression coverage for password capture and EF Core update reachability on macOS.

REPO_ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../.." && pwd)"
SCRIPT="${REPO_ROOT}/Documentation/Scripts/configure-database-macos.sh"
TEMP_HOME="$(mktemp -d)"
trap 'rm -rf "${TEMP_HOME}"' EXIT

mkdir -p "${TEMP_HOME}/.dotnet/tools" "${TEMP_HOME}/.config/kukulcan"

cat > "${TEMP_HOME}/fake-dotnet" <<'EOF'
#!/usr/bin/env bash
set -euo pipefail
if [[ "${1:-}" == "ef" && "${2:-}" == "--version" ]]; then
  printf '%s
' '10.0.12'
  exit 0
fi
if [[ "${1:-}" == "ef" && "${2:-}" == "database" && "${3:-}" == "update" ]]; then
  printf '%s
' "${*}" > "${FAKE_DOTNET_LOG}"
  exit 0
fi
if [[ "${1:-}" == "tool" ]]; then
  exit 0
fi
exit 0
EOF
chmod +x "${TEMP_HOME}/fake-dotnet"
ln -s "${TEMP_HOME}/fake-dotnet" "${TEMP_HOME}/.dotnet/tools/dotnet"

printf '2
Atlas
postgre
TestSecret123!
' |
  HOME="${TEMP_HOME}" PATH="${TEMP_HOME}/.dotnet/tools:${PATH}"   FAKE_DOTNET_LOG="${TEMP_HOME}/dotnet.log" bash "${SCRIPT}"

ENV_FILE="${TEMP_HOME}/.config/kukulcan/database.env"
grep -Fq 'KUKULCAN_DATABASE_PROVIDER="PostgresSql"' "${ENV_FILE}"
grep -Fq 'Password=TestSecret123!' "${ENV_FILE}"
grep -Fq 'KUKULCAN__DATABASE__CONNECTIONSTRING="Host=localhost;Port=5432;Database=Atlas;Username=postgre;Password=TestSecret123!"' "${ENV_FILE}"
! grep -Fq '*' "${ENV_FILE}"
grep -Fq -- 'ef database update --project Source/KUKULCAN.SharedKernel.i18n.Migrations.PostgreSql/KUKULCAN.SharedKernel.i18n.Migrations.PostgreSql.csproj --startup-project Source/KUKULCAN.SharedKernel.i18n.API/KUKULCAN.SharedKernel.i18n.API.csproj --configuration Release' "${TEMP_HOME}/dotnet.log"
