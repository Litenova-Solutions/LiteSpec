# Spending

Spending is the Claimwick module that handles work expenses: it tracks each expense, its receipt and the policy behind it. An employee who pays for a work cost depends on this module to get the money back, so its rules decide how fast that happens.

## Responsibilities

- Spending submits, decides, reimburses and exports work expenses.
- Spending tracks receipts and the receipt threshold policy.
- Spending holds the receipt threshold for submission checks.
- Spending answers the public read-expense query for the reporting module.

Submission starts each expense, and a manager's decision moves it on. Finance then reimburses the approved expenses and exports them in one batch.

## Boundaries

- Spending depends on no other module.
- Spending calls no other module.

Its record lists no dependencies, so Spending holds its own expenses, receipts and policy.

## Example

Ava's 48.90 EUR train fare passes through the module 4 times: Ava submits it, Ben decides it, Cara reimburses it, and Cara exports it in a batch with the other approved expenses.

The fare needs a receipt at submission, because 48.90 EUR is above the 25 EUR threshold in the policy. The full flow, with each step and its timing, is in `flow/submit-and-reimburse`.
