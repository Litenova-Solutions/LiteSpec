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
      "url": "https://technav.ieee.org/topic/requirements-management/",
      "readOn": "2026-10-30"
    },
    {
      "url": "https://technav.ieee.org/topic/technical-requirements/",
      "readOn": "2026-10-30"
    },
    {
      "url": "https://www.betterask.erni/the-need-for-requirements-traceability-within-development-projects/",
      "readOn": "2026-10-30"
    },
    {
      "url": "https://www.jamasoftware.com/requirements-management-guide/aerospace-and-defense/do-178c",
      "readOn": "2026-10-30"
    },
    {
      "url": "https://www.jamasoftware.com/legacy/requirements-management-guide/requirements-traceability/bidirectional-traceability/",
      "readOn": "2026-10-30"
    },
    {
      "url": "https://help.jamasoftware.com/ah/en/manage-content/coverage-and-traceability/relationships/relationship-status-indicator.html",
      "readOn": "2026-10-30"
    },
    {
      "url": "https://help.jamasoftware.com/ah/en/manage-content/coverage-and-traceability/relationships/clear-suspect-links.html",
      "readOn": "2026-10-30"
    },
    {
      "url": "https://help.jamasoftware.com/ah/en/manage-content/coverage-and-traceability/live-trace-explorer/modify-a-tile-in-the-diagram.html",
      "readOn": "2026-10-30"
    },
    {
      "url": "https://www.vector.com/en/business-unit/code-testing-and-verification/standards-and-compliance/functional-safety-industry-standards/",
      "readOn": "2026-10-30"
    },
    {
      "url": "https://link.springer.com/article/10.1007/s10664-014-9314-z",
      "readOn": "2026-10-30"
    },
    {
      "url": "http://wer.inf.puc-rio.br/WERpapers/artigos/artigos_WER09/werneck.pdf",
      "readOn": "2026-10-30"
    },
    {
      "url": "https://learn.microsoft.com/en-us/azure/devops/boards/queries/link-type-reference?view=azure-devops",
      "readOn": "2026-10-30"
    },
    {
      "url": "https://blogs.sw.siemens.com/polarion/links-between-work-items-and-their-intended-meaning/",
      "readOn": "2026-10-30"
    },
    {
      "url": "https://blogs.sw.siemens.com/polarion/interconnecting-your-engineering-elements/",
      "readOn": "2026-10-30"
    },
    {
      "url": "https://linear.app/docs/parent-and-sub-issues",
      "readOn": "2026-10-30"
    },
    {
      "url": "https://www.modernanalyst.com/Careers/InterviewQuestions/tabid/128/ID/6793/What-is-Impact-Mapping.aspx",
      "readOn": "2026-10-30"
    },
    {
      "url": "https://www.prodpad.com/glossary/opportunity-solution-tree/",
      "readOn": "2026-10-30"
    },
    {
      "url": "https://visual-paradigm.com/cn/guide/agile-software-development/what-is-user-story/",
      "readOn": "2026-10-30"
    },
    {
      "url": "https://qa.whatmatters.com/faqs/okrs-objectives-key-results-explanation-examples",
      "readOn": "2026-10-30"
    },
    {
      "url": "https://www.adammhoward.com/solavirtusinvicta/2013/10/27/traceability-matrix-myth-tricks",
      "readOn": "2026-10-30"
    },
    {
      "url": "https://nkdagility.com/resources/blog/rethinking-product-backlog-navigating-through-the-weeds-of-complexity/",
      "readOn": "2026-10-30"
    }
  ]
}
---

# Established Traceability Practice

## Answer

This record distils the established traceability practice investigation of 2026-10-30 into the claims this release relies on. Its web sources were read on 2026-10-30, in full except where Notes on Sources says otherwise.

ISO/IEC/IEEE 29148:2018 is the standard the IEEE navigator names for requirements elicitation, analysis, specification, and verification, and its requirements pages state the traceability rule directly: each requirement links back to its origins in stakeholder need and forward to the design elements, test cases, and delivered components that implement and verify it, so that a change's downstream impact is visible before approval. The consequence for LiteSpec is direct: every element needs a parent above and a child below, or the gap must be visible. IREB CPRE uses the same shape: traceability is the ability to trace a requirement back to its origins, forward to its implementation and tests, and to the requirements it depends on, claiming impact analysis, avoidance of unnecessary requirements, coverage, and progress measurement. LiteSpec matches that shape in its forward links and is stricter on quality-requirement scenarios, but its origin link is the gap: `basedOn` exists while `feature` is defined narrowly, so an ordinary shipped-feature requirement has no named origin, which both sources would call orphaned.

The regulated regimes gate the chain rather than advise it. DO-178C traces requirements to code to tests, requires every line of code and every test to be justified by a requirement with untraceable code treated as extraneous, and scales rigor through Design Assurance Levels A to E, with suspect links (a changed item whose neighbors are not re-reviewed) as a first-class state. ISO 26262 and IEC 62304 mandate bidirectional traceability with the same update discipline: analyze the full impact before approving a change, then propagate updates to all traced artifacts. LiteSpec already implements the regulated shape below the requirement line, and computed staleness is suspect-link semantics done automatically, but it leaves the top optional where the regimes refuse to: the 719 of 928 consumer requirements that name no origin would fail a DO-178C audit, since implementation with no linked requirement must not slip into a certified build. LiteSpec needs no DAL grading; its risk tiers are the proportionate substitute.

The goal methods confirm the shape and name one missing middle. KAOS and `i*` divide the work: KAOS refines strategic goals into requirements assigned to responsible agents, `i*` captures why actors depend on each other, and neither leaves a goal free-floating. LiteSpec's `except` and `fallback` already cover obstacles in product words, so refinement logic must not be imported. Impact mapping's four levels (goal, actor, impact, deliverable) expose LiteSpec's missing middle: there is no impact record stating the behavior change a feature aims to cause, though a journey's `finishedWhen` comes close. Opportunity solution trees match strongly (outcome, opportunity, solution, experiment), and LiteSpec is better at the experiment level, where scenarios are executable proofs. User story mapping's backbone already exists and is checked by `step-use-case-missing`. OKRs match the goal shape near-identically, but goals carry no authority in LiteSpec, so scoring them would be theatre.

The tools agree on four mechanics: store the link on one side and compute the inverse, enforce at most one parent in the tree while allowing many non-tree links, compute orphan status from per-type rules, and provide a tolerance (exclude from coverage, suspect, known states) so that no parent is a visible judgement rather than always an error. Jama computes orphan and missing-downstream status from administrator-set relationship rules, marks downstream items suspect when an upstream item changes, and lets teams exclude item types and relationship rules from coverage; Polarion derives semantic link pairs from one written link; Azure enforces one parent link per work item on a tree topology that prevents circular references; Linear breaks parent issues into sub-issues that inherit team, priority, and project, with optional auto-close when all sub-issues are done. LiteSpec already does the first (links live on the record that points), the fourth (known gaps and deviations with end dates), and the second via identifier containment, which cannot drift; its gap is the third above the requirement line, where no orphan is computed at all.

The skeptic search sets the qualifications. The benefit is shown, not automatic: in the one controlled experiment found, subjects given traceability performed on average 24 percent faster and produced on average 50 percent more correct solutions across real maintenance tasks on two third-party projects, with 71 subjects. Maintenance is the known failure mode: teams neglect it as a nuisance, so any LiteSpec link rule must be checkable by the builder on every run. Traceability theatre is real: a matrix that pairs tests to requirements without the reasoning behind the pairings contains no useful information yet is perceived as complete, which LiteSpec mitigates structurally through mandatory rationales and typed links, and which fails exactly where a field is optional and empty. Deep trees cost agility: a five-level backlog hierarchy reduces transparency and adaptability, so requirements must not become hostage to feature gardening, and the origin rule must accept `basedOn` or a deciding decision beside `feature`. Four genuine limits follow: refused features must stay exempt, cross-cutting constraints legitimately parent many requirements, background work needs an escape, and small projects should not face the new gates.

The investigation ranks seven adoptable rules: every requirement names one origin (feature, `basedOn`, or a deciding decision); the journey goal becomes a goal link; every non-refused feature names `solves` and `serves`; Intent orphans are computed advisory-only with named tolerances; one written parent per child with every inverse computed; strictness graded by risk; and changed records render downstream proofs suspect in the book. Rejected with evidence: AND/OR refinement, OKR scoring, mandatory leaf-to-leaf links, and a feature-parent-only rule.

## Notes on Sources

The IREB glossary wording is second-hand through the ERNI practitioner article (published 2022-10-12, updated 2024-06-03, quoting Glinz 2022), because IREB's own glossary was not read in this investigation; the judge may wish to confirm the wording against the IREB glossary PDF before LiteSpec cites it in a change. The regulated-regime claims rest on vendor restatements (Jama, Vector), not the paywalled standards. The live IEEE navigator pages state the traceability rule without the three-level StRS, SyRS, and SRS wording the source investigation reported, so the Answer follows the live pages. The Mader and Egyed experiment numbers come from the abstract, read in full, with the full text paywalled; they replace the task counts the source investigation reported, which the abstract does not support. The Werneck comparison paper was read through its `i*` and KAOS structure and comparison sections. The Linear page states no nesting depth limit and no completion counting, so the Answer follows the live page. The source investigation's claim that Jama's help concedes coverage dashboards go stale could not be confirmed on the help pages read, so the Answer no longer makes it. The method summaries for impact mapping, opportunity solution trees, story mapping, and OKRs are practitioner and vendor pages, not the originators' books.
