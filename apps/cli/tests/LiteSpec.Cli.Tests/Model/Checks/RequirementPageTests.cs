using System.Text.Json.Nodes;
using LiteSpec.Cli.Book.Outline;
using LiteSpec.Cli.Model;
using LiteSpec.Cli.Tests.Consumer;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves a requirement is a page of its own in its feature's folder: it is refused inside a feature file, its identifier names its folder's feature, its explanation stays optional, and it reaches Behavior and its feature.</summary>
public sealed class RequirementPageTests
{
    const string Requirement = ".litespec/product/pricing/flat-fee/flat-fee.yaml";
    const string RequirementExplanation = ".litespec/product/pricing/flat-fee/flat-fee.md";

    /// <summary>A requirement entry left in a feature file is refused with a message naming the folder to use.</summary>
    /// <proves>litespec/core/intent#identify-every-requirement</proves>
    [Fact(DisplayName = "requirement entry in a feature file is refused")]
    public void RequirementEntryInAFeatureFileIsRefused()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.WriteFile(folder.Path, ".litespec/product/pricing.yaml", """
            type: feature
            id: feature/pricing
            title: Pricing
            offering: Every order carries a flat fee of one euro.
            solves:
              - problem/slow-checkout
            records:
              - type: requirement
                id: requirement/pricing.extra-fee
                title: Extra fee
            """);

        var result = TestCli.Run("model", "check", "--root", folder.Path);

        Assert.Equal(1, result.Status);
        Assert.Contains("[record-invalid]", result.Output);
        Assert.Contains("a requirement is a record file of its own at <feature folder>/<name>/<name>.yaml", result.Output);
    }

    /// <summary>A requirement whose identifier names a feature other than its folder's is reported as identifier-invalid.</summary>
    /// <proves>litespec/core/language#follow-the-identifier-form-of-each-record-type</proves>
    [Fact(DisplayName = "requirement naming another feature reports identifier-invalid")]
    public void RequirementFeatureSegmentMustNameItsFolder()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateMeta(folder.Path, Requirement, meta => meta["id"] = "requirement/other.flat-fee");

        var result = TestCli.Run("model", "check", "--root", folder.Path);

        Assert.Equal(1, result.Status);
        Assert.Contains("[identifier-invalid]", result.Output);
        Assert.Contains("Requirement 'requirement/other.flat-fee' names feature 'other', but its folder sits in feature 'pricing'.", result.Output);
    }

    /// <summary>A requirement with no explanation file raises no explanation-missing on an explained layer.</summary>
    /// <proves>litespec/core/writing#give-each-explanation-file-its-fixed-sections</proves>
    [Fact(DisplayName = "requirement without an explanation is not missing one")]
    public void RequirementWithoutExplanationIsNotMissingOne()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateProject(folder.Path, project => project["explanations"] = new JsonObject
        {
            ["layers"] = new JsonArray("intent"),
        });

        var result = TestCli.Run("model", "check", "--root", folder.Path, "--all");

        Assert.DoesNotContain("Requirement 'requirement/pricing.flat-fee' holds no explanation file", result.Output);
    }

    /// <summary>A requirement explanation that is present is checked like any explanation: it must open with a paragraph.</summary>
    /// <proves>litespec/core/writing#give-each-explanation-file-its-fixed-sections</proves>
    [Fact(DisplayName = "present requirement explanation is checked")]
    public void PresentRequirementExplanationIsChecked()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.WriteFile(folder.Path, RequirementExplanation, "# Flat Fee\n\n## Example\n\nAlex pays one fee.\n");

        var result = TestCli.Run("model", "check", "--root", folder.Path, "--all");

        Assert.Contains("Requirement 'requirement/pricing.flat-fee' opens with no paragraph between its H1 and its first H2.", result.Output);
    }

    /// <summary>A requirement in its feature's folder reaches Behavior, and so does its feature.</summary>
    /// <proves>litespec/core/intent#reach-every-intent-record-with-behavior</proves>
    [Fact(DisplayName = "requirement in its feature folder reaches behavior")]
    public void RequirementInItsFeatureFolderReachesBehavior()
    {
        using var temp = TestCli.Temp("requirement-reach");
        var features = Path.Combine(temp.Path, ".litespec", "intent", "problems", "ghost-pain", "features", "borrowing");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("problem", "problem/ghost-pain", Path.Combine(temp.Path, "ghost-pain.yaml"), "{}"),
            TestModel.Record("feature", "feature/borrowing", Path.Combine(features, "borrowing.yaml"), """{}"""),
            TestModel.Record("requirement", "requirement/borrowing.max-loans", Path.Combine(features, "max-loans", "max-loans.yaml"), """{"kind": "functional", "statement": "A reader borrows a limited number of copies."}"""),
            TestModel.Record("use-case", "use-case/lending.borrow", Path.Combine(temp.Path, "borrow.yaml"), """{"fulfills": ["requirement/borrowing.max-loans"]}"""));

        var codes = TestModel.Codes(model);

        Assert.DoesNotContain("intent-unreached", codes);
    }

    /// <summary>The parent of a requirement is the feature whose folder holds it, even when its identifier names another feature.</summary>
    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact(DisplayName = "requirement parent is the feature folder that holds it")]
    public void RequirementParentIsTheFeatureFolder()
    {
        var model = JsonNode.Parse("""
            {
              "records": [
                { "type": "feature", "id": "feature/borrowing", "file": ".litespec/intent/problems/slow-desk-loans/features/borrowing/borrowing.yaml" },
                { "type": "requirement", "id": "requirement/other.max-loans", "file": ".litespec/intent/problems/slow-desk-loans/features/borrowing/max-loans/max-loans.yaml" }
              ]
            }
            """)!.AsObject();

        Assert.Equal("feature/borrowing", BookParents.ParentOf(model, "requirement/other.max-loans"));
    }
}
