# Build an Audit Trail

Build an audit trail on the LiteBus pipeline that runs each state change in the .NET backend: the record shape, the pipeline handlers, the protected store, and the read path. The trail proves who attempted what against which data and what each attempt produced, so the product can answer for every sensitive action. Each step names the audit rule it satisfies, and the guide adds no rule of its own.

## Prerequisites

Start with:

- The `audit` extension selected in the consumer's `.litespec/project.yaml`.
- The audit adoption decision that `litespec/ext/audit#record-the-audit-adoption-decision` requires.
- The `audit` field on each command record. The record schema requires it with no default: a level of `required`, `optional`, or `none` with its target kind, its category when audited, and the input naming the audited record when the command changes one, or `exemptBecause` with its justification.
- A LiteBus command pipeline that commits once for each command.
- A database account reserved for audit writes, separate from the account that writes business data.
- The LiteBus release pinned for the `LiteBus.*` packages in `litespec.manifest.yaml`.
- The pipeline reference in the `site/content/docs` folder of the [LiteBus 8.0.0-preview.3 tag](https://github.com/litenova/LiteBus/tree/v8.0.0-preview.3/site/content/docs). It defines the handler interfaces this guide implements.

Read the [audit extension](../ext/audit.md) before this guide. It states the obligations, and this guide shows one compliant way to meet them.

## Procedure

### Define the Record Shape

Model the audit record once, using the fields the extension requires in `litespec/ext/audit#record-the-required-audit-fields`. Each row below names the provision that requires it.

| Field | Type | Holds | Provision |
|:---|:---|:---|:---|
| `auditId` | UUID v7 | Record identity, ordered by write time | `litespec/ext/audit#record-the-required-audit-fields` |
| `occurredAt` | Timestamp with zone | Business time of the attempt | `litespec/ext/audit#record-the-required-audit-fields` |
| `recordedAt` | Timestamp with zone | Time the evidence was written | `litespec/ext/audit#record-the-required-audit-fields` |
| `action` | Use case identity | The invoked use case | `litespec/ext/audit#name-each-action-after-its-use-case` |
| `category` | Closed set | Retention and review classification | `litespec/ext/audit#use-a-fixed-category-vocabulary` |
| `outcome` | Closed set | `succeeded`, `denied`, or `failed` | `litespec/ext/audit#record-every-attempt-outcome` |
| `failureCode` | Stable code | The refusal or failure reason | `litespec/ext/audit#record-the-refusal-code-on-an-unsuccessful-attempt` |
| `actorKind` | Closed set | Person, device, or named process | `litespec/ext/audit#use-a-closed-actor-set` |
| `actorId` | Pseudonymous identifier | The acting account | `litespec/ext/audit#record-the-required-audit-fields` |
| `actorDeviceId` | Pseudonymous identifier | The acting device | `litespec/ext/audit#use-a-closed-actor-set` |
| `actorProcess` | Name | The acting scheduled process | `litespec/ext/audit#use-a-closed-actor-set` |
| `underGrant` | Boolean | The actor used a support grant | `litespec/ext/audit#mark-actions-taken-under-a-support-grant` |
| `tenantId` | Identifier | The tenant that owns the data | `litespec/ext/audit#scope-every-record-to-its-tenant` |
| `targetKind` | Name | The kind of thing acted on | `litespec/ext/audit#record-the-required-audit-fields` |
| `targetId` | Identifier | Its identity | `litespec/ext/audit#record-the-required-audit-fields` |
| `reason` | Bounded text | Why, where the action requires one | `litespec/ext/audit#record-the-required-audit-fields` |
| `sourceIp` | Address | Request origin | `litespec/ext/audit#record-the-request-origin` |
| `userAgent` | Bounded text | The calling client | `litespec/ext/audit#record-the-request-origin` |
| `requestId` | Identifier | The request that produced the attempt | `litespec/ext/audit#record-the-request-origin` |
| `traceId` | Identifier | Join key to the diagnostic log | `litespec/ext/audit#link-each-record-to-its-operation-trace` |
| `schemaVersion` | Integer | The shape this record was written in | `litespec/ext/audit#version-the-audit-record-shape` |
| `previousHash` | Hash | The preceding record hash | `litespec/ext/audit#chain-records-to-expose-tampering` |
| `entryHash` | Hash | Hash over this canonical record | `litespec/ext/audit#chain-records-to-expose-tampering` |

The shape leaves out person names, addresses, message bodies, and state snapshots, which is what lets the product meet `litespec/ext/audit#limit-personal-data-to-actor-and-target-identifiers` and `litespec/ext/audit#remove-the-identity-mapping-on-erasure`.

### Declare Which Commands Are Audited

Each command type declares beside itself whether it is audited. An audit definition implements `IAuditDefinition<TCommand>` in the message's `Application/` folder, as `litespec/backend/dotnet/application#bind-the-pipeline-to-the-pinned-litebus-apis` requires. It fixes the values per use case (the action identity, the target kind, and the category). Its `WithTargetFrom` and `WithReasonFrom` selectors name the message fields that carry the target identity and the reason value, so the pipeline records both on every outcome, including a denial.

Which use cases are audited, and why, is decided by the command record's `audit` field. Work that runs asynchronously is audited at request time.

```csharp
internal sealed class AddProductCommandAuditDefinition : IAuditDefinition<AddProductCommand>
{
    public AuditDeclaration Audit =>
        AuditDeclaration.Audited("catalog.add-product", category: "configuration", targetKind: "product")
            .WithTargetFrom<AddProductCommand>(command => command.ProductId.ToString())
            .WithReasonFrom<AddProductCommand>(command => command.Reason);
}

internal sealed class BrowseStorefrontQueryAuditDefinition : IAuditDefinition<BrowseStorefrontQuery>
{
    public AuditDeclaration Audit =>
        AuditDeclaration.Exempt("A public catalog read discloses no tenant-owned data.");
}
```

No command goes unmarked. Add an architecture test that scans every `ICommand` implementation and fails the build when a type has no audit definition. That test implements `litespec/ext/audit#fail-the-build-for-commands-without-an-audit-declaration`.

### Carry Request Context to the Pipeline

Capture the request origin at the HTTP boundary and place it where the pipeline reads it. The endpoint already resolves the actor from verified claims under `litespec/quality/security#derive-the-actor-from-claims`.

Register middleware that reads the connection address, the user agent, and the request identifier. Expose the three through a scoped accessor that the audit producer resolves. This satisfies `litespec/ext/audit#record-the-request-origin` and `litespec/ext/audit#link-each-record-to-its-operation-trace`.

### Open the Audit Scope in a Pre-Handler

Implement `ICommandPreHandler`, the interface LiteBus runs before every command.

Read the declaration from the command type, resolve the actor, the tenant, the clock, and the request context, and put the assembled scope into `IExecutionContext.Items`. The ambient execution context carries those items through every later pipeline stage.

```csharp
internal sealed class OpenAuditScopePreHandler : ICommandPreHandler
{
    private readonly IAuditScopeFactory _factory;

    public OpenAuditScopePreHandler(IAuditScopeFactory factory)
    {
        _factory = factory;
    }

    public Task PreHandleAsync(ICommand message, CancellationToken cancellationToken = default)
    {
        var scope = _factory.CreateFor(message);
        if (scope is not null)
        {
            AmbientExecutionContext.Current.Items[AuditScope.Key] = scope;
        }

        return Task.CompletedTask;
    }
}
```

### Enrich from the Handler

The command handler adds only the values it alone determines, as `litespec/ext/audit#restrict-handler-contribution-to-determined-values` requires. In practice that is two values: a generated identity and a reason value composed from loaded state.

```csharp
var product = Product.Create(ProductId.New(), ...);
productRepository.Store(product);
audit.Target(product.Id);
```

The handler sets no actor, action, timestamp, or outcome. The pipeline owns those.

### Close the Scope as Succeeded

Implement `ICommandPostHandler`. LiteBus runs a post-handler only when the main handler returns, so reaching this stage is itself the success signal.

Stage the audit record in the same session as the business change, as `litespec/ext/audit#commit-a-success-record-with-its-business-change` requires.

The commit needs to run after the audit write, and both post-handlers run in the same stage. Order them with explicit priorities: `HandlerPriorityAttribute` orders handlers by ascending value, and a handler with no attribute takes priority `0`.

```csharp
[HandlerPriority(10)]
internal sealed class WriteAuditRecordPostHandler : ICommandPostHandler { }

[HandlerPriority(20)]
internal sealed class CommitChangesPostHandler : ICommandPostHandler { }
```

Give the audit write the earlier priority explicitly. When both handlers keep the default priority, the order depends on registration order, which `litespec/ext/audit#stage-the-audit-write-before-the-business-commit` rejects.

### Close the Scope as Denied or Failed

Implement `ICommandErrorHandler`, which runs when any stage throws. Map an authorization refusal to `denied` and every other failure to `failed`.

A rollback must not take the evidence with it, so write this record on a connection outside the failing transaction, as `litespec/ext/audit#write-an-unsuccessful-record-outside-the-failed-transaction` requires. Open a second session, write the record, and commit it there.

```csharp
internal sealed class WriteFailedAuditRecordErrorHandler : ICommandErrorHandler
{
    private readonly IAuditFailureWriter _writer;

    public WriteFailedAuditRecordErrorHandler(IAuditFailureWriter writer)
    {
        _writer = writer;
    }

    public async Task HandleErrorAsync(MessageErrorContext<ICommand, object> context, CancellationToken cancellationToken = default)
    {
        var scope = AmbientExecutionContext.Current.Items[AuditScope.Key] as AuditScope;
        if (scope is null)
        {
            _writer.CountMissingScope();
            return;
        }

        var outcome = context.Exception is UseCaseForbiddenException ? AuditOutcome.Denied : AuditOutcome.Failed;
        await _writer.WriteOutOfBandAsync(scope.Complete(outcome, context.Exception), cancellationToken);
    }
}
```

Publish the write failure count of this path, as `litespec/ext/audit#publish-unsuccessful-write-failures` requires, and count a missing scope on the same counter before returning. The separate connection can lose a record when the process dies mid-write, and the counter is what tells that rare loss apart from a defect.

### Protect the Store

Create a database role that holds `INSERT` and `SELECT` on the audit table and holds neither `UPDATE` nor `DELETE`, and write through that role. The database itself then refuses rewrites, which satisfies `litespec/ext/audit#enforce-append-only-through-storage-privilege`.

Hold one hash chain segment per audited category and calendar year, each with its own genesis record and checkpoints, as `litespec/ext/audit#segment-the-audit-chain-by-category-and-calendar-year` requires. Then make tampering visible. Compute `entryHash` over the canonical record content and the preceding `previousHash` within the segment, publish a checkpoint of each live segment's head and record count where the store's operators cannot rewrite it, and run a recurring job that walks each live segment and raises an alert on a break. The chain, the checkpoints, and the job satisfy `litespec/ext/audit#chain-records-to-expose-tampering`, `litespec/ext/audit#anchor-the-audit-chain-outside-its-own-store`, and `litespec/ext/audit#verify-the-integrity-chain-on-a-schedule`.

Delete expired segments through a separate operator process under its own account holding `DELETE`, as `litespec/ext/audit#delete-expired-records-through-a-separate-operator-account` requires. Close each segment with a final checkpoint, delete it as a whole after its category's retention ends, and record the deletion in the following segment of the same category.

### Provide the Read Path

Add a query use case that filters by tenant, actor, action, target, and outcome, and gate it on a permission. This satisfies `litespec/ext/audit#provide-a-use-case-that-reads-the-trail`.

Declare that query audited. Reading the evidence is itself a sensitive action under `litespec/ext/audit#audit-reads-of-the-trail`.

## Check

Check the result against [the audit extension](../ext/audit.md): work through its provisions, and for each one this guide implements, check the evidence the provision names.

Prove three behaviors with tests before review:

- A refused authorization leaves one `denied` record after the transaction rolls back.
- A committed command leaves exactly one `succeeded` record in the same transaction.
- An `UPDATE` against the audit table returns a privilege error.

Record each behavior as a scenario on its use case page, proved by the test that asserts it.
