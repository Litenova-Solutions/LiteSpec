# Place Each Trigger with What It Runs

A .NET backend keeps each use case in folders of its own, one per layer that serves it. Backend developers open the use case's `Application/` and `WebApi/` folders to find its endpoint, message, handler, and every trigger that starts it. This decision places each reaction and job with the use case it runs. It stops any use case from naming another use case's types.

## Context

In 6.0.0 a use-case folder held its files flat, with Application and WebApi files side by side. Event handlers sat in a module `UseCases/On{Fact}/` folder named for the fact they answered, and jobs sat in a module `Jobs/` folder.

A handler in a fact folder named the command of a use case in another folder. LS2019 allowed that by exempting messages and results, so those commands and results moved to aggregate and module folders where every caller could reach them. One large consumer counted about 270 such files and 232 fact folders. A fact's reactions still spread across modules.

## Options

| Option | Gains | Costs |
|:---|:---|:---|
| Each use case owns its layer folders, its models and its triggers | One folder shows a use case whole, and no use case names another's types. | Every consumer moves its handlers and jobs, and two use cases with one shape hold a copy each. |
| Keep fact folders and shared message folders | No file moves. | Commands leave their folders, and fact folders still scatter. |
| Group by message kind inside each module | One folder per message kind. | Folders name mechanisms, and one use case spreads over four folders. |

## Decision

`Application/` and `WebApi/` each hold one plural aggregate folder with one folder per use case inside it: `Application/{Aggregates}/{UseCase}/` always, and `WebApi/{Aggregates}/{UseCase}/` when the use case has an endpoint. A module-level read drops the aggregate segment in each. A trigger lives with what it runs. A reaction or job that runs a use case sits in that use case's `Application/` folder. One that only sends sits in the `Application/Notifications/` folder named for its notification. One that only calls sits in the `Application/ExternalSystems/` folder named for its system.

No use case names a type in another use-case folder. A message, result, result part, or transport model belongs to one use case, and two use cases that need the same shape each declare it. A rule still has one home in Domain, because two copies of a rule disagree.

## Consequences

The module closed set loses `Jobs/`, `WebApi/`, and the `UseCases/` folders with their layer folders; `Application/` and `WebApi/` each hold one plural aggregate folder with one folder per use case inside it plus one folder per module-level read, and `Application/` gains `Notifications/`, `ExternalSystems/`, and `ReadModels/`. `Contracts/` keeps `Identifiers/`, `IntegrationEvents/`, and `Ports/`. The aggregate loses `WebApi/`.

`code-placement-drift` reads a trigger's folder from its record's effects. LS2019 drops its exemption.

The provision `litespec/workspace/naming#place-each-trigger-with-what-it-runs` states the new placement, and its identifier names the placement it states.

## Evidence

[research/cross-module-coupling-practice](research/cross-module-coupling-practice) holds the outside practice on slice coupling and the consumer counts.

## Revisit Conditions

A consumer shows a trigger that runs no use case, sends no notification, and calls no system. Or a consumer shows two use cases that must share a shape because separate copies would let them disagree on a rule.
