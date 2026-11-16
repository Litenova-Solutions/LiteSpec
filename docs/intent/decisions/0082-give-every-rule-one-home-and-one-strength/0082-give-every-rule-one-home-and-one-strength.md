# Give Every Rule One Home and One Strength

An agent follows the standard, but the standard mixed four contracts under one scope while its pages contradicted each other, so the agent invented policy. This decision gives each rule one home, resolves the verified contradictions, and separates strength from detection and blocking.

## Context

An outside review read the core pages and verified each finding against the docs. The core mixed the record model, one implementation architecture, an agent procedure, and writing conventions under one mandatory scope.

One agent rule held `MUST` and `MAY` in a single Statement and contradicted the context rule.

The intent layout was both fixed and replaceable, while no departure from a placement finding was allowed.

One page called every cross-module link blocking while the model page allowed declared ones.

`SHOULD` needed a waiver on one page and none on another.

Strength, detection, and blocking were tangled: the level letter of a finding decided blocking instead of the strength of its provision.

## Options

| Option | Gains | Costs |
|:---|:---|:---|
| One home per rule, contradictions resolved, strength apart | Each rule owned once; findings block by strength. | Rules move; authors learn new grades. |
| Patch each contradiction where found | Smaller move. | The next review finds the next pair. |
| Keep the core as the whole contract | One scope to read. | Four contracts stay mandatory as one. |

## Decision

Each glossary entry gains `home`, the page that owns the concept. Architecture rules move out of `docs/core/` into the architecture pages and the stack pages; the core keeps the record model, links, ownership, authority, evidence, the agent protocol, and writing. The pass removes the restated rules from neighboring pages, leaving citations.

The contradictions resolve one by one:

- The context rule states: query the records before changing behavior, access, a contract, or a trace link. A prose-only correction of an existing record may use the record and its explanation without a query.
- The local-pattern provision is absorbed into the context rule.
- The intent layout is fixed.
- Cross-module links follow the one rule on the model page.
- Placement uses four words: scope, containment, storage, and book grouping.

Strength, detection, and blocking separate. Strength is required, default, or advisory. `MUST` is required, `SHOULD` is advisory and needs no waiver, and a default is replaced by a project convention. The level letter of a finding states what it rests on. Whether it blocks follows from the strength of the provision it reports.

Authority reports conflicts. A lower record never overrides a higher one. A contradiction is reported, then resolved by updating or removing the inconsistent records. Only an explicit product `excludes` overrides a requirement. Behavior found in code with no record above it gains a record or a finding; nobody removes it for lack of a record.

## Consequences

A release never passes on a missing reader: under `model check --release`, a claimed layer whose reader is missing or failing blocks, unless a waiver with an expiry parks it.

The core stays strict about contracts; modeling and implementation choices belong to profiles and defaults.

## Evidence

[research/check-coverage-evidence](research/check-coverage-evidence) mapped which links the checker enforces and which it only reports, which is the detection half this decision separates. [research/comparable-rationale-practice](research/comparable-rationale-practice) found the specification remaining the only normative surface, which one home per rule keeps. Each contradiction above was verified against the pages with high confidence.

## Revisit Conditions

This decision reopens when a rule needs two homes to be found. It also reopens when a finding's strength misgrades its harm.
