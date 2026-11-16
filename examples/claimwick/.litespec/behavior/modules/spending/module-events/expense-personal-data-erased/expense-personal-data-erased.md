# Expense Personal Data Erased

The expense-personal-data-erased event tells other modules that the personal data of a decided expense was erased. A consumer holding personal data about the expense removes its copy too. The event carries only the expense identifier, so the consumer finds the expense it must clear by that identifier.

## Consumers

The reporting module clears the employee of the matching row in `read-model/reporting.reimbursed-expense`. The amount and the reimbursement time stay on the row, because the tax file keeps them.

## Compatibility

The payload may gain a new field. No field may be removed or renamed, and no meaning may change.

## Example

```json
{
  "expenseId": "exp_2026_1108_0042"
}
```
