# Claimwick App

Claimwick App is the browser app where people use the spending features of Claimwick. Every spending use case in these records is reached through it, so it is the only door into the module. It matters because it decides who can come in and how they sign in before any use case runs.

## Audience

Employees, managers and finance staff reach spending through this app. An employee submits expenses and attaches receipts. A manager approves or refuses expenses and sets the receipt threshold. Finance exports approved expenses, reimburses them and erases personal data.

## Authentication

The app is a browser app that signs each person in with a session. The records do not say how a session starts, how long it lasts or which system issues it. A session alone does not allow an action. Each use case checks its own permission, such as `permission/approve-expense` for a decision, before it runs.
