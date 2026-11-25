# Expense Submitted

The product raises Expense Submitted when an employee files a work expense. The event carries the expense, the amount and currency, the employee who submitted it, the submitter note when one is kept, the import batch when the expense arrived in one, and the line items. The reminder reaction starts from this event, so a waiting expense can reach its approver.

## Example

Ava submits her 48.90 EUR train fare for the client visit with `use-case/spending.submit-expense`. The expense moves to Submitted, and the event carries the expense, the 48.90 EUR amount, the EUR currency, Ava, and the one train-fare line.
