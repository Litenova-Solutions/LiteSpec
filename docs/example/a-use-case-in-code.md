# A Use Case in Code

Submit expense is the Claimwick use case that files a work expense so it can be decided and reimbursed. This page traces it from its record into the .NET code and the acceptance tests that prove it, so you can see each link in the chain in one sitting. The record is `examples/claimwick/.litespec/behavior/modules/spending/aggregates/expenses/use-cases/submit-expense/submit-expense.yaml`, and the code and tests are in `examples/claimwick/apps/api/`. Every file path and line of code below is copied from that folder.

## The Record

The use case record names what the use case does and what it touches: the grant, the flow step it serves, the entry point that reaches it, the domain event it raises, the aggregate it writes, and the policy it reads:

```yaml
type: use-case
id: use-case/spending.submit-expense
title: Submit Expense
summary: An employee files a work expense so it can be decided and reimbursed.
exposure: Any signed-in employee reaches this command through the expenses app under the expense-request permission. A bad submission creates review work for managers, but nothing is paid before a decision.
kind: command
access:
  grant: permission/request-expense
  tenant: own
serves:
- flow/submit-and-reimburse#s1
fulfills:
- requirement/approval.above-threshold-receipt
reachedThrough:
- entry-point/claimwick-app
raises:
- domain-event/expense.submitted
writes:
- aggregate/spending.expense
reads:
- aggregate/spending.policy
```

The file also declares the input, the idempotency, and the results. The input carries a `requestId` the app mints for each submission attempt, the amount, the currency, and an optional receipt. Idempotency is `key` on `{requestId}`, scoped to the account, with `returns-first-result` replay: a retried request returns the first result instead of minting a second expense.

The two results are `result/submit-expense.submitted`, a success, and `result/submit-expense.receipt-missing`, a refusal with the code `spending.receipt-missing` from the spending refusal set. The file has two scenarios, and the tests below prove them.

## The Rule Behind the Refusal

One rule stands behind the refusal: `rule/expense.receipt-required-above-threshold` states that an expense above the policy threshold needs a receipt, and it names the refusal code `spending.receipt-missing`. In code, the rule appears twice, as the exception that carries its stable code and as the check in the aggregate that raises it.

The exception is `apps/api/src/Claimwick.Modules/Spending/Expenses/Domain/Exceptions/ExpenseReceiptMissingException.cs`, marked with `<implements>rule/expense.receipt-required-above-threshold</implements>`. It returns the code `spending.receipt-missing`.

The check is the static factory `Expense.Submit` in `apps/api/src/Claimwick.Modules/Spending/Expenses/Domain/Expense.cs`:

```csharp
public static Expense Submit(Guid id, Guid employeeId, Money total, Guid? receiptId, Money receiptThreshold)
{
    if (receiptId is null && total.Amount > receiptThreshold.Amount)
    {
        throw new ExpenseReceiptMissingException();
    }

    var expense = new Expense(id, employeeId, total, receiptId);
    expense.RaiseDomainEvent(new ExpenseSubmittedEvent(id, total.Amount, total.Currency, employeeId));
    return expense;
}
```

The aggregate refuses the expense before it exists, and raises `ExpenseSubmittedEvent` only when the expense is valid. The `<implements>use-case/spending.submit-expense</implements>` tag on the same method ties that code to the record.

## The Endpoint

The HTTP endpoint is `apps/api/src/Claimwick.Modules/Spending/Expenses/UseCases/SubmitExpense/WebApi/SubmitExpenseEndpoint.cs`. It maps `POST /api/expenses`, returns 201 Created on success, and returns a problem result with status 422 on a refusal.

The submitter is not read from the body: the endpoint reads the employee from the verified claims of the signed-in user, which applies `litespec/backend/application#enforce-target-authorization-in-the-pipeline`:

```csharp
// The submitter is the signed-in employee, read from verified claims, never from the request body.
var employeeId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
var result = await mediator.SendAsync(
    new SubmitExpenseCommand(Guid.CreateVersion7(), employeeId, request.Amount, request.Currency, request.ReceiptId),
    cancellationToken);
```

The request body is `SubmitExpenseRequestModel` in the use case `WebApi/` folder, holding the amount, the currency, and the optional receipt identifier. The response is `SubmitExpenseResponseModel`, holding the new expense identity. Transport models stay apart from messages: `Application/` holds the command, the result, and the handler, while `WebApi/` holds the endpoint and the request and response models.

## The Handler

The handler is `apps/api/src/Claimwick.Modules/Spending/Expenses/UseCases/SubmitExpense/Application/SubmitExpenseCommandHandler.cs`. It loads the active policy through `IPolicyRepository`, submits the expense with the policy's threshold, stores the expense through `IExpenseRepository`, and returns the identity:

```csharp
var policy = await policies.GetActiveAsync(cancellationToken);
var expense = Expense.Submit(
    command.ExpenseId,
    command.EmployeeId,
    Money.Create(command.Amount, command.Currency),
    command.ReceiptId,
    policy.ReceiptThreshold);
expenses.Store(expense);
return new SubmitExpenseCommandResult(expense.Id);
```

The handler is marked `<implements>use-case/spending.submit-expense</implements>`. It holds no mediator and publishes no event. `Expense.Submit` raises `ExpenseSubmittedEvent`, and the pipeline stages the event with the expense. The repository interfaces it uses are in `Expenses/Domain/IExpenseRepository.cs` and `Policies/Domain/IPolicyRepository.cs`.

## The Acceptance Tests

Each scenario in the record gets one test method, and each test names the scenario it proves in a `<proves>` tag. The tests live in `apps/api/tests/Claimwick.Acceptance.Tests/Spending/Expenses/UseCases/SubmitExpense/SubmitExpenseScenarios.cs`:

```csharp
/// <proves>scenario/submit-expense.submitting-with-a-receipt-succeeds</proves>
[Fact]
public Task Submitting_with_a_receipt_succeedsAsync() => Runner.RunScenarioAsync(
    when => new SubmitExpenseSteps().SubmitAnExpenseAsync("employee-1"),
    then => new SubmitExpenseSteps().TheExpenseIsSubmittedAsync());

/// <proves>scenario/submit-expense.submitting-without-a-receipt-is-refused</proves>
[Fact]
public Task Submitting_without_a_receipt_is_refusedAsync() => Runner.RunScenarioAsync(
    when => new SubmitExpenseSteps().SubmitAnExpenseWithoutAReceiptAsync("employee-1"),
    then => new SubmitExpenseSteps().TheExpenseIsRefusedAsReceiptMissingAsync());
```

The steps live in `SubmitExpenseSteps.cs` beside them. The refusal step asserts the 422 status and the code `spending.receipt-missing`. The tests sit under `apps/api/tests`, which the project file declares as `paths.testRoots`.

## What the Model Sees

[How LiteSpec Works](../learn/how-litespec-works.md) summarizes this chain in its use-case trace; this section works it in full.

Querying the use case shows the links the builder derived from these files. The output below comes from `dotnet run --project apps/cli/src/LiteSpec.Cli -- model query use-case/spending.submit-expense --root examples/claimwick`, shortened where it says so:

```json
"computed": {
  "completion": {
    "stage": "coded",
    "proof": "unknown",
    "counts": {
      "code found": { "done": 1, "of": 1 },
      "scenarios verified": { "done": 0, "of": 2 }
    }
  },
  "implements": [
    "apps/api/src/Claimwick.Modules/Spending/Expenses/Domain/Expense.cs",
    "apps/api/src/Claimwick.Modules/Spending/Expenses/UseCases/SubmitExpense/Application/SubmitExpenseCommandHandler.cs",
    "apps/api/src/Claimwick.Modules/Spending/Expenses/UseCases/SubmitExpense/WebApi/SubmitExpenseEndpoint.cs"
  ],
  "proves": [
    {
      "claim": "scenario/submit-expense.submitting-with-a-receipt-succeeds",
      "confirmed": true,
      "passing": null
    },
    ...
  ]
}
```

The record reads `coded`, because its handler and endpoint exist. It does not read `verified`, because the example holds no passing run for its two scenarios: `passing` is null for each, and `scenarios verified` counts 0 of 2. Nobody types the stage into a status field; the model computes it from these links and runs.

The `proves` links are confirmed, which means each test names its scenario. A passing result is a separate fact, and the example has none yet. [The Model and Its Checks](model-and-checks.md) explains the waiver that covers the missing proof.
