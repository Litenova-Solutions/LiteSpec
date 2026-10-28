---
{
  "type": "aggregate",
  "id": "aggregate/lending.loan",
  "title": "Loan",
  "owner": "lending-team",
  "status": "reviewed",
  "states": [
    {
      "name": "Active",
      "meaning": "The loan is open."
    }
  ],
  "records": [
    {
      "type": "event",
      "id": "event/loan.loan-borrowed",
      "title": "Loan borrowed",
      "owner": "lending-team",
      "status": "reviewed",
      "meaning": "A copy was lent.",
      "carries": [
        "loanId"
      ]
    },
    {
      "type": "event",
      "id": "event/loan.loan-renewed",
      "title": "Loan renewed",
      "owner": "lending-team",
      "status": "reviewed",
      "meaning": "A loan was renewed.",
      "carries": [
        "loanId"
      ]
    }
  ]
}
---

# Loan

A loan lends one copy to one reader.
