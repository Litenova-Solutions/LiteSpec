---
{
  "type": "constraint",
  "id": "constraint/<name>",
  "title": "<Constraint display title>",
  "owner": "<owner>",
  "status": "draft",
  "decider": "<owner>",
  "decideBy": "<YYYY-MM-DD>",
  "authority": "<URL or document>",
  "kind": "<technical|organizational|convention>",
  "applies": true,
  "appliesBecause": "<Why this constraint applies in one sentence>",
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
# <Constraint Display Title>

## Summary

State the outside rule in one paragraph, in the words of its source.

## What It Requires

State what the product does to obey this constraint. State `None` when the constraint does not apply.
