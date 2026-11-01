using System.Net;
using System.Net.Http.Json;
using Claimwick.Acceptance.Tests.Support;

namespace Claimwick.Acceptance.Tests.Spending.Expenses.ApproveExpense;

public sealed class ApproveExpenseSteps
{
    public async Task ApproveAnExpenseAsync(string expense)
    {
        ExpensesWorld.SignInAs("manager-1");
        await ExpensesWorld.PostAsync($"/api/expenses/{ExpensesWorld.IdOf(expense)}:approve", new { approverId = ExpensesWorld.IdOf("manager-1") });
    }

    public Task TheExpenseIsApprovedAsync()
    {
        Assert.Equal(HttpStatusCode.OK, ExpensesWorld.LastResponse!.StatusCode);
        return Task.CompletedTask;
    }

    public async Task TheExpenseIsRefusedAsOwnExpenseAsync()
    {
        Assert.Equal(HttpStatusCode.UnprocessableEntity, ExpensesWorld.LastResponse!.StatusCode);
        var problem = await ExpensesWorld.LastResponse!.Content.ReadFromJsonAsync<RefusalProblem>();
        Assert.Equal("spending.own-expense", problem!.Code);
    }

    public async Task TheExpenseIsRefusedAsNotSubmittedAsync()
    {
        Assert.Equal(HttpStatusCode.UnprocessableEntity, ExpensesWorld.LastResponse!.StatusCode);
        var problem = await ExpensesWorld.LastResponse!.Content.ReadFromJsonAsync<RefusalProblem>();
        Assert.Equal("spending.not-submitted", problem!.Code);
    }

    private sealed record RefusalProblem(string Code);
}
