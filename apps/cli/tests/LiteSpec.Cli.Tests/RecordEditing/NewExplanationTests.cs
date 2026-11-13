using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.RecordEditing;

/// <summary>Tests for the explanation file <c>litespec new</c> writes, and for the explanation template each type carries.</summary>
public sealed class NewExplanationTests
{
    /// <summary>New problem writes the record and the explanation with the problem sections when intent is listed.</summary>
    /// <proves>litespec/core/model#scaffold-pages-and-reserve-identifiers</proves>
    [Fact(DisplayName = "new problem writes the explanation with the problem sections when intent is listed")]
    public void NewProblemWritesTheExplanationWhenIntentIsListed()
    {
        using var consumer = ConsumerWithLayers("intent");

        var result = TestCli.Run(
            "new", "problem", "expenses-wait-weeks", "--root", consumer.Path);

        Assert.Equal(0, result.Status);

        var page = Path.Combine(
            consumer.Path, ".litespec", "intent", "problems", "expenses-wait-weeks", "expenses-wait-weeks.yaml");

        Assert.True(File.Exists(page), $"Expected the problem page at '{page}'.");
        Assert.Contains("problem/expenses-wait-weeks", File.ReadAllText(page));

        var explanation = Path.ChangeExtension(page, ".md");
        Assert.True(File.Exists(explanation), $"Expected the explanation file beside '{page}'.");
        AssertSections(explanation, ["Affected Users", "Failure Cases", "Example", "Current Workaround", "Cost", "Evidence", "Resolution Criteria"]);
    }

    /// <summary>New goal writes the explanation with the two goal sections when intent is listed.</summary>
    /// <proves>litespec/core/model#scaffold-pages-and-reserve-identifiers</proves>
    [Fact(DisplayName = "new goal writes the explanation with the two goal sections when intent is listed")]
    public void NewGoalWritesTheExplanationWhenIntentIsListed()
    {
        using var consumer = ConsumerWithLayers("intent");

        var result = TestCli.Run(
            "new", "goal", "faster-reimbursement", "--root", consumer.Path);

        Assert.Equal(0, result.Status);

        var page = Path.Combine(
            consumer.Path, ".litespec", "intent", "goals", "faster-reimbursement", "faster-reimbursement.yaml");

        Assert.True(File.Exists(page), $"Expected the goal page at '{page}'.");

        var explanation = Path.ChangeExtension(page, ".md");
        Assert.True(File.Exists(explanation), $"Expected the explanation file beside '{page}'.");
        AssertSections(explanation, ["Target", "Measurement"]);
    }

    /// <summary>New vision writes the record at product level and the explanation with the three vision sections when intent is listed.</summary>
    /// <proves>litespec/core/model#scaffold-pages-and-reserve-identifiers</proves>
    [Fact(DisplayName = "new vision writes the explanation with the three vision sections when intent is listed")]
    public void NewVisionWritesTheExplanationWhenIntentIsListed()
    {
        using var consumer = ConsumerWithLayers("intent");

        var result = TestCli.Run(
            "new", "vision", "contractor-expenses", "--root", consumer.Path);

        Assert.Equal(0, result.Status);

        var page = Path.Combine(
            consumer.Path, ".litespec", "intent", "visions", "contractor-expenses", "contractor-expenses.yaml");

        Assert.True(File.Exists(page), $"Expected the vision page at '{page}'.");
        Assert.Contains("vision/contractor-expenses", File.ReadAllText(page));

        var explanation = Path.ChangeExtension(page, ".md");
        Assert.True(File.Exists(explanation), $"Expected the explanation file beside '{page}'.");
        AssertSections(explanation, ["Direction", "Effect on Current Design", "Trigger"]);
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
            consumer.Path, ".litespec", "intent", "modules", "spending", "features", "approval-reminders", "approval-reminders.yaml");

        Assert.True(File.Exists(page), $"Expected the feature page at '{page}'.");
        Assert.Contains("id: feature/approval-reminders", File.ReadAllText(page));

        var explanation = Path.ChangeExtension(page, ".md");
        Assert.True(File.Exists(explanation), $"Expected the explanation file beside '{page}'.");
        AssertSections(explanation, ["Capabilities", "Success Criteria", "Limits", "Example"]);
    }

    /// <summary>One explanation template per record type with a section table, as the writing standard gives the sections in order.</summary>
    /// <returns>The record type and its fixed sections, in order.</returns>
    public static TheoryData<string, string[]> ExplanationTemplates() => new()
    {
        { "product", ["Scope", "Users", "Out of Scope", "Direction"] },
        { "actor", ["Responsibilities", "Access", "Example"] },
        { "role", ["Access", "Limits"] },
        { "goal", ["Target", "Measurement"] },
        { "vision", ["Direction", "Effect on Current Design", "Trigger"] },
        { "problem", ["Affected Users", "Failure Cases", "Example", "Current Workaround", "Cost", "Evidence", "Resolution Criteria"] },
        { "constraint", ["Source", "Effect on the Product", "Open Points"] },
        { "research", ["Findings", "Source Notes"] },
        { "decision", ["Context", "Options", "Decision", "Consequences", "Evidence", "Revisit Conditions"] },
        { "module", ["Responsibilities", "Boundaries", "Example"] },
        { "feature", ["Capabilities", "Success Criteria", "Limits", "Example"] },
        { "journey", ["Preconditions", "Example", "Variations"] },
        { "aggregate", ["Lifecycle", "Data and Retention", "Example", "Limits"] },
        { "use-case", ["Flow", "Example", "Edge Cases"] },
        { "rule-set", ["Rationale", "Example"] },
        { "reaction", ["Trigger and Effect", "Failure Handling", "Example"] },
        { "job", ["Schedule", "Failure Handling", "Example"] },
        { "notification", ["Content", "Delivery", "Example"] },
        { "integration-event", ["Consumers", "Compatibility", "Example"] },
        { "external-system", ["Use", "Data Shared", "Failure Handling"] },
        { "entry-point", ["Audience", "Authentication", "Limits"] },
        { "screen", ["Task", "States", "Example"] },
        { "permission-list", ["Risk", "Future Roles"] },
        { "setting-set", ["Defaults", "Changes"] },
        { "retention-schedule", ["Legal Basis", "Conflicts", "Deletion"] },
        { "runbook", ["Prerequisites", "Steps", "Verification", "Recovery"] },
        { "departure", ["Provision", "Reason", "Risk", "Expiry"] },
    };

    /// <summary>Each explanation template opens with its paragraph and carries exactly the fixed sections of its record type, in order.</summary>
    /// <param name="type">The record type whose template is read.</param>
    /// <param name="sections">The fixed sections the writing standard gives the type, in order.</param>
    /// <proves>litespec/core/writing#give-each-explanation-file-its-fixed-sections</proves>
    /// <proves>litespec/core/writing#open-each-explanation-file-with-a-paragraph</proves>
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
            consumer.Path, ".litespec", "behavior", "modules", "spending", "reactions", "notify-approver", "notify-approver.yaml");

        Assert.True(File.Exists(page), $"Expected the reaction page at '{page}'.");
        Assert.False(
            File.Exists(Path.ChangeExtension(page, ".md")),
            $"Expected no explanation file beside '{page}'.");
    }

    /// <summary>New reaction writes the reaction explanation with its own sections when behavior is listed.</summary>
    /// <proves>litespec/core/model#scaffold-pages-and-reserve-identifiers</proves>
    [Fact(DisplayName = "new reaction writes the reaction explanation with its own sections when behavior is listed")]
    public void NewReactionWritesItsExplanationWhenBehaviorIsListed()
    {
        using var consumer = ConsumerWithLayers("intent", "behavior");

        var result = TestCli.Run(
            "new", "reaction", "spending.notify-approver", "--root", consumer.Path);

        Assert.Equal(0, result.Status);

        var page = Path.Combine(
            consumer.Path, ".litespec", "behavior", "modules", "spending", "reactions", "notify-approver", "notify-approver.yaml");

        Assert.True(File.Exists(page), $"Expected the reaction page at '{page}'.");

        var explanation = Path.ChangeExtension(page, ".md");
        Assert.True(File.Exists(explanation), $"Expected the explanation file beside '{page}'.");
        AssertSections(explanation, ["Trigger and Effect", "Failure Handling", "Example"]);
        Assert.Contains("# Notify approver", File.ReadAllText(explanation));
    }

    /// <summary>New constraint writes the constraint explanation with its own sections when intent is listed.</summary>
    /// <proves>litespec/core/model#scaffold-pages-and-reserve-identifiers</proves>
    [Fact(DisplayName = "new constraint writes the constraint explanation with its own sections when intent is listed")]
    public void NewConstraintWritesItsExplanationWhenIntentIsListed()
    {
        using var consumer = ConsumerWithLayers("intent");

        var result = TestCli.Run(
            "new", "constraint", "tax-receipt-retention", "--root", consumer.Path);

        Assert.Equal(0, result.Status);

        var page = Path.Combine(
            consumer.Path, ".litespec", "intent", "constraints", "tax-receipt-retention", "tax-receipt-retention.yaml");

        Assert.True(File.Exists(page), $"Expected the constraint page at '{page}'.");

        var explanation = Path.ChangeExtension(page, ".md");
        Assert.True(File.Exists(explanation), $"Expected the explanation file beside '{page}'.");
        AssertSections(explanation, ["Source", "Effect on the Product", "Open Points"]);
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
            consumer.Path, ".litespec", "intent", "problems", "expenses-wait-weeks", "expenses-wait-weeks.yaml");

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
            consumer.Path, ".litespec", "intent", "research", "approval-time-benchmarks", "approval-time-benchmarks.yaml");

        Assert.True(File.Exists(page), $"Expected the research page at '{page}'.");

        var explanation = Path.ChangeExtension(page, ".md");
        Assert.True(File.Exists(explanation), $"Expected the explanation file beside '{page}'.");
        AssertSections(explanation, ["Findings", "Source Notes"]);
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

    /// <summary>
    ///     Proves the explanation file opens with one H1, one paragraph of prose, and then exactly the sections, in order.
    /// </summary>
    /// <param name="file">The explanation file to read.</param>
    /// <param name="sections">The expected H2 section names, in order.</param>
    static void AssertSections(string file, string[] sections)
    {
        var lines = File.ReadAllText(file).Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');

        Assert.StartsWith("# ", lines[0], StringComparison.Ordinal);
        Assert.Equal(
            sections.Select(section => $"## {section}").ToArray(),
            lines.Select(line => line.Trim())
                .Where(line => line.StartsWith("## ", StringComparison.Ordinal))
                .ToArray());

        var firstSection = Array.FindIndex(lines, line => line.StartsWith("## ", StringComparison.Ordinal));
        Assert.True(
            lines.Skip(1).Take(firstSection - 1).Any(line => line.Trim().Length > 0 && !line.StartsWith('#')),
            $"Expected an opening paragraph between the H1 and the first H2 in '{file}'.");
    }
}
