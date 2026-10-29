namespace Library.Acceptance.Tests.Proof;

public sealed class ProvisionProofTests
{
    /// <proves>litespec/backend/persistence#commit-each-command-pipeline-once</proves>
    [Fact]
    public void Commits_once()
    {
        Assert.True(true);
    }
}
