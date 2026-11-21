# Reminder Preference

The reminder preference is one manager's choice to skip approval reminders. It concerns the manager who sets it and every employee whose expense waits, because the reminder reaction reads it before it sends. A manager holding a preference gets no reminder for a waiting expense.

## Lifecycle

The preference has one state, Active, which means the choice applies. `use-case/spending.change-reminder-preference` records the preference when the manager asks for no reminder, and removes it when the manager asks for reminders again. A manager with no preference gets reminders.

## Data and Retention

The preference holds three fields: the manager field is personal data, and the reason and the update time are business facts.

| Field | Sensitivity | Data class | Kept for |
|:---|:---|:---|:---|
| `manager` | personal | `preference-owner` | Until reminders are asked for again, then erased |
| `reason` | internal | `preference-record` | Until reminders are asked for again, then erased |
| `updatedAt` | internal | `preference-record` | Until reminders are asked for again, then erased |

When the manager asks for reminders again, `use-case/spending.change-reminder-preference` removes the preference, erasing the manager link, the reason and the update time together. A preference stays until the manager asks for reminders again; the records state no fixed period for it.

## Example

Ben asks for no approval reminders while he covers two teams, and his preference joins spending with the date and the reason. When Ava's fare waits past the reminder delay, the reaction finds Ben's preference and sends nothing. When Ben asks for reminders again, his preference leaves, and the next waiting expense earns its reminder.
