# Record Check-in

Record Check-in is how a visitor stores a check-in moment with an optional note, so the web list shows it newest first. The endpoint mints the check-in identifier and stamps the endpoint clock, so the visitor only sends the note.

## Flow

1. The visitor records a check-in with a note.
2. Health stores the check-in as Recorded and raises the recorded event.

Each request creates a new check-in with a new identity, while a repeated request returns the first result instead: the checkInId key recognizes the repeat. An audit entry is stored for the created check-in, with the handler contributing its identity.

## Example

Ava opens the check-ins list in the morning. She records a check-in with the note `Morning check-in`. The endpoint mints the identifier and stamps the clock, and the check-in is stored as Recorded.
