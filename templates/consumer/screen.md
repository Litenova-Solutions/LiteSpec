---
{
  "type": "screen",
  "id": "screen/<entry-point>.<name>",
  "title": "<Screen display title>",
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
      "question": "<The open question, or remove the questions block>",
      "decider": "<owner>",
      "decideBy": "<YYYY-MM-DD>"
    }
  ]
}
---
# <Screen Display Title>

## Summary

State what a person can do on this page. A navigation screen that offers no use case says so here.
