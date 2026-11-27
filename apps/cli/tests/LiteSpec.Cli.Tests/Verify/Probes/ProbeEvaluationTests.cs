using System.Text.Json.Nodes;
using LiteSpec.Cli.Verify.Probes;

namespace LiteSpec.Cli.Tests.Verify.Probes;

/// <summary>Proves the probe response evaluation compares status, refusal and fields.</summary>
public sealed class ProbeEvaluationTests
{
    /// <summary>Evaluation passes when the status and every field match the expectation.</summary>
    [Fact]
    public void EvaluatePassesWhenStatusAndFieldsMatch()
    {
        var probe = Case(200, null, ("/state", "lent", true));

        var failures = ProbeEvaluation.Evaluate(probe, 200, """{"state":"lent"}""");

        Assert.Empty(failures);
    }

    /// <summary>Evaluation reports one failure when the status differs from the expectation.</summary>
    [Fact]
    public void EvaluateFailsWhenStatusDiffers()
    {
        var probe = Case(200, null);

        var failures = ProbeEvaluation.Evaluate(probe, 404, """{"state":"lent"}""");

        Assert.Single(failures);
    }

    /// <summary>Evaluation reports one failure when a field holds another value.</summary>
    [Fact]
    public void EvaluateFailsWhenFieldDiffers()
    {
        var probe = Case(200, null, ("/state", "lent", true));

        var failures = ProbeEvaluation.Evaluate(probe, 200, """{"state":"lost"}""");

        Assert.Single(failures);
    }

    /// <summary>Evaluation passes when the problem code equals the expected refusal.</summary>
    [Fact]
    public void EvaluatePassesWhenRefusalCodeMatches()
    {
        var probe = Case(409, "lending.copy-not-available");

        var failures = ProbeEvaluation.Evaluate(probe, 409, """{"code":"lending.copy-not-available"}""");

        Assert.Empty(failures);
    }

    /// <summary>Evaluation passes when the problem type ends with the expected refusal.</summary>
    [Fact]
    public void EvaluatePassesWhenRefusalTypeMatches()
    {
        var probe = Case(409, "lending.copy-not-available");

        var failures = ProbeEvaluation.Evaluate(probe, 409, """{"type":"https://example.com/problems/lending.copy-not-available"}""");

        Assert.Empty(failures);
    }

    /// <summary>Evaluation reports one failure when the refusal names another code and type.</summary>
    [Fact]
    public void EvaluateFailsWhenRefusalDiffers()
    {
        var probe = Case(409, "lending.copy-not-available");

        var failures = ProbeEvaluation.Evaluate(probe, 409, """{"code":"lending.other","type":"https://example.com/problems/lending.other"}""");

        Assert.Single(failures);
    }

    /// <summary>Evaluation passes when a present check finds its path and fails when it does not.</summary>
    [Fact]
    public void EvaluateChecksPresence()
    {
        var present = new ProbeCase("scenario/a.b", "use-case/a.b", "f", 1, null, "GET", "/", null, 200, null, [new ProbeFieldCheck("/expenseId", null, false, true)]);
        var absent = new ProbeCase("scenario/a.b", "use-case/a.b", "f", 1, null, "GET", "/", null, 200, null, [new ProbeFieldCheck("/missing", null, false, true)]);

        Assert.Empty(ProbeEvaluation.Evaluate(present, 200, """{"expenseId":"e1"}"""));
        Assert.Single(ProbeEvaluation.Evaluate(absent, 200, """{"expenseId":"e1"}"""));
    }

    /// <summary>Evaluation resolves a nested pointer with an array index.</summary>
    [Fact]
    public void EvaluateResolvesNestedPointer()
    {
        var probe = Case(200, null, ("/lines/0/amount", 42.5, false));

        Assert.Empty(ProbeEvaluation.Evaluate(probe, 200, """{"lines":[{"amount":42.5}]}"""));
        Assert.Single(ProbeEvaluation.Evaluate(probe, 200, """{"lines":[{"amount":1}]}"""));
    }

    /// <summary>Builds one probe case for the evaluation tests.</summary>
    /// <param name="status">The expected status.</param>
    /// <param name="refusal">The expected refusal, or null.</param>
    /// <param name="fields">The field checks as path, value and string flag.</param>
    /// <returns>The probe case.</returns>
    static ProbeCase Case(int status, string? refusal, params (string Path, object? Value, bool IsString)[] fields)
    {
        var checks = new List<ProbeFieldCheck>();

        foreach (var (path, value, isString) in fields)
        {
            JsonNode? equals = value switch
            {
                null => null,
                string text when isString => JsonValue.Create(text),
                string text => JsonNode.Parse(text),
                double number => JsonValue.Create(number),
                _ => JsonValue.Create(value.ToString()!),
            };

            checks.Add(new ProbeFieldCheck(path, equals, true, null));
        }

        return new ProbeCase("scenario/a.b", "use-case/a.b", "f", 1, null, "GET", "/", null, status, refusal, checks);
    }
}
