using System.Text.Json;
using System.Text.Json.Nodes;
using LiteSpec.Cli.Tests.Consumer;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;
using LiteSpec.Standards;

namespace LiteSpec.Cli.Tests.Model.Claims;

/// <summary>Proves <c>litespec model check</c> reports only the findings the project's layer claims cover.</summary>
public sealed class CheckByClaimTests
{
    /// <summary>A record finding blocks under every claim, because a record finding needs no code and no proof.</summary>
    /// <param name="claim">The claim name, as <see cref="ConsumerFixture.Claim" /> reads it.</param>
    /// <proves>litespec/core/scope#claim-the-record-layers-the-project-uses</proves>
    [Theory]
    [InlineData("intent-only")]
    [InlineData("intent-and-behavior")]
    [InlineData("own-code-and-proof")]
    [InlineData("litespec-code-and-proof")]
    public void RecordFindingBlocksUnderEveryClaim(string claim)
    {
        using var fixture = ModelFixture.Prepare("large", "record-invalid");
        SetClaim(fixture.Path, ConsumerFixture.Claim(claim));

        var result = TestCli.Run(["model", "check", "--root", fixture.Path, "--format", "json"]);

        Assert.Equal(1, result.Status);
        Assert.Contains("record-invalid", result.Stdout, StringComparison.Ordinal);
    }

    /// <summary>An Implementation finding is silent when the project claims no code, and blocks once it claims code.</summary>
    /// <proves>litespec/core/scope#claim-the-record-layers-the-project-uses</proves>
    [Fact]
    public void ImplementationFindingIsSilentWithoutCode()
    {
        using var records = ModelFixture.Prepare("large", "use-case-unimplemented");
        SetClaim(records.Path, ConsumerFixture.Claim("intent-and-behavior"));

        var atRecords = TestCli.Run(["model", "check", "--root", records.Path, "--format", "json"]);

        Assert.Equal(0, atRecords.Status);
        Assert.DoesNotContain("use-case-unimplemented", atRecords.Stdout, StringComparison.Ordinal);

        using var owned = ModelFixture.Prepare("large", "use-case-unimplemented");
        SetClaim(owned.Path, ConsumerFixture.Claim("own-code-and-proof"));

        var atOwned = TestCli.Run(["model", "check", "--root", owned.Path, "--format", "json"]);

        Assert.Equal(1, atOwned.Status);
        Assert.Contains("use-case-unimplemented", atOwned.Stdout, StringComparison.Ordinal);
    }

    /// <summary>Proof without implementation: an Implementation finding is silent, since the project claims no code to mark.</summary>
    /// <proves>litespec/core/scope#claim-the-record-layers-the-project-uses</proves>
    [Fact]
    public void ProofWithoutImplementationReportsNoCodeFinding()
    {
        using var fixture = ModelFixture.Prepare("large", "use-case-unimplemented");
        SetClaim(fixture.Path, ConsumerFixture.Claim("proof-without-code"));

        var result = TestCli.Run(["model", "check", "--root", fixture.Path, "--format", "json"]);

        Assert.Equal(0, result.Status);
        Assert.DoesNotContain("use-case-unimplemented", result.Stdout, StringComparison.Ordinal);
    }

    /// <summary>Implementation without proof: no Proof finding is reported, so no use case reads as unverified.</summary>
    /// <proves>litespec/core/scope#claim-the-record-layers-the-project-uses</proves>
    [Fact]
    public void ImplementationWithoutProofReportsNoProofFinding()
    {
        using var fixture = ModelFixture.Prepare("large", "use-case-unimplemented");
        SetClaim(fixture.Path, ConsumerFixture.Claim("own-code-without-proof"));

        var result = TestCli.Run(["model", "check", "--root", fixture.Path, "--format", "json"]);

        Assert.Equal(1, result.Status);
        Assert.Contains("use-case-unimplemented", result.Stdout, StringComparison.Ordinal);
        using var document = JsonDocument.Parse(result.Stdout);
        var claim = new LayerClaims(Intent: true, Behavior: true, Implementation: LayerPractice.Own, Proof: null);

        foreach (var finding in document.RootElement.GetProperty("findings").EnumerateArray())
        {
            var code = finding.GetProperty("code").GetString()!;
            Assert.True(claim.Covers(code, []), $"Finding '{code}' is a Proof finding, which the claim does not cover.");
        }
    }

    /// <summary>A layout finding is silent for the project's own code and blocks for LiteSpec's code, whatever its proof.</summary>
    /// <proves>litespec/core/scope#claim-the-record-layers-the-project-uses</proves>
    [Fact]
    public void LayoutFindingFollowsTheImplementationPractice()
    {
        using var owned = ModelFixture.Prepare("large");
        AddStrayFolder(owned.Path);
        SetClaim(owned.Path, ConsumerFixture.Claim("own-code-and-proof"));

        var atOwned = TestCli.Run(["model", "check", "--root", owned.Path, "--format", "json"]);

        Assert.Equal(0, atOwned.Status);
        Assert.DoesNotContain("code-placement-drift", atOwned.Stdout, StringComparison.Ordinal);

        using var litespec = ModelFixture.Prepare("large");
        AddStrayFolder(litespec.Path);
        SetClaim(litespec.Path, ConsumerFixture.Claim("litespec-code-own-proof"));

        var atLiteSpec = TestCli.Run(["model", "check", "--root", litespec.Path, "--format", "json"]);

        Assert.Equal(1, atLiteSpec.Status);
        Assert.Contains("code-placement-drift", atLiteSpec.Stdout, StringComparison.Ordinal);
    }

    /// <summary>An intent-only project reports no Behavior finding, such as an Intent record that reaches no Behavior.</summary>
    /// <proves>litespec/core/scope#claim-the-record-layers-the-project-uses</proves>
    [Fact]
    public void IntentOnlyClaimReportsNoBehaviorFinding()
    {
        using var intentOnly = ModelFixture.Prepare("large", "intent-unreached");
        SetClaim(intentOnly.Path, ConsumerFixture.Claim("intent-only"));

        var atIntentOnly = TestCli.Run(["model", "check", "--root", intentOnly.Path, "--format", "json"]);

        Assert.DoesNotContain("intent-unreached", atIntentOnly.Stdout, StringComparison.Ordinal);

        using var behavior = ModelFixture.Prepare("large", "intent-unreached");
        SetClaim(behavior.Path, ConsumerFixture.Claim("intent-and-behavior"));

        var atBehavior = TestCli.Run(["model", "check", "--root", behavior.Path, "--format", "json"]);

        Assert.Contains("intent-unreached", atBehavior.Stdout, StringComparison.Ordinal);
    }

    /// <summary>A clean small fixture passes under the intent-and-behavior claim, and every finding it reports is one the claim covers.</summary>
    /// <proves>litespec/core/scope#claim-the-record-layers-the-project-uses</proves>
    [Fact]
    public void IntentAndBehaviorCheckPassesWithoutCode()
    {
        using var fixture = ModelFixture.Prepare("small");
        SetClaim(fixture.Path, ConsumerFixture.Claim("intent-and-behavior"));

        var result = TestCli.Run(["model", "check", "--root", fixture.Path, "--format", "json"]);

        Assert.True(result.Status == 0, result.Output);
        using var document = JsonDocument.Parse(result.Stdout);
        var claim = new LayerClaims(Intent: true, Behavior: true, Implementation: null, Proof: null);

        foreach (var finding in document.RootElement.GetProperty("findings").EnumerateArray())
        {
            var code = finding.GetProperty("code").GetString()!;
            Assert.NotEqual("blocking", finding.GetProperty("outcome").GetString());
            Assert.True(claim.Covers(code, []), $"Finding '{code}' is outside the intent-and-behavior claim.");
        }
    }

    /// <summary>Adds one folder naming no declared module under the backend root, so only a layout finding follows.</summary>
    /// <param name="root">The fixture consumer root folder.</param>
    static void AddStrayFolder(string root)
    {
        var folder = Path.Combine(root, "apps", "api", "src", "Library.Application", "Archive");
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "readme.txt"), "This folder names no declared module, so the placement check reports it.\n");
    }

    /// <summary>Writes the claimed layers into the fixture project file.</summary>
    /// <param name="root">The fixture consumer root folder.</param>
    /// <param name="layers">The layers object the project claims.</param>
    static void SetClaim(string root, JsonObject layers)
    {
        var file = Path.Combine(root, ".litespec/project.yaml");
        var project = RecordText.Read(file)!.AsObject();
        project["layers"] = layers;
        File.WriteAllText(file, project.ToJsonString());
    }
}
