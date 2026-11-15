# Attach Receipt

Attach Receipt lets an employee add a receipt to a submitted expense. It concerns the employee who submitted the expense. A receipt added after a decision would reach an expense a manager has already decided.

## Flow

The employee sends the receipt for a submitted expense. Spending refuses the receipt with `spending.expense-decided` when the expense is already decided. Otherwise the receipt sits on the expense, and `domain-event/receipt.attached` is raised. No audit entry is stored.

Attaching the same receipt to the same submitted expense again leaves the same receipt attached.

## Example

Ava submits a meal below the 25 EUR threshold without a receipt, and the meal is Submitted. Later she attaches the receipt to the meal. The receipt then sits on the expense, and `domain-event/receipt.attached` carries the receipt and expense identities.

## Edge Cases

Ben can approve the meal before Ava attaches the receipt. If Ben approves it first, the expense is decided. When Ava tries to attach the receipt, spending refuses with `spending.expense-decided`, so the receipt is not attached.
