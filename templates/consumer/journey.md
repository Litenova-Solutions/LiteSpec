---
{
  "type": "journey",
  "id": "journey/<name>",
  "title": "<Journey Display Title>",
  "owner": "<owner>",
  "status": "draft",
  "decider": "<owner>",
  "decideBy": "<YYYY-MM-DD>",
  "actor": "actor/<name>",
  "serves": ["goal/<name>"],
  "finishedWhen": "<the actor reaches the observable end>",
  "cites": ["requirement/<topic>.<name>"],
  "steps": [
    {
      "id": "<s1>",
      "statement": "<The actor performs the step in one present-tense sentence.>",
      "actor": "actor/<name>",
      "failure": "<The person experiences the failure in one present-tense sentence; remove this line when the step cannot fail>"
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
# <Journey Display Title>

## Example

State one concrete occasion for this journey, drawing people, place, dates, and amounts from the example cast.

## Before It Starts

Optional. State what must hold before this journey starts. Remove this section when the journey starts from nothing in particular.

## Between the Steps

State the failure branches and handoffs between the steps.
