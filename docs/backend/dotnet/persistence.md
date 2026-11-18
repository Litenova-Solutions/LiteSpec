# .NET Persistence

**Stack:** dotnet

**Concern:** infrastructure

This page defines how the shared [Persistence](../persistence.md) rules apply to a .NET backend that stores through Marten, the baseline document store. It covers repositories, the read session, the single commit, and the Infrastructure folder layout and session lifetime they assume.

## Concepts

### Marten as the Baseline Provider

Marten is the baseline provider: it supplies the session, the read session, and document storage. The provisions on this page name Marten types because the baseline is Marten. Backends that store through another provider follow the provider-neutral rules on [Persistence](../persistence.md), which these provisions bind for .NET.

### How a Command Reaches the Store

Every command runs inside one scoped session: a Marten `IDocumentSession` that the host resolves once per WebApi request or Worker operation. The handler loads and stages aggregates through Domain repositories, the interfaces Domain owns for storing and loading each aggregate. The stored document is the aggregate root itself: Marten persists each root as its own document, and the project writes no parallel document type for an aggregate.

Exactly one command post-handler commits the session with `SaveChangesAsync`. Only that post-handler commits, so a failed command leaves nothing stored.

The declared consistency mode decides how work splits across commands, but each command still commits once. Under `consistency: atomic` the one commit covers everything the handler staged, and other modes split work across commands that each commit once.

Events leave through the LiteBus outbox in process: the staging step commits each envelope with the business work, and the outbox processor delivers module events and domain-event reactions from the product's PostgreSQL database, retrying a failed handler with the configured backoff and dead-lettering at the bound. No broker is needed for anything inside the bounded context.

### How a Query Reads

A query handler never loads aggregates. It queries the aggregate type through the read session, `IQuerySession`, which filters, orders, pages, and shapes records. The handler applies filter, scope, ordering, and limit before materialization.

## Standards

### Stage Aggregate Writes Through Repositories

**ID:** `litespec/backend/dotnet/persistence#stage-aggregate-writes-through-repositories`

**Strength:** required

**Applies to:** artifact: command-handler

**Statement:** A command handler MUST reach persistence only through a Domain repository, never through `IDocumentSession`.

**Rationale:** Infrastructure implements each Domain repository with the scoped session, so the handler stages work without naming a provider type. A repository that exposed the session, a generic query, or a save operation would hand the handler the provider surface the layer exists to hide. This provision binds `litespec/backend/persistence#stage-aggregate-writes-through-domain-repositories` for .NET.

**Checked by:** `dotnet build` reports `LS2004` for a command handler writing through a document session or database context.

### Store Each Aggregate Root as Its Own Document

**ID:** `litespec/backend/dotnet/persistence#store-each-aggregate-root-as-its-own-document`

**Strength:** required

**Applies to:** artifact: aggregate

**Statement:** Marten MUST store each aggregate root as its own document, with no parallel document type for an aggregate.

**Rationale:** A parallel document type duplicates the aggregate shape in Infrastructure, forces mapping code between the two, and splits reads between two models of one thing. Storing the root keeps one shape from the handler to the disk. Infrastructure owns the JSON contract that makes it work: the store serializer handles non-public members and constructors, one registered discriminator per closed-set case, and converters for values such as `Money` and typed identifiers, while Domain carries no serialization attribute. Query handlers query the aggregate type through `IQuerySession`. This provision covers the stored aggregate; a query-shaped read document under `litespec/backend/persistence#add-read-documents-for-query-shaped-data` serves queries and is no parallel aggregate document. This provision binds `litespec/backend/persistence#keep-serialization-behavior-out-of-domain` for .NET. Settled by [decision/0105-store-each-aggregate-root-as-its-own-document](decision/0105-store-each-aggregate-root-as-its-own-document).

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

**ID:** `litespec/backend/dotnet/persistence#keep-pipeline-owned-collaborators-out-of-handlers`

**Strength:** required

**Applies to:** artifact: command-handler, query-handler

**Statement:** A command or query handler MUST NOT take a pipeline-owned collaborator, such as `IUnitOfWork`, `IIdempotencyStore`, or `ITransactionalOutbox`, through its constructor.

**Rationale:** The pipeline owns the unit of work, the idempotency store, the outbox, and the uniqueness claim service. A handler that takes one of them commits or reserves work outside the post-handler that owns the commit. The analyzer's default list names the LiteBus types, and a project adds its own claim service to the `litespec.LS2016.pipeline_owned_types` key. This provision binds `litespec/backend/persistence#keep-uniqueness-out-of-handlers` for .NET.

**Example:** The submit-expense handler takes its Domain repository and no unit of work, so the commit stays in the one post-handler. A handler constructor that adds `IUnitOfWork` is reported at the parameter.

**Checked by:** `dotnet build` reports `LS2016` for a command or query handler whose constructor takes a pipeline-owned type.

### Query Through IQuerySession

**ID:** `litespec/backend/dotnet/persistence#query-through-iquerysession`

**Strength:** required

**Applies to:** artifact: query-handler

**Statement:** A query handler MUST project through `IQuerySession`, applying filter, scope, ordering, and limit before materialization.

**Rationale:** The read session already filters, orders, pages, and projects. A read-store interface per aggregate, a generic read repository, or a callback wrapper adds a layer that repeats it. Ordering and paging follow `litespec/backend/api#bound-collection-queries`. This provision binds `litespec/backend/persistence#query-through-the-read-session` for .NET.

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

**ID:** `litespec/backend/dotnet/persistence#commit-once-in-the-command-pipeline`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Exactly one global command post-handler MUST call `SaveChangesAsync` for a command pipeline.

**Rationale:** When exactly one post-handler commits, a failed command leaves the scoped session uncommitted and no partial work persists. A second commit, in a handler, repository, validator, reaction, job, or endpoint, would store work the pipeline later refuses. This provision binds `litespec/backend/persistence#commit-each-command-pipeline-once` for .NET.

The obligation is one commit per pipeline, not one named method. Under the baseline provider that commit is `IDocumentSession.SaveChangesAsync`. Under `consistency: atomic` it covers everything the handler staged. Other modes split work across commands. Each command still commits once, and the job resumes from its cursor or runs the declared compensation.

**Reviewed for:** Does exactly one post-handler file call `SaveChangesAsync` in each pipeline? Look at the `SaveChangesAsync` calls in each pipeline.

### Enforce Uniqueness with Marten Unique Indexes

**ID:** `litespec/backend/dotnet/persistence#enforce-uniqueness-with-marten-unique-indexes`

**Strength:** required

**Applies to:** artifact: aggregate

**Statement:** Each Marten aggregate document MUST enforce its uniqueness rules through a unique index over the stored value or an identity derived from its owner.

**Rationale:** A unique index makes the store arbitrate the race at commit, where every concurrent write is visible. The index is declared with `options.Schema.For<T>().UniqueIndex`, or as a computed index carrying `IsUnique`. The index name follows `litespec/backend/persistence#use-database-naming-conventions`. A record that exists at most once per owner takes a document id equal to the owner id instead, so a second record for the same owner collides on identity. This provision binds `litespec/backend/persistence#enforce-uniqueness-in-the-store` for .NET.

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

**ID:** `litespec/backend/dotnet/persistence#claim-cross-record-keys-with-a-claims-document`

**Strength:** required

**Applies to:** artifact: repository

**Statement:** Each cross-record uniqueness key MUST be claimed through a claims document keyed by the key text and written with `session.Insert`, never with `Store`.

**Rationale:** Some keys admit no index: Marten cannot index elements of a list inside a document, cannot enforce uniqueness across document types, and computed indexes do not work on `DateTime` or `DateTimeOffset` members. Those keys are claimed through a small document whose id is the key text, so the store still arbitrates the race. The write uses `Insert`, which fails when the key document already exists. `Store` upserts, so it would overwrite a competing claim instead of colliding with it. The repository compares the aggregate keys at load with the keys at commit, and inserts the added key documents and deletes the removed ones in the same transaction. This provision binds `litespec/backend/persistence#claim-a-cross-record-key-at-commit` for .NET.

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

**ID:** `litespec/backend/dotnet/persistence#map-constraint-violations-with-the-postgresql-refusal-classifier`

**Strength:** required

**Applies to:** artifact: any

**Statement:** Each Marten unique index MUST map by name to its conflict refusal code in `PostgreSqlConstraintRefusalClassifier`.

**Rationale:** The `LiteBus.Storage.PostgreSql.PostgreSqlConstraintRefusalClassifier` carries the map from index name to refusal code and is registered through `UseFaultRefusalClassifierInstance`. The pipeline fault classification then turns a unique violation into the use case named conflict refusal under `litespec/backend/application#refuse-a-lost-race-as-a-conflict`. This provision binds `litespec/backend/persistence#map-a-constraint-violation-to-its-refusal` for .NET.

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

**ID:** `litespec/backend/dotnet/persistence#apply-optimistic-concurrency-to-every-aggregate-document`

**Strength:** required

**Applies to:** artifact: aggregate

**Statement:** Each Marten aggregate document type MUST enable optimistic concurrency.

**Rationale:** The store compares the version each command loaded with the version it commits against, so a stale write fails the commit instead of silently overwriting newer work. Each aggregate document type enables it with `UseOptimisticConcurrency(true)`, or one `Policies.ForAllDocuments` policy covers every aggregate document type at once. No aggregate document type is left without it. This provision binds `litespec/backend/persistence#version-every-aggregate` for .NET.

**Example:** The expense document enables the check on its own type:

```csharp
options.Schema.For<Expense>().UseOptimisticConcurrency(true);
```

**Reviewed for:** Does every Marten aggregate document type enable optimistic concurrency? Look at each document type and the document policy.

### Map Marten Concurrency Faults to Conflict Refusals

**ID:** `litespec/backend/dotnet/persistence#map-marten-concurrency-faults-to-conflict-refusals`

**Strength:** required

**Applies to:** artifact: any

**Statement:** A classifier MUST translate Marten's `ConcurrencyException` into the use case conflict refusal.

**Rationale:** A `ConcurrencyException` names a provider type that Application does not expose. The registered classifier translates it into the use case named conflict refusal, following `litespec/backend/application#refuse-a-lost-race-as-a-conflict`. A version race and a uniqueness race then reach the caller through the same refusal path. This provision binds `litespec/backend/application#refuse-a-lost-race-as-a-conflict` for .NET.

**Example:** Two approvers race on one submitted expense; the loser throws `ConcurrencyException` at commit, and the classifier returns the approve conflict refusal so the caller re-reads the expense before deciding again.

**Reviewed for:** Does every `ConcurrencyException` path at commit reach the use case conflict refusal? Look at the registered classifier.

### Read Each Guarding Aggregate Under a Shared Lock

**ID:** `litespec/backend/dotnet/persistence#read-each-guarding-aggregate-under-a-shared-lock`

**Strength:** required

**Applies to:** artifact: repository

**Statement:** A repository MUST read a guarding aggregate through `GetForGuardAsync`, which runs a `FOR SHARE` query through the Marten session in the command's transaction.

**Rationale:** A read followed by a write does not keep the guard true: a competing command can change the guarding row between the two. The guard read runs `SELECT ... FOR SHARE` on PostgreSQL through the Marten session, with `session.QueryAsync<T>` carrying the SQL or with `AdvancedSql.QueryAsync`, inside the transaction LiteBus opens through `IUnitOfWorkBoundary`, so a competing command that changes the guarding row waits until the guarded command commits, and a guarded command that starts after the change reads the new state and refuses. This provision binds `litespec/backend/persistence#honor-the-declared-consistency-mode` for .NET.

**Example:** Claimwick's submit-expense command guards on the expense report staying open. Its repository reads the report through `GetForGuardAsync`, so a close-report command that lands first makes submit-expense read the closed report and refuse, while a close-report that lands during the submit waits on its lock until the submit commits.

**Reviewed for:** Does every guarding-aggregate read go through `GetForGuardAsync` with a shared row lock in the command's transaction? Look at each repository guard read.

### Implement the Unit of Work Boundary

**ID:** `litespec/backend/dotnet/persistence#implement-the-unit-of-work-boundary`

**Strength:** required

**Applies to:** artifact: any

**Statement:** The project's unit of work MUST implement `IUnitOfWorkBoundary`.

**Rationale:** LiteBus calls `BeginAsync` after guards, validators, and shortcuts and before the first pre-handler, commits on success, and rolls back otherwise. The one commit per command pipeline under `litespec/backend/dotnet/persistence#commit-once-in-the-command-pipeline` and the guard read lock under `litespec/backend/dotnet/persistence#read-each-guarding-aggregate-under-a-shared-lock` then run inside one transaction that LiteBus opens.

**Reviewed for:** Does the project's unit of work implement `IUnitOfWorkBoundary` with `BeginAsync` and `RollbackAsync`? Look at the unit-of-work type beside the pipeline composition.

## Conventions

### Register One Scoped Session

**ID:** `litespec/backend/dotnet/persistence#register-one-scoped-session`

**Strength:** default

**Applies to:** artifact: any

**Default:** Register `IDocumentSession` with the WebApi request or Worker operation scope.

**Replacement:** A project names the session scope each request or operation uses in its project documentation.

**Rationale:** One session per WebApi request or Worker operation means the repositories and the commit handler in one command resolve the same instance. Two sessions would stage and commit separately, splitting one command across two units of work. This provision binds `litespec/backend/persistence#use-one-session-per-command` for .NET.

**Reviewed for:** Does the service registration wire `IDocumentSession` once per WebApi request or Worker operation scope? Look at the service registration.

## Reference Example

This informative example demonstrates `litespec/backend/dotnet/persistence#stage-aggregate-writes-through-repositories`.

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
