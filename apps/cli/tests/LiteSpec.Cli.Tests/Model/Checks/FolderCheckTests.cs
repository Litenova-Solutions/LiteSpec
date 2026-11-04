using System.Text.Json;
using LiteSpec.Model;
using LiteSpec.Model.Checks;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Checks;

/// <summary>Proves the folder checks: every folder is one of the closed set of its layer and level, every module folder names a declared module, and every record sits in the folder and level its type allows, at tier G.</summary>
public sealed class FolderCheckTests
{
    /// <summary>Closed Intent Folders Pass.</summary>
    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void Closed_Intent_Folders_Pass()
    {
        using var temp = Cli.Temp("checks");
        DirAt(temp.Path, ".litespec", "intent", "goals");
        DirAt(temp.Path, ".litespec", "intent", "modules", "lending", "features");
        DirAt(temp.Path, ".litespec", "intent", "modules", "lending", "goals");
        var moduleFile = PathAt(temp.Path, ".litespec", "intent", "modules", "lending", "module.yaml");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/lending", moduleFile, """{"dependsOn": []}"""));

        Assert.DoesNotContain(Run(model), found => found.Code == "folder-invalid");
    }

    /// <summary>Intent Unknown Folder Reports.</summary>
    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void Intent_Unknown_Folder_Reports()
    {
        using var temp = Cli.Temp("checks");
        var directory = DirAt(temp.Path, ".litespec", "intent", "writing");
        var model = TestModel.Model(TestModel.Config(temp.Path));

        var finding = Assert.Single(Run(model), found => found.Code == "folder-invalid");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Folder '.litespec/intent/writing' sits outside the closed folder set of its layer and level.", finding.Message);
        Assert.Equal(directory, finding.Path);
        Assert.Equal(1, finding.Line);
        Assert.Null(finding.Subject);
    }

    /// <summary>Governance Known Gaps Reports.</summary>
    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void Governance_Known_Gaps_Reports()
    {
        using var temp = Cli.Temp("checks");
        DirAt(temp.Path, ".litespec", "governance", "known-gaps");
        var model = TestModel.Model(TestModel.Config(temp.Path));

        var finding = Assert.Single(Run(model), found => found.Code == "folder-invalid");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Folder '.litespec/governance/known-gaps' sits outside the closed folder set of its layer and level.", finding.Message);
        Assert.Null(finding.Subject);
    }

    /// <summary>Behavior Unknown Module Subfolder Reports.</summary>
    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void Behavior_Unknown_Module_Subfolder_Reports()
    {
        using var temp = Cli.Temp("checks");
        DirAt(temp.Path, ".litespec", "behavior", "modules", "lending", "widgets");
        var moduleFile = PathAt(temp.Path, ".litespec", "intent", "modules", "lending", "module.yaml");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/lending", moduleFile, """{"dependsOn": []}"""));

        var finding = Assert.Single(Run(model), found => found.Code == "folder-invalid");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Folder '.litespec/behavior/modules/lending/widgets' sits outside the closed folder set of its layer and level.", finding.Message);
        Assert.Null(finding.Subject);
    }

    /// <summary>Behavior Aggregate Folder Passes.</summary>
    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void Behavior_Aggregate_Folder_Passes()
    {
        using var temp = Cli.Temp("checks");
        var aggregate = PathAt(temp.Path, ".litespec", "behavior", "modules", "lending", "loans", "aggregate.yaml");
        File.WriteAllText(aggregate, "type: aggregate\nid: aggregate/lending.loan\ntitle: Loan\n");
        var moduleFile = PathAt(temp.Path, ".litespec", "intent", "modules", "lending", "module.yaml");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/lending", moduleFile, """{"dependsOn": []}"""));

        Assert.DoesNotContain(Run(model), found => found.Code == "folder-invalid");
    }

    /// <summary>Behavior Operations Folder Reports.</summary>
    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void Behavior_Operations_Folder_Reports()
    {
        using var temp = Cli.Temp("checks");
        DirAt(temp.Path, ".litespec", "behavior", "operations");
        var model = TestModel.Model(TestModel.Config(temp.Path));

        var finding = Assert.Single(Run(model), found => found.Code == "folder-invalid");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Folder '.litespec/behavior/operations' sits outside the closed folder set of its layer and level.", finding.Message);
        Assert.Null(finding.Subject);
    }

    /// <summary>Implementation Ui Folder Reports.</summary>
    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void Implementation_Ui_Folder_Reports()
    {
        using var temp = Cli.Temp("checks");
        DirAt(temp.Path, ".litespec", "implementation", "ui");
        var model = TestModel.Model(TestModel.Config(temp.Path));

        var finding = Assert.Single(Run(model), found => found.Code == "folder-invalid");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Folder '.litespec/implementation/ui' sits outside the closed folder set of its layer and level.", finding.Message);
        Assert.Null(finding.Subject);
    }

    /// <summary>Type Folder Child Reports.</summary>
    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void Type_Folder_Child_Reports()
    {
        using var temp = Cli.Temp("checks");
        DirAt(temp.Path, ".litespec", "intent", "goals", "archive");
        var model = TestModel.Model(TestModel.Config(temp.Path));

        var finding = Assert.Single(Run(model), found => found.Code == "folder-invalid");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Folder '.litespec/intent/goals/archive' sits outside the closed folder set of its layer and level.", finding.Message);
        Assert.Null(finding.Subject);
    }

    /// <summary>Unknown Top Folder Reports.</summary>
    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void Unknown_Top_Folder_Reports()
    {
        using var temp = Cli.Temp("checks");
        DirAt(temp.Path, ".litespec", "product");
        var model = TestModel.Model(TestModel.Config(temp.Path));

        var finding = Assert.Single(Run(model), found => found.Code == "folder-invalid");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Folder '.litespec/product' sits outside the closed folder set of its layer and level.", finding.Message);
        Assert.Null(finding.Subject);
    }

    /// <summary>Proof And Generated Folders Skipped.</summary>
    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void Proof_And_Generated_Folders_Skipped()
    {
        using var temp = Cli.Temp("checks");
        DirAt(temp.Path, ".litespec", "proof", "registers", "deep");
        DirAt(temp.Path, ".litespec", "model", "shards");
        DirAt(temp.Path, ".litespec", "book", "pages");
        var model = TestModel.Model(TestModel.Config(temp.Path));

        Assert.DoesNotContain(Run(model), found => found.Code == "folder-invalid");
    }

    /// <summary>Implementation Entry Point Nesting Passes.</summary>
    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void Implementation_Entry_Point_Nesting_Passes()
    {
        using var temp = Cli.Temp("checks");
        DirAt(temp.Path, ".litespec", "implementation", "entry-points", "library-app", "catalog");
        var model = TestModel.Model(TestModel.Config(temp.Path));

        Assert.DoesNotContain(Run(model), found => found.Code == "folder-invalid");
    }

    /// <summary>Implementation Module Unknown Folder Reports.</summary>
    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void Implementation_Module_Unknown_Folder_Reports()
    {
        using var temp = Cli.Temp("checks");
        DirAt(temp.Path, ".litespec", "implementation", "modules", "lending", "widgets");
        var moduleFile = PathAt(temp.Path, ".litespec", "intent", "modules", "lending", "module.yaml");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/lending", moduleFile, """{"dependsOn": []}"""));

        var finding = Assert.Single(Run(model), found => found.Code == "folder-invalid");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Folder '.litespec/implementation/modules/lending/widgets' sits outside the closed folder set of its layer and level.", finding.Message);
        Assert.Null(finding.Subject);
    }

    /// <summary>Governance Module Unknown Folder Reports.</summary>
    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void Governance_Module_Unknown_Folder_Reports()
    {
        using var temp = Cli.Temp("checks");
        DirAt(temp.Path, ".litespec", "governance", "modules", "lending", "reviews");
        var moduleFile = PathAt(temp.Path, ".litespec", "intent", "modules", "lending", "module.yaml");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/lending", moduleFile, """{"dependsOn": []}"""));

        var finding = Assert.Single(Run(model), found => found.Code == "folder-invalid");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Folder '.litespec/governance/modules/lending/reviews' sits outside the closed folder set of its layer and level.", finding.Message);
        Assert.Null(finding.Subject);
    }

    /// <summary>Intent Module Actors Folder Reports.</summary>
    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void Intent_Module_Actors_Folder_Reports()
    {
        using var temp = Cli.Temp("checks");
        DirAt(temp.Path, ".litespec", "intent", "modules", "lending", "actors");
        var moduleFile = PathAt(temp.Path, ".litespec", "intent", "modules", "lending", "module.yaml");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/lending", moduleFile, """{"dependsOn": []}"""));

        var finding = Assert.Single(Run(model), found => found.Code == "folder-invalid");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Folder '.litespec/intent/modules/lending/actors' sits outside the closed folder set of its layer and level.", finding.Message);
        Assert.Null(finding.Subject);
    }

    /// <summary>Behavior Module Events Folder Reports When Not Flat.</summary>
    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void Behavior_Module_Events_Folder_Reports_When_Not_Flat()
    {
        using var temp = Cli.Temp("checks");
        DirAt(temp.Path, ".litespec", "behavior", "modules", "lending", "events");
        var loans = PathAt(temp.Path, ".litespec", "behavior", "modules", "lending", "loans", "aggregate.yaml");
        File.WriteAllText(loans, "type: aggregate\nid: aggregate/lending.loan\ntitle: Loan\n");
        var holds = PathAt(temp.Path, ".litespec", "behavior", "modules", "lending", "holds", "aggregate.yaml");
        File.WriteAllText(holds, "type: aggregate\nid: aggregate/lending.hold\ntitle: Hold\n");
        var moduleFile = PathAt(temp.Path, ".litespec", "intent", "modules", "lending", "module.yaml");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/lending", moduleFile, """{"dependsOn": []}"""),
            TestModel.Record("aggregate", "aggregate/lending.loan", loans, """{}"""),
            TestModel.Record("aggregate", "aggregate/lending.hold", holds, """{}"""));

        var finding = Assert.Single(Run(model), found => found.Code == "folder-invalid");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Folder '.litespec/behavior/modules/lending/events' sits outside the closed folder set of its layer and level.", finding.Message);
        Assert.Null(finding.Subject);
    }

    /// <summary>Behavior Module Events Folder Passes When Flat.</summary>
    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void Behavior_Module_Events_Folder_Passes_When_Flat()
    {
        using var temp = Cli.Temp("checks");
        DirAt(temp.Path, ".litespec", "behavior", "modules", "posts", "events");
        var moduleFile = PathAt(temp.Path, ".litespec", "intent", "modules", "posts", "module.yaml");
        var aggregateFile = PathAt(temp.Path, ".litespec", "behavior", "modules", "posts", "aggregate.yaml");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/posts", moduleFile, """{"dependsOn": []}"""),
            TestModel.Record("aggregate", "aggregate/posts.posts", aggregateFile, """{}"""));

        Assert.DoesNotContain(Run(model), found => found.Code == "folder-invalid");
    }

    /// <summary>Declared Module Passes.</summary>
    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void Declared_Module_Passes()
    {
        using var temp = Cli.Temp("checks");
        DirAt(temp.Path, ".litespec", "intent", "modules", "lending");
        DirAt(temp.Path, ".litespec", "behavior", "modules", "lending");
        DirAt(temp.Path, ".litespec", "implementation", "modules", "lending");
        DirAt(temp.Path, ".litespec", "governance", "modules", "lending");
        var moduleFile = PathAt(temp.Path, ".litespec", "intent", "modules", "lending", "module.yaml");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/lending", moduleFile, """{"dependsOn": []}"""));

        Assert.DoesNotContain(Run(model), found => found.Code == "module-undeclared");
    }

    /// <summary>Undeclared Behavior Module Reports.</summary>
    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void Undeclared_Behavior_Module_Reports()
    {
        using var temp = Cli.Temp("checks");
        var directory = DirAt(temp.Path, ".litespec", "behavior", "modules", "ghost");
        var model = TestModel.Model(TestModel.Config(temp.Path));

        var finding = Assert.Single(Run(model), found => found.Code == "module-undeclared");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Module folder '.litespec/behavior/modules/ghost' names no declared module.", finding.Message);
        Assert.Equal(directory, finding.Path);
        Assert.Equal(1, finding.Line);
        Assert.Null(finding.Subject);
    }

    /// <summary>Undeclared Intent Module Reports.</summary>
    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void Undeclared_Intent_Module_Reports()
    {
        using var temp = Cli.Temp("checks");
        DirAt(temp.Path, ".litespec", "intent", "modules", "ghost");
        var model = TestModel.Model(TestModel.Config(temp.Path));

        var finding = Assert.Single(Run(model), found => found.Code == "module-undeclared");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Module folder '.litespec/intent/modules/ghost' names no declared module.", finding.Message);
        Assert.Null(finding.Subject);
    }

    /// <summary>Undeclared Modules Report Per Folder.</summary>
    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact]
    public void Undeclared_Modules_Report_Per_Folder()
    {
        using var temp = Cli.Temp("checks");
        DirAt(temp.Path, ".litespec", "implementation", "modules", "ghost");
        DirAt(temp.Path, ".litespec", "governance", "modules", "ghost");
        var model = TestModel.Model(TestModel.Config(temp.Path));

        Assert.Equal(2, Run(model).Count(found => found.Code == "module-undeclared"));
    }

    /// <summary>Feature At Product Level Reports.</summary>
    /// <proves>litespec/core/model#write-every-record-in-its-home</proves>
    [Fact]
    public void Feature_At_Product_Level_Reports()
    {
        using var temp = Cli.Temp("checks");
        var file = PathAt(temp.Path, ".litespec", "intent", "goals", "stray-feature.yaml");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("feature", "feature/stray-feature", file, """{"solves": ["problem/overdue-loans-pile-up"]}"""));

        var finding = Assert.Single(Run(model), found => found.Code == "record-placement-invalid");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Feature 'feature/stray-feature' sits in 'intent/goals', but features live in 'intent/modules/<m>/features/'.", finding.Message);
        Assert.Equal(file, finding.Path);
        Assert.Equal(1, finding.Line);
        Assert.Equal("feature/stray-feature", finding.Subject);
    }

    /// <summary>Journey Under Module Reports.</summary>
    /// <proves>litespec/core/model#write-every-record-in-its-home</proves>
    [Fact]
    public void Journey_Under_Module_Reports()
    {
        using var temp = Cli.Temp("checks");
        var file = PathAt(temp.Path, ".litespec", "intent", "modules", "lending", "goals", "stray-journey.yaml");
        var moduleFile = PathAt(temp.Path, ".litespec", "intent", "modules", "lending", "module.yaml");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/lending", moduleFile, """{"dependsOn": []}"""),
            TestModel.Record("journey", "journey/stray-journey", file, """{}"""));

        var finding = Assert.Single(Run(model), found => found.Code == "record-placement-invalid");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Journey 'journey/stray-journey' sits in 'intent/modules/lending/goals', but journeys live in 'intent/journeys/'.", finding.Message);
        Assert.Equal("journey/stray-journey", finding.Subject);
    }

    /// <summary>Actor Under Module Reports.</summary>
    /// <proves>litespec/core/model#write-every-record-in-its-home</proves>
    [Fact]
    public void Actor_Under_Module_Reports()
    {
        using var temp = Cli.Temp("checks");
        var file = PathAt(temp.Path, ".litespec", "intent", "modules", "lending", "goals", "stray-actor.yaml");
        var moduleFile = PathAt(temp.Path, ".litespec", "intent", "modules", "lending", "module.yaml");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/lending", moduleFile, """{"dependsOn": []}"""),
            TestModel.Record("actor", "actor/visitor", file, """{}"""));

        var finding = Assert.Single(Run(model), found => found.Code == "record-placement-invalid");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Actor 'actor/visitor' sits in 'intent/modules/lending/goals', but actors live in 'intent/actors/'.", finding.Message);
        Assert.Equal("actor/visitor", finding.Subject);
    }

    /// <summary>Role Under Module Reports.</summary>
    /// <proves>litespec/core/model#write-every-record-in-its-home</proves>
    [Fact]
    public void Role_Under_Module_Reports()
    {
        using var temp = Cli.Temp("checks");
        var file = PathAt(temp.Path, ".litespec", "intent", "modules", "lending", "goals", "stray-role.yaml");
        var moduleFile = PathAt(temp.Path, ".litespec", "intent", "modules", "lending", "module.yaml");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/lending", moduleFile, """{"dependsOn": []}"""),
            TestModel.Record("role", "role/guest", file, """{}"""));

        var finding = Assert.Single(Run(model), found => found.Code == "record-placement-invalid");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Role 'role/guest' sits in 'intent/modules/lending/goals', but roles live in 'intent/roles/'.", finding.Message);
        Assert.Equal("role/guest", finding.Subject);
    }

    /// <summary>Requirement Outside Features Reports.</summary>
    /// <proves>litespec/core/model#write-every-record-in-its-home</proves>
    [Fact]
    public void Requirement_Outside_Features_Reports()
    {
        using var temp = Cli.Temp("checks");
        var file = PathAt(temp.Path, ".litespec", "intent", "problems", "stray-requirement.yaml");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("requirement", "requirement/hold-queue.max-active-holds", file, """{}"""));

        var finding = Assert.Single(Run(model), found => found.Code == "record-placement-invalid");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Requirement 'requirement/hold-queue.max-active-holds' sits in 'intent/problems', but requirements live in their feature file under 'intent/modules/<m>/features/'.", finding.Message);
        Assert.Equal("requirement/hold-queue.max-active-holds", finding.Subject);
    }

    /// <summary>Use Case Directly Under Module Reports.</summary>
    /// <proves>litespec/core/model#write-every-record-in-its-home</proves>
    [Fact]
    public void Use_Case_Directly_Under_Module_Reports()
    {
        using var temp = Cli.Temp("checks");
        var file = PathAt(temp.Path, ".litespec", "behavior", "modules", "lending", "stray.yaml");
        var moduleFile = PathAt(temp.Path, ".litespec", "intent", "modules", "lending", "module.yaml");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/lending", moduleFile, """{"dependsOn": []}"""),
            TestModel.Record("use-case", "use-case/lending.stray", file, """{"kind": "command"}"""));

        var finding = Assert.Single(Run(model), found => found.Code == "record-placement-invalid");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Use case 'use-case/lending.stray' sits in 'behavior/modules/lending', but use cases live in 'behavior/modules/<m>/<aggregate>/' or 'behavior/modules/<m>/use-cases/'.", finding.Message);
        Assert.Equal("use-case/lending.stray", finding.Subject);
    }

    /// <summary>Use Case Placed Passes.</summary>
    /// <proves>litespec/core/model#write-every-record-in-its-home</proves>
    [Fact]
    public void Use_Case_Placed_Passes()
    {
        using var temp = Cli.Temp("checks");
        var aggregate = PathAt(temp.Path, ".litespec", "behavior", "modules", "lending", "loans", "aggregate.yaml");
        File.WriteAllText(aggregate, "type: aggregate\nid: aggregate/lending.loan\ntitle: Loan\n");
        var underAggregate = PathAt(temp.Path, ".litespec", "behavior", "modules", "lending", "loans", "borrow-copy.yaml");
        var inTypeFolder = PathAt(temp.Path, ".litespec", "behavior", "modules", "lending", "use-cases", "list-loans.yaml");
        var moduleFile = PathAt(temp.Path, ".litespec", "intent", "modules", "lending", "module.yaml");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/lending", moduleFile, """{"dependsOn": []}"""),
            TestModel.Record("aggregate", "aggregate/lending.loan", aggregate, """{}"""),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", underAggregate, """{"kind": "command"}"""),
            TestModel.Record("use-case", "use-case/lending.list-loans", inTypeFolder, """{"kind": "query"}"""));

        Assert.DoesNotContain(Run(model), found => found.Code == "record-placement-invalid");
    }

    /// <summary>Screen Outside Entry Points Reports.</summary>
    /// <proves>litespec/core/model#write-every-record-in-its-home</proves>
    [Fact]
    public void Screen_Outside_Entry_Points_Reports()
    {
        using var temp = Cli.Temp("checks");
        var file = PathAt(temp.Path, ".litespec", "behavior", "modules", "lending", "reactions", "loans.yaml");
        var moduleFile = PathAt(temp.Path, ".litespec", "intent", "modules", "lending", "module.yaml");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/lending", moduleFile, """{"dependsOn": []}"""),
            TestModel.Record("screen", "screen/library-app.loans", file, """{"address": "/loans"}"""));

        var finding = Assert.Single(Run(model), found => found.Code == "record-placement-invalid");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Screen 'screen/library-app.loans' sits in 'behavior/modules/lending/reactions', but screens live in 'behavior/entry-points/<entry-point>/screens/'.", finding.Message);
        Assert.Equal("screen/library-app.loans", finding.Subject);
    }

    /// <summary>Screen Placed Passes.</summary>
    /// <proves>litespec/core/model#write-every-record-in-its-home</proves>
    [Fact]
    public void Screen_Placed_Passes()
    {
        using var temp = Cli.Temp("checks");
        var file = PathAt(temp.Path, ".litespec", "behavior", "entry-points", "library-app", "screens", "loans.yaml");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("screen", "screen/library-app.loans", file, """{"address": "/loans"}"""));

        Assert.DoesNotContain(Run(model), found => found.Code == "record-placement-invalid");
    }

    /// <summary>Rule In Module Yaml Reports.</summary>
    /// <proves>litespec/core/model#hold-module-rules-in-rule-sets</proves>
    [Fact]
    public void Rule_In_Module_Yaml_Reports()
    {
        using var temp = Cli.Temp("checks");
        var moduleFile = PathAt(temp.Path, ".litespec", "intent", "modules", "lending", "module.yaml");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/lending", moduleFile, """{"dependsOn": []}"""),
            TestModel.Record("rule", "rule/loan.stray", moduleFile, 10, """{"kind": "invariant", "statement": "A loan lasts 21 days."}"""));

        var finding = Assert.Single(Run(model), found => found.Code == "record-placement-invalid");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Rule 'rule/loan.stray' sits in '.litespec/intent/modules/lending/module.yaml', but module rules live in rule-set files under 'behavior/modules/<m>/rules/'.", finding.Message);
        Assert.Equal(moduleFile, finding.Path);
        Assert.Equal(10, finding.Line);
        Assert.Equal("rule/loan.stray", finding.Subject);
    }

    /// <summary>Module Holding Rules Field Reports.</summary>
    /// <proves>litespec/core/model#hold-module-rules-in-rule-sets</proves>
    [Fact]
    public void Module_Holding_Rules_Field_Reports()
    {
        using var temp = Cli.Temp("checks");
        var moduleFile = PathAt(temp.Path, ".litespec", "intent", "modules", "lending", "module.yaml");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/lending", moduleFile, """{"dependsOn": [], "rules": ["rule/loan.stray"]}"""));

        var finding = Assert.Single(Run(model), found => found.Code == "record-placement-invalid");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Module 'module/lending' holds rules in '.litespec/intent/modules/lending/module.yaml'; module rules live in rule-set files under 'behavior/modules/<m>/rules/'.", finding.Message);
        Assert.Equal(moduleFile, finding.Path);
        Assert.Equal(1, finding.Line);
        Assert.Equal("module/lending", finding.Subject);
    }

    /// <summary>Module Placed Passes.</summary>
    /// <proves>litespec/core/model#write-every-record-in-its-home</proves>
    [Fact]
    public void Module_Placed_Passes()
    {
        using var temp = Cli.Temp("checks");
        var moduleFile = PathAt(temp.Path, ".litespec", "intent", "modules", "lending", "module.yaml");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/lending", moduleFile, """{"dependsOn": []}"""));

        Assert.DoesNotContain(Run(model), found => found.Code == "record-placement-invalid");
    }

    /// <summary>Runbook In Behavior Reports.</summary>
    /// <proves>litespec/core/model#write-every-record-in-its-home</proves>
    [Fact]
    public void Runbook_In_Behavior_Reports()
    {
        using var temp = Cli.Temp("checks");
        var file = PathAt(temp.Path, ".litespec", "behavior", "rules", "clear-stuck-holds.yaml");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("runbook", "runbook/clear-stuck-holds", file, """{}"""));

        var finding = Assert.Single(Run(model), found => found.Code == "record-placement-invalid");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Runbook 'runbook/clear-stuck-holds' sits in 'behavior/rules', but runbooks live in 'implementation/runbooks/' or 'implementation/modules/<m>/runbooks/'.", finding.Message);
        Assert.Equal("runbook/clear-stuck-holds", finding.Subject);
    }

    /// <summary>Runbook Placed Passes.</summary>
    /// <proves>litespec/core/model#write-every-record-in-its-home</proves>
    [Fact]
    public void Runbook_Placed_Passes()
    {
        using var temp = Cli.Temp("checks");
        var product = PathAt(temp.Path, ".litespec", "implementation", "runbooks", "clear-stuck-holds.yaml");
        var module = PathAt(temp.Path, ".litespec", "implementation", "modules", "lending", "runbooks", "lending-nightly.yaml");
        var moduleFile = PathAt(temp.Path, ".litespec", "intent", "modules", "lending", "module.yaml");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/lending", moduleFile, """{"dependsOn": []}"""),
            TestModel.Record("runbook", "runbook/clear-stuck-holds", product, """{}"""),
            TestModel.Record("runbook", "runbook/lending-nightly", module, """{}"""));

        Assert.DoesNotContain(Run(model), found => found.Code == "record-placement-invalid");
    }

    /// <summary>Record Under Proof Reports.</summary>
    /// <proves>litespec/core/model#write-every-record-in-its-home</proves>
    [Fact]
    public void Record_Under_Proof_Reports()
    {
        using var temp = Cli.Temp("checks");
        var file = PathAt(temp.Path, ".litespec", "proof", "registers", "old-goal.yaml");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("goal", "goal/old-goal", file, """{}"""));

        var finding = Assert.Single(Run(model), found => found.Code == "record-placement-invalid");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Goal 'goal/old-goal' sits in 'proof/registers', but goals live in 'intent/goals/' or 'intent/modules/<m>/goals/'.", finding.Message);
        Assert.Equal("goal/old-goal", finding.Subject);
    }

    /// <summary>Decision In Implementation Passes.</summary>
    /// <proves>litespec/core/model#write-every-record-in-its-home</proves>
    [Fact]
    public void Decision_In_Implementation_Passes()
    {
        using var temp = Cli.Temp("checks");
        var product = PathAt(temp.Path, ".litespec", "implementation", "decisions", "0002-render-engine.yaml");
        var module = PathAt(temp.Path, ".litespec", "implementation", "modules", "lending", "decisions", "0003-nightly-scan.yaml");
        var moduleFile = PathAt(temp.Path, ".litespec", "intent", "modules", "lending", "module.yaml");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/lending", moduleFile, """{"dependsOn": []}"""),
            TestModel.Record("decision", "decision/0002-render-engine", product, """{}"""),
            TestModel.Record("decision", "decision/0003-nightly-scan", module, """{}"""));

        Assert.DoesNotContain(Run(model), found => found.Code == "record-placement-invalid");
    }

    /// <summary>Permission Misplaced Reports.</summary>
    /// <proves>litespec/core/model#write-every-record-in-its-home</proves>
    [Fact]
    public void Permission_Misplaced_Reports()
    {
        using var temp = Cli.Temp("checks");
        var file = PathAt(temp.Path, ".litespec", "behavior", "modules", "lending", "reactions", "borrow-copy.yaml");
        var moduleFile = PathAt(temp.Path, ".litespec", "intent", "modules", "lending", "module.yaml");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/lending", moduleFile, """{"dependsOn": []}"""),
            TestModel.Record("permission", "permission/borrow-copy", file, """{}"""));

        var finding = Assert.Single(Run(model), found => found.Code == "record-placement-invalid");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Permission 'permission/borrow-copy' sits in 'behavior/modules/lending/reactions', but permissions live in 'behavior/modules/<m>/permissions.yaml'.", finding.Message);
        Assert.Equal("permission/borrow-copy", finding.Subject);
    }

    /// <summary>Flat Module Passes.</summary>
    /// <proves>litespec/core/model#write-every-record-in-its-home</proves>
    [Fact]
    public void Flat_Module_Passes()
    {
        using var temp = Cli.Temp("checks");
        var aggregateFile = PathAt(temp.Path, ".litespec", "behavior", "modules", "posts", "aggregate.yaml");
        var useCaseFile = PathAt(temp.Path, ".litespec", "behavior", "modules", "posts", "create-post.yaml");
        var moduleFile = PathAt(temp.Path, ".litespec", "intent", "modules", "posts", "module.yaml");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/posts", moduleFile, """{"dependsOn": []}"""),
            TestModel.Record("aggregate", "aggregate/posts.posts", aggregateFile, """{}"""),
            TestModel.Record("use-case", "use-case/posts.create-post", useCaseFile, """{"kind": "command"}"""),
            TestModel.Record("event", "event/posts.created", aggregateFile, """{}"""));

        Assert.Empty(Run(model));
    }

    /// <summary>Valid Layout Passes.</summary>
    /// <proves>litespec/core/model#write-every-record-in-its-home</proves>
    [Fact]
    public void Valid_Layout_Passes()
    {
        using var temp = Cli.Temp("checks");
        var root = temp.Path;
        var aggregate = PathAt(root, ".litespec", "behavior", "modules", "lending", "loans", "aggregate.yaml");
        File.WriteAllText(aggregate, "type: aggregate\nid: aggregate/lending.loan\ntitle: Loan\n");
        var model = TestModel.Model(
            TestModel.Config(root),
            TestModel.Record("product", "product/library", PathAt(root, ".litespec", "intent", "product.yaml"), """{}"""),
            TestModel.Record("goal", "goal/faster-pickup", PathAt(root, ".litespec", "intent", "goals", "faster-pickup.yaml"), """{}"""),
            TestModel.Record("problem", "problem/overdue-loans-pile-up", PathAt(root, ".litespec", "intent", "modules", "lending", "problems", "overdue-loans-pile-up.yaml"), """{}"""),
            TestModel.Record("feature", "feature/hold-queue", PathAt(root, ".litespec", "intent", "modules", "lending", "features", "hold-queue.yaml"), """{}"""),
            TestModel.Record("requirement", "requirement/hold-queue.max-active-holds", PathAt(root, ".litespec", "intent", "modules", "lending", "features", "hold-queue.yaml"), """{}"""),
            TestModel.Record("constraint", "constraint/library-privacy-law", PathAt(root, ".litespec", "intent", "constraints", "library-privacy-law.yaml"), """{}"""),
            TestModel.Record("decision", "decision/0001-loan-period", PathAt(root, ".litespec", "intent", "decisions", "0001-loan-period.yaml"), """{}"""),
            TestModel.Record("decision", "decision/0002-render-engine", PathAt(root, ".litespec", "implementation", "decisions", "0002-render-engine.yaml"), """{}"""),
            TestModel.Record("research", "research/loan-period-benchmarks", PathAt(root, ".litespec", "intent", "research", "loan-period-benchmarks.yaml"), """{}"""),
            TestModel.Record("journey", "journey/borrow-and-return", PathAt(root, ".litespec", "intent", "journeys", "borrow-and-return.yaml"), """{}"""),
            TestModel.Record("actor", "actor/reader", PathAt(root, ".litespec", "intent", "actors", "reader.yaml"), """{}"""),
            TestModel.Record("role", "role/reader", PathAt(root, ".litespec", "intent", "roles", "reader.yaml"), """{}"""),
            TestModel.Record("module", "module/lending", PathAt(root, ".litespec", "intent", "modules", "lending", "module.yaml"), """{"dependsOn": []}"""),
            TestModel.Record("term", "term/loan", PathAt(root, ".litespec", "glossary.yaml"), """{}"""),
            TestModel.Record("aggregate", "aggregate/lending.loan", aggregate, """{}"""),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", PathAt(root, ".litespec", "behavior", "modules", "lending", "loans", "borrow-copy.yaml"), """{}"""),
            TestModel.Record("use-case", "use-case/lending.list-loans", PathAt(root, ".litespec", "behavior", "modules", "lending", "use-cases", "list-loans.yaml"), """{}"""),
            TestModel.Record("result", "result/borrow-copy.borrowed", PathAt(root, ".litespec", "behavior", "modules", "lending", "loans", "borrow-copy.yaml"), """{}"""),
            TestModel.Record("scenario", "scenario/borrow-copy.borrowing-succeeds", PathAt(root, ".litespec", "behavior", "modules", "lending", "loans", "borrow-copy.yaml"), """{}"""),
            TestModel.Record("event", "event/loan.loan-borrowed", aggregate, """{}"""),
            TestModel.Record("event", "event/loan.loan-returned", PathAt(root, ".litespec", "behavior", "modules", "lending", "loans", "events", "loan-returned.yaml"), """{}"""),
            TestModel.Record("rule", "rule/loan.loan-period", PathAt(root, ".litespec", "behavior", "modules", "lending", "loans", "rules", "loan-period.yaml"), """{}"""),
            TestModel.Record("rule", "rule/lending.max-renewals", PathAt(root, ".litespec", "behavior", "modules", "lending", "rules", "max-renewals.yaml"), """{}"""),
            TestModel.Record("rule", "rule/pricing.rounding", PathAt(root, ".litespec", "behavior", "rules", "pricing-rounding.yaml"), """{}"""),
            TestModel.Record("reaction", "reaction/lending.send-due-notice", PathAt(root, ".litespec", "behavior", "modules", "lending", "reactions", "send-due-notice.yaml"), """{}"""),
            TestModel.Record("job", "job/lending.scan-due", PathAt(root, ".litespec", "behavior", "modules", "lending", "jobs", "scan-due.yaml"), """{}"""),
            TestModel.Record("notification", "notification/lending.loan-due", PathAt(root, ".litespec", "behavior", "modules", "lending", "notifications", "loan-due.yaml"), """{}"""),
            TestModel.Record("setting", "setting/lending.max-renewals", PathAt(root, ".litespec", "behavior", "modules", "lending", "settings", "library.yaml"), """{}"""),
            TestModel.Record("integration-event", "integration-event/loan-borrowed", PathAt(root, ".litespec", "behavior", "modules", "lending", "integration-events", "loan-borrowed.yaml"), """{}"""),
            TestModel.Record("permission", "permission/borrow-copy", PathAt(root, ".litespec", "behavior", "modules", "lending", "permissions.yaml"), """{}"""),
            TestModel.Record("entry-point", "entry-point/library-app", PathAt(root, ".litespec", "behavior", "entry-points", "library-app", "entry-point.yaml"), """{}"""),
            TestModel.Record("screen", "screen/library-app.loans", PathAt(root, ".litespec", "behavior", "entry-points", "library-app", "screens", "loans.yaml"), """{}"""),
            TestModel.Record("external-system", "external-system/text-gateway", PathAt(root, ".litespec", "behavior", "external-systems", "text-gateway.yaml"), """{}"""),
            TestModel.Record("external-system", "external-system/lending-printer", PathAt(root, ".litespec", "behavior", "modules", "lending", "external-systems", "lending-printer.yaml"), """{}"""),
            TestModel.Record("runbook", "runbook/clear-stuck-holds", PathAt(root, ".litespec", "implementation", "runbooks", "clear-stuck-holds.yaml"), """{}"""),
            TestModel.Record("runbook", "runbook/lending-nightly", PathAt(root, ".litespec", "implementation", "modules", "lending", "runbooks", "lending-nightly.yaml"), """{}"""),
            TestModel.Record("exception", "exception/shelf-check-bypass", PathAt(root, ".litespec", "governance", "exceptions", "shelf-check-bypass.yaml"), """{}"""),
            TestModel.Record("exception", "exception/lending-waiver", PathAt(root, ".litespec", "governance", "modules", "lending", "exceptions", "lending-waiver.yaml"), """{}"""));

        Assert.Empty(Run(model));
    }

    /// <summary>Term Outside Glossary Reports.</summary>
    /// <proves>litespec/core/model#write-every-record-in-its-home</proves>
    [Fact]
    public void Term_Outside_Glossary_Reports()
    {
        using var temp = Cli.Temp("checks");
        var file = PathAt(temp.Path, ".litespec", "intent", "goals", "stray-term.yaml");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("term", "term/stray", file, """{}"""));

        var finding = Assert.Single(Run(model), found => found.Code == "record-placement-invalid");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Term 'term/stray' sits in 'intent/goals', but terms live in 'glossary.yaml'.", finding.Message);
        Assert.Equal("term/stray", finding.Subject);
    }

    /// <summary>Term In Glossary Passes.</summary>
    /// <proves>litespec/core/model#write-every-record-in-its-home</proves>
    [Fact]
    public void Term_In_Glossary_Passes()
    {
        using var temp = Cli.Temp("checks");
        var file = PathAt(temp.Path, ".litespec", "glossary.yaml");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("term", "term/loan", file, """{}"""));

        Assert.DoesNotContain(Run(model), found => found.Code == "record-placement-invalid");
    }

    /// <summary>Record Outside Record Roots Skipped.</summary>
    /// <proves>litespec/core/model#write-every-record-in-its-home</proves>
    [Fact]
    public void Record_Outside_Record_Roots_Skipped()
    {
        using var temp = Cli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("feature", "feature/hold-queue", Path.Combine(temp.Path, "holds.yaml"), """{}"""));

        Assert.DoesNotContain(Run(model), found => found.Code == "record-placement-invalid");
    }

    /// <summary>Derived Record Skipped.</summary>
    /// <proves>litespec/core/model#write-every-record-in-its-home</proves>
    [Fact]
    public void Derived_Record_Skipped()
    {
        using var temp = Cli.Temp("checks");
        var file = PathAt(temp.Path, ".litespec", "proof", "register.yaml");
        using var document = JsonDocument.Parse("{}");
        var derived = new LiteSpec.Model.Record
        {
            Type = "result",
            Id = "result/borrow-copy.forbidden",
            Title = "Forbidden",
            File = file,
            Line = 1,
            IsEntry = true,
            Derived = true,
            Raw = document.RootElement.Clone(),
        };
        var model = TestModel.Model(TestModel.Config(temp.Path), derived);

        Assert.DoesNotContain(Run(model), found => found.Code == "record-placement-invalid");
    }

    /// <summary>The findings the folder checks raise.</summary>
    /// <param name="model">The model to run the checks against.</param>
    /// <returns>The findings in walk and model order.</returns>
    static List<Finding> Run(ProjectModel model)
    {
        var findings = new List<Finding>();
        FolderChecks.Run(model, findings);
        return findings;
    }

    /// <summary>A record file path under the root, with its folders created.</summary>
    /// <param name="root">The consumer root folder.</param>
    /// <param name="parts">The path parts under the root.</param>
    /// <returns>The record file path; the file itself is left unwritten.</returns>
    static string PathAt(string root, params string[] parts)
    {
        var path = Path.Combine([root, .. parts]);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        return path;
    }

    /// <summary>A folder under the root, created.</summary>
    /// <param name="root">The consumer root folder.</param>
    /// <param name="parts">The path parts under the root.</param>
    /// <returns>The folder path.</returns>
    static string DirAt(string root, params string[] parts)
    {
        var path = Path.Combine([root, .. parts]);
        Directory.CreateDirectory(path);
        return path;
    }
}
