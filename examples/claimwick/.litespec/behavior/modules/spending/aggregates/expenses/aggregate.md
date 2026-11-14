# Expense

An expense is one work cost that an employee submitted. Claimwick keeps each expense from its submission to its reimbursement, and it keeps the facts that a tax file needs. Every decision, payment and erasure in the spending module acts on an expense, so this record is the one to understand first.

## Lifecycle

An expense starts in the Submitted state when an employee submits it with `use-case/spending.submit-expense`. A manager then moves it to Approved with `use-case/spending.approve-expense`, or to Refused with `use-case/spending.refuse-expense`. Finance moves an approved expense to Reimbursed with `use-case/spending.reimburse-expense`. Reimbursed means that the payout reached the employee.

Each state tells the next step who acts. A Submitted expense waits for a manager. An Approved expense waits for finance. Refused and Reimbursed have no transition out in the records, so no use case moves them again. Each decision use case refuses a second decision on the same expense.

## Data and Retention

The expense holds four fields. The employee field is personal data. The other three are business facts that the tax file keeps.

| Field | Sensitivity | Data class | Kept for |
|:---|:---|:---|:---|
| `employee` | personal | `submitter-contact` | 7 years, then pseudonymized |
| `amount` | internal | `expense-ledger` | 7 years, then kept |
| `receipt` | internal | `expense-ledger` | 7 years, then kept |
| `state` | internal | `expense-ledger` | 7 years, then kept |

The employee field is pseudonymized when its retention ends, so the name no longer identifies the employee. The job `job/spending.erase-expired-personal-data` does this. The amount, the receipt and the state stay on file, because the tax file must stay defensible. The records do not say from which date the 7 years count.

## Example

This example follows one expense from the example cast. Ava, an employee at Harbor and Pine, paid 48.90 EUR for a train fare to a client visit, and she kept the receipt.

1. Ava submits the 48.90 EUR fare with its receipt. The fare is above the 25 EUR threshold, so the receipt is required. The expense is Submitted.
2. Ben, her manager, approves it within days. The expense is Approved.
3. Cara, in finance, reimburses it on a Friday. The expense is Reimbursed, and the payout batch sends the 48.90 EUR to Ava.
4. Seven years after the retention starts, the erasure job pseudonymizes Ava's name on the expense. The amount, the receipt and the state stay on file.
