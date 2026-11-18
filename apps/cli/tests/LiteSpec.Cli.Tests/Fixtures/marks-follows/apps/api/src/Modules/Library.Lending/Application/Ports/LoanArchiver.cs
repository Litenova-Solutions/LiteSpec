namespace Library.Lending.Application.Ports;

/// <follows>litespec/frontend/web/svelte/stack#write-components-in-runes-mode</follows>
internal sealed class LoanArchiver
{
    public Task ArchiveAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
