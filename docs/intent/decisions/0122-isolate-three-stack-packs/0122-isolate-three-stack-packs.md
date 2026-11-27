# Isolate Three Stack Packs and Retire Blazor React

LiteSpec ships one folder per supported stack. A project picks its stacks, and reads only those folders.

## Context

One tree carried five frontend targets, so every shared page served several stacks and no page spoke to one owner.

Blazor and React carried dated templates with no active consumer. The `dotnet`, `svelte`, and `android` stacks each needed their own guides, templates, and checks, but shared pages buried the stack facts.

## Options

LiteSpec compared 3 options. The table sets each against what it gains and what it costs.

| Option | Gains | Costs |
|:---|:---|:---|
| Ship three isolated Stack Packs and remove Blazor and React | Each stack owner reads one folder with its own guides and templates. Shared pages stay small because stack detail moves out. | A team on a removed stack migrates or stays on an older release. |
| Keep all five targets in one tree | No team migrates, and every option stays listed. | Every page serves several stacks, and dated templates stay beside current ones. |
| Merge all stacks into one generic guide | One guide to keep. | Stack facts blur, and owners cannot tell which rule covers their code. |

## Decision

LiteSpec ships three Stack Packs: `dotnet`, `svelte`, and `android`. Selecting a stack applies its folder with the shared folders above it.

Blazor and React leave the tree with their templates, guides, and checks. A Claimwick expense report shows the result: a `dotnet` API with a `svelte` web client, and no page mentioning the removed stacks.

## Consequences

Five optional extensions remain: `delivery`, `ai-mcp`, `compat`, `realtime`, and `plans`. Everyday capabilities such as audit, tenancy, and jobs live in Core.

A project names its stacks in its manifest, and the toolchain applies only those folders. Removed-stack references fail in `validate` instead of lingering in prose.

## Revisit Conditions

Reopen this when a new frontend target gains an active consumer with its own templates and checks, or when a removed stack regains one.
