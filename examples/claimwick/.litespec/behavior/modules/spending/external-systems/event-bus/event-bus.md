# Event Bus

The event bus carries the spending module's integration events to the modules that read them. Spending publishes each submitted, reimbursed and erased expense onto it, and reporting builds its reimbursed-expense rows from what arrives. The modules never read each other's tables; the bus is the only connection between them.

## Use

Spending publishes three events through the bus: `integration-event/expense-submitted`, `integration-event/expense-reimbursed` and `integration-event/expense-personal-data-erased`. Reporting reads the reimbursement and the erasure from it. Each event travels at least once, in the order spending raised it.

## Data Shared

The bus carries what the event payloads hold: the expense identity, amount, currency, employee and times. The employee is personal data; the rest are business facts for the tax file.

## Failure Handling

A redelivery never counts twice: every event carries its identity, and each reader deduplicates on it. A reader that falls behind replays from the retained events.
