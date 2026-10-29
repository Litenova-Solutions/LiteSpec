namespace Library.Application.Lending.Technical;

/// <follows>decision/0004-legacy-loan-export</follows>
internal sealed class LegacyExporter
{
    public Task ExportAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
