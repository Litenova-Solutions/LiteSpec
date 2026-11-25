# Read Expense

Read Expense returns one expense with its current state to the reporting module. Reporting confirms each reimbursed expense still exists before it lists the payout, so finance never checks a payout against an expense Spending no longer holds.

## Flow

1. Reporting names one expense and the payout it holds.
2. Spending returns the expense with its current state.
3. Spending answers that no expense matches when the identity is unknown or the payout differs.

The answer carries the expense identity, the latest lifecycle state, the stored total, and the per-category subtotals. The query reads the expense as stored and changes nothing.

## Example

Cara, in finance, lists the reimbursed expenses of the week. Reporting passes the 48.90 EUR fare it holds and reads the expense through this query, confirming the fare before it lists the payout to Cara.
