# Settle the .NET Stack Binding, Protocol, and Sign-In

A consumer product built a whole application on the pre-release and found five gaps in the .NET stack. This decision binds the pipeline to the LiteBus 8.0.0-preview.3 APIs with one definition class per concern, points the agent protocol at the local docs, models a hosted sign-in as use cases, and consumes unreleased analyzer builds through a local feed.

## Context

The pinned binding named `LiteBus.Outbox.Storage.PostgreSql` or `LiteBus.Outbox.Storage.EntityFrameworkCore` for outbox storage, with no Marten storage, so a Marten project following it literally added a second persistence stack beside Marten only to hold outbox rows. The manifest pinned every LiteBus package by major version, which can resolve below the release the in-process dispatch needs.

The binding named `IAuditDefinition<TCommand>` for audit, said only that authorization runs through a guard reading the declaration from the message definition, and named nothing for idempotency. No page said how many definition classes a message has, where they sit, or what they are called, so the first implementation built custom attributes and guards instead.

The agent protocol told agents to read the online selection index of the reviewed release. That release is not published, so agents followed the nearest published release instead and built on its older layout.

Nothing said where a hosted sign-in fits, so each protocol step became a command calling the provider inline, which the effect rules refuse. And the analyzers failed to load when referenced by project, because their dependency assemblies do not travel with the reference.

## Options

LiteSpec compared 2 options for the stack gaps. The table sets each option against the same two points: what it gains and what it costs.

| Option | Gains | Costs |
|:---|:---|:---|
| Bind the pipeline to LiteBus 8.0.0-preview.3 definitions and Marten storage, point the protocol at the local docs, model hosted sign-in as use cases, and consume unreleased analyzers through a local feed | Every pipeline position names its LiteBus 8.0.0-preview.3 API, each message declares authorization, audit, and idempotency in placed definition classes, a Marten project stores outbox rows in its own transaction, agents read the release the project pins, the sign-in shows declared edges, and unreleased analyzer builds load. | The manifest pins LiteBus at exact versions, so a later LiteBus release needs a new LiteSpec release to bless it; the protocol template gains a docs placeholder that every renderer fills. |
| Keep one definition per message, Entity Framework Core storage beside Marten, major pins, the online protocol URL, host sign-in endpoints, and analyzer project references | No page, template, or renderer changes. | Each gap stays: invented guards beside LiteBus definitions, a second persistence stack for outbox rows, APIs that do not exist below 8.0.0-preview.3, agents following the wrong release, sign-in steps the effect rules refuse, and analyzers that fail to load. |

## Decision

Each message declares its pipeline positions in definition classes in its use case `Application/` folder: `{Message}AuthorizationDefinition`, `{Message}AuditDefinition`, and `{Message}IdempotencyDefinition`, each in its own file and implementing exactly one definition interface. Authorization and audit are unrelated concerns, so one class implementing both mixes them. A definition carries no LiteSpec mark; the handler carries the use case mark.

A Marten project stores outbox rows in its own session and transaction through `LiteBus.Outbox.Storage.Marten`, composed as `outbox.UsePostgreSqlStorage` with `UseMartenTransactionalOutbox`. A project that persists through Entity Framework Core keeps `LiteBus.Outbox.Storage.EntityFrameworkCore`.

The manifest pins every LiteBus package at `8.0.0-preview.3` and adds `LiteBus.Outbox.Storage.Marten`, because the schema states a pin as a major or an exact version, and only the exact pin names the preview.

The agent protocol points at the local docs of the carried release, never at an unpublished online release. The template carries a `__LITESPEC_DOCS__` placeholder: `model render` fills it with the local docs folder, and the site fills it with the selection index URL of the published release it shows.

A hosted sign-in is modeled as use cases that start and complete the sign-in, with the provider as an external system. The start use case builds the provider redirect with its PKCE challenge, and the complete use case exchanges the code, validates the tokens, finds or creates the person, and starts the session.

An unreleased analyzer build is consumed through a local feed, never through a project reference. The package route already bundles every assembly the analyzers load, so documentation beats new build machinery.

## Consequences

Each message declares its pipeline positions in its use case `Application/` folder in `{Message}AuthorizationDefinition`, `{Message}AuditDefinition`, and `{Message}IdempotencyDefinition`, each in its own file and implementing exactly one of `IAuthorizationDefinition<TMessage>`, `IAuditDefinition<TCommand>`, and `IIdempotencyDefinition<TMessage>`. A definition carries no LiteSpec mark, and a project uses no attribute or custom guard for what a definition declares.

A Marten project composes `outbox.UsePostgreSqlStorage` with `UseMartenTransactionalOutbox`, and a project that persists through Entity Framework Core keeps `LiteBus.Outbox.Storage.EntityFrameworkCore`. The manifest pins every LiteBus package at `8.0.0-preview.3` and adds `LiteBus.Outbox.Storage.Marten`.

The agent protocol template points at `__LITESPEC_DOCS__`, which `model render` fills with the local docs of the carried release and the site fills with the selection index URL, and it tells the agent to read this release only.

A hosted sign-in is modeled as use cases that start and complete the sign-in, with the provider as an external system. An unreleased analyzer build is consumed through a local feed, never through a project reference.

The new provision is `litespec/core/access#model-a-hosted-sign-in-as-use-cases`. The changed provisions are `litespec/backend/dotnet/application#bind-the-pipeline-to-the-pinned-litebus-apis`, `litespec/backend/dotnet/packages#run-litespec-analyzers-in-the-business-projects-and-hosts`, and the outbox paragraph of `docs/backend/dotnet/packages.md`.

## Revisit Conditions

This decision reopens when LiteBus moves its definitions or outbox storage to new APIs, when the tool can no longer ship its docs beside its binary, or when a hosted sign-in needs a shape the two use cases cannot state.

Any of those means the binding no longer names what projects compose, the protocol no longer points at what agents read, or the sign-in no longer fits its records. The team then decides again what the binding names and where the protocol points.
