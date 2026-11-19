using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Checks.RecordShape;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>
///     Proves the record folder layout: a named record sits in its own folder beside its explanation,
///     the folder carries the file stem, a use case folder inside an aggregate is not an aggregate, and
///     a flat named record is misplaced. The commands <c>new</c> and <c>rename</c> write and move that layout.
/// </summary>
public sealed class RecordFolderLayoutTests
{
    /// <summary>A named goal in its own folder passes the placement and folder checks.</summary>
    /// <proves>litespec/core/model#write-every-record-in-its-home</proves>
    [Fact(DisplayName = "a named goal in its own folder passes the placement and folder checks")]
    public void NamedGoalInItsOwnFolderPasses()
    {
        using var temp = TestCli.Temp("layout");
        var file = PathAt(temp.Path, ".litespec", "intent", "goals", "return-in-minutes", "return-in-minutes.yaml");
        File.WriteAllText(file, "type: goal\nid: goal/return-in-minutes\ntitle: Return in minutes\n");
        var model = TestModel.Model(TestModel.Config(temp.Path), TestModel.Record("goal", "goal/return-in-minutes", file, "{}"));

        var findings = Run(model);

        Assert.DoesNotContain(findings, found => found.Code is "record-placement-invalid" or "record-placement-invalid" or "record-placement-invalid");
    }

    /// <summary>A named goal written flat in its type's folder is misplaced: the folder layout is the only layout.</summary>
    /// <proves>litespec/core/model#write-every-record-in-its-home</proves>
    [Fact(DisplayName = "a named goal written flat in its type's folder is misplaced")]
    public void FlatNamedGoalIsMisplaced()
    {
        using var temp = TestCli.Temp("layout");
        var file = PathAt(temp.Path, ".litespec", "intent", "goals", "return-in-minutes.yaml");
        File.WriteAllText(file, "type: goal\nid: goal/return-in-minutes\ntitle: Return in minutes\n");
        var model = TestModel.Model(TestModel.Config(temp.Path), TestModel.Record("goal", "goal/return-in-minutes", file, "{}"));

        var finding = Assert.Single(Run(model), found => found.Code == "record-placement-invalid");
        Assert.Equal("G", finding.Tier);
        Assert.Contains("intent/goals/<name>/<name>.yaml", finding.Message);
    }

    /// <summary>A record folder whose name differs from its file stem is a record-placement-invalid finding.</summary>
    /// <proves>litespec/core/model#write-every-record-in-its-home</proves>
    [Fact(DisplayName = "a record folder whose name differs from its file stem is a mismatch finding")]
    public void FolderNameMismatchReports()
    {
        using var temp = TestCli.Temp("layout");
        var file = PathAt(temp.Path, ".litespec", "intent", "goals", "return-in-minutes", "minutes.yaml");
        File.WriteAllText(file, "type: goal\nid: goal/minutes\ntitle: Minutes\n");
        var model = TestModel.Model(TestModel.Config(temp.Path), TestModel.Record("goal", "goal/minutes", file, "{}"));

        var finding = Assert.Single(Run(model), found => found.Code == "record-placement-invalid");
        Assert.Equal("G", finding.Tier);
        Assert.Equal(
            "Goal 'goal/minutes' sits in folder 'return-in-minutes', but its file is 'minutes.yaml'; the folder must carry the file's name.",
            finding.Message);
        Assert.Equal(file, finding.Path);
    }

    /// <summary>A use case folder inside an aggregate folder passes, and the aggregate beside it is still an aggregate.</summary>
    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact(DisplayName = "a use case folder inside an aggregate folder passes and is not taken for an aggregate")]
    public void UseCaseFolderInsideAggregatePasses()
    {
        using var temp = TestCli.Temp("layout");
        var aggregate = PathAt(temp.Path, ".litespec", "behavior", "modules", "books", "aggregates", "entries", "aggregate.yaml");
        File.WriteAllText(aggregate, "type: aggregate\nid: aggregate/books.entry\ntitle: Entry\n");
        var useCase = PathAt(temp.Path, ".litespec", "behavior", "modules", "books", "aggregates", "entries", "use-cases", "post-entry", "post-entry.yaml");
        File.WriteAllText(useCase, "type: use-case\nid: use-case/books.post-entry\ntitle: Post entry\n");
        var moduleFile = PathAt(temp.Path, ".litespec", "behavior", "modules", "books", "module.yaml");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/books", moduleFile, """{"dependsOn": []}"""),
            TestModel.Record("aggregate", "aggregate/books.entry", aggregate, "{}"),
            TestModel.Record("use-case", "use-case/books.post-entry", useCase, "{}"));

        var findings = Run(model);

        Assert.DoesNotContain(findings, found => found.Code is "record-placement-invalid" or "record-placement-invalid" or "record-placement-invalid");
    }

    /// <summary>A use case folder directly under a module, outside any aggregate folder, is an invalid folder.</summary>
    /// <proves>litespec/core/model#give-every-record-one-parent</proves>
    [Fact(DisplayName = "a use case folder directly under a module, outside an aggregate, is an invalid folder")]
    public void UseCaseFolderUnderModuleReports()
    {
        using var temp = TestCli.Temp("layout");
        var useCase = PathAt(temp.Path, ".litespec", "behavior", "modules", "books", "post-entry", "post-entry.yaml");
        File.WriteAllText(useCase, "type: use-case\nid: use-case/books.post-entry\ntitle: Post entry\n");
        var moduleFile = PathAt(temp.Path, ".litespec", "behavior", "modules", "books", "module.yaml");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/books", moduleFile, """{"dependsOn": []}"""));

        var finding = Assert.Single(Run(model), found => found.Code == "record-placement-invalid");
        Assert.Equal("Folder '.litespec/behavior/modules/books/post-entry' is no typed folder of its module; aggregates live in 'behavior/modules/<m>/aggregates/<aggregate>/', and the other module children in their named folders.", finding.Message);
    }

    /// <summary>A module event under its aggregate folder passes the placement and folder checks.</summary>
    /// <proves>litespec/core/model#write-every-record-in-its-home</proves>
    [Fact(DisplayName = "a module event under its aggregate folder passes the placement and folder checks")]
    public void ModuleEventUnderItsAggregatePasses()
    {
        using var temp = TestCli.Temp("layout");
        var moduleFile = PathAt(temp.Path, ".litespec", "behavior", "modules", "books", "module.yaml");
        var aggregate = PathAt(temp.Path, ".litespec", "behavior", "modules", "books", "aggregates", "entries", "aggregate.yaml");
        File.WriteAllText(aggregate, "type: aggregate\nid: aggregate/books.entry\ntitle: Entry\n");
        var file = PathAt(temp.Path, ".litespec", "behavior", "modules", "books", "aggregates", "entries", "module-events", "entry-posted", "entry-posted.yaml");
        File.WriteAllText(file, "type: module-event\nid: module-event/books.entry-posted\ntitle: Entry posted\n");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/books", moduleFile, """{"dependsOn": []}"""),
            TestModel.Record("aggregate", "aggregate/books.entry", aggregate, "{}"),
            TestModel.Record("module-event", "module-event/books.entry-posted", file, "{}"));

        var findings = Run(model);

        Assert.DoesNotContain(findings, found => found.Code is "record-placement-invalid" or "record-placement-invalid" or "record-placement-invalid");
    }

    /// <summary>A module event at module level is misplaced: each fact sits under the aggregate whose domain event it translates.</summary>
    /// <proves>litespec/core/model#write-every-record-in-its-home</proves>
    [Fact(DisplayName = "a module event at module level is misplaced")]
    public void ModuleEventAtModuleLevelIsMisplaced()
    {
        using var temp = TestCli.Temp("checks");
        var file = PathAt(temp.Path, ".litespec", "behavior", "modules", "books", "module-events", "entry-posted", "entry-posted.yaml");
        File.WriteAllText(file, "type: module-event\nid: module-event/books.entry-posted\ntitle: Entry posted\n");
        var moduleFile = PathAt(temp.Path, ".litespec", "behavior", "modules", "books", "module.yaml");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/books", moduleFile, """{"dependsOn": []}"""),
            TestModel.Record("module-event", "module-event/books.entry-posted", file, "{}"));

        var findings = Run(model).Where(found => found.Code == "record-placement-invalid").ToList();

        Assert.NotEmpty(findings);
        Assert.Contains(findings, found => found.Subject == "module-event/books.entry-posted");
    }

    /// <summary>An integration event under its aggregate folder passes the placement and folder checks.</summary>
    /// <proves>litespec/core/model#write-every-record-in-its-home</proves>
    [Fact(DisplayName = "an integration event under its aggregate folder passes the placement and folder checks")]
    public void IntegrationEventUnderItsAggregatePasses()
    {
        using var temp = TestCli.Temp("layout");
        var moduleFile = PathAt(temp.Path, ".litespec", "behavior", "modules", "books", "module.yaml");
        var aggregate = PathAt(temp.Path, ".litespec", "behavior", "modules", "books", "aggregates", "entries", "aggregate.yaml");
        File.WriteAllText(aggregate, "type: aggregate\nid: aggregate/books.entry\ntitle: Entry\n");
        var file = PathAt(temp.Path, ".litespec", "behavior", "modules", "books", "aggregates", "entries", "integration-events", "entry-posted", "entry-posted.yaml");
        File.WriteAllText(file, "type: integration-event\nid: integration-event/entry-posted\ntitle: Entry posted\n");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/books", moduleFile, """{"dependsOn": []}"""),
            TestModel.Record("aggregate", "aggregate/books.entry", aggregate, "{}"),
            TestModel.Record("integration-event", "integration-event/entry-posted", file, "{}"));

        var findings = Run(model);

        Assert.DoesNotContain(findings, found => found.Code is "record-placement-invalid" or "record-placement-invalid" or "record-placement-invalid");
    }

    /// <summary>An integration event at module level is misplaced.</summary>
    /// <proves>litespec/core/model#write-every-record-in-its-home</proves>
    [Fact(DisplayName = "an integration event at module level is misplaced")]
    public void IntegrationEventAtModuleLevelIsMisplaced()
    {
        using var temp = TestCli.Temp("checks");
        var file = PathAt(temp.Path, ".litespec", "behavior", "modules", "books", "integration-events", "entry-posted", "entry-posted.yaml");
        File.WriteAllText(file, "type: integration-event\nid: integration-event/entry-posted\ntitle: Entry posted\n");
        var moduleFile = PathAt(temp.Path, ".litespec", "behavior", "modules", "books", "module.yaml");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/books", moduleFile, """{"dependsOn": []}"""),
            TestModel.Record("integration-event", "integration-event/entry-posted", file, "{}"));

        var findings = Run(model).Where(found => found.Code == "record-placement-invalid").ToList();

        Assert.NotEmpty(findings);
        Assert.Contains(findings, found => found.Subject == "integration-event/entry-posted");
    }

    /// <summary>A module event under another aggregate than its translated domain event is misplaced.</summary>
    /// <proves>litespec/core/model#write-every-record-in-its-home</proves>
    [Fact(DisplayName = "a module event under another aggregate than its translated domain event is misplaced")]
    public void ModuleEventTranslatingAnotherAggregatesEventReports()
    {
        using var temp = TestCli.Temp("layout");
        var moduleFile = PathAt(temp.Path, ".litespec", "behavior", "modules", "books", "module.yaml");
        var entries = PathAt(temp.Path, ".litespec", "behavior", "modules", "books", "aggregates", "entries", "aggregate.yaml");
        File.WriteAllText(entries, "type: aggregate\nid: aggregate/books.entry\ntitle: Entry\n");
        var fines = PathAt(temp.Path, ".litespec", "behavior", "modules", "books", "aggregates", "fines", "aggregate.yaml");
        File.WriteAllText(fines, "type: aggregate\nid: aggregate/books.fine\ntitle: Fine\n");
        var file = PathAt(temp.Path, ".litespec", "behavior", "modules", "books", "aggregates", "entries", "module-events", "fine-paid", "fine-paid.yaml");
        File.WriteAllText(file, "type: module-event\nid: module-event/books.fine-paid\ntitle: Fine paid\n");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/books", moduleFile, """{"dependsOn": []}"""),
            TestModel.Record("aggregate", "aggregate/books.entry", entries, "{}"),
            TestModel.Record("aggregate", "aggregate/books.fine", fines, "{}"),
            TestModel.Record("domain-event", "domain-event/fine.paid", fines, """{"meaning": "A fine was paid."}"""),
            TestModel.Record("module-event", "module-event/books.fine-paid", file, """{"translates": "domain-event/fine.paid"}"""));

        var finding = Assert.Single(Run(model), found => found.Code == "record-placement-invalid");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Module event 'module-event/books.fine-paid' sits in aggregate folder 'entries', but the domain event it translates lives in 'fines'.", finding.Message);
        Assert.Equal(file, finding.Path);
    }

    /// <summary>An integration event under another aggregate than its translated domain event is misplaced.</summary>
    /// <proves>litespec/core/model#write-every-record-in-its-home</proves>
    [Fact(DisplayName = "an integration event under another aggregate than its translated domain event is misplaced")]
    public void IntegrationEventTranslatingAnotherAggregatesEventReports()
    {
        using var temp = TestCli.Temp("layout");
        var moduleFile = PathAt(temp.Path, ".litespec", "behavior", "modules", "books", "module.yaml");
        var entries = PathAt(temp.Path, ".litespec", "behavior", "modules", "books", "aggregates", "entries", "aggregate.yaml");
        File.WriteAllText(entries, "type: aggregate\nid: aggregate/books.entry\ntitle: Entry\n");
        var fines = PathAt(temp.Path, ".litespec", "behavior", "modules", "books", "aggregates", "fines", "aggregate.yaml");
        File.WriteAllText(fines, "type: aggregate\nid: aggregate/books.fine\ntitle: Fine\n");
        var file = PathAt(temp.Path, ".litespec", "behavior", "modules", "books", "aggregates", "entries", "integration-events", "fine-paid", "fine-paid.yaml");
        File.WriteAllText(file, "type: integration-event\nid: integration-event/fine-paid\ntitle: Fine paid\n");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/books", moduleFile, """{"dependsOn": []}"""),
            TestModel.Record("aggregate", "aggregate/books.entry", entries, "{}"),
            TestModel.Record("aggregate", "aggregate/books.fine", fines, "{}"),
            TestModel.Record("domain-event", "domain-event/fine.paid", fines, """{"meaning": "A fine was paid."}"""),
            TestModel.Record("integration-event", "integration-event/fine-paid", file, """{"translates": "domain-event/fine.paid"}"""));

        var finding = Assert.Single(Run(model), found => found.Code == "record-placement-invalid");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Integration event 'integration-event/fine-paid' sits in aggregate folder 'entries', but the domain event it translates lives in 'fines'.", finding.Message);
        Assert.Equal(file, finding.Path);
    }

    /// <summary>An integration event in another module than its translated domain event is misplaced.</summary>
    /// <proves>litespec/core/model#write-every-record-in-its-home</proves>
    [Fact(DisplayName = "an integration event in another module than its translated domain event is misplaced")]
    public void IntegrationEventTranslatingAnotherModulesEventReports()
    {
        using var temp = TestCli.Temp("layout");
        var lendingModule = PathAt(temp.Path, ".litespec", "behavior", "modules", "lending", "module.yaml");
        var financeModule = PathAt(temp.Path, ".litespec", "behavior", "modules", "finance", "module.yaml");
        var lendingLoans = PathAt(temp.Path, ".litespec", "behavior", "modules", "lending", "aggregates", "loans", "aggregate.yaml");
        File.WriteAllText(lendingLoans, "type: aggregate\nid: aggregate/lending.loan\ntitle: Loan\n");
        var financeLoans = PathAt(temp.Path, ".litespec", "behavior", "modules", "finance", "aggregates", "loans", "aggregate.yaml");
        File.WriteAllText(financeLoans, "type: aggregate\nid: aggregate/finance.loan\ntitle: Loan\n");
        var file = PathAt(temp.Path, ".litespec", "behavior", "modules", "lending", "aggregates", "loans", "integration-events", "fine-paid", "fine-paid.yaml");
        File.WriteAllText(file, "type: integration-event\nid: integration-event/fine-paid\ntitle: Fine paid\n");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/lending", lendingModule, """{"dependsOn": []}"""),
            TestModel.Record("module", "module/finance", financeModule, """{"dependsOn": []}"""),
            TestModel.Record("aggregate", "aggregate/lending.loan", lendingLoans, "{}"),
            TestModel.Record("aggregate", "aggregate/finance.loan", financeLoans, "{}"),
            TestModel.Record("domain-event", "domain-event/loan.fine-paid", financeLoans, """{"meaning": "A fine was paid."}"""),
            TestModel.Record("integration-event", "integration-event/fine-paid", file, """{"translates": "domain-event/loan.fine-paid"}"""));

        var finding = Assert.Single(Run(model), found => found.Code == "record-placement-invalid");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("Integration event 'integration-event/fine-paid' sits in module 'lending', but the domain event it translates lives in module 'finance'.", finding.Message);
        Assert.Equal(file, finding.Path);
    }

    /// <summary>A module event under the aggregate of its translated domain event passes the placement checks.</summary>
    /// <proves>litespec/core/model#write-every-record-in-its-home</proves>
    [Fact(DisplayName = "a module event under the aggregate of its translated domain event passes the placement checks")]
    public void ModuleEventMatchingTranslatesAggregatePasses()
    {
        using var temp = TestCli.Temp("layout");
        var moduleFile = PathAt(temp.Path, ".litespec", "behavior", "modules", "books", "module.yaml");
        var entries = PathAt(temp.Path, ".litespec", "behavior", "modules", "books", "aggregates", "entries", "aggregate.yaml");
        File.WriteAllText(entries, "type: aggregate\nid: aggregate/books.entry\ntitle: Entry\n");
        var file = PathAt(temp.Path, ".litespec", "behavior", "modules", "books", "aggregates", "entries", "module-events", "entry-posted", "entry-posted.yaml");
        File.WriteAllText(file, "type: module-event\nid: module-event/books.entry-posted\ntitle: Entry posted\n");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/books", moduleFile, """{"dependsOn": []}"""),
            TestModel.Record("aggregate", "aggregate/books.entry", entries, "{}"),
            TestModel.Record("domain-event", "domain-event/entry.posted", entries, """{"meaning": "An entry was posted."}"""),
            TestModel.Record("module-event", "module-event/books.entry-posted", file, """{"translates": "domain-event/entry.posted"}"""));

        Assert.DoesNotContain(Run(model), found => found.Code == "record-placement-invalid");
    }

    /// <summary>A module event whose translates names no record stays silent here: the link check owns the dangling target.</summary>
    /// <proves>litespec/core/model#write-every-record-in-its-home</proves>
    [Fact(DisplayName = "a module event whose translates names no record stays silent in the folder checks")]
    public void ModuleEventWithUnresolvableTranslatesStaysSilent()
    {
        using var temp = TestCli.Temp("layout");
        var moduleFile = PathAt(temp.Path, ".litespec", "behavior", "modules", "books", "module.yaml");
        var entries = PathAt(temp.Path, ".litespec", "behavior", "modules", "books", "aggregates", "entries", "aggregate.yaml");
        File.WriteAllText(entries, "type: aggregate\nid: aggregate/books.entry\ntitle: Entry\n");
        var file = PathAt(temp.Path, ".litespec", "behavior", "modules", "books", "aggregates", "entries", "module-events", "entry-posted", "entry-posted.yaml");
        File.WriteAllText(file, "type: module-event\nid: module-event/books.entry-posted\ntitle: Entry posted\n");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/books", moduleFile, """{"dependsOn": []}"""),
            TestModel.Record("aggregate", "aggregate/books.entry", entries, "{}"),
            TestModel.Record("module-event", "module-event/books.entry-posted", file, """{"translates": "domain-event/ghost.missing"}"""));

        Assert.DoesNotContain(Run(model), found => found.Code == "record-placement-invalid");
    }

    /// <summary>A screen folder named for its home and name holds the screen beside it.</summary>
    /// <proves>litespec/core/model#write-every-record-in-its-home</proves>
    [Fact(DisplayName = "a screen folder named for its home and name holds the screen beside it")]
    public void ScreenFolderNamedForItsFileStemPasses()
    {
        using var temp = TestCli.Temp("layout");
        var file = PathAt(temp.Path, ".litespec", "behavior", "entry-points", "app", "screens", "app.loans", "app.loans.yaml");
        File.WriteAllText(file, "type: screen\nid: screen/app.loans\ntitle: Loans\n");
        var model = TestModel.Model(TestModel.Config(temp.Path), TestModel.Record("screen", "screen/app.loans", file, "{}"));

        var findings = Run(model);

        Assert.DoesNotContain(findings, found => found.Code is "record-placement-invalid" or "record-placement-invalid" or "record-placement-invalid");
    }

    /// <summary>The new command writes a named goal into its own folder, with its explanation beside it.</summary>
    /// <proves>litespec/core/model#scaffold-pages-and-reserve-identifiers</proves>
    [Fact(DisplayName = "new goal writes the record into its own folder")]
    public void NewGoalWritesItsOwnFolder()
    {
        using var consumer = TestCli.Temp("record-layout");
        File.WriteAllText(Path.Combine(consumer.Path, ".litespec", "project.yaml"), "{}");

        var result = TestCli.Run("new", "goal", "return-in-minutes", "--root", consumer.Path);

        Assert.Equal(0, result.Status);
        Assert.True(
            File.Exists(Path.Combine(consumer.Path, ".litespec", "intent", "goals", "return-in-minutes", "return-in-minutes.yaml")),
            "Expected the goal in its own folder.");
        Assert.False(
            File.Exists(Path.Combine(consumer.Path, ".litespec", "intent", "goals", "return-in-minutes.yaml")),
            "Expected no flat goal file.");
    }

    /// <summary>The rename command moves a named record and its folder to the folder its new identifier owns.</summary>
    /// <proves>litespec/core/model#rewrite-every-link-on-a-rename</proves>
    [Fact(DisplayName = "rename moves a named record and its folder to the new name")]
    public void RenameMovesTheRecordFolder()
    {
        using var consumer = TestCli.Temp("record-layout");
        File.WriteAllText(Path.Combine(consumer.Path, ".litespec", "project.yaml"), "{}");
        var folder = Path.Combine(consumer.Path, ".litespec", "intent", "goals", "return-in-minutes");
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "return-in-minutes.yaml"), "type: goal\nid: goal/return-in-minutes\ntitle: Return in minutes\n");
        File.WriteAllText(Path.Combine(folder, "return-in-minutes.md"), "# Return in Minutes\n");

        var result = TestCli.Run("rename", "goal/return-in-minutes", "goal/return-faster", "--root", consumer.Path);

        Assert.Equal(0, result.Status);
        var moved = Path.Combine(consumer.Path, ".litespec", "intent", "goals", "return-faster");
        Assert.True(File.Exists(Path.Combine(moved, "return-faster.yaml")), "Expected the record file under the new folder.");
        Assert.True(File.Exists(Path.Combine(moved, "return-faster.md")), "Expected the explanation beside the record file.");
        Assert.False(Directory.Exists(folder), "Expected the old folder removed.");
    }

    /// <summary>Runs the folder checks over the model.</summary>
    /// <param name="model">The loaded project model.</param>
    /// <returns>The findings the folder checks raise.</returns>
    static List<Finding> Run(ProjectModel model)
    {
        var findings = new List<Finding>();
        FolderChecks.Run(model, findings);
        return findings;
    }

    /// <summary>A record file path under the root, with its folders created.</summary>
    /// <param name="root">The consumer root folder.</param>
    /// <param name="parts">The path segments below the root.</param>
    /// <returns>The file path.</returns>
    static string PathAt(string root, params string[] parts)
    {
        var path = Path.Combine([root, .. parts]);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        return path;
    }
}
