namespace Library.Lending.Application.Ports;

/// <follows>litespec/backend/no-such-page#no-such-provision</follows>
internal sealed class LoanExporter
{
    public Task ExportAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
