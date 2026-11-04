using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Consumer;

/// <summary>
/// Proves <c>litespec model check</c> enforces the permission scope: an entry with
/// <c>scope</c> passes, one without fails with the schema error, and an entry
/// with an unknown property still fails. Each test owns its own folder because
/// tests run in parallel.
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
        return Cli.Run(args);
    }

    static JsonObject FirstPermission(JsonObject meta) =>
        (JsonObject)((JsonArray)meta["records"]!)[0]!;

    /// <summary>Permission entry with its scope.</summary>
    /// <proves>litespec/core/access#state-the-scope-of-every-permission</proves>
    [Fact(DisplayName = "permission entry with its scope")]
    public void PermissionEntryWithItsScope()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateMeta(
            folder.Path,
            ".litespec/behavior/permissions.yaml",
            meta => FirstPermission(meta)["scope"] = "self");
        AssertClean(RunConsumer(folder.Path));
    }

    /// <summary>Permission entry with its scope and group.</summary>
    /// <proves>litespec/core/access#state-the-scope-of-every-permission</proves>
    [Fact(DisplayName = "permission entry with its scope and group")]
    public void PermissionEntryWithItsScopeAndGroup()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateMeta(
            folder.Path,
            ".litespec/behavior/permissions.yaml",
            meta =>
            {
                FirstPermission(meta)["scope"] = "self";
                FirstPermission(meta)["group"] = "orders";
            });
        AssertClean(RunConsumer(folder.Path));
    }

    /// <summary>Permission entry without its scope.</summary>
    /// <proves>litespec/core/access#state-the-scope-of-every-permission</proves>
    [Fact(DisplayName = "permission entry without its scope")]
    public void PermissionEntryWithoutItsScope()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateMeta(
            folder.Path,
            ".litespec/behavior/permissions.yaml",
            meta => FirstPermission(meta).Remove("scope"));
        AssertProblem(RunConsumer(folder.Path), "missing required property 'scope'");
    }

    /// <summary>Permission entry with an unknown property.</summary>
    /// <proves>litespec/core/access#state-the-scope-of-every-permission</proves>
    [Fact(DisplayName = "permission entry with an unknown property")]
    public void PermissionEntryWithAnUnknownProperty()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateMeta(
            folder.Path,
            ".litespec/behavior/permissions.yaml",
            meta =>
            {
                FirstPermission(meta)["scope"] = "self";
                FirstPermission(meta)["reach"] = "self";
            });
        AssertProblem(RunConsumer(folder.Path), "unknown property 'reach'");
    }
}
