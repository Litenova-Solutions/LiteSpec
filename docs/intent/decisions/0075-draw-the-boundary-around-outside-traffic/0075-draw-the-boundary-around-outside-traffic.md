# Draw the Boundary Around Outside Traffic

Teams move calls, integration events, messages, and files across the product boundary in both directions. This decision names the arrival, the system, the consent, and the failure handling of each touch. The boundary reads from the records instead of from prose.

## Context

Inbound traffic named no arrival. A webhook-only rule excluded broker consumers and stream subscriptions. Conflicts stayed a prose requirement in the concurrency extension. Consent was a free-text string beside the notification that needed it. Notifications and external-system calls had no stated boundary, so mails to people were recorded as transport. Exports and cross-module reads followed no stated pattern. Quotas appeared beside rate limits with no line between them.

## Options

| Option | Gains | Costs |
|:---|:---|:---|
| Every touch names its arrival, system, consent, failure | Arrival, conflicts, consent, and messages each have a field. | Integration events name system and arrival; consent moves to records. |
| Keep arrival, consent, failure in prose | Fewer fields. | Broker consumers break the rule; consent drifts. |
| One generic outside-call record | One shape covers every touch. | Messages and calls share fields neither needs. |

## Decision

An inbound integration event names its arrival in `arrivesThrough`. The arrival is either the entry point of kind `webhook` a provider calls, or the external system whose broker or stream the module subscribes to.

Every integration event names the external system it travels through. The `webhook` notification channel is removed. A webhook to a system is an outbound integration event.

When the project selects the concurrency extension, a command that can conflict states `conflicts`. The field names what conflicts, which write wins, and the refusal the loser gets.

Consent is recorded data. An aggregate holds it, use cases grant and withdraw it, and a rule requires it. A notification or use case that needs consent names that rule.

Notifications keep their own type. A notification is a message to a person, with audience, content, consent, and channel, naming the external system that delivers it. An external-system call is transport.

A file export cites its row and size settings and keeps its resume mark as aggregate state.

One table states when another module's state is read through a read model, asked through a decision port, or composed on a screen.

A quota a customer's plan sets is a rule. A technical rate limit stays in the extensions.

## Consequences

Mails to people become notifications sent through the mail system.

The integrations extension states inbound arrival and the separated outcomes for its calls. The behavior page states the line between quotas and rate limits.

## Evidence

[research/lower-layers-practice](research/lower-layers-practice) surveyed outside practice below Intent and found the boundary shapes this decision turns into fields. [research/behavior-containment-evidence](research/behavior-containment-evidence) found containment by identifier sound, which the named systems and entry points keep. The webhook-only exclusion was read from the provision with high confidence.

## Revisit Conditions

This decision reopens when outside traffic arrives with no entry point or external system to name in `arrivesThrough`. It also reopens when a touch needs a shape between a message and a call.
