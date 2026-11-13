# Approval

Approval is the Claimwick feature in which employees submit work expenses, managers decide them, and finance reimburses the approved ones. It is part of `module/spending`. Expenses arrive every day, and each one needs a timely decision. Where the policy asks for it, each expense also needs a receipt as proof.

## Capabilities

The feature lets employees submit expenses with receipts, lets managers decide each one, and lets finance reimburse and export the approved ones. Five requirements state the rules behind these jobs.

| Requirement | Rule | Reason |
|:---|:---|:---|
| `requirement/approval.decided-within-five-days` | Each expense is decided within 5 business days. | Late decisions stall employees and payouts. |
| `requirement/approval.receipt-over-threshold` | An expense above the receipt threshold carries a receipt. | Proof above the line keeps the tax file defensible. |
| `requirement/approval.no-self-approval` | Nobody approves their own expense. | A second pair of eyes keeps approvals honest. |
| `requirement/approval.single-reimbursement` | An approved expense is reimbursed at most once. | A double payment is hard to take back. |
| `requirement/approval.erase-after-retention` | The personal data of a decided expense is erased after its retention. | Past its need, personal data is a risk with no use. |

The 25 EUR threshold comes from `decision/0001-receipt-threshold`. The 5-day target follows `research/approval-time-benchmarks`, which supports it with one industry study. The acknowledgement of each month-end submission has its own target in `feature/approval-reminders`, which sets it at 2 seconds.

## Success Criteria

The feature works when every expense is decided within 5 business days, and the employee sees the outcome without asking.

Employees learn the outcome within days instead of weeks, so they stop chasing managers by email. That chase is the problem in `problem/expenses-wait-weeks`. Finance pays from a file it can defend. The median time is measured in `goal/decided-in-days`, which sets a target of under 120 hours.

## Limits

Reminders for late decisions are not part of this feature. They belong to `feature/approval-reminders`.

The goal names that feature as one that advances `goal/decided-in-days`, so the reminders serve the same target from their own feature.

## Example

Ava's 48.90 EUR train fare shows the feature at work. The fare is above the 25 EUR threshold, so it needs a receipt. Ava submits the fare on Monday with the receipt attached.

Ben approves it on Tuesday, 1 business day later. Cara reimburses it on Friday, and Ava sees the paid state in her list. Ben's decision falls well inside the 5 business days. Every approved expense above the threshold carries its receipt, so finance pays from a file it can defend.
