---
{
  "type": "known-gap",
  "id": "known-gap/expense-steps-unscreened",
  "title": "Expense Steps Unscreened",
  "owner": "claimwick-team",
  "status": "approved",
  "check": "step-unscreened",
  "subject": [
    "journey/submit-and-reimburse#s1",
    "journey/submit-and-reimburse#s2",
    "journey/submit-and-reimburse#s3",
    "journey/submit-and-reimburse#s4",
    "journey/submit-and-reimburse#s6",
    "journey/submit-and-reimburse#s7",
    "journey/submit-and-reimburse#s8"
  ],
  "endsBy": "2027-06-01",
  "reason": "The web app holds the expense list only, so seven journey steps served through the browser have no screen.",
  "approvedBy": "claimwick-team",
  "approvedOn": "2026-10-30",
  "approvedHash": "959bf3b1"
}
---

# Expense Steps Unscreened

## Exit

Seven steps of the submit-and-reimburse journey are served by browser-reached use cases that no screen offers yet, because the web app holds the expense list only. The gap closes when each step's use case gains its screen, the same work that closes `known-gap/expense-screens-unimplemented`.
