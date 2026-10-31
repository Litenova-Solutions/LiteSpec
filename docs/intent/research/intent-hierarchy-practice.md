---
{
  "type": "research",
  "id": "research/intent-hierarchy-practice",
  "title": "Intent Hierarchy Practice",
  "owner": "LiteSpec maintainer",
  "status": "draft",
  "decider": "LiteSpec maintainer",
  "decideBy": "2026-11-30",
  "question": "How should Intent group its requirements, name their reasons, and say no, and what do outside tools and the largest consumer show?",
  "conclusion": "Outside tools group each promise with the unit its author writes, while the largest consumer shows authors follow the page rather than the parent, with 719 of 928 requirements naming no reason, so the topic becomes the parent record, reasons become one cites list, and saying no keeps two homes.",
  "sources": [
    {
      "url": "https://www.mintlify.com/github/spec-kit/advanced/templates",
      "readOn": "2026-10-30"
    },
    {
      "url": "https://openspec.dev/docs/core-concepts",
      "readOn": "2026-10-30"
    },
    {
      "url": "https://kiro.dev/docs/specs/concepts",
      "readOn": "2026-10-30"
    },
    {
      "url": "https://www.producttalk.org/glossary-discovery-opportunity-solution-tree/",
      "readOn": "2026-10-30"
    },
    {
      "url": "https://www.techtarget.com/searchsoftwarequality/definition/impact-mapping",
      "readOn": "2026-10-30"
    },
    {
      "url": "https://www.incose.org/wp-content/uploads/legacy/presentation/IS2013-pdf/3-3-1.pdf",
      "readOn": "2026-10-30"
    },
    {
      "url": "https://reqview-homepage.netlify.app/doc/iso-iec-ieee-29148-templates",
      "readOn": "2026-10-30"
    },
    {
      "url": "https://www.inf.ed.ac.uk/teaching/courses/seoc/2004_2005/resources/volere-template.pdf",
      "readOn": "2026-10-30"
    },
    {
      "url": "https://www.aha.io/roadmapping/guide/release-management/what-is-user-story-mapping",
      "readOn": "2026-10-30"
    },
    {
      "url": "https://link.springer.com/10.1007/s00766-023-00408-9",
      "readOn": "2026-10-30"
    },
    {
      "path": "docs/intent/research/intent-hierarchy-evidence.md",
      "readOn": "2026-10-31"
    },
    {
      "path": "docs/core/intent.md",
      "readOn": "2026-10-31"
    },
    {
      "path": "docs/core/model.md",
      "readOn": "2026-10-31"
    },
    {
      "path": "schemas/record.schema.json",
      "readOn": "2026-10-31"
    }
  ]
}
---

# Intent Hierarchy Practice

## Answer

This record distils the outside review of 2026-10-30, ten findings numbered F1 to F10, and the measured consumer counts behind decisions D1 to D8 into the claims this release relies on. The consumer counts were measured over the largest consumer's built model on 2026-10-30, holding 21,548 records with 928 requirements, and are carried here from the Intent hierarchy evidence record. The web sources below carry the review's read date of 2026-10-30; Notes on Sources states what was re-read and what was carried.

Requirements are grouped three different ways, so one feature's story is spread over three files. A requirement page groups commitments by topic area, a feature groups an offer under a problem, and a journey groups steps under a goal, while the parent of a requirement is the feature, the file holding it is the topic page, and the journey that walks it sits under a third record. The three agent-driven peers all group by the unit the author writes in: GitHub Spec Kit keeps one spec file per feature holding the user scenarios, numbered functional requirements, and success criteria together; OpenSpec keeps one spec per capability holding Requirement blocks with Scenario examples; Kiro keeps one requirements file per spec with user stories and EARS acceptance criteria. The largest consumer shows authors follow the page, not the parent: 719 of 928 requirements name neither feature (56 do) nor basedOn, and all 112 goal sentences are distinct while none contains any goal record's title. A reader cannot answer what was promised for one offer, and how a person lives it, from one place, and the parent rule produces hundreds of missing-parent findings the authors did not experience as mistakes. The topic page therefore becomes the parent of its requirements by containment, since the identifier requirement/<topic>.<name> already names it, and a requirement whose topic segment differs from its page's name is an identifier-invalid finding. Confidence is high: the peer shape is read from the tools' own concept pages, and the consumer counts are measured, not sampled.

A requirement has three origin fields with a precedence rule, where one ordered list would do. The origin is the first features entry, else the first basedOn entry, else a decision that names the requirement in decides, and a goal cannot be a requirement's reason at all. Goal-oriented practice refines goals directly into requirements: KAOS refines goals into requirements, and impact mapping traces deliverables to a goal through actor and impact. With the topic page as parent, the precedence rule has no work left, so a requirement names its reasons in one cites list holding any of goal, problem, feature, constraint, research, and decision, in no order of precedence. A requirement that cites nothing is requirement-cites-missing, tier A, and tier G when the requirement carries risk. The container follows the same fix: four containers carry three suffixes today, and the folder is called an area while the identifier segment is a topic, so the requirement container becomes the topic record type, the area stays a folder only, and one sentence on the Intent page says why requirements are entries.

Fulfilment, constraint answers, and saying no each get one home. A journey alone clears requirement-fulfillment-missing today, because the checker counts rule, use case, and journey as fulfilling, so only Behavior records fulfil a requirement from here: rule, use case, reaction, job, notification, and setting, while a journey names the requirements it shows in cites. A constraint's answer is written twice, as prose under What It Requires and as requirement entries citing the constraint, and 5 of 29 constraints in the largest consumer are cited by none, so What It Requires becomes a generated region listing the requirements that cite the constraint, and a constraint with applies true that no requirement cites is constraint-unreached at tier G. Volere supports the shape by treating a mandated constraint as a requirement with its own rationale and fit criterion, so the response stays measurable. Saying no lives in three places today, the product neverDoes, the refused feature, and the requirement page Refusals section, while Refusals also names Behavior use-case endings, so the product's exclusions hold product-wide limits, a refused feature holds a declined offer, and the Refusals section is removed.

Names follow outside meaning. LiteSpec's kind business is dated values such as a receipt threshold or a price, while ISO/IEC/IEEE 29148 names the Business Requirements Specification as the business or mission level above stakeholder requirements, and Wiegers and Beatty use business requirements for why the organization builds the system, so the kind becomes policy and the constraint kind gains legal for laws. A system journey stretches the word journey over scheduled work nobody walks: story mapping and use-case narratives walk a person through activities toward a goal, so journeys walk a person only, scheduled promises become requirements that jobs fulfil, a journey drops aim since its 112 sentences match no goal title and drift from serves, and a step's sentence becomes statement with its failure text as failure.

The Intent page never says why each record type exists or when not to use it: eleven similar types with one line each and no write-when, skip-when, or familiar name. Each comparable method is taught as a chain of questions, Why, Who, How, What in impact mapping and outcome, opportunity, solution in the opportunity solution tree, so the Concepts section opens with a table of the questions Intent answers, with Question, Record, Write It When, Skip It When, and Familiar As columns. Tracing above the requirements costs more than it returns today: teams skip tracing for effort without direct benefit and links going stale, and the common answer is to scale traceability to risk, which LiteSpec already carries as risk tiers. Strict parents stay where checks read them, the requirement and the journey step, and links above them stay advisory except on tier-1 records.

## Notes on Sources

The Spec Kit wording is second-hand through a mintlify mirror and stays marked secondary. The Wiegers and Beatty usage comes from the reviewer's memory of the book and was not re-read for the review. The 29148 level names rest on INCOSE conference slides and ReqView template pages, not the paywalled standard. The Volere claims rest on a 2004 university copy of the version 9 template. The method summaries for impact mapping, opportunity solution trees, and story mapping are practitioner and vendor pages, not the originators' books. The traceability-cost claims come from the Ruiz et al. 2023 page. The consumer counts are second-hand through the Intent hierarchy evidence record, measured 2026-10-30. The web entries above carry the review's read date because the review read them; this record re-read the repository paths on 2026-10-31 and carried the web citations forward.
