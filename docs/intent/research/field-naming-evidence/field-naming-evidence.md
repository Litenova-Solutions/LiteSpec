# Field Naming Evidence

Every LiteSpec record is a set of named fields. A field name tells a reader what kind of value the field contains. This research record counts where one field name carries 2 meanings and where one meaning carries 2 names. It covers the schemas, the templates, and the largest consumer, a project that uses LiteSpec. Readers and checks both rely on each name meaning one thing. The record finds 5 collisions that produce misleading or invalid consumer data.

## Findings

The field set is mostly systematic, and 5 collisions cause real damage. The consumer shows invalid or misleading data from 2 collisions. The field set uses one shared header and one suffix grammar, and its link names each carry one meaning.

All 37 page types share one header: `type`, `id`, `title`, `terms`, and the type's own fields. Links are present-tense verbs, and they are plural when they contain arrays. `fulfills`, `governs`, `solves`, `decides`, and `basedOn` each carry one meaning everywhere. Every field name in both schemas is camelCase or a single lowercase word. The record gives high confidence for these shape claims, because it parsed the inventory from the schemas with a program.

Multiword names follow a suffix grammar. `<verb>By` names a deadline, `<verb>On` names an event date, `<verb>When` names a condition, and `<verb>Because` names a justification. The record gives high confidence for this claim, because it parsed the inventory from the schemas with a program.

The first collision is 2 status-like fields with unrelated values. The record header field takes one value on all 111 consumer features. The feature offer field takes 4 values. The field `states` also names aggregate state entries. The record gives no confidence for this claim.

The second collision is that bare `by` names 2 kinds of value. Goals require a date, and requirements take dates. Features take milestones, such as `v1` and `M1 Evidence`, which appear in all 39 live records, under a plain string type. Beside it, 4 sibling fields follow the `<verb>By` pattern. All 111 features carry milestone-style `by` values where the surrounding convention expects dates. The record gives no confidence for this claim.

The third collision is that `source` carries 5 meanings. It marks the origin on 13 page types and on most entries, about 2,900 consumer fields. It contains a constraint's free-text outside source. It names the CloudEvents source of an integration event. It contains a nested input origin enum and a nested output origin aggregate array under a singular name. `research.sources` is a sixth shape beside them. The record gives no confidence for this claim.

The fourth collision is that `serves` covers 3 target sets across 1,807 records. A feature serves goals, a use case, reaction, or job serves steps or goals, and a screen serves steps. A feature contributes to a goal, while a use case names the step it serves. The record gives no confidence for this claim.

The fifth collision is that `goal` is both a record type and a required prose sentence on all 112 flows. The flow link change must resolve it. The record gives no confidence for this claim.

The consumer shows invalid or misleading data from 2 collisions. All 111 features carry milestone-style `by` values. Of the use-case `serves` values, 11 name a whole flow, where the schema allows only a flow step or a goal. The record gives no confidence for these claims.

The record judges several smaller overloads as keep-and-document. `steps` on flows and `steps` on coordination aggregates overlap, so LiteSpec should rename the aggregate side, which touches 6 records. `values` on requirements and terms overlap, so the term side should become `codes`. Exceptions and integration events share `subject`. `scope`, `check`, and `code` keep consistent cores. The record gives no confidence for these judgments.

The `*Because` suffix needs repair only where it contains links. The nested `consistency.because` contains a rule list, and `wideningBecause` contains a requirement link. Both should become verb-form link names. The effect-level `onFailure` is a string, while the page-level `onFailure` is a structured object. This is the most mechanical inconsistency in the schemas. The effect-level string should take the structured shape or the name `onFailurePolicy`. The record gives no confidence for these claims.

The investigation also found schema drift outside the brief. The behavior page, its provision, and the consumer glossary template all require `layer` and `aliases` on every term. The glossary schema defines neither, and it sets `additionalProperties: false`. The template's `schemaVersion: 3` contradicts the schema's `const: 2`. All 508 consumer terms carry `layer`, so the schema is out of step. The record gives no confidence for this claim.

The consumer carries fields that LiteSpec never defines. They are `result.kind` (163, which duplicates `outcome`), `setting.readBy` (15), `notification.sentBy` (8), `notification.personal` (9), `job.errorBackoff` (4), `job.startupDelay` (3), and `rule.writes` (33, always empty). The record calls these consumer inventions. The link-shaped names show authors seeking vocabulary that LiteSpec does not offer. The record gives no confidence for this claim.

The proposed naming rule is one concept, one name, one shape, everywhere it appears. A field name keeps one concept, one value kind, and one cardinality on every record that carries it. New fields follow the existing grammar. A classifier is `kind`. A link is a present-tense verb, plural when it contains an array. A deadline is `<verb>By`, and a recorded date is `<verb>On`. A justification is `reason` or `rationale`, never a `*Because` field that contains links. The record states no confidence for this rule.

The rule also fixes the other word classes. The defining sentence of a named thing is `meaning`, and the normative sentence of a promise is `statement`. A group nests as an object only when its fields always appear together. A name that a record type carries is never reused for a prose field. The maintainer validator can check the mechanical half of the rule, which covers same type, plural as array, and `*By` and `*On` as dates. The judgment half stays a reviewed provision. The record states no confidence for these parts of the rule.

## Source Notes

The record read the schemas and templates in the repository. They are the record schema (`schemas/record.schema.yaml`), the glossary schema (`schemas/glossary.schema.yaml`), and the consumer glossary template (`templates/consumer/glossary.yaml`). It also read the core model page (`docs/core/model.md`), all on 30 October 2026. The record does not say which claim each source supports.

The count of 434 files comes from a scan of the files under `docs/`, `schemas/`, `templates/`, and `examples/`. The consumer counts come from the consumer's built model, measured on 30 October 2026. The record names no second source for the scan or the counts.

The glossary template and the glossary schema disagree on `layer`, `aliases`, and the schema version. The record reports this disagreement and treats the schema as out of step. The record could not confirm the purpose of the consumer's extra fields, so it calls them consumer inventions.
