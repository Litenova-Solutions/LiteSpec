# Export Expenses

Export Expenses gives finance a file of the approved expenses since the last export. Finance uses the file to run the payout batch, so the file must list each approved expense. The export reads the expenses and changes none of them.

## Flow

1. Finance asks for the approved expenses of the period.
2. Spending refuses the export with `spending.nothing-approved` when none awaits it.
3. Spending writes one file with one row per approved expense.

Each row holds the expense identity, the amount, the currency, the approver and the approval time, and the file classifies its data as internal. The file holds every approved expense of the period, so the records set no paging. The records state no row limit or file size either.

## Example

Cara exports the expenses approved this week: Ava's 48.90 EUR train fare, approved by Ben, and one other expense from the same week.

The file holds two rows, each giving the expense identity, the amount and currency, Ben as the approver, and the time of approval. The records do not say how the last export is marked, so this example shows no second export.

## Edge Cases

- Spending refuses an export with no approved expense with `spending.nothing-approved`.

No file is written, because no approved expense awaits it.
