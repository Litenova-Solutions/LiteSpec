#!/usr/bin/env bash
# Prints the test filter arguments for one shard of the .NET test suite.
#
#   .github/scripts/test-shard.sh <shard> <total>
#
# Every test class in apps/*/tests (a class declared in a file that holds a
# [Fact] or a [Theory]) is named by its full name, the names are sorted, and
# shard <shard> of <total> (1-based) takes every <total>-th name. The output is
# one --filter-class argument per class, for `dotnet test`. Every class lands
# in exactly one shard, so the shards together run the whole suite.
set -euo pipefail

shard="${1:?shard number, 1-based}"
total="${2:?total number of shards}"
if [ "$shard" -lt 1 ] || [ "$shard" -gt "$total" ]; then
  echo "Shard $shard is outside 1..$total." >&2
  exit 2
fi

cd "$(dirname "$0")/../.."

find apps -path '*/tests/*' -name '*.cs' -not -path '*/bin/*' -not -path '*/obj/*' -print0 |
  xargs -0 grep -lE '\[(Fact|Theory)' |
  while IFS= read -r file; do
    namespace=$(grep -m1 -oE '^namespace [A-Za-z0-9_.]+' "$file" | awk '{print $2}')
    [ -n "$namespace" ] || continue
    grep -oE '^[[:space:]]*(public |internal |sealed |static |abstract |partial )*class [A-Za-z0-9_]+' "$file" |
      awk -v ns="$namespace" '{print ns "." $NF}'
  done |
  LC_ALL=C sort -u |
  awk -v shard="$shard" -v total="$total" '(NR - 1) % total == shard - 1 { print "--filter-class"; print $0 }'
