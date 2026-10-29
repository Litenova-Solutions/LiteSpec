namespace Library.Application.Lending.Technical;

/// <follows>litespec/backend/persistence#commit-each-command-pipeline-once</follows>
/// <follows>decision/0002-store-loans-as-documents</follows>
internal sealed class CommitStep
{
    public Task CommitAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
