# Settings

Spending reads one setting: the approval reminder delay. It has a default and a range, and an operator sets it. The delay sets how long an expense waits before a reminder. The receipt threshold is not a setting: the policy aggregate holds it, and a manager changes it with `use-case/spending.change-receipt-threshold`.

## Defaults

The approval reminder delay defaults to 72 hours, with a range of 1 to 120 hours. An operator sets it through the configuration path `Spending:ApprovalReminderDelay`. The setting record says to refuse a value at a bound of the range. The records give no reason for this default or range.

The reaction `reaction/spending.send-approver-reminders-on-expense-submitted` waits this delay before it reminds the approver, so the setting carries the three-day wait behind the reminder requirement.

## Changes

An operator changes the approval reminder delay through the configuration path. The records do not say how a change to the delay takes effect.
