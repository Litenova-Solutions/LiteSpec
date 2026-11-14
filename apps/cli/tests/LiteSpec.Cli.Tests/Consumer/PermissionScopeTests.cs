using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Consumer;

/// <summary>
/// Proves <c>litespec model check</c> keeps the scope on the role only: a permission entry
/// carrying <c>scope</c> fails the schema, a role without <c>scope</c> fails it, and an actor
/// holds roles as plain identifiers. Each test owns its own folder because tests run in parallel.
/// </summary>
public sealed class PermissionScopeTests
{
    /// <summary>One <c>model check</c> finding line: its path, line, level, code and message.</summary>
    static readonly Regex FindingLine = new(
        @"^(?<path>.+?):(?<line>\d+) \[(?<level>G|S|A)( unchanged)?\] \[(?<code>[a-z0-9-]+)\] (?<message>.*)$",
        RegexOptions.Compiled);

    /// <summary>The schema <c>record-invalid</c> lines of one <c>model check</c> run, without the model pass phrasing.</summary>
    /// <param name="result">The completed model check run.</param>
    /// <returns>The schema problem lines.</returns>
    static List<string> Problems(CliResult result)
    {
        var problems = new List<string>();

        foreach (var line in result.Output.Split('\n'))
        {
            var match = FindingLine.Match(line.TrimEnd('\r'));

            if (!match.Success || match.Groups["code"].Value != "record-invalid")
            {
                continue;
            }

            var message = match.Groups["message"].Value;

            if (message.StartsWith("Record '", StringComparison.Ordinal)
                || message.StartsWith("Screen '", StringComparison.Ordinal)
                || message.StartsWith("Term '", StringComparison.Ordinal)
                || message.StartsWith("Exception '", StringComparison.Ordinal))
            {
                continue;
            }

            problems.Add(match.Value);
        }

        return problems;
    }

    /// <summary>Asserts a clean permission scope: no schema problem. The exit code belongs to the whole check, so it is not asserted here.</summary>
    /// <param name="result">The completed model check run.</param>
    static void AssertClean(CliResult result)
    {
        var problems = Problems(result);
        Assert.Empty(problems);
    }

    /// <summary>Asserts a failing run: exit 1 with some problem carrying the expectation.</summary>
    /// <param name="result">The completed model check run.</param>
    /// <param name="expected">The text one problem line must contain.</param>
    static void AssertProblem(CliResult result, string expected)
    {
        var problems = Problems(result);
        Assert.Equal(1, result.Status);
        Assert.Contains(problems, problem => problem.Contains(expected, StringComparison.Ordinal));
    }

    static CliResult RunConsumer(string root)
    {
        string[] args = ["model", "check", "--root", root];
        return TestCli.Run(args);
    }

    static JsonObject FirstPermission(JsonObject meta) =>
        (JsonObject)((JsonArray)meta["records"]!)[0]!;

    /// <summary>Asserts a rejected record: exit 1 with at least one schema problem.</summary>
    /// <param name="result">The completed model check run.</param>
    static void AssertRejected(CliResult result)
    {
        Assert.Equal(1, result.Status);
        Assert.NotEmpty(Problems(result));
    }

    /// <summary>A permission entry without scope, with its group, passes.</summary>
    /// <proves>litespec/core/access#scope-every-role</proves>
    [Fact(DisplayName = "permission entry without scope, with its group")]
    public void PermissionEntryWithoutScopeWithItsGroup()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateMeta(
            folder.Path,
            ".litespec/behavior/permissions.yaml",
            meta => FirstPermission(meta)["group"] = "orders");
        AssertClean(RunConsumer(folder.Path));
    }

    /// <summary>A permission entry carrying scope is refused, because the role holds the scope.</summary>
    /// <proves>litespec/core/access#scope-every-role</proves>
    [Fact(DisplayName = "permission entry carrying scope")]
    public void PermissionEntryCarryingScope()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateMeta(
            folder.Path,
            ".litespec/behavior/permissions.yaml",
            meta => FirstPermission(meta)["scope"] = "self");
        AssertProblem(RunConsumer(folder.Path), "unknown property 'scope'");
    }

    /// <summary>Permission entry with an unknown property.</summary>
    /// <proves>litespec/core/access#scope-every-role</proves>
    [Fact(DisplayName = "permission entry with an unknown property")]
    public void PermissionEntryWithAnUnknownProperty()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateMeta(
            folder.Path,
            ".litespec/behavior/permissions.yaml",
            meta => FirstPermission(meta)["reach"] = "self");
        AssertProblem(RunConsumer(folder.Path), "unknown property 'reach'");
    }

    /// <summary>A role without its scope fails, because the scope is the role's only home.</summary>
    /// <proves>litespec/core/access#scope-every-role</proves>
    [Fact(DisplayName = "role without its scope")]
    public void RoleWithoutItsScope()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateMeta(
            folder.Path,
            ".litespec/behavior/roles/customer.yaml",
            meta => meta.Remove("scope"));
        AssertProblem(RunConsumer(folder.Path), "missing required property 'scope'");
    }

    /// <summary>An actor holds a role as an identifier and passes.</summary>
    /// <proves>litespec/core/access#record-every-role-an-actor-holds</proves>
    [Fact(DisplayName = "actor holds a role as an identifier")]
    public void ActorHoldsARoleAsAnIdentifier()
    {
        using var folder = ConsumerFixture.Create();
        AssertClean(RunConsumer(folder.Path));
    }

    /// <summary>An actor hold carrying scope is refused: the role holds the scope.</summary>
    /// <proves>litespec/core/access#record-every-role-an-actor-holds</proves>
    [Fact(DisplayName = "actor hold carrying scope")]
    public void ActorHoldCarryingScope()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateMeta(
            folder.Path,
            ".litespec/behavior/actors/customer.yaml",
            meta => meta["holds"] = new JsonArray(new JsonObject { ["role"] = "role/customer", ["scope"] = "self" }));
        AssertRejected(RunConsumer(folder.Path));
    }
}
