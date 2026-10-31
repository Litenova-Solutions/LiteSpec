# Write Code for a Use Case

## Intent

A use case is one command or query with its access, results, and scenarios, and its code spreads across every backend layer. This page maps each code element of a use case to its folder, its mark, and its check, so an author places new code without hunting six pages. The [marks table](../core/model.md#marks) owns which mark each element carries; the stack pages state each mark's written form.

## Concepts

### The Element Table

Each row names one code element, the folder that holds it, the mark it carries, and the check that watches it. A check cell naming codes names the findings the tools report; a check cell naming review names the code a reviewer reads. This page adds no rule of its own.

| Element | Goes In | Mark | Checked By |
|:---|:---|:---|:---|
| Command or query handler | The use-case folder in Application | `implements` naming `use-case/<module>.<name>` | `use-case-act-missing`, `write-undeclared`, `effect-undeclared` |
| Command or query message | The use-case folder beside its handler | None; the handler folder places it | `name-drift` |
| Validator | The use-case folder beside its message | `implements` naming `use-case/<module>.<name>` | Review the validator body |
| Result type | The use-case folder beside its handler | None; the handler folder places it | `result-undeclared` |
| Aggregate root | The aggregate folder in Domain | `implements` naming `aggregate/<module>.<name>` | `aggregate-unimplemented` |
| Root method that changes state | On the aggregate root | `implements` naming `use-case/<module>.<name>`, exactly one | `use-case-act-missing`, `mark-count-invalid`, `act-event-undeclared`, `act-event-unimplemented` |
| Domain event | The `Events` folder of its aggregate | `implements` naming `event/<aggregate>.<fact>` | `act-event-undeclared`, `act-event-unimplemented`, `LS1013` |
| Refusal type | The refusals of its aggregate | `implements` naming `rule/<home>.<name>` | `refusal-unimplemented` |
| Repository interface | The aggregate folder in Domain | None; the aggregate folder places it | `constituent-parent-missing` |
| Repository implementation | The persistence folder in Infrastructure | `implements` naming `aggregate/<module>.<name>`, or derived from the interface | `repository-unimplemented` |
| Endpoint | The use-case folder under `Endpoints` in WebApi | `implements` naming `use-case/<module>.<name>` | `use-case-endpoint-missing`, `operation-use-case-drift` |
| Request and response models | The endpoint folder beside the endpoint | None; the endpoint folder places them | Review the transport models |
| Event handler class | The `On{Fact}` folder in Application | `implements` naming `automation/<module>.<name>` | `automation-unimplemented`, `effect-undeclared` |
| Scheduled or manual host | Infrastructure, or Worker when the project has one | `implements` naming `automation/<module>.<name>` | `automation-unimplemented` |
| External-system port | Application beside the aggregate it serves | `implements` naming `external-system/<name>` | `external-system-unimplemented` |
| Test | The test project for its boundary | `proves` naming `scenario/<use-case>.<name>` | `proves-unreached`, `use-case-scenario-missing`, `use-case-unverified` |

Screens stay outside this table: whether a browser reaches a use case depends on the deployment. The [Project Model](../core/model.md) states the mark rules, and [System](../core/system.md) requires each mark; each stack page states the written form its language takes.

## Standards

None.

## Conventions

None.
