# Naming Grammar Evidence

LiteSpec records are written as YAML fields, and each field name should mean one thing. This research record holds the count of field names in the record schema and the collision analysis behind decisions `D9` to `D18`. From that analysis it sets one naming grammar for the fields. It matters because a reader cannot tell which shape a name holds when one name holds several shapes.

## Findings

The record schema has 248 distinct field names, and 21 of them carry more than one shape. Of those, 18 are genuine collisions. The fix is one naming grammar, in which one concept keeps one name, one shape and one cardinality everywhere it appears.

A script walked every properties block in the record schema on 31 October 2026. It counted 248 distinct property names, with the declaring definitions and the value shape for each name. The multi-shape table was re-verified on the same run, and the re-run still counts 248 names. The record gives high confidence for this count, because the inventory was parsed from the schemas by program.

The field set is mostly systematic. All 37 page types share one header: `type`, `id`, `title`, `terms` and the type's own fields. Links are present-tense verbs, plural when they hold arrays. `fulfills`, `governs`, `solves`, `decides` and `basedOn` each carry one meaning everywhere. Every field name is camelCase or a single lowercase word. The record gives high confidence for this claim, for the same reason as the count.

Most of the 21 multi-shape names are collisions that a reader cannot tell apart. The `id` and `kind` names vary by type by design, because each record carries its own identifier form and its own classifier. The other names do not vary by design. `audience` holds actor lists on a product and on a notification, but one permission on an integration event. The checker rejects that as an invalid field. `basedOn` holds 2 different arrays. `code` holds one refusal code or a list of codes. The record gives no confidence for these claims.

More names span several shapes. `direction`, `effects`, `fields`, `key`, `mode`, `runbook`, `scope`, `sends`, `steps` and `subject` each span 2 or 3 shapes. `onFailure` is a string policy on effects and a structured object on reactions and jobs. That is the most mechanical inconsistency in the schemas. `reason` is free prose in most places, but on audit and idempotency objects it is an enum that is required, optional or absent. `setBy` is `product` or `tenant` on requirements, and it adds `operator` on settings. The record gives no confidence for these claims.

`serves` spans 2 shapes across 5 record types, with 3 target sets. `source` carries 4 shapes across 21 definitions. Those are the origin marker, the constraint's free-text source, the integration event's CloudEvents source, and the nested input and output origin. Research sources are a sixth shape beside them. The record gives no confidence for these claims.

The consumer already shows bad data from 2 collisions. All 111 features carry milestone-style `by` values, where the convention expects dates. Of the use-case `serves` values, 11 name a whole journey, where the schema allows only a journey step or a goal. The record gives no confidence for these claims.

One concept therefore keeps one name, one shape and one cardinality everywhere it appears, with no exemption list. The record gives no confidence for this rule.

Justifications that hold links each get one rule. The `because` field on a consistency object holds a rule list, and `wideningBecause` holds a requirement link. `appliesBecause` holds prose. So `cites` becomes the one verb for a reason link everywhere. It covers the requirement, the journey, the consistency `because`, the query `wideningBecause` and the retention row intent. The record gives no confidence for this rule.

The boolean fields need no rename. `applies`, `secret`, `recorded`, `builtAsynchronously` and `sharesTransaction` are present-tense predicates with no `is` or `has` prefix. So a boolean stays a present-tense predicate that is true about the record when it is true. The record gives no confidence for this rule.

String mini-languages become objects with named fields. These are the bare advisory freshness, the cron and every schedule string, the free-text duration and period fields, the string `onFailure` policy, the retry and dead-letter strings, and the `call`, `via` and `operator` strings. Each of them parses only by convention today. The record gives no confidence for this rule.

Several remaining renames close the gaps. The use case names the settings it reads in `reads`, and the setting's `readBy` becomes a computed inverse. Field names, enum values and prose use American spelling, such as `license` and `pseudonymize`. The word `when` keeps only its Gherkin meaning. An exception entry becomes `condition` with `fallback`, and a coordinator step names its guard `guard`. The value style becomes a set of provisions, and their mechanical half is checked. The record gives no confidence for these renames.

The inventory supports 8 word classes. They are reason links, boolean predicates, classifiers, dates, justifications, defining and normative sentences, conditions, and names fixed by an outside standard. Only names that an outside standard fixes are exempt from the grammar. Those are the Gherkin step words `Given`, `When`, `Then`, `And` and `But`, and the CloudEvents attributes. The Gherkin reference confirms the step words, along with `Rule`, `Background` and `Examples`. The CloudEvents 1.0.3-wip text confirms that `id`, `source`, `type` and `specversion` are required, and that `dataschema` and `subject` are optional. The record gives no confidence for this claim.

The normative list of classes lives on the language page. This record holds only the evidence that the list rests on. The record states no confidence for this division of work.

## Source Notes

The 248 count comes from a fresh script run over the current record schema on 31 October 2026. The run matches the schema this release carries. The script's definition counts per name are truncated in its output, so the record relied only on the shape lists.

The consumer illustrations come second-hand from `research/field-naming-evidence`. That record measured them on 30 October 2026, over 434 scanned files and the largest consumer's built model. The Gherkin reference was read on 31 October 2026, including its keywords, Rule and Background sections. Its index page notes an update on 27 October 2026. The CloudEvents text was read as version 1.0.3-wip on the same date.

The record also read the glossary schema (`schemas/glossary.schema.yaml`) and the core model page (`docs/core/model.md`). It read them to check the layer, aliases and schema-version drift that `research/field-naming-evidence` reports. The record does not say which claim each source supports. It names no claim it could not confirm.
