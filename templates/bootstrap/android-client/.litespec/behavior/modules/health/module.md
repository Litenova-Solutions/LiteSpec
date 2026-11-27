# Health

Health is the module that shows the recorded check-ins of the backend sample on the phone. Ava opens the Health app to look over the check-ins recorded for her, so the module keeps the list and the detail reading the same backend rows.

## Covers

- Health shows the recorded check-ins in a list and one detail screen.
- Health reads the check-ins of the backend starter over HTTP.

## Boundaries

- Health records no check-in of its own; recording stays with the backend starter.

Its record lists no dependencies, so Health holds only the two screens and the reader behind them.

## Example

Ava opens the list after her morning check-in. Her newest check-in sits at the top as recorded, and opening it shows the recorded state behind it.
