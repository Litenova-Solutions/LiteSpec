# Receipt

A receipt is the proof of payment that an employee attaches to an expense. It concerns the employee who paid the cost and the expense it supports. It matters because an expense above the receipt threshold cannot be submitted without one.

## Lifecycle

A receipt has one state, Attached, which means it sits on the expense. `use-case/spending.attach-receipt` creates the receipt on a submitted expense. The records show no way to remove or replace a receipt once it is attached.

## Data and Retention

The records give the receipt no data fields of its own. The expense keeps the receipt in its `receipt` field, which falls under the expense ledger and is kept for 7 years on a statute basis. The records do not say whether the receipt file is stored apart from the expense.

## Example

Ava submits a meal below the 25 EUR threshold without a receipt. The meal is Submitted. Later she attaches the receipt to the meal. The receipt sits on the expense, and `event/receipt.attached` carries the receipt identity and the expense identity.
