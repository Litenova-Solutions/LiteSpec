# Submit Expense

Submit Expense is how an employee files a work cost so that a manager can decide it and finance can pay it back. The use case applies the receipt rule at this point. An expense above the receipt threshold with no receipt is refused, so each larger expense has proof of payment before it reaches a manager.

## Flow

The employee sends the amount, its currency and the receipt, when one is attached. Spending checks the receipt rule against the receipt threshold in the policy. An expense above the threshold with no receipt is refused with `spending.receipt-missing`.

Otherwise spending stores the expense as Submitted and raises `event/expense.submitted`. Each request creates a new expense with a new identity. A repeated request therefore submits a second expense. The records do not describe a check for repeated requests.

## Example

This example follows Ava, an employee at Harbor and Pine, who took the train to a client visit. Ava paid 48.90 EUR for the fare and kept the ticket receipt.

Ava submits the 48.90 EUR fare with the receipt attached. The fare is above the 25 EUR threshold, so the receipt is required, and it is present. The expense is stored as Submitted. The expense then waits for Ben, her manager, to decide. Ben decides within days.

## Edge Cases

Ava cannot submit the fare without its receipt. If she submits the 48.90 EUR fare without the receipt, spending refuses it with `spending.receipt-missing`. The records do not name a next step for Ava. The records also do not say whether an amount equal to the 25 EUR threshold needs a receipt.
