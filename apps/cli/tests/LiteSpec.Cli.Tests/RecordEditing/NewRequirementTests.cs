using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.RecordEditing;

/// <summary>Tests for <c>litespec new requirement</c>: a requirement is a page of its own in its feature's folder.</summary>
public sealed class NewRequirementTests
{
    const string FeatureFile = ".litespec/intent/problems/expenses-wait-weeks/features/approval/approval.yaml";
    const string ExistingFile = ".litespec/intent/problems/expenses-wait-weeks/features/approval/requirements/no-self-approval/no-self-approval.yaml";

    /// <summary>New requirement writes its own folder beside the feature's record and no explanation file.</summary>
    /// <proves>litespec/core/intent#identify-every-requirement</proves>
    /// <proves>litespec/core/intent#keep-intent-in-its-homes</proves>
    [Fact(DisplayName = "new requirement writes its own folder in the feature folder")]
    public void WritesItsOwnFolderInTheFeatureFolder()
    {
        using var consumer = Consumer();

        var result = TestCli.Run("new", "requirement", "approval.decided-within-five-days", "--root", consumer.Path);

        Assert.Equal(0, result.Status);
        var file = Path.Combine(consumer.Path, ".litespec/intent/problems/expenses-wait-weeks/features/approval/requirements/decided-within-five-days/decided-within-five-days.yaml");
        Assert.True(File.Exists(file), result.Output);
        var text = File.ReadAllText(file);
        Assert.Contains("id: requirement/approval.decided-within-five-days", text);
        Assert.Contains("title: Decided within five days", text);
        Assert.False(File.Exists(Path.ChangeExtension(file, ".md")));
    }

    /// <summary>New requirement takes no explanation file even on a layer the project explains.</summary>
    /// <proves>litespec/core/intent#identify-every-requirement</proves>
    [Fact(DisplayName = "new requirement writes no explanation on an explained layer")]
    public void WritesNoExplanationOnAnExplainedLayer()
    {
        using var consumer = Consumer();
        File.WriteAllText(Path.Combine(consumer.Path, ".litespec/project.yaml"), "explanations:\n  layers:\n  - intent\n");

        var result = TestCli.Run("new", "requirement", "approval.late-fee-cap", "--root", consumer.Path);

        Assert.Equal(0, result.Status);
        var folder = Path.Combine(consumer.Path, ".litespec/intent/problems/expenses-wait-weeks/features/approval/requirements/late-fee-cap");
        Assert.True(File.Exists(Path.Combine(folder, "late-fee-cap.yaml")), result.Output);
        Assert.Empty(Directory.GetFiles(folder, "*.md"));
    }

    /// <summary>New requirement fails with a clear message when the feature record does not exist.</summary>
    /// <proves>litespec/core/model#scaffold-pages-and-reserve-identifiers</proves>
    [Fact(DisplayName = "new requirement refuses a feature that does not exist")]
    public void RefusesAnUnknownFeature()
    {
        using var consumer = Consumer();

        var result = TestCli.Run("new", "requirement", "ghost.late-fee-cap", "--root", consumer.Path);

        Assert.Equal(2, result.Status);
        Assert.Contains("No feature 'ghost' holds the requirement", result.Output);
        Assert.False(Directory.Exists(Path.Combine(consumer.Path, ".litespec/intent/problems/expenses-wait-weeks/features/ghost")));
    }

    /// <summary>New requirement refuses an identifier a record already carries.</summary>
    /// <proves>litespec/core/model#scaffold-pages-and-reserve-identifiers</proves>
    [Fact(DisplayName = "new requirement refuses a taken identifier")]
    public void RefusesATakenIdentifier()
    {
        using var consumer = Consumer();
        var before = File.ReadAllText(Path.Combine(consumer.Path, ExistingFile));

        var result = TestCli.Run("new", "requirement", "approval.no-self-approval", "--root", consumer.Path);

        Assert.Equal(2, result.Status);
        Assert.Contains("already taken", result.Output);
        Assert.Equal(before, File.ReadAllText(Path.Combine(consumer.Path, ExistingFile)));
    }

    /// <summary>New requirement needs a feature name before its own name.</summary>
    /// <proves>litespec/core/model#scaffold-pages-and-reserve-identifiers</proves>
    [Fact(DisplayName = "new requirement needs a feature before its name")]
    public void NeedsAFeatureName()
    {
        using var consumer = Consumer();

        var result = TestCli.Run("new", "requirement", "late-fee-cap", "--root", consumer.Path);

        Assert.NotEqual(0, result.Status);
        Assert.Contains("new requirement <feature>.<name>", result.Output);
    }

    /// <summary>New entry names the command that writes a requirement, instead of appending it.</summary>
    /// <proves>litespec/core/model#scaffold-pages-and-reserve-identifiers</proves>
    [Fact(DisplayName = "new entry points a requirement to new requirement")]
    public void EntryPointsToNewRequirement()
    {
        using var consumer = Consumer();

        var result = TestCli.Run(
            "new", "entry", FeatureFile, "requirement", "late-fee-cap", "--root", consumer.Path);

        Assert.Equal(2, result.Status);
        Assert.Contains("use litespec new requirement <feature>.late-fee-cap", result.Output);
        Assert.False(Directory.Exists(Path.Combine(consumer.Path, ".litespec/intent/problems/expenses-wait-weeks/features/approval/requirements/late-fee-cap")));
    }

    /// <summary>A consumer with one feature folder holding one requirement folder.</summary>
    /// <returns>The temporary consumer folder.</returns>
    static TempFolder Consumer()
    {
        var temp = TestCli.Temp("new-requirement");
        File.WriteAllText(Path.Combine(temp.Path, ".litespec/project.yaml"), "{}");
        TestRecords.Write(temp.Path, FeatureFile, """
            {
              "type": "feature",
              "id": "feature/approval",
              "title": "Approval"
            }
            """);
        TestRecords.Write(temp.Path, ExistingFile, """
            {
              "type": "requirement",
              "id": "requirement/approval.no-self-approval",
              "title": "No self approval",
              "kind": "functional",
              "statement": "Nobody approves their own expense.",
              "rationale": "Self approval invites fraud."
            }
            """);
        return temp;
    }
}
