---
{
  "type": "aggregate",
  "id": "aggregate/spending.policy",
  "title": "Policy",
  "owner": "claimwick-team",
  "status": "reviewed",
  "summary": "The policy is the company spending policy and its receipt threshold.",
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
      "title": "Threshold never negative",
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
  "approvedOn": "2026-10-01",
  "approvedHash": "93bc3586"
}
---

# Policy

The policy is the company spending policy and its receipt threshold.

## Transitions

None.

## Data

None.
