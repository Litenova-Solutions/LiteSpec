using LiteSpec.Cli.Model;
using LiteSpec.Cli.Model.Checks.Implementation.Handlers;
using LiteSpec.Cli.Model.Code;
using LiteSpec.Cli.Tests.Support;
using LiteSpec.Model;

namespace LiteSpec.Cli.Tests.Model.Checks;

/// <summary>Proves the refusal declaration check: every refusal code a handler, a root method it calls or a one-hop helper throws is one its use case declares in a result.</summary>
public sealed class RefusalDeclarationCheckTests
{
    /// <summary>A handler throwing a declared refusal reports nothing.</summary>
    /// <proves>litespec/core/backend-application#return-only-declared-results-and-refusals</proves>
    [Fact]
    public void DeclaredHandlerRefusalReportsNothing()
    {
        using var temp = TestCli.Temp("checks");
        var folder = ApplicationFolder(temp.Path);
        var model = RefusalModel(temp.Path, folder, """{"kind": "command"}""");
        model.Code.Handlers.Add(new CodeHandler(
            "use-case/lending.borrow-copy", "lending", "borrow-copy", "BorrowCopy", "command",
            Path.Combine(folder, "BorrowCopyCommandHandler.cs"),
            Refusals: [new CodeRefusal("lending.overdue", Path.Combine(folder, "BorrowCopyCommandHandler.cs"), 9)]));
        model.Records.Add(TestModel.Record(
            RecordTypes.Result, "result/borrow-copy.overdue", Path.Combine(temp.Path, ".litespec", "borrow-copy.yaml"),
            """{"outcome": "refusal", "refusal": "refusal/lending.overdue"}"""));

        Assert.Empty(Run(model));
    }

    /// <summary>A handler throwing an undeclared refusal reports refusal-undeclared.</summary>
    /// <proves>litespec/core/backend-application#return-only-declared-results-and-refusals</proves>
    [Fact]
    public void UndeclaredHandlerRefusalReports()
    {
        using var temp = TestCli.Temp("checks");
        var folder = ApplicationFolder(temp.Path);
        var model = RefusalModel(temp.Path, folder, """{"kind": "command"}""");
        model.Code.Handlers.Add(new CodeHandler(
            "use-case/lending.borrow-copy", "lending", "borrow-copy", "BorrowCopy", "command",
            Path.Combine(folder, "BorrowCopyCommandHandler.cs"),
            Refusals: [new CodeRefusal("lending.overdue", Path.Combine(folder, "BorrowCopyCommandHandler.cs"), 9)]));

        var finding = Assert.Single(Run(model));

        Assert.Equal("refusal-undeclared", finding.Code);
        Assert.Equal("Use case 'use-case/lending.borrow-copy' throws 'lending.overdue', which no result of the use case declares.", finding.Message);
        Assert.Equal("use-case/lending.borrow-copy", finding.Subject);
    }

    /// <summary>A root method the handler calls throwing a declared refusal reports nothing, marked or not.</summary>
    /// <proves>litespec/core/backend-application#return-only-declared-results-and-refusals</proves>
    [Fact]
    public void DeclaredRootRefusalReportsNothing()
    {
        using var temp = TestCli.Temp("checks");
        var folder = ApplicationFolder(temp.Path);
        var model = RefusalModel(temp.Path, folder, """{"kind": "command"}""");
        WriteHandlerWithRootCall(model, folder);
        WriteSource(folder, "Book.cs", """
            internal sealed class Book
            {
                public void Approve(Guid approverId)
                {
                    throw new BookNotAvailableException();
                }
            }
            """);
        model.Code.AggregateMethods.Add(new AggregateMethod("Book", "Approve", [], Path.Combine(folder, "Book.cs"), 3));
        model.Code.Literals.Add(new CodeLiteral("lending.not-available", Path.Combine(folder, "BookNotAvailableException.cs"), 7, "BookNotAvailableException", true));
        model.Records.Add(TestModel.Record(
            RecordTypes.Result, "result/borrow-copy.not-available", Path.Combine(temp.Path, ".litespec", "borrow-copy.yaml"),
            """{"outcome": "refusal", "refusal": "refusal/lending.not-available"}"""));

        Assert.Empty(Run(model));
    }

    /// <summary>A root method the handler calls throwing an undeclared refusal reports refusal-undeclared from its place.</summary>
    /// <proves>litespec/core/backend-application#return-only-declared-results-and-refusals</proves>
    [Fact]
    public void UndeclaredRootRefusalReportsFromPlace()
    {
        using var temp = TestCli.Temp("checks");
        var folder = ApplicationFolder(temp.Path);
        var model = RefusalModel(temp.Path, folder, """{"kind": "command"}""");
        WriteHandlerWithRootCall(model, folder);
        WriteSource(folder, "Book.cs", """
            internal sealed class Book
            {
                public void Approve(Guid approverId)
                {
                    throw new BookNotAvailableException();
                }
            }
            """);
        model.Code.AggregateMethods.Add(new AggregateMethod("Book", "Approve", [], Path.Combine(folder, "Book.cs"), 3));
        model.Code.Literals.Add(new CodeLiteral("lending.not-available", Path.Combine(folder, "BookNotAvailableException.cs"), 7, "BookNotAvailableException", true));

        var finding = Assert.Single(Run(model));

        Assert.Equal("refusal-undeclared", finding.Code);
        Assert.Equal("Use case 'use-case/lending.borrow-copy' throws 'lending.not-available' from 'Book.Approve', which no result of the use case declares.", finding.Message);
        Assert.Equal("use-case/lending.borrow-copy", finding.Subject);
    }

    /// <summary>A helper the handler calls throwing an undeclared refusal reports refusal-undeclared via its place.</summary>
    /// <proves>litespec/core/backend-application#return-only-declared-results-and-refusals</proves>
    [Fact]
    public void UndeclaredHelperRefusalReportsViaPlace()
    {
        using var temp = TestCli.Temp("checks");
        var folder = ApplicationFolder(temp.Path);
        var model = RefusalModel(temp.Path, folder, """{"kind": "command"}""");
        WriteSource(folder, "BorrowCopyCommandHandler.cs", """
            internal sealed class BorrowCopyCommandHandler : ICommandHandler<BorrowCopyCommand, BorrowCopyCommandResult>
            {
                public Task<BorrowCopyCommandResult> HandleAsync(BorrowCopyCommand command, CancellationToken cancellationToken)
                {
                    Pricing.Quote(command.Deposit);
                    throw new InvalidOperationException();
                }
            }
            """);
        model.Code.Handlers.Add(new CodeHandler(
            "use-case/lending.borrow-copy", "lending", "borrow-copy", "BorrowCopy", "command",
            Path.Combine(folder, "BorrowCopyCommandHandler.cs"),
            "BorrowCopyCommandHandler",
            MessageType: "AbsentCommand"));
        WriteSource(folder, "Pricing.cs", """
            internal static class Pricing
            {
                public static void Quote(decimal amount)
                {
                    throw new PricingRefusedException();
                }
            }
            """);
        model.Code.Literals.Add(new CodeLiteral("lending.price-refused", Path.Combine(folder, "PricingRefusedException.cs"), 7, "PricingRefusedException", true));

        var finding = Assert.Single(Run(model));

        Assert.Equal("refusal-undeclared", finding.Code);
        Assert.Equal("Use case 'use-case/lending.borrow-copy' throws 'lending.price-refused' via 'Pricing.Quote', which no result of the use case declares.", finding.Message);
        Assert.Equal("use-case/lending.borrow-copy", finding.Subject);
    }

    /// <summary>A derived generic result covers the code its use case throws.</summary>
    /// <proves>litespec/core/backend-application#return-only-declared-results-and-refusals</proves>
    [Fact]
    public void DerivedGenericResultCovers()
    {
        using var temp = TestCli.Temp("checks");
        var folder = ApplicationFolder(temp.Path);
        var model = RefusalModel(temp.Path, folder, """{"kind": "command"}""");
        model.Code.Handlers.Add(new CodeHandler(
            "use-case/lending.borrow-copy", "lending", "borrow-copy", "BorrowCopy", "command",
            Path.Combine(folder, "BorrowCopyCommandHandler.cs"),
            Refusals: [new CodeRefusal("not-found", Path.Combine(folder, "BorrowCopyCommandHandler.cs"), 9)]));
        model.Records.Add(TestModel.Record(
            RecordTypes.Result, "result/borrow-copy.not-found", Path.Combine(temp.Path, ".litespec", "borrow-copy.yaml"),
            """{"outcome": "refusal", "code": "not-found"}"""));

        Assert.Empty(Run(model));
    }

    /// <summary>A root method throwing an exception with no refusal code reports nothing.</summary>
    /// <proves>litespec/core/backend-application#return-only-declared-results-and-refusals</proves>
    [Fact]
    public void UncodedThrowReportsNothing()
    {
        using var temp = TestCli.Temp("checks");
        var folder = ApplicationFolder(temp.Path);
        var model = RefusalModel(temp.Path, folder, """{"kind": "command"}""");
        WriteHandlerWithRootCall(model, folder);
        WriteSource(folder, "Book.cs", """
            internal sealed class Book
            {
                public void Approve(Guid approverId)
                {
                    throw new InvalidOperationException("No approver.");
                }
            }
            """);
        model.Code.AggregateMethods.Add(new AggregateMethod("Book", "Approve", [], Path.Combine(folder, "Book.cs"), 3));

        Assert.Empty(Run(model));
    }

    /// <summary>A called method with no file keeps no refusal rule.</summary>
    /// <proves>litespec/core/backend-application#return-only-declared-results-and-refusals</proves>
    [Fact]
    public void UnknownHelperReportsNothing()
    {
        using var temp = TestCli.Temp("checks");
        var folder = ApplicationFolder(temp.Path);
        var model = RefusalModel(temp.Path, folder, """{"kind": "command"}""");
        WriteSource(folder, "BorrowCopyCommandHandler.cs", """
            internal sealed class BorrowCopyCommandHandler : ICommandHandler<BorrowCopyCommand, BorrowCopyCommandResult>
            {
                public Task<BorrowCopyCommandResult> HandleAsync(BorrowCopyCommand command, CancellationToken cancellationToken)
                {
                    UnknownHelper.Check(command.CopyId);
                    throw new InvalidOperationException();
                }
            }
            """);
        model.Code.Handlers.Add(new CodeHandler(
            "use-case/lending.borrow-copy", "lending", "borrow-copy", "BorrowCopy", "command",
            Path.Combine(folder, "BorrowCopyCommandHandler.cs"),
            "BorrowCopyCommandHandler",
            MessageType: "AbsentCommand"));

        Assert.Empty(Run(model));
    }

    /// <summary>Runs the refusal declaration check and keeps its findings.</summary>
    /// <param name="model">The project model to check.</param>
    /// <returns>The refusal-undeclared findings.</returns>
    static List<Finding> Run(ProjectModel model)
    {
        var findings = new List<Finding>();
        RefusalDeclarationChecks.Run(model, findings);

        return findings.Where(finding => finding.Code == "refusal-undeclared").ToList();
    }

    /// <summary>A model with the use case record and no handler.</summary>
    /// <param name="root">The fixture root folder.</param>
    /// <param name="folder">The Application folder holding the handler.</param>
    /// <param name="json">The use case record fields as a JSON object.</param>
    /// <returns>The project model holding the record.</returns>
    static ProjectModel RefusalModel(string root, string folder, string json)
    {
        return TestModel.Model(
            TestModel.Config(root),
            TestModel.Record(RecordTypes.UseCase, "use-case/lending.borrow-copy", Path.Combine(root, ".litespec", "borrow-copy.yaml"), json));
    }

    /// <summary>Writes a handler loading its book through its repository and approving it, and adds the handler row.</summary>
    /// <param name="model">The project model the handler row is added to.</param>
    /// <param name="folder">The Application folder holding the handler.</param>
    static void WriteHandlerWithRootCall(ProjectModel model, string folder)
    {
        WriteSource(folder, "BorrowCopyCommandHandler.cs", """
            internal sealed class BorrowCopyCommandHandler : ICommandHandler<BorrowCopyCommand, BorrowCopyCommandResult>
            {
                private readonly IBookRepository _books;

                public BorrowCopyCommandHandler(IBookRepository books)
                {
                    _books = books;
                }

                public async Task<BorrowCopyCommandResult> HandleAsync(BorrowCopyCommand command, CancellationToken cancellationToken)
                {
                    var book = await _books.GetAsync(command.BookId, cancellationToken);
                    book.Approve(command.ApproverId);
                    throw new InvalidOperationException();
                }
            }
            """);
        model.Code.Handlers.Add(new CodeHandler(
            "use-case/lending.borrow-copy", "lending", "borrow-copy", "BorrowCopy", "command",
            Path.Combine(folder, "BorrowCopyCommandHandler.cs"),
            "BorrowCopyCommandHandler",
            MessageType: "AbsentCommand"));
    }

    /// <summary>The use case Application folder holding the handler and its messages.</summary>
    /// <param name="root">The fixture root folder.</param>
    /// <returns>The Application folder.</returns>
    static string ApplicationFolder(string root)
    {
        var folder = Path.Combine(root, "apps", "api", "src", "Modules", "Library.Lending", "Application", "BorrowCopy");
        Directory.CreateDirectory(folder);

        return folder;
    }

    /// <summary>Writes one source file holding the text.</summary>
    /// <param name="folder">The Application folder holding the file.</param>
    /// <param name="file">The source file name.</param>
    /// <param name="text">The file text.</param>
    static void WriteSource(string folder, string file, string text)
    {
        File.WriteAllText(Path.Combine(folder, file), text + "\n");
    }
}
