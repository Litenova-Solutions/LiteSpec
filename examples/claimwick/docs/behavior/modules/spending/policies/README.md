---
{
  "type": "aggregate",
  "id": "aggregate/spending.policy",
  "title": "Policy",
  "owner": "claimwick-team",
  "status": "reviewed",
  "summary": "The policy is the company spending policy and its receipt threshold.",
  "kind": "entity",
  "states": [
    {
      "name": "Active",
      "meaning": "The policy threshold applies."
    },
    {
      "name": "Retired",
      "meaning": "The policy no longer applies."
    }
  ],
  "records": [
    {
      "type": "rule",
      "id": "rule/policy.threshold-never-negative",
      "title": "Threshold Never Negative",
      "owner": "claimwick-team",
      "status": "reviewed",
      "kind": "invariant",
      "statement": "A threshold is never negative.",
      "governs": [
        "use-case/spending.change-receipt-threshold"
      ],
      "refusal": "result/change-receipt-threshold.negative-threshold",
      "basedOn": [],
      "fulfills": [
        "requirement/approval.receipt-over-threshold"
      ],
      "approvedBy": "claimwick-team",
      "approvedOn": "2026-10-01",
      "approvedHash": "9b08116e"
    }
  ],
  "approvedBy": "claimwick-team",
  "approvedOn": "2026-10-31",
  "approvedHash": "ffb5a4d4"
}
---

# Policy

The policy is the company spending policy and its receipt threshold.

## Transitions

None.

## Data

None.
