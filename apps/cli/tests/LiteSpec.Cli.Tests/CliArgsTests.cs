using System.Text.Json.Nodes;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.CommandLine;

namespace LiteSpec.Cli.Tests;

/// <summary>Proves the shared argument parser and the last-resort failure handler.</summary>
public sealed class CliArgsTests
{
    /// <summary>A repeated option is a usage error in both spellings.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact]
    public void RepeatedOptionIsAUsageError()
    {
        var spaced = CliArgs.Parse(["--root", "a", "--root", "b"], [], ["--root"]);
        Assert.NotNull(spaced.Error);
        Assert.Contains("--root", spaced.Error, StringComparison.Ordinal);

        var equals = CliArgs.Parse(["--root=a", "--root=b"], [], ["--root"]);
        Assert.NotNull(equals.Error);
        Assert.Contains("--root", equals.Error, StringComparison.Ordinal);
    }

    /// <summary>An empty value for --root, --model, --out or --editor is a usage error in both spellings.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact]
    public void EmptyValueIsAUsageError()
    {
        foreach (var option in new[] { "--root", "--model", "--out", "--editor" })
        {
            var equals = CliArgs.Parse([$"{option}="], [], [option]);
            Assert.True(equals.Error is not null, $"{option}= was accepted.");
            Assert.Contains(option, equals.Error, StringComparison.Ordinal);

            var spaced = CliArgs.Parse([option, ""], [], [option]);
            Assert.True(spaced.Error is not null, $"{option} with an empty value was accepted.");
            Assert.Contains(option, spaced.Error, StringComparison.Ordinal);
        }
    }

    /// <summary>A usage error is reported before --help is honoured.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact]
    public void UsageErrorComesBeforeHelp()
    {
        var parsed = CliArgs.Parse(["--bogus", "--help"], [], ["--root"]);

        Assert.NotNull(parsed.Error);
        Assert.False(parsed.Help);
    }

    /// <summary>A clean --help still reports help and no error.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact]
    public void HelpWithoutAnErrorStillWorks()
    {
        var parsed = CliArgs.Parse(["--help"], [], ["--root"]);

        Assert.Null(parsed.Error);
        Assert.True(parsed.Help);
    }

    /// <summary>The model check command reports a bogus option before --help and exits 2.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact]
    public void ModelCheckBogusOptionWithHelpExits2()
    {
        var result = TestCli.Run("model", "check", "--bogus", "--help");

        Assert.Equal(2, result.Status);
        Assert.Contains("--bogus", result.Output, StringComparison.Ordinal);
    }

    /// <summary>The authoring commands accept only the --root spelling, not --home.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact]
    public void HomeIsNotAnOption()
    {
        using var temp = TestCli.Temp("cli-args-home");

        var created = TestCli.Run("new", "topic", "spending.example", "--home", temp.Path);
        Assert.Equal(2, created.Status);
        Assert.Contains("--home", created.Output, StringComparison.Ordinal);

        var renamed = TestCli.Run(
            "rename", "topic/spending.example", "topic/spending.other", "--home", temp.Path);
        Assert.Equal(2, renamed.Status);
        Assert.Contains("--home", renamed.Output, StringComparison.Ordinal);
    }

    /// <summary>An unexpected exception becomes one Failed line on standard error and exit code 1.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact]
    public void LastResortWritesOneFailedLineWithoutAStackTrace()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var code = LastResort.Run(
            () => throw new InvalidOperationException("boom"),
            json: false,
            stdout,
            stderr);

        Assert.Equal(1, code);
        Assert.Equal("Failed: boom", stderr.ToString().Trim());
        Assert.Equal("", stdout.ToString());
        Assert.DoesNotContain("InvalidOperationException", stderr.ToString(), StringComparison.Ordinal);
        Assert.DoesNotContain(" at ", stderr.ToString(), StringComparison.Ordinal);
    }

    /// <summary>With JSON format an unexpected exception becomes one error object on standard output.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact]
    public void LastResortWithJsonWritesAnErrorObject()
    {
        var stdout = new StringWriter();
        var stderr = new StringWriter();

        var code = LastResort.Run(
            () => throw new InvalidOperationException("boom"),
            json: true,
            stdout,
            stderr);

        Assert.Equal(1, code);
        Assert.Equal("", stderr.ToString());
        var error = JsonNode.Parse(stdout.ToString())!.AsObject();
        Assert.Equal("boom", error["error"]!.GetValue<string>());
    }

    /// <summary>JSON format is detected in both spellings.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact]
    public void LastResortDetectsJsonFormat()
    {
        Assert.True(LastResort.WantsJson(["model", "--format", "json"]));
        Assert.True(LastResort.WantsJson(["model", "--format=json"]));
        Assert.False(LastResort.WantsJson(["model", "--format", "text"]));
        Assert.False(LastResort.WantsJson(["model"]));
    }

    /// <summary>Under JSON format a usage error writes one error object on standard output and nothing on standard error.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact]
    public void ModelCheckUsageErrorUnderJsonWritesOneErrorObject()
    {
        var result = TestCli.Run("model", "check", "--format", "json", "--bogus");

        Assert.Equal(2, result.Status);
        Assert.Equal("", result.Stderr);
        var error = JsonNode.Parse(result.Stdout)!.AsObject();
        Assert.Contains("--bogus", error["error"]!.GetValue<string>(), StringComparison.Ordinal);
    }

    /// <summary>Under JSON format a root without a project file writes the same one error object.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact]
    public void ModelCheckUnreadableProjectUnderJsonWritesOneErrorObject()
    {
        using var temp = TestCli.Temp("model-json-error");
        var result = TestCli.Run("model", "check", "--root", temp.Path, "--format", "json");

        Assert.Equal(2, result.Status);
        Assert.Equal("", result.Stderr);
        var error = JsonNode.Parse(result.Stdout)!.AsObject();
        Assert.Contains("holds no .litespec/project.yaml", error["error"]!.GetValue<string>(), StringComparison.Ordinal);
    }
}
