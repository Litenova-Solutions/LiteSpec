# Changed by Owner or Finance

This rule puts the choice of reminder preference in the hands of the manager it belongs to, or finance. It protects managers from another manager silencing their reminders, and it keeps expenses from going unseen.

## Rationale

`rule/reminder-preference.changed-by-owner-or-finance` governs `use-case/spending.change-reminder-preference`. The use case says that silencing the wrong manager's reminders leaves expenses unseen, so only the owner or finance changes the preference. The requirement `requirement/approval-reminders.remind-approver-after-three-days` notes that managers decide in batches, so an expense can wait unseen for weeks. The refusal `refusal/spending.not-preference-owner` names a changer who is neither the manager nor finance.

## Example

Ben asks for no reminders and gives a reason, so Spending records his preference. Another manager cannot change Ben's choice. Cara, who works in finance, may change it. When the wrong manager tries, Spending refuses with `spending.not-preference-owner`.

## Limits

This rule limits who changes a preference. It does not set the reminder delay; the setting `setting/spending.approval-reminder-delay` carries that value.
