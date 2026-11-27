# Receipt Attached Atomically

This rule stores a receipt and its link to the expense in one write. It protects the proof that a receipt gives, so finance never sees an expense pointing to a receipt that never landed.

## Rationale

`rule/spending.receipt-attached-atomically` is an atomicity rule based on `decision/0002-attach-receipt-writes-two-aggregates`. The decision says a receipt means nothing without the expense it proves. Splitting the write would leave a receipt no expense names, or an expense naming a receipt that never landed. The decision chose one transaction writing both aggregates, so `use-case/spending.attach-receipt` writes `aggregate/spending.expense` and `aggregate/spending.receipt` together.

## Example

Ava attaches the receipt for her 48.90 EUR train fare. Spending writes the receipt and the expense link in one transaction. The receipt and the expense link land together, so the expense names real proof.

## Limits

This rule governs how the write lands. The rule `rule/receipt.attaches-before-decision` sets when an employee may attach a receipt.
