# Approve Expense

Approve Expense is the decision a manager makes to accept a submitted expense. The employee learns the outcome from it, and finance can pay only an expense that a manager approved here. It matters because an approval is the step that moves an expense toward payment.

## Flow

The manager opens a submitted expense and approves it. Spending checks two things before it accepts the decision. It refuses the approval when the approver submitted the expense, with `spending.own-expense`. It refuses the approval when the expense is already decided, with `spending.not-submitted`.

Otherwise the expense moves to Approved, and `event/expense.approved` is raised with the approver and the decision time. No audit entry is stored for the approval. The first approval moves the expense out of Submitted, so a second approval finds it decided and is refused.

## Example

Ben approves Ava's 48.90 EUR train fare. Ava submitted the fare with its receipt, so the expense is Submitted when Ben opens it. Ben approves it, and the expense moves to Approved. The event `event/expense.approved` carries Ben as the approver.

Ben then approves the same expense again. Spending refuses the second approval with `spending.not-submitted`, because the expense is already decided.

## Edge Cases

Ben cannot approve his own expenses. When Ben submits a train fare of his own and then tries to approve it, spending refuses with `spending.own-expense`. The expense stays Submitted, because spending refused the decision.
