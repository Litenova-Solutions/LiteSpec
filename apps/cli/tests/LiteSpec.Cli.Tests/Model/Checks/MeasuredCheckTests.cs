using Record = LiteSpec.Model.Record;
using LiteSpec.Model;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves the requirement-measure-missing check against hand-built models.</summary>
public sealed class MeasuredCheckTests
{
    /// <summary>Measured Without A Check Reports.</summary>
    /// <proves>litespec/quality/ci#run-measured-checks-on-every-pull-request</proves>
    [Fact]
    public void MeasuredWithoutACheckReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "requirement",
                "requirement/borrowing.list-loans-latency",
                Path.Combine(temp.Path, "borrowing.md"),
                """{"kind": "quality", "statement": "Listing loans answers within 300 milliseconds at p95.", "stimulus": "Two hundred readers list their loans in the first hour of the term.", "environment": "Normal operation, the search index warm.", "response": "Each listing answers from the index.", "measure": "p95 latency of listing loans", "dueBy": "2027-01-01"}"""));

        Assert.Contains("requirement-measure-missing", TestModel.Codes(model));
    }

    /// <summary>Unmeasured Requirement Passes.</summary>
    /// <proves>litespec/quality/ci#run-measured-checks-on-every-pull-request</proves>
    [Fact]
    public void UnmeasuredRequirementPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record(
                "requirement",
                "requirement/borrowing.max-active-loans",
                Path.Combine(temp.Path, "borrowing.md"),
                """{"kind": "functional", "statement": "A reader holds at most five active loans."}"""));

        Assert.DoesNotContain("requirement-measure-missing", TestModel.Codes(model));
    }

    /// <summary>Measured With A Check But No Run Reports.</summary>
    /// <proves>litespec/quality/ci#run-measured-checks-on-every-pull-request</proves>
    [Fact]
    public void MeasuredWithACheckButNoRunReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(TestModel.Config(temp.Path), Measured(temp.Path));

        Assert.Contains("requirement-measure-missing", TestModel.Codes(model));
    }

    /// <summary>Failing Run Reports.</summary>
    /// <proves>litespec/quality/ci#run-measured-checks-on-every-pull-request</proves>
    [Fact]
    public void FailingRunReports()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(TestModel.Config(temp.Path), Measured(temp.Path));
        model.Measures.Add(new MeasureResult("k6", "p95<200", false, "240", "TestResults/k6.json"));

        Assert.Contains("requirement-measure-missing", TestModel.Codes(model));
    }

    /// <summary>Passing Run Passes.</summary>
    /// <proves>litespec/quality/ci#run-measured-checks-on-every-pull-request</proves>
    [Fact]
    public void PassingRunPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(TestModel.Config(temp.Path), Measured(temp.Path, "p95<200"));
        model.Measures.Add(new MeasureResult("k6", "p95<200", true, "180", "TestResults/k6.json"));

        Assert.DoesNotContain("requirement-measure-missing", TestModel.Codes(model));
    }

    /// <summary>Passing Run Matches Tool And Check Form.</summary>
    /// <proves>litespec/quality/ci#run-measured-checks-on-every-pull-request</proves>
    [Fact]
    public void PassingRunMatchesToolAndCheckForm()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(TestModel.Config(temp.Path), Measured(temp.Path, "k6 p95<200"));
        model.Measures.Add(new MeasureResult("k6", "p95<200", true, "180", "TestResults/k6.json"));

        Assert.DoesNotContain("requirement-measure-missing", TestModel.Codes(model));
    }

    /// <summary>Statement Naming Number Without Measure Reports At Tier A.</summary>
    /// <proves>litespec/core/intent#state-the-measure-on-a-counted-requirement</proves>
    [Fact]
    public void StatementNamingNumberWithoutMeasureReportsAtTierA()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("requirement", "requirement/borrowing.max-active-loans", Path.Combine(temp.Path, "borrowing.yaml"), """{"kind": "functional", "statement": "A reader holds at most 5 active loans."}"""));

        var finding = Assert.Single(Checker.Check(model).Findings, found => found.Code == "requirement-measure-missing");
        Assert.Equal("A", finding.Tier);
        Assert.Equal("Requirement 'requirement/borrowing.max-active-loans' names a number with no measure.", finding.Message);
        Assert.Equal("requirement/borrowing.max-active-loans", finding.Subject);
    }

    /// <summary>Statement Naming Number Words Without Measure Passes.</summary>
    /// <proves>litespec/core/intent#state-the-measure-on-a-counted-requirement</proves>
    [Fact]
    public void StatementNamingNumberWordsWithoutMeasurePasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("requirement", "requirement/borrowing.max-active-loans", Path.Combine(temp.Path, "borrowing.yaml"), """{"kind": "functional", "statement": "A reader holds at most five active loans."}"""));

        Assert.DoesNotContain("requirement-measure-missing", TestModel.Codes(model));
    }

    /// <summary>Statement Naming Only A Citation Passes.</summary>
    /// <proves>litespec/core/intent#state-the-measure-on-a-counted-requirement</proves>
    [Fact]
    public void StatementNamingOnlyACitationPasses()
    {
        using var temp = TestCli.Temp("checks");
        var model = TestModel.Model(
            TestModel.Config(temp.Path),
            TestModel.Record("requirement", "requirement/borrowing.loan-period-days", Path.Combine(temp.Path, "borrowing.yaml"), """{"kind": "functional", "statement": "Loans run as decision/0001-loan-period decides.", "cites": ["decision/0001-loan-period"]}"""),
            TestModel.Record("decision", "decision/0001-loan-period", Path.Combine(temp.Path, "decisions.yaml"), """{"options": [{"name": "Fourteen days", "pros": "Readers finish in time.", "cons": "Popular copies queue."}], "choice": "Fourteen days", "reopenedWhen": "Queues pass one hundred a day."}"""));

        Assert.DoesNotContain("requirement-measure-missing", TestModel.Codes(model));
    }

    static Record Measured(string root, string check = "p95<200") =>
        TestModel.Record(
            "requirement",
            "requirement/borrowing.list-loans-latency",
            Path.Combine(root, "borrowing.md"),
            "{\"kind\": \"quality\", \"statement\": \"Listing loans answers within 300 milliseconds at p95.\", \"stimulus\": \"Two hundred readers list their loans in the first hour of the term.\", \"environment\": \"Normal operation, the search index warm.\", \"response\": \"Each listing answers from the index.\", \"measure\": \"p95 latency of listing loans\", \"by\": \"2027-01-01\", \"check\": \"" + check + "\"}");
}
