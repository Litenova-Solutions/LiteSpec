---
{
  "type": "feature",
  "id": "feature/<name>",
  "title": "<Feature Display Title>",
  "owner": "<owner>",
  "status": "draft",
  "decider": "<owner>",
  "decideBy": "<YYYY-MM-DD>",
  "lifecycle": "<planned|shipped|later|refused>",
  "solves": ["problem/<name>, or the problem this feature declines, when refused"],
  "advances": ["goal/<name>", "or remove this line when the lifecycle is refused"],
  "refusedBecause": "<the cost outweighs the demand, when refused; remove this line otherwise>",
  "plannedFor": "<The milestone or date, when planned; remove this line otherwise>",
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
# <Feature Display Title>

## Offering

State what is offered and to whom, in one paragraph. State what is in and what is out.
