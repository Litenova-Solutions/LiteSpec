# Expense Reimbursed

The product raises Expense Reimbursed when finance pays an approved expense. The event carries the expense, the employee who was paid, the amount and currency, and the time of payment. The event starts the retention period that ends in erasure.

## Example

Cara pays Ava's approved 48.90 EUR train fare on Friday with `use-case/spending.reimburse-expense`. The expense moves to Reimbursed, and the event carries the expense, Ava, the 48.90 EUR amount, the EUR currency and the payment time.
