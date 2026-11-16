# Read Field Names in Their Record Type

A reader meets a field inside one record, so the record type gives the name its meaning. This decision reads each field name in its record type, keeps one name for one concept across types, and keeps three checked guards.

## Context

The review counted 291 field names across 100 definitions and found about 20 with more than one meaning today. The same word names different closed sets on different types: `kind` names 11 different closed sets, `mode` names 9, and `name`, `key`, `from`, `value`, `outcome`, `source`, `target` and `fields` each carry several unrelated meanings. `cites` names a requirement's reasons and a flow step's requirements, already read as carries. `action` names an operation's send or receive and a dead-letter policy's prose action.

The stated policy claimed one meaning per word everywhere, while the validator enforced a narrower rule: a name holds one coarse JSON type everywhere, a singular record-type name holds links to that type, a link verb holds links, and a date name holds a date. The stated policy pushed new fields toward less natural names, with `coverage` where every reader says `scope` and `displayStates` where every UI designer says `states`, while the schema already read names in context.

A few existing names also read poorly for reasons apart from collisions: abbreviations, names that look like flags but hold links, and one name holding two link meanings.

## Options

LiteSpec compared 2 options for field names. The table sets each option against the same two points: what it gains and what it costs.

| Option | Gains | Costs |
|:---|:---|:---|
| Read each field name in its record type, keep one name per concept across types, and keep three checked guards | New fields take the names their readers already use, such as a constraint's scope and a screen's states, and the stated rule matches the schema, where about 20 names already mean different things on different types. | One word can mean two things on two types, so the book labels and explains fields per type where a name is shared. |
| Keep one meaning per field name across every record type | A reader who learned a name once knows it everywhere. | New fields take less natural names, and about 20 existing names already depart from the rule, among them kind, mode, name, key and fields. |

## Decision

A field name is read in the context of its record type. The same word may mean different things on different types when each reads naturally where it stands.

One concept keeps one name across types, so `summary`, `fulfills`, `excludes` and `terms` read the same everywhere.

Three guards stay and stay checked by `litespec-maintain validate`. A field named after a record type, singular or plural, holds links to that type. A link verb holds links. A date name holds a date. The same-shape check across types is removed.

The book labels and explains a shared name per record type.

Eleven fields are renamed for readability in this release, and an external operation's timeout becomes an ISO 8601 duration. The records-complete fields take their natural names, including a constraint's `scope` and a screen's `states`.

## Consequences

A field name is read in the context of its record type. One concept keeps one name across types. `litespec-maintain validate` keeps three guards: a field named after a record type, singular or plural, holds links to that type; a link verb holds links; a date name holds a date. The same-shape check across types is removed. The book labels and explains a shared name per record type.

Eleven fields are renamed for readability: product `addresses` to `users`, screen `address` to `route`, flow-step `cites` to `carries`, `of` to `identifies`, `auth` to `authentication`, `dedupKey` to `deduplicationKey`, `params` to `parameters`, `requiresConsent` to `consentRule`, `recentSignIn` to `recentSignInLimit`, `atBound` to `outOfRange`, and waiver `exit` to `exitWhen`. An external operation's `timeout` becomes an ISO 8601 duration. The records-complete fields take their natural names: a constraint's `scope` and a screen's `states`.

The new provision is `litespec/core/language#read-each-field-name-in-its-record-type`.

## Revisit Conditions

This decision reopens when readers or agents misread a shared field name because of its other meaning, or when a check needs one meaning per name to work.

Either case means the names no longer read plainly in their records. The team then decides again whether names stay read per record type.
