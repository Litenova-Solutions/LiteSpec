# Settings

Spending reads two settings: the receipt threshold and the approval reminder delay. Each has a default and a range, and each is set by the product or by an operator. The receipt threshold decides which expenses need a receipt, and the reminder delay sets how long an expense waits before a reminder.

## Defaults

The receipt threshold defaults to 25 EUR, with a range of EUR 0 to EUR 500. The product sets it. The setting record says to refuse a value at a bound of the range. The records give no reason for the default or the range. The 25 EUR default is the threshold that the example uses.

The approval reminder delay defaults to 72 hours, with a range of 1 to 120 hours. An operator sets it through the configuration key `Spending:ApprovalReminderDelay`. The setting record says to refuse a value at a bound of the range. The records give no reason for this default or range.

The reaction `reaction/spending.send-approver-reminders-on-expense-submitted` waits this delay before it reminds the approver, so the setting carries the three-day wait behind the reminder requirement.

## Changes

A manager changes the receipt threshold with `use-case/spending.change-receipt-threshold`. From then on the policy carries the new threshold. The threshold is dated: each value applies from its date, and expenses submitted before a change keep the check they passed.

An operator changes the approval reminder delay through the configuration key. The records do not say how a change to the delay takes effect.
