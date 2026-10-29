using LiteSpec.Model;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Checks;

/// <summary>Proves the constituent derivation and the constituent-orphaned check against hand-built models.</summary>
public sealed class ConstituentCheckTests
{
    /// <proves>litespec/backend/domain#place-every-constituent-type-in-its-aggregates-folder</proves>
    [Fact]
    public void State_Beside_Its_Root_Is_Listed_Under_The_Aggregate()
    {
        using var temp = Cli.Temp("checks");
        WriteProject(temp.Path);
        var model = TestModel.Model(TestModel.Config(temp.Path));
        var root = Path.Combine(temp.Path, "src", "Domain", "Lending", "Loans", "Loan.cs");
        var state = Path.Combine(temp.Path, "src", "Domain", "Lending", "Loans", "LoanState.cs");
        AddRoot(model, root, "Loan", "aggregate/lending.loan");
        AddType(model, state, "LoanState", "Library.Domain.Lending.Loans", 3, false);

        Assert.DoesNotContain("constituent-orphaned", TestModel.Codes(model));

        Constituents.Compute(model);

        Assert.Contains("src/Domain/Lending/Loans/LoanState.cs:LoanState", model.Constituents["aggregate/lending.loan"]);
        Assert.Contains("src/Domain/Lending/Loans/LoanState.cs:LoanState", model.ToJson());
    }

    /// <proves>litespec/backend/domain#place-every-constituent-type-in-its-aggregates-folder</proves>
    [Fact]
    public void Type_In_A_Subfolder_Of_The_Aggregate_Folder_Passes()
    {
        using var temp = Cli.Temp("checks");
        WriteProject(temp.Path);
        var model = TestModel.Model(TestModel.Config(temp.Path));
        var root = Path.Combine(temp.Path, "src", "Domain", "Lending", "Loans", "Loan.cs");
        var state = Path.Combine(temp.Path, "src", "Domain", "Lending", "Loans", "States", "LoanState.cs");
        AddRoot(model, root, "Loan", "aggregate/lending.loan");
        AddType(model, state, "LoanState", "Library.Domain.Lending.Loans.States", 3, false);

        Assert.DoesNotContain("constituent-orphaned", TestModel.Codes(model));
    }

    /// <proves>litespec/backend/domain#place-every-constituent-type-in-its-aggregates-folder</proves>
    [Fact]
    public void Type_In_A_Module_Folder_With_No_Root_Reports()
    {
        using var temp = Cli.Temp("checks");
        WriteProject(temp.Path);
        var model = TestModel.Model(TestModel.Config(temp.Path));
        var file = Path.Combine(temp.Path, "src", "Domain", "Lending", "Copies", "CopyTitle.cs");
        AddType(model, file, "CopyTitle", "Library.Domain.Lending.Copies", 3, false);

        var finding = Assert.Single(
            Checker.Check(model).Findings,
            found => found.Code == "constituent-orphaned");

        Assert.Equal(
            "Domain type 'CopyTitle' sits in 'src/Domain/Lending/Copies', which holds no aggregate root and is not the shared kernel.",
            finding.Message);
        Assert.Equal(file, finding.Path);
        Assert.Equal(3, finding.Line);
        Assert.Null(finding.Subject);
    }

    /// <proves>litespec/backend/domain#place-every-constituent-type-in-its-aggregates-folder</proves>
    [Fact]
    public void Type_In_The_Shared_Kernel_Is_Listed_Under_Kernel()
    {
        using var temp = Cli.Temp("checks");
        WriteProject(temp.Path);
        var model = TestModel.Model(TestModel.Config(temp.Path));
        var file = Path.Combine(temp.Path, "src", "Domain", "Shared", "Money.cs");
        AddType(model, file, "Money", "Library.Domain.Shared", 4, false);

        Assert.DoesNotContain("constituent-orphaned", TestModel.Codes(model));

        Constituents.Compute(model);

        Assert.Contains("src/Domain/Shared/Money.cs:Money", model.Constituents["kernel"]);
    }

    /// <proves>litespec/backend/domain#place-every-constituent-type-in-its-aggregates-folder</proves>
    [Fact]
    public void Marked_Domain_Service_Is_Never_A_Constituent()
    {
        using var temp = Cli.Temp("checks");
        WriteProject(temp.Path);
        var model = TestModel.Model(TestModel.Config(temp.Path));
        var file = Path.Combine(temp.Path, "src", "Domain", "Lending", "Copies", "CopyPricer.cs");
        AddType(model, file, "CopyPricer", "Library.Domain.Lending.Copies", 5, true);

        Assert.DoesNotContain("constituent-orphaned", TestModel.Codes(model));

        Constituents.Compute(model);

        Assert.Empty(model.Constituents);
    }

    /// <proves>litespec/backend/domain#place-every-constituent-type-in-its-aggregates-folder</proves>
    [Fact]
    public void Two_Roots_In_One_Folder_Orphan_The_Type()
    {
        using var temp = Cli.Temp("checks");
        WriteProject(temp.Path);
        var model = TestModel.Model(TestModel.Config(temp.Path));
        var folder = Path.Combine(temp.Path, "src", "Domain", "Lending", "Loans");
        AddRoot(model, Path.Combine(folder, "Loan.cs"), "Loan", "aggregate/lending.loan");
        AddRoot(model, Path.Combine(folder, "Copy.cs"), "Copy", "aggregate/lending.copy");
        AddType(model, Path.Combine(folder, "LoanState.cs"), "LoanState", "Library.Domain.Lending.Loans", 3, false);

        Assert.Contains("constituent-orphaned", TestModel.Codes(model));
    }

    /// <proves>litespec/backend/domain#place-every-constituent-type-in-its-aggregates-folder</proves>
    [Fact]
    public void Type_At_The_Domain_Root_With_No_Kernel_Belongs_To_The_Kernel()
    {
        using var temp = Cli.Temp("checks");
        File.WriteAllText(
            Path.Combine(temp.Path, "litespec.project.json"),
            """{"stackConfig": {"dotnet": {"domain": "src/Domain"}}}""");
        var model = TestModel.Model(TestModel.Config(temp.Path));
        var file = Path.Combine(temp.Path, "src", "Domain", "AuditStamp.cs");
        AddType(model, file, "AuditStamp", "Library.Domain", 3, false);

        Assert.DoesNotContain("constituent-orphaned", TestModel.Codes(model));

        Constituents.Compute(model);

        Assert.Contains("src/Domain/AuditStamp.cs:AuditStamp", model.Constituents["kernel"]);
    }

    static void WriteProject(string root) =>
        File.WriteAllText(
            Path.Combine(root, "litespec.project.json"),
            """{"stackConfig": {"dotnet": {"domain": "src/Domain", "sharedKernel": "src/Domain/Shared"}}}""");

    static void AddRoot(ProjectModel model, string file, string root, string aggregate, int line = 5) =>
        model.Code.Elements.Add(new MarkedElement(
            CodeKind.AggregateRoot, root, null, file, line,
            [new CodeMark(aggregate, MarkStyle.Xml, MarkVerb.Implements, file, line - 1)],
            false));

    static void AddType(ProjectModel model, string file, string name, string space, int line, bool marked) =>
        model.Code.DomainTypes.Add(new DomainType(name, space, file, line, marked));
}
