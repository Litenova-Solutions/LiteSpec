# Export Carries Approved

This rule keeps the payout file to expenses a manager has approved. It protects finance from paying the wrong costs, and it protects the company from a batch that carries undecided expenses.

## Rationale

`rule/expense.export-carries-approved` governs `use-case/spending.export-expenses`. The use case says a wrong file pays the wrong expenses, so the file carries every approved expense of the period and nothing else. The refusal `refusal/spending.nothing-approved` names the case where no approved expense awaits the export. The rule keeps that case out of the payout file.

## Example

Ben approves Ava's 48.90 EUR train fare. Cara asks for the approved expenses, and Spending writes one file with one row for the fare. When no expense is approved, Spending refuses the export with `spending.nothing-approved`.
