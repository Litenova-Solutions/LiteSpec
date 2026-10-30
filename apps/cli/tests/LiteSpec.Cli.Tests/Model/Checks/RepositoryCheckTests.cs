using LiteSpec.Model;
using LiteSpec.Model.Readers.Dotnet;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Checks;

/// <summary>Proves the repository-unimplemented check against hand-built models and reader fixtures.</summary>
public sealed class RepositoryCheckTests
{
    /// <proves>litespec/backend/persistence#stage-aggregate-writes-through-domain-repositories</proves>
    [Fact]
    public void Changed_Aggregate_Without_An_Implementation_Reports()
    {
        using var temp = Cli.Temp("checks");
        CreateInfrastructure(temp.Path);
        var model = ChangedLoanModel(temp.Path);
        model.Code.DomainTypes.Add(RepositoryInterface(temp.Path));

        Assert.Contains("repository-unimplemented", TestModel.Codes(model));
    }

    /// <proves>litespec/backend/persistence#stage-aggregate-writes-through-domain-repositories</proves>
    [Fact]
    public void Changed_Aggregate_With_An_Implementation_Passes()
    {
        using var temp = Cli.Temp("checks");
        CreateInfrastructure(temp.Path);
        var model = ChangedLoanModel(temp.Path);
        model.Code.DomainTypes.Add(RepositoryInterface(temp.Path));
        model.Code.RepositoryImplementations.Add(new CodeRepositoryImplementation(
            "ILoanRepository", "LoanRepository", Path.Combine(temp.Path, "LoanRepository.cs"), 3));

        Assert.DoesNotContain("repository-unimplemented", TestModel.Codes(model));
    }

    /// <proves>litespec/backend/persistence#stage-aggregate-writes-through-domain-repositories</proves>
    [Fact]
    public void Reader_Fixture_Without_An_Implementation_Reports()
    {
        using var temp = Cli.Temp("checks");
        WriteProject(temp.Path);
        WriteSource(temp.Path, "Domain", "ILoanRepository.cs", """
            namespace Library.Domain.Lending.Loans;

            public interface ILoanRepository
            {
                Loan Load(Guid id);
            }
            """);
        Directory.CreateDirectory(Path.Combine(temp.Path, "src", "Infra"));
        var model = ChangedLoanModel(temp.Path);
        model.Code = new DotnetBackendReader().Read(ConsumerConfig.Load(temp.Path));

        Assert.Contains("repository-unimplemented", TestModel.Codes(model));
    }

    /// <proves>litespec/backend/persistence#stage-aggregate-writes-through-domain-repositories</proves>
    [Fact]
    public void Reader_Fixture_With_An_Implementation_Passes()
    {
        using var temp = Cli.Temp("checks");
        WriteProject(temp.Path);
        WriteSource(temp.Path, "Domain", "ILoanRepository.cs", """
            namespace Library.Domain.Lending.Loans;

            public interface ILoanRepository
            {
                Loan Load(Guid id);
            }
            """);
        WriteSource(temp.Path, "Infra", "LoanRepository.cs", """
            namespace Library.Infrastructure.Lending.Loans;

            public sealed class LoanRepository : ILoanRepository
            {
                public Loan Load(Guid id) => new Loan();
            }
            """);
        var model = ChangedLoanModel(temp.Path);
        model.Code = new DotnetBackendReader().Read(ConsumerConfig.Load(temp.Path));

        Assert.DoesNotContain("repository-unimplemented", TestModel.Codes(model));
    }

    /// <summary>A model whose borrow-copy use case changes the loan aggregate.</summary>
    static ProjectModel ChangedLoanModel(string root) =>
        TestModel.Model(
            TestModel.Config(root),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(root, "borrow.md"), """{"status": "reviewed", "kind": "command", "changes": ["aggregate/lending.loan"]}"""),
            TestModel.Record("aggregate", "aggregate/lending.loan", Path.Combine(root, "loan.md"), """{"status": "reviewed", "states": [{"name": "Active", "meaning": "The loan is active."}]}"""));

    /// <summary>The loan's domain repository interface as the Domain reader reports it.</summary>
    static DomainType RepositoryInterface(string root) =>
        new("ILoanRepository", "Library.Domain.Lending.Loans", Path.Combine(root, "ILoanRepository.cs"), 1, false);

    /// <summary>Creates the Infrastructure folder the default project layout resolves to.</summary>
    static void CreateInfrastructure(string root) =>
        Directory.CreateDirectory(Path.Combine(root, "apps", "api", "src", "Library.Infrastructure"));

    static void WriteProject(string root) =>
        File.WriteAllText(
            Path.Combine(root, "litespec.project.json"),
            """
            {"stacks": ["dotnet"], "stackConfig": {"dotnet": {
              "application": "src/App", "domain": "src/Domain",
              "webapi": "src/WebApi", "infrastructure": "src/Infra"}}}
            """);

    static void WriteSource(string root, string project, string file, string text)
    {
        var folder = Path.Combine(root, "src", project);
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, file), text);
    }
}
