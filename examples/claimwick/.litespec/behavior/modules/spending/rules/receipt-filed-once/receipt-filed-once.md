# Receipt Filed Once

This rule ties one receipt file to one expense across spending. It protects the meaning of the proof, so the same file never backs two different costs.

## Rationale

`rule/spending.receipt-filed-once` is an invariant on `use-case/spending.attach-receipt` and binds `aggregate/spending.receipt` to `aggregate/spending.expense`. A receipt is proof of payment for one expense, as `aggregate/spending.receipt` states. The use case notes that a receipt on the wrong expense misstates its proof. The rule keeps each file attached to the single expense it supports.

## Example

Ava attaches the receipt for her 48.90 EUR train fare. Spending files that receipt for her fare only. A second expense cannot use the same receipt file.

## Limits

This rule keeps one file to one expense. The rule `rule/receipt.attaches-before-decision` sets the time when an employee may attach it.
