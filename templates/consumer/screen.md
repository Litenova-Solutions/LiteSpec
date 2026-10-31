---
{
  "type": "screen",
  "id": "screen/<entry-point>.<name>",
  "title": "<Screen Display Title>",
  "owner": "<owner>",
  "status": "draft",
  "decider": "<owner>",
  "decideBy": "<YYYY-MM-DD>",
  "entryPoint": "entry-point/<name>",
  "address": "<Route template, or remove this line and keep host>",
  "host": "screen/<entry-point>.<name>",
  "offers": ["use-case/<module>.<name>"],
  "serves": ["journey/<name>#s1"],
  "floorplan": "<floorplan>",
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
# <Screen Display Title>

## Summary

State what a person can do on this page. A navigation screen that offers no use case says so here.
