#!/usr/bin/env bash
# Installs a freshly built archive the way a consumer does, with the install script, from a local
# HTTP server that lays the archive out as a GitHub release: v<version>/ with SHA256SUMS. On
# Windows it installs with Windows PowerShell 5.1 and with PowerShell 7, and keeps the second.
# It writes the installed executable to GITHUB_ENV as LITESPEC_INSTALLED, in a form both bash
# and .NET read, or prints it outside GitHub Actions.
#
#   .github/scripts/install-native.sh <archives-folder> <install-folder>
set -euo pipefail

archives="$(cd "$1" && pwd)"
mkdir -p "$2"
install_dir="$(cd "$2" && pwd)"
repository="$(cd "$(dirname "$0")/../.." && pwd)"
version="$(sed -n 's/^  "version": "\([^"]*\)",$/\1/p' "$repository/litespec.manifest.json" | head -n 1)"
site="$(mktemp -d)"

mkdir -p "$site/v$version"
cp "$archives"/litespec-* "$site/v$version/"

if command -v sha256sum >/dev/null 2>&1; then
  (cd "$site/v$version" && sha256sum litespec-* | sed 's/ \*/  /' > SHA256SUMS)
else
  (cd "$site/v$version" && shasum -a 256 litespec-* > SHA256SUMS)
fi

# The server lets the system pick a free port, since Windows reserves port ranges that differ by machine.
log="$(mktemp)"
node "$repository/.github/scripts/serve.mjs" "$site" >"$log" 2>&1 &
server=$!
trap 'kill "$server" 2>/dev/null || true; rm -rf "$site" "$log"' EXIT
port=""

for _ in $(seq 1 80); do
  port="$(sed -n 's/^port \([0-9][0-9]*\).*/\1/p' "$log" | head -n 1)"

  if [ -n "$port" ] && curl -fsS "http://127.0.0.1:$port/v$version/SHA256SUMS" >/dev/null 2>&1; then
    break
  fi

  sleep 0.25
done

test -n "$port"

export LITESPEC_DOWNLOAD_URL="http://127.0.0.1:$port"

case "$(uname -s)" in
  MINGW* | MSYS* | CYGWIN*)
    script="$(cygpath -w "$repository/apps/cli/install/install.ps1")"
    powershell.exe -NoProfile -ExecutionPolicy Bypass -File "$script" -Version "$version" -InstallDir "$(cygpath -w "$install_dir/desktop")"
    pwsh -NoProfile -ExecutionPolicy Bypass -File "$script" -Version "$version" -InstallDir "$(cygpath -w "$install_dir/core")"
    installed="$(cygpath -m "$install_dir/core/$version/litespec.exe")"
    ;;
  *)
    LITESPEC_INSTALL_DIR="$install_dir" sh "$repository/apps/cli/install/install.sh" --version "$version"
    installed="$install_dir/bin/litespec"
    ;;
esac

test "$("$installed" --version)" = "$version"

if [ -n "${GITHUB_ENV:-}" ]; then
  echo "LITESPEC_INSTALLED=$installed" >> "$GITHUB_ENV"
else
  echo "$installed"
fi
