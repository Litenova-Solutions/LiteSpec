# Draw the Boundary Around Outside Traffic

Calls, integration events, messages, and files cross the product boundary in both directions. This decision names the arrival, the system, the consent, and the failure handling of each touch, so the boundary reads from the records instead of from prose.

## Context

Inbound traffic named no arrival: a webhook-only rule excluded broker consumers and stream subscriptions. Conflicts stayed a prose requirement in the concurrency extension. Consent was a free-text string beside the notification that needed it. Notifications and external-system calls shared no stated line, so mails to people were recorded as transport. Exports and cross-module reads followed no stated pattern, and quotas sat beside rate limits with no line between them.

## Options

| Option | Gains | Costs |
|:---|:---|:---|
| Every touch names its arrival, system, consent, failure | Arrival, conflicts, consent, and messages each have a field. | Integration events name system and arrival; consent moves to records. |
| Keep arrival, consent, failure in prose | Fewer fields. | Broker consumers break the rule; consent drifts. |
| One generic outside-call record | One shape covers every touch. | Messages and calls share fields neither needs. |

## Decision

An inbound integration event names in `arrivesThrough` either the entry point of kind `webhook` a provider calls, or the external system whose broker or stream the module subscribes to. Every integration event names the external system it travels through. The `webhook` notification channel is removed: a webhook to a system is an outbound integration event.

When the project selects the concurrency extension, a command that can conflict states `conflicts` with what conflicts, which write wins, and the refusal the loser gets. Consent is recorded data: an aggregate holds it, use cases grant and withdraw it, and a rule requires it. A notification's or use case's need for consent names that rule.

Notifications keep their own type, with the line stated: a notification is a message to a person, with audience, content, consent, and channel, naming the external system that delivers it; an external-system call is transport. A file export cites its row and size settings and keeps its resume mark as aggregate state. One table says when another module's state is read through a read model, asked through a decision port, or composed on a screen. A quota a customer's plan sets is a rule; a technical rate limit stays in the extensions.

## Consequences

Mails to people become notifications sent through the mail system. The integrations extension states inbound arrival and the separated outcomes for its calls. The behavior page states the quota line.

## Evidence

[research/lower-layers-practice](research/lower-layers-practice) surveyed outside practice below Intent and found the boundary shapes this decision fields. [research/behavior-containment-evidence](research/behavior-containment-evidence) found containment by identifier sound, which the named systems and doors keep. The webhook-only exclusion was read from the provision with high confidence.

## Revisit Conditions

This decision reopens when outside traffic arrives with no entry point or external system to name in `arrivesThrough`. It also reopens when a touch needs a shape between a message and a call.
