# Expense Submitted

The expense-submitted event tells other parts of the company that an employee filed a new expense. It carries the expense identifier, and nothing else about the expense.

## Consumers

The records declare no consumer of the event yet, so it names none. The event carries only the identifier, so a consumer that needs more reads it from Claimwick.

## Compatibility

The payload may gain a new field. No field may be removed or renamed, and no meaning may change, until the event's `dataschema` moves to a new version.

## Example

```json
{
  "expenseId": "exp_2026_1108_0042"
}
```
