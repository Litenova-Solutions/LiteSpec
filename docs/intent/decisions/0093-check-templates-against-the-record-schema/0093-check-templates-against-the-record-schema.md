# Check Templates Against the Record Schema

Each LiteSpec template teaches the shape of its record type, and the check behind it decides what a template may hold. This decision checks each template against the record schema with placeholder values, so a template stays teachable while the example product changes beside it.

## Context

Decision 0056 made each YAML template a copy of one Claimwick record, and `litespec-maintain validate` reported a template that differs from its source. Every link in a template resolved in one coherent product.

The copy tied each template to the example product: a template changed when its Claimwick record changed, and Claimwick had to carry every record the templates need, whether or not the example itself needed it. A template could satisfy its source while teaching a shape the record schema refuses.

## Options

| Option | Gains | Costs |
|:---|:---|:---|
| Check each template against the record schema with placeholder values | The template stays teachable while the example changes; every field shows its schema shape. | Placeholders read as unfinished; the check fills each one before it validates. |
| Keep each template as a copy of a Claimwick record | Every link resolves in one product. | A template follows its record, and Claimwick must carry each record the templates need. |
| Check that templates parse without checking their shape | The check never blocks on a template. | Templates drift from the schema and teach shapes the schema refuses. |

## Decision

Each record template in `templates/consumer/` satisfies the record schema with a placeholder value in every field. `litespec-maintain validate` fills each placeholder with a value its field accepts and reports `TEMPLATE_SCHEMA` for a template that then fails the schema. The source comparison and `TEMPLATE_SOURCE_DIFFERS` are removed.

`model check` still reports `template-value-unchanged` for a scaffolded record that keeps a copied value unchanged, so a copied value cannot pass as a decision.

## Consequences

Templates no longer follow Claimwick records, and Claimwick no longer carries records only the templates need. This decision supersedes `decision/0056-copy-each-template-from-claimwick`.

## Revisit Conditions

This decision reopens when a template needs a shape the record schema cannot state, or when a placeholder value reads as a decided value.
