# Threshold Never Negative

This rule keeps the receipt threshold at zero or above, so the policy never asks for proof on every expense. It protects employees from pointless filing and finance from a threshold the company did not agree to.

## Rationale

`rule/policy.threshold-never-negative` is an invariant on `use-case/spending.change-receipt-threshold`, where a manager sets the policy threshold. The use case notes that a bad threshold asks for proof on every tiny expense or lets large ones go without. The refusal `refusal/spending.negative-threshold` names the negative case, and the result tells the manager to enter a threshold of zero or more. The requirement `requirement/approval.above-threshold-receipt` depends on a usable threshold.

## Example

The company agreed a receipt threshold of 25 EUR. When a manager sets that value, Spending stores it on `aggregate/spending.policy`. If the manager enters a negative threshold, Spending refuses with `spending.negative-threshold`.

## Limits

This rule checks the sign of the threshold only. The value itself stays a policy choice, as `decision/0001-receipt-threshold` states.
