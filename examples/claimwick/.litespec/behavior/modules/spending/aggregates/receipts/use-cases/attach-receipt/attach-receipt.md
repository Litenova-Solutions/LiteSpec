# Attach Receipt

Attach Receipt lets an employee add a receipt to a submitted expense. It concerns the employee who submitted the expense. A receipt added after a decision would reach an expense a manager has already decided.

## Flow

1. The employee sends the receipt for a submitted expense.
2. Spending refuses the receipt with `spending.expense-decided` when the expense is decided.
3. Spending attaches the receipt and raises the attached event.

An audit entry is stored for the receipt within its expense. Each request attaches a new receipt with a new identity, while a repeated request returns the first result instead: the receiptId key recognizes the repeat. The scan uploads the receipt as PNG, JPEG, or PDF up to 10 MB.

## Example

Ava submits a meal below the 25 EUR threshold without a receipt, and the meal is Submitted. Later she attaches the receipt to the meal. The receipt then sits on the expense, and `domain-event/receipt.attached` carries the receipt and expense identities.

## Edge Cases

- Spending refuses a receipt on a decided expense with `spending.expense-decided`.

The receipt is not attached.
