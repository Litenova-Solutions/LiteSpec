# Change Receipt Threshold

Change Receipt Threshold lets a manager set the receipt threshold, the amount above which an expense needs a receipt. It concerns managers and every employee who submits an expense. A lower threshold asks for more receipts, and a higher one lets more expenses go without one.

## Flow

1. The manager sends the new threshold with its currency.
2. Spending refuses the change with `spending.negative-threshold` when the threshold is negative.
3. Spending stores the new threshold on the policy.

The change raises no event and stores no audit entry. Sending the same value again leaves the same threshold in place. The records state no upper bound for the threshold.

## Example

Finance agreed the threshold of 25 EUR with the managers. Ben, a manager, sets the threshold to 25 EUR, and the policy carries that value from then on. Ava's 48.90 EUR train fare is above 25 EUR, so the fare needs a receipt when Ava submits it.

Ben then sends a negative threshold. Spending refuses the change with `spending.negative-threshold`, and the policy keeps 25 EUR.
