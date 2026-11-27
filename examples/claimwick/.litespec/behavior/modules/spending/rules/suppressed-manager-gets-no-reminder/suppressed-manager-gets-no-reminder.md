# Suppressed Manager Gets No Reminder

This rule stops an approval reminder for a manager who asked to skip them. It protects the manager's choice, and the expense still waits for its decision.

## Rationale

`rule/spending.suppressed-manager-gets-no-reminder` comes from a product choice, as its source states. A manager sets the choice with `use-case/spending.change-reminder-preference`. The reaction `reaction/spending.send-approver-reminders-on-expense-submitted` reports a suppressed outcome when the approver holds a preference, and `notification/spending.approval-reminder` names this rule as its consent rule. The requirement `requirement/approval-reminders.remind-approver-after-three-days` still drives the reminder for managers without a preference.

## Example

Ben asks for no reminders and gives a reason. Ava's 48.90 EUR fare waits past the reminder delay. When the waiting time ends, the reaction reports suppressed, so Ben gets no approval reminder.

## Limits

Suppression stops the reminder only. The expense still waits for its decision.
