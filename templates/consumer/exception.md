---
{
  "type": "exception",
  "id": "exception/<name>",
  "title": "<Exception Display Title>",
  "owner": "<owner>",
  "status": "draft",
  "decider": "<owner>",
  "decideBy": "<YYYY-MM-DD>",
  "kind": "<departure|gap>",
  "covers": "<litespec/<page>#<slug>; for a gap, replace this string with the finding codes it covers, such as [\"use-case-scenario-missing\"]>",
  "subject": ["<record id the exception covers>"],
  "paths": ["<path glob the exception covers, or remove this line>"],
  "reason": "<the project allows this until the date>",
  "expiresOn": "<YYYY-MM-DD>",
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
# <Exception Display Title>

## Consequences

State what is done instead and what it costs. Keep this section when the kind is departure; otherwise remove it.

## Exit

State the work that closes this gap before it ends. Keep this section when the kind is gap; otherwise remove it.
