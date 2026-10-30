---
{
  "type": "feature",
  "id": "feature/<name>",
  "title": "<Feature display title>",
  "owner": "<owner>",
  "status": "draft",
  "decider": "<owner>",
  "decideBy": "<YYYY-MM-DD>",
  "lifecycle": "<planned|shipped|later|refused>",
  "solves": ["problem/<name>, or the problem this feature declines, when refused"],
  "advances": ["goal/<name>", "or remove this line when the lifecycle is refused"],
  "refusedBecause": "<reason, when refused; remove this line otherwise>",
  "plannedFor": "<The milestone or date, when planned; remove this line otherwise>",
  "terms": ["term/<term-name>"],
  "questions": [
    {
      "question": "<The open question, or remove the questions block>",
      "decider": "<owner>",
      "decideBy": "<YYYY-MM-DD>",
      "recommended": "<The recommended answer>",
      "ballot": "<decision-id or topic slug>",
      "tier": "<1|3>"
    }
  ]
}
---
# <Feature Display Title>

## Offering

State what is offered and to whom, in one paragraph. State what is in and what is out.
