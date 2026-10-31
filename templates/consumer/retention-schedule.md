---
{
  "type": "retention-schedule",
  "title": "<Retention Schedule Display Title>",
  "owner": "<owner>",
  "status": "draft",
  "decider": "<owner>",
  "decideBy": "<YYYY-MM-DD>",
  "rows": [
    {
      "dataClass": "<class>",
      "covers": ["aggregate/<module>.<name>"],
      "basis": "<statute|business|engineering>",
      "period": "<ISO 8601 duration, or replace this value with { mode: none, reason: <the data stays until its subject asks> } when the data is held on demand>",
      "enforcedBy": "<job/<module>.<name>|record-expiry|store-ttl|query-filter>",
      "cites": "<requirement/<topic>.<name>, required for personal or sensitive>",
      "processors": [
        {
          "system": "external-system/<name>",
          "forwarding": {
            "mode": "<call|event|impossible>",
            "operation": "<external-system/<name>#<operation>, when the mode is call; otherwise remove this line>",
            "event": "<integration-event/<name>, when the mode is event; otherwise remove this line>",
            "reason": "<the processor holds no personal data, when the mode is impossible; otherwise remove this line>"
          },
          "acceptedBy": "<Decision or requirement id>"
        }
      ],
      "conflicts": "<The duty that wins when audit keeping and subject erasure collide, and the record that logs it>"
    }
  ]
}
---
# <Retention Schedule Display Title>
