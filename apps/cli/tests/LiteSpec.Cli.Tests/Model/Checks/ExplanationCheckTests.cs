using LiteSpec.Cli.Model;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves the explanation checks against hand-built models and files.</summary>
public sealed class ExplanationCheckTests
{
    /// <summary>An explanation with no record file beside it reports.</summary>
    /// <proves>litespec/core/model#explain-each-record-beside-its-record-file</proves>
    [Fact]
    public void OrphanedExplanationReports()
    {
        using var temp = TestCli.Temp("checks");
        var file = Path.Combine(temp.Path, ".litespec", "stray.md");
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, "# Stray\n\nNo record stands beside this file.\n");
        var model = TestModel.Model(TestModel.Config(temp.Path));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "explanation-orphaned");
        Assert.Equal("A", finding.Tier);
        Assert.Equal("Explanation file '.litespec/stray.md' stands beside no record file.", finding.Message);
        Assert.Equal(file, finding.Path);
        Assert.Equal(1, finding.Line);
        Assert.Null(finding.Subject);
    }

    /// <summary>A README without frontmatter is a document, not an orphaned explanation.</summary>
    /// <proves>litespec/core/model#explain-each-record-beside-its-record-file</proves>
    [Fact]
    public void ReadmeWithoutFrontmatterPasses()
    {
        using var temp = TestCli.Temp("checks");
        var folder = Path.Combine(temp.Path, ".litespec");
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "README.md"), "# Lending\n\nThe lending module.\n");
        var model = TestModel.Model(TestModel.Config(temp.Path));

        Assert.DoesNotContain("explanation-orphaned", TestModel.Codes(model));
    }

    /// <summary>A Markdown file opening with a frontmatter fence is a document.</summary>
    /// <proves>litespec/core/model#explain-each-record-beside-its-record-file</proves>
    [Fact]
    public void DocumentWithFrontmatterPasses()
    {
        using var temp = TestCli.Temp("checks");
        var folder = Path.Combine(temp.Path, ".litespec");
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "guide.md"), "---\ntype: reference\nid: guide/lending\n---\n\n# Guide\n");
        var model = TestModel.Model(TestModel.Config(temp.Path));

        Assert.DoesNotContain("explanation-orphaned", TestModel.Codes(model));
    }

    /// <summary>An explanation with its record file beside it passes.</summary>
    /// <proves>litespec/core/model#explain-each-record-beside-its-record-file</proves>
    [Fact]
    public void ExplanationBesideItsRecordPasses()
    {
        using var temp = TestCli.Temp("checks");
        var folder = Path.Combine(temp.Path, ".litespec");
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "loan.yaml"), "type: aggregate\n");
        File.WriteAllText(Path.Combine(folder, "loan.md"), "# Loan\n\nA loan lends one copy to one reader.\n");
        var model = TestModel.Model(TestModel.Config(temp.Path));

        Assert.DoesNotContain("explanation-orphaned", TestModel.Codes(model));
    }

    /// <summary>A decision with no explanation file reports the missing file.</summary>
    /// <proves>litespec/core/model#require-an-explanation-file-for-article-types</proves>
    [Fact]
    public void DecisionWithoutAnExplanationReports()
    {
        using var temp = TestCli.Temp("checks");
        var file = Path.Combine(temp.Path, ".litespec", "decisions", "0001-loan-period.yaml");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("decision", "decision/0001-loan-period", file, """{"options": [{"name": "Fourteen days", "pros": "Copies turn over fast.", "cons": "Readers rush."}], "reopenedWhen": "Demand shifts."}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "explanation-missing");
        Assert.Equal("A", finding.Tier);
        Assert.Equal("Decision 'decision/0001-loan-period' holds no explanation file '.litespec/decisions/0001-loan-period.md'.", finding.Message);
        Assert.Equal(file, finding.Path);
        Assert.Equal(1, finding.Line);
        Assert.Equal("decision/0001-loan-period", finding.Subject);
    }

    /// <summary>A decision explanation without Context reports the missing section.</summary>
    /// <proves>litespec/core/model#require-an-explanation-file-for-article-types</proves>
    [Fact]
    public void DecisionExplanationWithoutContextReports()
    {
        using var temp = TestCli.Temp("checks");
        var file = Path.Combine(temp.Path, "0001-loan-period.yaml");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "decision",
                "decision/0001-loan-period",
                file,
                1,
                """{"options": [{"name": "Fourteen days", "pros": "Copies turn over fast.", "cons": "Readers rush."}], "reopenedWhen": "Demand shifts."}""",
                Path.Combine(temp.Path, "0001-loan-period.md"),
                "# Loan period\n\nThe loan period decides how long a copy stays out.\n\n## Options\n\nFourteen days or twenty-one.\n\n## Decision\n\nFourteen days.\n\n## Consequences\n\nCopies turn over faster.\n\n## Revisit Conditions\n\nDemand shifts.\n"));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "explanation-missing");
        Assert.Equal("A", finding.Tier);
        Assert.Equal("Decision 'decision/0001-loan-period' holds no Context section in its explanation.", finding.Message);
        Assert.Equal(Path.ChangeExtension(file, ".md"), finding.Path);
        Assert.Equal(1, finding.Line);
        Assert.Equal("decision/0001-loan-period", finding.Subject);
    }

    /// <summary>A decision explanation with Context passes.</summary>
    /// <proves>litespec/core/model#require-an-explanation-file-for-article-types</proves>
    [Fact]
    public void DecisionExplanationWithContextPasses()
    {
        using var temp = TestCli.Temp("checks");
        var file = Path.Combine(temp.Path, "0001-loan-period.yaml");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "decision",
                "decision/0001-loan-period",
                file,
                1,
                """{"options": [{"name": "Fourteen days", "pros": "Copies turn over fast.", "cons": "Readers rush."}], "reopenedWhen": "Demand shifts."}""",
                Path.Combine(temp.Path, "0001-loan-period.md"),
                "# Loan period\n\nThe loan period decides how long a copy stays out.\n\n## Context\n\nReaders asked for a longer loan.\n\n## Options\n\nFourteen days or twenty-one.\n\n## Decision\n\nFourteen days.\n\n## Consequences\n\nCopies turn over faster.\n\n## Revisit Conditions\n\nDemand shifts.\n"));

        Assert.DoesNotContain("explanation-missing", TestModel.Codes(model));
    }

    /// <summary>A research explanation without Findings reports the missing section.</summary>
    /// <proves>litespec/core/model#require-an-explanation-file-for-article-types</proves>
    [Fact]
    public void ResearchExplanationWithoutFindingsReports()
    {
        using var temp = TestCli.Temp("checks");
        var file = Path.Combine(temp.Path, "benchmarks.yaml");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "research",
                "research/loan-period-benchmarks",
                file,
                1,
                """{"sources": [{"key": "benchmark-report"}], "question": "How long do others lend?", "conclusion": "Others lend for three weeks."}""",
                Path.Combine(temp.Path, "benchmarks.md"),
                "# Benchmarks\n\nThe benchmarks ask how long others lend.\n\n## Source Notes\n\nOne source covers the answer.\n"));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "explanation-missing");
        Assert.Equal("A", finding.Tier);
        Assert.Equal("Research 'research/loan-period-benchmarks' holds no Findings section in its explanation.", finding.Message);
        Assert.Equal(Path.ChangeExtension(file, ".md"), finding.Path);
        Assert.Equal(1, finding.Line);
        Assert.Equal("research/loan-period-benchmarks", finding.Subject);
    }

    /// <summary>A research explanation with Findings passes.</summary>
    /// <proves>litespec/core/model#require-an-explanation-file-for-article-types</proves>
    [Fact]
    public void ResearchExplanationWithFindingsPasses()
    {
        using var temp = TestCli.Temp("checks");
        var file = Path.Combine(temp.Path, "benchmarks.yaml");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "research",
                "research/loan-period-benchmarks",
                file,
                1,
                """{"sources": [{"key": "benchmark-report"}], "question": "How long do others lend?", "conclusion": "Others lend for three weeks."}""",
                Path.Combine(temp.Path, "benchmarks.md"),
                "# Benchmarks\n\nThe benchmarks ask how long others lend.\n\n## Findings\n\nOthers lend for three weeks [per the report][benchmark-report].\n"));

        Assert.DoesNotContain("explanation-missing", TestModel.Codes(model));
    }

    /// <summary>A research record with no explanation file reports the missing file.</summary>
    /// <proves>litespec/core/model#require-an-explanation-file-for-article-types</proves>
    [Fact]
    public void ResearchWithoutAnExplanationReports()
    {
        using var temp = TestCli.Temp("checks");
        var file = Path.Combine(temp.Path, ".litespec", "benchmarks.yaml");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("research", "research/loan-period-benchmarks", file, """{"sources": [{"key": "benchmark-report"}], "question": "How long do others lend?", "conclusion": "Others lend for three weeks."}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "explanation-missing");
        Assert.Equal("A", finding.Tier);
        Assert.Equal("Research 'research/loan-period-benchmarks' holds no explanation file '.litespec/benchmarks.md'.", finding.Message);
        Assert.Equal(file, finding.Path);
        Assert.Equal(1, finding.Line);
        Assert.Equal("research/loan-period-benchmarks", finding.Subject);
    }

    /// <summary>An entry of an article type needs no explanation file.</summary>
    /// <proves>litespec/core/model#require-an-explanation-file-for-article-types</proves>
    [Fact]
    public void EntryOfAnArticleTypeNeedsNoExplanation()
    {
        using var temp = TestCli.Temp("checks");
        var file = Path.Combine(temp.Path, "0001-loan-period.yaml");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "decision",
                "decision/0001-loan-period",
                file,
                4,
                """{"options": [{"name": "Fourteen days", "pros": "Copies turn over fast.", "cons": "Readers rush."}], "reopenedWhen": "Demand shifts."}""",
                isEntry: true));

        Assert.DoesNotContain("explanation-missing", TestModel.Codes(model));
    }

    /// <summary>A page outside the article types needs no explanation file.</summary>
    /// <proves>litespec/core/model#require-an-explanation-file-for-article-types</proves>
    [Fact]
    public void NonArticlePageNeedsNoExplanation()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/lending.borrow-copy", Path.Combine(temp.Path, "borrow.yaml"), """{"kind": "command"}"""));

        Assert.DoesNotContain("explanation-missing", TestModel.Codes(model));
    }

    /// <summary>A feature on a required layer without its explanation file reports the missing file.</summary>
    /// <proves>litespec/core/model#require-explanation-files-on-the-layers-a-project-names</proves>
    [Fact]
    public void RequiredLayerWithoutExplanationReports()
    {
        using var temp = TestCli.Temp("checks");
        var projectDir = Path.Combine(temp.Path, ".litespec");
        Directory.CreateDirectory(projectDir);
        File.WriteAllText(Path.Combine(projectDir, "project.yaml"), "layers:\n  intent: litespec\n  behavior: litespec\nexplanations:\n  layers:\n  - intent\n");
        var file = Path.Combine(temp.Path, "submit-an-expense-claim.yaml");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("feature", "feature/submit-an-expense-claim", file, """{"summary": "Reminders nudge approvers."}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "explanation-missing");
        Assert.Equal("A", finding.Tier);
        Assert.Equal("Feature 'feature/submit-an-expense-claim' holds no explanation file 'submit-an-expense-claim.md'.", finding.Message);
        Assert.Equal(file, finding.Path);
        Assert.Equal(1, finding.Line);
        Assert.Equal("feature/submit-an-expense-claim", finding.Subject);
    }

    /// <summary>A feature explanation without Success Criteria reports the missing section.</summary>
    /// <proves>litespec/core/model#require-explanation-files-on-the-layers-a-project-names</proves>
    [Fact]
    public void RequiredLayerExplanationWithoutSectionReports()
    {
        using var temp = TestCli.Temp("checks");
        var projectDir = Path.Combine(temp.Path, ".litespec");
        Directory.CreateDirectory(projectDir);
        File.WriteAllText(Path.Combine(projectDir, "project.yaml"), "layers:\n  intent: litespec\n  behavior: litespec\nexplanations:\n  layers:\n  - intent\n");
        var file = Path.Combine(temp.Path, "submit-an-expense-claim.yaml");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "feature",
                "feature/submit-an-expense-claim",
                file,
                1,
                """{"summary": "Reminders nudge approvers."}""",
                Path.Combine(temp.Path, "submit-an-expense-claim.md"),
                "# Submit an Expense Claim\n\nEmployees submit claims and wait for an answer.\n\n## Capabilities\n\nReminders nudge approvers.\n\n## Boundaries\n\nThe feature stops at the approval step.\n"));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "explanation-missing");
        Assert.Equal("A", finding.Tier);
        Assert.Equal("Feature 'feature/submit-an-expense-claim' holds no Success Criteria section in its explanation.", finding.Message);
        Assert.Equal(Path.ChangeExtension(file, ".md"), finding.Path);
        Assert.Equal(1, finding.Line);
        Assert.Equal("feature/submit-an-expense-claim", finding.Subject);
    }

    /// <summary>A use case explanation without its Flow section reports the missing section.</summary>
    /// <proves>litespec/core/model#require-explanation-files-on-the-layers-a-project-names</proves>
    [Fact]
    public void UseCaseExplanationWithoutFlowReports()
    {
        using var temp = TestCli.Temp("checks");
        var projectDir = Path.Combine(temp.Path, ".litespec");
        Directory.CreateDirectory(projectDir);
        File.WriteAllText(Path.Combine(projectDir, "project.yaml"), "layers:\n  intent: litespec\n  behavior: litespec\nexplanations:\n  layers:\n  - behavior\n");
        var file = Path.Combine(temp.Path, "submit-expense.yaml");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "use-case",
                "use-case/spending.submit-expense",
                file,
                1,
                """{"kind": "command"}""",
                Path.Combine(temp.Path, "submit-expense.md"),
                "# Submit Expense\n\nNo sections here.\n"));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "explanation-missing");
        Assert.Equal("A", finding.Tier);
        Assert.Equal("Use case 'use-case/spending.submit-expense' holds no Flow section in its explanation.", finding.Message);
        Assert.Equal(Path.ChangeExtension(file, ".md"), finding.Path);
        Assert.Equal(1, finding.Line);
        Assert.Equal("use-case/spending.submit-expense", finding.Subject);
    }

    /// <summary>A use case on a layer not listed needs no explanation file.</summary>
    /// <proves>litespec/core/model#require-explanation-files-on-the-layers-a-project-names</proves>
    [Fact]
    public void RecordOnUnlistedLayerNeedsNoExplanation()
    {
        using var temp = TestCli.Temp("checks");
        var projectDir = Path.Combine(temp.Path, ".litespec");
        Directory.CreateDirectory(projectDir);
        File.WriteAllText(Path.Combine(projectDir, "project.yaml"), "layers:\n  intent: litespec\n  behavior: litespec\nexplanations:\n  layers:\n  - intent\n");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("use-case", "use-case/spending.submit-expense", Path.Combine(temp.Path, "submit-expense.yaml"), """{"kind": "command"}"""));

        Assert.DoesNotContain("explanation-missing", TestModel.Codes(model));
    }

    /// <summary>A feature with no explanations field needs no explanation file.</summary>
    /// <proves>litespec/core/model#require-explanation-files-on-the-layers-a-project-names</proves>
    [Fact]
    public void FieldAbsentNeedsNoExplanation()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("feature", "feature/submit-an-expense-claim", Path.Combine(temp.Path, "submit-an-expense-claim.yaml"), """{"summary": "Reminders nudge approvers."}"""));

        Assert.DoesNotContain("explanation-missing", TestModel.Codes(model));
    }

    /// <summary>A listed layer that is not claimed reports the configuration finding.</summary>
    /// <proves>litespec/core/model#require-explanation-files-on-the-layers-a-project-names</proves>
    [Fact]
    public void ListedLayerNotClaimedReports()
    {
        using var temp = TestCli.Temp("checks");
        var projectDir = Path.Combine(temp.Path, ".litespec");
        Directory.CreateDirectory(projectDir);
        var projectFile = Path.Combine(projectDir, "project.yaml");
        File.WriteAllText(projectFile, "layers:\n  intent: litespec\nexplanations:\n  layers:\n  - behavior\n");
        var model = TestModel.Model(TestModel.Config(temp.Path));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "project-invalid");
        Assert.Equal("G", finding.Tier);
        Assert.Equal("explanations.layers names 'behavior', which the project does not claim in layers [litespec/core/model#require-explanation-files-on-the-layers-a-project-names]", finding.Message);
        Assert.Equal(projectFile, finding.Path);
        Assert.Equal(1, finding.Line);
        Assert.Null(finding.Subject);
    }

    /// <summary>One representative type per layer passes with each of its fixed sections, in order.</summary>
    /// <param name="type">The record type under test.</param>
    /// <param name="id">The record identifier.</param>
    /// <param name="sections">The fixed sections the explanation holds, in order.</param>
    /// <proves>litespec/core/writing#give-each-explanation-file-its-fixed-sections</proves>
    [Theory]
    [InlineData("product", "product/lending", new[] { "Scope", "Users", "Out of Scope", "Direction" })]
    [InlineData("aggregate", "aggregate/lending.loan", new[] { "Lifecycle", "Data and Retention", "Example" })]
    [InlineData("runbook", "runbook/restart-queue", new[] { "Prerequisites", "Steps", "Verification", "Recovery" })]
    [InlineData("departure", "departure/hold-queue", new[] { "Provision", "Reason", "Risk", "Expiry" })]
    public void RepresentativeTypePassesWithItsFixedSections(string type, string id, string[] sections)
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            RecordWithExplanation(temp.Path, type, id, Explanation("An opening paragraph names the record.", sections)));

        var codes = TestModel.Codes(model);
        Assert.DoesNotContain("explanation-missing", codes);
        Assert.DoesNotContain("explanation-opening-missing", codes);
    }

    /// <summary>An explanation missing a fixed section reports the section by its name.</summary>
    /// <param name="type">The record type under test.</param>
    /// <param name="id">The record identifier.</param>
    /// <param name="sections">The fixed sections of the type, in order.</param>
    /// <param name="missing">The section the explanation leaves out.</param>
    /// <proves>litespec/core/writing#give-each-explanation-file-its-fixed-sections</proves>
    [Theory]
    [InlineData("runbook", "runbook/restart-queue", new[] { "Prerequisites", "Steps", "Verification", "Recovery" }, "Recovery")]
    [InlineData("departure", "departure/hold-queue", new[] { "Provision", "Reason", "Risk", "Expiry" }, "Risk")]
    [InlineData("actor", "actor/reader", new[] { "Responsibilities", "Knowledge", "Example" }, "Knowledge")]
    public void MissingFixedSectionReportsItsName(string type, string id, string[] sections, string missing)
    {
        using var temp = TestCli.Temp("checks");
        var kept = sections.Where(section => section != missing).ToArray();
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            RecordWithExplanation(temp.Path, type, id, Explanation("An opening paragraph names the record.", kept)));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "explanation-missing");
        Assert.Equal("A", finding.Tier);
        Assert.Equal($"{char.ToUpperInvariant(type[0])}{type[1..]} '{id}' holds no {missing} section in its explanation.", finding.Message);
        Assert.Equal(id, finding.Subject);
    }

    /// <summary>An explanation holding a section its type's table does not state reports that section by its name.</summary>
    /// <proves>litespec/core/writing#give-each-explanation-file-its-fixed-sections</proves>
    [Fact]
    public void SectionOutsideTheTableIsReported()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            RecordWithExplanation(temp.Path, "actor", "actor/reader", Explanation("An opening paragraph names the record.", ["Responsibilities", "Access", "Knowledge", "Example"])));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "explanation-section-unknown");
        Assert.Equal("Actor 'actor/reader' holds the section 'Access' ahead of its fixed sections, and its type's section table does not state it. Free sections follow the fixed ones.", finding.Message);
    }

    /// <summary>Fixed sections the file holds out of table order report the pair that is out of order.</summary>
    /// <proves>litespec/core/writing#give-each-explanation-file-its-fixed-sections</proves>
    [Fact]
    public void FixedSectionsOutOfOrderReport()
    {
        using var temp = TestCli.Temp("checks");
        var text = "# Loan period\n\nThe loan period decides how long a copy stays out.\n\n"
            + "## Decision\n\nFourteen days.\n\n## Context\n\nReaders asked for a longer loan.\n\n"
            + "## Options\n\nFourteen days or twenty-one.\n\n## Consequences\n\nCopies turn over faster.\n\n"
            + "## Revisit Conditions\n\nDemand shifts.\n";
        var model = TestModel.Model(TestModel.Config(temp.Path), RecordWithExplanation(temp.Path, "decision", "decision/0001-loan-period", text));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "explanation-missing");
        Assert.Equal("Decision 'decision/0001-loan-period' holds its sections out of order: Decision must follow Context.", finding.Message);
    }

    /// <summary>An explanation that opens straight into its first H2 reports the missing opening paragraph.</summary>
    /// <proves>litespec/core/writing#open-each-explanation-file-with-a-paragraph</proves>
    [Fact]
    public void ExplanationWithoutOpeningParagraphReports()
    {
        using var temp = TestCli.Temp("checks");
        var record = RecordWithExplanation(
            temp.Path,
            "goal",
            "goal/faster-pickup",
            "# Faster Pickup\n\n## Target\n\nTen minutes.\n\n## Measurement\n\nThe pickup desk logs each wait.\n");
        var model = TestModel.Model(TestModel.Config(temp.Path), record);

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "explanation-opening-missing");
        Assert.Equal("A", finding.Tier);
        Assert.Equal("Goal 'goal/faster-pickup' opens with no paragraph between its H1 and its first H2.", finding.Message);
        Assert.Equal(Path.Combine(temp.Path, ".litespec", "goal", "faster-pickup", "faster-pickup.md"), finding.Path);
        Assert.Equal("goal/faster-pickup", finding.Subject);
    }

    /// <summary>An explanation with its opening paragraph and every fixed section reports nothing.</summary>
    /// <proves>litespec/core/writing#open-each-explanation-file-with-a-paragraph</proves>
    [Fact]
    public void ExplanationWithOpeningParagraphPasses()
    {
        using var temp = TestCli.Temp("checks");
        var record = RecordWithExplanation(
            temp.Path,
            "goal",
            "goal/faster-pickup",
            "# Faster Pickup\n\nPickup takes too long, and members leave.\n\n## Target\n\nTen minutes.\n\n## Measurement\n\nThe pickup desk logs each wait.\n");
        var model = TestModel.Model(TestModel.Config(temp.Path), record);

        var codes = TestModel.Codes(model);
        Assert.DoesNotContain("explanation-opening-missing", codes);
        Assert.DoesNotContain("explanation-missing", codes);
    }

    /// <summary>A comment and a code block before the first H2 are not the opening paragraph, so the explanation reports it.</summary>
    /// <proves>litespec/core/writing#open-each-explanation-file-with-a-paragraph</proves>
    [Fact]
    public void CommentAndCodeBeforeTheFirstSectionAreNotAnOpeningParagraph()
    {
        using var temp = TestCli.Temp("checks");
        var record = RecordWithExplanation(
            temp.Path,
            "goal",
            "goal/faster-pickup",
            "# Faster Pickup\n\n<!-- Written by the team. -->\n\n```text\nPickup takes too long.\n```\n\n## Target\n\nTen minutes.\n\n## Measurement\n\nThe pickup desk logs each wait.\n");
        var model = TestModel.Model(TestModel.Config(temp.Path), record);

        Assert.Single(Checker.Check(model).Findings, found => found.Code == "explanation-opening-missing");
    }

    /// <summary>A heading written inside a code block is not a section, so the explanation reports nothing for it.</summary>
    /// <proves>litespec/core/writing#give-each-explanation-file-its-fixed-sections</proves>
    [Fact]
    public void HeadingInsideACodeBlockIsNotASection()
    {
        using var temp = TestCli.Temp("checks");
        var record = RecordWithExplanation(
            temp.Path,
            "goal",
            "goal/faster-pickup",
            "# Faster Pickup\n\nPickup takes too long, and members leave.\n\n## Target\n\nTen minutes.\n\n## Measurement\n\n```markdown\n## Not A Section\n```\n");
        var model = TestModel.Model(TestModel.Config(temp.Path), record);

        Assert.DoesNotContain("explanation-section-unknown", TestModel.Codes(model));
    }

    /// <summary>A section finding points at the explanation file and the line of the heading it reports.</summary>
    /// <proves>litespec/core/writing#give-each-explanation-file-its-fixed-sections</proves>
    [Fact]
    public void SectionFindingPointsAtTheExplanationFile()
    {
        using var temp = TestCli.Temp("checks");
        var record = RecordWithExplanation(
            temp.Path,
            "goal",
            "goal/faster-pickup",
            "# Faster Pickup\n\nPickup takes too long, and members leave.\n\n## Target\n\nTen minutes.\n\n## Notes\n\nOne line.\n\n## Measurement\n\nThe desk logs each wait.\n");
        var model = TestModel.Model(TestModel.Config(temp.Path), record);

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "explanation-section-unknown");
        Assert.Equal(Path.Combine(temp.Path, ".litespec", "goal", "faster-pickup", "faster-pickup.md"), finding.Path);
        Assert.Equal(9, finding.Line);
    }

    /// <summary>A free section after the fixed sections is allowed, so the explanation reports nothing for it.</summary>
    /// <proves>litespec/core/writing#give-each-explanation-file-its-fixed-sections</proves>
    [Fact(DisplayName = "a free section after the fixed sections passes")]
    public void FreeSectionAfterTheFixedSectionsPasses()
    {
        using var temp = TestCli.Temp("checks");
        var record = RecordWithExplanation(
            temp.Path,
            "goal",
            "goal/faster-pickup",
            "# Faster Pickup\n\nPickup takes too long, and members leave.\n\n## Target\n\nTen minutes.\n\n## Measurement\n\nThe desk logs each wait.\n\n## Notes\n\nOne line.\n");
        var model = TestModel.Model(TestModel.Config(temp.Path), record);

        Assert.DoesNotContain("explanation-section-unknown", TestModel.Codes(model));
    }

    /// <summary>A record with its explanation file beside it, as the reader sees it.</summary>
    /// <param name="root">The fixture root folder.</param>
    /// <param name="type">The record type.</param>
    /// <param name="id">The record identifier.</param>
    /// <param name="text">The explanation file text.</param>
    /// <returns>The record with its explanation file.</returns>
    static LiteSpec.Cli.Model.Records.Record RecordWithExplanation(string root, string type, string id, string text)
    {
        var name = id[(id.IndexOf('/') + 1)..].Split('.').Last();
        var file = Path.Combine(root, ".litespec", type, name, name + ".yaml");
        var explanation = Path.ChangeExtension(file, ".md");

        return TestModel.Record(type, id, file, 1, "{}", explanation, text);
    }

    /// <summary>An explanation file text: the H1 title, one opening paragraph, then each section with one line.</summary>
    /// <param name="opening">The opening paragraph.</param>
    /// <param name="sections">The H2 section names, in order.</param>
    /// <returns>The explanation file text.</returns>
    static string Explanation(string opening, string[] sections) =>
        "# Record Title\n\n" + opening + "\n\n" + string.Concat(sections.Select(section => $"## {section}\n\nOne line of prose.\n\n"));
}
