# Check-in Detail

The check-in detail is the screen where Ava reads the recorded state of the check-in she opened from the list. It shows the identifier and the state, drawn from the token styles and dimensions.

## Task

Ava reads the recorded state of the check-in she opened from the list. The detail reads the check-in over HTTP when the screen opens, so the rows show as placeholders while it loads.

## States

- Loading: The rows show as placeholders while the check-in loads.
- Empty: The detail never shows empty; without an identifier it shows failed.
- Failed: The detail says the check-in is unavailable.

## Example

Ava opens her morning check-in from the list. The detail shows its identifier with the recorded state, and stepping back returns her to the list.
