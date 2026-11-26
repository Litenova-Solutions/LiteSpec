# Expense

An expense is one work cost that an employee submitted. Claimwick keeps each expense from its submission to its reimbursement, along with the facts a tax file needs. Every decision, payment and erasure in the spending module acts on an expense, so this is the record to understand first.

## Lifecycle

An expense starts Submitted when an employee submits it with `use-case/spending.submit-expense`. A manager then moves it to Approved with `use-case/spending.approve-expense`, or to Refused with `use-case/spending.refuse-expense`. Finance moves an approved expense to Reimbursed with `use-case/spending.reimburse-expense`, which means the payout reached the employee. Seven years after the reimbursement, the nightly erasure job moves it to Erased, which means its personal data is gone.

Each state says who acts next: a Submitted expense waits for a manager, and an Approved expense waits for finance. Refused has no transition out in the records, so no use case moves it again, and each decision use case refuses a second decision on the same expense.

## Data and Retention

The expense holds four fields: the employee field is personal data, and the other three are business facts that the tax file keeps.

| Field | Sensitivity | Data class | Kept for |
|:---|:---|:---|:---|
| `employee` | personal | `submitter-contact` | 7 years, then pseudonymized |
| `amount` | internal | `expense-ledger` | 7 years, then kept |
| `receipt` | internal | `expense-ledger` | 7 years, then kept |
| `state` | internal | `expense-ledger` | 7 years, then kept |

When its retention ends, the employee field is pseudonymized, so the name no longer identifies the employee. The job `job/spending.erase-expired-personal-data` performs the erasure. The amount, the receipt and the state stay on file, because the tax file must stay defensible. The 7 years count from the reimbursement of the expense, which the data classes name as their period start.

## Example

Ava, an employee at Harbor and Pine, paid 48.90 EUR for a train fare to a client visit, and she kept the receipt.

1. Ava submits the 48.90 EUR fare with its receipt. The fare is above the 25 EUR threshold, so the receipt is required. The expense is Submitted.
2. Ben, her manager, approves it within days. The expense is Approved.
3. Cara, in finance, reimburses it on a Friday. The expense is Reimbursed, and the payout batch sends the 48.90 EUR to Ava.
4. Seven years after the reimbursement, the erasure job pseudonymizes Ava's name on the expense and moves it to Erased. The amount and the receipt stay on file.
