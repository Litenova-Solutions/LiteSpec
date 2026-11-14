# Agents Never Weaken Proving Tests

A proving test checks one scenario of the specification. This decision forbids an agent to weaken such a test, and it has the model check report each proving test that changes while its scenario does not. A reviewer then reads each such change, and ordinary work does not stop for it.

## Context

Agents edit tests to make them pass when a test and the specification disagree. A proof that changes with the implementation stops proving the decided behavior and starts restating the code.

## Options

LiteSpec compared 3 options for changed proving tests. The table sets each option against the same two points: what it gains and what it costs.

| Option | Gains | Costs |
|:---|:---|:---|
| Forbid weakening, and report each changed proving test as an advisory finding | An agent that edits a test to pass leaves a finding for the reviewer, and the ledger already holds the hashes. | The finding is advisory, so a reviewer must read it, and it covers .NET methods only. |
| Rely on review of each change | No new rule and no new finding. | A test changed to match new code reads as a fix unless the reviewer compares it with the scenario. |
| Block every change to a proving test | No proving test changes without a block. | A correct edit to a proving test also blocks, so each test refactor needs a departure. |

## Decision

An agent never weakens a proving test. The rule lives on the agent page of the standard.

`litespec model check --since` reports each proving test that changed while its scenario did not, as an advisory finding. The reviewer sees the finding, and the change does not block the work.

## Consequences

`litespec/core/agent#protect-proofs-from-implementation-edits` states the rule. An agent does not delete, skip or weaken a proving test, or change its expected values, unless the scenario changed in the same change.

`litespec model check --since <ref>` reports `proof-hash-drift`, at level A on the Proof layer. The check reports it for each scenario whose proving test method changed while the scenario did not. Level A is the advisory check level.

The proof ledger keeps one method hash per scenario beside its file hash. Each method hash covers only the method's own text. Browser proving tests have no method hash yet, so the finding covers .NET methods only.

## Evidence

`research/agent-testing-evidence` reports that agents take shortcuts to pass a test that conflicts with the specification. ImpossibleBench builds such conflicts on purpose, and it counts a pass as a shortcut. The research read the three papers from their abstract pages, and no full text was read.

The research gives no rate for any named model. The record therefore rates the confidence that agents do this often as low, and the rule rests on the direction of the evidence.

## Revisit Conditions

This decision reopens when a test runner is added whose method boundary the builder cannot read, or when a review finds a weakened proving test that the finding did not report.

A runner without a readable method boundary leaves the method hash with nothing to hash. A missed weakening shows that the advisory finding misses a case.
