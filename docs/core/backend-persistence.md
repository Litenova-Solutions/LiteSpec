# Persistence

**Stack:** any

**Concern:** infrastructure

Commands change aggregates and queries read them, and neither touches the database directly. This page states that boundary, the transaction shape, and the obligations on stored data. It is for developers who write handlers, repositories, and store configuration. A second commit inside one command splits one unit of work across two transactions, and a failure between them leaves a partial write behind.

## Concepts

### The Storage Boundary

A command handler reaches persistence through Domain repositories alone. It loads aggregates through them, calls Domain behavior, and stages the changed aggregates back through them.

Exactly one command post-handler commits the command session. A query handler projects its result through the read session instead of loading an aggregate, applying filtering, scope, ordering, and limit before materialization.

In the Claimwick product, `use-case/spending.submit-expense` stages the new expense through its repository, and one post-handler commits once after the handler returns. A failed submission leaves the session uncommitted, so no partial expense is stored.

One store stays the default, configured in the product-level `Infrastructure/` folder. Each module keeps its mappings and repositories in its own `Infrastructure/` folders, beside the aggregates they store. One explicit boundary keeps storage decisions reviewable in Infrastructure instead of scattered through handlers.

### Consistency Modes

Each use-case record declares one consistency mode, and persistence commits exactly that way. Under `atomic`, everything the handler staged commits in one unit of work. With `guards`, the guarded read holds each guard to commit by a lock, a version check, or serializable isolation, and the stack page fixes which. Under `per-record`, a job runs one command per record. Each command commits alone, and the cursor resumes the rest after a failure. Each mode still commits once per command pipeline. The mode decides how work splits across commands, not how a command commits.

### Stored Data Obligations

Stored JSON member names, mandatory values, and discriminator values are database schema: old documents stay on disk after the code changes. A rename, removal, type change, member move, or discriminator change ships with a reviewed transformation or an expand-and-contract rollout. An additive member defines its behavior for documents written earlier, and counts as additive only when each earlier document stays valid.

Each stored aggregate carries a version the store compares at commit, so a stale write fails instead of silently overwriting newer work. Each uniqueness rule lives in the store: a unique index over the stored value, or an identity derived from its owner for a record existing at most once per owner. A check in code sees only its own transaction, while the store sees each concurrent write and arbitrates the race. When the rule ignores case or spacing, the stored value is normalized before writing and the index covers the normalized field.

A value shared by a list inside one aggregate, or unique across two record kinds, fits no single-record constraint. For those rules, the aggregate declares its current keys, and persistence compares the keys at load with the keys at commit, inserting the added keys and deleting the removed ones in the same transaction. Each unique constraint pairs by name with the conflict refusal of the use cases that break it, so the pipeline turns a lost race into the named refusal.

Document numbers for legal documents allocate from a per-owner sequence taken in the command transaction, so two concurrent issues share no number. A rule that an action happens at most once for a record lives as state on that record's aggregate, where the aggregate sees the earlier action in its own state and refuses the repeat.

Each aggregate document embeds only collections with an accepted business bound. Data with no accepted bound belongs in another aggregate, an owned document record, or a separate query-shaped document, depending on the consistency boundary it needs. Repeated cross-aggregate composition, deep traversal, or a distorting index shape belongs in a module-owned read document carrying that shape directly, with its owning use case defining consistency and rebuild behavior.

Serialization metadata lives on no Domain type: Infrastructure owns member names, constructors, converters, and polymorphic registration, with each stored concrete type registered once. One shared serializer configuration serves the document store, the outbox, and any scheduled-job store, so a typed identifier or money value round-trips the same everywhere. Tests for repository behavior, projections, mappings, correctness indexes, commit behavior, and schema application run against the pinned database, which alone runs the real query planner, constraints, and type behavior.

Indexes start from an accepted query or a measured operating need, with plans inspected on representative data: each index costs write throughput on each change to its table. Schema names use `snake_case` for schemas, tables, columns, indexes, constraints, document aliases, and SQL identifiers, with the store configuration setting the casing once. Schema changes in hosted environments apply through a reviewed step before traffic shifts, with no change applied from a starting replica; development and disposable integration databases still apply schema automatically.

### Where the Stack Binding Lives

The provisions below state the boundary, the transaction shape, and the obligations on stored data: writes travel through Domain repositories, queries project through the read session, and exactly one post-handler commits each command. The [.NET persistence binding](../packs/dotnet/persistence.md) names the members and configuration surface the pinned provider supplies. Each aggregate runs under exactly one provider.

The manifest pins the providers' packages, and `.litespec/project.yaml` records which choices a consumer took.

## Standards

### Stage Aggregate Writes Through Domain Repositories

**ID:** `litespec/core/backend-persistence#stage-aggregate-writes-through-domain-repositories`

**Strength:** required

**Applies to:** artifact: command-handler

**Statement:** A command handler MUST reach persistence only through a Domain repository, never through the session directly.

**Rationale:** The repository is the one reviewable path from a handler to the store, shaped around one aggregate type. Without it, handlers reach the session directly, bypass aggregate loading, and scatter store calls through Application. Infrastructure implements each repository with the command session. The repository exposes no session, generic query, or save operation.

**Example:** The submit-expense handler stages the new expense through the expense repository, without touching the session or issuing a store call of its own.

**Checked by:** `litespec model check` reports `repository-unimplemented` for an aggregate a use case changes that has no Infrastructure repository implementing its domain repository, and `code-placement-drift` for a repository implementation outside its aggregate's `Persistence/` folder.

### Query Through the Read Session

**ID:** `litespec/core/backend-persistence#query-through-the-read-session`

**Strength:** required

**Applies to:** artifact: query-handler

**Statement:** A query handler MUST project through the read session, applying filter, scope, ordering, and limit before materialization.

**Rationale:** The session already projects, filters, and pages. A read-store interface per aggregate, a generic read repository, or a callback wrapper adds a layer that only repeats it. Applying filter, scope, ordering, and limit before materialization keeps that work in the database. Materializing first loads rows the caller sees nowhere. Ordering and paging follow `litespec/core/backend-api#bound-collection-queries`.

**Reviewed for:** Is any query handler projecting outside the read session, or applying filter, scope, ordering, or limit after materialization? Look at each query handler body.

### Commit Each Command Pipeline Once

**ID:** `litespec/core/backend-persistence#commit-each-command-pipeline-once`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Exactly one global command post-handler MUST commit the session for a command pipeline.

**Rationale:** One commit point makes the transaction boundary visible. Everything the handler staged succeeds or fails together, and a failed command leaves the session uncommitted. When handlers or repositories commit, a later failure in the same pipeline leaves a partial write behind. Handlers, repositories, validators, reactions, jobs, and endpoints commit nothing. Each command runs as a top-level pipeline with its own commit, because no handler sends a command. A command uses no caller's commit. A declared consistency mode other than `atomic` splits work across commands. Each command still commits once.

**Example:** In the Claimwick product, `use-case/spending.submit-expense` stages the expense through its Domain repository, and one post-handler commits once after the handler returns:

```text
handler stages the expense through its repository
post-handler collects the pending events
post-handler commits the session once
```

A failed submission leaves the session uncommitted, so no partial expense is stored.

**Reviewed for:** Does any pipeline commit outside one command post-handler, or do any handlers, repositories, validators, reactions, jobs, or endpoints commit? Look at the commit calls in each pipeline.

### Evolve Stored Document Contracts Explicitly

**ID:** `litespec/core/backend-persistence#evolve-stored-document-contracts-explicitly`

**Strength:** required

**Applies to:** artifact: aggregate

**Statement:** A rename, removal, type change, member move, or discriminator change MUST have a reviewed data transformation or an expand-and-contract rollout.

**Rationale:** Stored JSON member names, mandatory values, and discriminator values are database schema. Old documents stay on disk after the code changes. Without a reviewed transformation or an expand-and-contract rollout, a rename or type change makes old documents unreadable. An additive member defines its behavior for documents written earlier.

An additive change is the only kind that needs no transformation. It counts as additive only when each earlier document stays valid. A new member with a defined absent-value behavior qualifies. A new mandatory member, a narrowed type, or a new discriminator value that an earlier reader rejects counts as breaking instead.

**Reviewed for:** Is any rename, removal, type change, member move, or discriminator change shipping without a reviewed transformation or an expand-and-contract rollout? Look at each stored-contract change.

### Control Production Schema Changes

**ID:** `litespec/core/backend-persistence#control-production-schema-changes`

**Strength:** required

**Applies to:** artifact: migration

**Statement:** A hosted environment MUST apply schema changes through a reviewed step before traffic shifts, not from a starting replica.

**Rationale:** A schema change that runs from a starting replica races each other starting replica. The winner is nondeterministic. A reviewed step produces the migration artifact instead. A deployment step applies that artifact before traffic shifts. Development and disposable integration databases still apply schema automatically.

A tool that alters the schema from application startup is no reviewed step, whichever provider runs it.

**Reviewed for:** Is any hosted schema change applied outside a reviewed step before traffic shifts, or from a starting replica? Look at the deployment steps beside the startup paths.

### Page and Filter Collection Queries in the Store

**ID:** `litespec/core/backend-persistence#page-and-filter-collection-queries-in-the-store`

**Strength:** required

**Applies to:** artifact: query-handler

**Statement:** Each collection query MUST page and filter in the store, never load a whole list to shape it in memory.

**Rationale:** A query that loads a whole list to filter, sort, or page it in memory pays the full read on each call. It breaks when the data grows. Repository list methods take the scope and the page and filter in the database instead, through the shared page request and page shapes.

**Example:** The list-expenses query passes its employee scope and page to the repository. A handler that filters a full list in memory is reported.

**Checked by:** `litespec model check` reports `query-filter-invalid` (level S) for a query handler filtering a repository list in memory.

### Assign Document Numbers from a Sequence

**ID:** `litespec/core/backend-persistence#assign-document-numbers-from-a-sequence`

**Strength:** required

**Applies to:** artifact: command-handler, repository; when: document-number

**Statement:** Each legal document number MUST allocate from a per-owner sequence taken in the command transaction.

**Rationale:** A number computed from a count plus one gives two concurrent issues the same legal number. A per-owner counter row taken in the command transaction allocates each number once, no matter what races it.

**Example:** The reimburse-expense command takes its number from the spending payout sequence, not from a list count.

**Reviewed for:** Is any legal document number allocated outside a per-owner sequence taken in the command transaction? Look at each number allocation.

### Enforce Uniqueness in the Store

**ID:** `litespec/core/backend-persistence#enforce-uniqueness-in-the-store`

**Strength:** required

**Applies to:** artifact: aggregate

**Statement:** Each uniqueness rule MUST be enforced by the store, through a unique index over the stored value or, for a record that exists at most once per owner, through an identity derived from its owner.

**Rationale:** The store is the one place that sees each concurrent write. A check in code sees only its own transaction. Two commands that both check and then both write store the same value twice. A unique index or an owner-derived identity makes the second write fail in the store instead. When the rule ignores case or spacing, the stored value is normalized before writing and the index covers the normalized field. Differently cased writes still collide.

**Example:** In the Claimwick product, each payout carries a legal document number that is unique per spending owner. The store holds a unique index over the owner and number pair, so two concurrent reimbursements collide on the index and the loser ends with the use case conflict refusal instead of sharing one number. In the library product, each book barcode is unique across books: two clerks registering the same barcode at once collide on the barcode unique index, and the second commit refuses with `book-barcode-taken`.

**Reviewed for:** Is any uniqueness rule missing a store unique index or an owner-derived identity, or indexing a denormalized field? Look at the store configuration for each rule.

### Keep Uniqueness Out of Handlers

**ID:** `litespec/core/backend-persistence#keep-uniqueness-out-of-handlers`

**Strength:** required

**Applies to:** artifact: command-handler

**Statement:** A handler MUST NOT reserve, claim, check or release a uniqueness key.

**Rationale:** A reservation made in a handler runs outside the commit that needs it. It leaks when the command fails later. It also teaches each later handler to coordinate with the store directly. The store enforces the rule at commit through `litespec/core/backend-persistence#enforce-uniqueness-in-the-store`. The pipeline turns a lost race into the use case conflict refusal under `litespec/core/backend-application#refuse-a-lost-race-as-a-conflict`, so the handler carries no reservation, check, or cleanup.

**Example:** The register-reader handler creates the reader aggregate and stages it through its Domain repository. It reserves no card number and checks no registry first. When two registrations race for one card number, the second commit violates the unique index and refuses with the use case conflict, with no reservation or cleanup code in the handler. The submit-expense handler likewise stages the expense and leaves the payout number to the store sequence.

**Reviewed for:** Is any handler reserving, claiming, checking, or releasing a uniqueness key? Look at each handler body.

### Version Every Aggregate

**ID:** `litespec/core/backend-persistence#version-every-aggregate`

**Strength:** required

**Applies to:** artifact: aggregate

**Statement:** Each stored aggregate MUST carry a version that the store compares at commit, so a lost update fails the commit.

**Rationale:** Without a compared version, two commands load the same aggregate and both commit. The second silently overwrites the first. The store compares the version each command loaded with the version it commits against. A stale write fails the commit instead of erasing newer work. A failed comparison ends with the use case conflict refusal, following `litespec/core/backend-application#refuse-a-lost-race-as-a-conflict`.

**Example:** Two approvers open the same submitted expense. The first approval commits and moves the version forward, and the second approval fails its version comparison and refuses with the approve conflict instead of overwriting the first decision. In the library product, two clerks extending one loan race the same way, and the loser re-reads the loan before deciding again.

**Reviewed for:** Is any stored aggregate committed without a compared version, or any stale write escaping the use case conflict refusal? Look at each stored aggregate beside its commit.

## Conventions

### Use One Persistence Folder

**ID:** `litespec/core/backend-persistence#use-one-persistence-folder`

**Strength:** default

**Applies to:** artifact: any

**Default:** Place store setup in the product `Infrastructure/` folder, each module's store setup in its own `Infrastructure/Persistence/` folder, and each aggregate's repository implementation, stored documents, and mapping in `Infrastructure/Persistence/{Aggregates}/`.

**Replacement:** A project names its persistence layout in its backend documentation.

**Rationale:** The product `Infrastructure/` folder holds store setup, session and commit plumbing, and serialization shared by each module. Each persistence type then has one discoverable home. Each module keeps its store setup in its own `Infrastructure/Persistence/` folder, and each aggregate's repository implementation, stored documents, and mapping in `Infrastructure/Persistence/{Aggregates}/`, so each layer nests by aggregate. Scattered configuration hides the provider setup across the product.

**Example:**

```text
{Product}/
  Infrastructure/                   store setup: registration, configuration, commit handler, event buffer
    StoreServiceRegistration
    StoreConfiguration
    Commands/
      CommitCommandPostHandler
    Events/
      DomainEventBuffer
  Posts/                            one aggregate: every layer holds the plural aggregate folder
    Infrastructure/
      Persistence/
        Posts/
          PostRepository
          PostStoreConfiguration
  Customers/                        two aggregates: one folder per aggregate
    Infrastructure/
      Persistence/
        CustomerAccounts/
          CustomerAccountRepository
          CustomerAccountStoreConfiguration
        Consents/
          ConsentRepository
          ConsentStoreConfiguration
```

The module folders follow `litespec/core/backend-architecture#organize-code-by-module-aggregate-and-use-case`: each module keeps its repositories in its `Infrastructure/Persistence/{Aggregates}/` folders.

Aggregate-specific configuration sits beside its aggregate's repository. Session and commit plumbing sit under the product store root.

**Checked by:** `litespec model check` reports `code-placement-drift` for a repository implementation outside its aggregate's `Persistence/` folder, for a use-case model or adapter file in `Persistence/`, and for any other file directly in `Persistence/` outside module-wide store setup.

### Use One Session per Command

**ID:** `litespec/core/backend-persistence#use-one-session-per-command`

**Strength:** default

**Applies to:** artifact: any

**Default:** Resolve one session for the repositories and the commit handler in one command.

**Replacement:** A project names its session scope in its backend documentation.

**Rationale:** One session per command means the repositories and the commit handler stage and commit the same unit of work. Two sessions in one command commit separately. A failure between them leaves a partial write behind.

**Reviewed for:** Is any command resolving more than one session across its repositories and commit handler? Look at the session wiring in each command.

### Add Read Documents for Query-Shaped Data

**ID:** `litespec/core/backend-persistence#add-read-documents-for-query-shaped-data`

**Strength:** default

**Applies to:** artifact: any

**Default:** Add a module-owned read document for repeated cross-aggregate composition, deep traversal, or a distorting index shape.

**Replacement:** A project names its read document shape in its backend documentation.

**Rationale:** Aggregate documents serve simple reads. Repeated cross-aggregate composition or deep traversal through them loads whole aggregates to show a few fields. A module-owned read document carries that shape directly. Its owning use case defines its consistency and rebuild behavior.

**Reviewed for:** Is any read document missing recorded consistency and rebuild behavior beside its owning use case? Look at each read document.

## Reference Example

This informative example demonstrates `litespec/core/backend-persistence#evolve-stored-document-contracts-explicitly`, `litespec/core/backend-persistence#version-every-aggregate`, and `litespec/core/backend-persistence#enforce-uniqueness-in-the-store`.

An `Order` document holds a `PaymentMethod` collection with `CardPayment` and `BankTransfer` values. Infrastructure registers the stable `card` and `bank_transfer` discriminators through the stored JSON contract. The Domain hierarchy carries no serialization metadata.

A `Post` document stores one `state` object, and the published case uses the stable `published` discriminator and owns `publishedAt`. No `isPublished` flag or aggregate-level `publishedAt` value sits beside it.

Renaming `shippingAddress.postalCode` to `shippingAddress.postcode` ships with a mixed-version reader and a reviewed data transformation, and the old reader is removed only after the rollout. A rename in code without that rollout is no compatible document evolution.

An accepted business rule caps `Order.Lines` at 200 entries inside the aggregate document. An unbounded status history goes in a separate read document or projection instead of growing the `Order` document indefinitely.
