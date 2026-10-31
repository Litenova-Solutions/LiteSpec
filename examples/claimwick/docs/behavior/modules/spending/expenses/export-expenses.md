---
{
  "type": "use-case",
  "id": "use-case/spending.export-expenses",
  "title": "Export Expenses",
  "owner": "claimwick-team",
  "status": "reviewed",
  "kind": "query",
  "access": {
    "auth": [
      "session"
    ],
    "grant": "permission/export-expenses",
    "tenant": "own"
  },
  "serves": [
    "journey/submit-and-reimburse#s7"
  ],
  "reachedThrough": [
    "entry-point/claimwick-app"
  ],
  "fulfills": [
    "requirement/approval.decided-within-five-days"
  ],
  "reads": [
    "aggregate/spending.expense"
  ],
  "idempotency": {
    "mode": "none",
    "reason": "the query reads approved expenses without changing them"
  },
  "output": {
    "include": {
      "file": {
        "fields": [
          {
            "name": "rows",
            "meaning": "The set of rows with one row per approved expense: its identity, amount, currency, approver and approval time."
          }
        ],
        "classification": "internal",
        "reads": [
          "aggregate/spending.expense"
        ],
        "freshness": {
          "mode": "immediate"
        }
      }
    }
  },
  "paging": {
    "mode": "none",
    "reason": "one file holds every approved expense of the period"
  },
  "records": [
    {
      "type": "result",
      "id": "result/export-expenses.exported",
      "title": "Exported",
      "owner": "claimwick-team",
      "outcome": "success",
      "meaning": "The approved expenses are exported.",
      "approvedBy": "claimwick-team",
      "approvedOn": "2026-10-01",
      "approvedHash": "0f8eff75"
    },
    {
      "type": "result",
      "id": "result/export-expenses.nothing-approved",
      "title": "Nothing Approved",
      "owner": "claimwick-team",
      "outcome": "refusal",
      "code": "spending.nothing-approved",
      "meaning": "The state of an export with no approved expense awaiting it.",
      "approvedBy": "claimwick-team",
      "approvedOn": "2026-10-31",
      "approvedHash": "3a0866c0"
    },
    {
      "type": "scenario",
      "id": "scenario/export-expenses.exporting-approved-expenses-succeeds",
      "title": "Exporting Approved Expenses Succeeds",
      "owner": "claimwick-team",
      "status": "reviewed",
      "given": "Two expenses were approved this week.",
      "when": "Finance exports the approved expenses.",
      "result": "result/export-expenses.exported",
      "then": "The approved expenses are exported.",
      "approvedBy": "claimwick-team",
      "approvedOn": "2026-10-01",
      "approvedHash": "cd3206cd"
    },
    {
      "type": "scenario",
      "id": "scenario/export-expenses.exporting-with-nothing-approved-is-refused",
      "title": "Exporting with Nothing Approved Is Refused",
      "owner": "claimwick-team",
      "status": "reviewed",
      "given": "No expense was approved this week.",
      "when": "Finance exports the approved expenses.",
      "result": "result/export-expenses.nothing-approved",
      "then": "The export is refused because no approved expense awaits it.",
      "approvedBy": "claimwick-team",
      "approvedOn": "2026-10-01",
      "approvedHash": "0584437c"
    }
  ],
  "approvedBy": "claimwick-team",
  "approvedOn": "2026-10-31",
  "approvedHash": "3a1cba69"
}
---
# Export Expenses

## Goal

Finance collects the approved expenses so the payout batch can run.

## What Happens

Finance asks for every approved expense since the last export. Spending refuses the export when no approved expense awaits it. Otherwise the approved expenses are handed over as one batch.
