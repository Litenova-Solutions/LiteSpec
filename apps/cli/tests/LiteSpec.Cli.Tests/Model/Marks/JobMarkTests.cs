namespace LiteSpec.Tests.Model.Marks;

/// <summary>Proves a plain class carrying a job mark hosts its job: it clears job-unimplemented and takes exactly one mark.</summary>
public sealed class JobMarkTests
{
    /// <proves>litespec/core/model#implement-each-mapped-behavior-record-behind-a-mark</proves>
    [Fact]
    public void Marked_Plain_Class_Clears_Job_Unimplemented()
    {
        using var fixture = MarksFixture.Prepare("unimplemented");
        WriteJob(fixture.Path);
        WriteSource(fixture.Path, "Library.Infrastructure", "ChargeDuesJob.cs", """
            namespace Library.Infrastructure.Billing;

            /// <implements>job/billing.charge-dues</implements>
            public sealed class ChargeDuesJob
            {
                public void Run()
                {
                }
            }
            """);

        var findings = MarksFixture.Check(fixture.Path);

        Assert.Contains(MarksFixture.WithCode(findings, "job-unimplemented"), finding => finding.Subject != null && finding.Subject.StartsWith("job/lending.", StringComparison.Ordinal));
        Assert.DoesNotContain(MarksFixture.WithCode(findings, "job-unimplemented"), finding => finding.Subject == "job/billing.charge-dues");
        Assert.Empty(MarksFixture.WithCode(findings, "mark-missing"));
        Assert.Empty(MarksFixture.WithCode(findings, "mark-count-invalid"));
    }

    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Fact]
    public void Marked_Plain_Class_With_Two_Marks_Reports_Mark_Count()
    {
        using var fixture = MarksFixture.Prepare("unimplemented");
        WriteJob(fixture.Path);
        WriteSource(fixture.Path, "Library.Infrastructure", "ChargeDuesJob.cs", """
            namespace Library.Infrastructure.Billing;

            /// <implements>job/billing.charge-dues</implements>
            /// <implements>job/billing.charge-dues</implements>
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
        Assert.DoesNotContain(MarksFixture.WithCode(findings, "job-unimplemented"), unimplemented => unimplemented.Subject == "job/billing.charge-dues");
    }

    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    /// <proves>litespec/core/model#implement-each-mapped-behavior-record-behind-a-mark</proves>
    [Fact]
    public void Unmarked_Plain_Class_Is_Technical_And_Job_Stays_Unimplemented()
    {
        using var fixture = MarksFixture.Prepare("unimplemented");
        WriteJob(fixture.Path);
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
        Assert.Contains(MarksFixture.WithCode(findings, "job-unimplemented"), unimplemented => unimplemented.Subject == "job/billing.charge-dues");
    }

    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    /// <proves>litespec/core/model#implement-each-mapped-behavior-record-behind-a-mark</proves>
    [Fact]
    public void Marked_Class_In_Domain_Is_Not_A_Job_Host()
    {
        using var fixture = MarksFixture.Prepare("unimplemented");
        WriteJob(fixture.Path);
        WriteSource(fixture.Path, "Library.Domain", "ChargeDuesJob.cs", """
            namespace Library.Domain.Billing;

            /// <implements>job/billing.charge-dues</implements>
            public sealed class ChargeDuesJob
            {
                public void Run()
                {
                }
            }
            """);

        var findings = MarksFixture.Check(fixture.Path);

        Assert.Contains(MarksFixture.WithCode(findings, "job-unimplemented"), finding => finding.Subject == "job/billing.charge-dues");

        var unresolved = MarksFixture.WithCode(findings, "mark-unknown");
        var stray = Assert.Single(unresolved);
        Assert.Contains("ChargeDuesJob", stray.Message);
        Assert.Contains("takes no mark", stray.Message);
    }

    static void WriteJob(string root)
    {
        var folder = Path.Combine(root, "docs", "behavior", "modules", "billing", "jobs");
        Directory.CreateDirectory(folder);
        File.WriteAllText(
            Path.Combine(folder, "charge-dues.md"),
            """
            ---
            {
              "type": "job",
              "id": "job/billing.charge-dues",
              "title": "Charge dues",
              "owner": "lending-team",
              "status": "reviewed",
              "kind": "scheduled",
              "schedule": {
                "every": "P1D"
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
