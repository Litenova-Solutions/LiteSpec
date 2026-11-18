using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.RecordEditing;

/// <summary>Tests for <c>litespec rename</c> on requirements: a requirement moves its folder, a feature moves its requirement folders with it, and a problem moves its features with it.</summary>
public sealed class RenameRequirementTests
{
    const string Problem = ".litespec/intent/problems/waiting-for-money";
    const string Feature = ".litespec/intent/problems/waiting-for-money/features/approval";

    /// <summary>Renaming a requirement moves its own folder and rewrites its identifier.</summary>
    /// <proves>litespec/core/model#rewrite-every-link-on-a-rename</proves>
    [Fact(DisplayName = "rename moves a requirement folder")]
    public void RenameMovesARequirementFolder()
    {
        using var consumer = Consumer();

        var result = TestCli.Run(
            "rename", "requirement/approval.nobody-approves-own-expense", "requirement/approval.self-approval-refused",
            "--root", consumer.Path);

        Assert.Equal(0, result.Status);
        var moved = Path.Combine(consumer.Path, Feature, "requirements", "self-approval-refused", "self-approval-refused.yaml");
        Assert.True(File.Exists(moved), result.Output);
        Assert.False(Directory.Exists(Path.Combine(consumer.Path, Feature, "requirements", "nobody-approves-own-expense")));
        Assert.Contains("id: requirement/approval.self-approval-refused", File.ReadAllText(moved));
    }

    /// <summary>Renaming a feature moves its whole folder, so its requirement folders move with it, and rewrites their identifiers.</summary>
    /// <proves>litespec/core/model#rewrite-every-link-on-a-rename</proves>
    [Fact(DisplayName = "rename moves a feature folder with its requirement folders")]
    public void RenameMovesAFeatureFolderWithItsRequirements()
    {
        using var consumer = Consumer();

        var result = TestCli.Run(
            "rename", "feature/approval", "feature/expense-approval",
            "--root", consumer.Path);

        Assert.Equal(0, result.Status);
        var features = Path.Combine(consumer.Path, ".litespec/intent/problems/waiting-for-money/features");
        var feature = Path.Combine(features, "expense-approval", "expense-approval.yaml");
        Assert.True(File.Exists(feature), result.Output);
        Assert.Contains("id: feature/expense-approval", File.ReadAllText(feature));
        Assert.False(Directory.Exists(Path.Combine(features, "approval")));

        var requirement = Path.Combine(features, "expense-approval", "requirements", "nobody-approves-own-expense", "nobody-approves-own-expense.yaml");
        Assert.True(File.Exists(requirement), result.Output);
        Assert.Contains("id: requirement/expense-approval.nobody-approves-own-expense", File.ReadAllText(requirement));
    }

    /// <summary>Renaming a problem moves its whole folder, so its features move with it, and the feature identifiers stay put.</summary>
    /// <proves>litespec/core/model#rewrite-every-link-on-a-rename</proves>
    [Fact(DisplayName = "rename moves a problem folder with its features")]
    public void RenameMovesAProblemFolderWithItsFeatures()
    {
        using var consumer = Consumer();

        var result = TestCli.Run(
            "rename", "problem/waiting-for-money", "problem/waiting-less",
            "--root", consumer.Path);

        Assert.Equal(0, result.Status);
        var moved = Path.Combine(consumer.Path, ".litespec/intent/problems/waiting-less/features/approval/approval.yaml");
        Assert.True(File.Exists(moved), result.Output);
        Assert.Contains("id: feature/approval", File.ReadAllText(moved));
        Assert.False(Directory.Exists(Path.Combine(consumer.Path, Problem)));

        var requirement = Path.Combine(consumer.Path, ".litespec/intent/problems/waiting-less/features/approval/requirements/nobody-approves-own-expense/nobody-approves-own-expense.yaml");
        Assert.True(File.Exists(requirement), result.Output);
        Assert.Contains("id: requirement/approval.nobody-approves-own-expense", File.ReadAllText(requirement));
    }

    /// <summary>A consumer with one problem folder holding one feature folder with one requirement folder.</summary>
    /// <returns>The temporary consumer folder.</returns>
    static TempFolder Consumer()
    {
        var temp = TestCli.Temp("rename-requirement");
        File.WriteAllText(Path.Combine(temp.Path, ".litespec/project.yaml"), "{}");
        TestRecords.Write(temp.Path, $"{Problem}/waiting-for-money.yaml", """
            {
              "type": "problem",
              "id": "problem/waiting-for-money",
              "title": "Waiting for Money"
            }
            """);
        TestRecords.Write(temp.Path, $"{Feature}/approval.yaml", """
            {
              "type": "feature",
              "id": "feature/approval",
              "title": "Approval"
            }
            """);
        TestRecords.Write(temp.Path, $"{Feature}/requirements/nobody-approves-own-expense/nobody-approves-own-expense.yaml", """
            {
              "type": "requirement",
              "id": "requirement/approval.nobody-approves-own-expense",
              "title": "Nobody Approves Their Own Expense",
              "kind": "functional",
              "statement": "Nobody approves their own expense.",
              "rationale": "Self approval invites fraud."
            }
            """);
        return temp;
    }
}
