#!/usr/bin/env bash
# Proves the packed LiteSpec.Cli works the way a consumer installs it: from a package
# folder, as a local tool, in a folder outside this repository, against a copy of the
# Claimwick example, with no LITESPEC_HOME. The tool must find its release beside itself.
#
#   .github/scripts/smoke-tool.sh <package-folder>
set -euo pipefail

packages="$(cd "$1" && pwd)"
repository="$(cd "$(dirname "$0")/../.." && pwd)"
version="$(jq -r .version "$repository/litespec.manifest.json")"
work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT

claimwick="$work/claimwick"
mkdir -p "$claimwick"
cp -R "$repository/examples/claimwick/." "$claimwick/"
cd "$claimwick"
unset LITESPEC_HOME

dotnet new tool-manifest >/dev/null
dotnet tool install LiteSpec.Cli --local --version "$version" --add-source "$packages" >/dev/null

# The packed tool must report the release the manifest names.
test "$(dotnet litespec --version)" = "$version"

dotnet litespec consumer
dotnet litespec ui
dotnet litespec model check

# A fresh consumer starts with init, which reads the templates inside the package.
starter="$work/starter"
mkdir -p "$starter"
cd "$starter"
dotnet new tool-manifest >/dev/null
dotnet tool install LiteSpec.Cli --local --version "$version" --add-source "$packages" >/dev/null
dotnet litespec init --preset dotnet-react --size small
test -f litespec.project.json
test "$(jq -r .reviewedLiteSpecVersion litespec.project.json)" = "$version"

cd "$claimwick"

# Build the model, then render the book from the model file the build writes.
dotnet litespec model build
test -f "$claimwick/.model/model.json"
dotnet litespec book --model "$claimwick/.model/model.json" --out "$work/book"
test -f "$work/book/index.html"

dotnet litespec model query use-case/spending.submit-expense >/dev/null

# Rename a record and rename it back; the copy must end byte for byte as it started.
before="$work/claimwick-before.sha256"
after="$work/claimwick-after.sha256"
(cd "$claimwick" && find . -type f -print0 | LC_ALL=C sort -z | xargs -0 sha256sum) > "$before"
dotnet litespec rename use-case/spending.submit-expense use-case/spending.submit-expense-smoke
dotnet litespec model query use-case/spending.submit-expense-smoke >/dev/null
dotnet litespec rename use-case/spending.submit-expense-smoke use-case/spending.submit-expense
(cd "$claimwick" && find . -type f -print0 | LC_ALL=C sort -z | xargs -0 sha256sum) > "$after"
diff "$before" "$after"
