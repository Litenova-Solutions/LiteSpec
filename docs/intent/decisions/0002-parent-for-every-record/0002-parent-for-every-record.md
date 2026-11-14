# Parent for Every Record

Every LiteSpec record has one parent. The parent is the record that the record sits under in the book. The book is the documentation that LiteSpec generates from its model. This decision sets how a parent is found, and what a record gets when no record sits above it. It matters because a record with no clear parent is placed by a silent rule.

## Context

Above the requirements, the hierarchy is written as rationale prose, not as required links. No check reads a feature goal or a journey goal. The book teaches a second hierarchy beside the model's own, and that second hierarchy invents groupings where the model has no parent.

## Options

LiteSpec compares 4 ways to give records parents.

| Option | Strengths | Drawbacks |
|---|---|---|
| One parent for every record | Each link is stored on one side, and identifier containment already resolves in the largest consumer | Authors must write the missing origins |
| Parents only below the requirements | The checker is dense there, and every enforced check passes | The top gaps stay open, and the book invents Intent groupings |
| Advisory parents at tier A | Existing projects are not blocked on day one | Regulated practice gates the chain rather than advising it, and a link rule the builder cannot check decays |
| Down-pointing parent lists | A goal or feature page reads from the top down | Breaks the one-written-parent rule and invites circular definitions |

Tier A is the advisory level, where a finding informs and does not block. Tier G is the gating level. The first option is the one chosen, because it is the only option that gives every record a parent.

## Decision

Every record of every type has exactly one parent. The parent is found in one of three ways. It is the record whose identifier contains the record, or the one record that the record's required written up-link names. For a type with no record above it, the parent is the product, through the product's layer list.

A missing required parent is an error at tier G. The model computes every inverse link, and no author writes one.

## Consequences

The provision `litespec/core/model#give-every-record-one-parent` states the rule. The provisions `litespec/core/model#organize-the-book-by-the-record-layers` and `litespec/core/model#write-links-on-the-record-that-points` carry the outline and the direction of links.

Authors write the missing origins. The book stops inventing groupings where no parent exists yet. The product is the parent of every record whose type has no record parent.

## Evidence

`research/behavior-containment-evidence` and `research/intent-hierarchy-evidence` are measured over the largest consumer on 2026-10-30, with high confidence. `research/book-outline-evidence` is read from the builder source. Its placement finding has high confidence, and its full-size behavior has medium confidence, because the sample is partial.

`research/established-traceability-practice` carries the limit that its claims on the regulated regimes rest on vendor restatements.

## Revisit Conditions

This decision reopens when a record type appears whose placement no identifier, link or layer list can state.
