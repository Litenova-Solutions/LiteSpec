# Attaches Before Decision

This rule keeps receipts tied to expenses that still await a decision. It protects the proof a manager reviews, and it stops a late receipt from changing an expense after the decision.

## Rationale

`rule/receipt.attaches-before-decision` governs `use-case/spending.attach-receipt` and binds `aggregate/spending.receipt` to `aggregate/spending.expense`. The use case notes that a receipt on the wrong expense misstates its proof. Once a manager decides, the expense leaves Submitted and no longer waits for proof. The refusal `refusal/spending.expense-decided` names the case where the expense is already decided.

## Example

Ava submits a small expense with no receipt, then attaches the receipt while the expense still waits. Ben can decide it after the receipt lands. If Ben has already approved the expense, Spending refuses Ava's receipt with `spending.expense-decided`.

## Limits

This rule checks the timing of the attachment. The rule `rule/spending.receipt-filed-once` keeps one receipt file to one expense, and `rule/spending.receipt-attached-atomically` keeps the write together.
