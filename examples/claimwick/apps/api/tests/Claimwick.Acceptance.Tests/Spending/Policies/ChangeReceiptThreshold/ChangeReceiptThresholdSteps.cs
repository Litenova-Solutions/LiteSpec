using System.Net;
using System.Net.Http.Json;
using Claimwick.Acceptance.Tests.Support;

namespace Claimwick.Acceptance.Tests.Spending.Policies.ChangeReceiptThreshold;

public sealed class ChangeReceiptThresholdSteps
{
    public async Task SetTheThresholdAsync(decimal threshold)
    {
        await ExpensesWorld.PostAsync("/api/policy:change-receipt-threshold", new { threshold, currency = "EUR" });
    }

    public Task TheThresholdIsSetAsync()
    {
        Assert.Equal(HttpStatusCode.OK, ExpensesWorld.LastResponse!.StatusCode);
        return Task.CompletedTask;
    }

    public async Task TheThresholdIsRefusedAsNegativeAsync()
    {
        Assert.Equal(HttpStatusCode.UnprocessableEntity, ExpensesWorld.LastResponse!.StatusCode);
        var problem = await ExpensesWorld.LastResponse!.Content.ReadFromJsonAsync<RefusalProblem>();
        Assert.Equal("spending.negative-threshold", problem!.Code);
    }

    private sealed record RefusalProblem(string Code);
}
