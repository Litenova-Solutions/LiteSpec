using System.Net;
using System.Net.Http.Json;
using Claimwick.Acceptance.Tests.Support;

namespace Claimwick.Acceptance.Tests.Spending.Expenses.ExportExpenses;

public sealed class ExportExpensesSteps
{
    public async Task ExportApprovedExpensesAsync()
    {
        await ExpensesWorld.PostAsync("/api/expenses:export");
    }

    public Task TheExpensesAreExportedAsync()
    {
        Assert.Equal(HttpStatusCode.OK, ExpensesWorld.LastResponse!.StatusCode);
        return Task.CompletedTask;
    }

    public async Task TheExportIsRefusedAsNothingApprovedAsync()
    {
        Assert.Equal(HttpStatusCode.UnprocessableEntity, ExpensesWorld.LastResponse!.StatusCode);
        var problem = await ExpensesWorld.LastResponse!.Content.ReadFromJsonAsync<RefusalProblem>();
        Assert.Equal("spending.nothing-approved", problem!.Code);
    }

    private sealed record RefusalProblem(string Code);
}
