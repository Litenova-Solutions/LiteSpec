# Spending

Spending is the Claimwick module that handles work expenses. It tracks each expense, its receipt and the policy behind it. An employee who pays for a work cost depends on this module to get the money back, so its rules decide how quickly that happens.

## Responsibilities

The module submits, decides, reimburses and exports work expenses. It also tracks the receipts and the policy that set which expenses need them.

Submission starts each expense, and a manager's decision moves it on. Finance then reimburses the approved expenses and exports them in one batch. The policy holds the receipt threshold, so the module knows which expenses need a receipt.

## Boundaries

The module depends on no other module. Its record lists no dependencies.

Spending therefore holds its own expenses, receipts and policy. The record names no other module that Spending calls.

## Example

Ava's 48.90 EUR train fare passes through the module 4 times. Ava submits it, Ben decides it, Cara reimburses it, and Cara exports it in a batch with the other approved expenses.

The fare needs a receipt at submission, because 48.90 EUR is above the 25 EUR threshold in the policy. The full walk, with each step and its timing, is in `journey/submit-and-reimburse`.
