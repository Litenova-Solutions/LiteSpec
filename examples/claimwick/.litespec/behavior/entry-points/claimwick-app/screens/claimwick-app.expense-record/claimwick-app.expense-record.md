# Expense Record

The expense record is the page where one expense is read and decided: its facts, its state, and the actions that state permits. It matters because every decision and payout starts from what this page shows.

## Task

The reader checks the expense, then approves, refuses, reimburses, attaches a missing receipt, or erases personal data past its retention, depending on who they are and what the state permits.

```wireframe
page
  breadcrumb "Expenses" "EXP-1042"
  header
    heading "EXP-1042"
    status "Submitted" caution
    text "Filed 18 Nov 2026, 09:32 for the client visit."
    action primary "Approve Expense"
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

## States

While the expense loads, the page shows its facts as placeholders. Each action shows only when the expense state permits it: approval and refusal on a submitted expense, reimbursement on an approved one, and erasure past its retention. When the expense does not exist, the page says so and links back to the list. When an action is refused, the page shows the refusal sentence beside the action.

## Example

Ben opens EXP-1042, sees it submitted with its receipt, and approves it, so Cara can reimburse it on Friday.
