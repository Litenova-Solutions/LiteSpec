---
{
  "type": "use-case",
  "id": "use-case/<module>.<name>",
  "title": "<Use Case Display Title>",
  "owner": "<owner>",
  "status": "draft",
  "decider": "<owner>",
  "decideBy": "<YYYY-MM-DD>",
  "kind": "<command|query>",
  "serves": ["<journey/<name>#<step-id>>"],
  "fulfills": ["requirement/<topic>.<name>"],
  "access": {
    "grant": "<permission/<name>|self(permission/<name>)|anonymous|system>",
    "tenant": "<own|cross|public|none>"
  },
  "reachedThrough": ["entry-point/<name>"],
  "input": [
    {
      "name": "<field>",
      "meaning": "<The meaning of this field>",
      "type": "<text|integer|decimal|money|boolean|date|date-time|duration|email|choice|id>",
      "of": "<the record an id refers to, or the choices of a choice, when the type is id or choice; otherwise remove this line>",
      "required": "<true|false>",
      "rules": ["<The shape each value obeys>"],
      "origin": "<caller|claim|derived>",
      "classification": "<public|internal|personal|sensitive>"
    }
  ],
  "output": {
    "include": {
      "<group>": {
        "fields": [
          {
            "name": "<field>",
            "meaning": "<The meaning of this field>"
          }
        ],
        "classification": "<public|internal|personal|sensitive>",
        "reads": ["aggregate/<module>.<name>"],
        "freshness": {
          "mode": "<immediate|snapshot|bounded|advisory>",
          "within": "<ISO 8601 duration the snapshot stays fresh, when the mode is bounded; otherwise remove this line>",
          "authority": "<record id that holds the authoritative value, when the mode is advisory; otherwise remove this line>"
        },
        "visibleWhen": "<the reader sees this group, or remove this line>",
        "cites": "<requirement/<topic>.<name>, or remove this line>"
      }
    }
  },
  "paging": {
    "mode": "pages",
    "order": "<Field and direction; replace the whole paging object with { mode: none, reason: <the query returns one object> } when the query returns one object>",
    "cursor": "<Cursor field, or replace this line with an offset field>",
    "defaultLimit": "<number>",
    "maxLimit": "<number>"
  },
  "builtAsynchronously": "<true when the query source lags, otherwise remove this line>",
  "changes": ["aggregate/<module>.<name>"],
  "reads": ["<aggregate/<module>.<name> or setting/<module>.<name>>"],
  "consistency": {
    "mode": "<atomic|per-record|compensated>",
    "cites": ["<rule/<aggregate>.<name> when the mode is atomic across aggregates, or remove this line>"]
  },
  "effects": [
    {
      "runs": "<use-case/<module>.<name>>",
      "timing": "<inline|after-commit|async>",
      "sharesTransaction": "<true or false when the timing is inline, otherwise remove this line>",
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
      "before": "<Another entry target with its reason, or remove this line>",
      "forEach": "<Scope, or remove this line>"
    },
    {
      "sends": "<notification/<module>.<name>, or replace this key with emits or calls>",
      "timing": "<after-commit|async>",
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
      "before": "<Another entry target with its reason, or remove this line>",
      "forEach": "<Scope, or remove this line>"
    }
  ],
  "raises": ["event/<aggregate>.<past-tense>"],
  "idempotency": {
    "mode": "<key|state|not-pending|none>",
    "key": "<Key template of {field} placeholders over the input field names, when the mode is key, otherwise remove this line>",
    "scope": "<tenant|account|login|global when the mode is key, otherwise remove this line>",
    "reason": "<replays stay harmless without a key, when the mode is none, otherwise remove this line>"
  },
  "audit": {
    "level": "<required|optional|none; replace this whole audit object with { exempt: <justification> } when exempt>",
    "target": "<Target kind, or none>"
  },
  "risk": ["<money|door|personal-data|access, or remove this line>"],
  "background": "<The shared starting state every scenario starts from, or remove this line>",
  "records": [
    {
      "type": "result",
      "id": "result/<use-case>.<name> (the use case's own name)",
      "title": "<Success Display Title>",
      "outcome": "success",
      "meaning": "<The observable result in one sentence>",
      "codes": ["<Recorded code, or remove this line>"]
    },
    {
      "type": "result",
      "id": "result/<use-case>.<name> (the use case's own name)",
      "title": "<Refusal Display Title>",
      "outcome": "refusal",
      "code": "<module>.<reason; never write the generic forbidden or invalid-input refusals, which the builder derives as result/<use-case>.forbidden and result/<use-case>.invalid-input>",
      "meaning": "<The message the caller sees in one sentence; refusals run in precedence order and the first match wins>",
      "recorded": "<true when the attempt is stored, otherwise remove this line>"
    },
    {
      "type": "scenario",
      "id": "scenario/<use-case>.<name> (the use case's own name)",
      "title": "<Scenario Display Title>",
      "exercises": ["<rule/<aggregate>.<name>, or remove this line>"],
      "given": "<The starting state>",
      "when": "<The actor performs the act>",
      "then": "<The observable result>",
      "result": "result/<use-case>.<name> (the use case's own name)",
      "examples": [
        {
          "<column>": "<value>"
        }
      ]
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
# <Use Case Display Title>

## Goal

State the actor goal in one sentence. For a command, replace the output object with a list of name and meaning entries, or remove output when the command returns nothing.

## What Happens

State the behavior in order, in plain sentences. Keep this section when the use case changes more than one aggregate, refuses for a reason beyond the generic ones, carries any risk, or has an inline effect; otherwise remove it.

## Example

Optional. Show one realistic run of this use case, drawing people, place, dates, and amounts from the example cast. Remove this section when no run adds to the scenarios.

## Notes

Optional. Record anything a reader needs that fits no section above, under free `###` headings. Notes are informative and never authoritative. Remove this section when there is nothing to note.
