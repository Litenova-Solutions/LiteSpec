# Why LiteSpec Exists

## Intent

LiteSpec exists because teams that build with AI agents watch the same request produce a different design each time, while the reasons behind past designs leave with the people who made them. This page names those failure modes and the 3 parts LiteSpec answers with: a versioned standard (one published set of rules), written records (short YAML files stating product intent), and a model builder (the command joining records to code, screens, and tests) that checks one against the other. Technical leads read it when they decide whether LiteSpec fits their team.

## The Problem

AI [agents](../reference/glossary.md) now write a large share of the code in many teams. Each one starts from what it can see: the request, the files it opens, and its own habits. Give the same request to two agents, or to one agent on two days, and the two designs differ. Both compile. Both pass their own tests. Together they leave a codebase with no single shape.

People drift the same way, more slowly. Over months, the reasons behind a design leave with the people who made them. A rule lives only in a handler. A requirement lives only in a chat thread. The documentation describes what the system did a year ago.

These gaps cause failures that repeat:

- Intent is lost between the request and the code, so nobody can say which requirement a piece of code serves.
- An agent meets a gap and invents behavior, because nothing marks the gap as missing.
- Familiar patterns are renamed until a new developer no longer recognizes them.
- A feature ships in one layer while its storage, its API, its tests, or its operating impact is missing.
- Documentation claims behavior that the code and its tests do not show.

## The Approach

LiteSpec makes the written record durable and the person or agent doing the work replaceable. A different developer, model, or tool can pick up the same records and reach the same boundaries.

It does that with three parts:

- **A standard.** A versioned set of rules for building one [bounded-context business application](../core/scope.md): how Intent states promises, how Behavior keeps them, and how code, tests, security, operations, and releases work. Each rule carries a stable ID, so a person, an agent, or a tool cites exactly one rule, and each states its strength, so an agent tells binding from advisory without a check existing. [Read the standard](reading-the-standard.md) explains how the standard is organized.
- **Records.** A project writes its intent as small record files of fixed types: the product, its modules with their features and requirements, its journeys, its aggregates, rules, use cases with their results, the reactions, jobs, notifications, and settings around them, and the decisions behind them. A record in git is decided, and the pull request that changes it is its review. [Records and the project model](records-and-the-model.md) explains what a project writes down.
- **A model builder.** A command-line tool reads the records, follows the marks by which code and tests point up to them, and works out how they connect: which handler implements which use case, which screen calls it, and which test proves it. It reports what is missing, and it renders the book the team reads. [How the tools fit together](the-tooling.md) explains when each tool runs.

The standard tells everyone how to build. The records say what to build, top down from promises to behavior to code. The model shows whether the two match.

## The End Goal

LiteSpec aims at one end goal: Intent so clear and structured that a person who is not a developer can read it, and exact enough that an agent builds from it without guessing.

Work runs top down through four layers. People and agents write Intent first: why the product exists and what it promises. Agents write Behavior from Intent: how the system acts to keep the promises. Agents write code from Behavior, and tests prove each promised example through the public boundary. Nothing above code is computed from code: each layer is written from the layer above it, and code points up to the record it serves.

Decisions live in git. Product people read Intent as journey detail: what each journey promises and what changes. Developers read Behavior: the modules, aggregates, use cases, reactions, jobs, notifications, and settings that carry the promises out. The tools prove that code matches Behavior and that every promised example passes.

The same shape makes a specification easy to learn and easy to use: a reader always knows where they are, what sits above (why) and what sits below (how), and which records they are expected to read. [Records and the project model](records-and-the-model.md) states the four layers and what the builder computes.

## Non-Goals

Three things LiteSpec does not try to be:

- **No framework or library.** A team builds with its own stack. LiteSpec states how, and checks the result.
- **No process.** LiteSpec holds no ceremonies or sprints. Who answers for an area is `CODEOWNERS`, not a record field.
- **No wider scope.** LiteSpec covers one bounded-context business application: one domain, one API, and one database, with one or more frontends. A platform of many services, a game, or an embedded system sits outside that scope.

## One Narrow Scope

LiteSpec stays narrow on purpose. A standard that tries to cover every architecture must stay vague, and a vague rule gives an agent nothing to follow. By fixing the shape of the application, LiteSpec states concrete rules: the modules project ordered by module, aggregate, and use case, one way to model a lifecycle, and one way to report a refused command. An agent that follows them makes the same choices a careful senior developer makes.

The name reflects that scope. *Lite* means a project adopts one bounded context, one size, and the stacks it selects, and nothing more. *Spec* means written records and rules define the work.

## Next

[How LiteSpec works](how-litespec-works.md) follows these parts through one Claimwick use case.
