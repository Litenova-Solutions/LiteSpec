# Give Every Rule One Home and One Strength

A standard mixing four contracts under one scope, with pages contradicting each other, leaves an agent to invent policy. This decision gives each rule one home, resolves the verified contradictions, and separates strength from detection and blocking.

## Context

An outside review read the core pages and verified each finding against the docs. The core mixed the record model, one implementation architecture, an agent procedure, and writing conventions under one mandatory scope. One agent rule held `MUST` and `MAY` in a single Statement and contradicted the context rule. The intent layout was both fixed and replaceable while placement findings could not be departed. One page called every cross-module link blocking while the model page allowed declared ones. `SHOULD` needed a departure on one page and none on another. Strength, detection, and blocking were tangled: the level letter of a finding decided blocking instead of the strength of its provision.

## Options

| Option | Gains | Costs |
|:---|:---|:---|
| One home per rule, contradictions resolved, strength apart | Each rule owned once; findings block by strength. | Rules move; authors learn new grades. |
| Patch each contradiction where found | Smaller move. | The next review finds the next pair. |
| Keep the core as the whole contract | One scope to read. | Four contracts stay mandatory as one. |

## Decision

Each glossary entry gains `home`, the page that owns the concept. Architecture rules move out of `docs/core/` into the architecture pages and the stack pages; the core keeps the record model, links, ownership, authority, evidence, the agent protocol, and writing. The pass removes the restated rules from neighboring pages, leaving citations.

The contradictions resolve one by one. The context rule states: query the records before changing behavior, access, a contract, or a trace link; a prose-only correction of an existing record may use the record and its explanation without a query. The local-pattern provision folds into it. The intent layout is fixed. Cross-module links follow the one rule on the model page. Placement uses four words: scope, containment, storage, and book grouping.

Strength, detection, and blocking separate. Strength is required, default, or advisory. `MUST` is required, `SHOULD` is advisory and needs no departure, and a default is replaced by a project convention. The level letter of a finding states what it rests on; whether it blocks follows from the strength of the provision it reports. Authority reports conflicts: a lower record cannot override a higher one, a contradiction is reported and resolved by updating or removing the inconsistent records, and only an explicit product `excludes` overrides a requirement. Behavior found in code with no record above it is recorded or reported, never removed for lack of a record.

## Consequences

A release never passes on a missing reader: under `model check --release`, a claimed layer whose reader is missing or failing blocks, unless a departure with an expiry parks it. The core is strict about contracts, where modelling and implementation choices stay with profiles and defaults.

## Evidence

[research/check-coverage-evidence](research/check-coverage-evidence) mapped which links the checker enforces and which it only reports, which is the detection half this decision separates. [research/comparable-rationale-practice](research/comparable-rationale-practice) found the specification remaining the only normative surface, which one home per rule keeps. Each contradiction above was verified against the pages with high confidence.

## Revisit Conditions

This decision reopens when a rule needs two homes to be found. It also reopens when a finding's strength misgrades its harm.
