# Permissions

The permission list names what each person may do in the reporting module, with one permission for each allowed action. It concerns finance, which lists the reimbursed expenses. It matters because the list shows the employee each expense was paid to.

## Risk

`permission/list-reimbursed-expenses` is tagged personal data, because each row names the employee who was paid.

## Boundaries

`permission/list-reimbursed-expenses` lists the reimbursed expenses and changes none of them. It does not grant the right to reimburse an expense, which the spending module names in `permission/post-reimbursement`.
