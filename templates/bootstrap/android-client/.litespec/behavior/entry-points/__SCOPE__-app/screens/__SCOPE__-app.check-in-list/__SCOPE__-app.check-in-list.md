# Check-in List

The check-in list is the screen where Ava finds the check-in from this morning. It shows the recorded check-ins newest first with the state of each, drawing each row from the token styles and dimensions.

## Task

Ava scans the recorded check-ins to find the one from this morning and opens it. The store loads once at startup, so the rows show as placeholders while the check-ins load.

## States

- Empty: The list says no check-in is recorded yet.
- Loading: The rows show as placeholders while the check-ins load.
- Failed: The list says the check-ins are unavailable and offers to try again.

## Example

Ava opens the list after her morning check-in. Her newest check-in sits at the top as recorded, and opening it shows the recorded state behind it.
