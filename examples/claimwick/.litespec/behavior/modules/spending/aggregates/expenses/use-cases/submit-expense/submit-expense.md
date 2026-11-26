# Submit Expense

Submit Expense is how an employee files a work cost so that a manager can decide it and finance can pay it back. The use case applies the receipt rule here: an expense above the receipt threshold with no receipt is refused, so each larger expense carries proof of payment before it reaches a manager.

## Flow

1. The employee sends the amount, currency and receipt.
2. Spending checks the receipt rule against the threshold.
3. Spending refuses the submission with `spending.receipt-missing` when the receipt is missing.
4. Spending stores the expense as Submitted and raises the submitted event.

Each request creates a new expense with a new identity, while a repeated request returns the first result instead: the expenseId key recognizes the repeat.

## Example

Ava, an employee at Harbor and Pine, took the train to a client visit. She paid 48.90 EUR for the fare and kept the ticket receipt.

Ava submits the 48.90 EUR fare with the receipt attached. The fare is above the 25 EUR threshold, so the receipt is required, and it is present. The expense is stored as Submitted, and it waits for Ben, her manager, to decide. Ben decides within days.

## Edge Cases

- Spending refuses a threshold expense with no receipt with `spending.receipt-missing`.

The records name no next step for Ava after that refusal. The records also do not say whether an amount equal to the 25 EUR threshold needs a receipt.
