# Reach Each Aggregate Only Through Its Root

In domain-driven design the aggregate root is the only entry point to its aggregate: callers never reach its entities, value objects, or helpers on their own, and the root alone decides how its state changes. This decision states that rule, narrows domain services to calculations no aggregate owns, and splits large roots into one file per action.

## Context

LiteSpec stated parts of the entry rule but never the rule itself. Named factories, one repository per aggregate, and invariants in Domain each guard one side, while no provision said that callers reach an aggregate only through its root. The domain service text invited the opposite: Application loads the aggregates, calls the service, and passes the result into aggregate behavior. Under that shape, handlers build aggregate parts, choose which rule applies, and depend on call order, which is exactly the knowledge the root should hide. Invariants then hold only if every caller follows the same steps, and a new caller, a reaction, or a job can skip one.

A large root had no sanctioned shape either. Every posting rule sat inside one root file because no convention offered a better answer that keeps one entry point.

## Options

LiteSpec compared 2 options for the aggregate entry rule. The table sets each option against the same two points: what it gains and what it costs.

| Option | Gains | Costs |
|:---|:---|:---|
| State the root-only entry rule, narrow domain services to ownerless calculations, and split large roots into one file per action | Every caller passes plain inputs to the root, which owns validation, order, and events; a large root splits into partial files without opening a second entry point. | Three new analyzers and a narrowed service provision to keep, and existing callers that build aggregate parts move that work into the root. |
| Keep the entry rule unstated and let callers combine services with aggregate behavior | No new provisions and no analyzer work. | Invariants hold only when every caller follows the same steps, and a new caller can skip one. |

## Decision

Code outside an aggregate reaches it only through its root's named factories and methods, passing plain inputs. A domain service holds only a calculation that spans aggregates with no owning aggregate. A root past 500 lines splits into one partial file per business action.

## Consequences

The new provision is `litespec/backend/domain#reach-an-aggregate-only-through-its-root`, with the .NET construction check in `litespec/backend/dotnet/domain#construct-aggregate-parts-only-inside-the-root` (LS1014). A child entity carries no public setter, mutating method, or constructor (LS1015), and an aggregate references another only through its typed identifier (LS1016, `litespec/backend/dotnet/domain#reference-other-aggregates-by-typed-identifier`).

The narrowed service provisions are `litespec/backend/domain#use-stateless-domain-services-for-ownerless-rules` and `litespec/backend/dotnet/domain#implement-domain-services-as-stateless-classes`: the root calls the service inside its own method, or Application passes the service's plain result into a root method as an input. The large-root convention is `litespec/backend/dotnet/domain#split-a-large-root-into-one-file-per-action`.

The review list asks whether every caller reaches the aggregate only through its root.

## Revisit Conditions

This decision reopens when a caller needs a shape the root cannot own, or when the construction check reports calls that pass only plain inputs.

Either case means the entry rule no longer fits how callers reach aggregates. The team then decides again where the entry point sits.
