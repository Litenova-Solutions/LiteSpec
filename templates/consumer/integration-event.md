---
{
  "type": "integration-event",
  "id": "integration-event/<name>",
  "title": "<Integration Event Display Title>",
  "owner": "<owner>",
  "status": "draft",
  "decider": "<owner>",
  "decideBy": "<YYYY-MM-DD>",
  "direction": "<out|in>",
  "eventType": "<CloudEvents type>",
  "source": "<CloudEvents source>",
  "subject": "<The record the subject identifies>",
  "dataschema": "<The schema URL for this event, or remove this line>",
  "payload": [
    {
      "name": "<field>",
      "meaning": "<The meaning of this field>",
      "type": "<text|integer|decimal|money|boolean|date|date-time|duration|email|choice|id>",
      "of": "<the record an id refers to, or the choices of a choice, when the type is id or choice; otherwise remove this line>",
      "required": "<true|false>",
      "classification": "<public|internal|personal|sensitive>"
    }
  ],
  "addresses": "<permission/<name> when the direction is out, otherwise remove this line>",
  "trust": "<trigger-only|payload>",
  "correlation": "<The match between an inbound event and its record, when the direction is in; otherwise remove this line>",
  "translates": "<event/<aggregate>.<past-tense> when the direction is out, otherwise remove this line>",
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
# <Integration Event Display Title>

## Summary

State what this event announces and who acts on it, in one sentence.

## Notes

Optional. Record anything a reader needs that fits no section above, under free `###` headings. Notes are informative and never authoritative. Remove this section when there is nothing to note.
