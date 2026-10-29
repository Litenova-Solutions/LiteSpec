namespace Library.Application.Lending.Technical;

/// <follows>deviation/export-review-bypass</follows>
internal sealed class ExportStep
{
    public Task ExportAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
