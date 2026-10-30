#!/usr/bin/env bash
# The verify ladder for the hierarchy release, run once per round over the combined tree.
#
#   bash verify.sh <out-dir>                 LiteSpec steps (waves 1 to 5)
#   NIGHTHALL=1 bash verify.sh <out-dir>     LiteSpec steps, then NightHall's model check (waves 6 to 8)
#
# Every step runs even after another fails, so one round shows every error. Each step's output
# goes to <out-dir>/<step>.log; <out-dir>/failed exists when any step failed.
set -u
out="$1"
rm -rf "$out"; mkdir -p "$out"
out="$(cd "$out" && pwd)"
ls_root="D:/Personal-Projects/LiteSpec"
nh_root="D:/Personal-Projects/Nighthall"
cd "$ls_root" || exit 2
fail=0
step() {
  local name="$1"; shift
  printf '%-14s ' "$name"
  if "$@" > "$out/$name.log" 2>&1; then echo pass; else echo "FAIL ($out/$name.log)"; fail=1; fi
}
cli() { dotnet run --project "$ls_root/apps/cli/src/LiteSpec.Cli" --no-build -- "$@"; }
maintain() { dotnet run --project "$ls_root/apps/maintain/src/LiteSpec.Maintain" --no-build -- "$@"; }

step build       dotnet build LiteSpec.slnx
step provisions  bash -c 'dotnet run --project apps/maintain/src/LiteSpec.Maintain --no-build -- provisions && git diff --exit-code -- docs/reference/provisions.md'
step validate    maintain validate
step tests       dotnet test --solution LiteSpec.slnx --no-build
step whitespace  git diff --check
step book-smoke  bash -c 'rm -rf "'"$out"'/book" && dotnet run --project apps/cli/src/LiteSpec.Cli --no-build -- book --model apps/cli/tests/LiteSpec.Cli.Tests/Fixtures/Book/model.json --out "'"$out"'/book"'

if [ "${NIGHTHALL:-0}" = 1 ]; then
  # NightHall: build the model with this LiteSpec, then check it. Its tests are never run here.
  step nh-build    cli model build --root "$nh_root"
  step nh-check    cli model check --root "$nh_root"
  step nh-consumer cli consumer --root "$nh_root"
  step nh-book     bash -c 'rm -rf "'"$out"'/nh-book" && dotnet run --project '"$ls_root"'/apps/cli/src/LiteSpec.Cli --no-build -- book --model '"$nh_root"'/.model/model.json --out "'"$out"'/nh-book"'
fi

[ "$fail" = 1 ] && touch "$out/failed"
exit "$fail"
