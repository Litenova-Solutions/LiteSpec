---
{
  "type": "role",
  "id": "role/<name>",
  "title": "<Role display title>",
  "owner": "<owner>",
  "status": "draft",
  "decider": "<owner>",
  "decideBy": "<YYYY-MM-DD>",
  "scope": "<scope, such as platform, tenant or self>",
  "allows": ["permission/<name>"],
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
# <Role Display Title>

## Summary

State what this role is for and who grants it.
