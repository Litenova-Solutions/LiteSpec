---
{
  "type": "decision",
  "id": "decision/<number>-<name>",
  "title": "<Decision display title>",
  "owner": "<owner>",
  "status": "draft",
  "decider": "<owner>",
  "decideBy": "<YYYY-MM-DD>",
  "decided": "<YYYY-MM-DD, set on approval>",
  "options": [
    {
      "name": "<one option>",
      "for": "<What speaks for this option>",
      "against": "<What speaks against this option>"
    }
  ],
  "chosen": "<The selected option name, set on approval>",
  "changeWhen": "<The measurable condition that reopens this decision>",
  "decides": ["<requirement, feature, rule, or constraint-applicability id>"],
  "basedOn": ["<constraint or research id; a technical decision also names the requirements it serves>"],
  "replaces": "<decision id, or remove this line>",
  "terms": ["term/<term-name>"],
  "questions": [
    {
      "question": "<The open question, or remove the questions block>",
      "decider": "<owner>",
      "decideBy": "<YYYY-MM-DD>",
      "recommended": "<The recommended answer>",
      "ballot": "<decision-id or topic slug>",
      "tier": "<1|3>"
    }
  ]
}
---
# <Decision Display Title>

## Context

State the situation that forced a choice, with the facts that bound it.

## Decision

State the selected option in one paragraph. A decision is replaced, never edited after approval.

## Consequences

State what the product, the company, or the code now has to do, and the cost accepted.

## Evidence with Confidence

Optional. State the confidence in each source named in basedOn, and where sources disagree. Remove this section when the decision rests on no outside source.
