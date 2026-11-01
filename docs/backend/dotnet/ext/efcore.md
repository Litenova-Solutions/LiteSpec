# EF Core Persistence

## Intent

This extension gives selected aggregates relational storage through Entity Framework Core. It states the Entity Framework Core form of the read, query, mapping, migration, and commit rules for aggregates needing relational navigation, change tracking, or provider behavior the Marten document-storage baseline does not offer. Teams use it after a recorded decision names the scope and the baseline gap, so the selected aggregates get relational storage while every other aggregate keeps the baseline, one aggregate path at a time.

## Activation

Activation scope: `local`.

Applicable specification kinds: `module`, `use-case`.

The consumer enables `efcore` only after a decision records aggregate scope, relational requirement, data evolution effect, and the Marten baseline gap. Without that record, relational storage spreads by habit instead of by need.

## Baseline Relationship

For Entity Framework Core-owned aggregate paths, this extension replaces:

- `litespec/backend/dotnet/persistence#query-through-iquerysession`: EF Core queries use `IApplicationDbContext`.
- `litespec/backend/persistence#keep-mappings-and-aliases-explicit`: EF Core mappings use `IEntityTypeConfiguration<T>` classes.
- `litespec/backend/dotnet/packages#keep-application-dependencies-narrow`: Application references the Entity Framework Core query surface its query interface needs.
- `litespec/backend/architecture#separate-command-and-query-behavior`: it replaces only the read half, and the command half still binds.

For an EF Core-owned command with durable delivery, this extension also replaces:

- `litespec/ext/outbox#stage-messages-with-business-work`
- `litespec/ext/outbox#reject-independent-outbox-commits`

Every other baseline provision binds EF Core-owned paths unchanged.

This extension switches on no optional record type.

## Standards

### Record EF Core Replacement Scope

**ID:** `litespec/backend/dotnet/ext/efcore#record-ef-core-replacement-scope`

**Statement:** An EF Core adoption decision MUST name affected aggregates, data evolution, transaction requirements, query impact, package changes, and rollback plan.

**Rationale:** EF Core replaces the provider, the transaction boundary, the query surface, and the migration story for its aggregates. A decision that names aggregates, data evolution, transaction requirements, query impact, package changes, and rollback plan lets review judge the whole replacement; without it, each part is decided separately and they disagree.

**Reviewed for:** Does the adoption decision name affected aggregates, data evolution, transaction requirements, query impact, package changes, and rollback plan? Look at the adoption decision.

### Retain Unaffected Baseline Persistence

**ID:** `litespec/backend/dotnet/ext/efcore#retain-unaffected-baseline-persistence`

**Statement:** An unaffected aggregate MUST retain baseline persistence rules.

**Rationale:** The extension activates per aggregate path, so aggregates outside its scope keep Marten behavior, Marten queries, and Marten commits. Letting EF Core conventions leak onto those paths leaves two providers over one aggregate with no owner.

**Reviewed for:** Which aggregate paths outside the recorded scope break baseline persistence behavior? Look at each unaffected aggregate path.

### Use One Write Provider

**ID:** `litespec/backend/dotnet/ext/efcore#use-one-write-provider`

**Statement:** An EF Core-owned command MUST stage aggregate writes through exactly one selected write provider.

**Rationale:** A command that stages writes through one provider commits them in one atomic boundary. Without that, part of the business work can persist while the rest fails.

**Reviewed for:** Which commands stage aggregate work through more than one provider? Look at the repository calls in each command handler.

### Reject Mixed Provider Writes

**ID:** `litespec/backend/dotnet/ext/efcore#reject-mixed-provider-writes`

**Statement:** A command MUST NOT stage aggregate writes through both Marten-backed and EF Core-backed repositories.

**Rationale:** Marten and EF Core commit independently, so a command writing through both can persist one half and lose the other. No transaction spans the two, so one local invariant cannot survive a mixed write.

**Reviewed for:** Which commands stage writes through both Marten-backed and EF Core-backed repositories? Look at the repository calls in each command handler.

### Record Provider Selection

**ID:** `litespec/backend/dotnet/ext/efcore#record-provider-selection`

**Statement:** An EF Core adoption decision MUST name command provider selection and its architecture-test evidence.

**Rationale:** Each command path has exactly one owner, and the decision names it with the architecture-test evidence that proves it. Without that record, a later change moves a repository to the other provider and review cannot tell the boundary moved.

**Reviewed for:** Does the adoption decision name each command path's provider with its architecture-test evidence? Look at the adoption decision.

### Register One Command Post-Handler

**ID:** `litespec/backend/dotnet/ext/efcore#register-one-command-post-handler`

**Statement:** An EF Core-owned command MUST register only its selected provider's LiteBus post-handler.

**Rationale:** The post-handler owns the command's one commit, and each provider's handler commits only its own session. Registering the wrong handler, or both, leaves the staged work uncommitted or commits the wrong session.

**Reviewed for:** Which commands resolve a post-handler other than their selected provider's? Look at the post-handler registration for each EF Core-owned command.

### Resolve Cross-Provider Invariants

**ID:** `litespec/backend/dotnet/ext/efcore#resolve-cross-provider-invariants`

**Statement:** An accepted atomic cross-provider invariant MUST move its aggregates to one provider or use a reviewed shared transaction mechanism.

**Rationale:** An invariant spanning a Marten aggregate and an EF Core aggregate has no atomic boundary until both sit on one provider or share a reviewed transaction mechanism. Accepting the invariant without either leaves a rule the storage cannot enforce.

**Reviewed for:** Which accepted atomic cross-provider invariants sit across providers without a reviewed shared transaction mechanism? Look at each such invariant.

### Stage Aggregate Writes in Repositories

**ID:** `litespec/backend/dotnet/ext/efcore#stage-aggregate-writes-in-repositories`

**Statement:** An EF Core Infrastructure repository MUST use its scoped `DbContext` to stage aggregate changes.

**Rationale:** Handlers coordinate; repositories stage. When the repository stages changes through its scoped `DbContext`, no handler names a provider type and the commit stays in the post-handler.

**Reviewed for:** Which repositories stage changes outside their scoped `DbContext`? Look at the Infrastructure repository implementations.

### Exclude Direct Handler Commits

**ID:** `litespec/backend/dotnet/ext/efcore#exclude-direct-handler-commits`

**Statement:** An EF Core command handler MUST NOT inject `DbContext` or call `SaveChangesAsync`.

**Rationale:** A handler that injects `DbContext` or calls `SaveChangesAsync` commits mid-pipeline, before validation, authorization, or later stages finish. The selected post-handler owns the one commit, after the whole pipeline succeeds.

**Reviewed for:** Which handlers inject `DbContext` or call `SaveChangesAsync`? Look at the constructor parameters and bodies of each command handler.

### Define EF Core Query Roots

**ID:** `litespec/backend/dotnet/ext/efcore#define-ef-core-query-roots`

**Statement:** Application MUST own one public `IApplicationDbContext` exposing required generic sets or named query roots, referencing only the Entity Framework Core abstractions that interface needs.

**Rationale:** Queries need an EF Core surface in Application, but exposing the Infrastructure `DbContext` would hand handlers the whole provider. One public `IApplicationDbContext` exposes only the required sets or named roots, and the permitted surface is those query types plus the manifest-pinned abstractions that declare them.

**Reviewed for:** Which Application types expose EF Core surface beyond the owned `IApplicationDbContext`? Look at the public types in the Application project.

### Implement Query Roots in Infrastructure

**ID:** `litespec/backend/dotnet/ext/efcore#implement-query-roots-in-infrastructure`

**Statement:** Infrastructure MUST implement `IApplicationDbContext` with the application `DbContext`.

**Rationale:** The application `DbContext` implements the owned interface, so provider configuration, connection handling, and mapping stay in Infrastructure while Application programs against the interface.

**Reviewed for:** Does the Infrastructure `DbContext` implement the owned query interface? Look at the application `DbContext` class declaration.

### Retain Marten Query Boundary

**ID:** `litespec/backend/dotnet/ext/efcore#retain-marten-query-boundary`

**Statement:** A Marten-owned query path MUST retain the baseline `IQuerySession` boundary.

**Rationale:** Marten-backed aggregates keep their `IQuerySession` boundary, so their queries neither gain an EF Core dependency nor lose the session they were written against.

**Reviewed for:** Which Marten query handlers leave the `IQuerySession` boundary? Look at the read calls in each Marten-owned query handler.

### Project EF Core Queries Directly

**ID:** `litespec/backend/dotnet/ext/efcore#project-ef-core-queries-directly`

**Statement:** An EF Core query handler MUST use `AsNoTracking`, filter before materialization, order and limit deterministically, and project records.

**Rationale:** `AsNoTracking` with filtering, deterministic order and limit before materialization returns shaped records without loading mutable aggregate state. Tracking, unfiltered loads, or nondeterministic paging waste work and return unstable pages. A read projection does not feed an invariant, because it may already be stale.

**Reviewed for:** Which queries track entities, skip filters, page nondeterministically, skip projection, or feed an invariant from a projection? Look at the query bodies in each EF Core query handler.

### Avoid Per-Aggregate Read Stores

**ID:** `litespec/backend/dotnet/ext/efcore#avoid-per-aggregate-read-stores`

**Statement:** An EF Core query implementation MUST NOT add one read-store interface per aggregate.

**Rationale:** One read-store interface per aggregate rebuilds a repository layer the context already provides, and each new aggregate adds another interface to maintain. `IApplicationDbContext` is the one query boundary.

**Reviewed for:** Which per-aggregate read-store interfaces exist beside `IApplicationDbContext`? Look at the interfaces in the Application project.

### Resolve the Tenant at Context Creation

**ID:** `litespec/backend/dotnet/ext/efcore#resolve-the-tenant-at-context-creation`

**Statement:** An EF Core `DbContext` MUST resolve the tenant when the context is created and apply the tenant filter to tenant-owned sets.

**Rationale:** Resolving the tenant when the context is created applies the filter to every tenant-owned set by convention. A tenant passed per query is a parameter every new query can forget, leaking rows across tenants.

**Reviewed for:** Which contexts resolve the tenant after creation or miss a tenant filter on an owned set? Look at the `DbContext` creation and its entity configurations.

### Commit EF Core Command Work Once

**ID:** `litespec/backend/dotnet/ext/efcore#commit-ef-core-command-work-once`

**Statement:** An EF Core LiteBus post-handler MUST call `SaveChangesAsync` once for its owned command.

**Rationale:** One `SaveChangesAsync` call per owned command keeps the provider transaction boundary intact. Two calls split one command across two transactions, so the second can fail after the first commits.

**Reviewed for:** Which post-handlers call `SaveChangesAsync` more than once for one owned command? Look at each EF Core post-handler body.

### Process Changed Aggregate Events

**ID:** `litespec/backend/dotnet/ext/efcore#process-changed-aggregate-events`

**Statement:** An EF Core post-handler MUST collect domain events from changed aggregates and route each to the reaction its reaction record declares.

**Rationale:** The post-handler collects domain events from the aggregates it changed and routes each to the reaction the reaction record declares, honoring its `onFailure` retry and dead-letter policy. Collecting from anywhere else drops events or delivers facts the commit did not store.

**Reviewed for:** Which domain events from changed aggregates are dropped or miss their declared `onFailure` handling? Look at the event collection in each post-handler.

### Use Durable Outbox Delivery

**ID:** `litespec/backend/dotnet/ext/efcore#use-durable-outbox-delivery`

**Statement:** An EF Core command requiring durable delivery MUST use the outbox extension without committing a Marten session.

**Rationale:** A command needing durable delivery stages its outbox record through EF Core, so business work and the record commit atomically. Committing a Marten session instead splits the two across providers.

**Reviewed for:** Which durable EF Core commands commit a Marten session? Look at the session calls in each durable command path.

### Stage EF Core Outbox Records Together

**ID:** `litespec/backend/dotnet/ext/efcore#stage-ef-core-outbox-records-together`

**Statement:** An EF Core-owned command MUST map and stage outbox records with aggregate changes through the same `DbContext`.

**Rationale:** Mapping and staging the outbox record through the same `DbContext` puts it in the command's transaction. Staging it anywhere else emits facts the business commit did not store, or stores work no message ever delivers.

**Reviewed for:** Which commands stage their outbox record outside their aggregate `DbContext`? Look at the outbox staging calls in each command path.

### Reject Cross-Provider Outbox Storage

**ID:** `litespec/backend/dotnet/ext/efcore#reject-cross-provider-outbox-storage`

**Statement:** An EF Core-owned command MUST NOT store its required outbox record through a Marten session.

**Rationale:** A Marten session commits independently of the EF Core transaction, so an outbox record stored through Marten can persist while the aggregate change rolls back, or the reverse. The record travels with the work it describes.

**Reviewed for:** Which EF Core commands store their outbox record through a Marten session? Look at the outbox staging calls in each command path.

### Configure EF Core Mappings in Infrastructure

**ID:** `litespec/backend/dotnet/ext/efcore#configure-ef-core-mappings-in-infrastructure`

**Statement:** Infrastructure MUST own `IEntityTypeConfiguration<T>` classes for EF Core persistence mappings.

**Rationale:** Entity configurations name tables, columns, and provider behavior, so they live with the provider in Infrastructure. Anywhere else, Application or Domain would name relational details they must not know.

**Reviewed for:** Which EF Core mappings live outside an Infrastructure-owned configuration class? Look at the mapping classes in each project.

### Configure Relational Details Explicitly

**ID:** `litespec/backend/dotnet/ext/efcore#configure-relational-details-explicitly`

**Statement:** An EF Core mapping MUST explicitly configure typed IDs, owned values, fields, indexes, constraints, precision, deletes, concurrency tokens, and database names.

**Rationale:** Conventions silently pick names, precisions, and delete behavior, and a provider upgrade can change the silence. Explicit configuration for typed IDs, owned values, fields, indexes, constraints, precision, deletes, concurrency tokens, and database names puts every storage choice in review and in migrations.

**Reviewed for:** Which mappings lack explicit configuration of typed IDs, owned values, fields, indexes, constraints, precision, deletes, concurrency tokens, or database names? Look at the mapping review.

### Retain Domain State Values

**ID:** `litespec/backend/dotnet/ext/efcore#retain-domain-state-values`

**Statement:** EF Core persistence MUST retain the Domain aggregate's single `{Aggregate}State` value.

**Rationale:** The aggregate owns its lifecycle state and state-specific facts, and relational storage records them without reshaping them. A relational column holds the active state record and its discriminator, which is the same contract the baseline provider stores in its document. The column holds no enumerated integer, because that discards the state-specific facts the record carries and reintroduces the shape `litespec/backend/domain#model-every-closed-set-of-domain-values-as-a-closed-type-hierarchy` removes. A state carrying its own fields maps to an owned type or to its own table. The discriminator selects which one a read materializes.

**Reviewed for:** Which persistence mappings reshape the aggregate's single Domain state value? Look at the state columns and owned types in each mapping.

### Map Stable Relational State Shape

**ID:** `litespec/backend/dotnet/ext/efcore#map-stable-relational-state-shape`

**Statement:** Infrastructure MUST map a stable discriminator and every state-specific value without adding Domain lifecycle flags or duplicate nullable state properties.

**Rationale:** Columns and owned types store each state's values while Domain keeps its record hierarchy. New lifecycle flags or duplicate nullable state properties in Domain would create a second lifecycle that disagrees with the first, so the mapping adds none.

**Reviewed for:** Which lifecycle flags or duplicate nullable state properties entered Domain for storage? Look at the aggregate classes in the Domain project.

### Use Direct State Mapping Only When Supported

**ID:** `litespec/backend/dotnet/ext/efcore#use-direct-state-mapping-only-when-supported`

**Statement:** An EF Core mapping MAY use direct owned or JSON state mapping only when pinned versions materialize, track, and round-trip each state record.

**Rationale:** Owned or JSON mapping of state records works only when the pinned provider versions materialize, track, and round-trip every sealed state record. Without that proof, reads silently drop state values or return stale state.

**Reviewed for:** Which state records fail to materialize, track, or round-trip under the pinned versions? Look at a round-trip read of each sealed state record.

### Map Unsupported State Shapes in Infrastructure

**ID:** `litespec/backend/dotnet/ext/efcore#map-unsupported-state-shapes-in-infrastructure`

**Statement:** An unsupported direct state mapping MUST use an Infrastructure persistence type with discriminator and state-specific columns.

**Rationale:** When direct mapping is unsupported, an Infrastructure persistence type with a discriminator and state-specific columns stores the state, and the repository maps it to and from the Domain hierarchy. Domain stays free of the storage shape.

**Reviewed for:** Which discriminator values map to no Domain state? Look at the discriminator map in each state mapping.

### Review New Persisted States

**ID:** `litespec/backend/dotnet/ext/efcore#review-new-persisted-states`

**Statement:** A new persisted state MUST include reviewed migration, mixed-version behavior, rollback behavior, and an integration fixture.

**Rationale:** A new state value lands in stored rows that older application versions still read. Reviewing the migration, mixed-version behavior, rollback behavior, and an integration fixture keeps the rollout and the rollback safe.

**Example:** `PostRow` maps `state_type`, `published_at`, and `archived_at` to one `PostState` record.

**Reviewed for:** Does the new state review cover migration, mixed-version behavior, rollback behavior, and an integration fixture? Look at the new state review.

### Generate Migrations for Schema Changes

**ID:** `litespec/backend/dotnet/ext/efcore#generate-migrations-for-schema-changes`

**Statement:** An EF Core schema change MUST generate a migration.

**Rationale:** A schema change without a migration reaches the database by hand or by drift, with no review and no record. The generated migration is the explicit, reviewed description of the evolution.

**Reviewed for:** Which schema diffs carry no generated migration? Look at the migration list beside the model snapshot.

### Review Migration Effects

**ID:** `litespec/backend/dotnet/ext/efcore#review-migration-effects`

**Statement:** A migration review MUST cover tables, columns, indexes, constraints, data movement, destructive operations, and rollback compatibility.

**Rationale:** Tables, columns, indexes, constraints, data movement, destructive operations, and rollback compatibility each decide whether a deploy is safe and whether the data survives it. Reviewing all of them catches the destructive step before it runs in production.

**Reviewed for:** Does the migration review cover tables, columns, indexes, constraints, data movement, destructive operations, and rollback compatibility? Look at the migration review.

### Apply Migrations as Release Work

**ID:** `litespec/backend/dotnet/ext/efcore#apply-migrations-as-release-work`

**Statement:** A release process MUST apply EF Core migrations before application startup.

**Rationale:** Migrations applied as release work run in a controlled order with evidence, before the new code starts. Startup-owned execution races replicas against each other and against the old version.

**Reviewed for:** Which release records skip applying migrations before replica startup? Look at the release process record.

### Keep Migration Files Together

**ID:** `litespec/backend/dotnet/ext/efcore#keep-migration-files-together`

**Statement:** An EF Core schema change MUST commit migration, model snapshot, and reviewed SQL together.

**Rationale:** The migration, the model snapshot, and the reviewed SQL committed together show the intended change and its exact relational effect. Any one alone leaves review guessing what the database will do.

**Reviewed for:** Which migration commits miss their migration, model snapshot, or reviewed SQL? Look at the files in each migration commit.

### Test Migration Starting States

**ID:** `litespec/backend/dotnet/ext/efcore#test-migration-starting-states`

**Statement:** An EF Core migration MUST apply successfully from an empty database and previous release database.

**Rationale:** A migration applies to a fresh database and to the previous release's database, because setup and upgrade are the two paths that run in production. The previous release database is the schema of the last released artifact, not the schema of the previous commit. The test builds it by applying that artifact's migrations to an empty database, then applies the current ones on top. A test that starts from the current migrations minus one proves that the last migration applies, which is the start least likely to be broken.

**Reviewed for:** Which migrations fail from an empty database or from the previous release database? Look at the migration test runs.

### Reject Startup Schema Mutation

**ID:** `litespec/backend/dotnet/ext/efcore#reject-startup-schema-mutation`

**Statement:** Production and staging startup MUST NOT call `EnsureCreated`, `EnsureDeleted`, or `Migrate`.

**Rationale:** Every replica runs startup, so a startup that mutates the schema races its siblings and applies schema work with no ordering or evidence. Production and staging startup own none of `EnsureCreated`, `EnsureDeleted`, or `Migrate`.

**Reviewed for:** Which production or staging startup calls `EnsureCreated`, `EnsureDeleted`, or `Migrate`? Look at the startup paths of those environments.

### Configure Optimistic Tokens

**ID:** `litespec/backend/dotnet/ext/efcore#configure-optimistic-tokens`

**Statement:** An EF Core aggregate MUST configure a provider-backed concurrency token and carry expected version through its command.

**Rationale:** The provider-backed token carries the caller's expected version into the write, so EF Core detects a concurrent change instead of silently overwriting it. Without the token, the last writer wins and the first writer's work vanishes. Every EF Core aggregate configures the token, following `litespec/backend/persistence#version-every-aggregate`.

**Reviewed for:** Which aggregates miss their configured token, or which commands miss their expected version? Look at the entity configurations and command messages.

### Map EF Core Concurrency Conflict

**ID:** `litespec/backend/dotnet/ext/efcore#map-ef-core-concurrency-conflict`

**Statement:** An EF Core post-handler MUST translate `DbUpdateConcurrencyException` into the Application conflict contract.

**Rationale:** A `DbUpdateConcurrencyException` names a provider type that Application does not expose. Translating it into the project-owned conflict contract keeps the failure transport-neutral and lets the error boundary map it like any other conflict.

**Reviewed for:** Which `DbUpdateConcurrencyException` paths escape the Application conflict contract? Look at the post-handler fault mapping.

### Map Unique Constraint Violations to Their Refusals

**ID:** `litespec/backend/dotnet/ext/efcore#map-unique-constraint-violations-to-their-refusals`

**Statement:** Each EF Core unique constraint MUST map by name to the conflict refusal of the use cases that can violate it.

**Rationale:** The post-handler translates the provider violation through the same fault classification that maps `DbUpdateConcurrencyException`, so a uniqueness race and a version race reach the caller as named conflict refusals alike. This provision states the EF Core form of `litespec/backend/persistence#map-a-constraint-violation-to-its-refusal`.

**Example:** A book barcode column carries a unique constraint named `uq_book_barcode`; a registration that loses the barcode race translates to `book-barcode-taken` through the map, the same path a stale version takes to the approve conflict.

**Reviewed for:** Which unique constraint violations escape the Application conflict contract? Look at the post-handler fault mapping.

### Reject Automatic Command Retry

**ID:** `litespec/backend/dotnet/ext/efcore#reject-automatic-command-retry`

**Statement:** An EF Core post-handler MUST NOT retry the complete command automatically after a concurrency exception.

**Rationale:** After a concurrency conflict, the state changed under the caller. Retrying the whole command reruns business behavior against state the caller did not see or approve, so the post-handler reports the conflict and lets the caller reread and decide.

**Reviewed for:** Which post-handlers retry the complete command automatically after a concurrency exception? Look at the exception handling in each post-handler.

## Conventions

### Use the EF Core Infrastructure Layout

**ID:** `litespec/backend/dotnet/ext/efcore#use-the-ef-core-infrastructure-layout`

**Default:** Use `Infrastructure/Persistence/EfCore/` for `ApplicationDbContext`, factory, post-handler, configurations, migrations, and repositories.

**Replacement:** A project names its own persistence layout in its documentation.

**Rationale:** One folder holds the provider's context, factory, post-handler, configurations, migrations, and repositories, so every EF Core persistence question has one place. `Infrastructure/Persistence/EfCore/Configurations/` holds the mapping classes.

**Reviewed for:** Which EF Core Infrastructure paths break the default layout without a documented alternative? Look at the persistence paths beside the project documentation.

### Use Snake-Case Relational Names

**ID:** `litespec/backend/dotnet/ext/efcore#use-snake-case-relational-names`

**Default:** Use the pinned naming-conventions package and verify generated names in each migration, with casing owned by `litespec/backend/persistence#use-database-naming-conventions`.

**Replacement:** A project names its own relational naming in its documentation.

**Rationale:** The pinned naming-conventions package generates the names, and verifying each migration shows the exact names that reach the database. Casing itself is owned by `litespec/backend/persistence#use-database-naming-conventions`.

**Reviewed for:** Which generated migrations carry a database name outside the verified snake-case form? Look at the names in each generated migration.

## Dependencies

Packages: `Microsoft.EntityFrameworkCore`, `Npgsql.EntityFrameworkCore.PostgreSQL`, and `EFCore.NamingConventions`, each pinned in the manifest.

Related extensions: the [Outbox Worker](../../../ext/outbox.md), for commands needing durable delivery.

Guides: None.
