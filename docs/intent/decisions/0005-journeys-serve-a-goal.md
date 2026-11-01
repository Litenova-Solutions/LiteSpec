# Journeys Serve a Goal

## Context

Every other Intent record that means goal links to a goal record, while the journey holds a sentence.

## Decision

The journey sentence field `goal` is renamed `aim`. Every journey names its goal record in a new required single link `serves: goal/<name>`, which is its parent. `finishedWhen` and the steps are unchanged.

## Evidence with Confidence

`research/intent-hierarchy-evidence` and `research/cross-layer-trace-evidence` are measured over the largest consumer on 2026-10-30, high confidence, including the finding that no goal sentence matches a goal title. `research/established-traceability-practice` carries the stated vendor-restatement limit on the regulated regimes.
