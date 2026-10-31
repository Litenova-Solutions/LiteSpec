---
{
  "type": "decision",
  "id": "decision/0010-topic-holds-its-requirements",
  "title": "Topic Holds Its Requirements",
  "owner": "LiteSpec maintainer",
  "status": "draft",
  "decider": "LiteSpec maintainer",
  "decideBy": "2026-11-30",
  "options": [
    {
      "name": "Topic record with cites and generated answers",
      "pros": "One parent by containment ends the three-way grouping, one reason list ends the precedence rule, and generated answers end prose that no check reads.",
      "cons": "Every requirement page migrates to a topic record, and every reason link moves to cites, in one change."
    },
    {
      "name": "Keep the feature parent and add a coverage check",
      "pros": "No page migrates and no identifier changes.",
      "cons": "It keeps the parent the authors do not follow: most requirements name no origin, and the page still groups what the parent does not."
    },
    {
      "name": "One ordered reason list with precedence",
      "pros": "An ordered list answers which reason matters most without a second field.",
      "cons": "Precedence reimposes one ranking on reasons that carry none, and the first entry becomes a parent by another name."
    },
    {
      "name": "Advisory links above Behavior",
      "pros": "Existing projects face no new gates on day one.",
      "cons": "A link rule the builder cannot check rots, and the unlinked requirements show that advisory links stay empty."
    }
  ],
  "choice": "Topic record with cites and generated answers",
  "reopenedWhen": "A requirement appears whose story no topic page can hold.",
  "basedOn": [
    "research/intent-hierarchy-practice"
  ],
  "replaces": "decision/0002-parent-for-every-record"
}
---

# Topic Holds Its Requirements

## Context

Requirements are grouped three ways at once: the page groups them by topic area, the parent rule points each at a feature, and the journey that walks them sits under a goal. Most requirements name no parent at all, so one offer's story spreads over three files and the parent rule reports gaps the authors never felt as mistakes.

## Decision

The topic page becomes a record type, `topic`, identifier `topic/<name>`, and the parent of its requirements by identifier containment: a requirement's identifier `requirement/<topic>.<name>` names its topic, and a topic segment that differs from its page's name is an `identifier-invalid` finding. A requirement names its reasons in one unordered list, `cites`, holding any of goal, problem, feature, constraint, research and decision. A requirement that cites nothing is `requirement-cites-missing`, at check level A, or G when the requirement carries `risk`. Only Behavior records fulfill a requirement: rule, use case, reaction, job, notification and setting. A journey names the requirements it shows in `cites`. A constraint's What It Requires section becomes a generated region listing the requirements that cite it, and a constraint with `applies: true` that no requirement cites is `constraint-unreached` at check level G.

## Consequences

`litespec/core/intent#shape-each-topic-page` shapes the topic page, `litespec/core/intent#cite-the-reasons-of-every-requirement` and `litespec/core/intent#cite-each-applying-constraint-from-a-requirement` carry the cites rules, and `litespec/core/intent#restrict-requirement-fulfillment-to-behavior` keeps fulfillment in Behavior. `litespec/core/model#give-every-record-one-parent` keeps the one-parent algebra; this decision replaces only the requirement-origin consequence of `decision/0002-parent-for-every-record`.

## Evidence with Confidence

`research/intent-hierarchy-practice` holds the outside comparison and the measured counts over the largest consumer behind D1 to D5, high confidence.
