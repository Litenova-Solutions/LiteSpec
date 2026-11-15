# Suppression List

The suppression list names the managers who get no approval reminder. The reminder reaction reads it before it sends, so a manager on the list never gets a reminder for a waiting expense. It matters because reminders to managers who asked for none would train every manager to ignore them.

## Lifecycle

The list carries no states: it is a store, not an entity. A row appears when a manager asks for no reminders, and it leaves when the manager asks for them again. No invariant guards the rows, so no lifecycle constrains them.

## Data and Retention

Each row holds the manager, the time the manager was added, and the reason. The manager is personal data; the time and the reason are business facts. The records state no retention for the rows: a row stays until the manager asks for reminders again.

## Example

Ben asks for no approval reminders while he covers two teams. His row joins the list with the date and the reason. When Ava's fare waits past the reminder delay, the reaction finds Ben on the list and sends nothing. When Ben asks for reminders again, his row leaves, and the next waiting expense earns its reminder.
