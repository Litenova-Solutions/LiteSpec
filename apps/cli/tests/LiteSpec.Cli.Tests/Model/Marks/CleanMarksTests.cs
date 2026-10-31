namespace LiteSpec.Tests.Model.Marks;

/// <summary>Proves the clean marks fixture reports none of the traceability findings.</summary>
public sealed class CleanMarksTests
{
    /// <summary>Every traceability finding code the CLI reports.</summary>
    public static TheoryData<string> TraceabilityCodes
    {
        get
        {
            TheoryData<string> data = new();

            foreach (var code in new[]
            {
                "mark-missing",
                "mark-unknown",
                "mark-count-invalid",
                "mark-style-invalid",
                "mark-folder-drift",
                "runtime-value-drift",
                "act-event-undeclared",
                "act-event-unimplemented",
                "use-case-act-missing",
                "aggregate-unimplemented",
                "automation-unimplemented",
                "external-system-unimplemented",
                "use-case-endpoint-missing",
                "effect-unimplemented",
                "use-case-unreached",
            })
            {
                data.Add(code);
            }

            return data;
        }
    }

    /// <proves>litespec/core/system#point-code-and-tests-up-to-records</proves>
    [Theory]
    [MemberData(nameof(TraceabilityCodes))]
    public void Clean_Fixture_Reports_No(string code)
    {
        using var fixture = MarksFixture.Prepare("clean");
        var findings = MarksFixture.Check(fixture.Path);

        Assert.DoesNotContain(code, findings.Select(finding => finding.Code));
    }

    /// <proves>litespec/core/model#compute-proof-statuses-in-the-builder</proves>
    [Fact]
    public void Marked_Rule_Exception_Proves_Its_Refusal_Code()
    {
        using var fixture = MarksFixture.Prepare("clean");
        var findings = MarksFixture.Check(fixture.Path);

        Assert.DoesNotContain(
            findings,
            finding => finding.Code == "refusal-unimplemented" && finding.Subject == "result/borrow-copy.copy-unavailable");
    }
}
