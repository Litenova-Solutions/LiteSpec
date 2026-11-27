# Attach Receipt Writes Two Aggregates

Attaching a receipt stores the receipt and its expense link in one transaction, so the two never land apart. This decision belongs to the Spending module's implementation. It keeps `use-case/spending.attach-receipt` writing `aggregate/spending.expense` and `aggregate/spending.receipt` together, under `rule/spending.receipt-attached-atomically`.

## Context

A receipt meant nothing without the expense it proved, so attaching one had to store the receipt and its expense link together. Splitting the write would leave a receipt no expense named, or an expense naming a receipt that never landed. Behind this choice sits `requirement/approval.above-threshold-receipt`, the requirement the decision rests on.

## Options

Two ways of landing the receipt and its link were weighed: one transaction writing both aggregates, and a receipt first with a reaction linking the expense after.

| Option | Advantage | Drawback |
|:---|:---|:---|
| One transaction writing both aggregates | The receipt and its expense link land together. | The command writes two aggregates in one transaction. |
| Receipt first with a reaction linking the expense after | Each command writes one aggregate. | A failure between the writes leaves a receipt no expense names. |

The single transaction keeps the pair together at the cost of one command writing two aggregates. The reaction route keeps one aggregate per command, but a failure between the writes leaves a receipt no expense names.

## Decision

`use-case/spending.attach-receipt` writes `aggregate/spending.expense` and `aggregate/spending.receipt` in one transaction, under `rule/spending.receipt-attached-atomically`.

## Consequences

The receipt and its expense link land together, so no receipt ever stands without the expense it proves. A later move of the receipt boundary reopens this choice.

## Evidence

The atomicity rule `rule/spending.receipt-attached-atomically` states the invariant the single transaction keeps: attaching a receipt stores the receipt and its expense link together.

## Revisit Conditions

The choice reopens when the receipt boundary moves into the expense aggregate, or when the expense link tolerates landing after the receipt. Either change removes the reason for writing both aggregates at once.
