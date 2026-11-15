# Expense Reimbursed

The expense-reimbursed event tells other modules that finance paid an approved expense to its employee. The reporting module reads it to list the reimbursed expenses. A reader who has read nothing else should see that the event carries the employee and the amount, so a consumer can total the payouts per employee.

## Consumers

The reporting module builds `read-model/reporting.reimbursed-expense` from this event. The read model keeps the employee, the amount, the currency and the reimbursement time of each expense.

## Compatibility

A new field may be added to the payload. A field may not be removed or renamed, and its meaning may not change, until the event's `dataschema` moves to a new version.

## Example

```json
{
  "expenseId": "exp_2026_1108_0042",
  "employeeId": "emp_ava_0007",
  "amount": 48.90,
  "currency": "EUR",
  "reimbursedAt": "2026-11-12T09:30:00+01:00"
}
```
