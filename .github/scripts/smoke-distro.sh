#!/bin/sh
# Proves the standalone binary on a stock Linux distribution: it installs the tools a consumer
# machine has, installs LiteSpec with install.sh from the release assets at
# LITESPEC_DOWNLOAD_URL, and runs the smoke scenario against the installed binary.
#
#   docker run --rm --network host -v "$PWD:/src:ro" -e LITESPEC_DOWNLOAD_URL=http://127.0.0.1:8000 \
#     debian:12 sh /src/.github/scripts/smoke-distro.sh
set -eu

. "$(dirname "$0")/lib.sh"

# These are the tools install.sh and smoke.sh call.
# No OpenSSL: the executable needs only the C library.
if command -v apt-get >/dev/null 2>&1; then
  export DEBIAN_FRONTEND=noninteractive
  retry apt-get update -qq >/dev/null
  retry apt-get install -y -qq --no-install-recommends bash ca-certificates curl diffutils findutils gawk gzip tar >/dev/null
elif command -v dnf >/dev/null 2>&1; then
  # Some images ship curl-minimal, which serves install.sh and conflicts with the curl package.
  retry dnf install -y -q bash diffutils findutils gawk gzip tar >/dev/null
elif command -v zypper >/dev/null 2>&1; then
  retry zypper --non-interactive --quiet install bash curl diffutils findutils gawk gzip tar >/dev/null
elif command -v apk >/dev/null 2>&1; then
  # Alpine keeps BusyBox wget, so install.sh downloads without curl there.
  retry apk add --no-cache -q bash coreutils diffutils findutils >/dev/null
else
  echo "smoke-distro.sh supports apt, dnf, zypper and apk distributions." >&2
  exit 2
fi

version="$(release_version /src/litespec.manifest.yaml)"
install_dir="$(mktemp -d)"

LITESPEC_INSTALL_DIR="$install_dir" sh /src/apps/cli/install/install.sh --version "$version"
bash /src/.github/scripts/smoke.sh --binary "$install_dir/bin/litespec"
