using System.Text.Json.Nodes;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests;

/// <summary>Proves the shared argument parser and the last-resort failure handler.</summary>
public sealed class CliArgsTests
{
    /// <summary>A repeated option is a usage error in both spellings.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact]
    public void Repeated_Option_Is_A_Usage_Error()
    {
        var spaced = CliArgs.Parse(["--root", "a", "--root", "b"], [], ["--root"]);
        Assert.NotNull(spaced.Error);
        Assert.Contains("--root", spaced.Error, StringComparison.Ordinal);

        var equals = CliArgs.Parse(["--root=a", "--root=b"], [], ["--root"]);
        Assert.NotNull(equals.Error);
        Assert.Contains("--root", equals.Error, StringComparison.Ordinal);
    }

    /// <summary>The repeatable <c>--batch</c> option of <c>model approve</c> stays repeatable.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact]
    public void Batch_Option_May_Repeat()
    {
        var parsed = CliArgs.Parse(["--batch", "a", "--batch", "b"], [], ["--batch"]);

        Assert.Null(parsed.Error);
    }

    /// <summary>An empty value for --root, --model, --out or --editor is a usage error in both spellings.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact]
    public void Empty_Value_Is_A_Usage_Error()
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
    public void Usage_Error_Comes_Before_Help()
    {
        var parsed = CliArgs.Parse(["--bogus", "--help"], [], ["--root"]);

        Assert.NotNull(parsed.Error);
        Assert.False(parsed.Help);
    }

    /// <summary>A clean --help still reports help and no error.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact]
    public void Help_Without_An_Error_Still_Works()
    {
        var parsed = CliArgs.Parse(["--help"], [], ["--root"]);

        Assert.Null(parsed.Error);
        Assert.True(parsed.Help);
    }

    /// <summary>The consumer command reports a bogus option before --help and exits 2.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact]
    public void Consumer_Bogus_Option_With_Help_Exits_2()
    {
        var result = Cli.Run("consumer", "--bogus", "--help");

        Assert.Equal(2, result.Status);
        Assert.Contains("--bogus", result.Output, StringComparison.Ordinal);
    }

    /// <summary>The authoring commands accept only the --root spelling, not --home.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact]
    public void Home_Is_Not_An_Option()
    {
        using var temp = Cli.Temp("cli-args-home");

        var created = Cli.Run("new", "topic", "spending.example", "--home", temp.Path);
        Assert.Equal(2, created.Status);
        Assert.Contains("--home", created.Output, StringComparison.Ordinal);

        var renamed = Cli.Run(
            "rename", "topic/spending.example", "topic/spending.other", "--home", temp.Path);
        Assert.Equal(2, renamed.Status);
        Assert.Contains("--home", renamed.Output, StringComparison.Ordinal);
    }

    /// <summary>An unexpected exception becomes one Failed line on standard error and exit code 1.</summary>
    /// <proves>litespec/core/authoring#run-repeatable-authoring-checks</proves>
    [Fact]
    public void LastResort_Writes_One_Failed_Line_Without_A_Stack_Trace()
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
    public void LastResort_With_Json_Writes_An_Error_Object()
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
    public void LastResort_Detects_Json_Format()
    {
        Assert.True(LastResort.WantsJson(["consumer", "--format", "json"]));
        Assert.True(LastResort.WantsJson(["consumer", "--format=json"]));
        Assert.False(LastResort.WantsJson(["consumer", "--format", "text"]));
        Assert.False(LastResort.WantsJson(["consumer"]));
    }
}
