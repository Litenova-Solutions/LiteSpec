# Expense Personal Data Erased

The product raises Expense Personal Data Erased when finance removes the personal data of a decided expense. The event carries only the expense, so a reader can tell which expense lost its personal data. The amount, the receipt and the state stay on the expense for the tax file.

## Example

Ava's train fare was reimbursed and its retention ended. Cara erases its personal data with `use-case/spending.erase-personal-data`. The expense moves to Erased, and the event carries the expense.
