# Erased After Retention

This rule keeps an employee's personal data on an expense only as long as its retention runs. It protects employees from data kept past its need, and it keeps the tax file intact for finance.

## Rationale

`rule/expense.erased-after-retention` is a precondition on `use-case/spending.erase-personal-data`, which finance runs one expense at a time. The use case notes that erasing the wrong expense cannot be undone. The requirement `requirement/approval.erased-after-retention` says personal data is erased after retention. Its rationale is direct: past its need, personal data is a risk with no use. The refusal `refusal/spending.retention-not-ended` names the case where retention still stands.

## Example

Cara erases the personal data of Ava's reimbursed fare once its retention ends. The employee field is pseudonymized, and Spending raises `domain-event/expense.personal-data-erased`. If the retention has not ended, Spending refuses with `spending.retention-not-ended`, and Ava's name stays on the expense.

## Limits

This rule covers personal data only. The amount, the receipt and the state stay on file for the tax file, as the expense data classes state.
