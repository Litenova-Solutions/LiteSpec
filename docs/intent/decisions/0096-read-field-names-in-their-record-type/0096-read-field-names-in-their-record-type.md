# Read Field Names in Their Record Type

A reader meets a field inside one record, and the name means the same wherever it stands. This decision gives every field name one meaning and one shape across every record type, and holds the table of those names where a check reads it.

## Context

The review counted 291 field names across 100 definitions and found about 20 with more than one meaning today. The same word names different closed sets on different types: `kind` names 11 different closed sets, `mode` names 9, and `name`, `key`, `from`, `value`, `outcome`, `source`, `target` and `fields` each carry several unrelated meanings. `cites` names a requirement's reasons and a flow step's requirements, already read as carries. `action` names an operation's send or receive and a dead-letter policy's prose action.

The link verbs already kept one meaning each, but the plain fields had drifted: the same word named different closed sets on different types, and no table stated which name meant what. The stated policy pushed new fields toward less natural names, with `coverage` where every reader says `scope` and `displayStates` where every UI designer says `states`, while the schema already read names in context.

A few existing names also read poorly for reasons apart from collisions: abbreviations, names that look like flags but hold links, and one name holding two link meanings.

## Options

LiteSpec compared 2 options for field names. The table sets each option against the same two points: what it gains and what it costs.

| Option | Gains | Costs |
|:---|:---|:---|
| One name per meaning and one meaning per name for every field, with a field table | A reader who learned a name once knows it everywhere, and a check holds every name to the table. | Fields with more than one meaning are renamed, starting from the known cases. |
| Read each field name in its record type | New fields take the names their readers already use. | One word means two things on two types, and about 20 existing names already collide. |

## Decision

Every field name carries one meaning and one shape across every record type that uses it: one name per meaning, one meaning per name, for every field of every record type. Each name still reads naturally where it stands, in the context of its record type.

One concept keeps one name across types, so `summary`, `fulfills`, `excludes` and `terms` read the same everywhere.

The field table holds one row per field name with its meaning, its shape, and the record types that use it. `litespec-maintain validate` reports a field name defined with two meanings or shapes, and a field outside the table.

Docs, templates, Claimwick, the book's field labels and the CLI readers follow the table.

## Consequences

The renames start from the known cases. `excludes` states what a record does not cover everywhere. A module states what it covers in `covers`, paired with `excludes`. `responsibilities` stays for actors only, and `obligations` for constraints only. Each refusal entry carries its `id` and `title`, with the code as the id's name. A research source label is `name`, a setting configuration path is `configurationPath`, and `key` carries only an idempotency key.

Eleven fields are renamed for readability: product `addresses` to `users`, screen `address` to `route`, flow-step `cites` to `carries`, `of` to `identifies`, `auth` to `authentication`, `dedupKey` to `deduplicationKey`, `params` to `parameters`, `requiresConsent` to `consentRule`, `recentSignIn` to `recentSignInLimit`, `atBound` to `outOfRange`, and waiver `exit` to `exitWhen`. An external operation's `timeout` becomes an ISO 8601 duration. The records-complete fields take their natural names: a constraint's `scope` and a screen's `states`.

The provision is `litespec/core/language#read-each-field-name-in-its-record-type`.

## Revisit Conditions

This decision reopens when a record type needs a field whose meaning no existing name carries naturally, or when readers misread a name shared across types.

Either case means the names stop reading plainly in their records. The team then decides again whether one name keeps one meaning.
