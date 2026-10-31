---
{
  "type": "decision",
  "id": "decision/0008-book-follows-record-layers",
  "title": "Book Follows Record Layers",
  "owner": "LiteSpec maintainer",
  "status": "draft",
  "decider": "LiteSpec maintainer",
  "decideBy": "2026-11-30",
  "options": [
    {
      "name": "Rebuild the generator from exported parents",
      "pros": "One exported parent per record ends file-path parents, silent coin tosses, and invented nodes in a single mechanism the checker already proves.",
      "cons": "The generator, its outline fixtures, and the book page contract are all rewritten at once."
    },
    {
      "name": "Patch the current outline",
      "pros": "Smaller diffs land sooner and existing fixtures keep passing.",
      "cons": "The defects are structural: file parents, first-match placement, invented groupings, and two deleted layers cannot be patched one by one."
    },
    {
      "name": "Keep file-grouped requirements",
      "pros": "Placement matches authoring and needs no new field.",
      "cons": "A page style is not a record type, and the grouping breaks the outline and label provisions at once."
    },
    {
      "name": "Keep the stray sweep",
      "pros": "No record ever goes unplaced, so the book always renders complete.",
      "cons": "Unplaced records are promoted to roots beside modules, which hides misplacement instead of reporting it as a finding."
    }
  ],
  "choice": "Rebuild the generator from exported parents",
  "reopenedWhen": "The approved mockup is replaced by a newer approved shape.",
  "basedOn": [
    "research/book-outline-evidence",
    "research/behavior-containment-evidence",
    "research/check-coverage-evidence"
  ]
}
---

# Book Follows Record Layers

## Context

The outline places Behavior correctly but invents Intent groupings, splits the glossary, and deletes the Implementation and Proof layers it builds.

## Decision

The book generator is rebuilt to the approved mockup, generated from the model: one offline shell with sharded data, every record placed once under its exported parent, and every unplaceable record reported as a finding instead of swept to a root.

## Consequences

`litespec/core/model#organize-the-book-by-the-record-layers` with `litespec/core/model#name-book-labels-with-litespec-terms`, `litespec/core/model#explain-the-book-from-litespec-definitions`, `litespec/core/model#show-only-computed-judgements-in-the-book`, `litespec/core/model#add-a-missing-fact-to-litespec-before-the-book-shows-it`, and `litespec/core/model#point-every-book-entry-to-its-source` carry the rebuilt contract.

## Evidence with Confidence

`research/book-outline-evidence` is read from the builder source, high confidence on placement, medium on full-size behavior since the sample is partial. `research/behavior-containment-evidence` and `research/check-coverage-evidence` are measured over the largest consumer on 2026-10-30, high confidence.
