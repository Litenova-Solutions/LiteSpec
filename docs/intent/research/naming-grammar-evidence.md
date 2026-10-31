---
{
  "type": "research",
  "id": "research/naming-grammar-evidence",
  "title": "Naming Grammar Evidence",
  "owner": "LiteSpec maintainer",
  "status": "draft",
  "decider": "LiteSpec maintainer",
  "decideBy": "2026-11-30",
  "question": "What does the 248-field inventory show about name collisions and value shapes, and which grammar follows from it?",
  "conclusion": "The inventory counts 248 field names with 18 genuine collisions plus link-shaped justifications and string mini-languages, so one grammar of eight word classes with no exceptions, objects for mini-languages, and American spelling follows.",
  "sources": [
    {
      "url": "https://cucumber.io/docs/gherkin/reference",
      "readOn": "2026-10-31"
    },
    {
      "url": "https://github.com/cloudevents/spec",
      "readOn": "2026-10-31"
    },
    {
      "path": "schemas/record.schema.json",
      "readOn": "2026-10-31"
    },
    {
      "path": "docs/intent/research/field-naming-evidence.md",
      "readOn": "2026-10-31"
    },
    {
      "path": "docs/core/model.md",
      "readOn": "2026-10-31"
    },
    {
      "path": "schemas/glossary.schema.json",
      "readOn": "2026-10-31"
    }
  ]
}
---

# Naming Grammar Evidence

## Answer

This record distils the field-name inventory and the collision analysis behind decisions D9 to D18 into the claims this release relies on. A script that walks every properties block in the record schema counted 248 distinct property names on 2026-10-31, with the declaring definitions and value shape per name, and the multi-shape table below was re-verified on the same run. Consumer illustrations are carried from the field naming evidence record, measured over the largest consumer's built model on 2026-10-30. The two outside standards were re-read on 2026-10-31.

The field set is mostly systematic. All 37 page types share one header of type, id, title, owner, status, decider, decideBy, terms, and approval fields on record-bearing types. Links are present-tense verbs, plural when they hold arrays, and fulfills, governs, solves, decides, and basedOn each carry one meaning everywhere. Every field name in both schemas is camelCase or a single lowercase word, and multiword names follow a readable suffix grammar: <verb>By deadlines, <verb>On event dates, <verb>When conditions, and <verb>Because justifications. Confidence is high for the shape claims: the inventory was parsed from the schemas programmatically, and the re-run on 2026-10-31 still counts 248 names.

Twenty-one names carry more than one shape, and eighteen are genuine collisions. The id, status, and kind names vary per type by design, since each record carries its own identifier form, its own approval state, and its own classifier. The rest share one name across shapes that a reader cannot tell apart: audience holds actor lists on a product and a notification but one permission on an integration event, which the checker rejects as an invalid field; basedOn holds two different arrays; code holds one refusal code or a list of codes; direction, effects, fields, key, mode, runbook, scope, sends, steps, and subject each span two or three shapes; onFailure is a string policy on effects and a structured object on reactions and jobs, the most mechanical inconsistency in the schemas; reason is free prose in most places but a required, optional, or none enum on audit and idempotency objects; setBy is founder or tenant on requirements but adds operator on settings; serves spans two shapes across five record types with three target sets; and source carries four shapes across 21 definitions, the transcription marker beside the constraint's free-text source, the integration event's CloudEvents source, and nested input and output provenance, with research sources as a sixth shape beside them. Two collisions already produce bad consumer data: all 111 features carry milestone-style by values where the surrounding convention expects dates, and 11 use-case serves values name a whole journey where the schema allows only a journey step or a goal. One concept therefore keeps one name, one shape, and one cardinality everywhere it appears, with no exceptions list.

Justifications that hold links and booleans each get one rule. The because field on a consistency object holds a rule list and wideningBecause holds a requirement link, while appliesBecause and refusedBecause hold prose, so cites becomes the one verb for a reason link everywhere: requirement, journey, consistency because, query wideningBecause, and retention row intent. The booleans need no rename, only a stated rule: applies, secret, recorded, builtAsynchronously, and sharesTransaction are all present-tense predicates with no is or has prefix, so a boolean stays a present-tense predicate that is true about the record when true.

String mini-languages become objects, and the remaining renames close the gaps. The bare advisory freshness, the cron and every schedule strings, the free-text duration and period fields, the string onFailure policy, the retry and dead-letter strings, and the call, via, and operator strings each parse only by convention, so each becomes an object with named fields. The use case names the settings it reads in reads, and the setting's readBy becomes a computed inverse. Field names, enum values, and prose use American spelling: license and pseudonymize. Ballot stays, defined in the glossary as the questions a decider answers together. A question carries question, decider, decideBy, ballot, and recommendation, with ballot required and recommendation required when the record carries no risk, and once answered it carries answer, answeredBy, and answeredOn, while its tier is derived from the record's risk. The word when keeps only its Gherkin meaning, an exception entry becomes condition with fallback, and a process step's guard becomes guard. The value style becomes a set of provisions with its mechanical half checked.

The inventory supports eight word classes: reason links, boolean predicates, classifiers, dates, justifications, defining and normative sentences, conditions, and outside-fixed names. Only names an outside standard fixes are exempt: the Gherkin step words given, when, and then, and the CloudEvents attributes. The Gherkin reference confirms Given, When, Then, And, and But as the step words alongside Rule, Background, and Examples, and the CloudEvents 1.0.3-wip text confirms the fixed attribute names with id, source, type, and specversion required and dataschema and subject optional. The normative class list lives on the language page; this record holds the evidence it rests on.

## Notes on Sources

The 248 count comes from a fresh script run over the current record schema on 2026-10-31, so it matches the schema this release carries; definition counts per name are truncated in the script output and only the shape lists were relied on. The consumer illustrations are second-hand through the field naming evidence record, measured 2026-10-30 over 434 scanned files and the largest consumer's built model. The Gherkin reference was read on 2026-10-31, including the keywords, Rule, and Background sections; its index page notes an update on 2026-10-27. The CloudEvents text was read as version 1.0.3-wip on the same date, including the required and optional context attributes. The glossary schema and glossary template were read for the layer, aliases, and schema-version drift the field naming record reports.
