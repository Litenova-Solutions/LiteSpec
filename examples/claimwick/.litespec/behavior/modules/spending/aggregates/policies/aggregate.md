# Policy

The policy is the company's spending policy, and it holds the receipt threshold. It concerns the managers who set the threshold and every employee who submits an expense, because submitting checks the policy. It matters because the threshold decides which expenses need a receipt.

## Lifecycle

The policy has one state, Active, which means the threshold applies. The records create the policy in that state and record no other state. `use-case/spending.change-receipt-threshold` changes the threshold but does not change the state.

## Data and Retention

The records give the policy no personal data. It holds the receipt threshold, which is an amount in EUR. The records do not state how long the policy is kept.

## Example

Finance agreed with the managers on a receipt threshold of 25 EUR, which is the default. Ben, a manager, sets the threshold to 25 EUR, and the policy holds that value. Ava's 48.90 EUR train fare is above 25 EUR. When Ava submits the fare, spending checks it against the threshold in the policy, so the fare needs a receipt.
