# Overview

## Intent

The Guides section teaches by doing: tutorials take a reader to a first working result one step at a time, and how-to guides solve one stated problem in a project that already runs. Every guide cites the standard it applies and adds no rule of its own, so a reader learns the standard by following along.

## Where to Start

When LiteSpec is new, follow [Start a Consumer Repository](../tutorial/getting-started.md) first: it builds a working consumer with one implemented use case on the `dotnet` backend and a React frontend. Then follow [Model a Behavior Module](../tutorial/model-domain.md) to specify one business area from Intent into Behavior the model builder reads. When the product ships another frontend, follow [Add a Svelte Frontend](add-a-svelte-frontend.md), [Add a Blazor Frontend](add-a-blazor-frontend.md), or [Add an Android Frontend](add-an-android-frontend.md). When the project runs and one goal needs solving, read the how-to that names it: [Write a Feature Under a Module](write-a-feature-under-a-module.md) writes one feature with its requirements under its module, [Folder Reference](folder-reference.md) lists where each record file lives, [Build an Audit Trail](audit-trail.md) implements the audit extension on the .NET command pipeline, and [Check .NET Code with the Analyzers](../analyzers/check-dotnet-code-with-the-analyzers.md) runs the Roslyn analyzers over a .NET backend.

## Tutorials

- [Start a Consumer Repository](../tutorial/getting-started.md): start a small consumer repository through one implemented use case.
- [Model a Behavior Module](../tutorial/model-domain.md): specify one spending module the builder reads.

## How-To Guides

- [Add a Svelte Frontend](add-a-svelte-frontend.md): declare a Svelte frontend on the web stack.
- [Add a Blazor Frontend](add-a-blazor-frontend.md): declare a Blazor frontend outside the controlled UI contract.
- [Add an Android Frontend](add-an-android-frontend.md): declare a native Android app built with .NET for Android.
- [Write a Feature Under a Module](write-a-feature-under-a-module.md): write one feature with its requirement entries under its module.
- [Folder Reference](folder-reference.md): where each record file lives, with its parent and its identifiers.
- [Build an Audit Trail](audit-trail.md): the audit extension on the .NET LiteBus pipeline, step by step.
