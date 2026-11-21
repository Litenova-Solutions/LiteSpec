# Give Every Stated Rule a Field

A page that states a rule no field holds leaves authors writing the rule in prose, where no check reaches it. This decision gives every stated Behavior rule a field, and adds a check where one proves cheap and sure.

## Context

The pages stated rules the records could not hold. A reaction's trigger had no declared boundary. Scenarios showed rules without naming them. Nested links missed the link checks. Domain events carried fields no type described. Each gap left a rule that stayed in prose alone.

In one consumer, four reactions triggered on another module's domain event. The model check reported no finding. Claimwick's scenarios showed rules its records never named.

## Options

| Option | Gains | Costs |
|:---|:---|:---|
| A field per rule, a check where cheap and sure | No rule stays in prose alone, and each check proved itself. | Records carry more fields. |
| Prose rules with sampled review | Fewer fields. | Rules drift with no finding. |
| A check for every rule | Everything stated is verified. | Unsure checks report false findings. |

## Decision

A reaction triggers only on its own module's domain event or on an integration event.

Scenarios name the rules they show in `exercises`.

Every link is a typed identifier and goes through the link and locality checks, including the nested ones.

Domain events carry typed fields in `carries`. A domain event carries every field its integration events publish.

The cheap checks from the content review come with the fields. They report a hand-written generic refusal and the vague-word list. They also report example rows with differing keys in one scenario and a product-level external system used by one module. `term-missing` ignores names from the example cast, which removes its false findings on sentence openers.

## Consequences

Claimwick fills `exercises` on every scenario, so the docs cite one instance per rule family.

The four cross-module reactions trigger on a new integration event instead of the foreign domain event.

Each check first runs against Claimwick and the fixtures. It is added only when every finding is a true defect.

## Evidence

[research/check-coverage-evidence](research/check-coverage-evidence) mapped which links the checker enforces and which none reads. The unmapped links are the ones this decision maps. [research/lower-layers-practice](research/lower-layers-practice) confirmed exceptions that are uneven and too wide with tooling that reports nothing where a finding belongs. The cross-module reactions were counted in live records with high confidence.

## Revisit Conditions

This decision reopens when a stated rule needs a field the record cannot carry. It also reopens when an added check reports false findings on real records.
