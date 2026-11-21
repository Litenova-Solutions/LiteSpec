# Naming Grammar Evidence

LiteSpec records are written as YAML fields, and each field name should mean one thing. This research record holds the count of field names in the record schema and the collision analysis behind decisions `D9` to `D18`. From that analysis it sets one naming grammar for the fields. It matters because a reader cannot tell which shape a name holds when one name holds several shapes.

## Findings

The [record schema][record-schema] holds 248 distinct field names. Twenty-one names carry more than one shape, and 18 are genuine collisions. A reader cannot tell the colliding shapes apart. The record infers one naming grammar where each concept keeps one name, shape, and cardinality.

A script walked every properties block in the [record schema][record-schema] on 31 October 2026. The count lists the declaring definitions and the value shape for each name. The same run re-verified the multi-shape table and still counts 248 names.

The record calls the field set mostly systematic [record schema][record-schema]. All 37 page types share one header of `type`, `id`, `title`, and `terms`. Links read as present-tense verbs, plural when they hold arrays. The verbs `fulfills`, `governs`, `solves`, `decides`, and `basedOn` each carry one meaning. Every field name reads as camelCase or as a single lowercase word.

The `id` and `kind` names vary by type by design [record schema][record-schema]. The `audience` name holds actor lists on a product and a notification but one permission on an integration event. The `basedOn` name holds two different arrays. The `code` name holds one refusal code or a list of codes.

Ten names each span two or three shapes, namely `direction`, `effects`, `fields`, `key`, `mode`, `runbook`, `scope`, `sends`, `steps`, and `subject` [record schema][record-schema]. The `onFailure` name holds a string policy on effects and a structured object on reactions and jobs. The record calls `onFailure` the most mechanical inconsistency in the schemas. The `reason` name holds free prose in most places but an enum on audit and idempotency objects. The `setBy` name holds `product` or `tenant` on requirements and adds `operator` on settings.

The `serves` name spans two shapes across five record types with three target sets [record schema][record-schema]. The `source` name carries four shapes across 21 definitions. Those shapes are the origin marker, the constraint free text, the CloudEvents source, and the nested origin. Research sources form a sixth shape beside the source shapes.

The record counts two collisions already showing bad data in the consumer [field naming evidence][field-naming-evidence]. All 111 consumer features carry milestone-style `by` values where the convention expects dates. Eleven use-case `serves` values name a whole flow where the schema allows only a flow step or a goal.

The record infers one name, one shape, and one cardinality for each concept, with no exemptions [record schema][record-schema].

The `because` field holds a rule list and `wideningBecause` holds a requirement link [record schema][record-schema]. The `appliesBecause` field holds prose. The record infers `cites` as the one verb for a reason link everywhere.

The `applies`, `secret`, `recorded`, `builtAsynchronously`, and `sharesTransaction` fields read as present-tense predicates [record schema][record-schema]. The record infers that a boolean stays a present-tense predicate true about the record.

The bare freshness, schedule, duration, string `onFailure` policy, retry, dead-letter, `call`, `via`, and `operator` values parse only by convention [record schema][record-schema]. The record infers objects with named fields for each string mini-language.

The record infers `reads` for the settings a use case reads, with `readBy` as a computed inverse [record schema][record-schema]. The record infers American spelling for field names, enum values, and prose, such as `license` and `pseudonymize`. The record infers that the word `when` keeps only its Gherkin meaning [Gherkin reference][cucumber-docs-gherkin-reference]. The record infers `condition` with `fallback` for an exception entry and `guard` for a coordinator guard. The record infers provisions for the value style, with checks covering their mechanical half.

The inventory supports eight word classes, including reason links, boolean predicates, classifiers, dates, justifications, defining and normative sentences, conditions, and outside-standard names [record schema][record-schema]. The record infers an exemption only for names fixed by an outside standard. The [Gherkin reference][cucumber-docs-gherkin-reference] confirms the step words `Given`, `When`, `Then`, `And`, and `But`. The Gherkin reference also confirms `Rule`, `Background`, and `Examples`. The [CloudEvents specification][github-cloudevents-spec] text confirms `id`, `source`, `type`, and `specversion` as required. The CloudEvents text confirms `dataschema` and `subject` as optional.

The language page holds the normative list of classes, and this record holds only the evidence [record schema][record-schema].

## Source Notes

The [Gherkin reference][cucumber-docs-gherkin-reference] was read on 31 October 2026, including its keywords, `Rule`, and `Background` sections. Its index page notes an update on 27 October 2026.

The [CloudEvents specification][github-cloudevents-spec] text was read as version 1.0.3-wip on 31 October 2026.

The 248 count comes from a fresh script run over the current [record schema][record-schema] on 31 October 2026. The script output truncates definition counts per name, so the record relied only on the shape lists.

The consumer illustrations come second-hand from [field naming evidence][field-naming-evidence]. It measured them on 30 October 2026 over 434 scanned files and the largest consumer built model.

The record read the core [model page][model] to check the layer, aliases, and schema-version drift that `research/field-naming-evidence` reports.

The record read the [glossary schema][glossary-schema] to check the layer, aliases, and schema-version drift that `research/field-naming-evidence` reports.

[cucumber-docs-gherkin-reference]: https://cucumber.io/docs/gherkin/reference "Read on 2026-10-31"
[github-cloudevents-spec]: https://github.com/cloudevents/spec "Read on 2026-10-31"
[record-schema]: ../../../../schemas/record.schema.yaml "Read on 2026-10-31"
[field-naming-evidence]: ../../../../docs/intent/research/field-naming-evidence/field-naming-evidence.md "Read on 2026-10-31"
[model]: ../../../../docs/core/model.md "Read on 2026-10-31"
[glossary-schema]: ../../../../schemas/glossary.schema.yaml "Read on 2026-10-31"
