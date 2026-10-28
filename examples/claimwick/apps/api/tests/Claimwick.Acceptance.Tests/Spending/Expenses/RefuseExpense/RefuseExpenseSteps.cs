using System.Net;
using Claimwick.Acceptance.Tests.Support;

namespace Claimwick.Acceptance.Tests.Spending.Expenses.RefuseExpense;

public sealed class RefuseExpenseSteps
{
    public async Task RejectAnExpenseAsync(string expense)
    {
        ExpensesWorld.SignInAs("manager-1");
        await ExpensesWorld.PostAsync($"/api/expenses/{ExpensesWorld.IdOf(expense)}:refuse", new
        {
            approverId = ExpensesWorld.IdOf("manager-1"),
            reason = "Personal purchase.",
        });
    }

    public Task TheExpenseIsRefusedAsync()
    {
        Assert.Equal(HttpStatusCode.OK, ExpensesWorld.LastResponse!.StatusCode);
        return Task.CompletedTask;
    }
}
