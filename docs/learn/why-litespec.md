# Why LiteSpec Exists

When people or AI agents build software, they make decisions that rarely reach the documentation. Requirements stay in chat threads. Business rules end up buried in handlers. The reasons behind a design leave when someone leaves the team.

AI agents make this harder to manage. Give two agents the same request and you get two designs. Both compile and both pass their tests, but each brings its own patterns, names, and assumptions.

LiteSpec gives your team a shared set of rules, a place to record what the product does, and a tool that checks how those records connect to the code. You write the rules once, and every agent follows the same ones.

## The Problem

An agent works from the context it has: the request, the files it reads, and the patterns it recognizes. When a requirement is missing, it fills the gap instead of flagging it. Developers do the same thing over time. Assumptions pile up, and the code, the requirements, and the documentation drift apart.

The same five problems keep showing up. Nobody can say which requirement a piece of code implements. Missing requirements turn into invented behavior. One concept gets a different name in each part of the codebase. A feature counts as done while its storage, API, tests, or operations are still open. Documentation describes behavior that no code or test supports.

Under all five sits one deeper gap: no reliable record of what you intended, what you decided, and what you actually built.

## How LiteSpec Helps

LiteSpec puts that information in version-controlled records, so it stops depending on one developer's memory or one agent's context. Someone new picks up the work and sees the requirements, decisions, and constraints as they stand.

It has three parts.

### A Standard: How You Build

The standard defines the rules for building one [bounded-context business application](../core/scope.md). It covers how you describe product intent and behavior, with rules for code, tests, security, operations, and releases. The `dotnet` pack states the backend shape, the `svelte` and `android` packs state the frontend shapes, and each screen passes its checks before it ships.

Each rule, called a provision, carries a stable ID and states whether it binds you, offers a default you may replace, or advises you. People, agents, and tools cite the same rule instead of guessing how strictly it applies.

[Read the Standard](reading-the-standard.md) explains how the standard is organized.

### Records: What You Build

You describe your product in short YAML files with defined record types. They cover the product's modules, features, requirements, user flows, business rules, use cases, and the decisions behind them. Field names read in the context of their record type, and short Markdown files beside the YAML give background without carrying facts the tools check.

The records live in git. Committed records are agreed decisions, and changes go through pull requests like code.

### A Model Builder: How It Connects

A command-line tool reads the records and the references to them in code and tests. It maps connections such as which handler implements a use case, which screen calls it, and which test proves the expected behavior. It reports the missing connections and renders the documentation from the model.

[How LiteSpec Works](how-litespec-works.md) explains the records, the tools, and when each tool runs.

## The Goal

LiteSpec aims to make product intent readable by someone who is not a developer, while giving an agent enough detail to implement it without inventing requirements.

Work moves through four layers, from Intent through Behavior and Implementation to Proof. The [record layers](../core/layers.md) define what belongs in each layer and how one translates into the next. For example, the Claimwick requirement that an expense above 25 EUR carries a receipt starts as an Intent requirement, becomes a Behavior scenario on the submit use case, ships as a handler check in Implementation, and is proven by a passing test in Proof.

## Why the Scope Is Narrow

LiteSpec covers one bounded-context business application: one domain, one API, and one database, with one or more frontends built from the `dotnet` backend pack and the `svelte` and `android` frontend packs.

That limit is deliberate. A standard for every kind of software would have to leave the big architectural choices open. LiteSpec fixes enough of the application structure to give specific guidance: how you organize code by module, aggregate, and use case, how you model a lifecycle, and how you report a refused command. Those choices leave developers and agents fewer gaps to fill and fewer reasons to invent competing patterns.

The name reflects this scope. *Lite* means you adopt only what applies to one bounded context and your project's chosen size and stack. *Spec* means you record the requirements and rules that guide the work.

## What LiteSpec Is Not

- **A framework or library.** You build with your own stack. LiteSpec supplies rules and checks, not runtime code.
- **A project-management process.** It prescribes no sprints or ceremonies. Ownership stays in `CODEOWNERS`, not in record fields.
- **A standard for every kind of system.** Multi-service platforms, games, and embedded systems sit outside its scope.

## Next

[How LiteSpec Works](how-litespec-works.md) walks through these parts using a Claimwick use case.
