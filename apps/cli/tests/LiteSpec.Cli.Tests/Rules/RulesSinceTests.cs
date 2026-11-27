using System.Diagnostics;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Rules;

/// <summary>Proves <c>litespec rules --since</c> prints one report skeleton over the changed files.</summary>
public sealed class RulesSinceTests
{
    /// <summary>A two-file change prints one skeleton with an empty How judged line each.</summary>
    /// <proves>litespec/core/agent#load-applicable-provisions-before-editing</proves>
    [Fact]
    public void SinceTwoFileChangePrintsOneSkeleton()
    {
        using var root = CopyFixture("rules-basic", "rules-since");
        CommitAll(root.Path);
        Touch(root.Path, "apps/api/src/Demo/Billing/Invoicing/SendInvoice/SendInvoiceCommandHandler.cs");
        Touch(root.Path, "apps/api/src/Demo/Billing/Invoicing/SendInvoice/SendInvoiceEndpoint.cs");
        var result = TestCli.Run("rules", "--root", root.Path, "--since", "HEAD");

        Assert.Equal(0, result.Status);
        Assert.Contains("Rules since HEAD (2 files, ", result.Output);
        Assert.Contains(" judgment provisions):", result.Output);
        Assert.Contains("How judged:", result.Output);
        Assert.DoesNotContain("Checked by a tool:", result.Output);
    }

    /// <summary>The union lists a provision shared by two files once.</summary>
    /// <proves>litespec/core/agent#load-applicable-provisions-before-editing</proves>
    [Fact]
    public void SinceUnionRemovesDuplicates()
    {
        using var root = CopyFixture("rules-basic", "rules-since");
        CommitAll(root.Path);
        Touch(root.Path, "apps/api/src/Demo/Billing/Invoicing/SendInvoice/SendInvoiceCommandHandler.cs");
        Touch(root.Path, "apps/api/src/Demo/Billing/Invoicing/SendInvoice/SendInvoiceEndpoint.cs");
        var result = TestCli.Run("rules", "--root", root.Path, "--since", "HEAD");

        Assert.Equal(0, result.Status);
        Assert.Equal(1, Occurrences(result.Output, "litespec/core/backend-api#keep-one-name-for-a-use-case-across-layers"));
    }

    /// <summary>Every skeleton line carries its file count, most governed first, and a provision both files share governs two files.</summary>
    /// <proves>litespec/core/agent#load-applicable-provisions-before-editing</proves>
    [Fact]
    public void SinceSkeletonCarriesFileCountsMostGovernedFirst()
    {
        using var root = CopyFixture("rules-basic", "rules-since");
        CommitAll(root.Path);
        Touch(root.Path, "apps/api/src/Demo/Billing/Invoicing/SendInvoice/SendInvoiceCommandHandler.cs");
        Touch(root.Path, "apps/api/src/Demo/Billing/Invoicing/SendInvoice/SendInvoiceEndpoint.cs");
        var result = TestCli.Run("rules", "--root", root.Path, "--since", "HEAD");

        Assert.Equal(0, result.Status);

        var lines = result.Stdout
            .Replace("\r\n", "\n", StringComparison.Ordinal)
            .Split('\n')
            .Where(line => line.StartsWith("- ", StringComparison.Ordinal))
            .ToList();

        Assert.NotEmpty(lines);
        Assert.All(lines, line => Assert.Contains(" file", line, StringComparison.Ordinal));

        var counts = lines.Select(FileCountOf).ToList();

        for (var index = 1; index < counts.Count; index++)
        {
            Assert.True(counts[index] <= counts[index - 1], "The skeleton orders by governing file count, most first.");
        }

        Assert.Contains(lines, line => line.StartsWith("- litespec/core/backend-api#keep-one-name-for-a-use-case-across-layers (2 files): ", StringComparison.Ordinal));
    }

    /// <summary>A convention without a verification line asks its Default sentence in the skeleton.</summary>
    /// <proves>litespec/core/agent#load-applicable-provisions-before-editing</proves>
    [Fact]
    public void SinceConventionWithoutVerificationAsksItsDefault()
    {
        using var root = CopyFixture("rules-basic", "rules-since");
        WriteUnverifiedConvention(root.Path);
        CommitAll(root.Path);
        Touch(root.Path, "apps/api/src/Demo/Billing/Invoicing/SendInvoice/SendInvoiceCommandHandler.cs");
        var result = TestCli.Run("rules", "--root", root.Path, "--since", "HEAD");

        Assert.Equal(0, result.Status);
        Assert.Contains("- demo/conventions#unstated-verification (1 file): Every invoice render keeps one layout.", result.Output);
        Assert.Contains("  How judged:", result.Output);
    }

    /// <summary>A revision naming no commit exits 2.</summary>
    /// <proves>litespec/core/agent#load-applicable-provisions-before-editing</proves>
    [Fact]
    public void SinceUnknownRefExits2()
    {
        using var root = CopyFixture("rules-basic", "rules-since");
        CommitAll(root.Path);
        var result = TestCli.Run("rules", "--root", root.Path, "--since", "no-such-ref");

        Assert.Equal(2, result.Status);
        Assert.Contains("names no commit", result.Output);
    }

    /// <summary>A positional beside --since is a usage error.</summary>
    /// <proves>litespec/core/language#name-commands-as-verbs-and-flags-by-what-they-set</proves>
    [Fact]
    public void SinceWithPositionalIsUsageError()
    {
        var result = TestCli.Run(
            "rules",
            "--root", TestCli.Fixture("rules-basic"),
            "--since", "HEAD",
            "apps/api/src/Demo/Billing/Invoicing/SendInvoice/SendInvoiceCommandHandler.cs");

        Assert.Equal(2, result.Status);
        Assert.Contains("--since takes no path or record id", result.Output);
    }

    /// <summary>How many times the text holds the value.</summary>
    /// <param name="text">The text to search.</param>
    /// <param name="value">The value to count.</param>
    /// <returns>The count of non-overlapping occurrences.</returns>
    static int Occurrences(string text, string value) =>
        text.Split(value, StringSplitOptions.None).Length - 1;

    /// <summary>The file count a skeleton line carries, from its <c>(N file)</c> or <c>(N files)</c> span.</summary>
    /// <param name="line">The skeleton line.</param>
    /// <returns>The governing file count.</returns>
    static int FileCountOf(string line)
    {
        var open = line.IndexOf(" (", StringComparison.Ordinal);
        var close = line.IndexOf(" file", StringComparison.Ordinal);
        return int.Parse(line.AsSpan(open + 2, close - open - 2), System.Globalization.CultureInfo.InvariantCulture);
    }

    /// <summary>Appends a comment line to the fixture file, so git sees it changed.</summary>
    /// <param name="root">The temporary consumer root.</param>
    /// <param name="relative">The file path with forward-slash segments.</param>
    static void Touch(string root, string relative)
    {
        var file = Path.Combine(root, relative.Replace('/', Path.DirectorySeparatorChar));
        File.AppendAllText(file, "\n// change\n");
    }

    /// <summary>Writes one convention without a verification line into the fixture.</summary>
    /// <param name="root">The temporary consumer root.</param>
    static void WriteUnverifiedConvention(string root)
    {
        var file = Path.Combine(root, ".litespec", "governance", "conventions", "unverified.md");
        Directory.CreateDirectory(Path.GetDirectoryName(file)!);
        File.WriteAllText(file, """
            # Keep One Invoice Layout

            **ID:** `demo/conventions#unstated-verification`

            **Strength:** default

            **Applies to:** `apps/api/src/**`

            **Default:** Every invoice render keeps one layout.

            **Rationale:** One layout keeps invoices comparable.
            """);
    }

    /// <summary>Copies one fixture to a temporary root.</summary>
    /// <param name="fixture">The fixture name.</param>
    /// <param name="prefix">The prefix in the temporary folder name.</param>
    /// <returns>The temporary consumer root.</returns>
    static TempFolder CopyFixture(string fixture, string prefix)
    {
        var source = TestCli.Fixture(fixture);
        var temp = TestCli.Temp(prefix);

        foreach (var file in Directory.GetFiles(source, "*", SearchOption.AllDirectories))
        {
            var target = Path.Combine(temp.Path, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, overwrite: true);
        }

        return temp;
    }

    /// <summary>Runs git in the folder and returns its standard output, throwing when it fails.</summary>
    /// <param name="workdir">The folder to run git in.</param>
    /// <param name="args">The git arguments.</param>
    /// <returns>The git standard output.</returns>
    static string RunGit(string workdir, params string[] args)
    {
        var start = new ProcessStartInfo("git")
        {
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            WorkingDirectory = workdir,
        };

        foreach (var arg in args)
        {
            start.ArgumentList.Add(arg);
        }

        using var process = Process.Start(start)!;
        var stdout = process.StandardOutput.ReadToEndAsync();
        var stderr = process.StandardError.ReadToEndAsync();

        if (!process.WaitForExit(TimeSpan.FromMinutes(2)))
        {
            process.Kill(entireProcessTree: true);
            throw new TimeoutException($"git {string.Join(' ', args)} did not finish in two minutes.");
        }

        if (process.ExitCode != 0)
        {
            throw new InvalidOperationException($"git {string.Join(' ', args)} failed: {stderr.Result}");
        }

        return stdout.Result;
    }

    /// <summary>Commits the whole fixture so the index starts clean.</summary>
    /// <param name="path">The fixture folder to commit.</param>
    static void CommitAll(string path)
    {
        RunGit(path, "-c", "init.defaultBranch=main", "init");
        RunGit(path, "add", "-A");
        RunGit(
            path, "-c", "user.name=Test", "-c", "user.email=test@example.com",
            "-c", "commit.gpgsign=false", "commit", "-m", "init");
    }
}
