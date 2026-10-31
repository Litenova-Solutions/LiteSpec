# Outside Review: The LiteSpec Intent Hierarchy and Its Names

Reviewed on 2026-10-30 against branch `v3` at commit `8c03938` plus its uncommitted edits. Subject: the Intent layer of LiteSpec 3.0.0, a documentation standard for agent-written business applications. Read: `docs/core/intent.md`, `docs/core/model.md` (Parents, Links, Identifiers), `docs/core/layers.md`, the Intent templates in `templates/consumer/`, `docs/intent/research/intent-hierarchy-evidence.md` and `docs/intent/decisions/0002-parent-for-every-record.md`.

The reviewer read the Intent page as a newcomer first. The places a newcomer got lost are the findings below.

## Findings

### F1. Requirements are grouped three different ways, so one feature's story is spread over three files

- **Severity and confidence:** major, confirmed.
- **Location:** `docs/core/model.md:93-94` (requirement entries live on `docs/intent/<area>/<topic>.md`), `docs/core/model.md:244-249` (a feature's parent is a problem, a journey's parent is a goal, a requirement's parent is its first `features` entry).
- **Evidence:** a requirement page groups commitments by topic area; a feature groups an offer under a problem; a journey groups steps under a goal. The parent of a requirement is the feature, but the file holding it is the topic page, and the journey that walks it sits under a third record. The largest consumer shows authors follow the page, not the parent: "719 of 928 requirements name neither `feature` (56 do) nor `basedOn`" (`docs/intent/research/intent-hierarchy-evidence.md:30`), and "all 112 goal sentences are distinct and none contains any goal record's title" (same file, `:32`).
- **Peer practice:** GitHub Spec Kit keeps one `spec.md` per feature holding the user scenarios, numbered functional requirements and success criteria together ([mintlify mirror of spec-kit templates, 2025-10](https://www.mintlify.com/github/spec-kit/advanced/templates), secondary). OpenSpec keeps one spec per capability, each holding `### Requirement:` blocks with `#### Scenario:` examples ([openspec.dev core concepts, 2025](https://openspec.dev/docs/core-concepts)). Kiro keeps one `requirements.md` per spec with user stories and EARS acceptance criteria ([kiro.dev specs concepts, 2025](https://kiro.dev/docs/specs/concepts)). All three group by the unit the author writes in.
- **Consequence:** a reader cannot answer "what did we promise for this offer, and how does a person live it" from one place, and the parent rule produces hundreds of missing-parent findings the authors did not experience as mistakes.
- **Fix:** make the topic page the parent of its requirements by containment (the identifier `requirement/<topic>.<name>` already names it), turn `features`, `basedOn` and `decides` into connections, and keep a coverage check that each requirement names at least one reason. Touches `docs/core/model.md` Parents table, `docs/core/intent.md#identify-every-requirement`, `schemas/record.schema.json` (`requirementPage` gains `id`, `owner`, `status`), the checker's parent resolution, and decision 0002.

### F2. The requirement container is the only Intent page style, and it is named unlike every other container

- **Severity and confidence:** minor, confirmed.
- **Location:** `requirement-page` (`templates/consumer/requirement-page.md`) beside Behavior's `setting-set`, `rule-set`, `entry-point-list`, `permission-list`.
- **Evidence:** four containers, three suffixes (`-page`, `-set`, `-list`). The folder is called an area and the identifier segment a topic (`docs/core/model.md:93`, `:131`).
- **Consequence:** a newcomer asks why requirements get a page kind and nothing else does; the answer (requirements are small entries that share one story) is never stated.
- **Fix:** one container suffix across the standard, one word for area and topic, and one sentence on the Intent page saying why requirements are entries.

### F3. "Business requirement" means a goal everywhere else

- **Severity and confidence:** major, confirmed.
- **Location:** `docs/core/intent.md:27`, `:46`; `record.schema.json` requirement `kind` enum.
- **Evidence:** LiteSpec's `kind: business` is "what the business holds, with dated values", such as a receipt threshold or a price. ISO/IEC/IEEE 29148:2018 names the Business Requirements Specification as the business or mission level above stakeholder requirements ([INCOSE IS2013 presentation on 29148](https://www.incose.org/wp-content/uploads/legacy/presentation/IS2013-pdf/3-3-1.pdf); [ReqView 29148 templates](https://reqview-homepage.netlify.app/doc/iso-iec-ieee-29148-templates)). Wiegers and Beatty, Software Requirements, 3rd ed. (2013) use business requirements for why the organization builds the system [assumption: from the book, not re-read for this review].
- **Consequence:** an analyst reading "business requirement" expects an objective like a goal, and finds a dated figure.
- **Fix:** rename the kind to `policy` (a dated value or rule the business sets). Touches the schema enum, `intent.md`, the requirement-page template, the glossary and the book labels.

### F4. Saying no lives in three places

- **Severity and confidence:** minor, confirmed.
- **Location:** product `neverDoes` (`intent.md:92`), feature `lifecycle: refused` with `refusedBecause` (`intent.md:124`), and the requirement page's Refusals section (`intent.md:184`).
- **Consequence:** an author does not know where a "we won't" goes, and "Refusals" also names Behavior's use-case endings (`docs/reference/glossary.md:551`).
- **Fix:** product-wide boundaries in `neverDoes`, a declined offer as a refused feature, and drop the Refusals section, or rename it Out of Scope.

### F5. A constraint restates its own requirements in prose, and nothing checks that an applying constraint is answered

- **Severity and confidence:** major, confirmed.
- **Location:** constraint section What It Requires (`templates/consumer/constraint.md`); requirement `basedOn` (`intent.md:194`).
- **Evidence:** the product's response to a constraint is written twice: as prose under What It Requires and as requirement entries that cite the constraint in `basedOn`. In the largest consumer "5 of 29 constraints ... are cited by no `basedOn`" (`docs/intent/research/intent-hierarchy-evidence.md:34`). Volere treats a mandated constraint as a requirement with its own rationale and fit criterion ([Volere template v9, University of Edinburgh copy](https://www.inf.ed.ac.uk/teaching/courses/seoc/2004_2005/resources/volere-template.pdf)), so the response is always measurable.
- **Consequence:** an applying constraint can sit with a prose answer that no Behavior record fulfills and no check sees.
- **Fix:** make What It Requires a generated region listing the requirements whose `basedOn` names the constraint, and add a check that a constraint with `applies: true` is cited by at least one requirement.

### F6. A requirement has three origin fields with a precedence rule, where one ordered list would do

- **Severity and confidence:** minor, confirmed.
- **Location:** `docs/core/model.md:235`, `:248`.
- **Evidence:** origin is the first `features` entry, else the first `basedOn` entry, else a decision that names the requirement in `decides`. A goal cannot be a requirement's reason at all.
- **Peer practice:** goal-oriented requirements engineering (KAOS) refines goals directly into requirements; impact mapping traces deliverables to a goal through actor and impact ([TechTarget on impact mapping](https://www.techtarget.com/searchsoftwarequality/definition/impact-mapping)).
- **Fix:** with F1, one `basedOn` list naming any reason (goal, problem, feature, constraint, research, decision), with no precedence rule because the page is the parent.

### F7. A system journey is not a journey

- **Severity and confidence:** minor, likely (confirm by checking whether any consumer journey uses a system actor).
- **Location:** `docs/core/intent.md:58-60` (`actor/recurring-job` erasing personal data past retention).
- **Peer practice:** story mapping and use-case narratives walk a person through activities toward their goal ([Aha on user story mapping](https://www.aha.io/roadmapping/guide/release-management/what-is-user-story-mapping)); scheduled work is a requirement with a measure.
- **Consequence:** the word "journey" stretches to cover cron work, and the founder approves a walk nobody takes.
- **Fix:** keep person journeys (employee and staff), and state scheduled promises as requirements that jobs fulfill.

### F8. A journey states its goal twice

- **Severity and confidence:** minor, confirmed.
- **Location:** `aim` and `serves` on the journey (`intent.md:54`).
- **Evidence:** the consumer's 112 aims match no goal title (F1 evidence), so the sentence and the link drift apart.
- **Fix:** drop `aim`, or define it as the journey's one-line summary rather than its goal.

### F9. The Intent page never says why each record type exists or when not to use it

- **Severity and confidence:** major, confirmed.
- **Location:** the term table at `docs/core/intent.md:17-32` gives one line per type and no reason.
- **Peer practice:** each comparable method is taught as a chain of questions: Why, Who, How, What in impact mapping; outcome, opportunity, solution in the opportunity solution tree ([Product Talk glossary](https://www.producttalk.org/glossary-discovery-opportunity-solution-tree/)).
- **Consequence:** a newcomer sees eleven similar record types with no way to tell which to write first or which they can skip.
- **Fix:** add a Concepts table with columns Question, Answered by, Write it when, Skip it when, and the industry name.

### F10. Traceability above the requirements costs more than it returns today

- **Severity and confidence:** note.
- **Evidence:** studies of why teams do not trace cite effort without direct benefit and links going stale ([Ruiz et al., Requirements Engineering 2023](https://link.springer.com/10.1007/s00766-023-00408-9)); the common answer is to scale traceability to risk. LiteSpec already has risk tiers.
- **Fix:** keep strict parents where checks read them (requirement, journey step), and make links above them advisory except on tier-1 records.

## Scope

Examined the Intent pages, schema and templates listed above and the largest consumer's measured counts as recorded in the repository's own research. The CLI was not run. Not examined: the Behavior layer, except where Intent links into it.

## What Already Works

- The split between promise (Intent) and mechanism (Behavior), and the rule that Behavior points up.
- Journey steps as product sentences with stable ids, so use cases and screens can point at a step.
- Constraint and research as separate records: a constraint binds, research only informs.
- Decisions as replace-never-edit records with options, which matches the architecture decision record practice.
- Quality requirements as stimulus, environment, response and measure, which matches the quality attribute scenario practice.

## Sources

| Source | Date | Why it was used |
|:---|:---|:---|
| [GitHub Spec Kit templates (mintlify mirror)](https://www.mintlify.com/github/spec-kit/advanced/templates) | 2025 | Agent-driven spec tool; one spec per feature. Secondary source |
| [OpenSpec core concepts](https://openspec.dev/docs/core-concepts) | 2025 | Agent-driven spec tool; requirement and scenario per capability |
| [Kiro specs concepts](https://kiro.dev/docs/specs/concepts) | 2025 | Agent-driven spec tool; requirements with EARS criteria |
| [Product Talk: opportunity solution tree](https://www.producttalk.org/glossary-discovery-opportunity-solution-tree/) | 2021 onward | Outcome, opportunity, solution chain |
| [TechTarget: impact mapping](https://www.techtarget.com/searchsoftwarequality/definition/impact-mapping) | undated | Goal, actor, impact, deliverable chain |
| [INCOSE IS2013 on ISO/IEC/IEEE 29148](https://www.incose.org/wp-content/uploads/legacy/presentation/IS2013-pdf/3-3-1.pdf) | 2013, still the structure of the 2018 edition | Business, stakeholder, system requirement levels |
| [Volere template v9](https://www.inf.ed.ac.uk/teaching/courses/seoc/2004_2005/resources/volere-template.pdf) | 2004 copy; template unchanged in substance | Constraints with fit criteria |
| [Ruiz et al., Why don't we trace?](https://link.springer.com/10.1007/s00766-023-00408-9) | 2023 | Evidence on traceability cost |

## Local Rules Met

- `litespec/core/model#give-every-record-one-parent`: F1 and F6 propose a different parent for requirements, which conflicts with decision 0002 as drafted.
- `litespec/core/authoring#use-one-term-for-one-concept`: F2, F3 and F4 apply it.
- AGENTS.md "never name a consumer project": this review says "the largest consumer".

## Open Questions

- Do most of the largest consumer's requirements belong to one feature, or are they area-wide? The 56 of 928 with a feature suggests area-wide; a per-page count would settle it.
- Is the requirement page meant to be read by people as a chapter (as `intent.md:184` says), or only rendered by the book? The answer decides whether it becomes a record.
