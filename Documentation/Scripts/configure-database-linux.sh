#!/usr/bin/env bash
set -euo pipefail

ENV_FILE="${HOME}/.config/kukulcan/database.env"
DOTNET_TOOLS_DIR="${HOME}/.dotnet/tools"
SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd -- "${SCRIPT_DIR}/../.." && pwd)"
API_PROJECT="Source/KUKULCAN.SharedKernel.i18n.API/KUKULCAN.SharedKernel.i18n.API.csproj"
SHELL_RC=""

case "${SHELL:-}" in
  */zsh) SHELL_RC="${HOME}/.zshrc" ;;
  */bash) SHELL_RC="${HOME}/.bashrc" ;;
  *) SHELL_RC="${HOME}/.profile" ;;
esac

mask_password() {
  local password=""
  local char=""
  while IFS= read -r -s -n 1 char; do
    [[ "${char}" == $'\n' || "${char}" == $'\r' ]] && break
    if [[ "${char}" == $'\177' || "${char}" == $'\b' ]]; then
      if [[ -n "${password}" ]]; then
        password="${password%?}"
        printf '\b \b' >&2
      fi
    else
      password+="${char}"
      printf '*' >&2
    fi
  done
  printf '\n' >&2
  printf '%s' "${password}"
}

escape_value() {
  local value="$1"
  value="${value//\\/\\\\}"
  value="${value//\"/\"\"}"
  printf '%s' "${value}"
}

ensure_ef_tool() {
  export PATH="${DOTNET_TOOLS_DIR}:${PATH}"

  local ef_version=""
  ef_version="$(dotnet ef --version 2>/dev/null || true)"

  if [[ "${ef_version}" != 10.* ]]; then
    if ! dotnet tool update --global dotnet-ef --version 10.* >/dev/null 2>&1; then
      dotnet tool install --global dotnet-ef --version 10.*
    fi
  fi

  export PATH="${DOTNET_TOOLS_DIR}:${PATH}"
}

while true; do
  printf '%s\n' "Seleccione el gestor de base de datos:"
  printf '%s\n' "1. SQL Server"
  printf '%s\n' "2. PostgreSQL"
  printf '%s\n' "3. MySQL"
  printf '%s\n' "0. Salir sin registrar las variables"
  read -r -p "Opción: " option

  case "${option}" in
    1)
      provider="SqlServer"
      port="1433"
      migration_project="Source/KUKULCAN.SharedKernel.i18n.Migrations.SqlServer/KUKULCAN.SharedKernel.i18n.Migrations.SqlServer.csproj"
      ;;
    2)
      provider="PostgresSql"
      port="5432"
      migration_project="Source/KUKULCAN.SharedKernel.i18n.Migrations.PostgreSql/KUKULCAN.SharedKernel.i18n.Migrations.PostgreSql.csproj"
      ;;
    3)
      provider="MySql"
      port="3306"
      migration_project="Source/KUKULCAN.SharedKernel.i18n.Migrations.MySql/KUKULCAN.SharedKernel.i18n.Migrations.MySql.csproj"
      ;;
    0)
      printf '%s\n' "Salir sin registrar las variables"
      exit 0
      ;;
    *)
      printf '%s\n\n' "Opción no válida. Inténtelo de nuevo."
      continue
      ;;
  esac
  break
done

read -r -p "Database name: " database
read -r -p "User: " user
printf '%s' "Password: "
password="$(mask_password)"

database_escaped="$(escape_value "${database}")"
user_escaped="$(escape_value "${user}")"
password_escaped="$(escape_value "${password}")"

case "${provider}" in
  SqlServer)
    connection_string="Server=localhost,${port};Database=${database_escaped};User Id=${user_escaped};Password=${password_escaped};TrustServerCertificate=True"
    ;;
  PostgresSql)
    connection_string="Host=localhost;Port=${port};Database=${database_escaped};Username=${user_escaped};Password=${password_escaped}"
    ;;
  MySql)
    connection_string="Server=localhost;Port=${port};Database=${database_escaped};User=${user_escaped};Password=${password_escaped}"
    ;;
esac

mkdir -p "$(dirname "${ENV_FILE}")"
umask 077
cat > "${ENV_FILE}" <<EOF
export KUKULCAN_DATABASE_PROVIDER="${provider}"
export KUKULCAN_DATABASE_CONNECTION_STRING="${connection_string}"
export KUKULCAN__DATABASE__PROVIDER="${provider}"
export KUKULCAN__DATABASE__CONNECTIONSTRING="${connection_string}"
EOF
chmod 600 "${ENV_FILE}"

source_line='[ -f "$HOME/.config/kukulcan/database.env" ] && . "$HOME/.config/kukulcan/database.env"'
touch "${SHELL_RC}"
if ! grep -Fqx "${source_line}" "${SHELL_RC}" 2>/dev/null; then
  printf '\n%s\n' "${source_line}" >> "${SHELL_RC}"
fi

export KUKULCAN_DATABASE_PROVIDER="${provider}"
export KUKULCAN_DATABASE_CONNECTION_STRING="${connection_string}"
export KUKULCAN__DATABASE__PROVIDER="${provider}"
export KUKULCAN__DATABASE__CONNECTIONSTRING="${connection_string}"

cd "${REPO_ROOT}"
ensure_ef_tool

printf '%s\n' "Ejecutando migraciones EF Core para ${provider}..."
dotnet ef database update \
  --project "${migration_project}" \
  --startup-project "${API_PROJECT}" \
  --configuration Release

printf '%s\n' "Base de datos configurada y migraciones aplicadas correctamente."
printf '%s\n' "Provider: ${provider}"
printf '%s\n' "Host: localhost"
printf '%s\n' "Port: ${port}"
printf '%s\n' "La configuración persistirá para futuras sesiones de terminal."
