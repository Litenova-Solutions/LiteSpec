---
{
  "type": "job",
  "id": "job/<module>.<name>",
  "title": "<Job Display Title>",
  "owner": "<owner>",
  "status": "draft",
  "decider": "<owner>",
  "decideBy": "<YYYY-MM-DD>",
  "kind": "scheduled",
  "schedule": { "every": "PT1H" },
  "selection": "<The records each run selects, or remove this line>",
  "batchSize": "<The number of items each batch takes, or remove this line>",
  "lease": {
    "name": "<lock, or remove the lease object>",
    "duration": "<ISO 8601 duration>"
  },
  "cursor": "<The store that holds the resume state, or remove this line>",
  "runbook": "<runbook/<name>, or remove this line>",
  "runsWhere": "<Deployment scope, or external:<timer>, or remove this line>",
  "guard": "<the person may start this run, required with a manual kind, otherwise remove this line>",
  "serviceLevel": "<The promised service level, or remove this line>",
  "evidence": "<The evidence each run leaves, or remove this line>",
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
      "when": "<The schedule fires the job>",
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
# <Job Display Title>

## Summary

State what this job does when its schedule fires, in one sentence.

The frontmatter shows a scheduled kind with an `every` duration. Write `{ "cron": "<expression>" }` with an optional `timezone`, or `{ "at": "startup" }`, for another schedule; write `"kind": "one-off"` and remove `schedule` for a run that happens once; write `"kind": "manual"` with a `guard` and remove `schedule` for a person-started run. Remove this paragraph.

## Example

Optional. Show one realistic run of this job, drawing people, place, dates, and amounts from the example cast. Remove this section when no run adds to the scenarios.

## Notes

Optional. Record anything a reader needs that fits no section above, under free `###` headings. Notes are informative and never authoritative. Remove this section when there is nothing to note.
