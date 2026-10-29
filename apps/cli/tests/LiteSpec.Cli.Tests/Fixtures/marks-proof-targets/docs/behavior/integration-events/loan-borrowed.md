---
{
  "type": "integration-event",
  "id": "integration-event/loan-borrowed",
  "title": "Loan borrowed",
  "owner": "lending-team",
  "status": "draft",
  "decider": "lending-team",
  "decideBy": "2030-01-31",
  "direction": "out",
  "eventType": "lending.loan-borrowed",
  "source": "lending-api",
  "subject": "loan",
  "translates": "event/loan.loan-borrowed",
  "payload": [
    {
      "name": "loanId",
      "meaning": "The loan that opened.",
      "classification": "internal"
    }
  ]
}
---

# Loan borrowed

A loan opened on the shelf.
