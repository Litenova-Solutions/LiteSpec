---
{
  "type": "integration-event",
  "id": "integration-event/<name>",
  "title": "<Integration event display title>",
  "owner": "<owner>",
  "status": "draft",
  "decider": "<owner>",
  "decideBy": "<YYYY-MM-DD>",
  "direction": "<out|in>",
  "eventType": "<CloudEvents type>",
  "source": "<CloudEvents source>",
  "subject": "<What the subject identifies>",
  "payload": [
    {
      "name": "<field>",
      "meaning": "<What this field means>",
      "classification": "<public|internal|personal|sensitive>"
    }
  ],
  "addresses": "<permission/<name> when the direction is out, otherwise remove this line>",
  "trust": "<trigger-only|payload>",
  "correlation": "<How an inbound event is matched, when the direction is in; otherwise remove this line>",
  "translates": "<event/<aggregate>.<past-tense> when the direction is out, otherwise remove this line>",
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
# <Integration Event Display Title>

## Summary

State what this event announces and who acts on it, in one sentence.

## Notes

Optional. Record anything a reader needs that fits no section above, under free `###` headings. Notes are informative and never authoritative. Remove this section when there is nothing to note.
