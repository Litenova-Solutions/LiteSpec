using LiteSpec.Model;
using LiteSpec.Model.Readers.Dotnet;
using LiteSpec.Tests.Support;

namespace LiteSpec.Tests.Model.Readers;

/// <summary>Proves the backend reader reads and parses each file once per model build.</summary>
public sealed class DotnetParseOnceTests
{
    /// <summary>Each Backend File Is Parsed Once Per Read.</summary>
    /// <proves>litespec/backend/dotnet/stack#read-net-code-through-the-stack-reader</proves>
    [Fact]
    public void Each_Backend_File_Is_Parsed_Once_Per_Read()
    {
        using var temp = Cli.Temp("reader-parse-once");
        WriteProject(temp.Path);
        WriteSource(temp.Path, "App", "BorrowCopyCommandHandler.cs", """
            namespace Library.Application.Lending.Loans.BorrowCopy;

            /// <implements>use-case/lending.borrow-copy</implements>
            public sealed class BorrowCopyCommandHandler : ICommandHandler<BorrowCopyCommand>
            {
                public Task Handle(BorrowCopyCommand command) => Task.CompletedTask;
            }
            """);
        WriteSource(temp.Path, "Domain", "LoanBorrowedEvent.cs", """
            namespace Library.Domain.Lending.Loans.Events;

            /// <implements>event/lending.loan-borrowed</implements>
            public sealed record LoanBorrowedEvent(Guid LoanId) : IDomainEvent;
            """);
        WriteSource(temp.Path, "WebApi", "BorrowCopyEndpoint.cs", """
            namespace Library.WebApi.Lending.Loans.BorrowCopy;

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
            Path.Combine(root, "litespec.project.json"),
            """
            {"stacks": ["dotnet"], "stackConfig": {"dotnet": {
              "application": "src/App", "domain": "src/Domain",
              "webapi": "src/WebApi", "infrastructure": "src/Infra"}}}
            """);

    static void WriteSource(string root, string project, string file, string text)
    {
        var folder = Path.Combine(root, "src", project);
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, file), text);
    }
}
