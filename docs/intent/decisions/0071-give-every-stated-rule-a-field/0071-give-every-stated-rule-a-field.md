# Give Every Stated Rule a Field

A page that states a rule no field holds asks authors to write the rule in prose, where no check reaches it. This decision gives every stated Behavior rule a field, and a check where one proves cheap and sure.

## Context

The pages stated rules the records could not hold. A reaction's trigger stayed within no declared boundary. Scenarios showed rules without naming them. Nested links skipped the link checks. Domain events carried fields no type described. Each gap left a rule that lived only in prose.

In one consumer, four reactions triggered on another module's domain event, and the model check stayed silent. Claimwick's scenarios showed rules its records never named.

## Options

| Option | Gains | Costs |
|:---|:---|:---|
| A field per rule, a check where cheap and sure | No rule lives only in prose, and each check proved itself. | Records carry more fields. |
| Prose rules with sampled review | Fewer fields. | Rules drift with no finding. |
| A check for every rule | Everything stated is verified. | Unsure checks report noise. |

## Decision

A reaction triggers only on its own module's domain event or on an integration event. Scenarios name the rules they show in `exercises`. Every link is a typed identifier and goes through the link and locality checks, including the nested ones. Domain events carry typed fields in `carries`, which holds the rule that a domain event carries every field its integration events publish.

The cheap checks from the content review land with the fields: a hand-written generic refusal, the vague-word list, example rows with differing keys in one scenario, and a product-level external system used by one module. `term-missing` ignores names from the example cast, which removes its sentence-opener noise.

## Consequences

Claimwick fills `exercises` on every scenario, so the docs cite one instance per rule family. The four cross-module reactions answer a new integration event instead of the foreign domain event. Each check first runs against Claimwick and the fixtures, and lands only when every finding is a true defect.

## Evidence

[research/check-coverage-evidence](research/check-coverage-evidence) mapped which links the checker enforces and which none reads: the unmapped links are the ones this decision maps. [research/lower-layers-practice](research/lower-layers-practice) confirmed exceptions that are uneven and too wide with tooling that stays silent where it should report. The cross-module reactions were counted in live records with high confidence.

## Revisit Conditions

This decision reopens when a stated rule needs a field the record cannot carry. It also reopens when a landed check reports noise on real records.
