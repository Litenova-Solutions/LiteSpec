# Payout Bank

The payout bank moves reimbursed money to employees. Claimwick sends one bank file per batch and reads back a settlement report. It matters because the payout ends each expense: money the bank never moves stays owed.

## Use

Finance exports the approved expenses of a period, and the payout batch carries them to the bank as one file. The bank settles the batch and reports back per payout, so finance can check each payment against its expense.

## Data Shared

The bank holds the payout batches: the expense identities, amounts and currencies of one period. These are the ledger facts of the expenses it pays.

## Failure Handling

The bank rejects a payout file that does not balance, and finance fixes the entry and re-sends the batch. A batch with an unknown outcome is reconciled against the settlement report before anything is re-sent.
