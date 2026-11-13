# Permissions

The permission list names what each person may do in the spending module, with one permission for each allowed action. It concerns every role that uses the module. It matters because each use case checks one of these permissions before it runs.

## Risk

Each permission has a scope, which is `self`, `team` or `platform`. The records give `self` to actions on a person's own expenses, and `team` to approving or refusing expenses and to setting the receipt threshold. They give `platform` to the finance actions: reimbursing, exporting and erasing. The records do not state the reason for this split.

Two permissions carry a risk tag. `permission/reimburse-expense` is tagged money, because a wrong payment moves money. `permission/erase-personal-data` is tagged personal data, because an erasure cannot be undone. The records give no risk tag to `permission/export-expenses`.
