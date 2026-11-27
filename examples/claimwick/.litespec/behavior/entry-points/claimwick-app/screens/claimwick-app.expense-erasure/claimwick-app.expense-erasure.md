# Expense Erasure

The expense erasure form is the page where finance erases the personal data of one decided expense after its retention ends. The business facts stay for the tax file, and the erasure cannot be undone.

## Task

Finance confirms the expense, then erases its personal data.

```wireframe
page
  breadcrumb "EXP-1042" "Erasure"
  header
    heading "Erasure"
    text "Erasure removes the personal data of this decided expense. The amount and the receipt stay on file."
  region "Personal data erasure"
    action destructive "Erase Personal Data"
    message "The retention of this expense has not ended, so its personal data stays." critical
```

## States

- Working: The erase action shows it is working and takes no second press.
- Refused: The refusal sentence appears beside the erase action.
- Failed: The page offers to try again.

## Example

Cara opens the erasure form for the reimbursed fare, confirms its retention ended, and erases the personal data, so the tax file keeps only the business facts.
