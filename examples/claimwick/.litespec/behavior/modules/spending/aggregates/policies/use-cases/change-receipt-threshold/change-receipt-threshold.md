# Change Receipt Threshold

Change Receipt Threshold lets a manager set the receipt threshold, the amount above which an expense needs a receipt. It concerns managers and every employee who submits an expense. It matters because a lower threshold asks for more receipts, and a higher one lets more expenses go without one.

## Flow

The manager sends the new threshold with its currency. Spending refuses a negative threshold with `spending.negative-threshold`. Otherwise the policy carries the new threshold from then on. The change raises no event and stores no audit entry.

A repeated change sets the same value, so the same threshold stays in place. The setting `setting/spending.receipt-threshold` gives the threshold a range of EUR 0 to EUR 500. This use case names only the refusal of a negative value. The records do not say what happens to a value above EUR 500.

## Example

Finance agreed with the managers that the threshold is 25 EUR. Ben, a manager, sets the threshold to 25 EUR, and the policy carries that value from then on. Ava's 48.90 EUR train fare is above 25 EUR, so the fare needs a receipt when Ava submits it.

Ben then sends a negative threshold. Spending refuses the change with `spending.negative-threshold`, and the policy keeps 25 EUR.
