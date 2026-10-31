---
{
  "type": "research",
  "id": "research/field-naming-evidence",
  "title": "Field Naming Evidence",
  "owner": "LiteSpec maintainer",
  "status": "draft",
  "decider": "LiteSpec maintainer",
  "decideBy": "2026-11-30",
  "question": "Where does one field name carry two concepts, or one concept carry two names, across the record types?",
  "conclusion": "The field set is mostly systematic, but five collisions - `status` beside feature `state`, the bare `by`, `source` with five meanings, `serves` with three target sets, and the journey `goal` sentence beside the goal record - already produce invalid or misleading consumer data.",
  "sources": [
    {
      "path": "schemas/record.schema.json",
      "readOn": "2026-10-30"
    },
    {
      "path": "schemas/glossary.schema.json",
      "readOn": "2026-10-30"
    },
    {
      "path": "templates/consumer/glossary.json",
      "readOn": "2026-10-30"
    },
    {
      "path": "docs/core/model.md",
      "readOn": "2026-10-30"
    }
  ]
}
---

# Field Naming Evidence

## Answer

This record distils the field naming investigation of 2026-10-30 into the claims this release relies on. LiteSpec counts below come from a scan of 434 files under `docs/`, `schemas/`, `templates/`, and `examples/`; consumer counts were measured over the largest consumer's built model on 2026-10-30.

The field set is mostly systematic. All 37 page types share one header (`type`, `id`, `title`, `owner`, `status`, `decider`, `decideBy`, `terms`, plus approval fields on record-bearing types). Links are present-tense verbs, plural when they hold arrays, and `fulfills`, `governs`, `solves`, `decides`, and `basedOn` each carry one meaning everywhere. Every field name in both schemas is camelCase or a single lowercase word, and multiword names follow a readable suffix grammar: `<verb>By` deadlines, `<verb>On` event dates, `<verb>When` conditions, and `<verb>Because` justifications. Confidence is high: the inventory was parsed from the schemas programmatically.

Five collisions do real damage. First, `status` (approval state, on every record) sits beside feature `state` (offer lifecycle: planned, shipped, later, refused), while `states` also names aggregate lifecycle entries; all 111 consumer features are `status: draft` while `state` splits 71 refused, 39 planned, 1 later, and 0 shipped, so readers meet two status-like fields with unrelated values. Second, bare `by` is one name for two value kinds: goals require a date, requirements take dates, but features take milestones (`v1`, `M1 Evidence` in all 39 planned records) under a type of plain string, beside four siblings that follow the `<verb>By` pattern. Third, `source` carries five meanings: the transcription marker on 13 page types plus most entries (about 2,900 consumer fields), the constraint's free-text outside source, the integration event's CloudEvents source, the nested input provenance enum, and the nested output provenance aggregate array under a singular name, while `research.sources` is a sixth shape beside them. Fourth, `serves` spans three target sets (feature to goals, use case and reaction and job to steps or goals, screen to steps) across 1,807 records, with two meanings: a feature contributes to a goal while a use case carries a walker through a step. Fifth, `goal` is both a record type and a required prose sentence on all 112 journeys, the break the journey link change must resolve.

Two collisions already produce bad consumer data. All 111 features carry milestone-style `by` values where the surrounding convention expects dates, and 11 use-case `serves` values name a whole journey where the schema allows only a journey step or a goal. Smaller overloads were judged keep-and-document: `steps` on journeys versus process aggregates (rename the aggregate side to `processSteps`, 6 records); `values` on requirements versus terms (rename the term side to `codes`); `subject` shared by known gaps, deviations, and integration events; `scope`, `check`, and `code` with consistent cores. The `*Because` suffix needs repair only where it holds links: nested `consistency.because` (a rule list) and `wideningBecause` (a requirement link) should become verb-form link names. Effect-level `onFailure` is a string while page-level `onFailure` is a structured object, the most mechanical inconsistency in the schemas; the effect-level string should take the structured shape or the name `onFailurePolicy`.

The investigation also found schema drift outside the brief's list. The behavior page, its provision, and the consumer glossary template all require `layer` and `aliases` on every term, but the glossary schema defines neither with `additionalProperties: false`, and the template's `schemaVersion: 3` contradicts the schema's `const: 2`; all 508 consumer terms carry `layer`, so the schema is the odd one out. The consumer further carries fields LiteSpec never defines: `result.kind` (163, duplicating `outcome`), `setting.readBy` (15), `notification.sentBy` (8), `notification.personal` (9), `job.errorBackoff` (4), `job.startupDelay` (3), and `rule.writes` (33, always empty). Those are consumer invention, but the link-shaped names show authors reaching for vocabulary LiteSpec does not offer.

The proposed naming rule is: one concept, one name, one shape, everywhere it appears. A field name means the same concept, holds the same value kind, and takes the same cardinality on every record that carries it. New fields are coined only by the existing grammar: a classifier is `kind`; a link is a present-tense verb, plural when it holds an array; a deadline is `<verb>By` and a recorded date is `<verb>On`; a justification is `reason` or `rationale`, never a `*Because` holding links; the defining sentence of a named thing is `meaning` and the normative sentence of a promise is `statement`; a group nests as an object only when its fields always travel together; and a name already taken by a record type is never reused for a prose field. The mechanical half (same name means same schema type, plural means array, `*By` and `*On` mean dates) is checkable by the maintainer validator; the judgement half stays a reviewed provision.
