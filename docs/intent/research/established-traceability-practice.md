---
{
  "type": "research",
  "id": "research/established-traceability-practice",
  "title": "Established Traceability Practice",
  "owner": "LiteSpec maintainer",
  "status": "draft",
  "decider": "LiteSpec maintainer",
  "decideBy": "2026-11-30",
  "question": "What do established requirements and traceability practices require of hierarchy, and where do they warn against requiring it?",
  "conclusion": "Established practice requires every requirement to name an origin, computes every inverse link, and grades strictness by risk, and it warns that links without training, named tolerances, and a promotion rule become theatre rather than traceability.",
  "sources": [
    {
      "url": "https://technav.ieee.org",
      "read": "2026-10-30"
    },
    {
      "url": "https://www.betterask.erni/the-need-for-requirements-traceability-within-development-projects/",
      "read": "2026-10-30"
    },
    {
      "url": "https://jamasoftware.com",
      "read": "2026-10-30"
    },
    {
      "url": "https://help.jamasoftware.com",
      "read": "2026-10-30"
    },
    {
      "url": "https://vector.com",
      "read": "2026-10-30"
    },
    {
      "url": "https://link.springer.com",
      "read": "2026-10-30"
    },
    {
      "url": "https://wer.inf.puc-rio.br",
      "read": "2026-10-30"
    },
    {
      "url": "https://learn.microsoft.com",
      "read": "2026-10-30"
    },
    {
      "url": "https://blogs.sw.siemens.com",
      "read": "2026-10-30"
    },
    {
      "url": "https://linear.app/docs",
      "read": "2026-10-30"
    }
  ]
}
---

# Established Traceability Practice

## Answer

This record distils the established traceability practice investigation of 2026-10-30 into the claims this release relies on. Its web sources were read in full on 2026-10-30, as stated by the source investigation, with the limits carried in Notes on Sources.

ISO/IEC/IEEE 29148 organises requirements in three levels (stakeholder, system, software), each derived from the one above, and IEEE's navigator states the levels should be considered together to ensure consistency and traceability. Bidirectional traceability links lifecycle products and processes to validate that all requirements are satisfied and no unnecessary work is done, and it should be established early and maintained as the project evolves. The consequence for LiteSpec is direct: every element needs a parent above and a child below, or the gap must be visible. IREB CPRE uses the same shape: traceability is the ability to trace a requirement back to its origins, forward to its implementation and tests, and to the requirements it depends on, claiming impact analysis, avoidance of unnecessary requirements, coverage, and progress measurement. LiteSpec matches the requirement levels and is stricter on quality-requirement scenarios, but its origin link is the gap: `basedOn` exists while `feature` is defined narrowly, so an ordinary shipped-feature requirement has no named origin, which both standards would call orphaned.

The regulated regimes gate the chain rather than advise it. DO-178C traces requirements to code to tests, feeds every derived requirement (code behavior with no parent) back up as a new parent, and scales rigor through Design Assurance Levels A to E, with suspect links (a changed item whose neighbors are not re-reviewed) as a first-class state. ISO 26262 and IEC 62304 mandate bidirectional traceability with the same update discipline. LiteSpec already implements the regulated shape below the requirement line, and computed staleness is suspect-link semantics done automatically, but it leaves the top optional where the regimes refuse to: a DO-178C auditor would fail 719 of 928 consumer requirements as derived requirements with no parent. LiteSpec needs no DAL grading; its risk tiers are the proportionate substitute.

The goal methods confirm the shape and name one missing middle. KAOS and i* are complementary: KAOS refines strategic goals into requirements assigned to responsible agents, i* captures why actors depend on each other, and neither leaves a goal free-floating. LiteSpec's `except` and `fallback` already cover obstacles in product words, so refinement logic must not be imported. Impact mapping's four levels (goal, actor, impact, deliverable) expose LiteSpec's missing middle: there is no impact record stating the behavior change a feature aims to cause, though a journey's `finishedWhen` comes close. Opportunity solution trees match strongly (outcome, opportunity, solution, experiment), and LiteSpec is better at the experiment level, where scenarios are executable proofs. User story mapping's backbone already exists and is checked by `step-unserved`. OKRs match the goal shape near-identically, but goals carry no authority in LiteSpec, so scoring them would be theatre.

The tools agree on four mechanics: store the link on one side and compute the inverse, enforce at most one parent in the tree while allowing many non-tree links, compute orphan status from per-type rules, and provide a tolerance (exclude from coverage, suspect, known states) so that no parent is a visible judgement rather than always an error. Jama documents the false-positive problem and its fix; Polarion derives semantic link pairs from one written link; Azure enforces one parent link with circularity counts; Linear nests sub-issues to six layers with completion counting. LiteSpec already does the first (links live on the record that points), the fourth (known gaps and deviations with end dates), and the second via identifier containment, which cannot drift; its gap is the third above the requirement line, where no orphan is computed at all.

The skeptic search sets the qualifications. The benefit is conditional: in the one controlled experiment found, engineers with traceability were faster in 8 of 12 maintenance tasks, but developers forced to reconstruct links worked slower, while those handed the artefact graph worked almost three times faster. Maintenance is the known failure mode: teams neglect it as a nuisance and dashboards go stale, so any LiteSpec link rule must be checkable by the builder on every run. Traceability theatre is real: a link without a reason is compliance decoration, which LiteSpec mitigates structurally through mandatory rationales and typed links, and which fails exactly where a field is optional and empty. Deep trees cost agility: five backlog levels add complexity at each level, so requirements must not become hostage to feature gardening, and the origin rule must accept `basedOn` or a deciding decision beside `feature`. Four genuine limits follow: refused features must stay exempt, cross-cutting constraints legitimately parent many requirements, background work needs an escape, and small projects should not face the new gates.

The investigation ranks seven adoptable rules: every requirement names one origin (feature, `basedOn`, or a deciding decision); the journey goal becomes a goal link; every non-refused feature names `solves` and `serves`; Intent orphans are computed advisory-only with named tolerances; one written parent per child with every inverse computed; strictness graded by risk; and changed records render downstream proofs suspect in the book. Rejected with evidence: AND/OR refinement, OKR scoring, mandatory leaf-to-leaf links, and a feature-parent-only rule.

## Notes on Sources

The IREB glossary wording is second-hand through the ERNI practitioner article (updated 2024-06-03, quoting Glinz 2022), because IREB's own pages timed out on three attempts; the judge may wish to confirm the wording against the IREB glossary PDF before LiteSpec cites it in a change. The regulated-regime claims rest on vendor restatements (Jama, Vector, LDRA), not the paywalled standards; the Vector test-method tables were skimmed, so clause-level claims for ISO 26262 and IEC 62304 rest mainly on the Jama restatement. The Werneck comparison paper rendered partially, with the i* and KAOS structure sections read. The Mader and Egyed experiment numbers come from the abstract and framing, read in full, with the full text paywalled. The method summaries for impact mapping, opportunity solution trees, story mapping, and OKRs are practitioner and vendor pages, not the originators' books.
