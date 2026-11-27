# .NET Testing

**Stack:** dotnet

**Concern:** tests

This page states how a .NET backend proves its behavior through tests. It is for developers who write acceptance tests and executable business scenarios. The suite proves reactions through the outbox they travel, and business-readable scenarios run as LightBDD feature classes through a public boundary.

## Concepts

### Durable Messaging in Tests

An acceptance test asserts what a caller observes: the response, a later read, or a reaction's effect. A reaction's effect travels through the outbox, so the test drains the outbox before asserting it. The durable-messaging test package hosts a transactional outbox in the test and drains it on demand, so a scenario observes the reaction's effect without waiting for the processor's polling delay.

In Claimwick, the expense-approval acceptance test sends the approve command, drains the outbox, and then asserts the payout the schedule-payout reaction wrote. Without the drain, the test would assert before the processor delivers the approval. The suite still runs through the real application host.

### Executable Business Scenarios

A business scenario is a C# method on a LightBDD feature class. Its steps are calls to step methods, and its business-readable text is the scenario method, the step method names, and the generated report. LightBDD binds steps at compile time, so a step that no longer exists, or whose parameters changed, fails the build rather than the run.

The project uses business language only where it improves shared understanding of cross-layer behavior: product, domain, engineering, and testing discussions then share one executable example. Routine unit behavior stays in narrower tests, where a failure points at the exact rule in milliseconds.

Each scenario carries one `proves` claim naming the scenario record it proves, written as a mark on the scenario method in the declared style. The example below traces a cancellation scenario:

```csharp
/// <proves>scenario/cancel-order.canceling-twice-is-refused</proves>
```

The claim names the scenario, the steps call the proved use case through a public boundary, and the assertion checks its result. Without the claim, the model builder does not know which scenario the class proves.

### Step Boundaries and Assertions

Each step calls the HTTP API or another declared public system boundary. A step reaching past the boundary depends on implementation detail, so a refactoring breaks the scenario without breaking the behavior. Each step definition translates its business phrase into a typed test-driver call, keeping HTTP mechanics, JSON shapes, and assertion mechanics in the driver while the scenario stays readable. Step definitions resolve no repositories, handlers, sessions, or the production service provider.

Each scenario asserts an observable outcome: a response, a later read, a durable event, an audit record, or another observable outcome. A run asserting nothing proves only that nothing threw. Direct database assertions appear only for a documented durable boundary, such as an outbox row, where the stored row is itself the promised evidence.

### Scenario Ownership and Isolation

Each scenario owns its identifiers, actor context, requests, responses, and expected state. A scenario borrowing identifiers or state prepared elsewhere breaks when that other setup changes. Owning everything keeps each scenario self-contained and runnable alone.

The suite resets database state between scenarios and avoids order dependencies. State left by one scenario becomes a hidden precondition of the next, and the suite then passes only in one order. Hooks await asynchronous setup and cleanup without replacing the original scenario failure, so the evidence stays trustworthy.

### Suite Layout

Feature classes group under `{Module}/{Aggregates}/{UseCase}/` folders in `Acceptance.Tests`, with a module-level read dropping the aggregate segment. A scenario filed by business module and aggregate sits beside the vocabulary it exercises, so a reader finds every example of one aggregate in one folder.

Reusable step definitions stay narrow and serve one business vocabulary. A step reused across vocabularies, such as a generic success step, means something subtly different in each place and couples unrelated modules to one definition. Each scenario context object holds only scenario state; resolving services from it turns it into a service locator hiding what the scenario depends on.

The small pull-request subset carries a critical test trait on the LightBDD scenario, separate from the `proves` claim. The trait selects the small subset whose failure stops a release, for fast pull-request feedback. The subset keeps a stated size bound and an owner keeping it within that bound.

Scenarios run on the LightBDD runner with `xunit.v3.mtp-v2`, each pinned in the manifest. LightBDD 3.12 builds against the xUnit v3 3.2 line, so the acceptance test project runs the 3.2 line while other test projects run the current one.

### Persistence Snapshot Harness

A .NET consumer proves its stored documents still read by keeping one snapshot per release and a small console project that reads and writes them. On record the harness serializes one canonical instance of each stored aggregate root and outbox envelope through the product serializer and writes the snapshots; on check it deserializes every snapshot and prints one JSON object per line with the snapshot file, whether it reads, and why not.

In Claimwick, the harness holds one submitted expense and one payout outbox envelope. After the team changes how the expense stores its amount, the check run prints a failed line for the earlier release, and `litespec verify persistence-compat` reports the snapshot as incompatible until the team adds the converter that reads the earlier shape.
The harness prints only the JSON lines, one object per line, such as `{"file": ".litespec/proof/persistence-snapshots/v1/expense.json", "status": "ok", "message": ""}`. It serializes through the product serializer, not a test copy, and it holds one canonical instance of each stored document, so a passing check proves the current code reads what the earlier release stored.

## Standards

None.

## Conventions

None.
