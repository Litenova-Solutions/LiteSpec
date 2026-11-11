#!/bin/sh
# Runs build-native.sh inside a Linux build container, so the native binary links against that
# container's C library: AlmaLinux 8 gives glibc 2.28, the oldest glibc the binary supports, and
# Alpine gives musl. The script installs the build tools and the SDK global.json names, builds a
# copy of the checkout so the host's build folders stay untouched, and hands the output to the
# caller's user.
#
#   docker run --rm -v "$PWD:/src:ro" -v "$PWD/native:/out" -e OWNER="$(id -u):$(id -g)" \
#     almalinux:8 sh /src/.github/scripts/build-native-container.sh linux-x64
set -eu

rid="$1"

. "$(dirname "$0")/lib.sh"

# Each phase prints how long it took, so a slow container build shows where its time went.
started=$(date +%s)
phase() {
  now=$(date +%s)
  echo "build-native-container: $1 took $((now - started))s"
  started=$now
}

if command -v dnf >/dev/null 2>&1; then
  # Weak dependencies and documentation are not needed to build, and parallel downloads
  # shorten the install on the AlmaLinux mirrors.
  retry dnf install -y -q --nodocs --setopt=install_weak_deps=False --setopt=max_parallel_downloads=10 \
    bash clang zlib-devel libicu krb5-libs openssl-libs findutils tar gzip which >/dev/null
elif command -v apk >/dev/null 2>&1; then
  retry apk add --no-cache -q bash clang build-base zlib-dev icu-libs krb5-libs libgcc libstdc++ curl tar >/dev/null
else
  echo "build-native-container.sh supports AlmaLinux and Alpine build images." >&2
  exit 2
fi

phase "installing the build tools"

curl -fsSL --retry 3 https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
retry bash /tmp/dotnet-install.sh --jsonfile /src/global.json --install-dir /opt/dotnet >/dev/null

export DOTNET_ROOT=/opt/dotnet
export PATH="/opt/dotnet:$PATH"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1

# The checkout is mounted read-only; build outputs, dependencies and history stay behind.
phase "installing the .NET SDK"

mkdir -p /tmp/src
(cd /src && tar --exclude=.git --exclude=node_modules --exclude=bin --exclude=obj -cf - .) | tar -C /tmp/src -xf -

phase "copying the checkout"

bash /tmp/src/.github/scripts/build-native.sh "$rid" /out
phase "building the native CLI"

if [ -n "${OWNER:-}" ]; then
  chown -R "$OWNER" /out
fi
