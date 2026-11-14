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

        Assert.DoesNotContain(findings, found => found.Code is "record-placement-invalid" or "record-folder-drift" or "folder-invalid");
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

    /// <summary>A record folder whose name differs from its file stem is a record-folder-drift finding.</summary>
    /// <proves>litespec/core/model#write-every-record-in-its-home</proves>
    [Fact(DisplayName = "a record folder whose name differs from its file stem is a mismatch finding")]
    public void FolderNameMismatchReports()
    {
        using var temp = TestCli.Temp("layout");
        var file = PathAt(temp.Path, ".litespec", "intent", "goals", "return-in-minutes", "minutes.yaml");
        File.WriteAllText(file, "type: goal\nid: goal/minutes\ntitle: Minutes\n");
        var model = TestModel.Model(TestModel.Config(temp.Path), TestModel.Record("goal", "goal/minutes", file, "{}"));

        var finding = Assert.Single(Run(model), found => found.Code == "record-folder-drift");
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
        var aggregate = PathAt(temp.Path, ".litespec", "behavior", "modules", "books", "aggregates", "entry", "aggregate.yaml");
        File.WriteAllText(aggregate, "type: aggregate\nid: aggregate/books.entry\ntitle: Entry\n");
        var useCase = PathAt(temp.Path, ".litespec", "behavior", "modules", "books", "aggregates", "entry", "use-cases", "post-entry", "post-entry.yaml");
        File.WriteAllText(useCase, "type: use-case\nid: use-case/books.post-entry\ntitle: Post entry\n");
        var moduleFile = PathAt(temp.Path, ".litespec", "behavior", "modules", "books", "module.yaml");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("module", "module/books", moduleFile, """{"dependsOn": []}"""),
            TestModel.Record("aggregate", "aggregate/books.entry", aggregate, "{}"),
            TestModel.Record("use-case", "use-case/books.post-entry", useCase, "{}"));

        var findings = Run(model);

        Assert.DoesNotContain(findings, found => found.Code is "folder-invalid" or "record-placement-invalid" or "record-folder-drift");
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

        Assert.DoesNotContain(findings, found => found.Code is "record-placement-invalid" or "record-folder-drift" or "folder-invalid");
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
