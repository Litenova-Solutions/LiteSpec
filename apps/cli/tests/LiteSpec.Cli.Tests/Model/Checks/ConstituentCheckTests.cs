using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Code;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves the constituent derivation and the constituent-parent-missing check against hand-built models.</summary>
public sealed class ConstituentCheckTests
{
    /// <summary>State Beside Its Root Is Listed Under The Aggregate.</summary>
    /// <proves>litespec/backend/domain#place-every-constituent-type-in-its-aggregates-folder</proves>
    [Fact]
    public void StateBesideItsRootIsListedUnderTheAggregate()
    {
        using var temp = TestCli.Temp("checks");
        WriteProject(temp.Path);
        var model = TestModel.Model(TestModel.Config(temp.Path));
        var root = Path.Combine(temp.Path, "src", "Domain", "Lending", "Loans", "Loan.cs");
        var state = Path.Combine(temp.Path, "src", "Domain", "Lending", "Loans", "LoanState.cs");
        AddRoot(model, root, "Loan", "aggregate/lending.loan");
        AddType(model, state, "LoanState", "Library.Domain.Lending.Loans", 3, false);

        Assert.DoesNotContain("constituent-parent-missing", TestModel.Codes(model));

        Constituents.Compute(model);

        Assert.Contains("src/Domain/Lending/Loans/LoanState.cs:LoanState", model.Constituents["aggregate/lending.loan"]);
        Assert.Contains("src/Domain/Lending/Loans/LoanState.cs:LoanState", model.ToJson());
    }

    /// <summary>Type In A Subfolder Of The Aggregate Folder Passes.</summary>
    /// <proves>litespec/backend/domain#place-every-constituent-type-in-its-aggregates-folder</proves>
    [Fact]
    public void TypeInASubfolderOfTheAggregateFolderPasses()
    {
        using var temp = TestCli.Temp("checks");
        WriteProject(temp.Path);
        var model = TestModel.Model(TestModel.Config(temp.Path));
        var root = Path.Combine(temp.Path, "src", "Domain", "Lending", "Loans", "Loan.cs");
        var state = Path.Combine(temp.Path, "src", "Domain", "Lending", "Loans", "States", "LoanState.cs");
        AddRoot(model, root, "Loan", "aggregate/lending.loan");
        AddType(model, state, "LoanState", "Library.Domain.Lending.Loans.States", 3, false);

        Assert.DoesNotContain("constituent-parent-missing", TestModel.Codes(model));
    }

    /// <summary>Type In A Module Folder With No Root Reports.</summary>
    /// <proves>litespec/backend/domain#place-every-constituent-type-in-its-aggregates-folder</proves>
    [Fact]
    public void TypeInAModuleFolderWithNoRootReports()
    {
        using var temp = TestCli.Temp("checks");
        WriteProject(temp.Path);
        var model = TestModel.Model(TestModel.Config(temp.Path));
        var file = Path.Combine(temp.Path, "src", "Domain", "Lending", "Copies", "CopyTitle.cs");
        AddType(model, file, "CopyTitle", "Library.Domain.Lending.Copies", 3, false);

        var finding = Assert.Single(
            Checker.Check(model).Findings,
            found => found.Code == "constituent-parent-missing");

        Assert.Equal(
            "Domain type 'CopyTitle' sits in 'src/Domain/Lending/Copies', which holds no aggregate root and is not the shared kernel.",
            finding.Message);
        Assert.Equal(file, finding.Path);
        Assert.Equal(3, finding.Line);
        Assert.Null(finding.Subject);
    }

    /// <summary>Type In The Shared Kernel Is Listed Under Kernel.</summary>
    /// <proves>litespec/backend/domain#place-every-constituent-type-in-its-aggregates-folder</proves>
    [Fact]
    public void TypeInTheSharedKernelIsListedUnderKernel()
    {
        using var temp = TestCli.Temp("checks");
        WriteProject(temp.Path);
        var model = TestModel.Model(TestModel.Config(temp.Path));
        var file = Path.Combine(temp.Path, "src", "Library.SharedKernel", "Domain", "Money.cs");
        AddType(model, file, "Money", "Library.SharedKernel.Domain", 4, false);

        Assert.DoesNotContain("constituent-parent-missing", TestModel.Codes(model));

        Constituents.Compute(model);

        Assert.Contains("src/Library.SharedKernel/Domain/Money.cs:Money", model.Constituents["kernel"]);
    }

    /// <summary>Marked Domain Service Is Never A Constituent.</summary>
    /// <proves>litespec/backend/domain#place-every-constituent-type-in-its-aggregates-folder</proves>
    [Fact]
    public void MarkedDomainServiceIsNeverAConstituent()
    {
        using var temp = TestCli.Temp("checks");
        WriteProject(temp.Path);
        var model = TestModel.Model(TestModel.Config(temp.Path));
        var file = Path.Combine(temp.Path, "src", "Domain", "Lending", "Copies", "CopyPricer.cs");
        AddType(model, file, "CopyPricer", "Library.Domain.Lending.Copies", 5, true);

        Assert.DoesNotContain("constituent-parent-missing", TestModel.Codes(model));

        Constituents.Compute(model);

        Assert.Empty(model.Constituents);
    }

    /// <summary>Two Roots In One Folder Orphan The Type.</summary>
    /// <proves>litespec/backend/domain#place-every-constituent-type-in-its-aggregates-folder</proves>
    [Fact]
    public void TwoRootsInOneFolderOrphanTheType()
    {
        using var temp = TestCli.Temp("checks");
        WriteProject(temp.Path);
        var model = TestModel.Model(TestModel.Config(temp.Path));
        var folder = Path.Combine(temp.Path, "src", "Domain", "Lending", "Loans");
        AddRoot(model, Path.Combine(folder, "Loan.cs"), "Loan", "aggregate/lending.loan");
        AddRoot(model, Path.Combine(folder, "Copy.cs"), "Copy", "aggregate/lending.copy");
        AddType(model, Path.Combine(folder, "LoanState.cs"), "LoanState", "Library.Domain.Lending.Loans", 3, false);

        Assert.Contains("constituent-parent-missing", TestModel.Codes(model));
    }

    /// <summary>Type At The Domain Root With No Kernel Belongs To The Kernel.</summary>
    /// <proves>litespec/backend/domain#place-every-constituent-type-in-its-aggregates-folder</proves>
    [Fact]
    public void TypeAtTheDomainRootWithNoKernelBelongsToTheKernel()
    {
        using var temp = TestCli.Temp("checks");
        File.WriteAllText(
            Path.Combine(temp.Path, ".litespec/project.yaml"),
            """{"stackConfig": {"dotnet": {"domain": "src/Domain"}}}""");
        var model = TestModel.Model(TestModel.Config(temp.Path));
        var file = Path.Combine(temp.Path, "src", "Domain", "AuditStamp.cs");
        AddType(model, file, "AuditStamp", "Library.Domain", 3, false);

        Assert.DoesNotContain("constituent-parent-missing", TestModel.Codes(model));

        Constituents.Compute(model);

        Assert.Contains("src/Domain/AuditStamp.cs:AuditStamp", model.Constituents["kernel"]);
    }

    static void WriteProject(string root) =>
        File.WriteAllText(
            Path.Combine(root, ".litespec/project.yaml"),
            """{"stackConfig": {"dotnet": {"domain": "src/Domain", "sharedKernel": "src/Library.SharedKernel"}}}""");

    static void AddRoot(ProjectModel model, string file, string root, string aggregate, int line = 5) =>
        model.Code.Elements.Add(new MarkedElement(
            CodeKind.AggregateRoot, root, null, file, line,
            [new CodeMark(aggregate, MarkStyle.Xml, MarkVerb.Implements, file, line - 1)],
            false));

    static void AddType(ProjectModel model, string file, string name, string space, int line, bool marked) =>
        model.Code.DomainTypes.Add(new DomainType(name, space, file, line, marked));
}
