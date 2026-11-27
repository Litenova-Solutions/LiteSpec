using LiteSpec.Cli.Scaffold;
using LiteSpec.Cli.Tests.Model;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Scaffold;

/// <summary>Proves <c>litespec scaffold</c> writes one use case as normal, single-file C# code.</summary>
public sealed class ScaffoldCommandTests
{
    /// <summary>Scaffold writes the ten borrow-copy files and names the aggregate act it expects.</summary>
    [Fact]
    public void ScaffoldWritesCommandFilesFromTheRecord()
    {
        using var temp = ModelFixture.Prepare("small");
        RemoveStaleScaffoldOutputs(temp.Path);
        var result = Run("use-case/lending.borrow-copy", "--root", temp.Path);

        Assert.Equal(0, result.Status);
        Assert.Contains("Expects aggregate act Loan.Borrow for use-case/lending.borrow-copy.", result.Stdout);

        var expected = AppFiles("Loans", "BorrowCopy", "BorrowCopyCommand")
            .Concat(WebFiles("Loans", "BorrowCopy"))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToList();
        var actual = expected
            .Select(relative => Path.Combine(temp.Path, relative))
            .ToList();

        Assert.Equal(10, expected.Count);

        foreach (var file in actual)
        {
            Assert.True(File.Exists(file), $"scaffolded file {file}");
            Assert.Contains("/// <implements>use-case/lending.borrow-copy</implements>", File.ReadAllText(file));
        }

        var message = Read(temp.Path, expected, "BorrowCopyCommand.cs");
        Assert.Contains("internal sealed record BorrowCopyCommand(", message);
        Assert.Contains("Guid CopyId", message);
        Assert.Contains("ReaderId", message);

        var handler = Read(temp.Path, expected, "BorrowCopyCommandHandler.cs");
        Assert.Contains("await _loanRepository.GetByIdAsync(command.CopyId, cancellationToken)", handler);
        Assert.Contains("loan.Borrow();", handler);
        Assert.Contains("_loanRepository.Store(loan);", handler);
        Assert.Contains("new BorrowCopyResult(loan.Id)", handler);

        var validator = Read(temp.Path, expected, "BorrowCopyCommandValidator.cs");
        Assert.Contains("command.CopyId == Guid.Empty", validator);

        var authorization = Read(temp.Path, expected, "BorrowCopyCommandAuthorizationDefinition.cs");
        Assert.Contains("AccessPolicies.Permission(\"borrow-copy\")", authorization);

        var tenancy = Read(temp.Path, expected, "BorrowCopyCommandTenancyDefinition.cs");
        Assert.Contains("TenancyDeclaration.Required()", tenancy);

        var idempotency = Read(temp.Path, expected, "BorrowCopyCommandIdempotencyDefinition.cs");
        Assert.Contains("Exempt<IdempotencyDeclaration>", idempotency);

        Assert.Contains("internal sealed record BorrowCopyResult(Guid LoanId);", Read(temp.Path, expected, "BorrowCopyResult.cs"));

        var endpoint = Read(temp.Path, expected, "BorrowCopyEndpoint.cs");
        Assert.Contains("app.MapPost(\"/api/lending/borrow-copy\", HandleAsync).WithName(\"BorrowCopy\");", endpoint);

        Assert.Contains("internal sealed record BorrowCopyRequest(", Read(temp.Path, expected, "BorrowCopyRequest.cs"));
        Assert.Contains("internal sealed record BorrowCopyResponse(Guid LoanId);", Read(temp.Path, expected, "BorrowCopyResponse.cs"));
        Assert.Contains("10 files written, 0 files skipped.", result.Stdout);
    }

    /// <summary>Scaffold writes the eight list-loans files with no validator, audit or idempotency.</summary>
    [Fact]
    public void ScaffoldWritesQueryFilesFromTheRecord()
    {
        using var temp = ModelFixture.Prepare("small");
        RemoveStaleScaffoldOutputs(temp.Path);
        var result = Run("use-case/lending.list-loans", "--root", temp.Path);

        Assert.Equal(0, result.Status);

        var expected = AppFiles("Loans", "ListLoans", "ListLoansQuery", query: true)
            .Concat(WebFiles("Loans", "ListLoans"))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToList();

        Assert.Equal(8, expected.Count);

        foreach (var relative in expected)
        {
            var file = Path.Combine(temp.Path, relative);
            Assert.True(File.Exists(file), $"scaffolded file {file}");
            Assert.Contains("/// <implements>use-case/lending.list-loans</implements>", File.ReadAllText(file));
        }

        var handler = Read(temp.Path, expected, "ListLoansQueryHandler.cs");
        Assert.Contains("private readonly IQuerySession _session;", handler);
        Assert.Contains("new ListLoansResult([], [])", handler);

        var queryResult = Read(temp.Path, expected, "ListLoansResult.cs");
        Assert.Contains("IReadOnlyList<ListLoansResult.LoanRow> Loans", queryResult);
        Assert.Contains("IReadOnlyList<ListLoansResult.HoldRow> Holds", queryResult);
        Assert.Contains("DateOnly DueDate", queryResult);

        var endpoint = Read(temp.Path, expected, "ListLoansEndpoint.cs");
        Assert.Contains("app.MapGet(\"/api/lending/list-loans\", HandleAsync).WithName(\"ListLoans\");", endpoint);
    }

    /// <summary>A second run skips every file and writes nothing.</summary>
    [Fact]
    public void ScaffoldSecondRunSkipsEveryFile()
    {
        using var temp = ModelFixture.Prepare("small");
        RemoveStaleScaffoldOutputs(temp.Path);
        Assert.Equal(0, Run("use-case/lending.borrow-copy", "--root", temp.Path).Status);

        var result = Run("use-case/lending.borrow-copy", "--root", temp.Path);

        Assert.Equal(0, result.Status);
        Assert.Contains("Skipped (exists)", result.Stdout);
        Assert.Contains("0 files written, 10 files skipped.", result.Stdout);
        Assert.DoesNotContain("Created", result.Stdout);
    }

    /// <summary>--diff prints each file it would create with its full content and writes nothing.</summary>
    [Fact]
    public void ScaffoldDiffPrintsFilesWithoutWriting()
    {
        using var temp = ModelFixture.Prepare("small");
        RemoveStaleScaffoldOutputs(temp.Path);
        var result = Run("use-case/lending.borrow-copy", "--root", temp.Path, "--diff");

        Assert.Equal(0, result.Status);
        Assert.Contains("Expects aggregate act Loan.Borrow for use-case/lending.borrow-copy.", result.Stdout);
        Assert.Contains("BorrowCopyCommand.cs", result.Stdout);
        Assert.Contains("loan.Borrow();", result.Stdout);
        Assert.False(File.Exists(Path.Combine(temp.Path, "apps", "api", "src", "Modules", "Library.Lending", "Application", "Loans", "BorrowCopy", "BorrowCopyCommand.cs")), "no file written");
    }

    /// <summary>A use case reached outside a web entry point gets no endpoint files.</summary>
    [Fact]
    public void ScaffoldWritesNoEndpointWithoutAWebEntryPoint()
    {
        using var temp = ModelFixture.Prepare("small");
        RemoveStaleScaffoldOutputs(temp.Path);
        var record = Path.Combine(temp.Path, ".litespec", "behavior", "modules", "lending", "aggregates", "loans", "use-cases", "borrow-copy", "borrow-copy.yaml");
        File.WriteAllText(record, File.ReadAllText(record).Replace("- entry-point/library-app", "- module/lending", StringComparison.Ordinal));

        var result = Run("use-case/lending.borrow-copy", "--root", temp.Path);

        Assert.Equal(0, result.Status);
        var web = Path.Combine(temp.Path, "apps", "api", "src", "Modules", "Library.Lending", "WebApi");
        Assert.False(Directory.Exists(web), "no WebApi folder");
        Assert.Contains("7 files written, 0 files skipped.", result.Stdout);
    }

    /// <summary>--help prints the usage text, and bad input is a usage or scaffold error.</summary>
    [Fact]
    public void ScaffoldHelpAndUsageErrors()
    {
        using var output = new StringWriter();
        using var error = new StringWriter();

        Assert.Equal(0, ScaffoldCommand.Run(output, error, ["--help"]));
        Assert.Contains("litespec scaffold", output.ToString());

        Assert.Equal(2, Run("use-case/lending.borrow-copy", "--root", "no-such-root").Status);

        using var temp = ModelFixture.Prepare("small");
        Assert.Equal(2, Run("--root", temp.Path).Status);

        var unknown = Run("use-case/lending.no-such-use-case", "--root", temp.Path);
        Assert.Equal(1, unknown.Status);
        Assert.Contains("not found", unknown.Stderr);

        Assert.Equal(2, Run("use-case/lending.borrow-copy", "--root", temp.Path, "--bogus").Status);
    }

    /// <summary>The expected Application files of one scaffolded use case.</summary>
    /// <param name="aggregates">The plural aggregate folder.</param>
    /// <param name="stem">The use case stem.</param>
    /// <param name="message">The message type name.</param>
    /// <param name="query">Whether the use case is a query.</param>
    /// <returns>The consumer-relative file paths.</returns>
    private static List<string> AppFiles(string aggregates, string stem, string message, bool query = false)
    {
        var folder = Path.Combine("apps", "api", "src", "Modules", "Library.Lending", "Application", aggregates, stem);
        var files = new List<string>
        {
            Path.Combine(folder, message + ".cs"),
            Path.Combine(folder, message + "Handler.cs"),
        };

        if (!query)
        {
            files.Add(Path.Combine(folder, message + "Validator.cs"));
        }

        files.Add(Path.Combine(folder, stem + "Result.cs"));
        files.Add(Path.Combine(folder, message + "AuthorizationDefinition.cs"));

        if (!query)
        {
            files.Add(Path.Combine(folder, message + "IdempotencyDefinition.cs"));
        }

        files.Add(Path.Combine(folder, message + "TenancyDefinition.cs"));

        return files;
    }

    /// <summary>The expected WebApi files of one scaffolded use case.</summary>
    /// <param name="aggregates">The plural aggregate folder.</param>
    /// <param name="stem">The use case stem.</param>
    /// <returns>The consumer-relative file paths.</returns>
    private static List<string> WebFiles(string aggregates, string stem)
    {
        var folder = Path.Combine("apps", "api", "src", "Modules", "Library.Lending", "WebApi", aggregates, stem);

        return
        [
            Path.Combine(folder, stem + "Endpoint.cs"),
            Path.Combine(folder, stem + "Request.cs"),
            Path.Combine(folder, stem + "Response.cs"),
        ];
    }

    /// <summary>Reads one scaffolded file by its file name.</summary>
    /// <param name="root">The prepared fixture folder.</param>
    /// <param name="expected">The expected relative paths.</param>
    /// <param name="file">The file name to read.</param>
    /// <returns>The file text.</returns>
    private static string Read(string root, List<string> expected, string file) =>
        File.ReadAllText(Path.Combine(root, expected.Single(path => path.EndsWith(file, StringComparison.Ordinal))));

    /// <summary>Removes the pre-scaffold consumer code the fixture ships, so each case scaffolds into a clean tree.</summary>
    /// <param name="root">The prepared fixture folder.</param>
    static void RemoveStaleScaffoldOutputs(string root)
    {
        foreach (var relative in new[]
        {
            Path.Combine("apps", "api", "src", "Modules", "Library.Lending", "Application"),
            Path.Combine("apps", "api", "src", "Modules", "Library.Lending", "WebApi"),
        })
        {
            var folder = Path.Combine(root, relative);

            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, recursive: true);
            }
        }
    }

    /// <summary>Runs the scaffold command in process with captured output.</summary>
    /// <param name="args">The command arguments.</param>
    /// <returns>The exit code and the two output streams of the run.</returns>
    private static (int Status, string Stdout, string Stderr) Run(params string[] args)
    {
        using var output = new StringWriter();
        using var error = new StringWriter();
        var status = ScaffoldCommand.Run(output, error, args);

        return (status, output.ToString(), error.ToString());
    }
}
