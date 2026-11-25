# Category Is Listed

This rule holds each submission to the listed expense categories. It keeps every report on the same values, so a label the table does not hold never reaches the stored expense.

## Rationale

`rule/expense.category-is-listed` governs `use-case/spending.submit-expense` on `aggregate/spending.expense`, so the submission carries only a listed category. The decision `decision/0006-expense-categories` fixed the list, and the table `reference-data/spending.expense-category` holds the kinds with their meanings. The boundary reads the label through the generated closed set and refuses an unlisted one.

## Example

Ava files a trip to a client as travel. The submission carries the travel category from `reference-data/spending.expense-category`, and the expense keeps it.
