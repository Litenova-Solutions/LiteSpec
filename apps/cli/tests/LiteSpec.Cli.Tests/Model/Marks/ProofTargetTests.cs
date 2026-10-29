using System.Text.Json;
using LiteSpec.Model;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Marks;

/// <summary>Proves a test proves the lowest record it verifies, from each of the seven proof kinds.</summary>
public sealed class ProofTargetTests
{
    public static TheoryData<string> PassingFiles
    {
        get
        {
            TheoryData<string> data = new();

            foreach (var file in new[]
            {
                "ScenarioProofTests.cs",
                "RuleProofTests.cs",
                "QualityProofTests.cs",
                "DecisionProofTests.cs",
                "ExternalSystemProofTests.cs",
                "IntegrationEventProofTests.cs",
                "ProvisionProofTests.cs",
                "MultiProofTests.cs",
            })
            {
                data.Add(file);
            }

            return data;
        }
    }

    public static TheoryData<string> StandingProofFiles
    {
        get
        {
            TheoryData<string> data = new();

            foreach (var file in new[]
            {
                "QualityProofTests.cs",
                "DecisionProofTests.cs",
                "ExternalSystemProofTests.cs",
                "IntegrationEventProofTests.cs",
                "ProvisionProofTests.cs",
                "MultiProofTests.cs",
            })
            {
                data.Add(file);
            }

            return data;
        }
    }

    [Theory(DisplayName = "A test proving each of the seven kinds reports no mark finding")]
    [MemberData(nameof(PassingFiles))]
    public void ProofOfEachKindPasses(string file)
    {
        using var temp = MarksFixture.Prepare("proof-targets");

        var findings = MarksFixture.Check(temp.Path);

        foreach (var code in new[] { "mark-missing", "mark-unresolved", "mark-count", "mark-style-mixed" })
        {
            Assert.DoesNotContain(
                MarksFixture.WithCode(findings, code),
                finding => FileName(finding) == file);
        }
    }

    [Theory(DisplayName = "A proof beyond scenario and rule never reports proves-unconfirmed")]
    [MemberData(nameof(StandingProofFiles))]
    public void StandingProofNeverReportsProvesUnconfirmed(string file)
    {
        using var temp = MarksFixture.Prepare("proof-targets");

        var unconfirmed = MarksFixture.WithCode(MarksFixture.Check(temp.Path), "proves-unconfirmed");

        Assert.DoesNotContain(unconfirmed, finding => FileName(finding) == file);
    }

    [Fact(DisplayName = "A test proving a functional requirement reports mark-unresolved")]
    public void FunctionalRequirementProofReportsMarkUnresolved()
    {
        using var temp = MarksFixture.Prepare("proof-targets");

        var finding = Assert.Single(
            MarksFixture.WithCode(MarksFixture.Check(temp.Path), "mark-unresolved"),
            finding => FileName(finding) == "FunctionalProofTests.cs");

        Assert.Contains("which is a functional requirement, not a quality requirement.", finding.Message);
        Assert.Equal("requirement/borrowing.borrow-form", finding.Subject);
    }

    [Fact(DisplayName = "A test proving a product decision reports mark-unresolved")]
    public void ProductDecisionProofReportsMarkUnresolved()
    {
        using var temp = MarksFixture.Prepare("proof-targets");

        var finding = Assert.Single(
            MarksFixture.WithCode(MarksFixture.Check(temp.Path), "mark-unresolved"),
            finding => FileName(finding) == "ProductDecisionProofTests.cs");

        Assert.Contains("which is a product decision, not a technical decision.", finding.Message);
        Assert.Equal("decision/0001-loan-threshold", finding.Subject);
    }

    [Fact(DisplayName = "A test proving an aggregate reports mark-unresolved")]
    public void AggregateProofReportsMarkUnresolved()
    {
        using var temp = MarksFixture.Prepare("proof-targets");

        var finding = Assert.Single(
            MarksFixture.WithCode(MarksFixture.Check(temp.Path), "mark-unresolved"),
            finding => FileName(finding) == "AggregateProofTests.cs");

        Assert.Contains("which is a aggregate record", finding.Message);
        Assert.Contains("not a scenario or rule or requirement or decision or external-system or integration-event or litespec record.", finding.Message);
        Assert.Equal("aggregate/lending.loan", finding.Subject);
    }

    [Fact(DisplayName = "A test proving an unknown provision reports mark-unresolved")]
    public void UnknownProvisionProofReportsMarkUnresolved()
    {
        using var temp = MarksFixture.Prepare("proof-targets");

        var finding = Assert.Single(
            MarksFixture.WithCode(MarksFixture.Check(temp.Path), "mark-unresolved"),
            finding => FileName(finding) == "UnknownProvisionProofTests.cs");

        Assert.Contains("which matches no provision of the carried release.", finding.Message);
        Assert.Equal("litespec/backend/no-such-page#no-such-provision", finding.Subject);
    }

    [Fact(DisplayName = "A test proving a provision on an unapplied page reports mark-unresolved")]
    public void UnappliedProvisionProofReportsMarkUnresolved()
    {
        using var temp = MarksFixture.Prepare("proof-targets");

        var finding = Assert.Single(
            MarksFixture.WithCode(MarksFixture.Check(temp.Path), "mark-unresolved"),
            finding => FileName(finding) == "UnappliedProvisionProofTests.cs");

        Assert.Contains("which sits on a page the project's stacks and extensions do not apply.", finding.Message);
        Assert.Equal("litespec/frontend/blazor/browser#bound-what-is-stored", finding.Subject);
    }

    [Fact(DisplayName = "No other mark-unresolved fires in the proof-targets fixture")]
    public void NoOtherMarkUnresolvedFires()
    {
        using var temp = MarksFixture.Prepare("proof-targets");

        var unresolved = MarksFixture.WithCode(MarksFixture.Check(temp.Path), "mark-unresolved");

        Assert.Equal(5, unresolved.Count);
    }

    [Fact(DisplayName = "A scenario proof still verifies its use case")]
    public void ScenarioProofStillVerifies()
    {
        using var temp = MarksFixture.Prepare("proof-targets");
        var model = ModelBuilder.Build(ConsumerConfig.Load(temp.Path));

        Assert.True(model.Status["use-case/lending.borrow-copy"].Verified);
    }

    [Fact(DisplayName = "A quality-requirement proof does not verify a use case")]
    public void QualityProofDoesNotVerify()
    {
        using var temp = MarksFixture.Prepare("proof-targets");
        var model = ModelBuilder.Build(ConsumerConfig.Load(temp.Path));

        Assert.False(model.Status["use-case/lending.extend-loan"].Verified);
    }

    [Fact(DisplayName = "Tested appears for proof targets with a passing proving test")]
    public void TestedAppearsForProofTargets()
    {
        using var temp = MarksFixture.Prepare("proof-targets");
        var model = ModelBuilder.Build(ConsumerConfig.Load(temp.Path));

        using var document = JsonDocument.Parse(model.ToJson());
        var tested = document.RootElement.GetProperty("tested");

        Assert.True(tested.GetProperty("requirement/borrowing.borrow-latency").GetBoolean());
        Assert.True(tested.GetProperty("external-system/text-gateway").GetBoolean());
        Assert.True(tested.GetProperty("decision/0002-store-loans-as-documents").GetBoolean());
        Assert.False(tested.GetProperty("integration-event/loan-borrowed").GetBoolean());
        Assert.False(tested.TryGetProperty("requirement/borrowing.borrow-form", out _));
        Assert.False(tested.TryGetProperty("decision/0001-loan-threshold", out _));
        Assert.False(tested.TryGetProperty("aggregate/lending.loan", out _));
        Assert.False(tested.TryGetProperty("use-case/lending.borrow-copy", out _));

        var query = JsonDocument.Parse(Querier.Query(model, "requirement/borrowing.borrow-latency")!);
        var computed = query.RootElement.GetProperty("computed");

        Assert.True(computed.GetProperty("tested").GetBoolean());
        Assert.Equal(2, computed.GetProperty("proves").GetArrayLength());
    }

    static string FileName(Finding finding) =>
        Path.GetFileName(finding.Path);
}
