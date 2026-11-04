# Folder Reference

## Purpose

Every record has exactly one parent, and its folder states that parent: the product, a module, an aggregate, or an entry point. This guide lists the one folder each record type lives in, the parent each folder stands for, and the qualified identifier the tool computes from the placement. Each step names the standard it satisfies, and the guide adds no rule of its own.

## Prerequisites

Start with:

- A consumer from [Start a Consumer Repository](../tutorial/getting-started.md).

Read the [Project Model](../core/model.md) before this guide: it states the placement rules, and this guide lists the folders they resolve to. The [command index](../tools/README.md) states how `litespec` runs in a consumer.

## Procedure

### Place Intent Records

Intent reads module, then feature, then requirement. Product-level records sit directly under `.litespec/intent/`; records under a module sit under `.litespec/intent/modules/<module>/`. A folder under `modules/` that names no declared module is a blocking finding. This satisfies `litespec/core/model#write-every-record-in-its-home` and `litespec/core/model#give-every-record-one-parent`.

| Folder | Holds | Parent |
|:---|:---|:---|
| `.litespec/intent/product.yaml` | The product | None |
| `.litespec/intent/goals/` | Goals | Product |
| `.litespec/intent/problems/` | Problems | Product |
| `.litespec/intent/constraints/` | Constraints | Product |
| `.litespec/intent/decisions/` | Decisions | Product |
| `.litespec/intent/research/` | Research | Product |
| `.litespec/intent/actors/` | Actors | Product |
| `.litespec/intent/roles/` | Roles | Product |
| `.litespec/intent/journeys/` | Journeys | Product |
| `.litespec/intent/modules/<module>/module.yaml` | The module | Product |
| `.litespec/intent/modules/<module>/goals/` | Goals | Module |
| `.litespec/intent/modules/<module>/problems/` | Problems | Module |
| `.litespec/intent/modules/<module>/constraints/` | Constraints | Module |
| `.litespec/intent/modules/<module>/decisions/` | Decisions | Module |
| `.litespec/intent/modules/<module>/research/` | Research | Module |
| `.litespec/intent/modules/<module>/features/` | Features with requirement entries | Module |

### Place Behavior Records

Behavior reads module, then aggregate, then use case. A module folder under `.litespec/behavior/modules/` carries the same name as its Intent module. Inside it, a subfolder is either a fixed type folder from the table or an aggregate holding `aggregate.yaml`. This satisfies `litespec/core/model#write-every-record-in-its-home` and `litespec/core/model#give-every-record-one-parent`.

| Folder | Holds | Parent |
|:---|:---|:---|
| `.litespec/behavior/modules/<module>/<aggregate>/aggregate.yaml` | The aggregate | Module |
| `.litespec/behavior/modules/<module>/<aggregate>/<use-case>.yaml` | Use cases owned by the aggregate | Aggregate |
| `.litespec/behavior/modules/<module>/use-cases/` | Use cases with no aggregate | Module |
| `.litespec/behavior/modules/<module>/<aggregate>/events/` | Events beside the aggregate file | Aggregate |
| `.litespec/behavior/modules/<module>/<aggregate>/rules/` | Rules of the aggregate | Aggregate |
| `.litespec/behavior/modules/<module>/rules/` | Rules of the module | Module |
| `.litespec/behavior/rules/` | Rules across modules | Product |
| `.litespec/behavior/modules/<module>/reactions/` | Reactions | Module |
| `.litespec/behavior/modules/<module>/jobs/` | Jobs | Module |
| `.litespec/behavior/modules/<module>/notifications/` | Notifications | Module |
| `.litespec/behavior/modules/<module>/settings/` | Settings | Module |
| `.litespec/behavior/modules/<module>/integration-events/` | Integration events | Module |
| `.litespec/behavior/modules/<module>/permissions.yaml` | Permission entries | Module |
| `.litespec/behavior/modules/<module>/external-systems/` | External systems | Module |
| `.litespec/behavior/external-systems/` | External systems | Product |
| `.litespec/behavior/entry-points/<entry-point>/entry-point.yaml` | The entry point | Product |
| `.litespec/behavior/entry-points/<entry-point>/screens/` | Screens | Entry point |
| `.litespec/behavior/retention-schedule.yaml` | Retention rows for every data class | Product |

Results and scenarios live as entries in their use case file: a result's parent is the use case, and a scenario's parent is its result. Events live in `aggregate.yaml` or under `events/`.

### Place Implementation Records

Implementation holds how the product is built and operated. Runbooks and operations documents live here only. This satisfies `litespec/core/model#write-every-record-in-its-home` and `litespec/core/model#give-every-record-one-parent`.

| Folder | Holds | Parent |
|:---|:---|:---|
| `.litespec/implementation/decisions/` | Technical decisions | Product |
| `.litespec/implementation/modules/<module>/decisions/` | Technical decisions | Module |
| `.litespec/implementation/runbooks/` | Runbooks | Product |
| `.litespec/implementation/modules/<module>/runbooks/` | Runbooks | Module |
| `.litespec/implementation/entry-points/<entry-point>/` | UI catalog, captures, and patterns | Entry point |
| `.litespec/implementation/api/` | The API contract | Product |
| `.litespec/implementation/operations/` | Operations documents | Product |
| `.litespec/implementation/guides/` | Guides, as documents, not records | Product |

### Place Governance Records

Governance holds exceptions and project conventions. An exception sits at product level, or under the module holding every record it sets aside. Proof holds no authored records: generated registers, gate files, and operating logs only. Terms live in the product glossary at `.litespec/glossary.yaml`. This satisfies `litespec/core/model#write-every-record-in-its-home` and `litespec/core/model#give-every-record-one-parent`.

| Folder | Holds | Parent |
|:---|:---|:---|
| `.litespec/governance/exceptions/` | Exceptions | Product |
| `.litespec/governance/modules/<module>/exceptions/` | Exceptions | Module |
| `.litespec/governance/conventions/` | Project conventions | Product |

### Read the Qualified Identifier

The identifier is short and unique in the product, and every link uses it. The qualified identifier adds the placement, is computed and never written, and the book and the tool show both. This satisfies `litespec/core/model#give-every-record-one-parent`.

| Placement | Qualified identifier |
|:---|:---|
| Product level | `<type>/<name>`, the identifier itself |
| Module level | `<module>/<type>/<name>` |
| Under an aggregate | `<module>/<aggregate>/<type>/<name>` |
| Under an entry point | `<entry-point>/<type>/<name>` |
| An entry | The qualified identifier of its file, then `/<type>/<name>` |

For example, `problem/organizers-wait-for-ticket-money` at product level keeps its identifier, `finance/problem/organizers-wait-for-ticket-money` is the same form under the finance module, and `finance/payout/use-case/request-payout` is a use case under an aggregate.

### Follow the Folder on a Move

The folder wins: an identifier whose home segment disagrees with its folder is a blocking finding. A move to another module changes the module segment of the identifier and every citation in the same change, so run `litespec rename` for the move. This satisfies `litespec/core/model#write-every-record-in-its-home`.

## Check

- `litespec model check` exits `0` from the consumer root with no finding on placement.
- `litespec model query <qualified identifier>` resolves the record at the folder this guide lists.
