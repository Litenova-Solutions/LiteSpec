# Flows Serve a Goal

A flow in LiteSpec describes a path that a person takes through the product, written as ordered steps. This decision sets how a flow links to the goal it serves. A flow serves one goal, and that goal is its parent. It matters because a reader who opens a flow can follow it up to its goal.

## Context

Every other Intent record that means a goal links to a goal record. A flow does not. It states its goal in a sentence, in the field `goal`, and no link connects it to a goal record.

## Options

LiteSpec compares 4 options for how a flow states its goal.

| Option | Strengths | Drawbacks |
|---|---|---|
| A flow serves one goal record | Story mapping and impact mapping both make the spine a link that the book can follow | Every flow sentence must be mapped to a goal record by hand, since no mechanical mapping exists |
| Keep the goal a sentence only | A founder accepts a flow chapter with no field names and no other page to open | The book cannot roll steps up to a goal, so the top-down link stays missing |
| Replace the sentence with the link | One field holds the whole goal fact, with nothing to keep in sync | The sentence that the founder accepts is lost, and a goal link carries less than that sentence |
| Allow several goals per flow | A walk can serve a staff outcome and a system promise at once | Both spine methods hold one spine, and the singular shape states that rule |

The chosen option keeps the sentence that the founder accepts and adds one link beside it. The link is what the book follows.

## Decision

The flow sentence field `goal` is renamed `aim`. Every flow names its goal record in one required link, `serves: goal/<name>`, and that link is its parent. The `finishedWhen` field and the steps stay unchanged.

## Consequences

The provision `litespec/core/intent#lead-each-flow-toward-the-record-it-exists-for` states the sentence beside the link. Use cases serve flow steps, not goals.

## Evidence

`research/intent-hierarchy-evidence` and `research/cross-layer-trace-evidence` are measured over the largest consumer on 2026-10-30, with high confidence. The measurement finds that no goal sentence matches a goal title. `research/established-traceability-practice` carries the limit that its claims on the regulated regimes rest on vendor restatements.

## Revisit Conditions

This decision reopens when a flow kind appears that walks toward no goal record.
