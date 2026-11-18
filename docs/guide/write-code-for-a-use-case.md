# Write Code for a Use Case

Place each code element of a use case in its folder and give it the mark its record needs, following the table below, so you can add a use case without searching the stack pages for each placement rule.

A use case is one command or query with its access, results, and scenarios, and its code spreads across every backend layer.

## Prerequisites

Start with:

- The use case record with the records it names, so every element points at a record that exists.
- The [marks table](../core/model.md#marks), which owns the mark each element carries.
- The stack pages for your language, which state the written form each mark takes.

## Procedure

### Place Each Element in Its Folder

Each layer folder holds one plural aggregate folder with one folder per use case inside it: `Application/{Aggregates}/{UseCase}/` always, `WebApi/{Aggregates}/{UseCase}/` when the use case has an endpoint, and `Infrastructure/{Aggregates}/{UseCase}/` when the use case has its own port. A module-level use case drops the aggregate segment in each. Put each file of the use case in one of the three, in the folder the table names, with the mark the table names. A row with no mark is placed by its folder alone.

| Element | Goes In | Mark | Checked By |
|:---|:---|:---|:---|
| Command or query handler | The `Application/{Aggregates}/{UseCase}/` folder | `implements` naming `use-case/<module>.<name>` | See the [finding table](../tools/model.md) |
| Command or query message | The `Application/{Aggregates}/{UseCase}/` folder beside its handler | None; the handler folder places it | Review the message name beside the use case name |
| Validator | The `Application/{Aggregates}/{UseCase}/` folder beside its message | `implements` naming `use-case/<module>.<name>` | Review the validator body |
| Result type | The `Application/{Aggregates}/{UseCase}/` folder beside its handler | None; the handler folder places it | See the [finding table](../tools/model.md) |
| Aggregate root | The module `Domain/{Aggregates}/` folder | `implements` naming `aggregate/<module>.<name>` | See the [finding table](../tools/model.md) |
| Root method that changes state | On the aggregate root | `implements` naming `use-case/<module>.<name>`, exactly one | See the [finding table](../tools/model.md) |
| Domain event | The `Events/` folder of the module `Domain/{Aggregates}/` folder | `implements` naming `domain-event/<aggregate>.<fact>` | See the [finding table](../tools/model.md) |
| Refusal type | The `Exceptions/` folder of the module `Domain/{Aggregates}/` folder | `implements` naming `rule/<home>.<name>` | See the [finding table](../tools/model.md) |
| Repository interface | The module `Domain/{Aggregates}/` folder | None; the module `Domain/{Aggregates}/` folder places it | See the [finding table](../tools/model.md) |
| Repository implementation | The module `Infrastructure/Persistence/` folder | `implements` naming `aggregate/<module>.<name>`, or derived from the interface | See the [finding table](../tools/model.md) |
| Endpoint | The `WebApi/{Aggregates}/{UseCase}/` folder, or the host for a host-only endpoint | `implements` naming `use-case/<module>.<name>` | See the [finding table](../tools/model.md) |
| Request and response models | The `WebApi/{Aggregates}/{UseCase}/` folder beside the endpoint | None; the folder places them | Review the transport models |
| Reaction handler class | The `Application/{Aggregates}/{UseCase}/` folder when the reaction runs a use case; otherwise the `Application/Notifications/<Name>/` folder when it sends a notification, or `Application/ExternalSystems/<Name>/` when it calls one | `implements` naming `reaction/<module>.<name>` | See the [finding table](../tools/model.md) |
| Scheduled job | The `Application/{Aggregates}/{UseCase}/` folder of the use case the job runs | `implements` naming `job/<module>.<name>` | See the [finding table](../tools/model.md) |
| Integration event | The module `Contracts/IntegrationEvents/` folder | `implements` naming `integration-event/<name>` | See the [finding table](../tools/model.md) |
| External-system port | The `Application/{Aggregates}/{UseCase}/` folder when one use case calls it, the module `Application/Ports/` folder when several use cases of the module call it, or the owning module `Contracts/Ports/` folder when called from another module | `implements` naming `external-system/<name>` | See the [finding table](../tools/model.md) |
| Use case adapter | The `Infrastructure/{Aggregates}/{UseCase}/` folder | `implements` naming `external-system/<name>` | See the [finding table](../tools/model.md) |
| Read-model reader or projection | The `Application/ReadModels/<ReadModelPlural>/` folder | `implements` naming `read-model/<module>.<name>` | See the [finding table](../tools/model.md) |
| Test | The test suite for its boundary, mirroring the module tree | `proves` naming `scenario/<use-case>.<name>` | See the [finding table](../tools/model.md) |

The table lists no screens: whether a browser reaches a use case depends on the deployment.

Where the Checked By cell names a review, a reviewer reads the row. Every other row is checked by a finding that the [model command](../tools/model.md) reports.

Two code checks hold the marks to their records:

- A reaction handler subscribes only to the events its reaction names in `triggeredBy`. A handler subscribed to another event reports `reaction-subscription-drift`, and a declared trigger no handler answers reports `reaction-trigger-unhandled`. This satisfies `litespec/backend/dotnet/application#subscribe-each-reaction-handler-to-its-declared-triggers`.
- Each refusal code is thrown only from its own module and by one exception type, so two rules that a boundary maps to one response still keep two codes. This satisfies `litespec/backend/dotnet/domain#throw-only-refusal-codes-the-module-owns`.

### Point Each Mark at Its Record

Write every `implements` or `proves` mark with the identifier the table names. The [Project Model](../core/model.md) states the mark rules, [System](../core/system.md) requires each mark, and each stack page states the written form its language takes.

## Check

- Every element in the table sits in its folder with the mark the table names.
- `litespec model check` reports no finding for the use case and its scenarios.
