---
{
  "type": "aggregate",
  "id": "aggregate/<module>.<name>",
  "title": "<Aggregate Display Title>",
  "owner": "<owner>",
  "status": "draft",
  "decider": "<owner>",
  "decideBy": "<YYYY-MM-DD>",
  "summary": "<The consistency this aggregate keeps in one sentence>",
  "kind": "<process|store, or remove this line for an entity>",
  "states": [
    {
      "name": "<state>",
      "meaning": "<The state of an instance sitting here>"
    }
  ],
  "dataClasses": [
    {
      "name": "<class>",
      "classification": "<public|internal|personal|sensitive>",
      "fields": ["<field>"],
      "onErasure": "<erase|pseudonymize|retain|exempt>",
      "basis": "<statute|business|engineering>",
      "period": "<ISO 8601 duration, or replace this value with { mode: none, reason: <the data stays until its subject asks> } when the data is held on demand>",
      "enforcedBy": "<job/<module>.<name>|record-expiry|store-ttl|query-filter>",
      "reason": "<the product keeps this data beyond erasure, when exempt, otherwise remove this line>",
      "reviewBy": "<Date when exempt, otherwise remove this line>",
      "acceptedBy": "<Decision or requirement id when personal or sensitive with retain or exempt, otherwise remove this line>",
      "reach": "<own-tenant|subject-wide|platform-wide>",
      "exceptions": [
        {
          "field": "<field>",
          "onErasure": "<erase|pseudonymize|retain|exempt>"
        }
      ]
    }
  ],
  "steps": [
    {
      "name": "<step; remove the steps, exits, finishedWhen, driver, and cursor lines when the kind is not process>",
      "when": "<the step runs next, or remove this line>",
      "compensation": {
        "mode": "<none|use-case>",
        "reason": "<the step changes nothing to undo, when the mode is none; otherwise remove this line>",
        "useCase": "<use-case/<module>.<name>, when the mode is use-case, otherwise remove this line>"
      }
    }
  ],
  "exits": [
    {
      "name": "<exit>",
      "trigger": "<the state, decision by <actor>, or <duration> wait that opens this exit>",
      "then": "<The run leaves through this exit.>"
    }
  ],
  "finishedWhen": "<every step reaches the observable end>",
  "driver": ["<reaction/<module>.<name>, job/<module>.<name>, or operator: <who, via runbook/<name>, due <duration>>>"],
  "cursor": "<The store that holds the resume state, or remove this line>",
  "records": [
    {
      "type": "rule",
      "id": "rule/<aggregate>.<name>",
      "title": "<Rule Display Title>",
      "kind": "<invariant|precondition|calculation|cross-aggregate|atomicity|reservation|commercial-data|default>",
      "statement": "<The aggregate keeps the rule in one sentence.>",
      "governs": ["aggregate/<module>.<name>"],
      "refusal": "<result id or refusal code, or remove this line>",
      "releasedBy": "<use-case/<module>.<name>, or { mode: never, reason: <the hold stands until the event ends> } when nothing releases it, when the kind is reservation; otherwise remove this line>",
      "frozenWhen": "<the value freezes, when the kind is commercial-data; otherwise remove this line>",
      "history": "<The store that holds past values, when the kind is commercial-data; otherwise remove this line>",
      "basedOn": ["<constraint or research id, or remove this line>"],
      "fulfills": ["requirement/<topic>.<name>"]
    },
    {
      "type": "event",
      "id": "event/<aggregate>.<past-tense>",
      "title": "<Event Display Title>",
      "meaning": "<The completion this event reports in one sentence>",
      "carries": ["<field-name>"]
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
# <Aggregate Display Title>

## Transitions

List every allowed state change with the use case that moves it.

| From | To | Triggered By |
|:---|:---|:---|
| <state> | <state> | use-case/<module>.<name> |

## Data

List every stored field with its meaning, data class, and classification.

| Name | Meaning | Data Class | Classification |
|:---|:---|:---|:---|
| <field> | <The meaning of this field> | <class> | <public\|internal\|personal\|sensitive> |

## Example

Optional. Show one realistic life of an instance of this aggregate, drawing people, place, dates, and amounts from the example cast. Remove this section when no life adds to the tables.

## Known Limits

Optional. State the deliberate boundaries of this aggregate, each linking the decision behind it. Remove this section when the aggregate holds no deliberate boundary.

## Notes

Optional. Record anything a reader needs that fits no section above, under free `###` headings. Notes are informative and never authoritative. Remove this section when there is nothing to note.
