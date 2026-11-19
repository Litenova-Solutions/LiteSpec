# Requirement Origin

People who write features and requirements need every requirement to name the record it came from. A requirement with no origin cannot be traced up to the feature, constraint, or decision behind it. This decision sets that origin.

## Context

Most requirements name neither a carrying feature nor a basis. Every enforced Intent check still passes. No check reports these missing origins.

## Options

LiteSpec compares 4 ways to name where a requirement comes from.

| Option | Strengths | Drawbacks |
|---|---|---|
| Origin in `feature` or `basedOn` | Established practice treats a requirement without an origin as incomplete | Authors must anchor most existing requirements, and list order matters |
| Feature only | One uniform rule covers every requirement | Wrong for requirements that follow from a constraint, and contradicts the field meaning in the Links table |
| Keep origin optional | No author writes anything new | Most requirements stay unanchored, and the chain from a goal to a test keeps breaking at the top |
| Group under the requirement page | Every requirement already sits on a page | A page style is not a record type, and file grouping hides the origin gap |

The chosen option gives each source one question to answer. A feature answers which feature carries the requirement. A constraint, research record, or decision in `basedOn` answers which fact grounds it or which choice settles it. Both standards the research cites call a requirement without an origin orphaned.

## Decision

Every requirement names its parent. The parent is a carrying `feature`, including a shipped feature. When no feature carries the requirement, the requirement names the constraint, research record, or decision behind it in `basedOn`.

When both apply, the feature stays the parent.

## Consequences

The provision `litespec/core/intent#identify-every-requirement` states the origin in its Statement. Authors anchor each existing requirement to an origin. Every feature carries requirements, including shipped features.

## Evidence

The measurements behind `research/intent-hierarchy-evidence` and `research/cross-layer-trace-evidence` cover the largest consumer on 2026-10-30, with high confidence. `research/established-traceability-practice` carries the limit that its claims on the regulated regimes rest on vendor restatements.

## Revisit Conditions

This decision reopens when a requirement kind appears that no feature carries, no constraint or research grounds, and no decision settles.
