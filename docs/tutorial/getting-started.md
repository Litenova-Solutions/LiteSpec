# Start a Consumer Repository

Turn an empty folder into a running consumer with the Health sample: start from the runnable starter, build and boot it, check the records and the generated files, run the scenario probes, and add the next use case with the scaffold command. A consumer is a project built against the LiteSpec standard. Follow these steps once, when you create the consumer.

A use case is what the product does for one actor, a person or system using it. The model check is the tool that confirms the records, code, and tests agree. Every step runs on the same stack pair, the `dotnet` backend with a Svelte frontend, so every reader sees the same result.

Each step names the standard it satisfies or the command page that states it; the tutorial adds no rule of its own.

## Prerequisites

- Install the .NET SDK and Node.js releases the adopted release pins under `stack` in its `litespec.manifest.yaml`.
- Install the LiteSpec CLI for the adopted release as the [CLI overview](../tools/README.md) shows, then check that `dotnet litespec --help` prints the command list from the consumer root. This satisfies [Workspace Structure](../workspace/structure.md).
- Read nothing in advance. Each step links the standard it satisfies, so open each linked page when you reach its step.
- This tutorial uses the `dotnet` backend with a Svelte frontend, the pair from the `dotnet-svelte` preset, so every reader sees the same result. When the product ships another frontend, finish this tutorial first, then declare that frontend with its own how-to, [Add a Svelte Frontend](../guide/add-a-svelte-frontend.md) or [Add an Android Frontend](../guide/add-an-android-frontend.md).
- When the project starts with records only and no stack, follow [Start with Records Only](../guide/start-with-records-only.md) instead of this tutorial.

## Lesson

The starter arrives complete: records, code, tests, and probes with no placeholder left to fill. The records you add later arrive with placeholders; replace every one of them with the project's own before committing.

1. Write the starter with `litespec init --preset dotnet-svelte --size small --name "Team Expenses" --bootstrap` in an empty folder, then confirm `.litespec/project.yaml` exists and the API solution sits at `apps/api/TeamExpenses.slnx`. The command fills the project name, the namespace, and the package scope, as the [initialize command](../tools/init.md) states. This satisfies `litespec/core/scope#select-a-size-a-shape-and-stacks`.
2. Select extensions only when their criteria already hold, and write each selection in the object form with its criterion. This satisfies `litespec/core/scope#select-conditional-extensions-explicitly` and `litespec/core/scope#record-selected-extensions`.
3. Build the API with `dotnet build apps/api/TeamExpenses.slnx` from the consumer root, and confirm the build ends with 0 warnings and 0 errors.
4. Boot the local stack from the AppHost project in `deploy/`, which starts PostgreSQL, the API on port 5005, and the web on port 5173. Confirm the API readiness probe answers at `http://localhost:5005/health/ready`, the OpenAPI document serves at `http://localhost:5005/openapi/v1.json`, and the `/check-ins` route lists check-ins in the browser. This satisfies `litespec/quality/development#start-the-stack-with-one-local-command`.
5. Run `dotnet litespec model check` from the consumer root, and fix every finding before continuing. The [model command](../tools/model.md) lists the findings and their levels.
6. Run `dotnet litespec sync --check` from the consumer root, and confirm it reports no file would change. The starter ships its generated files current; when a later record change would rewrite one, run `dotnet litespec sync` to write it. The [Sync](../tools/sync.md) page states what the command writes.
7. Run `dotnet litespec verify` from the consumer root against the booted API, and confirm every probe passes. The command signs each cast person in through the development sign-in route and checks each scenario probe status, refusal, and field, as the [Verify](../tools/verify.md) page states.
8. Add the next use case. Specify one more Health use case end to end with [Model a Behavior Module](model-domain.md), then run `dotnet litespec scaffold use-case/health.<name>` with its identifier. The command writes the message, the handler, the validator, the result, the authorization, audit, tenancy, and idempotency definitions, and the endpoint with its transport models, and it never overwrites an existing file, as the [Scaffold](../tools/scaffold.md) page states. Implement the aggregate act the handler calls, rebuild the solution, and run `dotnet litespec model check` again. This satisfies [LiteSpec](../core/system.md) and `litespec/core/layers#write-each-layer-from-the-layer-above-it`.
9. Run `dotnet litespec book`, open the served book at `http://localhost:7420/`, and read the flow detail, the way product reviewers read the change.

## What You Built

- A consumer repository at small size from the runnable starter that the model check accepts.
- A built and booted stack: the API builds with 0 warnings and 0 errors, the generated files are current, and every scenario probe passes.
- One more use case specified from Intent to Behavior, scaffolded into code, and checked.
- When you specify the use case after that, follow [Write Code for a Use Case](../guide/write-code-for-a-use-case.md) to place each of its code elements with its mark, then check.
- When the product ships another frontend, [Add a Svelte Frontend](../guide/add-a-svelte-frontend.md) and [Add an Android Frontend](../guide/add-an-android-frontend.md) declare it. The checks are documented on [Model Builder](../tools/model.md), and all commands are listed on the [command index](../tools/README.md).
