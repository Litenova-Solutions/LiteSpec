---
{
  "type": "research",
  "id": "research/intent-hierarchy-evidence",
  "title": "Intent Hierarchy Evidence",
  "owner": "LiteSpec maintainer",
  "status": "draft",
  "decider": "LiteSpec maintainer",
  "decideBy": "2026-11-30",
  "question": "Which Intent links does LiteSpec require and check, and what does the largest consumer show while they stay optional?",
  "conclusion": "Above the requirements, LiteSpec's hierarchy is rationale prose rather than required or checked links, and in the largest consumer 719 of 928 requirements name no origin while every enforced Intent check passes.",
  "sources": [
    {
      "path": "docs/core/intent.md",
      "readOn": "2026-10-30"
    },
    {
      "path": "docs/core/model.md",
      "readOn": "2026-10-30"
    },
    {
      "path": "docs/tools/model.md",
      "readOn": "2026-10-30"
    },
    {
      "path": "schemas/record.schema.json",
      "readOn": "2026-10-30"
    }
  ]
}
---

# Intent Hierarchy Evidence

## Answer

This record distils the Intent layer hierarchy investigation of 2026-10-30 into the claims this release relies on. Counts below were measured over the largest consumer's built model on 2026-10-30, holding 21,548 records.

Only four Intent links are required by schema, stated as obligations, and enforced by a check at the same time: `product.audience`, `journey.actor`, a requirement `except` entry to its `fallback`, and `decision.decides` on product decisions, the last by provision plus check despite an optional schema. Every other link above the requirements is optional in the schema, mentioned only in provision Rationale, and unchecked. One layer down LiteSpec already accepts the every-record-points-up principle: a use case with neither `serves` nor `fulfills` fails `record-orphaned` at tier G. The principle was simply never applied inside Intent. Confidence is high: each cell was read from the schema, the provision text, and the check table.

The central measured gap is requirements with no origin: 719 of 928 name neither `feature` (56 do) nor `basedOn` (164 do, 11 carry both). Absence of `feature` alone is not a defect, because the field is defined narrowly as the planned, later, or refused feature that carries the requirement, so a built requirement correctly carries none. The combined absence is still not a conformance defect today, because the provision puts the expectation in its Rationale while its Statement and check require only identifier, `kind`, `statement`, and dated business values. The 719 are therefore specification-tolerated traceability defects, chargeable to LiteSpec's missing obligation and missing check rather than to the consumer's authors. One measured signal shows the anchor is already load-bearing in practice: of 150 requirements fulfilled by nothing below, the 100 without a `feature` all appear as known `requirement-unfulfilled` findings, while none of the 50 with a `feature` produced such a finding.

Features split cleanly by state. All 40 live features carry `solves`, and all 8 goals are served by 16 `serves` links from 12 features, so the top of the chain is intact; the gap is that 28 of 40 live features name no goal. All 71 refused features carry the required `refusedBecause` and no `solves` or `serves`, which matches the refused exception exactly. Journeys name their goal as prose in 112 of 112 cases, by design: the schema requires a string, the provision requires the goal in words, and the Links table holds no journey to goal edge. A migration warning is measured: all 112 goal sentences are distinct and none contains any goal record's title, so no mechanical mapping to the eight goals exists.

The remaining populations are tolerated or by design. Five of 29 constraints and 15 of 29 research records are cited by no `basedOn`; no rule requires citation, so they form a basis pool, though 15 of 29 suggests the pool is written faster than decisions consume it. All 3 product decisions decide (2, 65, and 4 targets); the 71 technical decisions carry no `decides` by design and govern code instead, where 8 already draw the checked `decision-unfollowed` finding and 15 draw advisory `decision-without-basis`. One of 30 problems is solved by no feature, deliberately answered by refusal. Exactly one actor, `actor/report-recipient`, is named by nothing at all, a genuine but minor orphan. Fourteen of 164 intent-layer terms are cited by no record, which is tolerated. Requirement-page container conformance could not be judged from the model, because the builder emits zero `requirement-page` records; it needs the consumer validation run, not this record.

The investigation tested the owner's recommendation clause by clause. The requirement origin clause is supported with one amendment: keep `feature` and `basedOn` distinct in the wording, report the anchor found as one finding, count a decision-settled requirement as anchored, and keep counting refused-feature anchors. The feature clause is supported for `solves` now, since all 40 live features already comply, but `serves` should stay advisory: with only eight goals, forcing one goal per feature manufactures vague claims, so the expectation should attach where it informs sequencing, on risk-carrying or dated features. The journey goal clause is argued against as a replacement and supported as an added optional link, because the sentence in words is what lets a reviewer approve a journey chapter without opening a goal page. Advice-first checks are supported with a written promotion trigger: zero advisory findings on approved records, checks landing one slice at a time, and promotion through the known-gap mechanism with an end date.
