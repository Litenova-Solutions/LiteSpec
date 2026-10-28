# .NET Persistence

## Intent

A .NET backend persists aggregates through Marten, the baseline document store: command handlers stage writes through Domain repositories, queries project through the read session, and one post-handler commits each command pipeline. This page states those .NET bindings with the Infrastructure layout and session lifetime they assume, so every command keeps one transaction. The shared persistence rules live on [Persistence](../persistence.md); the `efcore` extension replaces the provider-specific rules it names for the aggregates it owns.

## Concepts

### Sessions, Repositories, and the One Commit

One scoped session serves a whole command: the Domain repositories stage aggregate writes through it, and exactly one command post-handler commits it. Query handlers read through the read session instead, projecting shaped records without loading aggregates. Only that post-handler commits, so a failed command leaves nothing stored. The declared consistency mode decides how work splits across commands; each command still commits once.

### The Baseline Provider and Its Replacement

Marten is the baseline provider, supplying the session, the read session, and document storage. The `efcore` extension replaces the provider-specific provisions it names for the aggregates it owns; every other provision on this page binds those paths unchanged.

## Standards

### Stage Aggregate Writes Through Repositories

**Statement:** A command handler MUST reach persistence only through a Domain repository, never through `IDocumentSession`.

**Rationale:** Infrastructure implements each Domain repository with the scoped session, so the handler stages work without naming a provider type. A repository that exposed the session, a generic query, or a save operation would hand the handler the provider surface the layer exists to hide. This provision states the .NET form of `litespec/backend/persistence#stage-aggregate-writes-through-domain-repositories`.

### Query Through IQuerySession

**Statement:** A query handler MUST project through `IQuerySession`, applying filter, scope, ordering, and limit before materialization.

**Rationale:** The read session already filters, orders, pages, and projects; a read-store interface per aggregate, a generic read repository, or a callback wrapper adds a layer that repeats it. Ordering and paging follow `litespec/backend/api#bound-collection-queries`. This provision states the .NET form of `litespec/backend/persistence#query-through-the-read-session`.

| Provider | Query surface |
|:---|:---|
| Marten baseline | `IQuerySession` |
| EF Core extension | The context type the extension declares |

The table names the binding per provider. The obligation to project with filter, scope, ordering, and limit holds under each.

### Commit Once in the Command Pipeline

**Statement:** Exactly one global command post-handler MUST call `SaveChangesAsync` for a command pipeline.

**Rationale:** Exactly one post-handler commits, so a failed command leaves the scoped session uncommitted and no partial work persists. A second committer, in a handler, repository, validator, reaction, or endpoint, would store work the pipeline later refuses. This provision states the .NET form of `litespec/backend/persistence#commit-each-command-pipeline-once`.

The obligation is one commit for one pipeline, not one named method. Under the baseline provider that commit is `IDocumentSession.SaveChangesAsync`. Under the `efcore` extension it is `DbContext.SaveChangesAsync`. The pipeline wraps the call in an explicit transaction when it writes through more than one context. Under `consistency: atomic` that commit covers everything the handler staged. Other modes split work across commands: each command still commits once, and the driver resumes from its cursor or runs the declared compensation.

### Enforce Uniqueness with Marten Unique Indexes

**Statement:** Each Marten aggregate document MUST enforce its uniqueness rules through a unique index over the stored value or an identity derived from its owner.

**Rationale:** A unique index makes the store arbitrate the race at commit, where every concurrent write is visible. The index is declared with `options.Schema.For<T>().UniqueIndex`, or as a computed index carrying `IsUnique`, and the index name follows `litespec/backend/persistence#use-database-naming-conventions`. A record that exists at most once per owner takes a document id equal to the owner id instead, so a second record for the same owner collides on identity. This provision states the .NET form of `litespec/backend/persistence#enforce-uniqueness-in-the-store`.

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

### Claim Cross-Record Keys with a Claims Document

**Statement:** Each cross-record uniqueness key MUST be claimed through a claims document keyed by the key text and written with `session.Insert`, never with `Store`.

**Rationale:** `Store` upserts, so it overwrites a competing claim instead of colliding with it; `Insert` fails when the key document already exists, which is the arbitration the rule needs. The repository compares the aggregate keys at load with the keys at commit, inserts the added key documents and deletes the removed ones in the same transaction. Marten cannot index elements of a list inside a document, cannot enforce uniqueness across document types, and computed indexes do not work on `DateTime` or `DateTimeOffset` members; those cases use the claims document. This provision states the .NET form of `litespec/backend/persistence#claim-a-cross-record-key-at-commit`.

**Example:** In the library product, each copy barcode belongs to one book, while a book holds its barcodes in a list:

```csharp
public sealed record BarcodeClaim(string Barcode);
```

```csharp
// The claim document id is the barcode text; Insert fails on a duplicate.
session.Insert(new BarcodeClaim(barcode));
```

### Map Constraint Violations with the PostgreSQL Refusal Classifier

**Statement:** Each Marten unique index MUST map by name to its conflict refusal code in `PostgreSqlConstraintRefusalClassifier`.

**Rationale:** The `LiteBus.Storage.PostgreSql.PostgreSqlConstraintRefusalClassifier` carries the map from index name to refusal code and is registered through `UseFaultRefusalClassifierInstance`, so the pipeline fault classification turns a unique violation into the use case named conflict refusal under `litespec/backend/application#refuse-a-lost-race-as-a-conflict`. This provision states the .NET form of `litespec/backend/persistence#map-a-constraint-violation-to-its-refusal`.

**Example:** The barcode and payout number indexes map to their refusals:

```csharp
services.UseFaultRefusalClassifierInstance(
    new PostgreSqlConstraintRefusalClassifier(new Dictionary<string, string>
    {
        ["uq_book_barcode"] = "book-barcode-taken",
        ["uq_payout_owner_number"] = "spending.payout-number-taken",
    }));
```

### Apply Optimistic Concurrency to Every Aggregate Document

**Statement:** Each Marten aggregate document type MUST enable optimistic concurrency.

**Rationale:** The store compares the version each command loaded with the version it commits against, so a stale write fails the commit instead of silently overwriting newer work. Each aggregate document type enables it with `UseOptimisticConcurrency(true)`, or one `Policies.ForAllDocuments` policy covers every aggregate document type at once; no aggregate document type is left without it. This provision states the .NET form of `litespec/backend/persistence#version-every-aggregate`.

**Example:** The expense document enables the check on its own type:

```csharp
options.Schema.For<ExpenseDocument>().UseOptimisticConcurrency(true);
```

### Map Marten Concurrency Faults to Conflict Refusals

**Statement:** A classifier MUST translate Marten's `ConcurrencyException` into the use case conflict refusal.

**Rationale:** A `ConcurrencyException` names a provider type that Application does not expose. The registered classifier translates it into the use case named conflict refusal, following `litespec/backend/application#refuse-a-lost-race-as-a-conflict`, so a version race and a uniqueness race reach the caller through the same refusal path. This provision states the .NET form of `litespec/backend/persistence#version-every-aggregate`.

**Example:** Two approvers race on one submitted expense; the loser throws `ConcurrencyException` at commit, and the classifier returns the approve conflict refusal so the caller re-reads the expense before deciding again.

## Conventions

### Use This Infrastructure Layout

**Default:** Place Marten configuration, repositories, commit behavior, and serialization under one Infrastructure persistence folder.

**Replacement:** A project names the folder holding its persistence types in its project documentation.

**Rationale:** One persistence folder holds Marten configuration, repositories, commit behavior, and serialization, so every persistence type has one discoverable home. Scattered configuration hides the provider setup across the project. This provision states the .NET form of `litespec/backend/persistence#use-one-persistence-folder`.

**Example:**

```text
{ProjectName}.Infrastructure/
  Persistence/
    Marten/
      MartenServiceRegistration.cs
      MartenStoreConfiguration.cs
      Commands/
        SaveChangesCommandPostHandler.cs
      Events/
        DomainEventBuffer.cs
      Posts/                        single aggregate whose name matches the module: files directly under the module
        PostRepository.cs
        PostMartenConfiguration.cs
      Customers/                    two aggregates: one folder per aggregate
        CustomerAccounts/
          CustomerAccountRepository.cs
          CustomerAccountMartenConfiguration.cs
        Consents/
          ConsentRepository.cs
          ConsentMartenConfiguration.cs
```

The module folders follow `litespec/backend/architecture#organize-every-layer-by-module-and-use-case`. A single-aggregate module stays flat when its aggregate root's plural name equals the module name. For example, `Post` stays flat under `Posts`. Otherwise, each aggregate takes its own folder.

Aggregate-specific configuration stays beside its aggregate, while session and commit plumbing stay under the Marten root.

### Register One Scoped Session

**Default:** Register `IDocumentSession` with the WebApi request or Worker operation scope.

**Replacement:** A project names the session scope each request or operation uses in its project documentation.

**Rationale:** One session per WebApi request or Worker operation means the repositories and the commit handler in one command resolve the same instance. Two sessions would stage and commit separately, splitting one command across two units of work. This provision states the .NET form of `litespec/backend/persistence#use-one-session-per-command`.

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

## Verification

| ID | Mark | Evidence |
|:---|:---|:---|
| litespec/backend/dotnet/persistence#stage-aggregate-writes-through-repositories | guidance | Command handler files under `apps/api/src/` name only Domain repository interfaces in their persistence calls; no handler file names `IDocumentSession`. |
| litespec/backend/dotnet/persistence#query-through-iquerysession | guidance | Query handler files project through `IQuerySession` with filter, scope, ordering, and limit applied before materialization. |
| litespec/backend/dotnet/persistence#commit-once-in-the-command-pipeline | guidance | One command post-handler file calls `SaveChangesAsync`; no handler, repository, validator, reaction, or endpoint file calls it. |
| litespec/backend/dotnet/persistence#enforce-uniqueness-with-marten-unique-indexes | guidance | Each Marten aggregate document with a uniqueness rule carries a unique index or an owner-derived identity in the store configuration. |
| litespec/backend/dotnet/persistence#claim-cross-record-keys-with-a-claims-document | guidance | Each cross-record key travels through a claims document written with `session.Insert`, with no `Store` call on the claims type. |
| litespec/backend/dotnet/persistence#map-constraint-violations-with-the-postgresql-refusal-classifier | guidance | The service registration pairs each unique index name with its conflict refusal code in the PostgreSQL refusal classifier. |
| litespec/backend/dotnet/persistence#apply-optimistic-concurrency-to-every-aggregate-document | guidance | Each Marten aggregate document type enables optimistic concurrency, per type or through the document policy. |
| litespec/backend/dotnet/persistence#map-marten-concurrency-faults-to-conflict-refusals | guidance | A `ConcurrencyException` at commit reaches the caller as the use case conflict refusal through the registered classifier. |
| litespec/backend/dotnet/persistence#use-this-infrastructure-layout | guidance | Marten configuration, repository, commit, and serialization files sit under the one Infrastructure persistence folder. |
| litespec/backend/dotnet/persistence#register-one-scoped-session | guidance | The service registration wires `IDocumentSession` once per WebApi request or Worker operation scope. |
