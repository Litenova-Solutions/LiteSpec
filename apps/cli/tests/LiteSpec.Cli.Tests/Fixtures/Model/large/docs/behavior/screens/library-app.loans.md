---
{
  "type": "screen",
  "id": "screen/library-app.loans",
  "title": "Loans",
  "owner": "lending-team",
  "status": "reviewed",
  "entryPoint": "entry-point/library-app",
  "address": "/loans",
  "offers": [
    "use-case/lending.register-copy",
    "use-case/lending.borrow-copy",
    "use-case/lending.extend-loan",
    "use-case/lending.list-loans",
    "use-case/lending.return-copy",
    "use-case/lending.place-hold",
    "use-case/lending.cancel-hold",
    "use-case/lending.retire-copy"
  ],
  "serves": [
    "journey/borrow-and-return#s1",
    "journey/borrow-and-return#s2",
    "journey/borrow-and-return#s3",
    "journey/borrow-and-return#s4",
    "journey/borrow-and-return#s5",
    "journey/borrow-and-return#s6",
    "journey/borrow-and-return#s7",
    "journey/borrow-and-return#s8"
  ],
  "approvedBy": "lending-team",
  "approvedOn": "2026-10-01",
  "approvedHash": "0d1bf8a6"
}
---

# Loans

## Summary

A reader can register, borrow, extend, list, return and hold copies, and a librarian can retire a copy.
