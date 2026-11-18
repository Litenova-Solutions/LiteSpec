# Module Dependencies Form a DAG

Module dependencies form a directed acyclic graph: each module builds on the modules below it, and no chain of edges returns to its start. This decision makes a dependency cycle a boundary defect with one fix: move the concept that is split across the two modules.

## Context

Two modules of the owner's product referenced each other's identifiers. Identity sessions held the chosen workspace, while workspace memberships named people, so neither module stated what it owned. Each module built on the other, and neither had a seam to split along.

The audit trail lived in Workspaces while its pseudonyms lived in Identity, so one concern sat split across two modules. The audit trail is a concern of its own, and splitting it across two owners left readers assembling it from both.

## Options

LiteSpec compared 2 options for module dependency cycles. The table sets each option against the same two points: what it gains and what it costs.

| Option | Gains | Costs |
|:---|:---|:---|
| Move the split concept into one module so the dependency graph stays acyclic | Every concept has one owning module. The dependency graph shows which module builds on which, and each edge has one direction. | Moving the concept changes the owning module's records, code, and stored data in one change. |
| Share the cycled identifiers through the Kernel | Neither module references the other: both reference the Kernel, and the project cycle is gone. | The cycle stays as a design fact with its project error hidden. The Kernel fills with identifiers owned by no module, and the split concept stays split. |

## Decision

Module dependencies form a directed acyclic graph over Contracts references, `queries` edges, and reactions to facts. A cycle means a concept is split across two modules, and the fix is to move the concept, never to share identifiers through the Kernel.

The library product keeps three modules in one direction:

| Module | Owns | Depends On |
|:---|:---|:---|
| Readers | Readers, cards, sign-in records | Nothing |
| Lending | Loans, holds, the current loans of each reader | Readers |
| History | The loan trail, its checkpoints and their verification, reader pseudonyms | Readers, Lending |

The reader's current loans moved from the reader aggregate to Lending, keyed by the reader identifier. The product behavior is unchanged: the loans were always lending's concept, kept in the wrong module. The loan trail and its pseudonyms moved into History, so one module owns the audit concern whole.

## Consequences

Every concept has one owning module, and the dependency graph shows which module builds on which. A dependency cycle is a finding naming both modules and the edges that close it, and its fix moves the split concept into one module.

The widened provision is `litespec/core/model#keep-module-dependencies-free-of-cycles`.

## Revisit Conditions

This decision reopens when two modules show a genuine mutual need that no moved concept resolves, or when a product shows a cycle its modules cannot break by moving one concept.

Either case means the graph no longer fits the product's concepts. The team then decides again how module dependencies are drawn.
