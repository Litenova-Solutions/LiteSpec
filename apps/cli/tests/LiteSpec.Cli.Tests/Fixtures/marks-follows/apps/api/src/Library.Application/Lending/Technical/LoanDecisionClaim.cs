namespace Library.Application.Lending.Technical;

/// <implements>decision/0002-store-loans-as-documents</implements>
internal sealed class LoanDecisionClaim
{
    public Task CheckAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
