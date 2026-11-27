# Expense Submitted

The product raises Expense Submitted when an employee files a work expense. The event carries the expense, the amount and currency, the employee who submitted it, and the identity of each line of the expense. The reminder reaction starts from this event, so a waiting expense can reach its approver.

## Example

Ava submits her 48.90 EUR train fare for the client visit with `use-case/spending.submit-expense`. The expense moves to Submitted, and the event carries the expense, the 48.90 EUR amount, the EUR currency, Ava, and the identity of the fare line.
