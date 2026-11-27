# Consolidate Everyday Capabilities into Core

Core covers what nearly every business product needs. Extensions cover what only some products need.

## Context

Nineteen extensions covered everyday needs such as audit trails, tenancy, and scheduled work. Nearly every project selected nearly every extension.

Selection lists grew long while the fields already lived in Core schemas. Owners read nineteen pages to learn they needed most of them.

## Options

LiteSpec compared 3 options. The table sets each against what it gains and what it costs.

| Option | Gains | Costs |
|:---|:---|:---|
| Fold everyday capabilities into Core and keep five optional extensions | Typical projects select stacks only. Optional surface stays small and honest. | Core grows, and a minimal project carries fields it never uses. |
| Keep all nineteen extensions optional | Core stays small, and each project picks exactly what it needs. | Nearly every project picks nearly every extension, so selection is theater. |
| Fold everything into Core with no extensions | No selection at all. | Genuinely optional needs such as billing plans and live push burden every project. |

## Decision

Core absorbs `audit`, `tenancy`, `concurrency`, `jobs`, `outbox`, `integrations`, `locale`, and `lifecycle`. Their fields and rules live in Core behavior, persistence, and security guides.

Five extensions stay optional: `delivery`, `ai-mcp`, `compat`, `realtime`, and `plans`. A Claimwick expense product shows the shape: Core audit and tenancy with no extension selected, plus `plans` only when it bills by tier.

## Consequences

Dormant extensions such as `cache`, `customer-rules`, `devices`, and `queues` retire. Any durable rule they held moves into Core or a Stack Pack.

Project manifests name stacks and at most the five optional extensions. `validate` reports a retired extension instead of applying it.

## Revisit Conditions

Reopen this when a folded capability proves optional in practice with measured non-use, or when a retired extension regains an active consumer.
