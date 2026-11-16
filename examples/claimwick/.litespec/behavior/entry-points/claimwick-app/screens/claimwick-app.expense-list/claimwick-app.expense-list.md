# Expense List

The expense list is the page where an employee watches their own expenses, newest first, with the amount and the latest state of each. The employee plans around the money and needs to see what each expense waits for without asking.

## Task

The employee scans the list for one expense, opens it to read or act on it, or starts a new one. The query behind the page runs when the page loads, so the drawing holds links and no action.

```wireframe
page
  breadcrumb "Home" "Expenses"
  header
    heading "Expenses"
    text "Your expenses, newest first."
    link "New expense"
  toolbar
    field "Expense number or name" search
    field "State" select = "Any state"
  region "Expenses"
    table 6 "Expense" "Filed" "State" "Paid"
      item: link "EXP-1042" · text muted "18 Nov 2026, 09:32" · status "Submitted" neutral · text "EUR 48.90"
    text muted "1 to 6 of 6"
```

## States

- Empty: The list says it is empty and points to the new expense form. With no expenses yet, the employee follows its pointer to the new expense form.
- Loading: The rows show as placeholders while the list loads.
- Failed: The page says the list is unavailable and offers to try again.

No refusal appears here, because listing takes no input the product can refuse.

## Example

Ava opens the list after her client visit. EXP-1042 sits at the top, submitted on 18 Nov 2026 for EUR 48.90 and waiting for Ben.
