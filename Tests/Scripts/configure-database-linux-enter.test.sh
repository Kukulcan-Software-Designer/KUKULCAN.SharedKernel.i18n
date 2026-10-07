#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="\$(cd -- "\$(dirname -- "\${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="\$(cd -- "\${SCRIPT_DIR}/../.." && pwd)"
DATABASE_SCRIPT="\${REPO_ROOT}/Documentation/Scripts/configure-database-linux.sh"
TEMP_HOME="\$(mktemp -d)"
FAKE_BIN="\${TEMP_HOME}/bin"
OUTPUT_FILE="\${TEMP_HOME}/terminal-output.log"
trap 'rm -rf "\${TEMP_HOME}"' EXIT
mkdir -p "\${FAKE_BIN}"

cat > "\${FAKE_BIN}/dotnet" <<'EOF'
#!/usr/bin/env bash
set -euo pipefail
if [[ "\${1:-}" == "ef" && "\${2:-}" == "--version" ]]; then
  printf '%s\n' '10.0.12'
  exit 0
fi
if [[ "\${1:-}" == "tool" ]]; then
  exit 0
fi
if [[ "\${1:-}" == "ef" && "\${2:-}" == "database" && "\${3:-}" == "update" ]]; then
  exit 0
fi
exit 1
EOF
chmod +x "\${FAKE_BIN}/dotnet"

export HOME="\${TEMP_HOME}"
export PATH="\${FAKE_BIN}:\${PATH}"
export DATABASE_SCRIPT
export OUTPUT_FILE

python3 - <<'PY'
import os
import pty
import select
import time

script = os.environ["DATABASE_SCRIPT"]
output_file = os.environ["OUTPUT_FILE"]

pid, fd = pty.fork()
if pid == 0:
    os.execvpe("bash", ["bash", script], os.environ)

os.write(fd, b"2\rAtlas\rpostgre\rTestSecret123!\r")

output = bytearray()
deadline = time.time() + 15

while time.time() < deadline:
    ready, _, _ = select.select([fd], [], [], 0.2)
    if not ready:
        continue
    try:
        chunk = os.read(fd, 4096)
    except OSError:
        break
    if not chunk:
        break
    output.extend(chunk)
    if b"Base de datos configurada y migraciones aplicadas correctamente." in output:
        break

with open(output_file, "wb") as handle:
    handle.write(output)

text = output.decode(errors="replace")
password = "TestSecret123!"
marker = "Password: "
start = text.find(marker)
if start < 0:
    raise SystemExit("Password prompt was not found.")

after_prompt = text[start + len(marker):]
execution = "Ejecutando migraciones EF Core para PostgresSql..."
end = after_prompt.find(execution)
if end < 0:
    raise SystemExit(
        "The script did not reach the next command after pressing Enter. "
        f"Terminal output: {text!r}"
    )

masked = after_prompt[:end]
if masked != "*" * len(password):
    raise SystemExit(
        "Password masking output is incorrect: "
        f"expected {len(password)} stars, got {masked!r}"
    )

print("Linux interactive password Enter regression test passed.")
PY
