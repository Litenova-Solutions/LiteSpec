using System.Text.Json;
using LiteSpec.Cli.Hook;
using LiteSpec.Cli.Tests.Model;

namespace LiteSpec.Cli.Tests.Hook;

/// <summary>Proves <c>litespec hook post-edit</c> prints the judgment provisions and the open findings for the edited file.</summary>
public sealed class HookCommandTests
{
    /// <summary>The console gate: command runs write to the process console, so only one runs at once.</summary>
    static readonly object ConsoleGate = new();

    /// <summary>Post-edit with a sample PostToolUse payload prints the judgment provisions within the line cap.</summary>
    /// <proves>litespec/core/agent#load-applicable-provisions-before-editing</proves>
    [Fact]
    public void PostEditPrintsJudgmentProvisionsWithinTheLineCap()
    {
        using var temp = ModelFixture.Prepare("small");
        var handler = WriteHandler(temp.Path);
        var payload = JsonSerializer.Serialize(new { tool_input = new { file_path = handler } });

        var (status, stdout, _) = RunHook(["post-edit", "--root", temp.Path], new StringReader(payload));

        Assert.Equal(0, status);
        var lines = stdout.Split('\n').Select(line => line.TrimEnd('\r')).Where(line => line.Length > 0).ToList();
        Assert.True(lines.Count > 0, "post-edit prints the provisions for the edited file");
        Assert.True(lines.Count <= 40, $"post-edit prints at most 40 lines, printed {lines.Count}");
        Assert.Contains(lines, line => line.Contains("litespec/", StringComparison.Ordinal));
    }

    /// <summary>Post-edit with no file path stays silent and exits 0.</summary>
    /// <proves>litespec/core/agent#load-applicable-provisions-before-editing</proves>
    [Fact]
    public void PostEditWithNoFilePathStaysSilent()
    {
        using var temp = ModelFixture.Prepare("small");

        var (status, stdout, _) = RunHook(["post-edit", "--root", temp.Path], new StringReader("{}"));

        Assert.Equal(0, status);
        Assert.Equal("", stdout);
    }

    /// <summary>Post-edit --help prints the usage text, and an unknown hook or option is a usage error.</summary>
    /// <proves>litespec/core/agent#load-applicable-provisions-before-editing</proves>
    [Fact]
    public void HookHelpAndUsageErrors()
    {
        using var temp = ModelFixture.Prepare("small");

        var help = RunHook(["--help"], new StringReader(""));
        Assert.Equal(0, help.Status);
        Assert.Contains("litespec hook", help.Stdout, StringComparison.Ordinal);

        Assert.Equal(2, RunHook(["bogus"], new StringReader("")).Status);
        Assert.Equal(2, RunHook(["post-edit", "--bogus", "--root", temp.Path], new StringReader("{}")).Status);
    }

    /// <summary>Writes a command handler marking the register-copy use case, so the edited file has governing provisions.</summary>
    /// <param name="root">The prepared fixture folder.</param>
    /// <returns>The absolute handler path.</returns>
    static string WriteHandler(string root)
    {
        var folder = Path.Combine(root, "apps", "api", "src", "Modules", "Library.Lending", "Application", "Copies", "RegisterCopy");
        Directory.CreateDirectory(folder);
        var handler = Path.Combine(folder, "RegisterCopyCommandHandler.cs");
        File.WriteAllText(handler, "// <implements>use-case/lending.register-copy</implements>\npublic sealed class RegisterCopyCommandHandler { }\n");
        return handler;
    }

    /// <summary>Runs the hook command with the process console redirected, so the case reads the exit code and the output together.</summary>
    /// <param name="args">The command arguments.</param>
    /// <param name="input">The standard input text for post-edit.</param>
    /// <returns>The exit code and the two output streams of the run.</returns>
    static (int Status, string Stdout, string Stderr) RunHook(string[] args, TextReader input)
    {
        lock (ConsoleGate)
        {
            var stdout = new StringWriter();
            var stderr = new StringWriter();
            var previousOut = Console.Out;
            var previousError = Console.Error;
            Console.SetOut(stdout);
            Console.SetError(stderr);

            try
            {
                var status = HookCommand.Run(args, input);
                stdout.Flush();
                stderr.Flush();
                return (status, stdout.ToString(), stderr.ToString());
            }
            finally
            {
                Console.SetOut(previousOut);
                Console.SetError(previousError);
            }
        }
    }
}
