using System.Net;
using System.Net.Http.Json;
using Claimwick.Acceptance.Tests.Support;

namespace Claimwick.Acceptance.Tests.Spending.Expenses.SubmitExpense;

public sealed class SubmitExpenseSteps
{
    public async Task SubmitAnExpenseAsync(string employee)
    {
        ExpensesWorld.SignInAs(employee);
        await ExpensesWorld.PostAsync("/api/expenses", new
        {
            amount = 48.90m,
            currency = "EUR",
            receiptId = ExpensesWorld.IdOf("receipt-1"),
        });

        var created = await ExpensesWorld.LastResponse!.Content.ReadFromJsonAsync<SubmittedExpense>();
        ExpensesWorld.Name("expense-1", created!.ExpenseId);
    }

    public async Task SubmitAnExpenseWithoutAReceiptAsync(string employee)
    {
        ExpensesWorld.SignInAs(employee);
        await ExpensesWorld.PostAsync("/api/expenses", new
        {
            amount = 48.90m,
            currency = "EUR",
        });
    }

    public Task TheExpenseIsSubmittedAsync()
    {
        Assert.Equal(HttpStatusCode.Created, ExpensesWorld.LastResponse!.StatusCode);
        return Task.CompletedTask;
    }

    public async Task TheExpenseIsRefusedAsReceiptMissingAsync()
    {
        Assert.Equal(HttpStatusCode.UnprocessableEntity, ExpensesWorld.LastResponse!.StatusCode);
        var problem = await ExpensesWorld.LastResponse!.Content.ReadFromJsonAsync<RefusalProblem>();
        Assert.Equal("spending.receipt-missing", problem!.Code);
    }

    private sealed record SubmittedExpense(Guid ExpenseId);

    private sealed record RefusalProblem(string Code);
}
