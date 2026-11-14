# Requirement Origin

A requirement in LiteSpec states something that the product must do. This decision requires every requirement to name its origin, which is the record it came from. It concerns the people who write features and requirements. It matters because a requirement with no origin cannot be traced up to the feature, constraint or decision behind it.

## Context

Most requirements name neither a carrying feature nor a basis. Every enforced Intent check still passes. No check reports these missing origins.

## Options

LiteSpec compares 4 ways to name where a requirement comes from.

| Option | Strengths | Drawbacks |
|---|---|---|
| Origin in `feature`, `basedOn` or `decides` | Established practice treats a requirement without an origin as incomplete | Authors must anchor most existing requirements, and list order matters |
| Feature only | One uniform rule covers every requirement | Wrong for requirements that a constraint gives rise to, and contradicts the field meaning in the Links table |
| Keep origin optional | No author writes anything new | Most requirements stay unanchored, and the chain from a goal to a test keeps breaking at the top |
| Group under the requirement page | Every requirement already sits on a page | A page style is not a record type, and file grouping hides the origin gap |

The chosen option gives each source one question to answer. A feature answers which carrying feature includes the requirement. A constraint or research record answers which fact grounds it. A decision answers which choice settles it. The record reports that both standards it cites call such a requirement orphaned when it has no origin.

## Decision

Every requirement names its parent. The parent is a carrying `feature`, including a shipped feature. When no feature carries the requirement, the parent is a constraint or research record in `basedOn`. When a decision settles the requirement, the decision names it in `decides`.

When more than one of these applies, the precedence runs in that order.

## Consequences

The provision `litespec/core/intent#identify-every-requirement` states the origin in its Statement. Authors anchor each existing requirement to an origin. Every feature carries requirements, including shipped features.

## Evidence

`research/intent-hierarchy-evidence` and `research/cross-layer-trace-evidence` are measured over the largest consumer on 2026-10-30, with high confidence. `research/established-traceability-practice` carries the limit that its claims on the regulated regimes rest on vendor restatements.

## Revisit Conditions

This decision reopens when a requirement kind appears that no feature carries, no constraint or research grounds, and no decision settles.
