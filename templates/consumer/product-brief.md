---
{
  "type": "product",
  "id": "product/<name>",
  "title": "<Product Display Title>",
  "owner": "<owner>",
  "status": "draft",
  "decider": "<owner>",
  "decideBy": "<YYYY-MM-DD>",
  "boundary": "<The ground the product covers in one sentence>",
  "exclusions": ["<The product never takes this step.>"],
  "addresses": ["actor/<name>"],
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
# <Product Display Title>

## Summary

State the outcome the product exists to produce and why it matters, in one short paragraph.
