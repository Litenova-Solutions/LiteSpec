# Store and Module Event Records with Proof Freshness from the Run Commit

LiteSpec filed a store as an aggregate with a flag, sent facts between two modules of one product through an external system, and called a proof fresh until a marked file changed. This decision gives the store and the module event their own record types and judges proof freshness from the run commit, so each shape says what it is and no proof outlives its code.

## Context

A store was an aggregate with `kind: store`. It had to carry no states, yet it carried the word that means a consistency boundary guarding invariants. The aggregate `owner` field pointed at an aggregate, while the domain model page said each store names its owning system or module in `owner`.

Two modules of one product could share facts only through an external system. A reaction on another module's domain event was refused with an order to use an integration event, and every integration event had to name the external system it travels through in `through`. Claimwick's payouts module reacting to the spending module's approval needed an invented message bus, which the book then showed as a dependency.

A proof went stale only when the scenario, its test body, or a file carrying the mark of the behavior it proves changed after the run. A change to an unmarked value object, such as a rounding rule in the shared kernel, left every proof of its callers verified.

## Options

| Option | Gains | Costs |
|:---|:---|:---|
| A store type, a module event type, freshness from the run commit | Plain shapes; no invented bus; stale proofs show. | Two types land; every run resolves its commit. |
| Keep the kind flag, in-product integration events, mark-based staleness | No new types; no commit tracking. | Wrong names stay; invented bus stays; proofs outlive code. |
| Add the two types but keep mark-based staleness | Record shapes land without touching proofs. | Unmarked changes still leave proofs verified. |

## Decision

Behavior gains a `store` record type. Its identifier is `store/<module>.<name>`, and it is filed under its module in a `stores` folder beside aggregates. A store carries `summary`, `owner` naming a module or an external system, `data`, `dataClasses`, `terms`, and `records`. It carries no states, no transitions, no kind, and no parts. The aggregate type loses `kind` and `owner` and reads again as a consistency boundary with states and invariants.

Links that accept an aggregate as the thing read or written accept a store too. A rule may govern how a store is used, such as "a manager the suppression list names gets no approval reminder", though a store itself holds no invariant. The repository or table configuration implementing a store carries `implements store/<module>.<name>`.

Behavior gains a `module-event` record type. A module event is a fact one module publishes for the other modules of the same product: a contract between modules, delivered in process through the transactional outbox, with no external transport. Its identifier is `module-event/<module>.<name>`, and it is filed under its publishing module in a `module-events` folder. It carries `summary`, `translates` naming one domain event of its own module, `payload` with typed fields, `terms`, and `records`, with Consumers and Compatibility in its explanation file. It carries no direction, no CloudEvents identity, no trust, and no `through`.

A module event publishes only fields its translated domain event carries, and it has exactly one publishing module, its own. A reaction triggers on a domain event of its own module, on a module event of a module its module depends on, or on an inbound integration event. A record under a module is linked from another module only along a declared module dependency, through a module event, or through an integration event. The commit stages each raised domain event for the reactions of its own module, each module event that translates it, and each outbound integration event that translates it. Read models built from another module's facts read module events. An integration event keeps `through` and stays for facts that cross the bounded context.

A scenario's proof is fresh when its run commit is the repository HEAD. It is also fresh when every path changed between the run commit and the working tree sits under the project's LiteSpec folder or is a Markdown file. Any other change leaves the proof stale, reading `last passed at <short commit>, <N> commits ago`. When git is unavailable or the run commit is unknown to the repository, the proof is stale with the reason.

## Consequences

Every check that tested `kind == store` tests the record type instead. The `leave-states-off-every-store` provision is removed, because the schema has no states field for a store. The `name-the-owner-of-every-store` provision stays, with `owner` pointing at a module or external system.

The field-carried check covers module events beside integration events as `event-field-uncarried`. The publisher check holds each module event to its own publishing module as `event-publisher-invalid`. The trigger refusal names the module event beside the domain event and the inbound integration event. The staging provision covers module events beside integration events in the same commit.

Inside the bounded context, module events and domain-event reactions travel in process through the transactional outbox with no broker. An outbound integration event uses a transport dispatcher toward the system its record names in `through`. Every event handler of a module event or domain event carries a subscriber name that survives a class rename. A read-model projection the next screen reads dispatches in process after commit, with the outbox processor as its fallback. Handlers stay idempotent, because delivery stays at least once.

`proofs.json` keeps the run commit and the outcome per scenario, and drops the scenario and test-body hashes. CI writes `proofs.json` from the run on the pull request head, so a merged pull request carries fresh proofs. The changed paths are computed once per run with git and cached per distinct run commit.

The book gives the store and the module event their own page kinds and lexicon labels, listed under their module beside aggregates, and never shows a module event as an external dependency. A stale proof shows its last passing commit and how many commits ago it passed.

## Evidence

The external review reported the store shape as F-08 and F-37, with the record schema lines holding `kind: store`, the aggregate `owner` field pointing at an aggregate, and the domain model line saying each store names its owning system or module. It reported the module-to-module gap as F-51, with the trigger refusal, the integration event definition crossing the bounded context, and the required `through` field. It reported proof freshness as F-32, with the model page line making a proof stale only on marked changes and the rounding-rule example. The file-and-line evidence in those findings stands as the basis; no new research record was written.

## Revisit Conditions

This decision reopens when a store needs states or an invariant of its own. It also reopens when modules need a broker for facts inside the bounded context, or when proof needs a signal the run commit cannot carry.
