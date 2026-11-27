# Read Check-in

Read Check-in is how the record screen shows one stored check-in with the moment it was recorded and its note. A check-in the caller names that nothing stored answers with the unknown refusal.

## Flow

1. The record screen names one check-in.
2. Health returns the check-in with its recorded state.
3. Health refuses the read with `health.check-in-unknown` when the check-in is unknown.

## Example

Ava recorded a check-in in the morning. She opens it from the list, and the record screen shows the recorded moment, the note, and the Recorded state. A stale link naming no stored check-in answers 404 with the unknown refusal.
