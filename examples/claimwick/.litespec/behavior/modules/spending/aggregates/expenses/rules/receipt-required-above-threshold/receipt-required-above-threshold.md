# Receipt Required Above Threshold

This rule asks for proof of payment when an expense is large enough to matter. It protects the tax file that finance defends, and it tells employees when a receipt must travel with a claim.

## Rationale

`rule/expense.receipt-required-above-threshold` governs `use-case/spending.submit-expense` and binds `aggregate/spending.expense` to `aggregate/spending.policy`, so the check reads the current threshold. The requirement `requirement/approval.above-threshold-receipt` says an expense above the receipt threshold carries a receipt. Its rationale keeps the tax file defensible. The decision `decision/0001-receipt-threshold` chose 25 EUR so small expenses stay light to submit. The refusal `refusal/spending.receipt-missing` names the case where the receipt is absent.

## Example

Ava's 48.90 EUR train fare is above the 25 EUR receipt threshold, so she attaches the receipt before she submits the fare. Without the receipt, Spending refuses with `spending.receipt-missing` and Ava submits again.

## Limits

This rule reads the threshold from `aggregate/spending.policy` at submission. A manager changes that threshold with `use-case/spending.change-receipt-threshold`; this rule does not set it.
