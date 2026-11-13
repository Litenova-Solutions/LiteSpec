# Established Traceability Practice

Traceability links each requirement to the items it came from and to the items that implement and test it. This research record reviews established practice in requirements management, safety standards, goal methods and tracking tools. It records what that practice requires of the top of the chain, which is the reason a feature exists, and where it warns against requiring it. It matters because LiteSpec must decide how strict its links above the requirements should be, and the largest consumer's measured gap sits exactly there.

## Findings

Established practice requires every requirement to name an origin, and it computes every inverse link. It warns that links alone do not give traceability. The links need named tolerances, a rule for when a check becomes enforced, and reasons behind them.

ISO/IEC/IEEE 29148:2018 is the standard that the IEEE navigator names for requirements work. Its pages state the rule directly. Each requirement links back to its origins in stakeholder need, and forward to the design elements, test cases and delivered components that implement and verify it. A change's downstream impact is then visible before it ships. For LiteSpec, every element needs a parent above and a child below, or the gap must be visible. The record gives no confidence for this claim.

IREB CPRE uses the same shape. Traceability is the ability to trace a requirement back to its origins, forward to its implementation and tests, and to the requirements it depends on. It claims impact analysis, avoidance of unnecessary requirements, coverage and progress measurement. The record gives no confidence for this claim. Its IREB wording is second-hand, as Source Notes states.

LiteSpec matches that shape in its forward links, and it is stricter on quality-requirement scenarios. Its origin link is the gap. `basedOn` exists, but `feature` is defined narrowly. So an ordinary shipped-feature requirement has no named origin, and both sources would call it orphaned. The record gives no confidence for this claim.

The regulated regimes require the chain rather than advise it. DO-178C traces requirements to code and to tests. It requires every line of code and every test to be justified by a requirement, and it treats untraceable code as extraneous. It scales rigor through Design Assurance Levels A to E, and it treats a suspect link as a state of its own. A suspect link is a changed item whose neighbors have not been reviewed again. The record gives no confidence for this claim.

ISO 26262 and IEC 62304 require bidirectional traceability with the same discipline. A team analyzes the full impact before it accepts a change, and then propagates the update to every traced artifact. The record gives no confidence for this claim.

LiteSpec already implements the regulated shape below the requirement line. Its computed staleness works like suspect-link semantics, done automatically. The regimes require the top of the chain as well, and LiteSpec leaves it optional. The record says that 719 of 928 consumer requirements, which name no origin, would fail a DO-178C audit, because implementation with no linked requirement must not enter a certified build. LiteSpec needs no Design Assurance Level grading. The record gives no confidence for this claim.

The goal methods confirm the shape and name one missing middle. KAOS refines strategic goals into requirements assigned to responsible agents. `i*` captures why actors depend on each other. Neither method leaves a goal free-floating. LiteSpec's `except` and `fallback` already cover obstacles in product words, so LiteSpec should not import refinement logic. The record gives no confidence for this claim.

Impact mapping has 4 levels: goal, actor, impact and deliverable. It exposes LiteSpec's missing middle. No impact record states the behavior change that a feature aims to cause, although a journey's `finishedWhen` comes close. The record gives no confidence for this claim.

Opportunity solution trees match LiteSpec well, with outcome, opportunity, solution and experiment. LiteSpec is stronger at the experiment level, because its scenarios are executable proofs. User story mapping's backbone already exists in LiteSpec, and the check `step-use-case-missing` covers it. OKRs match the goal shape almost exactly. Goals carry no authority in LiteSpec, so scoring them would give numbers that have no authority there. The record gives no confidence for these claims.

The tools agree on 4 mechanics. They store each link on one side and compute the inverse. They allow at most one parent in the tree and many non-tree links. They compute orphan status from rules for each type. They provide a tolerance, such as excluding items from coverage, marking them suspect or recording an exception. So no parent becomes a visible judgement rather than always an error. The record gives no confidence for this claim.

Jama computes orphan and missing-downstream status from relationship rules that an administrator sets. It marks downstream items suspect when an upstream item changes. Polarion derives semantic link pairs from one written link. Azure DevOps allows one parent link per work item on a tree that prevents circular references. Linear splits a parent issue into sub-issues that inherit the team, priority and project, with an optional auto-close. The record gives no confidence for these tool claims.

LiteSpec already does the first mechanic, because links live on the record that points. It does the fourth, because its exceptions carry end dates. It does the second through identifier containment, which cannot drift. Its gap is the third mechanic above the requirement line, where no orphan is computed at all. The record gives no confidence for this claim.

The benefit of traceability is shown, not automatic. In the one controlled experiment that the record found, subjects with traceability performed on average 24 percent faster. They also produced on average 50 percent more correct solutions. The tasks were real maintenance tasks on 2 third-party projects, with 71 subjects. The record gives no confidence for this claim, and Source Notes states what it read.

Maintenance is the known failure mode, because teams neglect links as a nuisance. So any LiteSpec link rule must be checkable by the builder on every run. A matrix that pairs tests with requirements, but holds no reasoning, looks complete and holds no useful information. LiteSpec limits that risk with mandatory rationales and typed links. The matrix fails most where a field is optional and empty. The record gives no confidence for these claims.

Deep trees cost agility. A 5-level backlog hierarchy reduces transparency and adaptability. So requirements must not become hostage to feature gardening, and the origin rule must accept `basedOn` or a deciding decision beside `feature`. The record names 3 genuine limits. Cross-cutting constraints legitimately parent many requirements. Background work needs an escape. Small projects should not face the new gates. The record gives no confidence for these claims.

The investigation ranks 6 rules that LiteSpec can adopt. Every requirement names one origin: a feature, a `basedOn` entry or a deciding decision. The journey gets a goal link. Every feature names `solves` and `serves`. Intent orphans are computed as advisory only, with named tolerances. Each child has one written parent, and every inverse is computed. Changed records render their downstream proofs suspect in the book. The record states no confidence for these rules.

The record rejects 4 approaches with evidence. They are AND/OR refinement, OKR scoring, mandatory leaf-to-leaf links and a rule that only features may be parents. The record gives no confidence for these rejections.

## Source Notes

The web sources were read on 30 October 2026. They were read in full, except where the notes below say otherwise. The sources include the IEEE navigator topic pages on requirements management and technical requirements, and an ERNI practitioner article on IREB traceability. They also include Jama Software guidance on DO-178C and bidirectional traceability and on relationship status, and a Vector page on functional safety standards.

The sources also include the Mader and Egyed experiment abstract in Springer's software quality journal, a comparison paper on `i*` and KAOS from PUC-Rio, and Microsoft's Azure DevOps link type reference. Other sources are 2 Siemens Polarion blog posts, the Linear documentation on parent and sub-issues, and a Modern Analyst page on impact mapping. They also include a ProdPad glossary entry on opportunity solution trees, a Visual Paradigm guide on user stories, a What Matters page on OKRs, a blog post on the traceability matrix, and an nKdAgility post on the product backlog. The record does not say which claim each source supports, beyond the topics above.

The IREB glossary wording is second-hand, through the ERNI article published on 12 October 2022 and updated on 3 June 2024. That article quotes Glinz 2022. The record did not read IREB's own glossary, so the wording still needs checking against the IREB glossary PDF before a LiteSpec change cites it.

The regulated-regime claims rest on vendor restatements from Jama and Vector, not on the paywalled standards. The IEEE navigator pages state the traceability rule without the 3-level StRS, SyRS and SRS wording that the source investigation reported. So the record follows the live pages. The Mader and Egyed numbers come from the abstract, because the full text is paywalled. They replace the task counts that the source investigation reported, which the abstract does not support.

The record could not confirm that Jama's help concedes that coverage dashboards go stale, so it drops that claim. The method summaries for impact mapping, opportunity solution trees, story mapping and OKRs come from practitioner and vendor pages, not from the originators' books. The Linear page states no nesting depth limit and no completion counting, so the record follows that page.
