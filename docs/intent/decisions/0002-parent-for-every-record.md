---
{
  "type": "decision",
  "id": "decision/0002-parent-for-every-record",
  "title": "Parent for Every Record",
  "owner": "LiteSpec maintainer",
  "status": "draft",
  "decider": "LiteSpec maintainer",
  "decideBy": "2026-11-30",
  "options": [
    {
      "name": "One parent for every record",
      "for": "Every serious tool stores the link on one side, computes the inverse, and enforces at most one parent, while identifier containment in the largest consumer already resolves everywhere it is used.",
      "against": "Authors must write the missing origins, and the book must stop inventing groupings where no parent exists yet."
    },
    {
      "name": "Parents only below the requirements",
      "for": "The checker is already dense there and every enforced check passes.",
      "against": "It leaves the measured top gaps in place: most requirements name no origin, no check reads a feature goal or a journey goal, and the book invents Intent groupings."
    },
    {
      "name": "Advisory parents at tier A",
      "for": "Existing projects are not blocked on day one, and staged promotion matches the skeptic evidence on training and theatre.",
      "against": "Regulated practice gates the chain rather than advising it, and a link rule the builder cannot check rots exactly as the skeptic sources describe."
    },
    {
      "name": "Down-pointing parent lists",
      "for": "A goal or feature page reads top down without following reverse links.",
      "against": "It breaks the one-written-parent rule every reviewed tool keeps, invites circular definitions, and contradicts the provision that a link lives on the record that points."
    }
  ],
  "chosen": "One parent for every record",
  "changeWhen": "A record type appears whose placement no identifier, link, or layer list can state.",
  "basedOn": [
    "research/behavior-containment-evidence",
    "research/intent-hierarchy-evidence",
    "research/book-outline-evidence",
    "research/established-traceability-practice"
  ]
}
---

# Parent for Every Record

## Context

Above the requirements the hierarchy is rationale prose rather than required links, and the book teaches a second hierarchy beside the model's own.

## Decision

Every record of every type has exactly one parent: by identifier containment, by one required written up-link, or, for a type with no record above it, its layer list under the product. A missing required parent is an error at tier G, reported even on a draft. Every inverse link is computed, never written.

## Consequences

`litespec/core/model#give-every-record-one-parent` states the algebra, and `litespec/core/model#organize-the-book-by-the-record-layers` with `litespec/core/model#write-links-on-the-record-that-points` carry the outline and link-direction duties. The product is the parent of every record whose type has no record parent.

## Evidence with Confidence

`research/behavior-containment-evidence` and `research/intent-hierarchy-evidence` are measured over the largest consumer on 2026-10-30, high confidence. `research/book-outline-evidence` is read from the builder source, high confidence on placement, medium on full-size behavior since the sample is partial. `research/established-traceability-practice` carries the stated vendor-restatement limit.
