# Approval Reminder

Approval Reminder is an email that tells a manager an expense still waits for a decision. It names the expense and how long it has waited. It supports `requirement/approval.decided-within-five-days`, which asks that each expense is decided in time.

## Content

The reminder names the expense and its waiting time, using the template `approval-reminder-manager`, which ships in English only. The records list one parameter, `submitterName`, so the reminder shows who submitted the expense. The records do not say how the reminder fills in the expense and the waiting time.

## Delivery

The reminder goes by email to the manager who decides the expense, as `actor/manager`, delivered through `external-system/mail-relay`. A manager holding a reminder preference gets no reminder, as the rule `rule/spending.suppressed-manager-gets-no-reminder` requires. The frequency is once, and the dedup key is `approval-reminder-{expenseId}`, so each waiting expense earns one reminder.

The reaction `reaction/spending.send-approver-reminders-on-expense-submitted` decides whether the reminder is sent. It skips an expense that is decided, so a decided expense gets no reminder.

## Example

Ben, a manager, receives the reminder about Ava's 48.90 EUR train fare. The fare has waited past the waiting time in the reaction, which the reminder delay setting sets, and Ben has not decided it. The records do not record the wording of the email. This example gives the facts it carries: the expense, Ava as the submitter and the waiting time.
