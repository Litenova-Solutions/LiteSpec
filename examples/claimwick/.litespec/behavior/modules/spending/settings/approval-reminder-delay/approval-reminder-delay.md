# Approval Reminder Delay

The approval reminder delay sets how long an expense waits without a decision before Spending reminds its approver. The reaction `reaction/spending.send-approver-reminders-on-expense-submitted` reads the setting. An operator sets the value, and the receipt threshold is not a setting because `aggregate/spending.policy` holds it.

## Defaults

The approval reminder delay defaults to 72 hours, with a range of 1 to 120 hours. An operator sets it through the configuration path `Spending:ApprovalReminderDelay`. Spending refuses a value outside the range.

The reaction waits this delay before it reminds the approver, so the setting carries the three-day wait behind the requirement `requirement/approval.decided-within-five-days`.

## Changes

An operator changes the approval reminder delay through the configuration path `Spending:ApprovalReminderDelay`.
