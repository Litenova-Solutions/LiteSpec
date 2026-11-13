# Submit and Reimburse

An employee who paid for a work cost follows this path from submission to reimbursement. The employee submits the expense, with its receipt where one is needed, a manager decides it, and finance pays it out. The employee waits for the money through each step, so the path has to move quickly. Each expense should be decided within 5 business days.

## Preconditions

Two things hold before Ava submits. She is signed in as an employee, and the policy carries its receipt threshold.

The threshold is 25 EUR, the amount set in `decision/0001-receipt-threshold`. Any expense above it needs a receipt.

## Example

Ava's 48.90 EUR train fare from a client visit goes through every step. The fare is above the 25 EUR threshold, so Ava attaches the receipt. Ben, her manager, approves it. Cara works in finance and reimburses it on a Friday.

1. Ava submits the fare with its amount, 48.90 EUR, and the receipt.
2. Ben approves the fare within the 5 business days.
3. Cara reimburses the approved fare on Friday.
4. Ava sees the expense as paid in her list.
5. Cara's export takes the approved fare in its batch.

## Variations

Ben may reject the expense instead of approving it. The journey then ends with the rejection and no payout.

Cara reimburses only approved expenses, and anything else waits. The export in step 5 takes only approved expenses too, so a rejected expense does not reach the batch.

If Ben has not decided within 3 business days, the expense is past the reminder point, and Ben is reminded. The 5-business-day limit still applies.
