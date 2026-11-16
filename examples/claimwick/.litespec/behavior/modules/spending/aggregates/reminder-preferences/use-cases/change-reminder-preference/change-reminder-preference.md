# Change Reminder Preference

Change Reminder Preference lets a manager set whether they get a reminder for a waiting expense. It concerns managers and every employee who submits an expense. Asking for no reminder keeps the inbox quiet, and asking for reminders again brings them back.

## Flow

The manager sends whether they get reminders and, when asking for none, the reason. Spending refuses a change by anyone but the manager the preference belongs to or finance with `spending.not-preference-owner`. Otherwise the preference applies from then on: asking for no reminder records the preference, and asking for reminders again removes it. The change raises no event.

Sending the same choice again leaves the same preference in place.

## Example

Ben covers two teams and asks for no approval reminders with his reason, and spending records his preference. When Ava's fare waits past the reminder delay, the reaction finds Ben's preference and sends nothing. Ben then asks for reminders again. Spending removes his preference, and the next waiting expense earns its reminder.

When another manager changes Ben's preference, spending refuses the change with `spending.not-preference-owner`, and Ben's preference stands.
