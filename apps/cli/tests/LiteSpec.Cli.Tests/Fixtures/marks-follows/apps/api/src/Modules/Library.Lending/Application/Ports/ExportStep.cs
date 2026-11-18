namespace Library.Lending.Application.Ports;

/// <follows>waiver/export-review-bypass</follows>
internal sealed class ExportStep
{
    public Task ExportAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
