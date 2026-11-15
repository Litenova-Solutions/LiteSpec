# Export Expenses

Export Expenses gives finance a file of the approved expenses since the last export. Finance uses the file to run the payout batch, so the file must list each approved expense. The export reads the expenses and changes none of them.

## Flow

Finance asks for an export of every expense approved since the last export. Spending refuses with `spending.nothing-approved` when no approved expense waits for it. Otherwise spending writes one file with one row for each approved expense.

Each row holds the expense identity, the amount, the currency, the approver and the approval time, and the file classifies its data as internal. The file holds every approved expense of the period, so the records set no paging. The records state no row limit or file size either.

## Example

Cara exports the expenses approved this week: Ava's 48.90 EUR train fare, approved by Ben, and one other expense from the same week.

The file holds two rows, each giving the expense identity, the amount and currency, Ben as the approver, and the time of approval. The records do not say how the last export is marked, so this example shows no second export.

## Edge Cases

No expense may be approved in a week. When Cara exports a week in which no expense was approved, spending refuses with `spending.nothing-approved`, and no file is written, because no approved expense awaits it.
