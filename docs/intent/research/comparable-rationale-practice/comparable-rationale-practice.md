# Comparable Rationale Practice

A design rationale explains why a rule exists, what it costs and what remains open. This research record compares how 5 comparable templates and processes keep that rationale. The 5 are Python's PEP 1, the Rust RFC template, the Kubernetes KEP template, MADR and RFC 6855. LiteSpec uses the comparison to decide where its own rationale should live, beside the rules it produces. It matters because a rule read without its reasons is harder to judge.

## Findings

Comparable projects keep rationale in numbered, dated records that are never edited, and they keep it beside the normative text. Each record states its motivation, its drawbacks and its unresolved questions. The specification remains the only normative text.

PEP 1 requires Rationale and Rejected Ideas sections. It also counts a draft's header among the parts of the proposal. So the reasoning travels inside the proposal from its first draft. The record gives high confidence for this claim, because it read each template or document on the read date.

The Rust RFC template requires Motivation, Drawbacks, Rationale and Alternatives, and Unresolved Questions. It states that a merged RFC is the shared-agreement text, not a final specification. The record gives high confidence for this claim.

The Kubernetes KEP template requires Risks and Mitigations and Drawbacks. It keeps each KEP as one directory of record beside the process. The record gives high confidence for this claim.

MADR 4.0.0, dated 17 September 2024, records the context, the options with their pros and cons, the chosen outcome, and the positive and negative consequences. It keeps all of them in one numbered file per decision. The record gives high confidence for this claim.

RFC 6855, dated March 2013, keeps its design rationale in a non-normative appendix. The appendix lists the alternatives that were considered, and it stays separate from the protocol text. The record gives high confidence for this claim.

The pattern holds across all 5 sources. Rationale lives in numbered, dated records beside the normative text. Each record states its motivation, its drawbacks and its unresolved questions. The specification stays the only normative surface. The record gives no confidence for this pattern, which it draws from the 5 sources.

The record carries 2 limits. The skeptic search found practitioner commentary on stale decision records, and the record did not read that commentary in full. So the case against keeping rationale rests on the skeptic evidence in `research/established-traceability-practice` and on the drawbacks sections that the templates require. The consumer's record bodies sit outside this repository. Every consumer claim in the companion records comes from files read on 30 October 2026 or from counts measured over its model on the same date. The record gives no confidence for these limits.

## Source Notes

The 5 sources were read in full on 30 October 2026. They are PEP 1 (`peps-python-pep-0001`), the Rust RFC template (`raw-githubusercontent-rust-lang-rfcs-master`), the Kubernetes KEP template (`raw-githubusercontent-kubernetes-enhancements`), MADR (`adr-github-madr`) and RFC 6855 (`rfc-editor-rfc-rfc6855`).

The sources agree on the record shape. They disagree on where the rationale sits and whether the record is final. PEP, Rust, KEP and MADR keep rationale inside the proposal or decision record. RFC 6855 moves it into a non-normative appendix. The record follows the majority and keeps its research and decision records beside its provisions, not inside them.

The record takes the Rust position that a merged RFC is shared agreement, not a final specification. It takes the MADR position of one file per decision, not the KEP position of one directory per proposal. All 5 sources require the author to state drawbacks or rejected alternatives. The record reads that requirement as evidence that rationale records disciplines their authors.

Nygard's original ADR article is LiteSpec's existing citation in `docs/reference/decision-records.md`. The record did not list it as read. It could not confirm the practitioner commentary on stale decision records, because it did not read that commentary in full.
