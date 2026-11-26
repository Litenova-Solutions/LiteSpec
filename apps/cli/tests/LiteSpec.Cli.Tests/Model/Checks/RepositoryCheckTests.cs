using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Code;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Readers.Dotnet.Backend;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves the repository-unimplemented check against hand-built models and reader fixtures.</summary>
public sealed class RepositoryCheckTests
{
    /// <summary>Changed Aggregate Without An Implementation Reports.</summary>
    /// <proves>litespec/core/backend-persistence#stage-aggregate-writes-through-domain-repositories</proves>
    [Fact]
    public void ChangedAggregateWithoutAnImplementationReports()
    {
        using var temp = TestCli.Temp("checks");
        CreateInfrastructure(temp.Path);
        var model = ChangedLoanModel(temp.Path);
        model.Code.DomainTypes.Add(RepositoryInterface(temp.Path));

        Assert.Contains("repository-unimplemented", TestModel.Codes(model));
    }

    /// <summary>Changed Aggregate With An Implementation Passes.</summary>
    /// <proves>litespec/core/backend-persistence#stage-aggregate-writes-through-domain-repositories</proves>
    [Fact]
    public void ChangedAggregateWithAnImplementationPasses()
    {
        using var temp = TestCli.Temp("checks");
        CreateInfrastructure(temp.Path);
        var model = ChangedLoanModel(temp.Path);
        model.Code.DomainTypes.Add(RepositoryInterface(temp.Path));
        model.Code.RepositoryImplementations.Add(new CodeRepositoryImplementation(
            "ILoanRepository", "LoanRepository", Path.Combine(temp.Path, "LoanRepository.cs"), 3));

        Assert.DoesNotContain("repository-unimplemented", TestModel.Codes(model));
    }

    /// <summary>Reader Fixture Without An Implementation Reports.</summary>
    /// <proves>litespec/core/backend-persistence#stage-aggregate-writes-through-domain-repositories</proves>
    [Fact]
    public void ReaderFixtureWithoutAnImplementationReports()
    {
        using var temp = TestCli.Temp("checks");
        WriteProject(temp.Path);
        WriteSource(temp.Path, Path.Combine("App", "Domain", "Loans"), "ILoanRepository.cs", """
            namespace Library.App.Domain.Loans;

            public interface ILoanRepository
            {
                Loan Load(Guid id);
            }
            """);
        Directory.CreateDirectory(Path.Combine(temp.Path, "src", "App", "Infrastructure"));
        var model = ChangedLoanModel(temp.Path);
        model.Code = new DotnetBackendReader().Read(ConsumerConfig.Load(temp.Path));

        Assert.Contains("repository-unimplemented", TestModel.Codes(model));
    }

    /// <summary>Reader Fixture With An Implementation Passes.</summary>
    /// <proves>litespec/core/backend-persistence#stage-aggregate-writes-through-domain-repositories</proves>
    [Fact]
    public void ReaderFixtureWithAnImplementationPasses()
    {
        using var temp = TestCli.Temp("checks");
        WriteProject(temp.Path);
        WriteSource(temp.Path, Path.Combine("App", "Domain", "Loans"), "ILoanRepository.cs", """
            namespace Library.App.Domain.Loans;

            public interface ILoanRepository
            {
                Loan Load(Guid id);
            }
            """);
        WriteSource(temp.Path, Path.Combine("App", "Infrastructure", "Persistence"), "LoanRepository.cs", """
            namespace Library.App.Infrastructure.Persistence;

            public sealed class LoanRepository : ILoanRepository
            {
                public Loan Load(Guid id) => new Loan();
            }
            """);
        var model = ChangedLoanModel(temp.Path);
        model.Code = new DotnetBackendReader().Read(ConsumerConfig.Load(temp.Path));

        Assert.DoesNotContain("repository-unimplemented", TestModel.Codes(model));
    }

    /// <summary>Reader Fixture Without A Domain Interface Reports.</summary>
    /// <proves>litespec/core/backend-persistence#stage-aggregate-writes-through-domain-repositories</proves>
    [Fact]
    public void ReaderFixtureWithoutADomainInterfaceReports()
    {
        using var temp = TestCli.Temp("checks");
        WriteProject(temp.Path);
        Directory.CreateDirectory(Path.Combine(temp.Path, "src", "App", "Infrastructure"));
        var model = ChangedLoanModel(temp.Path);
        model.Code = new DotnetBackendReader().Read(ConsumerConfig.Load(temp.Path));

        Assert.Contains("repository-unimplemented", TestModel.Codes(model));
    }

    /// <summary>A model whose borrow-copy use case changes the loan aggregate.</summary>
    /// <param name="root">The fixture root folder the record files sit under.</param>
    /// <returns>The project model holding the use case and the aggregate.</returns>
    static ProjectModel ChangedLoanModel(string root) =>
        TestModel.Model(
            TestModel.Config(root),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(root, "borrow.yaml"), """{"status": "reviewed", "kind": "command", "writes": ["aggregate/lending.loan"]}"""),
            TestModel.Record("aggregate", "aggregate/lending.loan", Path.Combine(root, "loan.yaml"), """{"status": "reviewed", "states": [{"name": "Active", "meaning": "The loan is active."}]}"""));

    /// <summary>The loan's domain repository interface as the Domain reader reports it.</summary>
    /// <param name="root">The fixture root folder the interface file sits under.</param>
    /// <returns>The domain type describing the repository interface.</returns>
    static DomainType RepositoryInterface(string root) =>
        new("ILoanRepository", "Library.App.Domain.Loans", Path.Combine(root, "ILoanRepository.cs"), 1, false);

    /// <summary>Creates the Infrastructure folder and declares it, so the check reads past the empty-root gate.</summary>
    /// <param name="root">The fixture root folder to create the folder under.</param>
    static void CreateInfrastructure(string root)
    {
        Directory.CreateDirectory(Path.Combine(root, "apps", "api", "src", "Modules", "Library.Lending", "Infrastructure"));
        File.WriteAllText(
            Path.Combine(root, ".litespec/project.yaml"),
            """
            {"stacks": ["dotnet"], "stackConfig": {"dotnet": {
              "modules": "apps/api/src/Modules/Library.Lending"}}}
            """);
    }

    static void WriteProject(string root) =>
        File.WriteAllText(
            Path.Combine(root, ".litespec/project.yaml"),
            """
            {"stacks": ["dotnet"], "stackConfig": {"dotnet": {
              "modules": "src/App", "webapi": "src/WebApi"}}}
            """);

    /// <summary>Repository Implementation In Its Aggregate Persistence Folder Passes.</summary>
    /// <proves>litespec/core/backend-persistence#stage-aggregate-writes-through-domain-repositories</proves>
    [Fact]
    public void RepositoryImplementationInItsAggregatePersistenceFolderPasses()
    {
        using var temp = TestCli.Temp("checks");
        CreateInfrastructure(temp.Path);
        var model = PlacedLoanModel(temp.Path);
        model.Code.DomainTypes.Add(RepositoryInterface(temp.Path));
        model.Code.RepositoryImplementations.Add(new CodeRepositoryImplementation(
            "ILoanRepository", "LoanRepository",
            Path.Combine(temp.Path, "apps", "api", "src", "Modules", "Library.Lending", "Infrastructure", "Persistence", "Loans", "LoanRepository.cs"), 3));

        Assert.DoesNotContain("code-placement-drift", TestModel.Codes(model));
    }

    /// <summary>Repository Implementation Outside Its Aggregate Persistence Folder Reports.</summary>
    /// <proves>litespec/core/backend-persistence#stage-aggregate-writes-through-domain-repositories</proves>
    [Fact]
    public void RepositoryImplementationOutsideItsAggregatePersistenceFolderReports()
    {
        using var temp = TestCli.Temp("checks");
        CreateInfrastructure(temp.Path);
        var model = PlacedLoanModel(temp.Path);
        model.Code.DomainTypes.Add(RepositoryInterface(temp.Path));
        model.Code.RepositoryImplementations.Add(new CodeRepositoryImplementation(
            "ILoanRepository", "LoanRepository",
            Path.Combine(temp.Path, "apps", "api", "src", "Modules", "Library.Lending", "Infrastructure", "Persistence", "LoanRepository.cs"), 3));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "code-placement-drift");
        Assert.Contains("belongs in 'Infrastructure/Persistence/Loans/'", finding.Message);
    }

    /// <summary>A model whose borrow-copy use case changes the loan aggregate placed in its module folder.</summary>
    /// <param name="root">The fixture root folder the record files sit under.</param>
    /// <returns>The project model holding the use case and the aggregate.</returns>
    static ProjectModel PlacedLoanModel(string root) =>
        TestModel.Model(
            TestModel.Config(root),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(root, "borrow.yaml"), """{"status": "reviewed", "kind": "command", "writes": ["aggregate/lending.loan"]}"""),
            PlacedAggregate(root));

    /// <summary>The loan aggregate record in its module aggregates folder.</summary>
    /// <param name="root">The fixture root folder the record file sits under.</param>
    /// <returns>The aggregate record.</returns>
    static LiteSpec.Cli.Model.Records.Record PlacedAggregate(string root)
    {
        using var document = System.Text.Json.JsonDocument.Parse("""{"status": "reviewed", "states": [{"name": "Active", "meaning": "The loan is active."}]}""");

        return new LiteSpec.Cli.Model.Records.Record
        {
            Type = "aggregate",
            Id = "aggregate/lending.loan",
            Title = "aggregate/lending.loan",
            File = Path.Combine(root, ".litespec", "behavior", "modules", "lending", "aggregates", "loans", "aggregate.yaml"),
            Line = 1,
            Folder = ".litespec/behavior/modules/lending/aggregates/loans",
            Module = "lending",
            Raw = document.RootElement.Clone(),
        };
    }

    static void WriteSource(string root, string project, string file, string text)
    {
        var folder = Path.Combine(root, "src", project);
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, file), text);
    }
}
