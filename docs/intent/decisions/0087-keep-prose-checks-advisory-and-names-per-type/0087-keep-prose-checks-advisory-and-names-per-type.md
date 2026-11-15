# Keep Prose Checks Advisory and Names per Type

Contracts block a build when they break; wording should never stop a release. This decision keeps prose and naming findings advisory, scopes name uniqueness per record type, and lines the writing rules up with their own examples.

## Context

Prose and naming findings blocked beside contract findings, so a sentence could stop a release. Names had to stand unique across the product, so `actor/owner` and `role/owner` collided with no harm behind the collision. The writing rules themselves broke their own examples: `meaning` could not be a sentence where events and results needed one, titles took forms the table never showed, the active-voice example broke the rule it showed, a number anywhere asked for a measure, and ASCII covered quotations it should never touch.

## Options

| Option | Gains | Costs |
|:---|:---|:---|
| Advise on prose and naming; block on contracts | Wording never blocks; names share across types. | Wording drifts until read; shared names may confuse. |
| Block on prose and naming too | Wording stays exact. | Sentences stop releases; harmless names collide. |
| Drop prose checks | No advisory noise. | Metaphors and loose verbs return unfound. |

## Decision

The core is strict about contracts and leaves the rest to advice. Blocking stays on identifiers, references, required fields, duplicate keys, ownership, schema validity, declared effects, and access. Prose and naming checks are advisory: a finding on an advisory provision never blocks.

Names are unique per record type, not across the product, so `actor/owner` and `role/owner` coexist. The writing rules match their own examples: `meaning` may be a sentence for domain events and results; use-case and flow titles are verb phrases; the active-voice example is rewritten; a number in a requirement asks for a measure only on a quality requirement; ASCII is stated as LiteSpec's preference for its own pages, and exact quotations and names keep their characters.

## Consequences

The advisory naming provision on the edges page stops reporting at a blocking level. Authors share names across types where the domain does. Reviewers judge wording beside the corrected examples.

## Evidence

[research/check-coverage-evidence](research/check-coverage-evidence) mapped which findings enforce and which only report, which is the line this decision draws. [research/naming-grammar-evidence](research/naming-grammar-evidence) counted the inventory behind the per-type scope. The example mismatches were read from the writing page with high confidence.

## Revisit Conditions

This decision reopens when an advisory wording finding hides a contract break. It also reopens when per-type names confuse more than they free.
