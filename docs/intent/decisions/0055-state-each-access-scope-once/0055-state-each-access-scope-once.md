# State Each Access Scope Once

Access in LiteSpec names a role, the reach of that role, and the permissions the role allows. This decision gives the scope of a grant one home, the role, so that the reach of a role is stated once and read the same wherever it is used.

## Context

Scope appeared three times: on the role, on each entry of an actor's `holds` list and on each permission. The access page said both that the same role grants different reach in different places and that every role carries exactly one scope. No rule said what happened when the three disagreed.

In the reference consumer, each hold's scope repeated its role's scope. A scope level was a bare label, so a reader could not tell what the level `tenant` covered in one product.

## Options

LiteSpec compared 3 options for where the scope of a grant lives. The table sets each option against the same two points: what it gains and what it costs.

| Option | Gains | Costs |
|:---|:---|:---|
| Scope on the role only | One field holds the reach, and no record repeats it. | The analyzers read the role's scope, and the tenant or team a person holds moves to runtime data. |
| Scope on each hold only | One role can be held at different levels. | The role's reach is undefined until someone holds it. |
| Three fields with an equality rule | Each record names its scope. | Three copies of one value must stay equal. |

## Decision

The role holds the scope of its grant. An actor's `holds` entry names the role only, and a permission names no scope. The instance that a person holds a role for, such as a particular tenant or team, is runtime data held by role assignments.

A permission that two roles allow at different levels takes its reach from the role the caller holds. Holding a permission never bypasses a rule refusal, because a permission allows a call and the call still meets every rule.

Each scope level a role names has a glossary term that states what the level covers in this product. A permission name opens with a verb from the use-case verb table, or with manage.

## Consequences

`scope` is removed from `holds` entries and from permission entries, and `holds` becomes a list of role identifiers. The Access page's concepts for actors, roles and scopes and permissions match this rule, and the rule that the role is the scope's only home is stated once there.

`model check` reports `scope-term-missing` for a role scope with no glossary term titled with the level name and Scope, and `permission-verb-unknown` for a permission name whose verb is not in the table. The permission-list explanations state Risk and Boundaries, with Boundaries naming what a reader might assume a permission allows but it does not.

## Revisit Conditions

This decision holds until one actor must hold one role at different levels. It also holds until the analyzers can generate permission code from role scopes without loss. Either report reopens the choice.
