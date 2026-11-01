namespace Library.Application.Lending.Technical;

/// <follows>litespec/frontend/blazor/browser#bound-what-is-stored</follows>
internal sealed class LoanArchiver
{
    public Task ArchiveAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
