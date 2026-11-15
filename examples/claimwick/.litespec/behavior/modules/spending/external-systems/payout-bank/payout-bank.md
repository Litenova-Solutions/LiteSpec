# Payout Bank

The payout bank moves reimbursed money to employees. Claimwick sends one bank file per batch and reads back a settlement report. The payout ends each expense: money the bank never moves stays owed.

## Use

Finance exports the approved expenses of a period, and the payout batch carries them to the bank as one file. The bank settles the batch and reports back per payout, so finance can check each payment against its expense.

## Data Shared

The bank holds the payout batches of one period: the expense identities, amounts and currencies. These are the ledger facts of the expenses it pays.

## Failure Handling

The bank rejects a payout file that does not balance. Finance then fixes the entry and re-sends the batch. A batch with an unknown outcome is reconciled against the settlement report before anything is re-sent.
