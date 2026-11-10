namespace Library.Application.Lending.Technical;

/// <follows>departure/export-review-bypass</follows>
internal sealed class ExportStep
{
    public Task ExportAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
