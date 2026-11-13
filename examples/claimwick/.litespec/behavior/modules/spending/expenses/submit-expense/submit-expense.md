# Submit Expense

## What Happens

The employee sends the amount with its currency and the receipt, if one is attached. Spending checks the receipt rule against the policy threshold. An expense above the threshold with no receipt is refused with `spending.receipt-missing`. Otherwise the expense is stored as Submitted and `event/expense.submitted` is raised.

## Example

Ava takes the train to a client visit and keeps the ticket receipt. Back at the office she submits the 48.90 EUR fare with the receipt attached. The expense waits for Ben to decide.
