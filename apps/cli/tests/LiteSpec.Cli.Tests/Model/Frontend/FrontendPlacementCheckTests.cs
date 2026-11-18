using System.Text.Json;
using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Code;
using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;
using Record = LiteSpec.Cli.Model.Records.Record;

namespace LiteSpec.Cli.Tests.Model.Frontend;

/// <summary>Proves the frontend placement check over hand-built models: a web module of an aggregate use case sits in its plural aggregate folder, and a module-level use case stays flat.</summary>
public sealed class FrontendPlacementCheckTests
{
    /// <summary>Flat Module For An Aggregate Use Case Reports Placement Drift.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact]
    public void FlatModuleForAnAggregateUseCaseReportsPlacementDrift()
    {
        using var temp = TestCli.Temp("frontend-placement");
        var model = BaseModel(temp.Path, underAggregate: true);
        AddModule(model, temp.Path, ["lending", "borrow-copy"], "use-case/lending.borrow-copy");

        var finding = Assert.Single(WithCode(model, "code-placement-drift"));
        Assert.Equal("use-case/lending.borrow-copy", finding.Subject);
        Assert.Contains("lending/loans/borrow-copy", finding.Message);
    }

    /// <summary>Nested Module For An Aggregate Use Case Reports No Placement Drift.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact]
    public void NestedModuleForAnAggregateUseCaseReportsNoPlacementDrift()
    {
        using var temp = TestCli.Temp("frontend-placement");
        var model = BaseModel(temp.Path, underAggregate: true);
        AddModule(model, temp.Path, ["lending", "loans", "borrow-copy"], "use-case/lending.borrow-copy");

        Assert.Empty(WithCode(model, "code-placement-drift"));
    }

    /// <summary>Module In Another Aggregate Folder Reports Placement Drift.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact]
    public void ModuleInAnotherAggregateFolderReportsPlacementDrift()
    {
        using var temp = TestCli.Temp("frontend-placement");
        var model = BaseModel(temp.Path, underAggregate: true);
        AddModule(model, temp.Path, ["lending", "holds", "borrow-copy"], "use-case/lending.borrow-copy");

        var finding = Assert.Single(WithCode(model, "code-placement-drift"));
        Assert.Equal("use-case/lending.borrow-copy", finding.Subject);
        Assert.Contains("lending/loans/borrow-copy", finding.Message);
    }

    /// <summary>Flat Module For A Module Level Use Case Reports No Placement Drift.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact]
    public void FlatModuleForAModuleLevelUseCaseReportsNoPlacementDrift()
    {
        using var temp = TestCli.Temp("frontend-placement");
        var model = BaseModel(temp.Path, underAggregate: false);
        AddModule(model, temp.Path, ["lending", "borrow-copy"], "use-case/lending.borrow-copy");

        Assert.Empty(WithCode(model, "code-placement-drift"));
    }

    /// <summary>Nested Module For A Module Level Use Case Reports Placement Drift.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact]
    public void NestedModuleForAModuleLevelUseCaseReportsPlacementDrift()
    {
        using var temp = TestCli.Temp("frontend-placement");
        var model = BaseModel(temp.Path, underAggregate: false);
        AddModule(model, temp.Path, ["lending", "loans", "borrow-copy"], "use-case/lending.borrow-copy");

        var finding = Assert.Single(WithCode(model, "code-placement-drift"));
        Assert.Equal("use-case/lending.borrow-copy", finding.Subject);
        Assert.Contains("lending/borrow-copy", finding.Message);
    }

    /// <summary>Marked Modules In Their Aggregate Folders Report No Placement Drift.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact]
    public void MarkedModulesInTheirAggregateFoldersReportNoPlacementDrift()
    {
        using var fixture = FrontendMarksFixture.Prepare("clean");
        var model = FrontendMarksFixture.Build(fixture.Path);

        Assert.Contains(model.Code.Elements, element => element.Kind == CodeKind.FrontendModule);

        Assert.DoesNotContain(Checker.Check(model).Findings, finding => finding.Code == "code-placement-drift");
    }

    static List<Finding> WithCode(ProjectModel model, string code) =>
        Checker.Check(model).Findings.Where(finding => finding.Code == code).ToList();

    static void AddModule(ProjectModel model, string root, string[] segments, string useCaseId)
    {
        var parts = new List<string> { root, "apps", "web", "src", "lib", "modules" };
        parts.AddRange(segments);
        parts.Add("litespec.ts");
        var file = Path.Combine([.. parts]);

        model.Code.Elements.Add(new MarkedElement(
            CodeKind.FrontendModule, string.Join('/', segments), null, file, 3,
            [new CodeMark(useCaseId, MarkStyle.Typed, MarkVerb.Implements, file, 3)], InTests: false));
    }

    static ProjectModel BaseModel(string root, bool underAggregate)
    {
        Directory.CreateDirectory(Path.Combine(root, ".litespec"));
        File.WriteAllText(
            Path.Combine(root, ".litespec", "project.yaml"),
            """
            {
              "owners": ["lending-team"],
              "frontends": [
                  { "name": "web", "path": "apps/web", "platform": "svelte" }
                ], "paths": {
              }
            }
            """);
        Directory.CreateDirectory(Path.Combine(root, "apps", "web", "src", "lib", "modules"));

        var model = new ProjectModel { Config = ConsumerConfig.Load(root) };

        model.Records.Add(Rec(
            "module", "module/lending",
            Path.Combine(root, ".litespec", "behavior", "modules", "lending", "module.yaml"),
            ".litespec/behavior/modules/lending", "lending"));

        if (underAggregate)
        {
            model.Records.Add(Rec(
                "aggregate", "aggregate/lending.loan",
                Path.Combine(root, ".litespec", "behavior", "modules", "lending", "aggregates", "loans", "aggregate.yaml"),
                ".litespec/behavior/modules/lending/aggregates/loans", "lending"));
            model.Records.Add(Rec(
                "use-case", "use-case/lending.borrow-copy",
                Path.Combine(root, ".litespec", "behavior", "modules", "lending", "aggregates", "loans", "use-cases", "borrow-copy", "borrow-copy.yaml"),
                ".litespec/behavior/modules/lending/aggregates/loans/use-cases/borrow-copy", "lending"));
        }
        else
        {
            model.Records.Add(Rec(
                "use-case", "use-case/lending.borrow-copy",
                Path.Combine(root, ".litespec", "behavior", "modules", "lending", "use-cases", "borrow-copy", "borrow-copy.yaml"),
                ".litespec/behavior/modules/lending/use-cases/borrow-copy", "lending"));
        }

        return model;
    }

    static Record Rec(string type, string id, string file, string folder, string module)
    {
        using var document = JsonDocument.Parse("{}");

        return new Record
        {
            Type = type,
            Id = id,
            Title = id,
            File = file,
            Line = 1,
            Folder = folder,
            Module = module,
            Raw = document.RootElement.Clone(),
        };
    }
}
