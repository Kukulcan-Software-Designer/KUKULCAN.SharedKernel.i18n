#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd -- "${SCRIPT_DIR}/../.." && pwd)"
DATABASE_SCRIPT="${REPO_ROOT}/Documentation/Scripts/configure-database-linux.sh"
TEMP_HOME="$(mktemp -d)"
FAKE_BIN="${TEMP_HOME}/bin"
DOTNET_LOG="${TEMP_HOME}/dotnet.log"
cleanup() { rm -rf "${TEMP_HOME}"; }
trap cleanup EXIT
mkdir -p "${FAKE_BIN}"
cat > "${FAKE_BIN}/dotnet" <<'EOF'
#!/usr/bin/env bash
set -euo pipefail
if [[ "${1:-}" == "ef" && "${2:-}" == "--version" ]]; then
  printf '%s\n' "Entity Framework Core tools version 10.0.302"
  exit 0
fi
if [[ "${1:-}" == "tool" && ( "${2:-}" == "install" || "${2:-}" == "update" ) ]]; then
  exit 0
fi

if [[ "${1:-}" == "ef" && "${2:-}" == "database" && "${3:-}" == "update" ]]; then
  printf '%s\n' "$*" > "${DOTNET_LOG}"
  exit 0
fi
printf 'Unexpected dotnet invocation: %s\n' "$*" >&2
exit 1
EOF
chmod +x "${FAKE_BIN}/dotnet"
export HOME="${TEMP_HOME}"
export DOTNET_LOG
export PATH="${FAKE_BIN}:${PATH}"
printf '%s\n' '2' 'Atlas' 'postgre' 'TestSecret123!' | bash "${DATABASE_SCRIPT}"
ENV_FILE="${HOME}/.config/kukulcan/database.env"
test -f "${ENV_FILE}"
grep -Fq 'KUKULCAN__DATABASE__PROVIDER="PostgresSql"' "${ENV_FILE}"
printf '%s\n' '2' 'Atlas' 'postgre' 'TestSecret123!' | bash "${DATABASE_SCRIPT}"
printf '%s\n' '2' 'Atlas' 'postgre' 'TestSecret123!' | bash "${DATABASE_SCRIPT}"
grep -Fq 'KUKULCAN__DATABASE__CONNECTIONSTRING="Host=localhost;Port=5432;Database=Atlas;Username=postgre;Password=TestSecret123!"' "${ENV_FILE}"
if grep -Eq '(^|[[:space:]])export KUKULCAN_DATABASE_(PROVIDER|CONNECTION_STRING)=|^KUKULCAN_DATABASE_(PROVIDER|CONNECTION_STRING)=' "${ENV_FILE}"; then
  printf '%s\n' 'Single-underscore database variables must not be persisted.' >&2
  exit 1
fi
if grep -Fq 'KUKULCAN__DATABASE__CONNECTION__STRING' "${ENV_FILE}"; then
  printf '%s\n' 'The legacy double-underscore connection variable without the STRING separator must not be persisted.' >&2
  exit 1
fi
if grep -Fq '*' "${ENV_FILE}"; then
  printf '%s\n' 'Password masking characters leaked into the persisted connection string.' >&2
  exit 1
fi
test -f "${DOTNET_LOG}"
grep -Fq 'ef database update --project Source/KUKULCAN.SharedKernel.i18n.Migrations.PostgreSql/KUKULCAN.SharedKernel.i18n.Migrations.PostgreSql.csproj --startup-project Source/KUKULCAN.SharedKernel.i18n.API/KUKULCAN.SharedKernel.i18n.API.csproj --configuration Release' "${DOTNET_LOG}"
printf '%s\n' 'Linux database configuration script integration test passed.'
