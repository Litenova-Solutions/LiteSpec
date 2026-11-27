# Note Length

This rule keeps each check-in note short, so the check-in list stays readable and each check-in stores a bounded amount of text.

## Rationale

`rule/health.note-length` is an invariant on `aggregate/health.health-check`, enforced when `use-case/health.record-check-in` takes the note. A note longer than 500 characters is refused before anything is stored.

## Example

A visitor records a check-in with the note "Slept well, walked twice." The note is 25 characters, so the check-in is stored. A note of 600 characters is refused, and the list does not change.

## Limits

This rule bounds the note length only. It does not judge what the note says.
