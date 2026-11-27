using LiteSpec.Cli.Model.Refusals;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves the refusal meaning checks: a refusal result names its refusal and message without a meaning, a refusal-set entry owns the meaning without a problem type, and the problem type of a code joins the declared base with the code's part after its module.</summary>
public sealed class RefusalMeaningCheckTests
{
    /// <summary>A refusal result with a meaning reports record-invalid, because the module's refusal set holds the meaning.</summary>
    /// <proves>litespec/core/use-cases#declare-each-modules-refusals-once</proves>
    [Fact]
    public void RefusalResultWithMeaningReportsInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("result", "result/approve-expense.receipt-missing", Path.Combine(temp.Path, "approve.yaml"), """{"outcome": "refusal", "refusal": "refusal/spending.receipt-missing", "meaning": "The expense is above the receipt threshold and carries no receipt.", "message": "Add a receipt to file this expense."}"""));

        Assert.Contains("record-invalid", TestModel.Codes(model));
    }

    /// <summary>A refusal result without a meaning passes the shape rule.</summary>
    /// <proves>litespec/core/use-cases#declare-each-modules-refusals-once</proves>
    [Fact]
    public void RefusalResultWithoutMeaningPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("result", "result/approve-expense.receipt-missing", Path.Combine(temp.Path, "approve.yaml"), """{"outcome": "refusal", "refusal": "refusal/spending.receipt-missing", "message": "Add a receipt to file this expense."}"""));

        Assert.DoesNotContain("record-invalid", TestModel.Codes(model));
        Assert.DoesNotContain("record-invalid", TestModel.Codes(model));
    }

    /// <summary>A success result without a meaning reports record-invalid, because only a refusal result drops the meaning.</summary>
    /// <proves>litespec/core/use-cases#end-every-use-case-in-named-results</proves>
    [Fact]
    public void SuccessResultWithoutMeaningReportsInvalid()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("result", "result/approve-expense.approved", Path.Combine(temp.Path, "approve.yaml"), """{"outcome": "success"}"""));

        Assert.Contains("record-invalid", TestModel.Codes(model));
    }

    /// <summary>A success result with a meaning passes the shape rule.</summary>
    /// <proves>litespec/core/use-cases#end-every-use-case-in-named-results</proves>
    [Fact]
    public void SuccessResultWithMeaningPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("result", "result/approve-expense.approved", Path.Combine(temp.Path, "approve.yaml"), """{"outcome": "success", "meaning": "The expense was approved."}"""));

        Assert.DoesNotContain("record-invalid", TestModel.Codes(model));
        Assert.DoesNotContain("record-invalid", TestModel.Codes(model));
    }

    /// <summary>A refusal-set entry without a problem type reads with its code and meaning and no problem.</summary>
    /// <proves>litespec/core/use-cases#declare-each-modules-refusals-once</proves>
    [Fact]
    public void RefusalSetEntryWithoutProblemTypeReads()
    {
        using var temp = TestCli.Temp("checks");
        WriteRefusalSet(temp.Path, """{"refusals": [{"id": "refusal/spending.receipt-missing", "title": "Receipt Missing", "meaning": "The expense is above the receipt threshold and carries no receipt."}]}""");

        var problems = new List<Finding>();
        var entries = RefusalSetReader.Read(TestModel.Config(temp.Path), problems);

        var entry = Assert.Single(entries);
        Assert.Equal("spending", entry.Module);
        Assert.Equal("spending.receipt-missing", entry.Code);
        Assert.Equal("The expense is above the receipt threshold and carries no receipt.", entry.Meaning);
        Assert.Empty(problems);
    }

    /// <summary>A refusal-set entry naming another module's code reports record-invalid.</summary>
    /// <proves>litespec/core/use-cases#declare-each-modules-refusals-once</proves>
    [Fact]
    public void RefusalSetEntryWithForeignCodeReportsInvalid()
    {
        using var temp = TestCli.Temp("checks");
        WriteRefusalSet(temp.Path, """{"refusals": [{"id": "refusal/payouts.receipt-missing", "title": "Receipt Missing", "meaning": "The expense is above the receipt threshold and carries no receipt."}]}""");

        var problems = new List<Finding>();
        var entries = RefusalSetReader.Read(TestModel.Config(temp.Path), problems);

        Assert.Empty(entries);
        Assert.Equal("record-invalid", Assert.Single(problems).Code);
    }

    /// <summary>The problem type of a code joins the declared base with the code's part after its module.</summary>
    /// <proves>litespec/core/use-cases#declare-each-modules-refusals-once</proves>
    [Fact]
    public void ProblemTypeOfCodeJoinsBaseAndName()
    {
        Assert.Equal(
            "https://claimwick.dev/problems/receipt-missing",
            RefusalSetReader.ProblemTypeOf("spending.receipt-missing", "https://claimwick.dev/problems/"));
    }

    /// <summary>The problem type of a code without a module appends the whole code to the base.</summary>
    /// <proves>litespec/core/use-cases#declare-each-modules-refusals-once</proves>
    [Fact]
    public void ProblemTypeOfCodeWithoutModuleAppendsCode()
    {
        Assert.Equal(
            "https://claimwick.dev/problems/forbidden",
            RefusalSetReader.ProblemTypeOf("forbidden", "https://claimwick.dev/problems/"));
    }

    /// <summary>The config reads the declared problem type base.</summary>
    /// <proves>litespec/core/use-cases#declare-each-modules-refusals-once</proves>
    [Fact]
    public void ProblemTypeBaseReadsFromDeclaration()
    {
        using var temp = TestCli.Temp("checks");
        Directory.CreateDirectory(Path.Combine(temp.Path, ".litespec"));
        File.WriteAllText(Path.Combine(temp.Path, ".litespec", "project.yaml"), """{"problemTypeBase": "https://claimwick.dev/problems/"}""");

        Assert.Equal("https://claimwick.dev/problems/", TestModel.Config(temp.Path).ProblemTypeBase);
    }

    /// <summary>The problem type base reads empty when the declaration names none.</summary>
    /// <proves>litespec/core/use-cases#declare-each-modules-refusals-once</proves>
    [Fact]
    public void ProblemTypeBaseMissingReadsEmpty()
    {
        using var temp = TestCli.Temp("checks");

        Assert.Equal("", TestModel.Config(temp.Path).ProblemTypeBase);
    }

    /// <summary>Writes the spending module's refusal set under the behavior root the config reads.</summary>
    /// <param name="root">The consumer root folder.</param>
    /// <param name="json">The refusal set file content.</param>
    static void WriteRefusalSet(string root, string json)
    {
        var folder = Path.Combine(root, ".litespec", "behavior", "modules", "spending");
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "refusals.yaml"), json + "\n");
    }
}
