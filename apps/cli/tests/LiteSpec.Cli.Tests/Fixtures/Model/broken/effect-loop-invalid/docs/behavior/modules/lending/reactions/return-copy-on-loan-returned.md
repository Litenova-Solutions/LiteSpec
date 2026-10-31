---
{
  "type": "reaction",
  "id": "reaction/lending.return-copy-on-loan-returned",
  "title": "Return again",
  "owner": "lending-team",
  "status": "reviewed",
  "triggeredBy": [
    "event/loan.loan-returned"
  ],
  "effects": [
    {
      "runs": "use-case/lending.return-copy",
      "timing": "async"
    }
  ],
  "fulfills": [
    "requirement/borrowing.loan-period-days"
  ],
  "approvedBy": "lending-team",
  "approvedOn": "2026-10-31",
  "approvedHash": "549b880a"
}
---

# Return again

Every returned loan is returned again.
