using System.Net;
using System.Net.Http.Json;
using Claimwick.Acceptance.Tests.Support;

namespace Claimwick.Acceptance.Tests.Spending.Expenses.ReimburseExpense;

public sealed class ReimburseExpenseSteps
{
    public async Task ReimburseAnExpenseAsync(string expense)
    {
        await ExpensesWorld.PostAsync($"/api/expenses/{ExpensesWorld.IdOf(expense)}:reimburse");
    }

    public Task TheExpenseIsReimbursedAsync()
    {
        Assert.Equal(HttpStatusCode.OK, ExpensesWorld.LastResponse!.StatusCode);
        return Task.CompletedTask;
    }

    public async Task TheExpenseIsRefusedAsNotApprovedAsync()
    {
        Assert.Equal(HttpStatusCode.UnprocessableEntity, ExpensesWorld.LastResponse!.StatusCode);
        var problem = await ExpensesWorld.LastResponse!.Content.ReadFromJsonAsync<RefusalProblem>();
        Assert.Equal("spending.not-approved", problem!.Code);
    }

    public async Task TheExpenseIsRefusedAsAlreadyReimbursedAsync()
    {
        Assert.Equal(HttpStatusCode.UnprocessableEntity, ExpensesWorld.LastResponse!.StatusCode);
        var problem = await ExpensesWorld.LastResponse!.Content.ReadFromJsonAsync<RefusalProblem>();
        Assert.Equal("spending.already-reimbursed", problem!.Code);
    }

    private sealed record RefusalProblem(string Code);
}
