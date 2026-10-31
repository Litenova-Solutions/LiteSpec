---
{
  "type": "topic",
  "id": "topic/<name>",
  "title": "<Topic display title>",
  "owner": "<owner>",
  "status": "draft",
  "decider": "<owner>",
  "decideBy": "<YYYY-MM-DD>",
  "reviewBy": "<YYYY-MM-DD>",
  "records": [
    {
      "type": "requirement",
      "id": "requirement/<topic>.<name>",
      "title": "<Functional display title>",
      "kind": "functional",
      "statement": "<The commitment in one sentence>",
      "rationale": "<Why this commitment holds, in one line>",
      "except": [
        {
          "condition": "<Condition in product words, or remove the except block>",
          "fallback": "requirement/<topic>.<name>"
        }
      ],
      "setBy": "<decider|operator|tenant>",
      "risk": ["<money|door|personal-data|access, or remove this line>"],
      "cites": ["<goal, problem, feature, constraint, research, or decision id>"]
    },
    {
      "type": "requirement",
      "id": "requirement/<topic>.<name>",
      "title": "<Quality display title>",
      "kind": "quality",
      "statement": "<The measurable commitment in one sentence>",
      "rationale": "<Why this commitment holds, in one line>",
      "stimulus": "<The event the commitment answers>",
      "environment": "<The conditions it holds under>",
      "response": "<What the product does>",
      "measure": "<What is counted>",
      "dueBy": "<The date or milestone the target is met>",
      "target": "<The value that counts as met, with its unit>",
      "conditions": "<The load, window and environment the target assumes>",
      "check": "<The tool and assertion that confirm it, such as k6 http_req_duration p(95)<200>",
      "runsOn": "<pull-request, nightly, pre-release or production>",
      "evidence": "<Where the passing run is recorded>",
      "except": [
        {
          "condition": "<Condition in product words, or remove the except block>",
          "fallback": "requirement/<topic>.<name>"
        }
      ],
      "setBy": "<decider|operator|tenant>",
      "risk": ["<money|door|personal-data|access, or remove this line>"],
      "cites": ["<goal, problem, feature, constraint, research, or decision id>"]
    },
    {
      "type": "requirement",
      "id": "requirement/<topic>.<name>",
      "title": "<Policy display title>",
      "kind": "policy",
      "statement": "<The guaranteed value in one sentence>",
      "rationale": "<Why this commitment holds, in one line>",
      "values": [
        {
          "value": "<value>",
          "effectiveFrom": "<YYYY-MM-DD>"
        }
      ],
      "except": [
        {
          "condition": "<Condition in product words, or remove the except block>",
          "fallback": "requirement/<topic>.<name>"
        }
      ],
      "setBy": "<decider|operator|tenant>",
      "risk": ["<money|door|personal-data|access, or remove this line>"],
      "cites": ["<goal, problem, feature, constraint, research, or decision id>"]
    }
  ],
  "terms": ["term/<term-name>"],
  "questions": [
    {
      "question": "<The open question, or remove the questions block>",
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
# <Topic Display Title>

## Summary

State the current position on this topic in one short paragraph.

## The Situation

Tell what the customer faces on this topic. Give the numbers that size the problem, each with its source and date.

## What We Do

Explain the choice and how it works, as a person would explain it to a colleague. Link the decision records that set it.

## Why It Is Better

Compare the choice with the main alternative, with the numbers that favor it.

## Example

Show one realistic case walking through the choice from start to finish, drawing people, place, dates, and amounts from the example cast.

## Costs and Risks

State what the choice gives up, what it costs, and what could go wrong.

## How Sure We Are

State the confidence in the choice and what would raise it.

## Position

Link each decision that sets a choice on this topic, including choices no system carries.
