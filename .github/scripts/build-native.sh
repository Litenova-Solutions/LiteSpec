#!/usr/bin/env bash
# Builds the native LiteSpec CLI for one runtime identifier, on a machine of that operating
# system: Native AOT cannot compile for another one. The output folder receives
#
#   packages/LiteSpec.Cli.<rid>.<version>.nupkg   the platform's .NET tool package
#   archives/litespec-<version>-<rid>.tar.gz      the standalone release archive, .zip on Windows
#
# The archive holds one folder, litespec-<version>-<rid>/, with the executable, the release it
# carries and the licence. Build the frontend reader and the book client first: the tool
# carries them, and packing fails without them.
#
#   .github/scripts/build-native.sh <rid> <output-folder>
set -euo pipefail

rid="$1"
mkdir -p "$2"
out="$(cd "$2" && pwd)"
repository="$(cd "$(dirname "$0")/../.." && pwd)"
version="$(sed -n 's/^  "version": "\([^"]*\)",$/\1/p' "$repository/litespec.manifest.json" | head -n 1)"
project="$repository/apps/cli/src/LiteSpec.Cli"

# Packing for one runtime identifier publishes the native executable first, into the publish folder the archive reuses.
dotnet pack "$project" --configuration Release --runtime "$rid" --output "$out/packages"

name="litespec-$version-$rid"
stage="$(mktemp -d)"
trap 'rm -rf "$stage"' EXIT
cp -R "$project/bin/Release/net10.0/$rid/publish" "$stage/$name"
cp "$repository/LICENSE" "$stage/$name/LICENSE"
mkdir -p "$out/archives"

case "$rid" in
  win-*)
    # Git Bash paths mean nothing to PowerShell, so hand it Windows paths.
    source_path="$(cygpath -w "$stage/$name")"
    archive_path="$(cygpath -w "$out/archives/$name.zip")"
    pwsh -NoProfile -Command "Compress-Archive -Path '$source_path' -DestinationPath '$archive_path'"
    ;;
  *)
    tar -C "$stage" -czf "$out/archives/$name.tar.gz" "$name"
    ;;
esac

echo "Built $(ls "$out/packages"/*."$version".nupkg | tr '\n' ' ')and $(ls "$out/archives")."
