---
{
  "type": "decision",
  "id": "decision/<four-digit number>-<name>",
  "title": "<Decision Display Title>",
  "owner": "<owner>",
  "status": "draft",
  "decider": "<owner>",
  "decideBy": "<YYYY-MM-DD>",
  "approvedBy": "<The approving decider, set on approval>",
  "approvedOn": "<YYYY-MM-DD, set on approval>",
  "approvedHash": "<The semantic hash, set on approval>",
  "options": [
    {
      "name": "<one option>",
      "pros": "<The case for this option>",
      "cons": "<The case against this option>"
    }
  ],
  "choice": "<The selected option name, set on approval>",
  "reopenedWhen": "<the measure crosses its threshold>",
  "decides": ["<requirement, feature, rule, constraint, permission, role, actor, deviation, entry-point, setting, or decision id>"],
  "basedOn": ["<constraint or research id; a technical decision also names the requirements it serves>"],
  "replaces": "<decision id, or remove this line>",
  "terms": ["term/<term-name>"],
  "questions": [
    {
      "question": "<Which question stays open? remove the questions block when none does.>",
      "decider": "<owner>",
      "decideBy": "<YYYY-MM-DD>",
      "ballot": "<decision-id or topic slug>",
      "recommendation": "<The recommended answer, or remove this line when the record carries risk>",
      "answer": "<The answer, set on answer>",
      "answeredBy": "<The answerer, set on answer>",
      "answeredOn": "<YYYY-MM-DD, set on answer>"
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
