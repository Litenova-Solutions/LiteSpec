# Erase Personal Data

Erase Personal Data removes the personal data that an old expense holds, once the retention period for it has ended. It concerns the employee who submitted the expense. It matters because personal data is kept only for its retention period, and erasure cannot be undone.

## Flow

Finance names one decided expense. Spending erases the personal data on that expense. The employee field is pseudonymized, and the business facts stay for the tax file. The use case raises `domain-event/expense.personal-data-erased`, which announces the erasure to other modules. It stores no audit entry.

A repeat call finds nothing left to erase, so the second call changes nothing. An expense whose retention has not ended yet is refused with `spending.retention-not-ended`, so finance names only expenses past it.

## Example

Cara, in finance, names Ava's 48.90 EUR train fare. The fare was reimbursed more than 7 years ago, so its retention, counted from the reimbursement, has ended. Cara runs the erasure. The employee field on the expense is pseudonymized, and the expense moves to Erased. The 48.90 EUR amount and the receipt stay on file for the tax file.

Cara runs the erasure a second time on the same expense. The second call finds nothing left to erase, and the result stays Erased.
