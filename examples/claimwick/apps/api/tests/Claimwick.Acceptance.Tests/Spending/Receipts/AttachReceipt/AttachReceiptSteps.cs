using System.Net;
using System.Net.Http.Json;
using Claimwick.Acceptance.Tests.Support;

namespace Claimwick.Acceptance.Tests.Spending.Receipts.AttachReceipt;

public sealed class AttachReceiptSteps
{
    public async Task AttachAReceiptAsync(string expense)
    {
        await ExpensesWorld.PostAsync($"/api/expenses/{ExpensesWorld.IdOf(expense)}/receipts", new { receiptId = ExpensesWorld.IdOf("receipt-2") });
    }

    public Task TheReceiptIsAttachedAsync()
    {
        Assert.Equal(HttpStatusCode.Created, ExpensesWorld.LastResponse!.StatusCode);
        return Task.CompletedTask;
    }

    public async Task TheReceiptIsRefusedAsExpenseDecidedAsync()
    {
        Assert.Equal(HttpStatusCode.UnprocessableEntity, ExpensesWorld.LastResponse!.StatusCode);
        var problem = await ExpensesWorld.LastResponse!.Content.ReadFromJsonAsync<RefusalProblem>();
        Assert.Equal("spending.expense-decided", problem!.Code);
    }

    private sealed record RefusalProblem(string Code);
}
