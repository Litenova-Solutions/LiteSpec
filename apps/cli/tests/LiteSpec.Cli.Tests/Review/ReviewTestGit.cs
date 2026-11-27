using System.Diagnostics;

namespace LiteSpec.Cli.Tests.Review;

/// <summary>Runs git for the review command tests: each case builds a temporary repository with one changed file.</summary>
static class ReviewTestGit
{
    /// <summary>Starts a git repository in the folder with a local author, so commits need no global configuration.</summary>
    /// <param name="root">The temporary folder.</param>
    public static void Init(string root)
    {
        Run(root, "init", "--quiet");
        Run(root, "config", "user.email", "review@example.com");
        Run(root, "config", "user.name", "Review Test");
    }

    /// <summary>Commits every file in the folder.</summary>
    /// <param name="root">The temporary repository.</param>
    /// <param name="message">The commit message.</param>
    public static void CommitAll(string root, string message)
    {
        Run(root, "add", "-A");
        Run(root, "commit", "--quiet", "-m", message);
    }

    /// <summary>Runs one git command and fails the case when git reports an error.</summary>
    /// <param name="root">The folder git runs in.</param>
    /// <param name="args">The git arguments, in order.</param>
    public static void Run(string root, params string[] args)
    {
        var start = new ProcessStartInfo("git")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = root,
        };

        foreach (var arg in args)
        {
            start.ArgumentList.Add(arg);
        }

        using var process = Process.Start(start)!;
        process.StandardOutput.ReadToEnd();
        var stderr = process.StandardError.ReadToEnd();
        process.WaitForExit();

        Assert.Equal(0, process.ExitCode);
    }
}
