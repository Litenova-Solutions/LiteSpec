# Bind Proof to Code and Outcomes

A proof says a behavior holds, and a goal says an outcome was reached. This decision binds each proof to the code it ran against and each goal outcome to dated observations, so neither claim outlives its evidence.

## Context

A test run recorded no source commit, so a passing test followed by a code change still read as proof. A `fulfills` link read as verified before its scenarios passed. A goal's completion mixed delivery with outcome, so a fully built goal read as reached with nothing measured. Money rules faced examples written by the same author that implemented them.

## Options

| Option | Gains | Costs |
|:---|:---|:---|
| Runs record commits; goals separate delivery, outcome | Stale proofs show; goals never read reached unmeasured. | Runs record commits; held-back needs a second author. |
| Keep proof by test run alone | No commit tracking. | Green runs outlive their code; delivery marks goals reached. |
| Measure outcomes without observations | No new goal fields. | No date, value, or source backs the outcome. |

## Decision

A test run records its source commit. A scenario's proof is stale when the scenario, the test body, or any file carrying the mark of the behavior it proves changed after the run. The book says claimed for a link and verified for proof: a `fulfills` link reads as a claim until its scenarios pass.

Goals separate delivery from outcome. A goal's completion shows delivery only: its features, flows, and requirements. Its outcome reads not measured until the goal records `observations` with date, measured value, and source. A goal is never shown as reached from delivery alone.

Held-back examples have a procedure. They are required for calculation and refusal rules that carry the `money` risk and advisory elsewhere. They are written by a different author than the implementing agent, stored under `proof/withheld/` which the implementing agent's instructions exclude, carry expected values that cite the scenario's rule, and run in CI.

## Consequences

The book shows each goal's delivery beside its outcome, with unmeasured outcomes reading not measured. The proof states follow the recorded commit and the marks, so a code change after a green run marks its proofs stale.

## Evidence

[research/agent-testing-evidence](research/agent-testing-evidence) measured coding agents adding mocks and models asserting buggy behavior from buggy code, so the author proving a money rule differs from the author implementing it. [research/check-coverage-evidence](research/check-coverage-evidence) mapped the proof checks the commit record feeds. The unmeasured goals were read from live records with high confidence.

## Revisit Conditions

This decision reopens when proof needs a signal the commit and marks cannot carry. It also reopens when an outcome needs measuring no observation can state.
