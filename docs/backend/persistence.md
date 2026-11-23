# Persistence

**Stack:** any

**Concern:** infrastructure

Commands change aggregates and queries read them, and neither touches the database directly. This page states that boundary, the transaction shape, and the obligations on stored data.

## Concepts

### The Storage Boundary

A command handler reaches persistence only through Domain repositories. It loads aggregates through them, calls Domain behavior, and stages the changed aggregates back through them.

Exactly one command post-handler then commits the command session.

A query handler projects its result through the read session instead of loading an aggregate, applying filtering, scope, ordering, and limit before materialization.

In the Claimwick product, `use-case/spending.submit-expense` stages the new expense through its repository, and one post-handler commits once after the handler returns. A failed submission leaves the session uncommitted, so no partial expense is stored.

One store stays the default, configured in the product-level `Infrastructure/` folder. Each module keeps its mappings and repositories in its own `Infrastructure/` folders, beside the aggregates they store. One explicit boundary keeps storage decisions reviewable in Infrastructure instead of scattered through handlers.

### Consistency Modes

Each use-case record declares one consistency mode, and persistence commits exactly that way. Under `atomic`, everything the handler staged commits in one unit of work. With `guards`, the guarded read holds each guard to commit by a lock, a version check, or serializable isolation, and the stack page fixes which. Under `per-record`, a job runs one command per record. Each command commits alone, and the cursor resumes the rest after a failure. Every mode still commits once per command pipeline. The mode decides how work splits across commands, not how a command commits.

### Provider-Neutral and Provider-Specific Rules

The provisions below divide into two groups, so a project that replaces its provider knows which obligations move with it.

Provider-neutral provisions state the boundary, the transaction shape, and the obligations on stored data. Writes go through Domain repositories, queries project through the read session, and exactly one post-handler commits each command. The provider does not change any of these obligations.

Provider-specific provisions name the members and configuration surface the pinned provider supplies, such as the document alias, the JSON contract ownership, and the session registration. The [.NET persistence binding](dotnet/persistence.md) names the baseline provider and the calls that perform each obligation.

Each aggregate runs under exactly one provider.

The manifest pins both providers' packages, because the extension is part of this release. A pinned package is an available choice rather than an active one. `.litespec/project.yaml` records which choices a consumer made.

## Standards

### Stage Aggregate Writes Through Domain Repositories

**ID:** `litespec/backend/persistence#stage-aggregate-writes-through-domain-repositories`

**Strength:** required

**Applies to:** artifact: command-handler

**Statement:** A command handler MUST reach persistence only through a Domain repository, never through the session directly.

**Rationale:** The repository is the one reviewable path from a handler to the store, shaped around one aggregate type. Without it, handlers reach the session directly, bypass aggregate loading, and scatter store calls through Application. Infrastructure implements each repository with the command session. The repository exposes no session, generic query, or save operation.

**Example:** The submit-expense handler stages the new expense through the expense repository; it names no session and issues no store call of its own.

**Checked by:** `litespec model check` reports `repository-unimplemented` (level S) for an aggregate a use case changes that has no Infrastructure repository implementing its domain repository, and `code-placement-drift` for a repository implementation outside its aggregate's `Persistence/` folder.

### Query Through the Read Session

**ID:** `litespec/backend/persistence#query-through-the-read-session`

**Strength:** required

**Applies to:** artifact: query-handler

**Statement:** A query handler MUST project through the read session, applying filter, scope, ordering, and limit before materialization.

**Rationale:** The session already projects, filters, and pages. A read-store interface per aggregate, a generic read repository, or a callback wrapper adds a layer that only repeats it. Applying filter, scope, ordering, and limit before materialization keeps that work in the database. Materializing first loads rows the caller does not see. Ordering and paging follow `litespec/backend/api#bound-collection-queries`.

**Reviewed for:** Is any query handler projecting outside the read session, or applying filter, scope, ordering, or limit after materialization? Look at each query handler body.

### Commit Each Command Pipeline Once

**ID:** `litespec/backend/persistence#commit-each-command-pipeline-once`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Exactly one global command post-handler MUST commit the session for a command pipeline.

**Rationale:** One commit point makes the transaction boundary visible. Everything the handler staged succeeds or fails together, and a failed command leaves the session uncommitted. If handlers or repositories committed, a later failure in the same pipeline would leave a partial write behind. Handlers, repositories, validators, reactions, jobs, and endpoints commit nothing. Every command runs as a top-level pipeline with its own commit, because no handler sends a command. A command uses no caller's commit. A declared consistency mode other than `atomic` splits work across commands. Each command still commits once.

**Example:** In the Claimwick product, `use-case/spending.submit-expense` stages the expense through its Domain repository, and one post-handler commits once after the handler returns:

```text
handler stages the expense through its repository
post-handler collects the pending events
post-handler commits the session once
```

A failed submission leaves the session uncommitted, so no partial expense is stored.

**Reviewed for:** Does any pipeline commit outside one command post-handler, or do any handlers, repositories, validators, reactions, jobs, or endpoints commit? Look at the commit calls in each pipeline.

### Honor the Declared Consistency Mode

**ID:** `litespec/backend/persistence#honor-the-declared-consistency-mode`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Persistence MUST commit a command exactly as its use-case record's `consistency` mode states.

**Rationale:** Under `atomic`, everything the handler staged commits in one unit of work, citing the rule in `boundBy` and the decision in the rule's `basedOn` when `writes` names more than one aggregate. When `consistency` names `guards`, the guarded read holds each guard to commit by a lock, a version check, or serializable isolation, and the stack page fixes which. Under `per-record`, each record's command commits alone, and the job resumes from its cursor after a failure. A second commit inside one command splits one unit of work across two transactions. A failure between them leaves a partial write.

**Example:** The erase-expired-data job commits each expense alone under `per-record`, while the submit-expense command commits its staged aggregates together under `atomic`.

**Reviewed for:** Is any command committed in a shape its record's `consistency` mode does not declare? Look at each commit beside the record's mode.

### Collect Events Without a Public Unit of Work

**ID:** `litespec/backend/persistence#collect-events-without-a-public-unit-of-work`

**Strength:** required

**Applies to:** artifact: repository

**Statement:** A repository MUST register each touched aggregate with the internal event buffer that the post-handler drains.

**Rationale:** Pending events leave the process exactly when their write commits. The post-handler drains the buffer and commits in one place. A public unit-of-work type would expose that mechanism to every handler. The internal buffer keeps it inside the pipeline. Async delivery leaves after the commit succeeds.

**Reviewed for:** Is any touched aggregate missing from the internal event buffer, or any event mechanics exposed through a public type? Look at the repository staging calls beside the post-handler.

### Keep Mappings and Aliases Explicit

**ID:** `litespec/backend/persistence#keep-mappings-and-aliases-explicit`

**Strength:** required

**Applies to:** artifact: aggregate

**Statement:** Each stored aggregate MUST declare a stable document alias, typed-identifier mapping, and the indexes its accepted queries need.

**Rationale:** Document types and collection names are storage identity. If they followed code names, renaming a class would orphan every stored document. An explicit alias, identifier mapping, and index list keeps storage identity independent of code names. The store configuration owned in Infrastructure holds all three.

**Reviewed for:** Is any stored aggregate missing a stable document alias, a typed-identifier mapping, or the indexes its accepted queries need? Look at the store configuration for each aggregate.

### Keep Serialization Behavior Out of Domain

**ID:** `litespec/backend/persistence#keep-serialization-behavior-out-of-domain`

**Strength:** required

**Applies to:** artifact: aggregate

**Statement:** Infrastructure MUST own the JSON contract for stored documents, including member names, constructors, converters, and polymorphic registration.

**Rationale:** Serialization metadata on Domain types couples the business model to one store's format. Changing the format then means editing the rules. Infrastructure owns member names, constructors, converters, and polymorphic registration. Domain stays free of serialization metadata. Every concrete type behind a base or interface is registered, because an unregistered subtype fails at read time.

Registration covers the types this store actually persists. The store that writes a union referenced from another aggregate registers it once, instead of every store that names the referencing type. A type reachable only through an identifier is persisted by no store here, so it needs no registration.

**Reviewed for:** Is any serialization metadata sitting on a Domain type, or any stored JSON contract owned outside Infrastructure? Look at the Domain types beside the Infrastructure JSON contract.

### Evolve Stored Document Contracts Explicitly

**ID:** `litespec/backend/persistence#evolve-stored-document-contracts-explicitly`

**Strength:** required

**Applies to:** artifact: aggregate

**Statement:** A rename, removal, type change, member move, or discriminator change MUST have a reviewed data transformation or an expand-and-contract rollout.

**Rationale:** Stored JSON member names, required values, and discriminator values are database schema. Old documents stay on disk after the code changes. Without a reviewed transformation or an expand-and-contract rollout, a rename or type change makes old documents unreadable. An additive member still defines its behavior for documents written earlier.

An additive change is the only kind that needs no transformation. It counts as additive only when every earlier document stays valid. A new member with a defined absent-value behavior qualifies. A new required member, a narrowed type, or a new discriminator value that an earlier reader rejects does not.

**Reviewed for:** Is any rename, removal, type change, member move, or discriminator change shipping without a reviewed transformation or an expand-and-contract rollout? Look at each stored-contract change.

### Keep Unbounded Collections Out of Aggregate Documents

**ID:** `litespec/backend/persistence#keep-unbounded-collections-out-of-aggregate-documents`

**Strength:** required

**Applies to:** artifact: aggregate

**Statement:** An aggregate document MUST NOT embed a collection that has no accepted business bound.

**Rationale:** A document grows with every write to its collection. An unbounded embedded collection eventually exceeds size limits and slows every load of its aggregate. Data with no accepted business bound belongs in another aggregate, an owned document record, or a separate query-shaped document, depending on the consistency boundary it needs.

**Reviewed for:** Is any collection with no accepted business bound embedded in an aggregate document? Look at the embedded collections in each document.

### Use Database Naming Conventions

**ID:** `litespec/backend/persistence#use-database-naming-conventions`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A database schema, table, column, index, constraint, document alias, or SQL identifier MUST use `snake_case`.

**Rationale:** One casing rule lets anyone predict a table or column name from the code type without opening the mapping. Code types keep normal language naming while the mapping layer converts once. The store configuration sets the casing. No configuration repeats the rule per entity.

**Reviewed for:** Is any schema, table, column, index, constraint, document alias, or SQL identifier breaking `snake_case`? Look at the schemas, tables, columns, and indexes.

### Control Production Schema Changes

**ID:** `litespec/backend/persistence#control-production-schema-changes`

**Strength:** required

**Applies to:** artifact: migration

**Statement:** A hosted environment MUST apply schema changes through a reviewed step before traffic shifts, not from a starting replica.

**Rationale:** A schema change that runs from a starting replica races every other starting replica. The winner is nondeterministic. A reviewed step produces the migration artifact instead. A deployment step applies that artifact before traffic shifts. Development and disposable integration databases still apply schema automatically.

A tool that alters the schema from application startup is no reviewed step, whichever provider runs it.

**Reviewed for:** Is any hosted schema change applied outside a reviewed step before traffic shifts, or from a starting replica? Look at the deployment steps beside the startup paths.

### Test Persistence Against the Pinned Database

**ID:** `litespec/backend/persistence#test-persistence-against-the-pinned-database`

**Strength:** required

**Applies to:** artifact: test

**Statement:** Repository behavior, projections, mappings, correctness indexes, commit behavior, and schema application MUST run against the pinned database.

**Rationale:** Only the pinned database runs the real query planner, constraints, and type behavior. An in-memory substitute proves the test's own model of the database. A query that passes against it fails in production. HTTP integration through the real host is covered under `litespec/backend/testing#test-http-integration-through-the-real-application-host`.

**Reviewed for:** Is any repository behavior, projection, mapping, correctness index, commit behavior, or schema application tested outside the pinned database, or against an in-memory substitute? Look at the persistence test fixtures.

### Page and Filter Collection Queries in the Store

**ID:** `litespec/backend/persistence#page-and-filter-collection-queries-in-the-store`

**Strength:** required

**Applies to:** artifact: query-handler

**Statement:** Each collection query MUST page and filter in the store, never load a whole list to shape it in memory.

**Rationale:** A query that loads a whole list to filter, sort, or page it in memory pays the full read on every call. It breaks when the data grows. Repository list methods take the scope and the page and filter in the database instead, through the shared page request and page shapes.

**Example:** The list-expenses query passes its employee scope and page to the repository. A handler that filters a full list in memory is reported.

**Checked by:** `litespec model check` reports `query-filter-invalid` (level A) for a query handler filtering a repository list in memory.

### Assign Document Numbers from a Sequence

**ID:** `litespec/backend/persistence#assign-document-numbers-from-a-sequence`

**Strength:** required

**Applies to:** artifact: command-handler, repository; when: document-number

**Statement:** Each legal document number MUST allocate from a per-owner sequence taken in the command transaction.

**Rationale:** A number computed from a count plus one gives two concurrent issues the same legal number. A per-owner counter row taken in the command transaction allocates each number once, no matter what races it.

**Example:** The reimburse-expense command takes its number from the spending payout sequence, not from a list count.

**Reviewed for:** Is any legal document number allocated outside a per-owner sequence taken in the command transaction? Look at each number allocation.

### Enforce Uniqueness in the Store

**ID:** `litespec/backend/persistence#enforce-uniqueness-in-the-store`

**Strength:** required

**Applies to:** artifact: aggregate

**Statement:** Each uniqueness rule MUST be enforced by the store, through a unique index over the stored value or, for a record that exists at most once per owner, through an identity derived from its owner.

**Rationale:** The store is the only place that sees every concurrent write. A check in code sees only its own transaction. Two commands that both check and then both write store the same value twice. A unique index or an owner-derived identity makes the second write fail in the store instead. When the rule ignores case or spacing, the stored value is normalized before writing and the index covers the normalized field. Differently cased writes still collide.

**Example:** In the Claimwick product, each payout carries a legal document number that is unique per spending owner. The store holds a unique index over the owner and number pair, so two concurrent reimbursements collide on the index and the loser ends with the use case conflict refusal instead of sharing one number. In the library product, each book barcode is unique across books: two clerks registering the same barcode at once collide on the barcode unique index, and the second commit refuses with `book-barcode-taken`.

**Reviewed for:** Is any uniqueness rule missing a store unique index or an owner-derived identity, or indexing a denormalized field? Look at the store configuration for each rule.

### Model a Once-Only Rule as Aggregate State

**ID:** `litespec/backend/persistence#model-a-once-only-rule-as-aggregate-state`

**Strength:** required

**Applies to:** artifact: aggregate

**Statement:** Each rule that an action happens at most once for a record MUST be state on that record's aggregate.

**Rationale:** A flag on the aggregate travels with the record it guards. The rule is then checked where the record is changed. The aggregate sees the earlier action in its own state and refuses the repeat, or leaves it without effect. A counter or marker kept anywhere else splits the rule from the record. The two disagree under concurrent writes.

**Example:** In the library product, a loan charges its overdue fee at most once. The loan records that it charged the fee, and a repeat command finds the flag and refuses with the loan conflict refusal. In the Claimwick product, an expense is reimbursed at most once: the reimbursed state carries the fact, so a second reimbursement finds an already reimbursed expense and ends with `spending.not-approved`.

**Reviewed for:** Is any at-most-once rule held outside its aggregate's state, or any repeated action escaping the refusal? Look at each such rule beside its aggregate.

### Claim a Cross-Record Key at Commit

**ID:** `litespec/backend/persistence#claim-a-cross-record-key-at-commit`

**Strength:** required

**Applies to:** artifact: repository

**Statement:** A uniqueness rule that no single-record constraint can express MUST be declared by the aggregate as a key and claimed and released by persistence in the commit that changes the aggregate.

**Rationale:** Some rules fit no single-record constraint: a value held in a list inside the aggregate, or a value that is unique across two record kinds. A check in code sees only its own transaction. It passes two racing writers. For those rules, the aggregate declares its current keys through `UniquenessKeys` (see `litespec/backend/domain#declare-cross-record-keys-on-the-aggregate`). Persistence compares the keys at load with the keys at commit, inserting the added keys and deleting the removed ones in the same transaction. The database still arbitrates the race.

**Example:** In the library product, a book holds a list of copy barcodes and each barcode belongs to one book: the book declares each barcode as a key, and the commit that adds a copy claims the new barcode while releasing a removed one. In the Claimwick product, a payout reference that is unique across payouts and adjustments is declared by each aggregate that carries it, so a reference already used by either record blocks the other at commit.

**Reviewed for:** Is any cross-record uniqueness rule missing declared keys, or claiming and releasing them outside the committing transaction? Look at each such rule beside its commit.

### Keep Uniqueness Out of Handlers

**ID:** `litespec/backend/persistence#keep-uniqueness-out-of-handlers`

**Strength:** required

**Applies to:** artifact: command-handler

**Statement:** A handler MUST NOT reserve, claim, check or release a uniqueness key.

**Rationale:** A reservation made in a handler runs outside the commit that needs it. It can leak when the command fails later. It also teaches every later handler to coordinate with the store directly. The store enforces the rule at commit through `litespec/backend/persistence#enforce-uniqueness-in-the-store` and `litespec/backend/persistence#claim-a-cross-record-key-at-commit`. The pipeline turns a lost race into the use case conflict refusal under `litespec/backend/application#refuse-a-lost-race-as-a-conflict`, so the handler carries no reservation, check, or cleanup.

**Example:** The register-reader handler creates the reader aggregate and stages it through its Domain repository. It reserves no card number and checks no registry first. When two registrations race for one card number, the second commit violates the unique index and refuses with the use case conflict, with no reservation or cleanup code in the handler. The submit-expense handler likewise stages the expense and leaves the payout number to the store sequence.

**Reviewed for:** Is any handler reserving, claiming, checking, or releasing a uniqueness key? Look at each handler body.

### Map a Constraint Violation to Its Refusal

**ID:** `litespec/backend/persistence#map-a-constraint-violation-to-its-refusal`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Each unique constraint MUST map by name to the conflict refusal of the use cases that can violate it.

**Rationale:** When the store refuses a commit, pipeline fault classification turns that fault into the use case named conflict refusal, following `litespec/backend/application#refuse-a-lost-race-as-a-conflict`. The map keys each unique constraint by name. The classifier then knows which refusal the violation answers. An unmapped violation still fails the commit, but it reaches the caller as a generic conflict that names no rule and suggests no next step. A classifier that builds the code from the lost key, one code per key scope, declares its template in the project file `refusals.composed`. The model checks then read each code as raised where its key scope is declared.

**Example:** The barcode unique index is named `uq_book_barcode`, and the map pairs it with `book-barcode-taken`, so a registration that loses the barcode race refuses with that code. The payout number index is named `uq_payout_owner_number`, and the map pairs it with the reimburse conflict refusal, so each racing command names the number it lost.

**Reviewed for:** Is any unique constraint missing a conflict refusal mapping for the use cases that can violate it? Look at the fault classification map.

### Version Every Aggregate

**ID:** `litespec/backend/persistence#version-every-aggregate`

**Strength:** required

**Applies to:** artifact: aggregate

**Statement:** Each stored aggregate MUST carry a version that the store compares at commit, so a lost update fails the commit.

**Rationale:** Without a compared version, two commands can load the same aggregate and both commit. The second silently overwrites the first. The store compares the version each command loaded with the version it commits against. A stale write fails the commit instead of erasing newer work. A failed comparison ends with the use case conflict refusal, following `litespec/backend/application#refuse-a-lost-race-as-a-conflict`.

**Example:** Two approvers open the same submitted expense. The first approval commits and moves the version forward, and the second approval fails its version comparison and refuses with the approve conflict instead of overwriting the first decision. In the library product, two clerks extending one loan race the same way, and the loser re-reads the loan before deciding again.

**Reviewed for:** Is any stored aggregate committed without a compared version, or any stale write escaping the use case conflict refusal? Look at each stored aggregate beside its commit.

### Serialize Every Persisted Payload with One Configuration

**ID:** `litespec/backend/persistence#serialize-every-persisted-payload-with-one-configuration`

**Strength:** required

**Applies to:** artifact: any

**Statement:** The document store, the outbox, and any scheduled-job store MUST serialize every persisted payload through one shared serializer configuration.

**Rationale:** A typed identifier or money value that serializes one way in the document store and another way in the outbox arrives in a different shape than the store wrote. Two configurations drift with the first converter added to one and not the other: an identifier that survives the store arrives empty through the outbox. One shared configuration with the same converters keeps every persisted payload round-tripping the same everywhere.

**Example:** In the Claimwick product, the expense document, the expense-reimbursed outbox envelope, and the erase-expired-data job payload each carry the expense identifier and money amounts. All three serialize through the one product serializer configuration, so the identifier and the amounts read back identically from each store.

**Reviewed for:** Does any persisted payload serialize outside the one shared configuration, or any store carry converters the others lack? Look at the document store, the outbox store, and the scheduled-job store beside the shared configuration.

## Conventions

### Use One Persistence Folder

**ID:** `litespec/backend/persistence#use-one-persistence-folder`

**Strength:** default

**Applies to:** artifact: any

**Default:** Place store setup in the product `Infrastructure/` folder, each module's store setup in its own `Infrastructure/Persistence/` folder, and each aggregate's repository implementation, stored documents, and mapping in `Infrastructure/Persistence/{Aggregates}/`.

**Replacement:** A project names its persistence layout in its backend documentation.

**Rationale:** The product `Infrastructure/` folder holds store setup, session and commit plumbing, and serialization shared by every module. Every persistence type then has one discoverable home. Each module keeps its store setup in its own `Infrastructure/Persistence/` folder, and each aggregate's repository implementation, stored documents, and mapping in `Infrastructure/Persistence/{Aggregates}/`, so every layer nests by aggregate. Scattered configuration hides the provider setup across the product.

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

The module folders follow `litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case`: each module keeps its repositories in its `Infrastructure/Persistence/{Aggregates}/` folders.

Aggregate-specific configuration sits beside its aggregate's repository. Session and commit plumbing sit under the product store root.

**Checked by:** `litespec model check` reports `code-placement-drift` for a repository implementation outside its aggregate's `Persistence/` folder, for a use-case model or adapter file in `Persistence/`, and for any other file directly in `Persistence/` outside module-wide store setup.

### Use One Session per Command

**ID:** `litespec/backend/persistence#use-one-session-per-command`

**Strength:** default

**Applies to:** artifact: any

**Default:** Resolve one session for the repositories and the commit handler in one command.

**Replacement:** A project names its session scope in its backend documentation.

**Rationale:** One session per command means the repositories and the commit handler stage and commit the same unit of work. Two sessions in one command commit separately. A failure between them leaves a partial write behind.

**Reviewed for:** Is any command resolving more than one session across its repositories and commit handler? Look at the session wiring in each command.

### Review Query Plans for New Indexes

**ID:** `litespec/backend/persistence#review-query-plans-for-new-indexes`

**Strength:** advisory

**Applies to:** artifact: any

**Default:** Add an index only from an accepted query or a measured operating need, and inspect its plan on representative data.

**Replacement:** A project names its index review in its backend documentation.

**Rationale:** Every index costs write throughput on each change to its table. An index added without reading its plan on representative data pays that cost for a read with no measured need. So each index starts from an accepted query or a measured operating need.

**Reviewed for:** Is any new index missing an accepted query or a justifying plan on representative data? Look at each new index beside its query.

### Add Read Documents for Query-Shaped Data

**ID:** `litespec/backend/persistence#add-read-documents-for-query-shaped-data`

**Strength:** default

**Applies to:** artifact: any

**Default:** Add a module-owned read document for repeated cross-aggregate composition, deep traversal, or a distorting index shape.

**Replacement:** A project names its read document shape in its backend documentation.

**Rationale:** Aggregate documents serve simple reads. Repeated cross-aggregate composition or deep traversal through them loads whole aggregates to show a few fields. A module-owned read document carries that shape directly. Its owning use case defines its consistency and rebuild behavior.

**Reviewed for:** Is any read document missing recorded consistency and rebuild behavior beside its owning use case? Look at each read document.

### Persist One Explicit State Object

**ID:** `litespec/backend/persistence#persist-one-explicit-state-object`

**Strength:** default

**Applies to:** artifact: state-record

**Default:** Persist the aggregate state object itself rather than a flattened projection, round-tripping every state object through the configured serializer in integration tests.

**Replacement:** A project names its state storage shape in its backend documentation.

**Rationale:** The stored state object keeps the aggregate's lifecycle and its state-specific facts in one value. A read then materializes the same state the command stored. A flattened projection splits that value across members the store cannot version together. A flag beside the object disagrees with it after the next transition.

**Example:** A stable discriminator property uses `$state` with `draft`, `published`, and `archived` values. The persisted state value carries its state-specific data:

```json
{
  "state": {
    "$state": "published",
    "publishedAt": "2026-07-16T10:30:00Z"
  }
}
```

Every concrete aggregate state object is registered in Infrastructure. A reimbursed expense in the Claimwick product stores its state the same way, with the `Reimbursed` case from `aggregate/spending.expense`:

```json
{
  "state": {
    "$state": "reimbursed",
    "reimbursedAt": "2026-07-16T10:30:00Z"
  }
}
```

The document stores the state object itself, with no flattened `isReimbursed` flag beside it. The `Submitted`, `Approved`, and `Refused` cases use the `submitted`, `approved`, and `refused` discriminators, each with its own data.

A new state discriminator breaks an older application version during a mixed-version deployment unless the release plan prevents it. The plan either keeps the older version from reading the new state or introduces a compatible reader before commands persist that state.

**Reviewed for:** Is any aggregate document flattening its state object, or any state failing to round-trip through the serializer? Look at each stored state in the integration tests.

## Reference Example

This informative example demonstrates `litespec/backend/persistence#keep-serialization-behavior-out-of-domain`.

An `Order` document holds a `PaymentMethod` collection with `CardPayment` and `BankTransfer` values. Infrastructure registers the stable `card` and `bank_transfer` discriminators through the stored JSON contract. The Domain hierarchy carries no serialization metadata.

A `Post` document stores one `state` object, and the published case uses the stable `published` discriminator and owns `publishedAt`. No `isPublished` flag or aggregate-level `publishedAt` value sits beside it.

Renaming `shippingAddress.postalCode` to `shippingAddress.postcode` ships with a mixed-version reader and a reviewed data transformation, and the old reader is removed only after the rollout. A rename in code without that rollout is not compatible document evolution.

An accepted business rule caps `Order.Lines` at 200 entries inside the aggregate document. An unbounded status history goes in a separate read document or projection rather than growing the `Order` document indefinitely.
