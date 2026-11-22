# Expense Approved

The product raises Expense Approved when a manager decides a submitted expense in its favor. The event carries the expense, the manager who approved it and the time of the decision. Finance uses the approved state to build the payout batch, and the employee learns the outcome.

## Example

Ben approves Ava's 48.90 EUR train fare with `use-case/spending.approve-expense`. The expense moves to Approved, and the event carries the expense, Ben as the approver and the decision time.
