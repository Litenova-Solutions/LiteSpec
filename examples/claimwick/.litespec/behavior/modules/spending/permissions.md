# Permissions

This permission set names what each person may do in the spending module, with one permission for each allowed action. It concerns every role that uses the module, and each use case checks one of these permissions before it runs.

## Risk

Two permissions carry a risk tag: `permission/post-reimbursement` is tagged money, because a wrong payment moves money, and `permission/remove-personal-data` is tagged personal data, because an erasure cannot be undone. The records give no risk tag to `permission/export-expenses`.

## Boundaries

- The request-expense permission does not cover approving or refusing an expense. Attaching a receipt uses the same permission, because the attach use case cites it.
- The approve-expense permission covers refusing an expense too, because the refuse use case cites it.
- The post-reimbursement permission does not cover exporting approved expenses.
- The list-expenses permission reads expenses and changes none of them.
- The manage-reminder-preference permission does not let a manager change another preference. Only finance changes a preference that belongs to someone else.
- The remove-personal-data permission does not remove the expense.
