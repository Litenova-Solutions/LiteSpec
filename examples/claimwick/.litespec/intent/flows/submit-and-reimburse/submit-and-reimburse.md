# Submit and Reimburse

An employee who paid a work cost follows this path from submission to reimbursement: the employee submits the expense, with its receipt where one is needed, a manager decides it, and finance pays it out. The employee waits for the money through each step, so the path has to move fast, and each expense should be decided within 5 business days.

## Example

Ava paid a 48.90 EUR train fare for a client visit and has not submitted it yet. The fare is above the receipt threshold, so Ava attaches the receipt. Ben, her manager, approves it within the 5 business days. Cara works in finance and reimburses it on a Friday. Ava sees the expense as paid in her list.

1. Ava submits the fare with its amount, 48.90 EUR.
2. Ava attaches the receipt to the fare.
3. Ben approves the fare within the 5 business days.
4. Cara reimburses the approved fare on Friday.
5. Ava sees the expense as paid in her list.
6. Ben sets whether he gets a reminder for a waiting expense.

## Branches

At `step/submit-and-reimburse.decide-expense`, when Ben rejects the fare instead of approving it, the flow ends with the rejection and no payout. Ben may reject any expense, so the employee learns the decision from the list before any money moves.
