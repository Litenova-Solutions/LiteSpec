#!/bin/sh
# Installs the LiteSpec command line on Linux or macOS from a GitHub release: the native
# binary for this machine and the LiteSpec release it carries, checked against the
# release's SHA256SUMS. The binary needs no .NET runtime.
#
#   curl -fsSL https://github.com/Litenova-Solutions/LiteSpec/releases/latest/download/install.sh | sh
#   curl -fsSL https://github.com/Litenova-Solutions/LiteSpec/releases/latest/download/install.sh | sh -s -- --version 4.1.0
#
# Options, each also read from the environment:
#
#   --version <version>   LITESPEC_VERSION       The release to install. Without one, the script installs
#                                                the release .litespec/project.json reviews in the working
#                                                directory, and else the latest release.
#   --install-dir <dir>   LITESPEC_INSTALL_DIR   The folder that keeps each release in <dir>/<version> and
#                                                links the active one at <dir>/bin/litespec. Defaults to
#                                                ~/.litespec.
#                         LITESPEC_DOWNLOAD_URL  The base URL that holds v<version>/ with the release assets.
#                                                Defaults to the GitHub releases of LiteSpec.
#
# Running again with another version installs it beside the others and points bin/litespec at it.
set -eu

repository="Litenova-Solutions/LiteSpec"
version="${LITESPEC_VERSION:-}"
install_dir="${LITESPEC_INSTALL_DIR:-${HOME:?HOME is not set}/.litespec}"
download_url="${LITESPEC_DOWNLOAD_URL:-https://github.com/$repository/releases/download}"

fail() {
  printf 'litespec install: %s\n' "$1" >&2
  exit 1
}

usage() {
  cat <<'USAGE'
Usage: install.sh [--version <version>] [--install-dir <dir>]

Installs the LiteSpec command line from a GitHub release, checked against its SHA256SUMS.

  --version      The release to install. Defaults to the release .litespec/project.json
                 reviews in the working directory, and else the latest release.
                 Also read from LITESPEC_VERSION.
  --install-dir  The folder that keeps each release and links the active one at bin/litespec.
                 Defaults to ~/.litespec. Also read from LITESPEC_INSTALL_DIR.

LITESPEC_DOWNLOAD_URL names another base URL that holds v<version>/ with the release assets.
USAGE
}

while [ $# -gt 0 ]; do
  case "$1" in
    --version)
      [ $# -ge 2 ] || fail "--version needs a value"
      version="$2"
      shift 2
      ;;
    --version=*)
      version="${1#*=}"
      shift
      ;;
    --install-dir)
      [ $# -ge 2 ] || fail "--install-dir needs a value"
      install_dir="$2"
      shift 2
      ;;
    --install-dir=*)
      install_dir="${1#*=}"
      shift
      ;;
    -h | --help)
      usage
      exit 0
      ;;
    *)
      fail "unknown option '$1'; use --version or --install-dir"
      ;;
  esac
done

have() {
  command -v "$1" >/dev/null 2>&1
}

# Downloads a URL to a file with curl, or with wget where curl is absent.
fetch() {
  if have curl; then
    curl -fsSL --retry 3 --output "$2" "$1"
  elif have wget; then
    wget -q -O "$2" "$1"
  else
    fail "neither curl nor wget is installed"
  fi
}

# The latest release version, from the tag GitHub redirects the latest release page to.
latest() {
  page="https://github.com/$repository/releases/latest"

  if have curl; then
    final="$(curl -fsSLI --retry 3 --output /dev/null --write-out '%{url_effective}' "$page")"
  elif have wget; then
    final="$(wget -q -S --spider "$page" 2>&1 | sed -n 's/^ *[Ll]ocation: *//p' | tail -n 1 | tr -d '\r')"
  else
    fail "neither curl nor wget is installed"
  fi

  tag="${final##*/}"

  case "$tag" in
    v[0-9]*) printf '%s' "${tag#v}" ;;
    *) fail "the latest release could not be read from $page; pass --version" ;;
  esac
}

# True on a Linux that runs musl, such as Alpine.
is_musl() {
  if have ldd && ldd --version 2>&1 | grep -qi musl; then
    return 0
  fi

  ls /lib/ld-musl-* >/dev/null 2>&1
}

if [ -z "$version" ] && [ -f .litespec/project.json ]; then
  version="$(sed -n 's/.*"reviewedLiteSpecVersion"[[:space:]]*:[[:space:]]*"\([^"]*\)".*/\1/p' .litespec/project.json | head -n 1)"

  if [ -n "$version" ]; then
    printf 'Installing the release .litespec/project.json reviews: %s\n' "$version"
  fi
fi

if [ -z "$version" ]; then
  version="$(latest)"
fi

version="${version#v}"

case "$(uname -s)" in
  Linux) os="linux" ;;
  Darwin) os="osx" ;;
  *) fail "no native build for $(uname -s); on Windows run install.ps1, and elsewhere install the .NET tool LiteSpec.Cli" ;;
esac

case "$(uname -m)" in
  x86_64 | amd64) arch="x64" ;;
  aarch64 | arm64) arch="arm64" ;;
  *) fail "no native build for $(uname -m); install the .NET tool LiteSpec.Cli" ;;
esac

# A shell running under Rosetta reports x86_64 on Apple silicon; the arm64 build runs natively there.
if [ "$os" = "osx" ] && [ "$arch" = "x64" ] && [ "$(sysctl -n sysctl.proc_translated 2>/dev/null || true)" = "1" ]; then
  arch="arm64"
fi

if [ "$os" = "linux" ] && is_musl; then
  os="linux-musl"
fi

rid="$os-$arch"
name="litespec-$version-$rid"
archive="$name.tar.gz"
work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT

printf 'Downloading LiteSpec %s for %s\n' "$version" "$rid"
fetch "$download_url/v$version/$archive" "$work/$archive" || fail "could not download $download_url/v$version/$archive"
fetch "$download_url/v$version/SHA256SUMS" "$work/SHA256SUMS" || fail "could not download $download_url/v$version/SHA256SUMS"

expected="$(awk -v file="$archive" '$2 == file || $2 == "*" file { print $1 }' "$work/SHA256SUMS")"
[ -n "$expected" ] || fail "SHA256SUMS lists no $archive"

if have sha256sum; then
  actual="$(sha256sum "$work/$archive" | awk '{ print $1 }')"
elif have shasum; then
  actual="$(shasum -a 256 "$work/$archive" | awk '{ print $1 }')"
else
  fail "neither sha256sum nor shasum is installed, so the download cannot be checked"
fi

[ "$expected" = "$actual" ] || fail "$archive does not match its SHA256SUMS entry"

mkdir -p "$work/extract"
tar -xzf "$work/$archive" -C "$work/extract"
[ -x "$work/extract/$name/litespec" ] || fail "$archive holds no $name/litespec"

# Each release keeps its own folder, so replacing one never touches another or anything else in the install folder.
mkdir -p "$install_dir/bin"
rm -rf "${install_dir:?}/$version"
mv "$work/extract/$name" "$install_dir/$version"
ln -sfn "../$version/litespec" "$install_dir/bin/litespec"

installed="$("$install_dir/bin/litespec" --version)" || fail "the installed binary does not run; see the platform requirements in the CLI overview"
[ "$installed" = "$version" ] || fail "the installed binary reports $installed instead of $version"
printf 'Installed LiteSpec %s at %s\n' "$version" "$install_dir/$version"

bin="$install_dir/bin"

# In a GitHub Actions job, later steps find the command on the PATH.
if [ -n "${GITHUB_PATH:-}" ]; then
  printf '%s\n' "$bin" >>"$GITHUB_PATH"
fi

case ":$PATH:" in
  *":$bin:"*)
    printf 'Run: litespec --help\n'
    ;;
  *)
    printf 'Add %s to your PATH, for example in ~/.profile or ~/.zshrc:\n\n  export PATH="%s:$PATH"\n\n' "$bin" "$bin"
    ;;
esac
