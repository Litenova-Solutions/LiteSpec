# Expense Personal Data Erased

The expense-personal-data-erased event tells other modules that the personal data of a decided expense was erased. A consumer that keeps personal data about the expense removes its copy as well. The event carries only the expense identifier, so a consumer finds the expense it must clear by that identifier.

## Consumers

The reporting module clears the employee of the matching row in `view/reporting.reimbursed-expense`. The amount and the reimbursement time stay, because the tax file keeps them.

## Compatibility

A new field may be added to the payload. A field may not be removed or renamed, and its meaning may not change, until the event's `dataschema` moves to a new version.

## Example

```json
{
  "expenseId": "exp_2026_1108_0042"
}
```
