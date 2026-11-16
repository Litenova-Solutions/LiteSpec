# Erase Personal Data

Erase Personal Data removes the personal data an old expense holds, once the retention period for it has ended. It concerns the employee who submitted the expense. Personal data is kept only for its retention period, and erasure cannot be undone.

## Flow

1. Finance names one decided expense.
2. Spending refuses the erasure with `spending.retention-not-ended` when retention stands.
3. Spending pseudonymizes the employee and raises the erased event.

The business facts stay for the tax file. It stores no audit entry. A repeat call finds nothing left to erase, so the second call changes nothing.

## Example

Cara, in finance, names Ava's 48.90 EUR train fare. The fare was reimbursed more than 7 years ago, so its retention, counted from the reimbursement, has ended. Cara runs the erasure: the employee field on the expense is pseudonymized, and the expense moves to Erased. The 48.90 EUR amount and the receipt stay on file for the tax file.

Cara runs the erasure a second time on the same expense. The second call finds nothing left to erase, and the result stays Erased.
