# Start a Consumer Repository

## Purpose

A LiteSpec consumer is a project built against the LiteSpec standard, and this tutorial turns an empty folder into one: its configuration, its first Intent records, the Behavior specified from them, one working use case, and a passing model check. A use case names what the product does for an actor, a person or system using it, and the model check confirms the records, code, and tests agree. This tutorial follows one backend stack and one frontend stack, the `dotnet` backend with a React frontend, so every step runs the same way for every reader. A developer follows these steps once, when creating the consumer. Each step names the standard it satisfies, and the tutorial adds no rule of its own.

## Prerequisites

- Read the [agent protocol](../core/agent.md), the [authoring standard](../core/authoring.md), and the [supported scope](../core/scope.md) to learn how agents work, how pages are written, and what one project covers.
- Read the [extension catalog](../ext/README.md) to learn which capabilities stay inactive at the start.
- Install the .NET SDK and Node.js releases the adopted release pins under `stack` in its `litespec.manifest.json`.
- Ship the `dotnet` backend with a React frontend: this tutorial follows that pair from the `dotnet-react` preset. When the product ships another frontend, follow this tutorial first, then declare that frontend with its own how-to, [Add a Svelte Frontend](../guide/add-a-svelte-frontend.md) or [Add a Blazor Frontend](../guide/add-a-blazor-frontend.md).

## Lesson

This lesson creates a new project. After the project runs, adding new intent to it follows the one sequence under [New Intent in a Running Project](../core/agent.md#new-intent-in-a-running-project) on the agent page, and the check steps below walk that same order for the first slice.

1. Install the LiteSpec release as a local .NET tool: run `dotnet new tool-manifest` when the folder has no `.config/dotnet-tools.json` yet, then `dotnet tool install LiteSpec.Cli --version 4.1.0` and `dotnet tool restore`. From here on, run every command below as `dotnet litespec <command>` from the consumer root. This satisfies `litespec/workspace/structure#use-the-canonical-root-tree`.
2. Write the project configuration: run `dotnet litespec init --preset dotnet-react --size small`, which writes `.litespec/project.json` with the shape, the stacks, and the adopted release in `reviewedLiteSpecVersion`, with the `## LiteSpec` block in `AGENTS.md`, the glossary, and the example cast beside it. Fill every placeholder. This satisfies `litespec/core/scope#select-a-size-a-shape-and-stacks`.
3. Select extensions only when their criteria already hold. Write each selection in the object form with its criterion. This satisfies `litespec/core/scope#select-conditional-extensions-explicitly` and `litespec/core/scope#record-selected-extensions`.
4. Point the Behavior docs path at the Behavior root in `paths.behaviorDocs`. This satisfies `litespec/core/scope#point-behavior-docs-at-the-behavior-root`.
5. Create the toolchain configuration from the manifest pins. Add the SDK floor, the shared build settings, the central NuGet versions, the tool manifest, and the JavaScript toolchain pin. This satisfies `litespec/backend/dotnet/config#set-the-sdk-floor-at-the-workspace-root`, `litespec/backend/dotnet/config#centralize-net-build-settings`, `litespec/backend/dotnet/config#centralize-nuget-versions`, `litespec/backend/dotnet/config#commit-the-tool-manifest`, and `litespec/workspace/config#pin-the-javascript-toolchain`.
6. Add only the packages the manifest already pins. Record each new package approval explicitly. This satisfies `litespec/workspace/dependencies#pin-every-dependency-version-centrally` and `litespec/workspace/dependencies#approve-new-packages-explicitly`.
7. Create the API solution. Add the shared kernel and modules projects, the WebApi host, the AppHost, the ServiceDefaults, and the Integration test project. Add the unit test project when unit tests exist, the Architecture test project for what the analyzers do not check, and the Acceptance project for executable specifications at large size. Add the Worker host only when background work needs its own process. Reference `LiteSpec.Analyzers` from the business projects and the hosts, or once in `Directory.Build.props`:
   ```xml
   <ItemGroup>
     <PackageReference Include="LiteSpec.Analyzers" Version="4.1.0" PrivateAssets="all" />
   </ItemGroup>
   ```
   This satisfies `litespec/backend/dotnet/architecture#separate-the-shared-kernel-from-the-modules`, `litespec/backend/testing#keep-the-test-suites-the-size-requires`, and `litespec/backend/dotnet/packages#run-litespec-analyzers-in-the-business-projects-and-hosts`.
8. Apply the naming rules: match each C# file to its primary type and each namespace to its folder. This satisfies `litespec/backend/dotnet/naming#match-c-files-and-primary-types` and `litespec/workspace/naming#keep-namespaces-aligned-with-folders`.
9. Write the product brief. Copy `product-brief.yaml` from the template index to `.litespec/intent/product.yaml`. State the boundary, the addresses, and the summary. This satisfies `litespec/core/intent#state-one-product` and `litespec/core/intent#keep-intent-in-its-homes`.
10. Write one module with its first feature. Copy `module.yaml` from the template index to `.litespec/intent/modules/<module>/module.yaml`, then write the feature with its requirements as entries by following [Write a Feature Under a Module](../guide/write-a-feature-under-a-module.md). This satisfies `litespec/core/intent#keep-intent-in-its-homes` and `litespec/core/intent#identify-every-requirement`.
11. State which constraints apply. Write one constraint record file per outside rule, with its authority, kind, and summary, or record that none applies. This satisfies `litespec/core/intent#answer-every-constraint` and `litespec/core/scope#answer-the-constraints-question`.
12. Record the first decision. Copy `decision.yaml` from the template index to `.litespec/intent/decisions/<number>-<name>.yaml`, with `decision.md` beside it for the explanation. Name the options with what speaks for and against each, the choice, the consequences, and what would change the choice, and write the Context in the explanation file. This satisfies `litespec/core/intent#shape-each-decision-page`.
13. Write the people records. Add one actor record file per party, one role record file per grantable set, and the permission entries on the permissions record file. Each use case later declares its access against one of those permissions. The `actor.yaml`, `role.yaml`, and `permission-list.yaml` templates define the fields. When the slice exchanges anything with an outside system, add an external system record file for it with its summary, operations, and erasure.
14. Write one journey. Copy `journey.yaml` from the template index to `.litespec/intent/journeys/<name>.yaml`. State the goal record it serves in `serves`, the observable `finishedWhen`, the steps as product sentences with ids, each with its actor and what the person experiences when it goes wrong, and what passes between the steps in `betweenSteps`. Steps name no use case; use cases point up to them later with `serves`. This satisfies `litespec/core/intent#order-each-journey-toward-one-goal`, `litespec/core/intent#write-journey-steps-as-sentences-with-ids`, and `litespec/core/intent#state-when-each-journey-finishes`.
15. Check the Intent records. Run `dotnet litespec model check` from the consumer root and fix every finding before continuing. The run scans the records written so far alone, before any Behavior record or code exists, and a clean run prints the output below.

   ```text
   0 findings, 0 blocking, 0 known.
   ```

16. Specify the module from Intent. Follow [Model a Behavior Module](model-domain.md) for the module, the aggregates with their data classes, the rules, the use cases with their access, consistency, and effects, the scenarios, the events, the reactions, jobs, notifications, and settings, and the retention rows. Then run `dotnet litespec model check` from the consumer root and fix every finding before code moves. This satisfies `litespec/core/system#group-module-use-case-files-by-aggregate` and `litespec/core/model#author-one-slice-at-a-time`.
17. Implement one use case. Move the transition into a business method, keep the handler narrow, commit once, expose one endpoint, and authorize the target. Code points up to its record through its mark. This satisfies `litespec/backend/domain#express-transitions-through-business-methods`, `litespec/backend/application#keep-command-handlers-to-load-call-and-stage`, `litespec/backend/dotnet/persistence#commit-once-in-the-command-pipeline`, `litespec/backend/dotnet/api#use-one-endpoint-per-operation`, and `litespec/quality/security#authorize-each-target-resource`.
18. Declare the React frontend. Declare the application tree and mirror the backend use case path. This satisfies `litespec/frontend/web/structure#use-the-frontend-application-tree`. Build each pattern Default and record it with `source: recommended`, waiting for an owner only when a route departs from the Default or no Default exists, and compose each route from one floorplan. This satisfies `litespec/frontend/ui#compose-each-route-from-one-floorplan`.
19. Deliver one complete slice. Ship source, tests, contracts, and operating evidence for one use case. This satisfies `litespec/core/system#deliver-one-complete-use-case`.
20. Add the operating records. Write the settings and runbooks, add deployment, backup, restore, and rollback, and configure the pipeline gates. This satisfies `litespec/quality/operations#start-the-stack-with-one-local-command` and `litespec/quality/ci#run-applicable-gates-on-every-pull-request`.
21. Run the boundary gates and the test suites. Run the gates and suites the changed areas select, so the latest test run stands before the model reads it.
22. Check the model. Run `dotnet litespec model check`. It builds the model in memory from records, code, screens, and tests, prints a level beside each finding, and exits 1 on any unresolved citation, missing link, undeclared edge, or unproved scenario. Fix every finding before continuing.
23. Render the generated regions: run `dotnet litespec model render`, then commit what it wrote. This satisfies `litespec/core/release#run-consistency-checks`.
24. Read the book. Run `dotnet litespec book` and open the journey detail, the way product reviewers read the change.

## What You Built

- A consumer repository at small size that the model check accepts.
- One implemented use case with Intent, Behavior, code, tests, and generated contracts.
- When the product ships another frontend, [Add a Svelte Frontend](../guide/add-a-svelte-frontend.md) and [Add a Blazor Frontend](../guide/add-a-blazor-frontend.md) declare it. The checks are documented on [Model Builder](../tools/model.md); all commands are listed on the [command index](../tools/README.md).
