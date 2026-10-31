---
{
  "type": "automation",
  "id": "automation/spending.erase-expired-personal-data",
  "title": "Erase Expired Personal Data",
  "owner": "claimwick-team",
  "status": "reviewed",
  "triggeredBy": {
    "schedule": {
      "every": "P1D",
      "timezone": "Europe/Amsterdam"
    }
  },
  "selection": "Decided expenses past their retention.",
  "effects": [
    {
      "runs": "use-case/spending.erase-personal-data",
      "timing": "async"
    }
  ],
  "reads": [
    "aggregate/spending.expense"
  ],
  "runbook": "runbook/clear-stuck-reimbursements",
  "fulfills": [
    "requirement/approval.erase-after-retention"
  ],
  "approvedBy": "claimwick-team",
  "approvedOn": "2026-10-31",
  "approvedHash": "b0168786"
}
---

# Erase Expired Personal Data

Each night the automation erases the personal data of decided expenses past their retention. The bare business facts stay for the tax file.
