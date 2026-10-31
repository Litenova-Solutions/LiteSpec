---
{
  "type": "job",
  "id": "job/spending.erase-expired-personal-data",
  "title": "Erase Expired Personal Data",
  "owner": "claimwick-team",
  "status": "reviewed",
  "kind": "scheduled",
  "schedule": {
    "every": "P1D",
    "timezone": "Europe/Amsterdam"
  },
  "selection": "Decided expenses past their retention.",
  "effects": [
    {
      "issues": "use-case/spending.erase-personal-data",
      "timing": "async"
    }
  ],
  "reads": [
    "aggregate/spending.expense"
  ],
  "serviceLevel": "Decided expenses past retention are erased each night, per requirement/approval.erase-after-retention.",
  "evidence": "no run log; the example keeps no evidence of a run",
  "runbook": "runbook/clear-stuck-reimbursements",
  "fulfills": [
    "requirement/approval.erase-after-retention"
  ],
  "approvedBy": "claimwick-team",
  "approvedOn": "2026-10-31",
  "approvedHash": "795f2801"
}
---

# Erase Expired Personal Data

Each night the job erases the personal data of decided expenses past their retention. The bare business facts stay for the tax file.
