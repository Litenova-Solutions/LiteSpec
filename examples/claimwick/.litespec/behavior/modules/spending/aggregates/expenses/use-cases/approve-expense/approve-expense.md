# Approve Expense

Approve Expense is the decision by which a manager accepts a submitted expense. The employee learns the outcome from it, and finance can pay only an expense a manager approved here. The approval moves the expense toward payment.

## Flow

1. The manager opens a submitted expense.
2. Spending refuses the approval with `spending.own-expense` when the approver submitted it.
3. Spending refuses the approval with `spending.not-submitted` when the expense is decided.
4. Spending moves the expense to Approved and raises the approved event.

The first approval moves the expense out of Submitted, so a second approval finds it decided and is refused. Spending reads whether the approver holds the finance role from the verified claims. No audit entry is stored for the approval.

## Example

Ben approves Ava's 48.90 EUR train fare. Ava submitted the fare with its receipt, so the expense is Submitted when Ben opens it. Ben approves it, the expense moves to Approved, and `domain-event/expense.approved` carries Ben as the approver.

Ben then approves the same expense again. Spending refuses the second approval with `spending.not-submitted`, because the expense is already decided.

## Edge Cases

- Spending refuses approval of the manager's own expense with `spending.own-expense`.

The expense stays Submitted, because spending refused the decision.
