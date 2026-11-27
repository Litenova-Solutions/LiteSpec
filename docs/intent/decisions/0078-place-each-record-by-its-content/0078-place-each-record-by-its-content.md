# Place Each Record by Its Content

An author placing a record reads its folder for where its content belongs and its links for why it exists. This decision defines content as what a record states and changes, so each record nests only where its whole content lies.

## Context

The placement provision said a record nests under another record only when its whole content lies within it, but the Placement Table predated it. Decisions, rules, and external systems each allowed more than one home.

Reactions answering another module's event sat with the module that triggered them instead of the module they changed.

## Options

| Option | Gains | Costs |
|:---|:---|:---|
| Content is what a record states and changes | Each record nests where its content lies. | Records placed by their links move. |
| Place each record with its trigger or publisher | Cause and record sit together. | A reaction sits with a module it never changes. |
| Keep the table as written | Nothing moves. | The table contradicts the provision. |

## Decision

Content is what a record states and what it changes; its links are its reasons, not its content. A reaction answering another module's integration event sits with the module whose records it changes. An integration event sits with its one publishing module, or its receiving module when inbound. An external system sits at the lowest node holding every record that calls it: a module when one module calls it.

Each parent in the table was checked against the provision. A requirement lies within its feature, a feature lies within the one problem it answers, and a flow sits under the product, and the known cases hold.

## Consequences

The Placement Table states the integration-event and external-system homes. A product-level external system called from one module only is reported, so the system moves to its module.

No cheap and sure check was found for the general provision, so it stays guidance.

## Evidence

[research/behavior-containment-evidence](research/behavior-containment-evidence) found containment by identifier sound and complete, with results, scenarios, events, and rules finding their parent only through placement. [research/intent-hierarchy-evidence](research/intent-hierarchy-evidence) found authors following the page rather than the parent. The multi-home types were read from the table with high confidence.

## Revisit Conditions

This decision reopens when a record's content and its reasons point at different homes with no lowest node holding all of it.
