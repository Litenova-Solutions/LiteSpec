---
{
  "type": "research",
  "id": "research/comparable-rationale-practice",
  "title": "Comparable Rationale Practice",
  "owner": "LiteSpec maintainer",
  "status": "draft",
  "decider": "LiteSpec maintainer",
  "decideBy": "2026-11-30",
  "question": "How do comparable standards projects keep design rationale so it can be referenced beside the rules it produced?",
  "conclusion": "Comparable projects keep rationale in numbered, dated, never-edited records beside the normative text - each stating its motivation, its drawbacks, and its unresolved questions - while the specification itself remains the only normative surface.",
  "sources": [
    {
      "url": "https://peps.python.org/pep-0001/",
      "read": "2026-10-30"
    },
    {
      "url": "https://raw.githubusercontent.com/rust-lang/rfcs/master/0000-template.md",
      "read": "2026-10-30"
    },
    {
      "url": "https://raw.githubusercontent.com/kubernetes/enhancements/master/keps/0000-kep-template/README.md",
      "read": "2026-10-30"
    },
    {
      "url": "https://adr.github.io/madr/",
      "read": "2026-10-30"
    },
    {
      "url": "https://www.rfc-editor.org/rfc/rfc6855.txt",
      "read": "2026-10-30"
    }
  ]
}
---

# Comparable Rationale Practice

## Answer

This record distils the comparable rationale practice investigation of 2026-10-30 into the claims this release relies on. Its web sources were read in full on 2026-10-30. Nygard's original ADR article is named here as LiteSpec's existing citation in `docs/reference/decision-records.md` and was not listed as read in this investigation.

PEP 1 requires Rationale and Rejected Ideas sections and counts a draft's header among its parts, so the reasoning travels inside the proposal from its first draft. The Rust RFC template requires Motivation, Drawbacks, Rationale and Alternatives, and Unresolved Questions, and it states that a merged RFC is the shared-agreement text, not a final specification. The Kubernetes KEP template requires Risks and Mitigations and Drawbacks and keeps each KEP as one directory of record beside the process. MADR 4.0.0 (2024-09-17) records context, options with pros and cons, the chosen outcome, and its positive and negative consequences in one numbered file per decision. RFC 6855 (March 2013) keeps its design rationale in a non-normative appendix with alternatives considered, separate from the protocol text. Confidence is high: each claim was read from the template or document itself on the read date above.

The pattern holds across all five: rationale lives in numbered, dated, never-edited records beside the normative text, each stating its motivation, its drawbacks, and its unresolved questions, while the specification itself remains the only normative surface. Two limits are carried rather than hidden. The skeptic search returned practitioner commentary on stale decision records that was not read in full, so the case against keeping rationale rests on the skeptic evidence gathered for the traceability practice record and on the templates' own required Drawbacks sections, not on that commentary. And the consumer's record bodies sit outside this repository; every consumer claim in the companion records comes from files read directly on 2026-10-30 or from counts measured over its model on the same date, marked as such where they appear.

## Notes on Sources

The sources agree on the record shape and disagree on placement and finality. PEP, Rust, KEP, and MADR keep the rationale inside the proposal or decision record beside the normative text, while RFC 6855 separates it into a non-normative appendix, so LiteSpec follows the majority by keeping its research and decision records beside its provisions rather than inside them. A merged Rust RFC is explicitly shared agreement rather than a final specification, while a MADR file stands as the decision record itself; LiteSpec takes the Rust side of that split, since its provisions remain the only obligation and the records explain them. KEP keeps one directory of record per proposal while MADR keeps one file per decision; LiteSpec takes the MADR side, with one file per record in consumer-shaped homes. All five require the author to state drawbacks or rejected alternatives, which is the evidence that rationale records discipline their authors rather than merely documenting them.
