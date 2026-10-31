# Persistence

## Intent

Commands change aggregates and queries read them, with neither touching the database directly: repositories stage writes, the read session serves queries, and one post-handler commits each command. This page states that boundary, the transaction shape, and the obligations on stored data. One explicit boundary keeps storage decisions reviewable in Infrastructure instead of scattered through handlers.

## Concepts

### The Storage Boundary

A command handler reaches persistence only through Domain repositories. It loads aggregates through their Domain repositories, calls Domain behavior, and stages the changed aggregates back through those repositories. Exactly one command post-handler then commits the scoped session. A query handler projects its result through the read session instead of loading an aggregate, with filtering, scope, ordering, and limit applied before materialization.

In the Claimwick product, `use-case/spending.submit-expense` stages the new expense through its repository, and one post-handler commits once after the handler returns. A failed submission leaves the session uncommitted, so no partial expense is stored.

### Consistency Modes

Each use-case record declares one consistency mode, and persistence honors it. `atomic` commits everything the handler staged in one unit of work. `per-record` lets a job run one command per record, and each command commits alone with the cursor resuming the rest. `compensated` commits each step in its own command, and a failure runs the compensation the step declares. Every mode still commits once per command pipeline; the mode decides how work splits across commands.

### Provider-Neutral and Provider-Specific Rules

The persistence provisions divide into two groups, so a project that replaces the provider knows which obligations move with it.

Provider-neutral provisions state the boundary, the transaction shape, and the obligations on stored data: writes go through Domain repositories, queries project through the read session, and exactly one post-handler commits each command. Each one states an obligation the provider does not change.

Provider-specific provisions name the member names and configuration surface the pinned provider supplies, such as the document alias, the JSON contract ownership, and the session registration. The [.NET persistence binding](dotnet/persistence.md) names the baseline provider and the calls that perform each obligation.

The EF Core extension states in its own Baseline relationship section which provisions it replaces for the aggregates it owns. Each aggregate runs under exactly one provider.

The manifest pins both providers' packages, because the extension is part of this release. A pinned package is an available choice rather than an active one, and `litespec.project.json` records which choices a consumer made.

## Standards

### Stage Aggregate Writes Through Domain Repositories

**ID:** `litespec/backend/persistence#stage-aggregate-writes-through-domain-repositories`

**Statement:** A command handler MUST reach persistence only through a Domain repository, never through the session directly.

**Rationale:** The repository is the only path from a handler to the store, so every write passes a reviewable boundary that loads one aggregate type. Infrastructure implements each repository with the scoped session, and the repository exposes no session, generic query, or save operation. A handler that reached the session directly bypasses aggregate loading and scatters store calls through Application.

**Checked by:** `litespec model check` reports `repository-unimplemented` (level S) for an aggregate a use case changes that has no Infrastructure repository implementing its domain repository.

### Query Through the Read Session

**ID:** `litespec/backend/persistence#query-through-the-read-session`

**Statement:** A query handler MUST project through the read session, applying filter, scope, ordering, and limit before materialization.

**Rationale:** The session already projects, filters, and pages, so a read-store interface per aggregate, a generic read repository, or a callback wrapper adds a layer that only repeats it. Applying filter, scope, ordering, and limit before materialization keeps that work in the database; materializing first loads rows the caller does not see. Ordering and paging follow `litespec/backend/api#bound-collection-queries`.

**Reviewed for:** Which query handlers project outside the read session, or apply filter, scope, ordering, or limit after materialization? Look at each query handler body.

### Commit Each Command Pipeline Once

**ID:** `litespec/backend/persistence#commit-each-command-pipeline-once`

**Statement:** Exactly one global command post-handler MUST commit the session for a command pipeline.

**Rationale:** One commit point makes the transaction boundary visible: everything the handler staged succeeds or fails together, and a failed command leaves the session uncommitted. If handlers or repositories committed, a later failure in the same pipeline leaves a partial write behind. Handlers, repositories, validators, reactions, jobs, and endpoints commit nothing. A declared consistency mode other than `atomic` splits work across commands; each command still commits once.

**Example:** In the Claimwick product, `use-case/spending.submit-expense` stages the expense through its Domain repository, and one post-handler commits once after the handler returns:

```text
handler stages the expense through its repository
post-handler collects the pending events
post-handler commits the session once
```

A failed submission leaves the session uncommitted, so no partial expense is stored. Handlers, repositories, validators, reactions, jobs, and endpoints commit nothing.

**Reviewed for:** Which pipelines commit outside one command post-handler, or which handlers, repositories, validators, reactions, jobs, or endpoints commit? Look at the commit calls in each pipeline.

### Honor the Declared Consistency Mode

**ID:** `litespec/backend/persistence#honor-the-declared-consistency-mode`

**Statement:** Persistence MUST commit a command exactly as its use-case record's `consistency` mode states.

**Rationale:** `atomic` commits everything the handler staged in one unit of work, citing the rule in `cites` when `changes` names more than one aggregate. `per-record` commits each record's command alone, with the job resuming from its cursor after a failure. `compensated` commits each step alone, and a failure runs the compensation the step declares. A second commit in one command splits one unit of work across two transactions, so the failure between them leaves a partial write.

**Example:** The erase-expired-data job commits each expense alone under `per-record`; the submit-expense command commits its staged aggregates together under `atomic`.

**Reviewed for:** Which commands commit outside their record's `consistency` mode? Look at each commit beside the record's mode.

### Collect Events Without a Public Unit of Work

**ID:** `litespec/backend/persistence#collect-events-without-a-public-unit-of-work`

**Statement:** A repository MUST register each touched aggregate with the internal scoped event buffer that the post-handler drains.

**Rationale:** Pending events leave the process exactly when their write commits, so the post-handler drains the buffer and commits in one place. A public unit-of-work type exposes that mechanics to every handler; the internal buffer keeps it inside the pipeline. Async delivery leaves after the commit succeeds.

**Reviewed for:** Which repositories skip the internal event buffer, or which event mechanics escape into a public type? Look at the repository staging calls beside the post-handler.

### Keep Mappings and Aliases Explicit

**ID:** `litespec/backend/persistence#keep-mappings-and-aliases-explicit`

**Statement:** Each stored aggregate MUST declare a stable document alias, typed-identifier mapping, and the indexes its accepted queries need.

**Rationale:** Document types and collection names are storage identity: if they followed code names, renaming a class orphans every stored document. An explicit alias, identifier mapping, and index list keeps storage identity independent of code names, with the store configuration owned in Infrastructure.

**Reviewed for:** Which stored aggregates lack a stable document alias, typed-identifier mapping, or index list? Look at the store configuration for each aggregate.

### Keep Serialization Behavior Out of Domain

**ID:** `litespec/backend/persistence#keep-serialization-behavior-out-of-domain`

**Statement:** Infrastructure MUST own the JSON contract for stored documents, including member names, constructors, converters, and polymorphic registration.

**Rationale:** Serialization metadata on Domain types couples the business model to one store's format, so changing the format means editing the rules. Infrastructure owns member names, constructors, converters, and polymorphic registration, and Domain stays free of serialization metadata. Every concrete type behind a base or interface is registered, because an unregistered subtype fails at read time.

Registration covers the types this store actually persists. The store that writes a union referenced from another aggregate registers it once, rather than every store that names the referencing type. The store persists no type reachable only through an identifier here, and it needs no registration.

**Reviewed for:** Which Domain types carry serialization metadata, or which stored JSON contracts escape Infrastructure? Look at the Domain types beside the Infrastructure JSON contract.

### Evolve Stored Document Contracts Explicitly

**ID:** `litespec/backend/persistence#evolve-stored-document-contracts-explicitly`

**Statement:** A rename, removal, type change, member move, or discriminator change MUST have a reviewed data transformation or an expand-and-contract rollout.

**Rationale:** Stored JSON member names, required values, and discriminator values are database schema: old documents stay on disk after the code changes. Without a reviewed transformation or an expand-and-contract rollout, a rename or type change makes old documents unreadable. An additive member still defines its behavior for documents written earlier.

An additive change is the only kind that needs no transformation, and it is additive only when every earlier document stays valid. A new member with a defined absent-value behavior qualifies. A new required member, a narrowed type, and a new discriminator value that an earlier reader rejects do not.

**Reviewed for:** Which renames, removals, type changes, member moves, or discriminator changes lack a reviewed transformation or an expand-and-contract rollout? Look at each stored-contract change.

### Bound Aggregate Document Growth

**ID:** `litespec/backend/persistence#bound-aggregate-document-growth`

**Statement:** An aggregate document MUST NOT embed a collection that has no accepted business bound.

**Rationale:** A document grows with every write to its collection, so an unbounded embedded collection eventually exceeds size limits and slows every load of its aggregate. Data without an accepted business bound belongs in another aggregate, an owned document record, or a separate query-shaped document, depending on the consistency boundary it needs.

**Reviewed for:** Which aggregate documents embed a collection with no accepted business bound? Look at the embedded collections in each document.

### Use Database Naming Conventions

**ID:** `litespec/backend/persistence#use-database-naming-conventions`

**Statement:** A PostgreSQL schema, table, column, index, constraint, document alias, or SQL identifier MUST use `snake_case`.

**Rationale:** One casing rule lets anyone predict a table or column name from the code type without opening the mapping. Code types keep normal language naming while the mapping layer converts once: under the baseline provider the store configuration sets the casing, and under the `efcore` extension a naming plugin applies it, so no configuration repeats the rule per entity.

**Reviewed for:** Which generated database identifiers break `snake_case`? Look at the schemas, tables, columns, and indexes.

### Control Production Schema Changes

**ID:** `litespec/backend/persistence#control-production-schema-changes`

**Statement:** A hosted environment MUST apply schema changes through a reviewed step before traffic shifts, not from a starting replica.

**Rationale:** A schema change that runs from a starting replica races every other starting replica, and the winner is nondeterministic. A reviewed step produces an artifact that is approved before it runs, and a deployment step applies that migration before traffic shifts. Development and disposable integration databases still apply schema automatically.

A tool that alters the schema from application startup is not a reviewed step whichever provider runs it.

**Reviewed for:** Which hosted schema changes run outside a reviewed step before traffic shifts? Look at the deployment steps beside the startup paths.

### Test Persistence Against PostgreSQL

**ID:** `litespec/backend/persistence#test-persistence-against-postgresql`

**Statement:** Repository behavior, projections, mappings, correctness indexes, commit behavior, and schema application MUST run against the pinned PostgreSQL.

**Rationale:** Only the pinned PostgreSQL runs the real query planner, constraints, and type behavior. An in-memory substitute proves the test's own model of the database, so a query that passes against it fails in production. HTTP integration through the real host is covered under `litespec/backend/testing#test-http-integration-through-the-real-application-host`.

**Reviewed for:** Which persistence tests run outside the pinned PostgreSQL, or against an in-memory substitute? Look at the persistence test fixtures.

### Read Collection Queries in the Store with Paging and Filters

**ID:** `litespec/backend/persistence#read-collection-queries-in-the-store-with-paging-and-filters`

**Statement:** Each collection query MUST page and filter in the store, never load a whole list to shape it in memory.

**Rationale:** A query that loads a whole list to filter, sort, or page it in memory pays the full read on every call and breaks when the data grows. Repository list methods take the scope and the page and filter in the database, through the shared page request and page shapes.

**Example:** The list-expenses query passes its employee scope and page to the repository, while a handler applying `Where` to a `ListAsync` result is reported.

**Checked by:** `litespec model check` reports `query-filter-invalid` (level A) for a query handler filtering a repository list in memory.

### Assign Document Numbers from a Sequence

**ID:** `litespec/backend/persistence#assign-document-numbers-from-a-sequence`

**Statement:** Each legal document number MUST allocate from a per-owner sequence taken in the command transaction.

**Rationale:** A number computed from a count plus one hands two concurrent issues the same legal number. A per-owner counter row taken in the command transaction allocates each number once, whatever races it.

**Example:** The reimburse-expense command takes its number from the spending payout sequence, never from a list count.

**Reviewed for:** Which legal document numbers allocate outside a per-owner sequence taken in the command transaction? Look at each number allocation.

### Enforce Uniqueness in the Store

**ID:** `litespec/backend/persistence#enforce-uniqueness-in-the-store`

**Statement:** Each uniqueness rule MUST be enforced by the store, through a unique index over the stored value or, for a record that exists at most once per owner, through an identity derived from its owner.

**Rationale:** The store is the only place that sees every concurrent write, while a check in code sees only its own transaction: two commands that both check and then both write store the same value twice. A unique index or an owner-derived identity makes the second write fail in the store instead. When the rule ignores case or spacing, the stored value is normalized before writing and the index covers the normalized field, so differently cased writes still collide.

**Example:** In the Claimwick product, each payout carries a legal document number that is unique per spending owner; the store holds a unique index over the owner and number pair, so two concurrent reimbursements collide on the index and the loser ends with the use case conflict refusal instead of sharing one number. In the library product, each book barcode is unique across books; two clerks registering the same barcode at once collide on the barcode unique index, and the second commit refuses with `book-barcode-taken`.

**Reviewed for:** Which uniqueness rules lack a store unique index or an owner-derived identity, or index a denormalized field? Look at the store configuration for each rule.

### Model a Once-Only Rule as Aggregate State

**ID:** `litespec/backend/persistence#model-a-once-only-rule-as-aggregate-state`

**Statement:** Each rule that an action happens at most once for a record MUST be state on that record's aggregate.

**Rationale:** A flag on the aggregate travels with the record it guards, so the rule is checked where the record is changed: the aggregate sees the earlier action in its own state and refuses the repeat or leaves it without effect. A counter or marker kept anywhere else splits the rule from the record, and the two disagree under concurrent writes.

**Example:** In the library product, a loan charges its overdue fee at most once: the loan records that it charged the fee, and a repeat command finds the flag and refuses with the loan conflict refusal. In the Claimwick product, an expense is reimbursed at most once: the reimbursed state carries the fact, so a second reimbursement finds an expense that is already reimbursed and ends with `spending.not-approved`.

**Reviewed for:** Which at-most-once rules live outside their aggregate's state, or which repeats escape the refusal? Look at each such rule beside its aggregate.

### Claim a Cross-Record Key at Commit

**ID:** `litespec/backend/persistence#claim-a-cross-record-key-at-commit`

**Statement:** A uniqueness rule that no single-record constraint can express MUST be declared by the aggregate as a key and claimed and released by persistence in the commit that changes the aggregate.

**Rationale:** Some rules fit no single-record constraint: a value held in a list inside the aggregate, or a value that is unique across two record kinds. For those, the aggregate declares its current keys through `UniquenessKeys` (see `litespec/backend/domain#declare-cross-record-keys-on-the-aggregate`), and persistence compares the keys at load with the keys at commit, inserting the added keys and deleting the removed ones in the same transaction, so the database still arbitrates the race.

**Example:** In the library product, a book holds a list of copy barcodes and each barcode belongs to one book: the book declares each barcode as a key, and the commit that adds a copy claims the new barcode while releasing a removed one. In the Claimwick product, a payout reference that is unique across payouts and adjustments is declared by each aggregate that carries it, so a reference already used by either record blocks the other at commit.

**Reviewed for:** Which cross-record uniqueness rules miss declared keys, or claim and release them outside the committing transaction? Look at each such rule beside its commit.

### Keep Uniqueness Out of Handlers

**ID:** `litespec/backend/persistence#keep-uniqueness-out-of-handlers`

**Statement:** A handler MUST NOT reserve, claim, check or release a uniqueness key.

**Rationale:** A reservation made in a handler runs outside the commit that needs it: it can leak when the command fails later, and it teaches every later handler to coordinate with the store directly. The store enforces the rule at commit through `litespec/backend/persistence#enforce-uniqueness-in-the-store` and `litespec/backend/persistence#claim-a-cross-record-key-at-commit`, and the pipeline turns a lost race into the use case conflict refusal under `litespec/backend/application#refuse-a-lost-race-as-a-conflict`, so the handler carries no reservation, check, or cleanup.

**Example:** The register-reader handler creates the reader aggregate and stages it through its Domain repository, reserving no card number and checking no registry first. When two registrations race for one card number, the second commit violates the unique index and refuses with the use case conflict, with no reservation or cleanup code in the handler. The submit-expense handler likewise stages the expense and leaves the payout number to the store sequence.

**Checked by:** `dotnet build` reports `LS2016` for a handler taking a pipeline-owned collaborator.

### Map a Constraint Violation to Its Refusal

**ID:** `litespec/backend/persistence#map-a-constraint-violation-to-its-refusal`

**Statement:** Each unique constraint MUST map by name to the conflict refusal of the use cases that can violate it.

**Rationale:** The pipeline fault classification turns a store fault into the use case named conflict refusal, following `litespec/backend/application#refuse-a-lost-race-as-a-conflict`. The map keys each unique constraint by name, so the classifier knows which refusal the violation answers. An unmapped violation still fails the commit, but it reaches the caller as a generic conflict that names no rule and suggests no next step. A classifier that builds the code from the lost key, one code per key scope, declares its template in the project file `refusals.composed`, so the model checks read each code as raised where its key scope is declared.

**Example:** The barcode unique index is named `uq_book_barcode`, and the map pairs it with `book-barcode-taken`: a registration that loses the barcode race refuses with that code. The payout number index is named `uq_payout_owner_number`, and the map pairs it with the reimburse conflict refusal, so each racing command names the number it lost.

**Reviewed for:** Which unique constraints lack a conflict refusal mapping for their violating use cases? Look at the fault classification map.

### Version Every Aggregate

**ID:** `litespec/backend/persistence#version-every-aggregate`

**Statement:** Each stored aggregate MUST carry a version that the store compares at commit, so a lost update fails the commit.

**Rationale:** Without a compared version, two commands that load the same aggregate both commit, and the second silently overwrites the first. The store compares the version each command loaded with the version it commits against, so a stale write fails the commit instead of erasing newer work. A failed comparison ends with the use case conflict refusal, following `litespec/backend/application#refuse-a-lost-race-as-a-conflict`.

**Example:** Two approvers open the same submitted expense; the first approval commits and moves the version forward, and the second approval fails its version comparison and refuses with the approve conflict instead of overwriting the first decision. In the library product, two clerks extending one loan race the same way, and the loser re-reads the loan before deciding again.

**Reviewed for:** Which stored aggregates lack a compared version at commit, or which stale writes escape the use case conflict refusal? Look at each stored aggregate beside its commit.

## Conventions

### Use One Persistence Folder

**ID:** `litespec/backend/persistence#use-one-persistence-folder`

**Default:** Place store configuration, repositories, commit behavior, and serialization under one Infrastructure persistence folder.

**Replacement:** A project names its persistence layout in its backend documentation.

**Rationale:** One persistence folder holds store configuration, repositories, commit behavior, and serialization, so every persistence type has one discoverable home. Scattered configuration hides the provider setup across the project.

**Example:**

```text
{ProjectName}.Infrastructure/
  Persistence/
    Store/
      StoreServiceRegistration
      StoreConfiguration
      Commands/
        CommitCommandPostHandler
      Events/
        DomainEventBuffer
      Posts/                        single aggregate whose name matches the module: files directly under the module
        PostRepository
        PostStoreConfiguration
      Customers/                    two aggregates: one folder per aggregate
        CustomerAccounts/
          CustomerAccountRepository
          CustomerAccountStoreConfiguration
        Consents/
          ConsentRepository
          ConsentStoreConfiguration
```

The module folders follow `litespec/backend/architecture#organize-every-layer-by-module-and-use-case`. A single-aggregate module stays flat when its aggregate root's plural name equals the module name. For example, `Post` stays flat under `Posts`. Otherwise, each aggregate takes its own folder.

The module folders follow the same module rule as every layer. Aggregate-specific configuration sits beside its aggregate, while session and commit plumbing sit under the store root.

**Reviewed for:** Which store configuration, repository, or commit files sit outside the one persistence folder? Look at the persistence files in Infrastructure.

### Use One Session per Command

**ID:** `litespec/backend/persistence#use-one-session-per-command`

**Default:** Resolve one session for the repositories and the commit handler in one command.

**Replacement:** A project names its session scope in its backend documentation.

**Rationale:** One session per command means the repositories and the commit handler stage and commit the same unit of work. Two sessions in one command commit separately, so a failure between them leaves a partial write behind.

**Reviewed for:** Which commands resolve more than one session across their repositories and commit handler? Look at the session wiring in each command.

### Review Query Plans for New Indexes

**ID:** `litespec/backend/persistence#review-query-plans-for-new-indexes`

**Default:** Add an index only from an accepted query or a measured operating need, and inspect its plan on representative data.

**Replacement:** A project names its index review in its backend documentation.

**Rationale:** Every index costs write throughput on each change to its table. An index added without reading its plan on representative data pays that cost for a read with no measured need, so each index starts from an accepted query or a measured operating need.

**Reviewed for:** Which new indexes lack an accepted query and a justifying plan? Look at each new index beside its query.

### Add Read Documents for Query-Shaped Data

**ID:** `litespec/backend/persistence#add-read-documents-for-query-shaped-data`

**Default:** Add a module-owned read document for repeated cross-aggregate composition, deep traversal, or a distorting index shape.

**Replacement:** A project names its read document shape in its backend documentation.

**Rationale:** Aggregate documents serve simple reads, but repeated cross-aggregate composition or deep traversal through them loads whole aggregates to show a few fields. A module-owned read document carries that shape directly, and its owning use case defines its consistency and rebuild behavior.

**Reviewed for:** Which read documents lack recorded consistency and rebuild behavior beside their owning use case? Look at each read document.

### Persist One Explicit State Object

**ID:** `litespec/backend/persistence#persist-one-explicit-state-object`

**Default:** Persist the aggregate state object itself rather than a flattened projection, round-tripping every state object through the configured serializer in integration tests.

**Replacement:** A project names its state storage shape in its backend documentation.

**Rationale:** The stored state object keeps the aggregate's lifecycle and its state-specific facts in one value, so a read materializes the same state the command stored. A flattened projection splits that value across members the store cannot version together, and a flag beside the object disagrees with it after the next transition.

**Example:** A stable discriminator property uses `$state` with `draft`, `published`, and `archived` values. The persisted state value contains its state-specific data:

```json
{
  "state": {
    "$state": "published",
    "publishedAt": "2026-07-16T10:30:00Z"
  }
}
```

The example registers every concrete aggregate state object in Infrastructure.

For example, a reimbursed expense in the Claimwick product stores its state the same way, with the `Reimbursed` case from `aggregate/spending.expense`:

```json
{
  "state": {
    "$state": "reimbursed",
    "reimbursedAt": "2026-07-16T10:30:00Z"
  }
}
```

The document stores the state object itself, with no flattened `isReimbursed` flag beside it. The `Submitted`, `Approved`, and `Refused` cases use the `submitted`, `approved`, and `refused` discriminators with their own data.

Adding a state discriminator breaks an older application version during a mixed-version deployment unless the release plan prevents it. The release plan either prevents the older version from reading the new state or introduces a compatible reader before commands persist that state.

**Reviewed for:** Which aggregate documents flatten their state object, or which states fail to round-trip through the serializer? Look at each stored state in the integration tests.

## Reference Example

This informative example demonstrates `litespec/backend/persistence#keep-serialization-behavior-out-of-domain`.

An `Order` document holds a `PaymentMethod` collection with `CardPayment` and `BankTransfer` values. Infrastructure registers stable `card` and `bank_transfer` discriminators through the JSON contract resolver. The Domain hierarchy carries no serialization metadata.

A `Post` document stores one `state` object. The published case uses the stable `published` discriminator and owns `publishedAt`. The document does not also store an `isPublished` flag or an aggregate-level `publishedAt` value.

Renaming `shippingAddress.postalCode` to `shippingAddress.postcode` uses a mixed-version reader and a reviewed data transformation before the old reader is removed. Renaming a property in code without that rollout is not compatible document evolution.

An accepted business rule caps `Order.Lines` at 200 entries inside the aggregate document. An unbounded status history uses a separate read document or projection instead of growing the `Order` document indefinitely.
