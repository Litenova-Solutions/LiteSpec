# .NET Golden Slice

**Stack:** dotnet

**Concern:** application

A new .NET backend starts from one small working slice building, booting, and passing its checks in under two minutes. The slice holds one aggregate with one command and one query, written as explicit per-use-case files, with storage and one executable probe scenario. A team copies this shape for each new use case instead of inventing a second layout.

The slice uses `litespec scaffold` for normal code files and `litespec sync` for pure generated data, with no partial twin files. The [architecture page](architecture.md) states the module layout, and the [persistence page](persistence.md) states how Marten stores each aggregate.

## Concepts

### What the Slice Contains

The slice holds a minimal health aggregate with one command recording a check-in and one query reading the latest entry. It includes the aggregate root with its state record, the two use-case folders with their messages and handlers, the endpoint models, the serializer setup, and one probe scenario proving the round trip through the public boundary.

### How Files Are Organized

Each use case owns its folder under its aggregate, and each file in the folder holds one thing. The command folder holds the command, the handler, the validator, the result, and the authorization, audit, and idempotency definitions. The query folder holds the query, the handler, and the result. No file is shared between the two use cases, so a change to one does not touch the other.

```text
Modules/Health/HealthModule.cs
Modules/Health/Domain/Checks/HealthCheck.cs
Modules/Health/Domain/Checks/States/HealthCheckState.cs
Modules/Health/Application/Checks/RecordCheckIn/RecordCheckInCommand.cs
Modules/Health/Application/Checks/RecordCheckIn/RecordCheckInCommandHandler.cs
Modules/Health/Application/Checks/RecordCheckIn/RecordCheckInCommandValidator.cs
Modules/Health/Application/Checks/RecordCheckIn/RecordCheckInResult.cs
Modules/Health/Application/Checks/ReadCheckIn/ReadCheckInQuery.cs
Modules/Health/Application/Checks/ReadCheckIn/ReadCheckInQueryHandler.cs
Modules/Health/Application/Checks/ReadCheckIn/ReadCheckInResult.cs
Modules/Health/WebApi/Checks/RecordCheckIn/RecordCheckInEndpoint.cs
Modules/Health/WebApi/Checks/ReadCheckIn/ReadCheckInEndpoint.cs
```

Explicit files keep one use case readable in one folder: the message, the coordination, the validation, and the result each sit where a reader looks for them. A partial twin splitting one use case across hand-written and generated halves drifts apart, so a reader reconciles two sources for one behavior.

### How Storage and Proof Work

Marten stores the health aggregate as its own document, with the store serializer carrying the converters for money, typed identifiers, and closed-set discriminators. The slice records one frozen JSON snapshot of the aggregate at release time, and the build deserializes that snapshot through the real serializer. The frozen snapshot proves later field changes still read old rows: without it, a rename or a new value type breaks stored history and the failure appears only in production.

The probe scenario records a check-in through the endpoint and reads it back through the query, so the slice proves the write path, the read path, and the stored form together. A slice without a probe compiles but proves nothing about the wiring between the endpoint, the handler, the store, and the read path. One probe through the public boundary shows a fresh checkout builds, boots, and serves the slice, which is the result a new consumer checks first.

### Related Reading

The [architecture page](architecture.md) states the Kernel, the Platform, and the module folders the slice follows. The [persistence page](persistence.md) states the repository, the single commit, and the serializer the slice uses.

## Standards

None.

## Conventions

None.
