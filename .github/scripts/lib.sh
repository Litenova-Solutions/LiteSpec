# Helpers the CI scripts share. Each script sources this file. It is POSIX sh, because the Alpine
# and AlmaLinux build images run their scripts under sh and the runners run them under bash.
#
#   . "$(dirname "$0")/lib.sh"

# Runs a command up to three times, so a package mirror that fails once does not fail the job.
retry() {
  for attempt in 1 2 3; do
    if "$@"; then
      return 0
    fi

    if [ "$attempt" -lt 3 ]; then
      echo "Attempt $attempt of '$*' failed; retrying." >&2
      sleep $((attempt * 15))
    fi
  done

  return 1
}

# Prints the version the manifest names. The manifest is YAML whose top-level version is one plain
# scalar line. The build, smoke and distribution containers carry no Node, so the line is read here.
#   manifest_version <manifest-file>
manifest_version() {
  sed -n -E "s/^version: *['\"]?([^'\" ]+)['\"]? *\$/\1/p" "$1" | head -n 1 | grep . || {
    echo "$1 names no version." >&2
    return 1
  }
}

# Prints the SHA-256 command the host has: sha256sum where it exists, shasum -a 256 on macOS.
sha256_command() {
  if command -v sha256sum >/dev/null 2>&1; then
    echo sha256sum
  else
    echo "shasum -a 256"
  fi
}

# Prints the SHA-256 of every file under a folder, in a stable order.
#   sha256_tree <folder>
sha256_tree() {
  (cd "$1" && find . -type f -print0 | LC_ALL=C sort -z | xargs -0 $(sha256_command))
}

# Serves a folder with serve.mjs in the background and waits until the server names its port. The
# caller reads SERVER_PID and SERVER_PORT afterwards; the server writes its output to the log file.
#   serve_folder <folder> <log-file> <serve.mjs>
serve_folder() {
  node "$3" "$1" >"$2" 2>&1 &
  SERVER_PID=$!
  SERVER_PORT=""

  for _ in $(seq 1 80); do
    SERVER_PORT="$(sed -n 's/^port \([0-9][0-9]*\).*/\1/p' "$2" | head -n 1)"

    if [ -n "$SERVER_PORT" ]; then
      return 0
    fi

    sleep 0.25
  done

  return 1
}
