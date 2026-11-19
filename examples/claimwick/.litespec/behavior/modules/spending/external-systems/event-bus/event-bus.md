# Event Bus

The event bus carries the spending module's expense-submitted event to the other parts of the company that read it. Eventline provides the bus. Spending publishes each submitted expense onto it. Reporting reads the spending module's module events, not the bus.

## Use

The system actor `actor/event-bus` stands for the bus in flows. Spending publishes one event through the bus: `integration-event/expense-submitted`. Each event travels at least once, in the order spending raised it.

## Data Shared

The bus carries what the event payload holds: the expense identity. The identity is a business fact for the tax file. A redelivery never counts twice: every event carries its identity, and each reader deduplicates on it. A reader that falls behind replays from the retained events.
