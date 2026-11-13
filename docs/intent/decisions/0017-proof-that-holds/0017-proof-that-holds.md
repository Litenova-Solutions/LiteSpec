# Proof That Holds

## Context

"Verified" does not mean what it says: browser-proved scenarios can never pass, staleness lives in a git-ignored cache that is empty in CI, nothing blocks on an unverified scenario, and finding codes use synonyms for one problem.

## Decision

Proof state lives in `.litespec/proof/proofs.json`, committed, holding each scenario's hash and the hashes of its proving tests, by file and by method, at the last passing run; stale when the scenario or a proving test changes; browser outcomes are read. A finding blocks or reports by its check level. `use-case-unverified` reports by default; `litespec model check --release` makes the release-readiness items block; `litespec model check --since <ref>` blocks only on what changed since `<ref>`. Four proof words hold: coded for code found, verified for a use case or a requirement whose fulfillers are all verified, tested for a passing test with an assertion, and exercised for a journey step. Finding codes follow `<subject>-<problem>` with fifteen problem words; an unknown code fails the build; the book renders the in-memory model and judges nothing itself.

## Evidence with Confidence

`research/lower-layers-practice` holds the outside review and the measured counts over the largest consumer behind `decision/0017-proof-that-holds`, high confidence.
