# Claimwick App

Claimwick App is the browser app where people use the spending features of Claimwick. Every spending use case in these records is reached through it, so it is the only entry point into the module. The app decides who can come in and how they sign in before any use case runs.

## Audience

Employees, managers and finance staff reach spending through this app. The record names them as `actor/employee`, `actor/manager` and `actor/finance`. Employees submit expenses and attach receipts. Managers approve or refuse expenses and set the receipt threshold. Finance exports approved expenses, reimburses them and erases personal data.

## Authentication

The app signs each person in with a session. The records do not say how a session starts, how long it lasts or which system issues it. A session alone allows no action: each use case checks its own permission, such as `permission/approve-expense` for a decision, before it runs.
