# .NET Persistence

**Stack:** dotnet

**Concern:** infrastructure

This page defines how the shared [Persistence](../persistence.md) rules apply to a .NET backend that stores through Marten, the baseline document store. It covers repositories, the read session, the single commit, and the Infrastructure folder layout and session lifetime they assume.

## Concepts

### Marten as the Baseline Provider

Marten is the baseline provider: it supplies the session, the read session, and document storage. The provisions on this page name Marten types because the baseline is Marten. Backends that store through another provider follow the provider-neutral rules on [Persistence](../persistence.md), which these provisions bind for .NET.

### How a Command Reaches the Store

Every command runs inside one scoped session: a Marten `IDocumentSession` that the host resolves once per WebApi request or Worker operation. The handler loads and stages aggregates through Domain repositories, the interfaces Domain owns for storing and loading each aggregate.

Exactly one command post-handler commits the session with `SaveChangesAsync`. Only that post-handler commits, so a failed command leaves nothing stored.

The declared consistency mode decides how work splits across commands, but each command still commits once. Under `consistency: atomic` the one commit covers everything the handler staged, and other modes split work across commands that each commit once.

### How a Query Reads

A query handler never loads aggregates. It projects through the read session, `IQuerySession`, which filters, orders, pages, and shapes records. The handler applies filter, scope, ordering, and limit before materialization.

## Standards

### Stage Aggregate Writes Through Repositories

**ID:** `litespec/backend/dotnet/persistence#stage-aggregate-writes-through-repositories`

**Strength:** required

**Applies to:** artifact: command-handler

**Statement:** A command handler MUST reach persistence only through a Domain repository, never through `IDocumentSession`.

**Rationale:** Infrastructure implements each Domain repository with the scoped session, so the handler stages work without naming a provider type. A repository that exposed the session, a generic query, or a save operation would hand the handler the provider surface the layer exists to hide. This provision binds `litespec/backend/persistence#stage-aggregate-writes-through-domain-repositories` for .NET.

**Checked by:** `dotnet build` reports `LS2004` for a command handler writing through a document session or database context.

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
options.Schema.For<PayoutDocument>().UniqueIndex(
    UniqueIndexType.Computed,
    "uq_payout_owner_number",
    x => x.PayoutNumber);
```

A payout exists at most once per expense, so its document id is the expense id:

```csharp
// The payout document id equals the expense id: one payout per expense.
session.Store(new PayoutDocument(expense.Id, payoutNumber));
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
options.Schema.For<ExpenseDocument>().UseOptimisticConcurrency(true);
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

## Conventions

### Use the Persistence Layer Folder Layout

**ID:** `litespec/backend/dotnet/persistence#use-the-persistence-layer-folder-layout`

**Strength:** default

**Applies to:** artifact: any

**Default:** Place Marten configuration, repositories, commit behavior, and serialization in the shared kernel and module Infrastructure folders this provision names.

**Replacement:** A project names the folders holding its persistence types in its project documentation.

**Rationale:** One store stays the default, configured in the shared kernel Infrastructure folder, while each module's mappings and repositories live in its own Infrastructure folders. Every persistence type then has one discoverable home. Scattered configuration hides the provider setup across the product. This provision binds `litespec/backend/persistence#use-one-persistence-folder` for .NET.

**Example:**

```text
{Product}.SharedKernel/
  Infrastructure/
    Marten/
      MartenServiceRegistration.cs
      MartenStoreConfiguration.cs
      Commit/
        SaveChangesCommandPostHandler.cs
      Events/
        DomainEventBuffer.cs
{Product}.Modules/
  Posts/                            single aggregate whose name matches the module: files directly under the module
    Infrastructure/
      PostRepository.cs
      PostMartenConfiguration.cs
  Customers/                        two aggregates: one folder per aggregate
    CustomerAccounts/
      Infrastructure/
        CustomerAccountRepository.cs
        CustomerAccountMartenConfiguration.cs
    Consents/
      Infrastructure/
        ConsentRepository.cs
        ConsentMartenConfiguration.cs
```

The module folders follow `litespec/backend/architecture#organize-code-by-module-aggregate-and-use-case`. A single-aggregate module stays flat when its aggregate root plural name equals the module name. For example, `Post` stays flat under `Posts`. Otherwise, each aggregate takes its own folder.

Aggregate-specific configuration stays beside its aggregate, while session and commit plumbing stay in the shared kernel Infrastructure folder.

**Reviewed for:** Does every Marten configuration, repository, commit, and serialization file sit in its shared kernel or module Infrastructure folder? Look at the persistence files.

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
internal sealed class PostRepository(
    IDocumentSession session,
    DomainEventBuffer eventBuffer) : IPostRepository
{
    public async Task<Post> GetByIdAsync(
        PostId id,
        CancellationToken cancellationToken)
    {
        var post = await session.LoadAsync<Post>(id, cancellationToken);
        return post ?? throw new PostNotFoundException();
    }

    public void Store(Post post)
    {
        session.Store(post);
        eventBuffer.Track(post);
    }
}
```
