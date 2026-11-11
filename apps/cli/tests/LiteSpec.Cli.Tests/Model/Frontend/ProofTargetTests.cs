using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;
using LiteSpec.Standards;

namespace LiteSpec.Cli.Tests.Model.Frontend;

/// <summary>Proves the records file lets a browser test prove a frontend provision, and only those.</summary>
public sealed class ProofTargetTests
{
    /// <summary>Proof Targets Hold Frontend Provisions And No Other Provision.</summary>
    /// <proves>litespec/frontend/web/structure#generate-the-records-file-into-the-frame</proves>
    [Fact]
    public void ProofTargetsHoldFrontendProvisionsAndNoOtherProvision()
    {
        using var temp = TestCli.Temp("proof-targets");
        Directory.CreateDirectory(Path.Combine(temp.Path, ".litespec"));
        File.WriteAllText(Path.Combine(temp.Path, ".litespec/project.yaml"), """{ "owners": ["lending-team"] }""");
        var model = new ProjectModel
        {
            Config = ConsumerConfig.Load(temp.Path),
            Provisions = new HashSet<string>(StringComparer.Ordinal)
            {
                "litespec/frontend/ui#check-every-route",
                "litespec/backend/persistence#commit-each-command-pipeline-once",
            },
        };

        var targets = FrontendRecords.ProofTargetIds(model);

        Assert.Contains("litespec/frontend/ui#check-every-route", targets);
        Assert.DoesNotContain("litespec/backend/persistence#commit-each-command-pipeline-once", targets);
    }
}
