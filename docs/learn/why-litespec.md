# Why LiteSpec Exists

When people or AI agents build software, they make decisions that rarely make it into the documentation. Requirements stay in chat threads. Business rules end up buried in handlers. The reasons behind a design disappear when someone leaves.

AI agents make this harder to manage: give two agents the same request and they may produce different designs. Both compile and both pass their tests, but each introduces its own patterns, names, and assumptions.

LiteSpec gives teams a shared set of rules, a place to record what the product should do, and a tool that checks how those records connect to the implementation.

## The Problem

An agent works from the context it has: the request, the files it reads, and the patterns it recognizes. If a requirement is missing, it may fill the gap rather than flag it.

Developers do this too. Over time, assumptions accumulate and the code, requirements, and documentation drift apart.

The same problems keep showing up:

- Nobody can explain which requirement a piece of code implements.
- Missing requirements become invented behavior.
- The same concept gets different names in different parts of the codebase.
- A feature is treated as complete even though its storage, API, tests, or operational needs haven't been addressed.
- Documentation describes behavior that the code and tests don't support.

Beneath these sits a deeper gap: no reliable record of what was intended, what was decided, and what was actually built.

## How LiteSpec Helps

LiteSpec puts that information in version-controlled records, so it doesn't depend on one developer's memory or one agent's context. Someone else can pick up the work and see the requirements, decisions, and constraints.

It has three parts.

### A Standard: How to Build

The standard defines rules for building one [bounded-context business application](../core/scope.md). It covers how to describe product intent and behavior, along with rules for code, tests, security, operations, and releases.

Each rule, called a provision, has a stable ID and states whether it is required, a default the project may replace with its own convention, or advisory. People, agents, and tools can refer to the same rule without having to infer how strictly it applies.

[Read the standard](reading-the-standard.md) explains how it is organized.

### Records: What to Build

A project describes its product in short YAML files with defined record types. These cover the product's modules, features, requirements, user flows, business rules, use cases, and supporting behavior, as well as the decisions behind them.

The records live in git. Committed records represent agreed decisions; changes are reviewed through pull requests.

### A Model Builder: How It Connects

A command-line tool reads the records and the references to them in code and tests. It maps connections such as:

- Which handler implements a use case?
- Which screen calls it?
- Which test verifies the expected behavior?

The builder reports missing connections and generates documentation from the model.

[How LiteSpec Works](how-litespec-works.md) explains the records, the tools, and when each tool runs.

## The Goal

LiteSpec aims to make product intent readable by someone who isn't a developer, while giving an agent enough detail to implement it without inventing requirements.

Work moves through four layers:

1. **Intent** describes what the product promises.
2. **Behavior** defines how the application keeps those promises.
3. **Implementation** is the code that implements that behavior.
4. **Proof** is the tests that verify the promised examples through the application's public boundary.

The [record layers](../core/layers.md) define what belongs in each layer and how one translates into the next.

## Why the Scope Is Narrow

LiteSpec covers one bounded-context business application: one domain, one API, and one database, with one or more frontends.

That limit is deliberate. A standard for every kind of software would have to leave many architectural choices open. LiteSpec fixes enough of the application's structure to give specific guidance: how to organize code by module, aggregate, and use case; how to model a lifecycle; and how to report a refused command.

Those choices give developers and agents fewer gaps to fill and fewer reasons to invent competing patterns.

The name reflects this scope. *Lite* means adopting only what applies to one bounded context and the project's chosen size and stack. *Spec* means recording the requirements and rules that guide the work.

## What LiteSpec Is Not

- **A framework or library.** You build with your own stack. LiteSpec supplies rules and checks, not runtime code.
- **A project-management process.** It doesn't prescribe sprints or ceremonies. Ownership stays in `CODEOWNERS`, not in record fields.
- **A standard for every kind of system.** Multi-service platforms, games, and embedded systems are outside its scope.

## Next

[How LiteSpec Works](how-litespec-works.md) walks through these parts using a Claimwick use case.
