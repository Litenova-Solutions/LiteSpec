using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Consumer;

/// <summary>
/// Proves the consumer pass inside <c>litespec model check</c> against the fixture
/// consumer, asserting the expected diagnostics. Each test owns its own folder
/// because tests run in parallel.
/// </summary>
public sealed partial class ConsumerValidatorTests
{
    /// <summary>One <c>model check</c> finding line: its path, line, level, code and message.</summary>
    static readonly Regex FindingLine = new(
        @"^(?<path>.+?):(?<line>\d+) \[(?<level>G|S|A)( unchanged)?\] \[(?<code>[a-z0-9-]+)\] (?<message>.*)$",
        RegexOptions.Compiled);

    /// <summary>
    ///     The finding codes the consumer pass reports: the project, page, glossary and
    ///     frontend codes, the record code it shares with the model pass, and the
    ///     controlled UI codes it carries in.
    /// </summary>
    static readonly HashSet<string> ConsumerCodes = new(StringComparer.Ordinal)
    {
        "project-invalid", "project-path-invalid", "release-drift", "stack-invalid", "extension-invalid",
        "stage-deferral-invalid", "page-invalid", "page-section-missing", "page-heading-invalid",
        "page-example-invalid", "glossary-invalid", "frontend-invalid", "record-invalid",
        "ui-package-invalid", "ui-baseline-missing", "ui-frontend-invalid",
        "ui-components-invalid", "ui-sourcelock-invalid", "ui-design-contract-invalid",
        "ui-stylesheet-invalid", "ui-class-invalid", "ui-import-invalid", "ui-dependency-invalid",
        "ui-catalog-invalid", "ui-route-suite-missing",
    };

    /// <summary>The consumer pass finding lines of one <c>model check</c> run.</summary>
    /// <param name="result">The completed model check run.</param>
    /// <returns>The finding lines the consumer pass reports.</returns>
    static List<string> Problems(CliResult result)
    {
        var problems = new List<string>();

        foreach (var line in result.Output.Split('\n'))
        {
            var match = FindingLine.Match(line.TrimEnd('\r'));

            if (!match.Success || !ConsumerCodes.Contains(match.Groups["code"].Value))
            {
                continue;
            }

            // record-invalid is shared: the model pass phrases its own record
            // findings as "Record '...'", so only the schema messages belong here.
            if (match.Groups["code"].Value == "record-invalid"
                && IsModelRecordMessage(match.Groups["message"].Value))
            {
                continue;
            }

            problems.Add(match.Value);
        }

        return problems;
    }

    /// <summary>Whether a record-invalid message is phrased by the model pass.</summary>
    /// <param name="message">The finding message to judge.</param>
    /// <returns>True when the message opens with a model record subject.</returns>
    static bool IsModelRecordMessage(string message) =>
        message.StartsWith("Record '", StringComparison.Ordinal)
        || message.StartsWith("Screen '", StringComparison.Ordinal)
        || message.StartsWith("Term '", StringComparison.Ordinal)
        || message.StartsWith("Exception '", StringComparison.Ordinal);

    /// <summary>Asserts a clean consumer pass: no consumer problem. The exit code belongs to the whole check, so it is not asserted here.</summary>
    /// <param name="result">The completed model check run.</param>
    static void AssertClean(CliResult result)
    {
        var problems = Problems(result);
        Assert.True(problems.Count == 0, result.Output);
    }

    /// <summary>Asserts a failing run: exit 1 with some consumer problem carrying the expectation. Every consumer finding blocks, so the exit follows.</summary>
    /// <param name="result">The completed model check run.</param>
    /// <param name="expected">The text one problem line must contain.</param>
    static void AssertProblem(CliResult result, string expected)
    {
        var problems = Problems(result);
        Assert.Equal(1, result.Status);
        Assert.Contains(problems, problem => problem.Contains(expected, StringComparison.Ordinal));
    }

    /// <summary>Asserts no consumer problem names the diagnostic.</summary>
    /// <param name="result">The completed model check run.</param>
    /// <param name="absent">The diagnostic text no problem line may contain.</param>
    static void AssertAbsent(CliResult result, string absent)
    {
        var problems = Problems(result);
        Assert.All(problems, problem => Assert.DoesNotContain(absent, problem));
    }

    static CliResult RunConsumer(string root, params string[] extra)
    {
        string[] args = ["model", "check", "--root", root, ..extra];
        return Cli.Run(args);
    }

    static CliResult RunConsumer(string root, TempFolder home, params string[] extra)
    {
        string[] args = ["model", "check", "--root", root, ..extra];
        return Cli.Run(args, environment: new Dictionary<string, string> { ["LITESPEC_HOME"] = home.Path });
    }

    static JsonObject PathsOf(JsonObject project) => (JsonObject)project["paths"]!;

    /// <summary>Fixture validates as written.</summary>
    /// <proves>litespec/core/authoring#declare-structured-specification-metadata</proves>
    [Fact(DisplayName = "fixture validates as written")]
    public void FixtureValidatesAsWritten()
    {
        using var folder = ConsumerFixture.Create();
        AssertClean(RunConsumer(folder.Path));
    }

    /// <summary>A template placeholder left in the project file.</summary>
    /// <proves>litespec/core/scope#select-a-size-a-shape-and-stacks</proves>
    [Fact(DisplayName = "a template placeholder left in the project file")]
    public void TemplatePlaceholderLeftInTheProjectFile()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateProject(folder.Path, project => ((JsonObject)project["project"]!)["name"] = "__PROJECT__");
        AssertProblem(RunConsumer(folder.Path), "template placeholder '__PROJECT__' is not filled");
    }

    /// <summary>Size outside the allowed sizes.</summary>
    /// <proves>litespec/core/scope#select-a-size-a-shape-and-stacks</proves>
    [Fact(DisplayName = "size outside the allowed sizes")]
    public void SizeOutsideTheAllowedSizes()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateProject(folder.Path, project => project["size"] = "colossal");
        AssertProblem(RunConsumer(folder.Path), "/size");
    }

    /// <summary>Reviewed version outside its form.</summary>
    /// <proves>litespec/core/authoring#record-the-reviewed-litespec-release</proves>
    [Fact(DisplayName = "reviewed version outside its form")]
    public void ReviewedVersionOutsideItsForm()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateProject(folder.Path, project => project["reviewedLiteSpecVersion"] = "v3");
        AssertProblem(RunConsumer(folder.Path), "/reviewedLiteSpecVersion");
    }

    /// <summary>Selected extensions outside its array form.</summary>
    /// <proves>litespec/core/scope#record-selected-extensions</proves>
    [Fact(DisplayName = "selected extensions outside its array form")]
    public void SelectedExtensionsOutsideItsArrayForm()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateProject(folder.Path, project => project["selectedExtensions"] = "outbox");
        AssertProblem(RunConsumer(folder.Path), "/selectedExtensions");
    }

    /// <summary>Frontend platform outside the stacks.</summary>
    /// <proves>litespec/core/scope#select-a-size-a-shape-and-stacks</proves>
    [Fact(DisplayName = "frontend platform outside the stacks")]
    public void FrontendPlatformOutsideTheStacks()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateProject(folder.Path, project =>
        {
            PathsOf(project)["frontends"] = new JsonArray(
                new JsonObject { ["name"] = "web", ["path"] = "apps/web", ["platform"] = "winforms" });
        });
        AssertProblem(RunConsumer(folder.Path), "/paths/frontends/0/platform");
    }

    /// <summary>Reviewed version behind the installed release.</summary>
    /// <proves>litespec/core/authoring#record-the-reviewed-litespec-release</proves>
    [Fact(DisplayName = "reviewed version behind the installed release")]
    public void ReviewedVersionBehindTheInstalledRelease()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateProject(folder.Path, project => project["reviewedLiteSpecVersion"] = "0.0.0");
        var result = RunConsumer(folder.Path);
        AssertProblem(result, "does not match the installed LiteSpec tool release");
        AssertProblem(result, "dotnet tool update LiteSpec.Cli --version 0.0.0");
    }

    /// <summary>Extension the manifest does not ship.</summary>
    /// <proves>litespec/core/scope#record-selected-extensions</proves>
    [Fact(DisplayName = "extension the manifest does not ship")]
    public void ExtensionTheManifestDoesNotShip()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateProject(folder.Path, project =>
        {
            project["selectedExtensions"] = new JsonArray(
                new JsonObject
                {
                    ["id"] = "no-such-extension",
                    ["criterion"] = "Nothing needs it.",
                });
        });
        AssertProblem(RunConsumer(folder.Path), "is not an extension in the pinned LiteSpec release");
    }

    /// <summary>Known extension with its criterion.</summary>
    /// <proves>litespec/core/scope#record-selected-extensions</proves>
    [Fact(DisplayName = "known extension with its criterion")]
    public void KnownExtensionWithItsCriterion()
    {
        using var folder = ConsumerFixture.Create();
        ConsumerFixture.MutateProject(folder.Path, project =>
        {
            project["selectedExtensions"] = new JsonArray(
                new JsonObject
                {
                    ["id"] = ConsumerFixture.KnownExtension,
                    ["criterion"] = "The suite needs one.",
                });
        });
        AssertClean(RunConsumer(folder.Path));
    }
}
