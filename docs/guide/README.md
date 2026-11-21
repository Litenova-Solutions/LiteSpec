# Overview

The guides teach by doing. Tutorials take a reader to a first working result one step at a time, and how-to guides solve one stated problem in a project that already runs.

Every guide cites the standard it applies and adds no rule of its own, so a reader learns the standard by following along.

## Where to Start

If LiteSpec is new to you, work in this order. Install the command line first with [Install the CLI](install-the-cli.md), because every later guide runs it. Then work through [Start a Consumer Repository](../tutorial/getting-started.md), which builds a working consumer with one coded use case on the `dotnet` backend and a React frontend. Then work through [Model a Behavior Module](../tutorial/model-domain.md), which specifies one business area from Intent into Behavior that the model builder reads.

A team that wants records, the book, and the checks before changing any code follows [Start with Records Only](start-with-records-only.md) instead. That guide claims the Intent and Behavior records alone.

When the product ships another frontend, follow the guide that names it: [Add a Svelte Frontend](add-a-svelte-frontend.md), [Add a Blazor Frontend](add-a-blazor-frontend.md), or [Add an Android Frontend](add-an-android-frontend.md).

When the project runs and one goal needs solving, open the how-to that names it in How-To Guides below: writing code, problems, features, constraints, flows, and research records, the audit trail, and the per-edit rules check. [Check .NET Code with the Analyzers](../analyzers/check-dotnet-code-with-the-analyzers.md) runs the Roslyn analyzers over a .NET backend.

To find where a record file lives, look it up in [Record Folders](../reference/folders.md), which the release generates from the same source the checks read.

## Tutorials

- [Start a Consumer Repository](../tutorial/getting-started.md): start a small consumer repository through one coded use case.
- [Model a Behavior Module](../tutorial/model-domain.md): specify one spending module the builder reads.

## How-To Guides

- [Install the CLI](install-the-cli.md): install the command line for a consumer repository or a machine, by every method a release ships.
- [Start with Records Only](start-with-records-only.md): claim the Intent and Behavior records alone, and check them with the model check and the book.
- [Add a Svelte Frontend](add-a-svelte-frontend.md): declare a Svelte frontend on the web stack.
- [Add a Blazor Frontend](add-a-blazor-frontend.md): declare a Blazor frontend outside the controlled UI contract.
- [Add an Android Frontend](add-an-android-frontend.md): declare a native Android app built with .NET for Android.
- [Write Code for a Use Case](write-code-for-a-use-case.md): place each code element of a use case with its mark and its check.
- [Write a Problem](write-a-problem.md): write one problem with its failure cases, evidence, and resolution criteria, step by step with weak and strong examples.
- [Write a Feature](write-a-feature.md): write one feature with its requirements and the problem it answers, with testable statements and the feature boundaries.
- [Write a Constraint](write-a-constraint.md): write one outside rule with its sources, its scope, its rules, and the requirements that hold its effect.
- [Write a Flow](write-a-flow.md): write one actor path toward one goal or one problem, from its start through its steps, blockers, and branches.
- [Write a Research Record](write-a-research-record.md): write one question's findings, each tied to its source and marked as stated or inferred.
- [Build an Audit Trail](audit-trail.md): implement the audit extension on the .NET LiteBus pipeline, step by step.
- [Run Rules After Every Edit](run-rules-after-every-edit.md): run `rules` on each file after it is edited, and check the change set before the agent stops.
- [Add a Stack or an Extension](add-a-stack-or-extension.md): add a stack folder or an extension page to the release manifest.
