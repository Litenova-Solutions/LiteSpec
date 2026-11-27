# .NET Persistence

**Stack:** dotnet
**Concern:** infrastructure

This page states how a .NET backend stores through Marten, the baseline document store. It is for developers who write repositories, read models, and migration steps. A second write path or a second serializer splits one command across two units of work, so storage goes through one session and one serializer configuration.

## Concepts

### How a Command Reaches the Store

Every command runs inside one scoped session: a Marten `IDocumentSession` the host resolves once per WebApi request or Worker operation. The handler loads and stages aggregates through Domain repositories, and the repository exposes no session, generic query, or save operation. The stored document is the aggregate root itself, with no parallel document type for an aggregate. Infrastructure owns the JSON contract behind that shape: the store serializer handles non-public members and constructors, one registered discriminator per closed-set case, and converters for values such as `Money` and typed identifiers, while Domain carries no serialization attribute.

Exactly one global command post-handler commits the session with `SaveChangesAsync`. A failed command leaves the scoped session uncommitted, so no partial work persists. The declared consistency mode decides how work splits across commands: `consistency: atomic` covers everything the handler staged, and other modes split work across commands that each commit once, with the job resuming from its cursor or running the declared compensation.

Events leave through the LiteBus outbox in process. The staging step commits each envelope with the business work, and the outbox processor delivers module events and domain-event reactions from the product PostgreSQL database, retrying a failed handler with the configured backoff and dead-lettering at the bound. No broker sits in front of anything inside the bounded context, and each handler gets its own delivery, so one failing handler retries alone.

Guard reads and the commit share one transaction. A guarding aggregate reads through `GetForGuardAsync`, which runs `SELECT ... FOR SHARE` on PostgreSQL through the Marten session, inside the transaction LiteBus opens through `IUnitOfWorkBoundary`. LiteBus calls `BeginAsync` after guards, validators, and shortcuts and before the first pre-handler, commits on success, and rolls back otherwise. Claimwick submit-expense guards on the expense report staying open: its repository reads the report through `GetForGuardAsync`, so a close-report that lands first makes submit-expense read the closed report and turn away, while a close-report landing mid-submit waits on the lock until the submit commits.

### How Uniqueness and Concurrency Hold

The store arbitrates each race at commit. A uniqueness rule over a stored value becomes a unique index declared with `options.Schema.For<T>().UniqueIndex`, or a computed index carrying `IsUnique`, with index names following the database naming conventions. A record that exists at most once per owner takes a document id derived from the owner instead: a payout exists at most once per expense, so its document id is the expense id, and a second payout for the same expense collides on identity.

Some keys admit no index: Marten indexes no elements of a list inside a document, enforces no uniqueness across document types, and computed indexes skip `DateTime` and `DateTimeOffset` members. Those keys go through a small claims document whose id is the key text, written with `session.Insert`, which fails when the key document already exists. `Store` would upsert and overwrite a competing claim instead of colliding with it. The repository compares the aggregate keys at load with the keys at commit, and inserts the added key documents and deletes the removed ones in the same transaction.

In the library product, each copy barcode belongs to one book while a book holds its barcodes in a list, so each barcode goes through a `BarcodeClaim` keyed by the barcode text.

Each Marten unique index maps by name to its conflict refusal code in `PostgreSqlConstraintRefusalClassifier`, registered through `UseFaultRefusalClassifierInstance`. The pipeline fault classification turns the unique violation into the use case named conflict refusal. A version race travels the same path: each aggregate document type enables optimistic concurrency with `UseOptimisticConcurrency(true)`, or one `Policies.ForAllDocuments` policy covers every aggregate document type at once, and the registered classifier turns Marten `ConcurrencyException` into the use case conflict refusal. Two approvers racing on one submitted expense show the result: the loser gets the approve conflict refusal and re-reads the expense before deciding again.

```csharp
services.UseFaultRefusalClassifierInstance(
    new PostgreSqlConstraintRefusalClassifier(new Dictionary<string, string>
    {
        ["uq_book_barcode"] = "book-barcode-taken",
        ["uq_payout_owner_number"] = "spending.payout-number-taken",
    }));
```

### How a Query Reads

A query handler projects through the read session, `IQuerySession`, which filters, orders, pages, and shapes records. The handler applies filter, scope, ordering, and limit before materialization. A read-store interface per aggregate, a generic read repository, or a callback wrapper adds a layer that repeats the session, so the handler uses the session directly. Ordering and paging follow the bound-collection-queries rule.

A table the project keeps beside its documents reads through the same session. A spending-owners running total, a counter, or an append-only ledger written through plain SQL returns through `AdvancedSql.QueryAsync<TRow>` with a JSON-serializable row type. The SQL selects one JSON object per row, keyed in the casing the store serializer uses, with the filter, ordering, and limit in its `WHERE`, `ORDER BY`, and `LIMIT`. The handler keeps no repository for such a table, and the project adds no read interface for it.

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

In the Claimwick product, each spending owner running total lives in a `spending_totals` table that a command updates in place, and the read-spending-totals query reads it through the session as shown. The database driver binds a list parameter by the element type it knows, so a query compares typed identifiers by their underlying values: the library list-loans query takes reader identifiers, maps them with `query.ReaderIds.Select(id => id.Value).ToList()`, and passes the values as its parameter. A batch load call keys its lookup by the form the store understands, so a repository loads documents keyed by a typed identifier one at a time through the single-document load, looping `GetByIdAsync` per identifier.

### How the Schema and Stored Documents Change

A release changes two different things in the store: the schema objects (tables, columns, and indexes) and the JSON inside documents already on disk. Schema objects rebuild or alter through statements against the database. Document JSON stays as the earlier version wrote it, because a code change reshapes no bytes a previous version stored. Treating the two as one problem either drops data or leaves old documents unreadable, so Marten keeps them separate.

`IDocumentStore.Storage.CreateMigrationAsync()` computes the schema difference between the configured model and the database without applying it, and `ToDatabaseScript()` returns the full creation script. `ApplyAllConfiguredChangesToDatabaseAsync(AutoCreate.CreateOrUpdate)` applies the difference, adding and altering objects to match the model. `AutoCreate.All` drops and recreates tables that differ from the model, so it stays away from any database holding data. `AssertDatabaseMatchesConfigurationAsync()` reports drift between the model and the database without changing either. Weasel, the schema tool behind Marten, offers the same jobs outside application code: `db-apply`, `db-assert`, `db-patch`, `db-dump`, and `db-list`.

Outside development, schema objects change in a migration step that runs before the hosts start. The step applies the difference once with `AutoCreate.CreateOrUpdate`, and each host opens its store with `AutoCreate.None`, so startup alters no schema. Development and disposable integration databases still create schema automatically. The step prints the SQL `CreateMigrationAsync()` plans, marks every destructive statement, and turns away those statements without an explicit allowance.

Each applied run goes into a ledger with the build schema fingerprint, and the ledger holds this build fingerprint before the release opens traffic. In Claimwick, a release adding an index over submitted expenses prints the planned SQL, stops on a statement dropping an old column until an owner allows it, writes the fingerprint after applying, and the readiness job checks that ledger entry.

For documents, Marten upcasts events, not documents. An old document keeps the JSON its version wrote until the code reading it upgrades that JSON in memory. The project registers an upgrade step for each stored contract version: a read runs the steps on the JSON before deserializing, and the stored form rewrites when the aggregate saves again.

That split protects an append-only record or a hash-chained audit record, whose stored form stays put: the upgrade shapes the in-memory object on read, and no step rewrites the bytes on disk. A deploy-time rewrite stays reserved for documents that change form, and then for a query or index that needs the new shape.

In Claimwick, expense version 1 stores the submitter under `submittedBy` and version 2 renames that member to `employeeId`: a registered step from version 1 to version 2 rewrites the JSON on read before deserializing, so old and new documents load as the current aggregate. An append-only approval record keeps its stored bytes and gains only the in-memory shape.

All three stores share one serializer configuration built by one factory method. Marten, the outbox store, and the scheduled-job store take their options from `ClaimwickJson.CreateOptions()`, which builds the `JsonSerializerOptions` with the converters and discriminator registrations. A converter added in one place reaches every store, and a typed identifier with a validating constructor and a get-only value round-trips identically through each one.

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

### Binary File and Blob Lifecycle

A file upload travels two paths: bytes to blob storage, facts to the aggregate. The endpoint streams the upload to a staging blob through an infrastructure port, which returns a blob key. The command carries that key, and the aggregate stores the key, not the bytes. Blob bytes stay outside the document, so the stored aggregate keeps its small JSON shape while the port owns content types, size bounds, and storage calls.

```csharp
public interface IReceiptBlob
{
    Task<string> StageAsync(Stream content, string contentType, CancellationToken cancellationToken);
    Task PromoteAsync(string blobKey, CancellationToken cancellationToken);
    Task DeleteAsync(string blobKey, CancellationToken cancellationToken);
}
```

Promotion and deletion follow the aggregate lifecycle. Attaching the receipt stores the staged key on the expense; approving the expense promotes the staged blob to the final location; erasing or removing the expense deletes the blob beside the record. The aggregate names the key in each transition, and the adapter performs the storage move. A receipt deleted with its expense leaves no blob behind, and a rejected expense keeps its staged key until the rule holding it releases it.

An orphan sweep job removes staged blobs past their window. It selects staged keys older than the staging window with no live aggregate pointing at them, sends one delete per key, and keeps a cursor so a restart resumes where the last run stopped. In Claimwick, the sweep selects staged receipt keys past seven days with no expense carrying them, deletes each blob through the receipt port, and stores its cursor after each batch.

## Standards

### Stage Aggregate Writes Through Repositories

**ID:** `litespec/packs/dotnet/persistence#stage-aggregate-writes-through-repositories`

**Strength:** required

**Applies to:** artifact: command-handler

**Statement:** A command handler MUST reach persistence only through a Domain repository, never through `IDocumentSession`.

**Rationale:** Infrastructure implements each Domain repository with the scoped session, so the handler stages work without naming a provider type. A repository that exposed the session, a generic query, or a save operation would hand the handler the provider surface the layer exists to hide. This provision binds `litespec/core/backend-persistence#stage-aggregate-writes-through-domain-repositories` for .NET.

**Example:** The submit-expense handler takes its Domain repository and no document session, so the commit stays in the one post-handler. A handler constructor adding the session is reported at the parameter.

**Checked by:** `dotnet build` reports `LS2004` for a command handler writing through a document session or database context.

### Store Each Aggregate Root as Its Own Document

**ID:** `litespec/packs/dotnet/persistence#store-each-aggregate-root-as-its-own-document`

**Strength:** required

**Applies to:** artifact: aggregate

**Statement:** Marten MUST store each aggregate root as its own document, with no parallel document type for an aggregate.

**Rationale:** A parallel document type duplicates the aggregate shape in Infrastructure, forcing mapping code between the two and splitting reads between two models of one thing. Storing the root keeps one shape from the handler to the disk. Infrastructure owns the JSON contract that makes it work: the store serializer handles non-public members and constructors, one registered discriminator per closed-set case, and converters for values such as `Money` and typed identifiers, while Domain carries no serialization attribute. Query handlers query the aggregate type through `IQuerySession`. This provision covers the stored aggregate; a query-shaped read document serves queries and is no parallel aggregate document. This provision binds [Backend Persistence](../../core/backend-persistence.md) for .NET. Settled by [decision/0105-store-each-aggregate-root-as-its-own-document](decision/0105-store-each-aggregate-root-as-its-own-document).

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

**Rationale:** The pipeline owns the unit of work, the idempotency store, the outbox, and the uniqueness claim service. A handler that takes one of them commits or reserves work outside the post-handler that owns the commit. The analyzer default list names the LiteBus types, and a project adds its own claim service to the `litespec.LS2016.pipeline_owned_types` key. This provision binds `litespec/core/backend-persistence#keep-uniqueness-out-of-handlers` for .NET.

**Example:** The submit-expense handler takes its Domain repository and no unit of work, so the commit stays in the one post-handler. A handler constructor that adds `IUnitOfWork` is reported at the parameter.

**Checked by:** `dotnet build` reports `LS2016` for a command or query handler whose constructor takes a pipeline-owned type.

### Compare Typed Identifiers by Their Underlying Values in Queries

**ID:** `litespec/packs/dotnet/persistence#compare-typed-identifiers-by-their-underlying-values-in-queries`

**Strength:** required

**Applies to:** artifact: query-handler

**Statement:** A query MUST compare typed identifiers by their underlying values instead of passing a list of typed identifiers as a query parameter.

**Rationale:** The database driver binds a list parameter by the element type it knows. A list of typed identifiers reaches the driver as a list of an unknown structured type, and the comparison then fails or matches nothing at the database. Comparing the underlying values keeps the parameter a list of values the driver binds. Each identifier still travels as its typed form in code and crosses into its underlying value at the query.

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

**Rationale:** A batch load call keys its lookup by the identifier form the store understands. A batch of typed identifiers reaches the store in a form its key comparison misses, so the call returns nothing or the wrong documents. Loading each document by its typed identifier through the single-document load keeps every lookup on the path the typed-identifier mapping defines.

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

None.