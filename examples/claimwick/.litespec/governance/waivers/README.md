# Waivers

<!-- generated:waiver-index -->
### Gaps

| Waiver | Module | Expires On | Covers | Closed By |
|:---|:---|:---|:---|:---|
| [Export Review Bypass](export-review-bypass/export-review-bypass.yaml) | - | 2027-01-01 | `litespec/backend/application#enforce-target-authorization-in-the-pipeline` | `use-case/spending.export-expenses` |
| [Expense Screens Unimplemented](expense-screens-unimplemented/expense-screens-unimplemented.yaml) | - | 2027-06-01 | `use-case-unreached` | `use-case/reporting.list-reimbursed-expenses`, `use-case/spending.change-receipt-threshold`, `use-case/spending.change-reminder-preference`, `use-case/spending.export-expenses` |
| [Expense Steps Unscreened](expense-steps-unscreened/expense-steps-unscreened.yaml) | - | 2027-06-01 | `step-screen-missing` | `step/export-approved-expenses.export-batch`, `step/set-receipt-threshold.set-threshold`, `step/submit-and-reimburse.set-reminder-preference` |
| [Export Expenses Unexposed](export-expenses-unexposed/export-expenses-unexposed.yaml) | - | 2027-06-01 | `use-case-endpoint-missing` | `use-case/spending.export-expenses` |
| [Export Expenses Unimplemented](export-expenses-unimplemented/export-expenses-unimplemented.yaml) | - | 2027-06-01 | `step-implementation-missing`, `use-case-unimplemented` | `step/export-approved-expenses.export-batch`, `use-case/spending.export-expenses` |
| [Reimbursed List Unserved](reimbursed-list-unserved/reimbursed-list-unserved.yaml) | - | 2027-06-01 | `use-case-flow-missing` | `use-case/reporting.list-reimbursed-expenses` |
| [Use Cases Unverified](use-cases-unverified/use-cases-unverified.yaml) | - | 2027-06-01 | `use-case-unverified` | `use-case/reporting.list-reimbursed-expenses`, `use-case/spending.approve-expense`, `use-case/spending.attach-receipt`, `use-case/spending.change-receipt-threshold`, `use-case/spending.change-reminder-preference`, `use-case/spending.erase-personal-data`, `use-case/spending.export-expenses`, `use-case/spending.list-expenses`, `use-case/spending.read-expense`, `use-case/spending.refuse-expense`, `use-case/spending.reimburse-expense`, `use-case/spending.submit-expense` |

### Lasting

| Waiver | Module | Covers | Consequences |
|:---|:---|:---|:---|
| [Read Expense Unserved](read-expense-unserved/read-expense-unserved.yaml) | - | `use-case-flow-missing` | No flow shows the query, so a reader finds its caller through the queries edge on the reporting list. |
<!-- /generated:waiver-index -->
