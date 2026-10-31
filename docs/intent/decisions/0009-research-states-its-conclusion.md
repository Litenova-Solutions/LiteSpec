---
{
  "type": "decision",
  "id": "decision/0009-research-states-its-conclusion",
  "title": "Research States Its Conclusion",
  "owner": "LiteSpec maintainer",
  "status": "draft",
  "decider": "LiteSpec maintainer",
  "decideBy": "2026-11-30",
  "options": [
    {
      "name": "A conclusion field plus dated sources",
      "pros": "One sentence as data enters the approval hash, renders in the book, and gives every citing decision a stable takeaway to point at.",
      "cons": "Every research record gains a field to write, and approved records need re-approval because the new field is semantic."
    },
    {
      "name": "A Conclusion body section",
      "pros": "No schema, checker, or approval change is needed.",
      "cons": "Seven invented heading names show the drift a section codifies, and prose stays outside the approval hash and cannot be cited as data."
    },
    {
      "name": "A record-level confidence field",
      "pros": "One comparable value summarizes how far to trust each record.",
      "cons": "One value falsifies multi-claim records, so confidence stays in Answer, claim by claim."
    },
    {
      "name": "Leave research as question plus Answer",
      "pros": "No author writes anything new and no approval reopens.",
      "cons": "An approved record finding stays editable without reopening approval, and the book renders pages whose takeaway no field states."
    }
  ],
  "choice": "A conclusion field plus dated sources",
  "reopenedWhen": "A research kind appears whose takeaway one sentence cannot state.",
  "basedOn": [
    "research/research-conclusion-evidence"
  ]
}
---

# Research States Its Conclusion

## Context

Every research record states its finding in Answer, but what follows migrates to invented sections and index tables nothing machine-readable holds.

## Decision

The research record gains a required one-sentence `conclusion` field, every source carries its read date, and a source names exactly one of a web `url` or a repository-relative evidence `path`.

## Consequences

`litespec/core/intent#record-research-with-read-sources` states the question, the conclusion, the dated sources, and Answer. Changing a conclusion reopens approval, and the book renders the Conclusion beside the question.

## Evidence with Confidence

`research/research-conclusion-evidence` is read from the largest consumer's research folder on 2026-10-30, high confidence on the section drift and the read-date counts.
