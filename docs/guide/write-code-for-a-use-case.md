# Write Code for a Use Case

## Purpose

A use case is one command or query with its access, results, and scenarios, and its code spreads across every backend layer. This guide places each code element of a use case in its folder and gives it the mark its record needs, so a writer adds a use case without hunting six pages.

## Prerequisites

Start with:

- The use case record and the records it names, so each element has a record to point at.
- The [marks table](../core/model.md#marks), which owns which mark each element carries.
- The stack pages for the written form each mark takes in the project's language.

## Procedure

### Place Each Element

Create each element in the folder the table names and carry the mark the table names. A row with no mark is placed by its folder. A row whose check names a review is read by a reviewer; every other check is a finding the [model command](../tools/model.md) reports.

| Element | Goes In | Mark | Checked By |
|:---|:---|:---|:---|
| Command or query handler | The use case folder | `implements` naming `use-case/<module>.<name>` | See the [finding table](../tools/model.md) |
| Command or query message | The use case folder beside its handler | None; the handler folder places it | Review the message name beside the use case name |
| Validator | The use case folder beside its message | `implements` naming `use-case/<module>.<name>` | Review the validator body |
| Result type | The use case folder beside its handler | None; the handler folder places it | See the [finding table](../tools/model.md) |
| Aggregate root | The aggregate `Domain/` folder | `implements` naming `aggregate/<module>.<name>` | See the [finding table](../tools/model.md) |
| Root method that changes state | On the aggregate root | `implements` naming `use-case/<module>.<name>`, exactly one | See the [finding table](../tools/model.md) |
| Domain event | The `Events/` folder of its aggregate `Domain/` folder | `implements` naming `event/<aggregate>.<fact>` | See the [finding table](../tools/model.md) |
| Refusal type | The `Refusals/` folder of its aggregate `Domain/` folder | `implements` naming `rule/<home>.<name>` | See the [finding table](../tools/model.md) |
| Repository interface | The aggregate `Domain/` folder | None; the aggregate folder places it | See the [finding table](../tools/model.md) |
| Repository implementation | The aggregate `Infrastructure/` folder | `implements` naming `aggregate/<module>.<name>`, or derived from the interface | See the [finding table](../tools/model.md) |
| Endpoint | The use case folder, or the host for a host-only endpoint | `implements` naming `use-case/<module>.<name>` | See the [finding table](../tools/model.md) |
| Request and response models | The use case folder beside the endpoint, or the node `WebApi/` folder when shared | None; the endpoint folder places them | Review the transport models |
| Event handler class | The `UseCases/On{Fact}` folder inside the module `UseCases/` folder | `implements` naming `reaction/<module>.<name>` | See the [finding table](../tools/model.md) |
| Scheduled or manual host | The module `Jobs/` folder, hosted in Infrastructure or Worker | `implements` naming `job/<module>.<name>` | See the [finding table](../tools/model.md) |
| External-system port | The node `Application/` folder beside the aggregate it serves, or the owning module `Contracts/Ports/` folder when called from another module | `implements` naming `external-system/<name>` | See the [finding table](../tools/model.md) |
| Test | The test suite for its boundary, mirroring the module tree | `proves` naming `scenario/<use-case>.<name>` | See the [finding table](../tools/model.md) |

Screens stay outside this table: whether a browser reaches a use case depends on the deployment.

### Point Each Mark at Its Record

Write every `implements` or `proves` mark with the identifier the table names. The [Project Model](../core/model.md) states the mark rules, and [System](../core/system.md) requires each mark. Each stack page states the written form its language takes.

## Check

- Every element in the table sits in its folder with the mark the table names.
- `litespec model check` reports no finding for the use case and its scenarios.
