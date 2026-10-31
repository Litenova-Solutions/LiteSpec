namespace LiteSpec.Tests.Model.Marks;

/// <summary>Proves a plain class carrying an automation mark hosts its automation: it clears automation-unimplemented and takes exactly one mark.</summary>
public sealed class JobMarkTests
{
    /// <proves>litespec/core/model#implement-each-mapped-behavior-record-behind-a-mark</proves>
    [Fact]
    public void Marked_Plain_Class_Clears_Automation_Unimplemented()
    {
        using var fixture = MarksFixture.Prepare("unimplemented");
        WriteAutomation(fixture.Path);
        WriteSource(fixture.Path, "Library.Infrastructure", "ChargeDuesJob.cs", """
            namespace Library.Infrastructure.Billing;

            /// <implements>automation/billing.charge-dues</implements>
            public sealed class ChargeDuesJob
            {
                public void Run()
                {
                }
            }
            """);

        var findings = MarksFixture.Check(fixture.Path);

        Assert.Contains(MarksFixture.WithCode(findings, "automation-unimplemented"), finding => finding.Subject != null && finding.Subject.StartsWith("automation/lending.", StringComparison.Ordinal));
        Assert.DoesNotContain(MarksFixture.WithCode(findings, "automation-unimplemented"), finding => finding.Subject == "automation/billing.charge-dues");
        Assert.Empty(MarksFixture.WithCode(findings, "mark-missing"));
        Assert.Empty(MarksFixture.WithCode(findings, "mark-count-invalid"));
    }

    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact]
    public void Marked_Plain_Class_With_Two_Marks_Reports_Mark_Count()
    {
        using var fixture = MarksFixture.Prepare("unimplemented");
        WriteAutomation(fixture.Path);
        WriteSource(fixture.Path, "Library.Infrastructure", "ChargeDuesJob.cs", """
            namespace Library.Infrastructure.Billing;

            /// <implements>automation/billing.charge-dues</implements>
            /// <implements>automation/billing.charge-dues</implements>
            public sealed class ChargeDuesJob
            {
                public void Run()
                {
                }
            }
            """);

        var findings = MarksFixture.Check(fixture.Path);
        var counts = MarksFixture.WithCode(findings, "mark-count-invalid");

        var finding = Assert.Single(counts, found => found.Message.Contains("twice"));
        Assert.Contains("ChargeDuesJob", finding.Message);
        Assert.DoesNotContain(MarksFixture.WithCode(findings, "automation-unimplemented"), unimplemented => unimplemented.Subject == "automation/billing.charge-dues");
    }

    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    /// <proves>litespec/core/model#implement-each-mapped-behavior-record-behind-a-mark</proves>
    [Fact]
    public void Unmarked_Plain_Class_Is_Technical_And_Automation_Stays_Unimplemented()
    {
        using var fixture = MarksFixture.Prepare("unimplemented");
        WriteAutomation(fixture.Path);
        WriteSource(fixture.Path, "Library.Infrastructure", "ChargeDuesJob.cs", """
            namespace Library.Infrastructure.Billing;

            public sealed class ChargeDuesJob
            {
                public void Run()
                {
                }
            }
            """);

        var findings = MarksFixture.Check(fixture.Path);
        var missing = MarksFixture.WithCode(findings, "mark-missing");

        var finding = Assert.Single(missing);
        Assert.Contains("Technical class", finding.Message);
        Assert.Contains("ChargeDuesJob", finding.Message);
        Assert.Contains(MarksFixture.WithCode(findings, "automation-unimplemented"), unimplemented => unimplemented.Subject == "automation/billing.charge-dues");
    }

    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    /// <proves>litespec/core/model#implement-each-mapped-behavior-record-behind-a-mark</proves>
    [Fact]
    public void Marked_Class_In_Domain_Is_Not_A_Job_Host()
    {
        using var fixture = MarksFixture.Prepare("unimplemented");
        WriteAutomation(fixture.Path);
        WriteSource(fixture.Path, "Library.Domain", "ChargeDuesJob.cs", """
            namespace Library.Domain.Billing;

            /// <implements>automation/billing.charge-dues</implements>
            public sealed class ChargeDuesJob
            {
                public void Run()
                {
                }
            }
            """);

        var findings = MarksFixture.Check(fixture.Path);

        Assert.Contains(MarksFixture.WithCode(findings, "automation-unimplemented"), finding => finding.Subject == "automation/billing.charge-dues");

        var unresolved = MarksFixture.WithCode(findings, "mark-unknown");
        var stray = Assert.Single(unresolved);
        Assert.Contains("ChargeDuesJob", stray.Message);
        Assert.Contains("takes no mark", stray.Message);
    }

    static void WriteAutomation(string root)
    {
        var folder = Path.Combine(root, "docs", "behavior", "modules", "billing", "automations");
        Directory.CreateDirectory(folder);
        File.WriteAllText(
            Path.Combine(folder, "charge-dues.md"),
            """
            ---
            {
              "type": "automation",
              "id": "automation/billing.charge-dues",
              "title": "Charge dues",
              "owner": "lending-team",
              "status": "reviewed",
              "triggeredBy": {
                "schedule": {
                  "every": "P1D"
                }
              },
              "effects": [
                {
                  "runs": "use-case/billing.charge-dues",
                  "timing": "async"
                }
              ]
            }
            ---

            # Charge Dues

            Each night the dues are charged.
            """);
    }

    static void WriteSource(string root, string project, string file, string text)
    {
        var folder = Path.Combine(root, "apps", "api", "src", project);
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, file), text);
    }
}
