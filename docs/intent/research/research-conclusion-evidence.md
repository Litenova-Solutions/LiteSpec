---
{
  "type": "research",
  "id": "research/research-conclusion-evidence",
  "title": "Research Conclusion Evidence",
  "owner": "LiteSpec maintainer",
  "status": "draft",
  "decider": "LiteSpec maintainer",
  "decideBy": "2026-11-30",
  "question": "Do consumer research records state a conclusion a decision can cite, and where does that conclusion live today?",
  "conclusion": "Every consumer research record states its finding in Answer, but sixteen of twenty-nine restate what follows under seven different invented headings and the register index repeats the conclusions outside the records, so the conclusion needs one field of its own.",
  "sources": [
    {
      "path": "templates/consumer/research.md",
      "readOn": "2026-10-30"
    },
    {
      "path": "schemas/record.schema.json",
      "readOn": "2026-10-30"
    },
    {
      "path": "docs/core/intent.md",
      "readOn": "2026-10-30"
    }
  ]
}
---

# Research Conclusion Evidence

## Answer

This record distils Part 1 of the hierarchy release investigation, written 2026-10-30, into the claims this release relies on. It is the first record to carry the new `conclusion` field about its own type. The consumer's research folder (`docs/intent/research/`, 29 records plus its index) was read directly on 2026-10-30.

The consumer research record carries a required frontmatter `question`, a required `sources` array, and one required body section, Answer. Three gaps follow. First, there is no conclusion anywhere a tool can read: Answer is body prose, and the approval hash for research covers only `sources` and `question`, so an approved record's actual finding can be edited without reopening its approval. Second, the book renders only `question` and `sources` for research, so a decision citing research through `basedOn` points at a page whose takeaway no field states. Third, the schema does not enforce the read date the provision already requires: `read` is optional on a source entry. Confidence is high: each gap was read from the schema, the template, the validator, the checker, the approver, and the book field list.

The consumer's 29 records show where the conclusion migrates instead. Every one carries Answer. Sixteen then add a second, conclusion-shaped section under seven drifting names: `What This Means for the Choice` (6 records), `What This Means for Each Choice` (4), and one each of `What This Means for the Model`, a product-named variant, `Conclusion`, `The Position`, and `What This Record Changes`. The short records state their whole conclusion inside Answer and need nothing more; the long records separate what the sources say from what it means for each choice because their Answer runs to many pages. The register index then repeats the conclusions a third time in a beliefs table with a confidence and a required action per record, outside any record, unchecked and unhashed. All 125 source entries already carry a `read` date, 21 records are draft and 8 approved, and 15 of 29 are cited by no other record's `basedOn`, which is tolerated, not defective. The evidence is consistent: the finding lives in Answer, the conclusion migrates to an invented section or an index table, and nothing machine-readable holds it.

The decision this evidence supports adds one required frontmatter field, `conclusion`, to the research page, makes `read` required on every source, and allows exactly one of `url` (a web source) or `path` (a repository-relative evidence path), because repository evidence cannot name a URL honestly. The provision then obliges a research record to state its `question`, state its `conclusion` in one sentence, list each source with its read date, and carry Answer. The checks follow: the consumer fails the page through the schema, the model checker reports a missing conclusion with the other `record-invalid` findings, changing a conclusion reopens approval, and the book renders a Conclusion row with `path` sources as code text. Rejected: a `Conclusion` body section, because seven invented names are the drift a section would codify, and prose stays outside the approval hash; and a record-level `confidence` field, because multi-claim records carry per-claim labels, so one value would falsify them, and confidence stays in Answer claim by claim. The consumer cost is 29 one-sentence fields lifted from existing openings, 8 re-approvals because the new field is semantic, and nothing for the `read` tightening, since 125 of 125 entries already carry one.
