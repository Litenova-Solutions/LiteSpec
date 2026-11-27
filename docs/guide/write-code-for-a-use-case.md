# Write Code for a Use Case

Place each code element of a use case in its folder and give it the mark its record needs, following the table below, so you can add a use case without searching the stack pages for each placement rule.

A use case is one command or query with its access, results, and scenarios, and its code spreads across every backend layer. For example, the Claimwick submit-expense use case spreads its command, handler, validator, result, endpoint, and tests across `Application/`, `WebApi/`, and `Domain/`, and each element points back at the same use-case record.

## Prerequisites

Start with:

- The use case record with the records it names, so every element points at a record that exists.
- The [marks table](../core/model.md#marks), which owns the mark each element carries.
- The stack pages for your language, which state the written form each mark takes.

## Procedure

1. Scaffold the files. Run `litespec scaffold <use-case-id>` from the consumer root with the identifier of the use case record, such as `litespec scaffold use-case/spending.submit-expense`. The command writes the message, the handler, the validator, the result, the authorization, audit, tenancy, and idempotency definitions, and the endpoint with its request and response models, and it never overwrites an existing file. The [Scaffold](../tools/scaffold.md) page states what the command writes.
2. Place each element in its folder. `Application/` and `WebApi/` each hold one plural aggregate folder with one folder per use case inside it: `Application/{Aggregates}/{UseCase}/` always, and `WebApi/{Aggregates}/{UseCase}/` when the use case has an endpoint. A module-level read drops the aggregate segment in each. Every port of the module lives in its own `Ports/{Port}/` folder with its adapter in `Infrastructure/{Port}/`. Put each file of the use case in the folder the table names, with the mark the table names. A row with no mark is placed by its folder alone. For example, the submit handler, its command message, its validator, and its result all sit in `Application/Expenses/SubmitExpense/`, while the endpoint with its request and response models sits in `WebApi/Expenses/SubmitExpense/`.

   Place the handler and its message, validator, and result together:

   | Element | Goes In | Mark | Checked By |
   |:---|:---|:---|:---|
   | Command or query handler | The `Application/{Aggregates}/{UseCase}/` folder | `implements` naming `use-case/<module>.<name>` | See the [finding table](../tools/model.md) |
   | Command or query message | The `Application/{Aggregates}/{UseCase}/` folder beside its handler | None; the handler folder places it | Review the message name beside the use case name |
   | Validator | The `Application/{Aggregates}/{UseCase}/` folder beside its message | `implements` naming `use-case/<module>.<name>` | Review the validator body |
   | Result type | The `Application/{Aggregates}/{UseCase}/` folder beside its handler | None; the handler folder places it | See the [finding table](../tools/model.md) |

   Place the aggregate, its state-changing method, and its events beside it:

   | Element | Goes In | Mark | Checked By |
   |:---|:---|:---|:---|
   | Aggregate root | The module `Domain/{Aggregates}/` folder | `implements` naming `aggregate/<module>.<name>` | See the [finding table](../tools/model.md) |
   | Root method that changes state | On the aggregate root | `implements` naming `use-case/<module>.<name>`, exactly one | See the [finding table](../tools/model.md) |
   | Domain event | The `Events/` folder of the module `Domain/{Aggregates}/` folder | `implements` naming `domain-event/<aggregate>.<fact>` | See the [finding table](../tools/model.md) |

   Place the refusal, the repository, and its store beside the aggregate:

   | Element | Goes In | Mark | Checked By |
   |:---|:---|:---|:---|
   | Refusal type | The `Exceptions/` folder of the module `Domain/{Aggregates}/` folder | `implements` naming `rule/<home>.<name>` | See the [finding table](../tools/model.md) |
   | Repository interface | The module `Domain/{Aggregates}/` folder | None; the module `Domain/{Aggregates}/` folder places it | See the [finding table](../tools/model.md) |
   | Repository implementation | The module `Infrastructure/Persistence/{Aggregates}/` folder | `implements` naming `aggregate/<module>.<name>`, or derived from the interface | See the [finding table](../tools/model.md) |

   Place the endpoint and the contracts the module publishes:

   | Element | Goes In | Mark | Checked By |
   |:---|:---|:---|:---|
   | Endpoint | The `WebApi/{Aggregates}/{UseCase}/` folder, or the host for a host-only endpoint | `implements` naming `use-case/<module>.<name>` | See the [finding table](../tools/model.md) |
   | Request and response models | The `WebApi/{Aggregates}/{UseCase}/` folder beside the endpoint | None; the folder places them | Review the transport models |
   | Integration event | The `Contracts/{Aggregates}/On{Fact}/` folder of the owning module's Contracts project | `implements` naming `integration-event/<name>` | See the [finding table](../tools/model.md) |
   | Module event | The `Contracts/{Aggregates}/On{Fact}/` folder of the owning module's Contracts project | `implements` naming `module-event/<module>.<name>` | See the [finding table](../tools/model.md) |

   Place the reactions and jobs with the work they run:

   | Element | Goes In | Mark | Checked By |
   |:---|:---|:---|:---|
   | Reaction handler class | The `Application/{Aggregates}/{UseCase}/` folder when the reaction runs a use case; otherwise the `Application/Notifications/<Name>/` folder when it sends a notification, or `Application/ExternalSystems/<Name>/` when it calls one | `implements` naming `reaction/<module>.<name>` | See the [finding table](../tools/model.md) |
   | Scheduled job | The `Application/{Aggregates}/{UseCase}/` folder of the use case the job runs | `implements` naming `job/<module>.<name>` | See the [finding table](../tools/model.md) |

   Place the published contract messages and the ports with their adapters:

   | Element | Goes In | Mark | Checked By |
   |:---|:---|:---|:---|
   | Public query message and result | The `Contracts/{Aggregates}/{UseCase}/` folder of the owning module's Contracts project | None; the handler folder places them | See the [finding table](../tools/model.md) |
   | Public command message and result | The `Contracts/{Aggregates}/{UseCase}/` folder of the owning module's Contracts project | None; the handler folder places them | See the [finding table](../tools/model.md) |
   | External-system port | The module `Ports/{Port}/` folder, spelling the record name in PascalCase | `implements` naming `external-system/<name>` | See the [finding table](../tools/model.md) |
   | Port adapter | The module `Infrastructure/{Port}/` folder | `implements` naming `external-system/<name>` | See the [finding table](../tools/model.md) |

   Place the sent cross-module messages, the read model, and the test:

   | Element | Goes In | Mark | Checked By |
   |:---|:---|:---|:---|
   | Public query sent to another module | The handler, reaction, or job that sends it | None; the `queries` edge on the sending record places it | See the [finding table](../tools/model.md) |
   | Public command sent to another module | The reaction or job that sends it | None; the `commands` edge on the sending record places it | See the [finding table](../tools/model.md) |
   | Read-model reader or projection | The `Application/ReadModels/<ReadModelPlural>/` folder | `implements` naming `read-model/<module>.<name>` | See the [finding table](../tools/model.md) |
   | Test | The test suite for its boundary, mirroring the module tree | `proves` naming `scenario/<use-case>.<name>` | See the [finding table](../tools/model.md) |

   The table lists no screens: whether a browser reaches a use case depends on the deployment.

   Where the Checked By cell names a review, a reviewer reads the row. Every other row is checked by a finding that the [model command](../tools/model.md) reports.

   Two code checks hold the marks to their records. A reaction handler subscribes only to the events its reaction names in `triggeredBy`. A handler subscribed to another event reports `reaction-subscription-drift`, and a declared trigger no handler answers reports `reaction-trigger-unhandled`. For example, the Claimwick reminder reaction answers only its own expense event and no other. This satisfies `litespec/packs/dotnet/application#subscribe-each-reaction-handler-to-its-declared-triggers`. Each refusal code is thrown only from its own module and by one exception type, so two rules that a boundary maps to one response still keep two codes. For example, the receipt rule of the spending module throws its own refusal from the spending module alone. This satisfies `litespec/packs/dotnet/domain#throw-only-refusal-codes-the-module-owns`.
3. Point each mark at its record. Write every `implements` or `proves` mark with the identifier the table names. The [Project Model](../core/model.md) states the mark rules, [System](../core/system.md) requires each mark, and each stack page states the written form its language takes. For example, the submit handler carries `<implements>use-case/spending.submit-expense</implements>`, and its scenario test carries `<proves>scenario/submit-expense.receipt-required</proves>`.

## Check

- Every element in the table sits in its folder with the mark the table names.
- `litespec model check` reports no finding for the use case and its scenarios.
- `litespec sync --check` reports no file would change, so the refusal codes, permissions, event decisions, and closed sets the records determine match what the repository holds.
- When a scenario of the use case carries a probe, `litespec verify --use-case <use-case-id>` passes against the running product, as the [Verify](../tools/verify.md) page states.
