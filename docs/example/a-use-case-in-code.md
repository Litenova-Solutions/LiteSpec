# A Use Case in Code

## Intent

This page follows one use case, Submit expense, from its record into the .NET code and the acceptance tests that prove it. The record is `examples/claimwick/.litespec/behavior/modules/spending/expenses/submit-expense.yaml`, and the code and tests are in `examples/claimwick/apps/api/`. Every file path and line of code below is copied from that folder, so a reader can check the chain from record to test in one sitting.

## The Record

The use case names what it does and what it touches. Its file records the grant, the journey step it serves, the entry point that reaches it, the event it raises, the aggregate it changes and the policy it reads:

```yaml
type: use-case
id: use-case/spending.submit-expense
title: Submit Expense
summary: An employee files a work expense so it can be decided and reimbursed.
kind: command
access:
  grant: permission/submit-expense
  tenant: own
serves:
- journey/submit-and-reimburse#s1
fulfills:
- requirement/approval.receipt-over-threshold
reachedThrough:
- entry-point/claimwick-app
raises:
- event/expense.submitted
changes:
- aggregate/spending.expense
reads:
- aggregate/spending.policy
```

The file also declares the input, the idempotency and the results. The input is an amount, a currency and an optional receipt. The idempotency mode is `none`, because each request mints a fresh expense identity. The two results are `result/submit-expense.submitted`, a success, and `result/submit-expense.receipt-missing`, a refusal with the code `spending.receipt-missing`. The file has two scenarios, and the tests below prove them.

## The Rule Behind the Refusal

The refusal answers one rule. The rule `rule/expense.receipt-required-above-threshold` states that an expense above the policy threshold needs a receipt, and it names the refusal `result/submit-expense.receipt-missing`. In code, the rule appears twice: as the exception that carries its stable code, and as the check in the aggregate that raises it.

The exception is `apps/api/src/Claimwick.Modules/Spending/Expenses/Domain/Exceptions/ExpenseReceiptMissingException.cs`. It is marked with `<implements>rule/expense.receipt-required-above-threshold</implements>` and returns the code `spending.receipt-missing`.

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

The aggregate refuses the expense before it exists, and it raises `ExpenseSubmittedEvent` only when the expense is valid. The `<implements>use-case/spending.submit-expense</implements>` tag on the same method ties the code to the record.

## The Endpoint

The HTTP endpoint is `apps/api/src/Claimwick.Modules/Spending/Expenses/UseCases/SubmitExpense/SubmitExpenseEndpoint.cs`. It maps `POST /api/expenses`, returns 201 Created on success, and returns a problem result with status 422 on a refusal. The submitter is not read from the body. The endpoint reads the employee from the verified claims of the signed-in user, which applies `litespec/backend/application#enforce-target-authorization-in-the-pipeline`:

```csharp
// The submitter is the signed-in employee, read from verified claims, never from the request body.
var employeeId = Guid.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);
var result = await mediator.SendAsync(
    new SubmitExpenseCommand(Guid.CreateVersion7(), employeeId, request.Amount, request.Currency, request.ReceiptId),
    cancellationToken);
```

The request body is `SubmitExpenseRequestModel`, which holds the amount, the currency and the optional receipt identifier. The response is `SubmitExpenseResponseModel`, which holds the new expense identity. The layer keeps its own types, so the command, the result and the request and response models are separate records in the same folder.

## The Handler

The handler is `apps/api/src/Claimwick.Modules/Spending/Expenses/UseCases/SubmitExpense/SubmitExpenseCommandHandler.cs`. It loads the active policy through `IPolicyRepository`, submits the expense with the policy's threshold, stores it through `IExpenseRepository`, and returns the identity:

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

The handler is marked `<implements>use-case/spending.submit-expense</implements>`. The repository interfaces it uses are in `Expenses/Domain/IExpenseRepository.cs` and `Policies/Domain/IPolicyRepository.cs`.

## The Acceptance Tests

Each scenario in the record has one test method, and the test names the scenario it proves in a `<proves>` tag. The scenarios are in `apps/api/tests/Claimwick.Acceptance.Tests/Spending/Expenses/UseCases/SubmitExpense/SubmitExpenseScenarios.cs`:

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

The steps are in `SubmitExpenseSteps.cs` beside them. The refusal step asserts the 422 status and the code `spending.receipt-missing`. The tests live under `apps/api/tests`, which the project file declares as `paths.testRoots`.

## What the Model Sees

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
    "apps/api/src/Claimwick.Modules/Spending/Expenses/UseCases/SubmitExpense/SubmitExpenseCommandHandler.cs",
    "apps/api/src/Claimwick.Modules/Spending/Expenses/UseCases/SubmitExpense/SubmitExpenseEndpoint.cs"
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

The record reads `coded`, because its handler and endpoint exist. It does not read `verified`, because the example holds no passing run for its two scenarios: `passing` is null for each, and `scenarios verified` counts 0 of 2. The stage is computed from these links and runs, not typed into a status field.

The `proves` links are confirmed, which means each test names its scenario. A passing result is a separate fact. [The Model and Its Checks](model-and-checks.md) explains the departure that covers the missing proof.
