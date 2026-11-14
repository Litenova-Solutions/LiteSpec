# Proof That Holds

LiteSpec marks scenarios and requirements as proven, and its checks report findings that block or pass a build. This decision sets where the proof state is kept, which findings block, what the 4 proof words mean, and how finding codes are written. It matters because a word such as `verified` must mean the same thing in every check.

## Context

The word verified does not mean what it says. Browser-proved scenarios can never pass. Staleness lives in a git-ignored cache, which is empty in CI. Nothing blocks on an unverified scenario, and finding codes use synonyms for one problem.

## Options

LiteSpec compares 4 ways to keep proof honest.

| Option | Strengths | Drawbacks |
|---|---|---|
| Tracked proof state, check level blocking, 4 words, one code grammar | Committed hashes make staleness visible in CI, check level blocking stops silent silencing, the 4 words end double meanings, and one grammar ends synonym codes | The ledger moves to a tracked file, every finding code is renamed, and the book drops its judgement code in one change |
| Keep the cache ledger | No file moves, and local runs keep their speed | The cache is empty in CI, so the first passing run approves whatever it finds |
| Block on everything every day | Nothing unverified sits quietly | Daily blocking on release-readiness items pushes adoption toward global silencing instead of steady proof |
| Keep the book as judge | The book shows one verdict per record | Rendering judges instead of the builder, so the two vocabularies drift apart |

The chosen option keeps judgement in the builder and blocks by check level. The other options keep a cache that CI cannot read, block too much, or split the judgement between the builder and the book.

## Decision

Proof state lives in `.litespec/proof/proofs.json`, which is committed. The file holds each scenario's hash and the hashes of its proving tests, by file and by method, as of the last passing run. A scenario is stale when the scenario or a proving test changes. The builder reads browser outcomes.

A finding blocks or reports by its check level. The finding `use-case-unverified` reports by default. `litespec model check --release` makes the release-readiness items block. `litespec model check --since <ref>` blocks only on what changed since `<ref>`.

The 4 proof words are `coded`, `verified`, `tested` and `exercised`. `coded` means that code was found. `verified` means that a use case or a requirement has fulfillers that are all verified. `tested` means that a test passes with an assertion. `exercised` means that a journey step ran.

Finding codes follow `<subject>-<problem>` with 15 problem words. An unknown code fails the build. The book renders the in-memory model and judges nothing itself.

## Consequences

The provision `litespec/core/model#compute-proof-statuses-in-the-builder` holds the tracked proof state and keeps judgement in the builder. The provision `litespec/core/model#show-only-computed-judgements-in-the-book` keeps rendering in the book. Findings block by check level.

The glossary holds the 4 proof words. `decision/0033-agents-never-weaken-proving-tests` adds the advisory proof-change finding on the method hash.

## Evidence

`research/lower-layers-practice` holds the outside review and the counts measured over the largest consumer. This decision rests on that research, with high confidence.

## Revisit Conditions

This decision reopens when a test runner appears whose outcomes the builder cannot read.
