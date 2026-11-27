using LiteSpec.Cli.Model.Configuration;
using LiteSpec.Cli.Model.Readers.Dotnet.Backend;
using LiteSpec.Cli.Tests.Support;

namespace LiteSpec.Cli.Tests.Model.Readers;

/// <summary>Proves the backend reader reads and parses each file once per model run.</summary>
public sealed class DotnetParseOnceTests
{
    /// <summary>Each Backend File Is Parsed Once Per Read.</summary>
    /// <proves>litespec/packs/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void EachBackendFileIsParsedOncePerRead()
    {
        using var temp = TestCli.Temp("reader-parse-once");
        WriteProject(temp.Path);
        WriteSource(temp.Path, Path.Combine("App", "Application", "BorrowCopy"), "BorrowCopyCommandHandler.cs", """
            namespace Library.App.Application.BorrowCopy;

            /// <implements>use-case/lending.borrow-copy</implements>
            public sealed class BorrowCopyCommandHandler : ICommandHandler<BorrowCopyCommand>
            {
                public Task Handle(BorrowCopyCommand command) => Task.CompletedTask;
            }
            """);
        WriteSource(temp.Path, Path.Combine("App", "Domain", "Loans", "Events"), "LoanBorrowedEvent.cs", """
            namespace Library.App.Domain.Loans.Events;

            /// <implements>domain-event/lending.loan-borrowed</implements>
            public sealed record LoanBorrowedEvent(Guid LoanId) : IDomainEvent;
            """);
        WriteSource(temp.Path, Path.Combine("App", "WebApi", "BorrowCopy"), "BorrowCopyEndpoint.cs", """
            namespace Library.App.WebApi.BorrowCopy;

            /// <implements>use-case/lending.borrow-copy</implements>
            public sealed class BorrowCopyEndpoint : IEndpoint
            {
                public void Map(WebApplication app)
                {
                    app.MapPost("/api/borrow", Handle).WithName("BorrowCopy");
                }

                private static void Handle()
                {
                }
            }
            """);

        var counts = new Dictionary<string, int>(StringComparer.Ordinal);
        var gate = new object();
        var code = new DotnetBackendReader().Read(ConsumerConfig.Load(temp.Path), file =>
        {
            lock (gate)
            {
                counts[file] = counts.TryGetValue(file, out var seen) ? seen + 1 : 1;
            }
        });

        Assert.Equal(3, counts.Count);
        Assert.All(counts.Values, count => Assert.Equal(1, count));
        Assert.Single(code.Handlers);
        Assert.Single(code.Events);
        Assert.Equal(2, code.Endpoints.Count);
    }

    static void WriteProject(string root) =>
        File.WriteAllText(
            Path.Combine(root, ".litespec/project.yaml"),
            """
            {"stacks": ["dotnet"], "stackConfig": {"dotnet": {
              "modules": "src/App", "webapi": "src/WebApi"}}}
            """);

    static void WriteSource(string root, string project, string file, string text)
    {
        var folder = Path.Combine(root, "src", project);
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, file), text);
    }
}
