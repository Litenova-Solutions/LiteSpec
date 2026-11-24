# Expense Record

The expense record is the page where one expense is read and decided: its facts, its state, and the actions that state permits. Every decision and payout starts from what this page shows.

## Task

The reader checks the expense, then acts depending on who they are and what the state permits.

```wireframe
page
  breadcrumb "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Submitted" caution
    text "Filed 18 Nov 2026, 09:32 for the client visit."
    action primary "Approve" -> spending.approve-expense
    action "Refuse Expense"
    action "Attach Receipt"
    action "Reimburse Expense"
    action destructive "Erase Personal Data"
  split 2:1
    region "The expense"
      fact "Spender" "Ava"
      fact "Amount" "EUR 48.90"
      fact "Receipt" "Attached"
    aside
      region "What happened"
        timeline 2
          item: text "Submitted" · text muted "18 Nov 2026, 09:32"
  region "Refusals"
    message "You submitted this expense, so another manager must decide it." critical
```

Each action shows only when the expense state permits it: approval and refusal on a submitted expense, reimbursement on an approved one, and erasure past its retention.

## States

- Loading: The expense facts show as placeholders while the expense loads.
- Missing: The page says the expense does not exist and links back to the list.
- Refused: The refusal sentence appears beside the action.

## Example

Ben opens EXP-1042, reads the submitted expense with its receipt, and approves it, so Cara can reimburse it on Friday.
