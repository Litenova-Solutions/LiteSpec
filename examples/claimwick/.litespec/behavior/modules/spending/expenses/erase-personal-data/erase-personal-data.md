# Erase Personal Data

Erase Personal Data removes the personal data that an old expense holds, once the retention period for it has ended. It concerns the employee who submitted the expense. It matters because personal data is kept only for its retention period, and erasure cannot be undone.

## Flow

Finance names one decided expense. Spending erases the personal data on that expense. The employee field is pseudonymized, and the business facts stay for the tax file. The use case raises no event and stores no audit entry.

A repeat call finds nothing left to erase, so the second call changes nothing. The records state one result, Erased, and no refusal. They do not say what happens when the retention has not ended yet, although finance is meant to name only expenses past it.

## Example

Cara, in finance, names Ava's 48.90 EUR train fare. The fare was reimbursed long enough ago that its retention has ended, which the records set at 7 years. Cara runs the erasure. The employee field on the expense is pseudonymized. The 48.90 EUR amount, the receipt and the Reimbursed state stay on file for the tax file.

Cara runs the erasure a second time on the same expense. The second call finds nothing left to erase, and the result stays Erased.
