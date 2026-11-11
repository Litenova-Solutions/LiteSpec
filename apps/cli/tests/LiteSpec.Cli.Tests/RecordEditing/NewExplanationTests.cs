using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.RecordEditing;

/// <summary>Tests for the explanation file <c>litespec new</c> writes on a required layer.</summary>
public sealed class NewExplanationTests
{
    /// <summary>New problem writes the record and the explanation with the four problem sections when intent is listed.</summary>
    /// <proves>litespec/core/model#scaffold-pages-and-reserve-identifiers</proves>
    [Fact(DisplayName = "new problem writes the explanation with the four problem sections when intent is listed")]
    public void NewProblemWritesTheExplanationWhenIntentIsListed()
    {
        using var consumer = ConsumerWithLayers("intent");

        var result = TestCli.Run(
            "new", "problem", "expenses-wait-weeks", "--root", consumer.Path);

        Assert.Equal(0, result.Status);

        var page = Path.Combine(
            consumer.Path, ".litespec", "intent", "problems", "expenses-wait-weeks.yaml");

        Assert.True(File.Exists(page), $"Expected the problem page at '{page}'.");
        Assert.Contains("problem/expenses-wait-weeks", File.ReadAllText(page));

        var explanation = Path.ChangeExtension(page, ".md");
        Assert.True(File.Exists(explanation), $"Expected the explanation file beside '{page}'.");
        AssertSections(explanation, ["Who Has It", "What They Do Today", "Evidence", "What Would Retire It"]);
    }

    /// <summary>New goal writes the explanation with the three goal sections when intent is listed.</summary>
    /// <proves>litespec/core/model#scaffold-pages-and-reserve-identifiers</proves>
    [Fact(DisplayName = "new goal writes the explanation with the three goal sections when intent is listed")]
    public void NewGoalWritesTheExplanationWhenIntentIsListed()
    {
        using var consumer = ConsumerWithLayers("intent");

        var result = TestCli.Run(
            "new", "goal", "faster-reimbursement", "--root", consumer.Path);

        Assert.Equal(0, result.Status);

        var page = Path.Combine(
            consumer.Path, ".litespec", "intent", "goals", "faster-reimbursement.yaml");

        Assert.True(File.Exists(page), $"Expected the goal page at '{page}'.");

        var explanation = Path.ChangeExtension(page, ".md");
        Assert.True(File.Exists(explanation), $"Expected the explanation file beside '{page}'.");
        AssertSections(explanation, ["Why It Matters", "What Counts as Done", "What It Connects To"]);
    }

    /// <summary>New feature takes its module for the folder, writes the identifier without a home, and writes the feature sections when intent is listed.</summary>
    /// <proves>litespec/core/model#scaffold-pages-and-reserve-identifiers</proves>
    [Fact(DisplayName = "new feature writes feature/<name> under its module with the feature sections when intent is listed")]
    public void NewFeatureWritesTheExplanationWhenIntentIsListed()
    {
        using var consumer = ConsumerWithLayers("intent");

        var result = TestCli.Run(
            "new", "feature", "spending.approval-reminders", "--root", consumer.Path);

        Assert.Equal(0, result.Status);

        var page = Path.Combine(
            consumer.Path, ".litespec", "intent", "modules", "spending", "features", "approval-reminders.yaml");

        Assert.True(File.Exists(page), $"Expected the feature page at '{page}'.");
        Assert.Contains("id: feature/approval-reminders", File.ReadAllText(page));

        var explanation = Path.ChangeExtension(page, ".md");
        Assert.True(File.Exists(explanation), $"Expected the explanation file beside '{page}'.");
        AssertSections(explanation, ["Who It Serves and Why", "What It Offers", "What Good Looks Like"]);
    }

    public static TheoryData<string, string[]> ExplanationTemplates() => new()
    {
        { "problem", ["Who Has It", "What They Do Today", "Evidence", "What Would Retire It"] },
        { "journey", ["Who Walks and Why", "Before It Starts", "Example", "What It Connects To"] },
        { "feature", ["Who It Serves and Why", "What It Offers", "What Good Looks Like"] },
        { "goal", ["Why It Matters", "What Counts as Done", "What It Connects To"] },
        { "actor", ["Who They Are", "What They Do", "What They Can Reach"] },
    };

    /// <summary>Each explanation template carries exactly the fixed sections of its record type, in order.</summary>
    /// <param name="type">The record type whose template is read.</param>
    /// <param name="sections">The fixed sections the writing standard gives the type, in order.</param>
    /// <proves>litespec/core/model#scaffold-pages-and-reserve-identifiers</proves>
    [Theory(DisplayName = "each explanation template carries exactly the fixed sections of its record type")]
    [MemberData(nameof(ExplanationTemplates))]
    public void ExplanationTemplatesCarryTheirFixedSections(string type, string[] sections) =>
        AssertSections(Path.Join(TestCli.Repository, "templates", "consumer", $"{type}.md"), sections);

    /// <summary>New reaction writes no explanation file when behavior is not listed.</summary>
    /// <proves>litespec/core/model#scaffold-pages-and-reserve-identifiers</proves>
    [Fact(DisplayName = "new reaction writes no explanation file when behavior is not listed")]
    public void NewReactionWritesNoExplanationWhenBehaviorIsNotListed()
    {
        using var consumer = ConsumerWithLayers("intent");

        var result = TestCli.Run(
            "new", "reaction", "spending.notify-approver", "--root", consumer.Path);

        Assert.Equal(0, result.Status);

        var page = Path.Combine(
            consumer.Path, ".litespec", "behavior", "modules", "spending", "reactions", "notify-approver.yaml");

        Assert.True(File.Exists(page), $"Expected the reaction page at '{page}'.");
        Assert.False(
            File.Exists(Path.ChangeExtension(page, ".md")),
            $"Expected no explanation file beside '{page}'.");
    }

    /// <summary>New reaction writes the generic explanation when behavior is listed.</summary>
    /// <proves>litespec/core/model#scaffold-pages-and-reserve-identifiers</proves>
    [Fact(DisplayName = "new reaction writes the generic explanation when behavior is listed")]
    public void NewReactionWritesTheGenericExplanationWhenBehaviorIsListed()
    {
        using var consumer = ConsumerWithLayers("intent", "behavior");

        var result = TestCli.Run(
            "new", "reaction", "spending.notify-approver", "--root", consumer.Path);

        Assert.Equal(0, result.Status);

        var page = Path.Combine(
            consumer.Path, ".litespec", "behavior", "modules", "spending", "reactions", "notify-approver.yaml");

        Assert.True(File.Exists(page), $"Expected the reaction page at '{page}'.");

        var explanation = Path.ChangeExtension(page, ".md");
        Assert.True(File.Exists(explanation), $"Expected the explanation file beside '{page}'.");
        AssertSections(explanation, ["What It Means"]);
    }

    /// <summary>New constraint writes the generic explanation when intent is listed.</summary>
    /// <proves>litespec/core/model#scaffold-pages-and-reserve-identifiers</proves>
    [Fact(DisplayName = "new constraint writes the generic explanation when intent is listed")]
    public void NewConstraintWritesTheGenericExplanationWhenIntentIsListed()
    {
        using var consumer = ConsumerWithLayers("intent");

        var result = TestCli.Run(
            "new", "constraint", "tax-receipt-retention", "--root", consumer.Path);

        Assert.Equal(0, result.Status);

        var page = Path.Combine(
            consumer.Path, ".litespec", "intent", "constraints", "tax-receipt-retention.yaml");

        Assert.True(File.Exists(page), $"Expected the constraint page at '{page}'.");

        var explanation = Path.ChangeExtension(page, ".md");
        Assert.True(File.Exists(explanation), $"Expected the explanation file beside '{page}'.");
        AssertSections(explanation, ["What It Means"]);
    }

    /// <summary>New problem writes no explanation file when the project lists no explanation layer.</summary>
    /// <proves>litespec/core/model#scaffold-pages-and-reserve-identifiers</proves>
    [Fact(DisplayName = "new problem writes no explanation file when the project lists no explanation layer")]
    public void NewProblemWritesNoExplanationWithoutExplanations()
    {
        using var consumer = TestCli.Temp("new-explanation");
        File.WriteAllText(Path.Combine(consumer.Path, ".litespec/project.yaml"), "{}");

        var result = TestCli.Run(
            "new", "problem", "expenses-wait-weeks", "--root", consumer.Path);

        Assert.Equal(0, result.Status);

        var page = Path.Combine(
            consumer.Path, ".litespec", "intent", "problems", "expenses-wait-weeks.yaml");

        Assert.True(File.Exists(page), $"Expected the problem page at '{page}'.");
        Assert.False(
            File.Exists(Path.ChangeExtension(page, ".md")),
            $"Expected no explanation file beside '{page}'.");
    }

    /// <summary>New research still writes the explanation file when the project lists no explanation layer.</summary>
    /// <proves>litespec/core/model#scaffold-pages-and-reserve-identifiers</proves>
    [Fact(DisplayName = "new research still writes the explanation file when the project lists no explanation layer")]
    public void NewResearchStillWritesTheExplanationWithoutExplanations()
    {
        using var consumer = TestCli.Temp("new-explanation");
        File.WriteAllText(Path.Combine(consumer.Path, ".litespec/project.yaml"), "{}");

        var result = TestCli.Run(
            "new", "research", "approval-time-benchmarks", "--root", consumer.Path);

        Assert.Equal(0, result.Status);

        var page = Path.Combine(
            consumer.Path, ".litespec", "intent", "research", "approval-time-benchmarks.yaml");

        Assert.True(File.Exists(page), $"Expected the research page at '{page}'.");

        var explanation = Path.ChangeExtension(page, ".md");
        Assert.True(File.Exists(explanation), $"Expected the explanation file beside '{page}'.");
        AssertSections(explanation, ["Answer", "Notes on Sources"]);
    }

    /// <summary>A fresh consumer whose project file lists the explanation layers.</summary>
    /// <param name="layers">The layers <c>explanations.layers</c> lists.</param>
    /// <returns>The temporary consumer folder holding the project file.</returns>
    static TempFolder ConsumerWithLayers(params string[] layers)
    {
        var consumer = TestCli.Temp("new-explanation");
        var listed = string.Join(",", layers.Select(layer => $"\"{layer}\""));
        File.WriteAllText(
            Path.Combine(consumer.Path, ".litespec/project.yaml"),
            $"{{\"explanations\":{{\"layers\":[{listed}]}}}}");
        return consumer;
    }

    /// <summary>Proves the explanation file opens with one H1 and carries exactly the sections, in order.</summary>
    /// <param name="file">The explanation file to read.</param>
    /// <param name="sections">The expected H2 section names, in order.</param>
    static void AssertSections(string file, string[] sections)
    {
        var text = File.ReadAllText(file);

        Assert.StartsWith("# ", text);
        Assert.Equal(
            sections.Select(section => $"## {section}").ToArray(),
            text.Split('\n')
                .Select(line => line.Trim())
                .Where(line => line.StartsWith("## ", StringComparison.Ordinal))
                .ToArray());
    }
}
