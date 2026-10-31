---
{
  "type": "runbook",
  "id": "runbook/<name>",
  "title": "<Runbook Display Title>",
  "owner": "<owner>",
  "status": "draft",
  "decider": "<owner>",
  "decideBy": "<YYYY-MM-DD>",
  "trigger": "<alert or request>",
  "severity": "<The damage while this alert fires>",
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
# <Runbook Display Title>

## Impact

State what breaks and who feels it.

## Prerequisites

- Required access and approvals.
- Required artifact, database, or service identifiers.
- Safety checks and communication channel.

## Procedure

1. Record the current state and correlation identifiers.
2. Run the exact approved commands or platform actions for this environment.
3. Verify the result before continuing.

```text
<exact command or platform action>
```

## Verification

State the health checks, data checks, or metrics that prove recovery.

## Rollback or Recovery

State the rollback command, restore condition, and data compatibility limit.

## Escalation

State the owner, backup contact, evidence to attach, and stop condition.
