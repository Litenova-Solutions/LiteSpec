---
{
  "type": "external-system",
  "id": "external-system/<name>",
  "title": "<External System Display Title>",
  "owner": "<owner>",
  "status": "draft",
  "decider": "<owner>",
  "decideBy": "<YYYY-MM-DD>",
  "actor": "actor/<name>",
  "operations": [
    {
      "name": "<operation>",
      "direction": "<out|in>",
      "timeout": "<ISO 8601 duration>",
      "onTimeout": "<Callers see a timeout when the wait ends.>",
      "response": "<The data the response carries>",
      "cache": "<Cache policy, or remove this line>"
    }
  ],
  "erasure": {
    "mode": "<call|event|impossible>",
    "operation": "<external-system/<name>#<operation>, when the mode is call; otherwise remove this line>",
    "event": "<integration-event/<name>, when the mode is event; otherwise remove this line>",
    "reason": "<the processor holds no personal data, when the mode is impossible; otherwise remove this line>"
  },
  "acceptedBy": "<Decision or requirement id when erasure is impossible, otherwise remove this line>",
  "sends": ["<The data the product sends, or remove this line>"],
  "receives": ["<The data the product receives, or remove this line>"],
  "failure": "<Callers read cached answers while this system stays down.>",
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
# <External System Display Title>

## Summary

State what this system does for the product and who operates it.
