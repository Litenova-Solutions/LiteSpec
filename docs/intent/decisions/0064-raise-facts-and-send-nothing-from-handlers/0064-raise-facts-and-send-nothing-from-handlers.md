# Raise Facts and Send Nothing from Handlers

A command handler loads an aggregate, calls one act and stages the change. This decision removes the last ways a handler reached past that act: running another use case inline, and publishing its own integration events. Every follow-on act becomes a reaction to the fact the act raised, and the pipeline publishes what the act raised.

## Context

In 6.0.0 a use case declared `runs` effects with `inline` timing and `sharesTransaction`, so one command ran another inside its own transaction. It declared `emits` effects for the integration events it published, and the handler or a factory built each event from the aggregate.

One large consumer held 57 inline edges in 47 records. Its nested dispatch needed two ambient values, a per-message aggregate cache and reload methods for stale reads, and a nested command rode on its caller's commit. 69 handlers published integration events themselves.

## Options

| Option | Gains | Costs |
|:---|:---|:---|
| Handlers raise facts, reactions run what follows, and the pipeline publishes | One commit per use case, and no user code publishes. | Inline edges become eventual and need state guards. |
| Keep inline edges that state their transaction sharing | One round trip and atomic writes across use cases. | Ambient scopes, shared caches and hidden write ownership. |
| Allow act ports between modules | A typed call instead of a message. | The same nesting, hidden behind an interface. |

## Decision

A handler sends no command, query or event. Only an entry point sends a message: an endpoint, a reaction's event handler or a job, each sending one use case's message. A use case's `effects` hold `sends` and `calls` only, and a reaction or job runs at most one use case.

No record declares `emits`. An outbound integration event names the domain event it carries in `translates`. The one pipeline step that stages the raised domain events also stages each integration event that translates them, built from the domain event alone.

A module publishes its typed identifiers, its integration events and its ports. It publishes no command and no refusal, and a use case refuses only with refusals its own module declares.

## Consequences

`sharesTransaction` and inline `runs` leave the schema. Atomic consistency stays inside the writes of one use case. LS2021 reports a mediator held by a type that is not an entry point, and LS2022 reports an integration event published by user code. `event-publisher-invalid` derives publishers from `raises` and `translates`.

Four provisions state the new rules, each under an identifier that names its rule: `litespec/core/principles#share-no-transaction-across-an-edge`, `litespec/core/principles#confine-atomic-consistency-to-one-use-case`, `litespec/core/use-cases#run-follow-on-work-only-from-reactions-and-jobs` and `litespec/core/edges#publish-every-outbound-integration-event-from-the-event-it-translates`.

A consumer rewrites each inline edge as a reaction. Where the old edge relied on one transaction, the consumer writes a state guard, such as an order that waits for its hold before it takes payment.

## Evidence

[research/cross-module-coupling-practice](research/cross-module-coupling-practice) holds the modular monolith reference and the consumer counts.

## Revisit Conditions

A consumer shows a decision that no state guard can hold and that needs two use cases' writes in one transaction. Or a consumer shows a caller whose answer cannot wait for a later fact.
