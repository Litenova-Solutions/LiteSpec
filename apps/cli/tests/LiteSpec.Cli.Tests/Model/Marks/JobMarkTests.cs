using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Marks;

/// <summary>Proves a plain class carrying a job mark hosts its job: it clears job-unimplemented.</summary>
public sealed class JobMarkTests
{
    /// <summary>Marked Plain Class Clears Job Unimplemented.</summary>
    /// <proves>litespec/core/model#implement-each-mapped-behavior-record-behind-a-mark</proves>
    [Fact]
    public void MarkedPlainClassClearsJobUnimplemented()
    {
        using var fixture = MarksFixture.Prepare("unimplemented");
        WriteJob(fixture.Path);
        WriteSource(fixture.Path, Path.Combine("Modules", "Library.Lending", "Application", "ChargeDues"), "ChargeDuesJob.cs", """
            namespace Library.Lending.Application.ChargeDues;

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
    }

    /// <summary>Unmarked Plain Class Is Technical And Job Stays Unimplemented.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    /// <proves>litespec/core/model#implement-each-mapped-behavior-record-behind-a-mark</proves>
    [Fact]
    public void UnmarkedPlainClassIsTechnicalAndJobStaysUnimplemented()
    {
        using var fixture = MarksFixture.Prepare("unimplemented");
        WriteJob(fixture.Path);
        WriteSource(fixture.Path, Path.Combine("Modules", "Library.Lending", "Application", "ChargeDues"), "ChargeDuesJob.cs", """
            namespace Library.Lending.Application.ChargeDues;

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

    /// <summary>Marked Class In Domain Is Not A Job Host.</summary>
    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    /// <proves>litespec/core/model#implement-each-mapped-behavior-record-behind-a-mark</proves>
    [Fact]
    public void MarkedClassInDomainIsNotAJobHost()
    {
        using var fixture = MarksFixture.Prepare("unimplemented");
        WriteJob(fixture.Path);
        WriteSource(fixture.Path, Path.Combine("Modules", "Library.Lending", "Domain", "Loans"), "ChargeDuesJob.cs", """
            namespace Library.Lending.Domain.Loans;

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
        TestRecords.Write(root, ".litespec/behavior/modules/billing/jobs/charge-dues.yaml", """
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
            """);
        TestRecords.Explain(root, ".litespec/behavior/modules/billing/jobs/charge-dues.md", """
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
