# Expense Submitted

The expense-submitted event tells other parts of the company that an employee filed a new expense. A reader who has read nothing else should see that the event carries the expense identifier and nothing else about the expense.

## Consumers

The records declare no consumer of the event yet, so it names none. The event carries only the identifier, so a consumer that needs more reads it from Claimwick.

## Compatibility

A new field may be added to the payload. A field may not be removed or renamed, and its meaning may not change, until the event's `dataschema` moves to a new version.

## Example

```json
{
  "expenseId": "exp_2026_1108_0042"
}
```
