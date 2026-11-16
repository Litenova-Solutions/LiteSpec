# Expense Reimbursed

The expense-reimbursed event tells other modules that finance paid an approved expense to its employee. The reporting module reads it to list the reimbursed expenses. The event carries the employee and the amount, so a consumer can total the payouts per employee.

## Consumers

The reporting module builds `read-model/reporting.reimbursed-expense` from this event. The read model keeps the employee, the amount, the currency and the reimbursement time of each expense.

## Compatibility

The selection may gain a new field the domain event carries. No field may be removed, and no meaning may change.

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
