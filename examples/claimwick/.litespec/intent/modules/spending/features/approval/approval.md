# Approval

Approval is the Claimwick feature in which employees submit work expenses, managers decide them, and finance reimburses the approved ones. It is part of `module/spending`. Expenses arrive every day, and each one needs a timely decision. Where the policy asks for it, each expense also needs a receipt as proof.

## Capabilities

The feature lets employees submit expenses with receipts, lets managers decide each one, and lets finance reimburse and export the approved ones. Five requirements state the rules behind these jobs.

| Requirement | Rule | Reason |
|:---|:---|:---|
| `requirement/approval.decided-within-five-days` | Each expense is decided within 5 business days. | Late decisions stall employees and payouts. |
| `requirement/approval.receipt-over-threshold` | An expense above the receipt threshold carries a receipt. | Proof above the line keeps the tax file defensible. |
| `requirement/approval.no-self-approval` | Nobody approves their own expense. | A second person checks each expense before it is paid. |
| `requirement/approval.single-reimbursement` | An approved expense is reimbursed at most once. | A double payment is hard to take back. |
| `requirement/approval.erase-after-retention` | The personal data of a decided expense is erased after its retention. | Past its need, personal data is a risk with no use. |

The 25 EUR threshold comes from `decision/0001-receipt-threshold`. The 5-day target follows `research/approval-time-benchmarks`, which supports it with one industry study. The acknowledgement of each month-end submission has its own target in `feature/approval-reminders`, which sets it at 2 seconds.

## Success Criteria

- An expense above the 25 EUR threshold cannot be submitted without a receipt.
- No manager can approve their own expense.
- An approved expense is reimbursed at most once.
- The employee sees the decision on each expense in their own list, without asking the manager.

The time to a decision depends on the managers as well as on the feature, so it is measured as a goal rather than listed here: `goal/decided-in-days` sets a median of under 120 hours.

## Boundaries

Reminders for late decisions are not part of this feature. They belong to `feature/approval-reminders`, which advances the same goal.

The feature depends on `decision/0001-receipt-threshold` for the 25 EUR threshold.

One question is open for the owner: whether a manager on leave hands decisions to a deputy.

## Example

Ava's 48.90 EUR train fare shows the feature at work. The fare is above the 25 EUR threshold, so it needs a receipt. Ava submits the fare on Monday with the receipt attached.

Ben approves it on Tuesday, 1 business day later. Cara reimburses it on Friday, and Ava sees the paid state in her list. Ben's decision falls well inside the 5 business days. Every approved expense above the threshold carries its receipt, so finance pays from a file it can defend.
