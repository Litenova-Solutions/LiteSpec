---
{
  "type": "decision",
  "id": "decision/0017-proof-that-holds",
  "title": "Proof That Holds",
  "owner": "LiteSpec maintainer",
  "status": "draft",
  "decider": "LiteSpec maintainer",
  "decideBy": "2026-11-30",
  "options": [
    {
      "name": "Tracked proof state, own-status blocking, four words, one code grammar",
      "pros": "Committed hashes make staleness visible in CI, own-status blocking ends silent silencing, four words end the double meanings, and one grammar ends synonym codes.",
      "cons": "The ledger moves to a tracked file, every finding code is renamed, and the book drops its judgement code in one change."
    },
    {
      "name": "Keep the cache ledger",
      "pros": "No file moves and local runs keep their speed.",
      "cons": "The cache is empty in CI, so the first passing run blesses whatever it finds."
    },
    {
      "name": "Block on everything every day",
      "pros": "Nothing unverified ever sits quietly.",
      "cons": "Daily blocking on release-readiness items pushes adoption toward global silencing instead of steady proof."
    },
    {
      "name": "Keep effective-status blocking",
      "pros": "One inherited status decides every finding.",
      "cons": "A draft requirement silences blocking findings on approved use cases the draft never touched."
    }
  ],
  "choice": "Tracked proof state, own-status blocking, four words, one code grammar",
  "reopenedWhen": "A test runner appears whose outcomes the builder cannot read.",
  "basedOn": [
    "research/lower-layers-practice"
  ]
}
---

# Proof That Holds

## Context

"Verified" does not mean what it says: browser-proved scenarios can never pass, staleness lives in a git-ignored cache that is empty in CI, nothing blocks on an unverified scenario, a draft record silences blocking findings on approved records, and finding codes use synonyms for one problem.

## Decision

Proof state lives in `docs/proof/proofs.json`, committed, holding each scenario's hash and its test body's hash at the last passing run; stale when either changes; browser outcomes are read. A finding blocks or reports by its record's own status, and an inherited draft shows as a label. `use-case-unverified` reports by default; `litespec model check --release` makes the release-readiness items block; `litespec model check --since <ref>` blocks only on what changed since `<ref>`. Four proof words hold: verified for a use case, met for a requirement, tested for a passing test with an assertion, exercised for a journey step. Finding codes follow `<subject>-<problem>` with twelve problem words; an unknown code fails the build; the book renders `model.json` and judges nothing itself.

## Consequences

`litespec/core/model#compute-proof-statuses-in-the-builder` holds the tracked proof state and keeps judgement in the builder, `litespec/core/model#show-only-computed-judgements-in-the-book` keeps rendering in the book, and `litespec/core/model#hold-each-finding-to-its-own-status` gives way to own-status blocking. The glossary holds the four proof words.

## Evidence with Confidence

`research/lower-layers-practice` holds the outside review and the measured counts over the largest consumer behind D35 to D39, high confidence.
