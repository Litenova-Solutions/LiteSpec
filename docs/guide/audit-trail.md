# Build an Audit Trail

Build an audit trail on the LiteBus pipeline that runs each state change in the .NET backend: the record shape, the pipeline handlers, the protected store, and the read path. The trail proves who attempted what against which data and what each attempt produced, so the product can answer for every sensitive action. Each step names the audit rule it satisfies, and this guide adds no rule of its own.

## Prerequisites

Start with:

- The `audit` extension selected in the consumer's `.litespec/project.yaml`.
- The audit adoption decision that `litespec/core/audit#record-the-audit-adoption-decision` requires.
- The `audit` field on each command record. The record schema requires it with no default: a level of `required`, `optional`, or `none` with its target kind, or `exemptBecause` with its justification.
- A LiteBus command pipeline that commits once for each command.
- A database account reserved for audit writes, separate from the account that writes business data.
- The LiteBus packages pinned for the `LiteBus.*` packages in `litespec.manifest.yaml`.
- The pipeline reference in the `site/content/docs` folder of the [LiteBus 8.0.0-preview.8 tag](https://github.com/litenova/LiteBus/tree/v8.0.0-preview.8/site/content/docs). It defines the handler interfaces this guide implements.

Read the [audit extension](../core/audit.md) before this guide. It states the obligations, and this guide shows one compliant way to meet them.

## Procedure

1. Define the record shape. Model the audit record once, using the fields the [audit extension](../core/audit.md) requires. Each row below names what the field holds.

   Name what happened and when it was recorded:

   | Field | Type | Holds |
   |:---|:---|:---|
   | `auditId` | UUID v7 | Record identity, ordered by write time |
   | `occurredAt` | Timestamp with zone | Business time of the attempt |
   | `recordedAt` | Timestamp with zone | Time the evidence was written |
   | `action` | Use case identity | The invoked use case |
   | `category` | Closed set | Retention and review classification |
   | `outcome` | Closed set | `succeeded`, `denied`, or `failed` |
   | `failureCode` | Stable code | The refusal or failure reason |

   Name who acted and whose data it touched:

   | Field | Type | Holds |
   |:---|:---|:---|
   | `actorKind` | Closed set | Person, device, or named process |
   | `actorId` | Pseudonymous identifier | The acting account |
   | `actorDeviceId` | Pseudonymous identifier | The acting device |
   | `actorProcess` | Name | The acting scheduled process |
   | `underGrant` | Boolean | The actor used a support grant |
   | `tenantId` | Identifier | The tenant that owns the data |
   | `targetKind` | Name | The kind of thing acted on |
   | `targetId` | Identifier | Its identity |

   Name where it came from and how the chain links:

   | Field | Type | Holds |
   |:---|:---|:---|
   | `reason` | Bounded text | Why, where the action requires one |
   | `sourceIp` | Address | Request origin |
   | `userAgent` | Bounded text | The calling client |
   | `requestId` | Identifier | The request that produced the attempt |
   | `traceId` | Identifier | Join key to the diagnostic log |
   | `schemaVersion` | Integer | The shape this record was written in |
   | `previousHash` | Hash | The preceding record hash |
   | `entryHash` | Hash | Hash over this canonical record |

   The shape leaves out person names, addresses, message bodies, and state snapshots. Every attempt records its outcome, as `litespec/core/audit#record-every-attempt-outcome` requires, and each record carries its tenant, as `litespec/core/audit#scope-every-record-to-its-tenant` requires. For example, a refused Claimwick payout records one `denied` row keyed to the tenant that owns the expense, with no member name in it.
2. Declare which commands are audited. Each command type declares beside itself whether it is audited, as `litespec/core/audit#declare-which-commands-are-audited` requires. An audit definition implements `IAuditDefinition<TCommand>` in the message's `Application/` folder. It fixes the values per use case (the action identity, the target kind, and the category). Its `WithTargetFrom` and `WithReasonFrom` selectors name the message fields that carry the target identity and the reason value, so the pipeline records both on every outcome, including a denial.

   Which use cases are audited, and why, is decided by the command record's `audit` field, as `litespec/core/audit#audit-only-selected-commands` requires. Work that runs asynchronously is audited at request time.

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

   No command goes unmarked. Add an architecture test that scans every `ICommand` implementation and fails the build when a type has no audit definition. For example, a new Claimwick command without a definition fails the build until its author declares it audited or exempt.
3. Carry request context to the pipeline. Capture the request origin at the HTTP boundary and place it where the pipeline reads it. The endpoint already resolves the actor from verified claims. Register middleware that reads the connection address, the user agent, and the request identifier. Expose the three through a scoped accessor that the audit producer resolves. For example, the Claimwick submit endpoint resolves the employee from the verified token, and the middleware adds the connection address beside it.
4. Open the audit scope in a pre-handler. Implement `ICommandPreHandler`, the interface LiteBus runs before every command. Read the declaration from the command type, resolve the actor, the tenant, the clock, and the request context, and put the assembled scope into `IExecutionContext.Items`. The ambient execution context carries those items through every later pipeline stage.

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
5. Enrich from the handler. The command handler adds only the values it alone determines. In practice that is two values: a generated identity and a reason value composed from loaded state.

   ```csharp
   var product = Product.Create(ProductId.New(), ...);
   productRepository.Store(product);
   audit.Target(product.Id);
   ```

   The handler sets no actor, action, timestamp, or outcome. The pipeline owns those. For example, the Claimwick submit handler records the new expense identity and the receipt reason, and nothing else.
6. Close the scope as succeeded. Implement `ICommandPostHandler`. LiteBus runs a post-handler only when the main handler returns, so reaching this stage is itself the success signal. Stage the audit record in the same session as the business change, as `litespec/core/audit#commit-a-success-record-with-its-business-change` requires. The commit runs after the audit write, and both post-handlers run in the same stage. Order them with explicit priorities: `HandlerPriorityAttribute` orders handlers by ascending value, and a handler with no attribute takes priority `0`.

   ```csharp
   [HandlerPriority(10)]
   internal sealed class WriteAuditRecordPostHandler : ICommandPostHandler { }

   [HandlerPriority(20)]
   internal sealed class CommitChangesPostHandler : ICommandPostHandler { }
   ```

   Give the audit write the earlier priority explicitly. When both handlers keep the default priority, the order depends on registration order, which the [audit extension](../core/audit.md) rejects. For example, the Claimwick submit writes its `succeeded` row before the expense row commits, so the two land together.
7. Close the scope as denied or failed. Implement `ICommandErrorHandler`, which runs when any stage throws. Map an authorization refusal to `denied` and every other failure to `failed`, as `litespec/core/audit#record-refused-authorization` requires. A rollback does not take the evidence with it, so write this record on a connection outside the failing transaction, as `litespec/core/audit#write-an-unsuccessful-record-outside-the-failed-transaction` requires. Open a second session, write the record, and commit it there.

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

   Publish the write failure count of this path, and count a missing scope on the same counter before returning. The separate connection can lose a record when the process dies mid-write, and the counter is what tells that rare loss apart from a defect. For example, a refused Claimwick payout leaves one `denied` row after the expense transaction rolls back.
8. Protect the store. Create a database role that holds `INSERT` and `SELECT` on the audit table and holds neither `UPDATE` nor `DELETE`, and write through that role. The database itself then refuses rewrites, as `litespec/core/audit#enforce-append-only-through-storage-privilege` requires.

   Hold one hash chain segment per audited category and calendar year, each with its own genesis record and checkpoints, as `litespec/core/audit#segment-the-audit-chain-by-category-and-calendar-year` requires. Then make tampering visible. Compute `entryHash` over the canonical record content and the preceding `previousHash` within the segment, publish a checkpoint of each live segment's head and record count where the store's operators cannot rewrite it, and run a recurring job that walks each live segment and raises an alert on a break.

   Delete expired segments through a separate operator process under its own account holding `DELETE`, as `litespec/core/audit#delete-expired-records-through-a-separate-operator-account` requires. Close each segment with a final checkpoint, delete it as a whole after its category's retention ends, and record the deletion in the following segment of the same category, as `litespec/core/audit#record-each-segment-deletion-in-the-following-segment` requires. For example, the Claimwick payout rows chain within their yearly segment, and last year's closed segment deletes whole once its retention ends.
9. Provide the read path. Add a query use case that filters by tenant, actor, action, target, and outcome, and gate it on a permission. For example, a support engineer filters the Claimwick trail by tenant and action to answer what happened to one expense. Declare that query audited. Reading the evidence is itself a sensitive action.

## Check

Check the result against [the audit extension](../core/audit.md): work through its provisions, and for each one this guide implements, check the evidence the provision names.

Prove three behaviors with tests before review:

- A refused authorization leaves one `denied` record after the transaction rolls back.
- A committed command leaves exactly one `succeeded` record in the same transaction.
- An `UPDATE` against the audit table returns a privilege error.

Record each behavior as a scenario on its use case page, proved by the test that asserts it.
