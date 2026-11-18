using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Code;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves the part derivation and the part-parent-missing check against hand-built models.</summary>
public sealed class PartCheckTests
{
    /// <summary>State Beside Its Root Is Listed Under The Aggregate.</summary>
    /// <proves>litespec/backend/domain#place-every-part-type-in-its-aggregates-folder</proves>
    [Fact]
    public void StateBesideItsRootIsListedUnderTheAggregate()
    {
        using var temp = TestCli.Temp("checks");
        WriteProject(temp.Path);
        var model = TestModel.Model(TestModel.Config(temp.Path));
        var root = Path.Combine(temp.Path, "src", "Modules", "Library.Lending", "Domain", "Loans", "Loan.cs");
        var state = Path.Combine(temp.Path, "src", "Modules", "Library.Lending", "Domain", "Loans", "LoanState.cs");
        AddRoot(model, root, "Loan", "aggregate/lending.loan");
        AddType(model, state, "LoanState", "Library.Lending.Domain.Loans", 3, false);

        Assert.DoesNotContain("part-parent-missing", TestModel.Codes(model));

        Parts.Compute(model);

        Assert.Contains("src/Modules/Library.Lending/Domain/Loans/LoanState.cs:LoanState", model.Parts["aggregate/lending.loan"]);
        Assert.Contains("src/Modules/Library.Lending/Domain/Loans/LoanState.cs:LoanState", model.ToJson());
    }

    /// <summary>Type In A Subfolder Of The Aggregate Folder Passes.</summary>
    /// <proves>litespec/backend/domain#place-every-part-type-in-its-aggregates-folder</proves>
    [Fact]
    public void TypeInASubfolderOfTheAggregateFolderPasses()
    {
        using var temp = TestCli.Temp("checks");
        WriteProject(temp.Path);
        var model = TestModel.Model(TestModel.Config(temp.Path));
        var root = Path.Combine(temp.Path, "src", "Modules", "Library.Lending", "Domain", "Loans", "Loan.cs");
        var state = Path.Combine(temp.Path, "src", "Modules", "Library.Lending", "Domain", "Loans", "States", "LoanState.cs");
        AddRoot(model, root, "Loan", "aggregate/lending.loan");
        AddType(model, state, "LoanState", "Library.Lending.Domain.Loans.States", 3, false);

        Assert.DoesNotContain("part-parent-missing", TestModel.Codes(model));
    }

    /// <summary>Type In A Module Folder With No Root Reports.</summary>
    /// <proves>litespec/backend/domain#place-every-part-type-in-its-aggregates-folder</proves>
    [Fact]
    public void TypeInAModuleFolderWithNoRootReports()
    {
        using var temp = TestCli.Temp("checks");
        WriteProject(temp.Path);
        var model = TestModel.Model(TestModel.Config(temp.Path));
        var file = Path.Combine(temp.Path, "src", "Modules", "Library.Lending", "Domain", "Copies", "CopyTitle.cs");
        AddType(model, file, "CopyTitle", "Library.Lending.Domain.Copies", 3, false);

        var finding = Assert.Single(
            Checker.Check(model).Findings,
            found => found.Code == "part-parent-missing");

        Assert.Equal(
            "Domain type 'CopyTitle' sits in 'src/Modules/Library.Lending/Domain/Copies', which holds no aggregate root and is not the Kernel.",
            finding.Message);
        Assert.Equal(file, finding.Path);
        Assert.Equal(3, finding.Line);
        Assert.Null(finding.Subject);
    }

    /// <summary>Type In The Kernel Is Listed Under Kernel.</summary>
    /// <proves>litespec/backend/domain#place-every-part-type-in-its-aggregates-folder</proves>
    [Fact]
    public void TypeInTheKernelIsListedUnderKernel()
    {
        using var temp = TestCli.Temp("checks");
        WriteProject(temp.Path);
        var model = TestModel.Model(TestModel.Config(temp.Path));
        var file = Path.Combine(temp.Path, "src", "Library.Kernel", "Money.cs");
        AddType(model, file, "Money", "Library.Kernel", 4, false);

        Assert.DoesNotContain("part-parent-missing", TestModel.Codes(model));

        Parts.Compute(model);

        Assert.Contains("src/Library.Kernel/Money.cs:Money", model.Parts["kernel"]);
    }

    /// <summary>Marked Domain Service Is Never A Part.</summary>
    /// <proves>litespec/backend/domain#place-every-part-type-in-its-aggregates-folder</proves>
    [Fact]
    public void MarkedDomainServiceIsNeverAPart()
    {
        using var temp = TestCli.Temp("checks");
        WriteProject(temp.Path);
        var model = TestModel.Model(TestModel.Config(temp.Path));
        var file = Path.Combine(temp.Path, "src", "Modules", "Library.Lending", "Domain", "Copies", "CopyPricer.cs");
        AddType(model, file, "CopyPricer", "Library.Lending.Domain.Copies", 5, true);

        Assert.DoesNotContain("part-parent-missing", TestModel.Codes(model));

        Parts.Compute(model);

        Assert.Empty(model.Parts);
    }

    /// <summary>Two Roots In One Folder Orphan The Type.</summary>
    /// <proves>litespec/backend/domain#place-every-part-type-in-its-aggregates-folder</proves>
    [Fact]
    public void TwoRootsInOneFolderOrphanTheType()
    {
        using var temp = TestCli.Temp("checks");
        WriteProject(temp.Path);
        var model = TestModel.Model(TestModel.Config(temp.Path));
        var folder = Path.Combine(temp.Path, "src", "Modules", "Library.Lending", "Domain", "Loans");
        AddRoot(model, Path.Combine(folder, "Loan.cs"), "Loan", "aggregate/lending.loan");
        AddRoot(model, Path.Combine(folder, "Copy.cs"), "Copy", "aggregate/lending.copy");
        AddType(model, Path.Combine(folder, "LoanState.cs"), "LoanState", "Library.Lending.Domain.Loans", 3, false);

        Assert.Contains("part-parent-missing", TestModel.Codes(model));
    }

    static void WriteProject(string root) =>
        File.WriteAllText(
            Path.Combine(root, ".litespec/project.yaml"),
            """{"stackConfig": {"dotnet": {"modules": "src/Modules/Library.Lending", "kernel": "src/Library.Kernel"}}}""");

    static void AddRoot(ProjectModel model, string file, string root, string aggregate, int line = 5) =>
        model.Code.Elements.Add(new MarkedElement(
            CodeKind.AggregateRoot, root, null, file, line,
            [new CodeMark(aggregate, MarkStyle.Xml, MarkVerb.Implements, file, line - 1)],
            false));

    static void AddType(ProjectModel model, string file, string name, string space, int line, bool marked) =>
        model.Code.DomainTypes.Add(new DomainType(name, space, file, line, marked));
}
