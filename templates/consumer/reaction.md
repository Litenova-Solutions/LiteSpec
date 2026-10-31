---
{
  "type": "reaction",
  "id": "reaction/<module>.<name>",
  "title": "<Reaction Display Title>",
  "owner": "<owner>",
  "status": "draft",
  "decider": "<owner>",
  "decideBy": "<YYYY-MM-DD>",
  "triggeredBy": [
    "<event/<aggregate>.<past-tense> or integration-event/<name>>"
  ],
  "skippedWhen": "<the trigger needs no run, or remove this line>",
  "effects": [
    {
      "runs": "<use-case/<module>.<name>, or replace this key with sends, emits, or calls>",
      "timing": "<inline|after-commit|async, or remove this line to read as async>"
    }
  ],
  "reads": ["aggregate/<module>.<name>"],
  "changes": ["aggregate/<module>.<name>"],
  "fulfills": ["requirement/<topic>.<name>"],
  "serves": ["journey/<name>#<step-id>"],
  "idempotency": {
    "mode": "<key|state|not-pending|none>",
    "key": "<Key template of {field} placeholders over the input field names, when the mode is key, otherwise remove this line>",
    "scope": "<tenant|account|login|global when the mode is key, otherwise remove this line>",
    "reason": "<replays stay harmless without a key, when the mode is none, otherwise remove this line>"
  },
  "onFailure": {
    "mode": "<retry|skip-and-log|discard>",
    "retry": {
      "maxAttempts": "<number of attempts, when the mode is retry; remove the retry object otherwise>",
      "initialDelay": "<ISO 8601 duration, when the mode is retry; otherwise remove this line>",
      "maxDelay": "<ISO 8601 duration, when the mode is retry; otherwise remove this line>",
      "backoff": "<exponential|fixed, when the mode is retry; otherwise remove this line>"
    },
    "then": "<dead-letter, when the mode is retry; otherwise remove this line>",
    "deadLetter": {
      "after": "<ISO 8601 retention, or remove the deadLetter object>",
      "action": "<What a person does>"
    },
    "surfacesIn": "<entry-point/<name>, or remove this line>",
    "runbook": "<runbook/<name>, or remove this line>"
  },
  "risk": ["<money|physical-access|personal-data|access, or remove this line>"],
  "records": [
    {
      "type": "scenario",
      "id": "scenario/<use-case>.<name>",
      "title": "<Scenario Display Title>",
      "given": "<The starting state>",
      "when": "<The event fires the reaction>",
      "then": "<The observable result>",
      "result": "result/<use-case>.<name>"
    }
  ],
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
# <Reaction Display Title>

## Summary

State what this reaction does when its trigger fires, in one sentence.

The frontmatter shows one event trigger. Add further `event/...` or `integration-event/...` ids to the `triggeredBy` list when several events fire this reaction. Remove this paragraph.

## Example

Optional. Show one realistic firing of this reaction, drawing people, place, dates, and amounts from the example cast. Remove this section when no firing adds to the scenarios.

## Notes

Optional. Record anything a reader needs that fits no section above, under free `###` headings. Notes are informative and never authoritative. Remove this section when there is nothing to note.
