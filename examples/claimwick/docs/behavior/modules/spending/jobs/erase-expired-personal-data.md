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
  "fulfills": [
    "requirement/approval.erase-after-retention"
  ],
  "approvedBy": "claimwick-team",
  "approvedOn": "2026-10-31",
  "approvedHash": "9a6e6d7d"
}
---

# Erase Expired Personal Data

Each night the job erases the personal data of decided expenses past their retention. The bare business facts stay for the tax file.
