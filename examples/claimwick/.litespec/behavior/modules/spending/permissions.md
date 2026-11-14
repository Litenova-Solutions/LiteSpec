# Permissions

The permission list names what each person may do in the spending module, with one permission for each allowed action. It concerns every role that uses the module. It matters because each use case checks one of these permissions before it runs.

## Risk

Two permissions carry a risk tag. `permission/post-reimbursement` is tagged money, because a wrong payment moves money. `permission/remove-personal-data` is tagged personal data, because an erasure cannot be undone. The records give no risk tag to `permission/export-expenses`.

## Boundaries

A permission allows only the action its meaning states. These readings are not allowed by the list:

- `permission/request-expense` does not cover approving or refusing an expense. Attaching a receipt uses the same permission, because the attach use case cites it.
- `permission/approve-expense` covers refusing an expense too, because the refuse use case cites it.
- `permission/post-reimbursement` does not cover exporting the approved expenses. Export has its own permission.
- `permission/list-expenses` reads expenses and changes none of them.
- `permission/remove-personal-data` removes personal data of a decided expense only. It does not remove the expense.
