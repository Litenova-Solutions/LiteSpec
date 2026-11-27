# Name Fields in Record Context

A field name means what its record type says it means. Readers meet the name inside the record, so the name fits that record.

## Context

Decision 0096 required one meaning per field name everywhere, so new fields took unnatural names to avoid collisions.

Readers already read a field inside its record type. The 20 known collisions caused no misread in context, while the global rule pushed authors toward names their readers would never use.

## Options

LiteSpec compared 3 options. The table sets each against what it gains and what it costs.

| Option | Gains | Costs |
|:---|:---|:---|
| Read each field name in its record type | New fields take the names their readers already use. Record types keep short, natural names. | One word means two things on two types, so cross-type queries must state the type. |
| Keep one meaning per name for every field | A reader who learned a name once knows it everywhere. | New fields take unnatural names, and authors bend the domain to fit the table. |
| Prefix every field with its record type | No collision is possible. | Every record carries stuttering names, and authors and agents type the prefix forever. |

## Decision

A field name reads in the context of its record type. A Claimwick expense record shows the shape: a rule carries `inputs` and `refusals`, while a screen carries `fields` and `states`, and nobody confuses the two.

The v7 set keeps contextual names such as `inputs`, `outputs`, `results`, `scenarios`, `refusals`, `permissions`, `entryPoints`, `fields`, `via`, `basedOn`, `states`, `scope`, `message`, `translations`, `timeZone`, `tracksState`, `direction`, `recentSignInWindow`, `job`, and `archetype`.

## Consequences

Checks qualify names by type instead of reporting a shared word. The field table lists each name under its record type with its meaning there.

This decision supersedes `decision/0096-read-field-names-in-their-record-type`: where that decision required one meaning everywhere, this one reads each name in its record.

## Revisit Conditions

Reopen this when readers misread a shared name across types, or when a cross-type query cannot state the type it means.
