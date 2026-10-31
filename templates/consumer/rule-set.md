---
{
  "type": "rule-set",
  "title": "<Rule Set Display Title>",
  "owner": "<owner>",
  "status": "draft",
  "decider": "<owner>",
  "decideBy": "<YYYY-MM-DD>",
  "records": [
    {
      "type": "rule",
      "id": "rule/<home>.<name>",
      "title": "<Rule Display Title>",
      "kind": "cross-aggregate",
      "statement": "<The records keep the rule in one sentence.>",
      "governs": ["<aggregate or use-case id>"],
      "refusal": "<result id or refusal code, or remove this line>",
      "basedOn": ["<constraint or research id, or remove this line>"],
      "fulfills": ["requirement/<topic>.<name>"]
    }
  ]
}
---
# <Rule Set Display Title>

## Summary

State why these rules sit together on one page.

## Notes

Optional. Record anything a reader needs that fits no section above, under free `###` headings. Notes are informative and never authoritative. Remove this section when there is nothing to note.
