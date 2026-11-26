# .NET Persistence

**Stack:** dotnet

**Concern:** infrastructure

This page defines how the shared [Backend Persistence](../../core/backend-persistence.md) rules apply to a .NET backend that stores through Marten, the baseline document store. It covers repositories, the read session, the single commit, and the Infrastructure folder layout and session lifetime they assume.

## Concepts

### Marten as the Baseline Provider

Marten is the baseline provider: it supplies the session, the read session, and document storage. The provisions on this page name Marten types because the baseline is Marten. Backends that store through another provider follow the provider-neutral rules on [Backend Persistence](../../core/backend-persistence.md), which these provisions bind for .NET.

### How a Command Reaches the Store

Every command runs inside one scoped session: a Marten `IDocumentSession` that the host resolves once per WebApi request or Worker operation. The handler loads and stages aggregates through Domain repositories, the interfaces Domain owns for storing and loading each aggregate. The stored document is the aggregate root itself: Marten persists each root as its own document, and the project writes no parallel document type for an aggregate.

Exactly one command post-handler commits the session with `SaveChangesAsync`. Only that post-handler commits, so a failed command leaves nothing stored.

The declared consistency mode decides how work splits across commands, but each command still commits once. Under `consistency: atomic` the one commit covers everything the handler staged, and other modes split work across commands that each commit once.

Events leave through the LiteBus outbox in process: the staging step commits each envelope with the business work, and the outbox processor delivers module events and domain-event reactions from the product's PostgreSQL database, retrying a failed handler with the configured backoff and dead-lettering at the bound. No broker is needed for anything inside the bounded context.

### How a Query Reads

A query handler never loads aggregates. It queries the aggregate type through the read session, `IQuerySession`, which filters, orders, pages, and shapes records. The handler applies filter, scope, ordering, and limit before materialization.

### How the Schema and Stored Documents Change

A release changes two different things in the store: the schema objects (tables, columns, and indexes) and the JSON inside documents already on disk. Schema objects are rebuilt or altered by statements against the database. Document JSON is not, because a code change does not reshape the bytes a previous version wrote. Treating the two as one problem either drops data or leaves old documents unreadable.

Marten keeps the two separate. `IDocumentStore.Storage.CreateMigrationAsync()` computes the schema difference between the configured model and the database without applying it, and `ToDatabaseScript()` returns the full creation script. `ApplyAllConfiguredChangesToDatabaseAsync(AutoCreate.CreateOrUpdate)` applies the difference, adding and altering objects to match the model. `AutoCreate.All` goes further: it drops and recreates tables that do not match the model, so it must never run against a database that holds data. `AssertDatabaseMatchesConfigurationAsync()` reports drift between the model and the database without changing either. Weasel, the schema tool behind Marten, also has a command line with `db-apply`, `db-assert`, `db-patch`, `db-dump`, and `db-list` for the same jobs outside application code.

For documents, Marten upcasts only events, never documents. An old document keeps the JSON an earlier version wrote until the code that reads it upgrades that JSON in memory. The project registers an upgrade step for each stored contract version; a read runs the steps on the JSON before deserializing, and the stored form is rewritten only when the aggregate is saved again. This protects an append-only record or a hash-chained audit record, whose stored form must never change: the upgrade shapes the in-memory object on read, and no step rewrites the bytes on disk.

## Standards

### Stage Aggregate Writes Through Repositories

**ID:** `litespec/packs/dotnet/persistence#stage-aggregate-writes-through-repositories`

**Strength:** required

**Applies to:** artifact: command-handler

**Statement:** A command handler MUST reach persistence only through a Domain repository, never through `IDocumentSession`.

**Rationale:** Infrastructure implements each Domain repository with the scoped session, so the handler stages work without naming a provider type. A repository that exposed the session, a generic query, or a save operation would hand the handler the provider surface the layer exists to hide. This provision binds `litespec/core/backend-persistence#stage-aggregate-writes-through-domain-repositories` for .NET.

**Checked by:** `dotnet build` reports `LS2004` for a command handler writing through a document session or database context.

### Store Each Aggregate Root as Its Own Document

**ID:** `litespec/packs/dotnet/persistence#store-each-aggregate-root-as-its-own-document`

**Strength:** required

**Applies to:** artifact: aggregate

**Statement:** Marten MUST store each aggregate root as its own document, with no parallel document type for an aggregate.

**Rationale:** A parallel document type duplicates the aggregate shape in Infrastructure, forces mapping code between the two, and splits reads between two models of one thing. Storing the root keeps one shape from the handler to the disk. Infrastructure owns the JSON contract that makes it work: the store serializer handles non-public members and constructors, one registered discriminator per closed-set case, and converters for values such as `Money` and typed identifiers, while Domain carries no serialization attribute. Query handlers query the aggregate type through `IQuerySession`. This provision covers the stored aggregate; a query-shaped read document under `litespec/core/backend-persistence#add-read-documents-for-query-shaped-data` serves queries and is no parallel aggregate document. This provision binds `litespec/core/backend-persistence#keep-serialization-behavior-out-of-domain` for .NET. Settled by [decision/0105-store-each-aggregate-root-as-its-own-document](decision/0105-store-each-aggregate-root-as-its-own-document).

**Example:** The product `Infrastructure/` store setup registers the expense aggregate with its stable alias and typed-identifier mapping, and a serializer carrying the converters and discriminator registrations:

```csharp
options.Schema.For<Expense>().Identity(x => x.Id);
options.Serializer(new ClaimwickSerializer());
```

```csharp
internal sealed class ClaimwickSerializer : MartenSerializer
{
    public override JsonSerializerOptions JsonSerializerOptions { get; } = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new MoneyJsonConverter());
        options.Converters.Add(new StronglyTypedIdJsonConverterFactory());
        options.TypeInfoResolver = JsonTypeInfoResolver.Combine(
            ExpensePolymorphism.Resolver,
            new DefaultJsonTypeInfoResolver());
        return options;
    }
}
```

The resolver registers one discriminator per closed-set case, the converters read and write `Money` and typed identifiers, and a modifier on the same resolver exposes the non-public members and constructors the aggregate shape needs. Domain carries no serialization attribute.

**Reviewed for:** Is any aggregate stored through a parallel document type instead of as its own document? Look at the stored document types beside the aggregates.

### Keep Pipeline-Owned Collaborators Out of Handlers

**ID:** `litespec/packs/dotnet/persistence#keep-pipeline-owned-collaborators-out-of-handlers`

**Strength:** required

**Applies to:** artifact: command-handler, query-handler

**Statement:** A command or query handler MUST NOT take a pipeline-owned collaborator, such as `IUnitOfWork`, `IIdempotencyStore`, or `ITransactionalOutbox`, through its constructor.

**Rationale:** The pipeline owns the unit of work, the idempotency store, the outbox, and the uniqueness claim service. A handler that takes one of them commits or reserves work outside the post-handler that owns the commit. The analyzer's default list names the LiteBus types, and a project adds its own claim service to the `litespec.LS2016.pipeline_owned_types` key. This provision binds `litespec/core/backend-persistence#keep-uniqueness-out-of-handlers` for .NET.

**Example:** The submit-expense handler takes its Domain repository and no unit of work, so the commit stays in the one post-handler. A handler constructor that adds `IUnitOfWork` is reported at the parameter.

**Checked by:** `dotnet build` reports `LS2016` for a command or query handler whose constructor takes a pipeline-owned type.

### Query Through IQuerySession

**ID:** `litespec/packs/dotnet/persistence#query-through-iquerysession`

**Strength:** required

**Applies to:** artifact: query-handler

**Statement:** A query handler MUST project through `IQuerySession`, applying filter, scope, ordering, and limit before materialization.

**Rationale:** The read session already filters, orders, pages, and projects. A read-store interface per aggregate, a generic read repository, or a callback wrapper adds a layer that repeats it. Ordering and paging follow `litespec/core/backend-api#bound-collection-queries`. This provision binds `litespec/core/backend-persistence#query-through-the-read-session` for .NET.

Under the Marten baseline the query surface is `IQuerySession`, and the obligation to project with filter, scope, ordering, and limit holds.

A table the project keeps beside its documents, such as a counter or an append-only ledger written through plain SQL, is read through the same session. Under Marten, `AdvancedSql.QueryAsync<TRow>` returns any JSON-serializable type. The SQL selects one JSON object per row, keyed in the casing the store's serializer uses, with the filter, ordering, and limit in its `WHERE`, `ORDER BY`, and `LIMIT`. The handler keeps no repository for such a table, and the project adds no read interface for it.

**Example:** In the Claimwick product, each spending owner's running totals live in a `spending_totals` table that a command updates in place. The read-spending-totals query reads it through the session:

```csharp
internal sealed record SpendingTotalRow(Guid OwnerId, string Currency, decimal Submitted, decimal Approved);

var rows = await session.AdvancedSql.QueryAsync<SpendingTotalRow>(
    """
    select jsonb_build_object(
        'ownerId', owner_id, 'currency', currency, 'submitted', submitted, 'approved', approved)
    from spending_totals
    where owner_id = ?
    order by currency
    limit 100
    """,
    cancellationToken,
    query.OwnerId.Value);
```

**Reviewed for:** Does every query handler project through `IQuerySession`, with filter, scope, ordering, and limit applied before materialization? Look at each query handler file.

### Commit Once in the Command Pipeline

**ID:** `litespec/packs/dotnet/persistence#commit-once-in-the-command-pipeline`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Exactly one global command post-handler MUST call `SaveChangesAsync` for a command pipeline.

**Rationale:** When exactly one post-handler commits, a failed command leaves the scoped session uncommitted and no partial work persists. A second commit, in a handler, repository, validator, reaction, job, or endpoint, would store work the pipeline later refuses. This provision binds `litespec/core/backend-persistence#commit-each-command-pipeline-once` for .NET.

The obligation is one commit per pipeline, not one named method. Under the baseline provider that commit is `IDocumentSession.SaveChangesAsync`. Under `consistency: atomic` it covers everything the handler staged. Other modes split work across commands. Each command still commits once, and the job resumes from its cursor or runs the declared compensation.

**Reviewed for:** Does exactly one post-handler file call `SaveChangesAsync` in each pipeline? Look at the `SaveChangesAsync` calls in each pipeline.

### Enforce Uniqueness with Marten Unique Indexes

**ID:** `litespec/packs/dotnet/persistence#enforce-uniqueness-with-marten-unique-indexes`

**Strength:** required

**Applies to:** artifact: aggregate

**Statement:** Each Marten aggregate document MUST enforce its uniqueness rules through a unique index over the stored value or an identity derived from its owner.

**Rationale:** A unique index makes the store arbitrate the race at commit, where every concurrent write is visible. The index is declared with `options.Schema.For<T>().UniqueIndex`, or as a computed index carrying `IsUnique`. The index name follows `litespec/core/backend-persistence#use-database-naming-conventions`. A record that exists at most once per owner takes a document id equal to the owner id instead, so a second record for the same owner collides on identity. This provision binds `litespec/core/backend-persistence#enforce-uniqueness-in-the-store` for .NET.

**Example:** The payout number is unique per spending owner:

```csharp
options.Schema.For<Payout>().UniqueIndex(
    UniqueIndexType.Computed,
    "uq_payout_owner_number",
    x => x.PayoutNumber);
```

A payout exists at most once per expense, so its document id is the expense id:

```csharp
// The payout document id equals the expense id: one payout per expense.
var payout = Payout.Issue(expense.Id, payoutNumber);
session.Store(payout);
```

**Reviewed for:** Does every Marten aggregate document with a uniqueness rule carry a unique index or an owner-derived identity? Look at the store configuration.

### Claim Cross-Record Keys with a Claims Document

**ID:** `litespec/packs/dotnet/persistence#claim-cross-record-keys-with-a-claims-document`

**Strength:** required

**Applies to:** artifact: repository

**Statement:** Each cross-record uniqueness key MUST be claimed through a claims document keyed by the key text and written with `session.Insert`, never with `Store`.

**Rationale:** Some keys admit no index: Marten cannot index elements of a list inside a document, cannot enforce uniqueness across document types, and computed indexes do not work on `DateTime` or `DateTimeOffset` members. Those keys are claimed through a small document whose id is the key text, so the store still arbitrates the race. The write uses `Insert`, which fails when the key document already exists. `Store` upserts, so it would overwrite a competing claim instead of colliding with it. The repository compares the aggregate keys at load with the keys at commit, and inserts the added key documents and deletes the removed ones in the same transaction. This provision binds `litespec/core/backend-persistence#claim-a-cross-record-key-at-commit` for .NET.

**Example:** In the library product, each copy barcode belongs to one book, while a book holds its barcodes in a list:

```csharp
public sealed record BarcodeClaim(string Barcode);
```

```csharp
// The claim document id is the barcode text; Insert fails on a duplicate.
session.Insert(new BarcodeClaim(barcode));
```

**Reviewed for:** Is every cross-record key claimed through a claims document written with `session.Insert`, never with `Store`? Look at each key claim.

### Map Constraint Violations with the PostgreSQL Refusal Classifier

**ID:** `litespec/packs/dotnet/persistence#map-constraint-violations-with-the-postgresql-refusal-classifier`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Each Marten unique index MUST map by name to its conflict refusal code in `PostgreSqlConstraintRefusalClassifier`.

**Rationale:** The `LiteBus.Storage.PostgreSql.PostgreSqlConstraintRefusalClassifier` carries the map from index name to refusal code and is registered through `UseFaultRefusalClassifierInstance`. The pipeline fault classification then turns a unique violation into the use case named conflict refusal under `litespec/core/backend-application#refuse-a-lost-race-as-a-conflict`. This provision binds `litespec/core/backend-persistence#map-a-constraint-violation-to-its-refusal` for .NET.

**Example:** The barcode and payout number indexes map to their refusals:

```csharp
services.UseFaultRefusalClassifierInstance(
    new PostgreSqlConstraintRefusalClassifier(new Dictionary<string, string>
    {
        ["uq_book_barcode"] = "book-barcode-taken",
        ["uq_payout_owner_number"] = "spending.payout-number-taken",
    }));
```

**Reviewed for:** Does every unique index name map to its conflict refusal code in the PostgreSQL refusal classifier? Look at the service registration.

### Apply Optimistic Concurrency to Every Aggregate Document

**ID:** `litespec/packs/dotnet/persistence#apply-optimistic-concurrency-to-every-aggregate-document`

**Strength:** required

**Applies to:** artifact: aggregate

**Statement:** Each Marten aggregate document type MUST enable optimistic concurrency.

**Rationale:** The store compares the version each command loaded with the version it commits against, so a stale write fails the commit instead of silently overwriting newer work. Each aggregate document type enables it with `UseOptimisticConcurrency(true)`, or one `Policies.ForAllDocuments` policy covers every aggregate document type at once. No aggregate document type is left without it. This provision binds `litespec/core/backend-persistence#version-every-aggregate` for .NET.

**Example:** The expense document enables the check on its own type:

```csharp
options.Schema.For<Expense>().UseOptimisticConcurrency(true);
```

**Reviewed for:** Does every Marten aggregate document type enable optimistic concurrency? Look at each document type and the document policy.

### Map Marten Concurrency Faults to Conflict Refusals

**ID:** `litespec/packs/dotnet/persistence#map-marten-concurrency-faults-to-conflict-refusals`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A classifier MUST translate Marten's `ConcurrencyException` into the use case conflict refusal.

**Rationale:** A `ConcurrencyException` names a provider type that Application does not expose. The registered classifier translates it into the use case named conflict refusal, following `litespec/core/backend-application#refuse-a-lost-race-as-a-conflict`. A version race and a uniqueness race then reach the caller through the same refusal path. This provision binds `litespec/core/backend-application#refuse-a-lost-race-as-a-conflict` for .NET.

**Example:** Two approvers race on one submitted expense; the loser throws `ConcurrencyException` at commit, and the classifier returns the approve conflict refusal so the caller re-reads the expense before deciding again.

**Reviewed for:** Does every `ConcurrencyException` path at commit reach the use case conflict refusal? Look at the registered classifier.

### Read Each Guarding Aggregate Under a Shared Lock

**ID:** `litespec/packs/dotnet/persistence#read-each-guarding-aggregate-under-a-shared-lock`

**Strength:** required

**Applies to:** artifact: repository

**Statement:** A repository MUST read a guarding aggregate through `GetForGuardAsync`, which runs a `FOR SHARE` query through the Marten session in the command's transaction.

**Rationale:** A read followed by a write does not keep the guard true: a competing command can change the guarding row between the two. The guard read runs `SELECT ... FOR SHARE` on PostgreSQL through the Marten session, with `session.QueryAsync<T>` carrying the SQL or with `AdvancedSql.QueryAsync`, inside the transaction LiteBus opens through `IUnitOfWorkBoundary`, so a competing command that changes the guarding row waits until the guarded command commits, and a guarded command that starts after the change reads the new state and refuses. This provision binds `litespec/core/backend-persistence#honor-the-declared-consistency-mode` for .NET.

**Example:** Claimwick's submit-expense command guards on the expense report staying open. Its repository reads the report through `GetForGuardAsync`, so a close-report command that lands first makes submit-expense read the closed report and refuse, while a close-report that lands during the submit waits on its lock until the submit commits.

**Reviewed for:** Does every guarding-aggregate read go through `GetForGuardAsync` with a shared row lock in the command's transaction? Look at each repository guard read.

### Implement the Unit of Work Boundary

**ID:** `litespec/packs/dotnet/persistence#implement-the-unit-of-work-boundary`

**Strength:** required

**Applies to:** artifact: any

**Statement:** The project's unit of work MUST implement `IUnitOfWorkBoundary`.

**Rationale:** LiteBus calls `BeginAsync` after guards, validators, and shortcuts and before the first pre-handler, commits on success, and rolls back otherwise. The one commit per command pipeline under `litespec/packs/dotnet/persistence#commit-once-in-the-command-pipeline` and the guard read lock under `litespec/packs/dotnet/persistence#read-each-guarding-aggregate-under-a-shared-lock` then run inside one transaction that LiteBus opens.

**Reviewed for:** Does the project's unit of work implement `IUnitOfWorkBoundary` with `BeginAsync` and `RollbackAsync`? Look at the unit-of-work type beside the pipeline composition.

### Apply Schema Changes in a Migration Step

**ID:** `litespec/packs/dotnet/persistence#apply-schema-changes-in-a-migration-step`

**Strength:** required

**Applies to:** artifact: migration

**Statement:** Outside development, schema objects MUST change only in a migration step that runs before the hosts start and applies the difference with `CreateOrUpdate`, never `AutoCreate.All`, while the hosts run with `AutoCreate.None`.

**Rationale:** A schema change that runs from a starting replica races every other replica, and `AutoCreate.All` drops and recreates tables that do not match the model, so it would destroy the data in a database that holds it. A migration step applies the difference once with `ApplyAllConfiguredChangesToDatabaseAsync(AutoCreate.CreateOrUpdate)` before any host starts, and each host opens its store with `AutoCreate.None` so startup never alters the schema. Development and disposable integration databases may still create schema automatically, as the shared rule allows. This provision binds `litespec/core/backend-persistence#control-production-schema-changes` for .NET.

**Example:** In the Claimwick product, a release adds an index over submitted expenses. A deployment step applies the difference with `AutoCreate.CreateOrUpdate` before the Claimwick WebApi and Worker hosts start, and both hosts configure the store with `AutoCreate.None`, so neither changes the schema at startup. No host carries `AutoCreate.All`.

**Reviewed for:** Does any hosted schema change run from application startup or through `AutoCreate.All`, or does any host open its store with a schema-changing `AutoCreate` value? Look at the migration step and the host store configuration.

### Share One Serializer Configuration Across Marten and the Outbox

**ID:** `litespec/packs/dotnet/persistence#share-one-serializer-configuration-across-marten-and-the-outbox`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Marten, the outbox store, and any scheduled-job store MUST share one serializer configuration built by one factory method.

**Rationale:** This provision binds `litespec/core/backend-persistence#serialize-every-persisted-payload-with-one-configuration` for .NET: one factory method builds the `JsonSerializerOptions` with the converters and discriminator registrations, and each store takes its options from that method. A converter added in one place then reaches every store, and a typed identifier with a validating constructor and a get-only value round-trips identically through each one.

**Example:**

```csharp
internal static class ClaimwickJson
{
    public static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new MoneyJsonConverter());
        options.Converters.Add(new StronglyTypedIdJsonConverterFactory());
        return options;
    }
}
```

Marten takes its serializer from `ClaimwickJson.CreateOptions()`, and the outbox and job stores take theirs from the same method.

**Reviewed for:** Does every store build its serializer options from the one factory method, with no store carrying its own converters? Look at the Marten, outbox, and job store registrations beside the factory method.

### Compare Typed Identifiers by Their Underlying Values in Queries

**ID:** `litespec/packs/dotnet/persistence#compare-typed-identifiers-by-their-underlying-values-in-queries`

**Strength:** required

**Applies to:** artifact: query-handler

**Statement:** A query MUST compare typed identifiers by their underlying values instead of passing a list of typed identifiers as a query parameter.

**Rationale:** The database driver binds a list parameter by the element type it knows. A list of typed identifiers reaches the driver as a list of an unknown structured type, and the comparison then fails or matches nothing at the database. Comparing the underlying values keeps the parameter a list of values the driver binds. Each identifier still travels as its typed form in code and crosses into its underlying value only at the query.

**Example:** In the library product, the list-loans query takes a list of reader identifiers and compares `reader_id` against the underlying values:

```csharp
var ids = query.ReaderIds.Select(id => id.Value).ToList();
```

The query passes `ids` as its parameter, so the database compares values it binds.

**Checked by:** `dotnet build` reports `LS2032` for a query passing a list of typed identifiers as a database parameter. It reports `LS2037` for a query reading the document's own identity value inside a queryable expression.

### Load Documents Keyed by Typed Identifiers One at a Time

**ID:** `litespec/packs/dotnet/persistence#load-documents-keyed-by-typed-identifiers-one-at-a-time`

**Strength:** required

**Applies to:** artifact: repository

**Statement:** A repository MUST load documents keyed by a typed identifier one at a time instead of batching them in one load call.

**Rationale:** A batch load call keys its lookup by the identifier form the store understands. A batch of typed identifiers reaches the store in a form its key comparison does not match, so the call returns nothing or the wrong documents. Loading each document by its typed identifier through the single-document load keeps every lookup on the path the typed-identifier mapping defines.

**Example:** In the Claimwick product, a repository method that needs several expenses loops over the single-document load:

```csharp
public async Task<IReadOnlyList<Expense>> GetByIdsAsync(
    IReadOnlyList<ExpenseId> ids,
    CancellationToken cancellationToken)
{
    var expenses = new List<Expense>(ids.Count);
    foreach (var id in ids)
    {
        expenses.Add(await GetByIdAsync(id, cancellationToken));
    }
    return expenses;
}
```

Each lookup follows the typed-identifier mapping instead of batching the identifiers in one call.

**Checked by:** `dotnet build` reports `LS2033` for a batch document load keyed by typed identifiers.

## Conventions

### Register One Scoped Session

**ID:** `litespec/packs/dotnet/persistence#register-one-scoped-session`

**Strength:** default

**Applies to:** artifact: any

**Default:** Register `IDocumentSession` with the WebApi request or Worker operation scope.

**Replacement:** A project names the session scope each request or operation uses in its project documentation.

**Rationale:** One session per WebApi request or Worker operation means the repositories and the commit handler in one command resolve the same instance. Two sessions would stage and commit separately, splitting one command across two units of work. This provision binds `litespec/core/backend-persistence#use-one-session-per-command` for .NET.

**Reviewed for:** Does the service registration wire `IDocumentSession` once per WebApi request or Worker operation scope? Look at the service registration.

### Plan Each Schema Change Before Applying It

**ID:** `litespec/packs/dotnet/persistence#plan-each-schema-change-before-applying-it`

**Strength:** default

**Applies to:** artifact: migration

**Default:** The migration step prints the SQL that `CreateMigrationAsync()` plans, marks every destructive statement, refuses those statements without an explicit allowance, records each applied run in a ledger with the build's schema fingerprint, and requires the ledger to hold this build's fingerprint before the release is ready.

**Replacement:** A project names its own migration planning and ledger convention in its backend documentation.

**Rationale:** `CreateMigrationAsync()` computes the difference without applying it and `ToDatabaseScript()` gives the full creation script, so the step can show what it will run and stop on a drop no one approved. `AssertDatabaseMatchesConfigurationAsync()` and Weasel's `db-assert` report drift on demand. A ledger keyed by schema fingerprint tells the readiness check which schema a database already holds, so a build is not applied twice or applied to a database at a different schema.

**Example:** In the Claimwick product, the migration step prints the planned SQL for a new index over expenses and flags a statement that would drop an old column; the release stops until an owner allows that drop. After the step applies, it writes the build's schema fingerprint to the migration ledger, and the readiness job checks that ledger entry before opening traffic to the Claimwick hosts.

**Reviewed for:** Does any migration step apply SQL it has not printed, pass a destructive statement without an explicit allowance, or leave the ledger without this build's schema fingerprint at readiness? Look at the migration step output, the allowance, and the ledger.

### Evolve Stored Documents When They Are Read

**ID:** `litespec/packs/dotnet/persistence#evolve-stored-documents-when-they-are-read`

**Strength:** default

**Applies to:** artifact: aggregate

**Default:** Each stored document carries its contract version, a registered upgrade step for each version runs on the JSON before deserializing, the stored form is rewritten only when the aggregate is saved again, composition refuses a broken chain of steps, and a rewrite at deploy is reserved for documents that may change and only when a query or index needs the new shape.

**Replacement:** A project names its own document upgrade convention in its backend documentation.

**Rationale:** Marten upcasts only events, never documents, so a stored document keeps the JSON an earlier version wrote. Chaining one step per version and refusing a broken chain keeps a document at any old version readable. Because the upgrade runs on read and rewrites only on the next save, an append-only record or a hash-chained audit record keeps its stored form: the upgrade shapes the in-memory object without changing the bytes on disk. A deploy-time rewrite would touch those records, so it stays limited to documents whose form may change and only when a query or index cannot work with the old shape. This is how a .NET consumer meets `litespec/core/backend-persistence#evolve-stored-document-contracts-explicitly`.

**Example:** In the Claimwick product, version 1 of an expense document stores the submitter under `submittedBy`. Version 2 renames that member to `employeeId`. A registered step from version 1 to version 2 rewrites the JSON on read before deserializing, so both old and new documents load as the current aggregate, and the old document's stored form changes only the next time that expense is saved. For an append-only approval record, the same read step produces the current in-memory shape but never rewrites the stored document.

**Reviewed for:** Is any stored document missing a contract version, any contract change missing a registered upgrade step, any stored form rewritten outside the next save without an approved deploy rewrite for a query or index, or any broken upgrade chain accepted at composition? Look at the document contract versions, the registered upgrade steps, and the store configuration.

## Reference Example

This informative example demonstrates `litespec/packs/dotnet/persistence#stage-aggregate-writes-through-repositories`.

```csharp
internal sealed class PostRepository : IPostRepository
{
    private readonly IDocumentSession _session;
    private readonly DomainEventBuffer _eventBuffer;

    public PostRepository(IDocumentSession session, DomainEventBuffer eventBuffer)
    {
        _session = session;
        _eventBuffer = eventBuffer;
    }

    public async Task<Post> GetByIdAsync(
        PostId id,
        CancellationToken cancellationToken)
    {
        var post = await _session.LoadAsync<Post>(id, cancellationToken);
        return post ?? throw new PostNotFoundException();
    }

    public void Store(Post post)
    {
        _session.Store(post);
        _eventBuffer.Track(post);
    }
}
```
