using System.Text.Json.Nodes;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Consumer;

/// <summary>
/// Proves <c>litespec consumer</c> enforces the permission scope: an entry with
/// <c>scope</c> passes, one without fails with the schema error, and an entry
/// with an unknown property still fails. Each test owns its own folder because
/// tests run in parallel.
/// </summary>
public sealed class PermissionScopeTests
{
    /// <summary>The output lines that start with <c>  - </c>, without the prefix.</summary>
    static List<string> Problems(CliResult result) => result.Output
        .Split('\n')
        .Where(line => line.StartsWith("  - ", StringComparison.Ordinal))
        .Select(line => line[4..])
        .ToList();

    /// <summary>Asserts a clean run: no problems and exit 0.</summary>
    static void AssertClean(CliResult result)
    {
        var problems = Problems(result);
        Assert.Equal(0, result.Status);
        Assert.Empty(problems);
    }

    /// <summary>Asserts a failing run: exit 1 with some problem carrying the expectation.</summary>
    static void AssertProblem(CliResult result, string expected)
    {
        var problems = Problems(result);
        Assert.Equal(1, result.Status);
        Assert.Contains(problems, problem => problem.Contains(expected, StringComparison.Ordinal));
    }

    static CliResult RunConsumer(string root)
    {
        string[] args = ["consumer", "--root", root];
        return Cli.Run(args);
    }

    static JsonObject FirstPermission(JsonObject meta) =>
        (JsonObject)((JsonArray)meta["records"]!)[0]!;

    [Fact(DisplayName = "permission entry with its scope")]
    public void PermissionEntryWithItsScope()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateMeta(
            folder.Path,
            "docs/behavior/permissions.md",
            meta => FirstPermission(meta)["scope"] = "self");
        AssertClean(RunConsumer(folder.Path));
    }

    [Fact(DisplayName = "permission entry with its scope and group")]
    public void PermissionEntryWithItsScopeAndGroup()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateMeta(
            folder.Path,
            "docs/behavior/permissions.md",
            meta =>
            {
                FirstPermission(meta)["scope"] = "self";
                FirstPermission(meta)["group"] = "orders";
            });
        AssertClean(RunConsumer(folder.Path));
    }

    [Fact(DisplayName = "permission entry without its scope")]
    public void PermissionEntryWithoutItsScope()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateMeta(
            folder.Path,
            "docs/behavior/permissions.md",
            meta => FirstPermission(meta).Remove("scope"));
        AssertProblem(RunConsumer(folder.Path), "missing required property 'scope'");
    }

    [Fact(DisplayName = "permission entry with an unknown property")]
    public void PermissionEntryWithAnUnknownProperty()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateMeta(
            folder.Path,
            "docs/behavior/permissions.md",
            meta =>
            {
                FirstPermission(meta)["scope"] = "self";
                FirstPermission(meta)["reach"] = "self";
            });
        AssertProblem(RunConsumer(folder.Path), "unknown property 'reach'");
    }
}
