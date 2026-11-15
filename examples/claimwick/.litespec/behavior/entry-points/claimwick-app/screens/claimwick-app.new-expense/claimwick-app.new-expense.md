# New Expense

The new expense form is the page where an employee files a work expense with its amount, currency, and receipt. Every later step waits on a complete submission.

## Task

The employee enters the amount and currency, attaches the receipt, and submits. An expense above EUR 25.00 needs its receipt attached before it can be submitted.

```wireframe
page
  breadcrumb "Expenses" "New expense"
  header
    heading "New expense"
    text "Expenses above EUR 25.00 need a receipt."
  region "New expense"
    field "Amount" text = "48.90"
    field "Currency" select = "EUR"
    field "Receipt" file
    message "Attach the receipt for this expense, then submit again." critical
    link "Cancel"
    action primary "Submit Expense"
```

## States

While the submission runs, the submit action shows it is working and takes no second press. When the submission is refused, the refusal sentence appears beside the receipt field. When the submission fails to reach the server, every entered value stays in place and the page offers to try again.

## Example

Ava files the train fare from the form in one step: EUR 48.90 with the receipt attached.
