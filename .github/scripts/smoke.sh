#!/usr/bin/env bash
# Proves an installed LiteSpec CLI works the way a consumer runs it: in a folder outside
# this repository, against a copy of the Claimwick example, with no LITESPEC_HOME, so the
# CLI must find the release it carries beside itself. One scenario serves every install:
#
#   .github/scripts/smoke.sh --tool <package-folder>   the LiteSpec.Cli .NET tool, installed locally
#                                                      from the packages in the folder
#   .github/scripts/smoke.sh --binary <executable>     a native binary, such as one an install
#                                                      script or a release archive put in place
#   .github/scripts/smoke.sh --dll <litespec.dll>      the framework-dependent build, on the
#                                                      .NET runtime
#
# The script needs bash, coreutils and findutils, and dotnet only for --tool and --dll.
set -euo pipefail

mode="${1:-}"
target="${2:-}"

if [ -z "$mode" ] || [ -z "$target" ]; then
  echo "Usage: smoke.sh --tool <package-folder> | --binary <executable> | --dll <litespec.dll>" >&2
  exit 2
fi

repository="$(cd "$(dirname "$0")/../.." && pwd)"
. "$repository/.github/scripts/lib.sh"
version="$(manifest_version "$repository/litespec.manifest.json")"
work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT
unset LITESPEC_HOME

case "$mode" in
  --tool)
    packages="$(cd "$target" && pwd)"
    install="tool"
    litespec() { dotnet litespec "$@"; }
    # A local tool lives in the manifest of the folder it runs from, so each consumer folder installs it.
    prepare() {
      dotnet new tool-manifest >/dev/null
      dotnet tool install LiteSpec.Cli --local --version "$version" --add-source "$packages" >/dev/null
    }
    ;;
  --binary)
    executable="$(cd "$(dirname "$target")" && pwd)/$(basename "$target")"
    install="standalone"
    litespec() { "$executable" "$@"; }
    prepare() { :; }
    ;;
  --dll)
    assembly="$(cd "$(dirname "$target")" && pwd)/$(basename "$target")"
    install="tool"
    litespec() { dotnet "$assembly" "$@"; }
    prepare() { :; }
    ;;
  *)
    echo "Unknown mode '$mode'; use --tool, --binary or --dll." >&2
    exit 2
    ;;
esac

claimwick="$work/claimwick"
mkdir -p "$claimwick"
cp -R "$repository/examples/claimwick/." "$claimwick/"
cd "$claimwick"
prepare

# The installed CLI must report the release the manifest names.
test "$(litespec --version)" = "$version"

litespec model check
litespec model check --format json > "$work/check.json"
grep -q '"findings"' "$work/check.json"

# A fresh consumer starts with init, which reads the templates inside the install.
starter="$work/starter"
mkdir -p "$starter"
cd "$starter"
prepare
litespec init --preset dotnet-react --size small --install "$install"
test -f .litespec/project.json
grep -q "\"reviewedLiteSpecVersion\": \"$version\"" .litespec/project.json

if [ "$install" = "standalone" ]; then
  test ! -e .config/dotnet-tools.json
  grep -q 'Run `litespec rules <path>`' AGENTS.md
else
  test -f .config/dotnet-tools.json
  grep -q 'Run `dotnet litespec rules <path>`' AGENTS.md
fi

cd "$claimwick"

# Render the generated outputs, then render the book from the consumer root.
litespec model render
litespec book --root "$claimwick" --out "$work/book"
test -f "$work/book/index.html"

litespec model query use-case/spending.submit-expense > "$work/query.json"
grep -q '"id": "use-case/spending.submit-expense"' "$work/query.json"

litespec rules apps/api/src/Claimwick.SharedKernel/Domain/Money.cs > /dev/null

# Rename a record and rename it back; the copy must end byte for byte as it started.
sha256_tree "$claimwick" > "$work/claimwick-before.sha256"
litespec rename use-case/spending.submit-expense use-case/spending.submit-expense-smoke
litespec model query use-case/spending.submit-expense-smoke > /dev/null
litespec rename use-case/spending.submit-expense-smoke use-case/spending.submit-expense
sha256_tree "$claimwick" > "$work/claimwick-after.sha256"
diff "$work/claimwick-before.sha256" "$work/claimwick-after.sha256"

echo "LiteSpec $version passed the smoke run ($mode)."
