# Overview

## Intent

The Guides section teaches by doing: tutorials take a reader to a first working result one step at a time, and how-to guides solve one stated problem in a project that already runs. Every guide cites the standard it applies and adds no rule of its own, so a reader learns the standard by following along.

## Where to Start

When LiteSpec is new, install the command line first with [Install the CLI](install-the-cli.md): every later guide runs it. Then follow [Start a Consumer Repository](../tutorial/getting-started.md): it builds a working consumer with one coded use case on the `dotnet` backend and a React frontend. Then follow [Model a Behavior Module](../tutorial/model-domain.md) to specify one business area from Intent into Behavior the model builder reads. A team that wants records, the book and the checks before it changes any code takes the alternative path instead: [Start with Records Only](start-with-records-only.md) claims the Intent and Behavior records alone. When the product ships another frontend, follow [Add a Svelte Frontend](add-a-svelte-frontend.md), [Add a Blazor Frontend](add-a-blazor-frontend.md), or [Add an Android Frontend](add-an-android-frontend.md). When the project runs and one goal needs solving, read the how-to that names it: [Write Code for a Use Case](write-code-for-a-use-case.md) places each code element of a use case with its mark and its check, [Write a Problem](write-a-problem.md) writes one problem with its failure cases, evidence and resolution criteria, [Write a Feature Under a Module](write-a-feature-under-a-problem.md) writes one feature with its requirements under its module, [Build an Audit Trail](audit-trail.md) implements the audit extension on the .NET command pipeline, [Check .NET Code with the Analyzers](../analyzers/check-dotnet-code-with-the-analyzers.md) runs the Roslyn analyzers over a .NET backend, and [Run Rules After Every Edit](run-rules-after-every-edit.md) returns the governing provisions after each edit and checks the change set before the agent stops. To find where a record file lives, look it up in [Record Folders](../reference/folders.md), which the release generates from the same source the checks read.

## Tutorials

- [Start a Consumer Repository](../tutorial/getting-started.md): start a small consumer repository through one coded use case.
- [Model a Behavior Module](../tutorial/model-domain.md): specify one spending module the builder reads.

## How-To Guides

- [Install the CLI](install-the-cli.md): install the command line for a consumer repository or a machine, by every method a release ships.
- [Start with Records Only](start-with-records-only.md): claim the Intent and Behavior records alone, and check them with the model check and the book.
- [Add a Svelte Frontend](add-a-svelte-frontend.md): declare a Svelte frontend on the web stack.
- [Add a Blazor Frontend](add-a-blazor-frontend.md): declare a Blazor frontend outside the controlled UI contract.
- [Add an Android Frontend](add-an-android-frontend.md): declare a native Android app built with .NET for Android.
- [Write Code for a Use Case](write-code-for-a-use-case.md): where each code element of a use case goes, the mark it carries, and the check that watches it.
- [Write a Problem](write-a-problem.md): write one problem with its failure cases, evidence and resolution criteria, step by step with weak and strong examples.
- [Write a Feature Under a Problem](write-a-feature-under-a-problem.md): write one feature with its requirements under the problem it answers, with statements a tester can decide and the feature's boundaries.
- [Write a Constraint](write-a-constraint.md): write one outside rule with its sources, its scope, its rules and the requirements that hold its effect.
- [Write a Journey](write-a-journey.md): write one actor's path toward one goal, from its start through its steps, blockers and branches.
- [Write a Research Record](write-a-research-record.md): write one question's findings, each tied to its source and marked as stated or inferred.
- [Build an Audit Trail](audit-trail.md): the audit extension on the .NET LiteBus pipeline, step by step.
- [Run Rules After Every Edit](run-rules-after-every-edit.md): run `rules` on each file after it is edited, and check the change set before the agent stops.
