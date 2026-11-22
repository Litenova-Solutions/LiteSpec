# Expense Refused

The product raises Expense Refused when a manager turns down a submitted expense. The event carries the expense, the manager who refused it and the time of the decision. The employee sees the reason the manager gave, and the expense keeps its Refused state.

## Example

Ben refuses Ava's out-of-policy fare with `use-case/spending.refuse-expense` and gives a reason. The expense moves to Refused, and the event carries the expense, Ben as the manager and the decision time.
